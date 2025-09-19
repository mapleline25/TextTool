using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace TextTool.Library.Utils;

public sealed class WindowsCommandLine : ICommandLine
{
    private const int _ExecuteDelay = 50;
    private const int _ExitTimeOut = 10000;
    private const string _InitResult = "init";
    private const string _InitCommand = $"0 & @echo off & chcp 65001 && echo {_InitResult}";
    private const string _MainResult = "command";
    private const string _MainCommand = $"for /l %a in (0) do (echo {_MainResult} && SET /P input= && !input!)";
    private const string _StartCommandPrefix = "start \"\" /B ";
    private const string _ExitCommand = "EXIT";

    private readonly Process _process;
    private readonly Lock _lock = new();
    private readonly List<Exception> _errors;
    private string _executeFieName;
    private char[]? _buffer;
    private int _bufferCount;

    private AutoResetEvent? _syncEvent;
    private AutoResetEvent? _exitEvent;
    private TaskCompletionSource _asyncTaskSource;
    private Task _asyncTask = Task.CompletedTask;
    private OperationState _state = OperationState.None;
    private string? _awaitingResult = null;
    private bool _supressError = false;
    private bool _started = false;
    private bool _exited = false;
    private bool _disposed = false;

    public WindowsCommandLine()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new NotSupportedException($"{nameof(WindowsCommandLine)} does not support non-Windows operating system.");
        }
        
        _errors = [];
        _process = new();
    }

    public void Start()
    {
        ThrowIfDisposed();
        ThrowIfExited();

        if (_started)
        {
            return;
        }

        Encoding encoding = new UTF8Encoding(true);

        ProcessStartInfo info = new("cmd.exe", "/v")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            StandardInputEncoding = encoding,
            StandardOutputEncoding = encoding,
            StandardErrorEncoding = encoding,
        };

        _syncEvent = new(false);
        _exitEvent = new(false);

        _process.StartInfo = info;
        _process.EnableRaisingEvents = true;
        _process.OutputDataReceived += OnOutputDataReceived;
        _process.ErrorDataReceived += OnErrorDataReceived;
        _process.Exited += OnExited;
        _process.Start();

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _supressError = true;
        Execute(_InitCommand, _InitResult);
        Execute(_MainCommand, _MainResult);
        _supressError = false;
        _started = true;
    }

    private void OnExited(object? sender, EventArgs e)
    {
        _exitEvent.Set();
    }

    public void Execute(string fileName, ReadOnlySpan<char> arguments, bool waitForExit)
    {
        ThrowIfDisposed();
        ThrowIfExited();
        ThrowIfNotStarted();
        ThrowIfInOperation();
        ValidateFileName(fileName);

        _executeFieName = fileName;

        PrepareBuffer(GetBufferLength(fileName, arguments));
        PrepareCommand(fileName, arguments, waitForExit);

        Execute(BufferSpan, _MainResult);
    }

    private void Execute(ReadOnlySpan<char> cmd, string? awaitingResult)
    {
        _awaitingResult = awaitingResult;

        if (awaitingResult != null)
        {
            _state = OperationState.Sync;
        }

        Thread.Sleep(_ExecuteDelay);

        try
        {
            _process.StandardInput.WriteLine(cmd);
        }
        finally
        {
            DisposeBuffer();
        }

        if (awaitingResult != null)
        {
            _syncEvent.WaitOne();
            ThrowIfError();
        }
    }

    public Task ExecuteAsync(string fileName, ReadOnlySpan<char> arguments, bool waitForExit, CancellationToken token = default)
    {
        ThrowIfDisposed();
        ThrowIfExited();
        ThrowIfNotStarted();
        ThrowIfInOperation();
        ValidateFileName(fileName);

        _executeFieName = fileName;

        PrepareBuffer(GetBufferLength(fileName, arguments));
        PrepareCommand(fileName, arguments, waitForExit);

        return ExecuteAsync(BufferMemory, _MainResult, token);
    }

    private Task ExecuteAsync(ReadOnlyMemory<char> cmd, string? awaitingResult, CancellationToken token = default)
    {
        _awaitingResult = awaitingResult;

        Task? task = null;
        if (awaitingResult != null)
        {
            _state = OperationState.Async;
            _asyncTaskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            task = _asyncTaskSource.Task;
        }

        _asyncTask = ExecuteCoreAsync(cmd, task, token);

        return _asyncTask;
    }

    private async Task ExecuteCoreAsync(ReadOnlyMemory<char> cmd, Task? resultTask, CancellationToken token)
    {
        await Task.Delay(_ExecuteDelay, token);

        try
        {
            await _process.StandardInput.WriteLineAsync(cmd, token);
        }
        finally
        {
            DisposeBuffer();
        }

        if (resultTask != null)
        {
            await resultTask;
            ThrowIfError();
        }
    }

    public void Exit()
    {
        ThrowIfDisposed();
        ThrowIfNotStarted();

        if (_exited)
        {
            return;
        }

        _exited = true;

        ExitCore();
    }

    private void ExitCore()
    {
        if (!_asyncTask.IsCompleted)
        {
            OnSetResult();
            _asyncTask.GetAwaiter().GetResult();
        }

        Execute(_ExitCommand, null);
        Close();
    }

    public Task ExitAsync()
    {
        ThrowIfDisposed();
        ThrowIfNotStarted();

        if (_exited)
        {
            return Task.CompletedTask;
        }

        _exited = true;

        return ExitCoreAsync();
    }

    private async Task ExitCoreAsync()
    {
        if (!_asyncTask.IsCompleted)
        {
            OnSetResult();
            await _asyncTask;
        }

        await ExecuteAsync(_ExitCommand.AsMemory(), null);
        Close();
    }

    private void Close()
    {
        _process.StandardInput.Close();
        
        if (_exitEvent.WaitOne(_ExitTimeOut))
        {
            _process.CancelOutputRead();
            _process.CancelErrorRead();
        }
        else
        {
            _process.Kill();
            _exitEvent.WaitOne();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_started && !_exited)
        {
            try
            {
                _supressError = true;
                ExitCore();
            }
            catch { }
        }
        
        _process.OutputDataReceived -= OnOutputDataReceived;
        _process.ErrorDataReceived -= OnErrorDataReceived;
        _process.Exited -= OnExited;
        _process.Dispose();
        _syncEvent?.Dispose();
        _exitEvent?.Dispose();
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is not string data)
        {
            return;
        }

        ReadOnlySpan<char> chars = data.AsSpan().Trim();

        if (chars.SequenceEqual(_awaitingResult))
        {
            OnSetResult();
        }
    }

    private void OnSetResult()
    {
        lock (_lock)
        {
            _awaitingResult = null;

            if (_state == OperationState.Async)
            {
                _state = OperationState.None;
                _asyncTaskSource.TrySetResult();
            }
            else if (_state == OperationState.Sync)
            {
                _state = OperationState.None;
                _syncEvent.Set();
            }
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        string? error = e.Data;

        if (error == null || _supressError)
        {
            return;
        }

        _errors.Add(new Exception(string.IsNullOrEmpty(error) ? $"Error in '{_executeFieName}'" : error));
        OnSetResult();
    }

    private static int GetBufferLength(ReadOnlySpan<char> fileName, ReadOnlySpan<char> arguments)
    {
        int defaultLength = 1024;
        int length = _StartCommandPrefix.Length + fileName.Length + arguments.Length + 3; // start "" /B ["]fileName["][]arguments
        return length < defaultLength ? defaultLength : length;
    }

    private void PrepareCommand(ReadOnlySpan<char> fileName, ReadOnlySpan<char> arguments, bool wait)
    {
        WriteBuffer(_StartCommandPrefix);

        if (wait)
        {
            WriteBuffer("/WAIT ");
        }

        WriteBuffer("\"");
        WriteBuffer(fileName);
        WriteBuffer("\"");

        if (!arguments.IsEmpty)
        {
            WriteBuffer(" ");
            WriteBuffer(arguments);
        }
    }

    private ReadOnlySpan<char> BufferSpan => new(_buffer, 0, _bufferCount);

    private ReadOnlyMemory<char> BufferMemory => new(_buffer, 0, _bufferCount);

    private void WriteBuffer(ReadOnlySpan<char> chars)
    {
        int length = _bufferCount + chars.Length;
        PrepareBuffer(length);
        chars.CopyTo(_buffer.AsSpan(_bufferCount));
        _bufferCount = length;
    }

    private void PrepareBuffer(int length)
    {
        if (_buffer == null)
        {
            _buffer = ArrayPool<char>.Shared.Rent(length);
            _bufferCount = 0;
        }
        else if (length > _buffer.Length)
        {
            char[] buffer = ArrayPool<char>.Shared.Rent(length);
            _buffer.AsSpan(0, _bufferCount).CopyTo(buffer);
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = buffer;
        }
    }

    private void DisposeBuffer()
    {
        if ( _buffer != null)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = null;
            _bufferCount = 0;
        }
    }

    private static void ValidateFileName(string fileName)
    {
        if (!FileService.PathExists(fileName))
        {
            throw new FileNotFoundException($"Cannot access file:\n{fileName}");
        }
    }

    private void ThrowIfNotStarted()
    {
        if (!_started)
        {
            throw new InvalidOperationException("The process has not started.");
        }
    }

    private void ThrowIfError()
    {
        lock (_lock)
        {
            if (_errors.Count > 0)
            {
                Exception[] exceptions = _errors.ToArray();
                _errors.Clear();
                throw new AggregateException(exceptions);
            }
        }
    }

    private void ThrowIfInOperation()
    {
        if (_state != OperationState.None)
        {
            throw new InvalidOperationException("The process has a running operation.");
        }
    }

    private void ThrowIfExited()
    {
        if (_exited)
        {
            throw new ObjectDisposedException(GetType().Name, "The process has exited.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name, "The process has been disposed.");
        }
    }

    private enum OperationState
    {
        None, Sync, Async
    }
}
