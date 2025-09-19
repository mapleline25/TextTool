using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using TextTool.Library.Utils;

namespace TextTool.Core.Models;

public class FileItem : ObservableObject
{
    private static readonly DateTime _LocalTimeWithZeroFileTime = DateTime.FromFileTimeUtc(0).ToLocalTime();
    private static uint _CurrentId;
    private readonly Lock _lock = new();
    private readonly uint _id;
    private bool _inUse = false;
    private bool _existed = false;

    protected readonly string _filePath;
    protected readonly string _fileDirectory;
    protected readonly string _fileName;
    protected readonly string _fileType;
    protected DateTime? _lastModifiedTime;
    protected Encoding? _encoding = null;
    protected string _encodingName;
    
    public FileItem(string filePath, string? fileType = null)
        : this(filePath, Path.GetDirectoryName(filePath), Path.GetFileName(filePath), fileType)
    {
    }

    public FileItem(string fileDirectory, string fileName, string? fileType = null)
        : this(Path.Join(fileDirectory, fileName), fileDirectory, fileName, fileType)
    {
    }

    private FileItem(string filePath, string? fileDirectory, string? fileName, string? fileType)
    {
        _id = Interlocked.Add(ref _CurrentId, 1);
        _filePath = filePath;
        _fileDirectory = fileDirectory ?? string.Empty;
        _fileName = fileName ?? string.Empty;

        if (string.IsNullOrEmpty(fileType))
        {
            string? ext = Path.GetExtension(_filePath);
            _fileType = string.IsNullOrEmpty(ext) ? string.Empty : ext.ToLower().Substring(1);
        }
        else
        {
            _fileType = fileType;
        }
    }

    public uint Id => _id;

    public bool InUse => _inUse;

    public string FilePath => _filePath;

    public string FileDirectory => _fileDirectory;

    public string FileName => _fileName;

    public string FileType => _fileType;

    public Encoding? Encoding => _encoding;

    public bool Existed
    {
        get => _existed;
        private set => SetProperty(ref _existed, value);
    }

    public DateTime? LastModifiedTime
    {
        get => _lastModifiedTime;
        private set => SetProperty(ref _lastModifiedTime, value);
    }

    public string EncodingName
    {
        get => _encodingName;
        private set => SetProperty(ref _encodingName, value);
    }

    public bool NeedRefresh => LastModifiedTime != GetFileLastWriteTime();

    public void Refresh()
    {
        try
        {
            if (UpdateLastModifiedTime())
            {
                using Stream stream = File.OpenRead(_filePath);
                MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate((int)stream.Length);

                _ = stream.Read(buffer.Span);

                UpdateEncoding(buffer);
                UpdateProperties(buffer);
            }
            else
            {
                UpdateProperties(null);
            }

            Existed = true;
        }
        catch
        {
            Existed = false;
            throw;
        }
    }

    public async Task RefreshAsync(CancellationToken token = default)
    {
        try
        {
            if (UpdateLastModifiedTime())
            {
                using Stream stream = File.OpenRead(_filePath);
                using MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate((int)stream.Length);

                _ = await stream.ReadAsync(buffer.Memory, token);

                UpdateEncoding(buffer);
                UpdateProperties(buffer);
            }
            else
            {
                UpdateProperties(null);
            }

            Existed = true;
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                Existed = false;
            }
            throw;
        }
    }

    public void ConvertEncoding(Encoding? srcEncoding, Encoding dstEncoding, string? savePath)
    {
        srcEncoding = GetSourceEncoding(srcEncoding);

        try
        {
            MemoryOwner<byte>? outputBuffer;

            using (Stream stream = File.OpenRead(_filePath))
            {
                outputBuffer = TranscodingStreamReader.ReadToEnd(stream, srcEncoding, dstEncoding, false);
            }

            if (outputBuffer == null)
            {
                return;
            }

            using (outputBuffer)
            {
                File.WriteAllBytes(savePath != null ? savePath : _filePath, outputBuffer.Span);

                if (savePath == null)
                {
                    UpdateLastModifiedTime();
                    UpdateEncoding(dstEncoding);
                    UpdateProperties(outputBuffer);
                }
            }

            Existed = true;
        }
        catch
        {
            Existed = false;
            throw;
        }
    }

    public async Task ConvertEncodingAsync(Encoding? srcEncoding, Encoding dstEncoding, string? savePath, CancellationToken token = default)
    {
        srcEncoding = GetSourceEncoding(srcEncoding);

        try
        {
            MemoryOwner<byte>? outputBuffer;

            using (Stream stream = File.OpenRead(_filePath))
            {
                outputBuffer = await TranscodingStreamReader.ReadToEndAsync(stream, srcEncoding, dstEncoding, false, token);
            }

            if (outputBuffer == null)
            {
                return;
            }

            using (outputBuffer)
            {
                await File.WriteAllBytesAsync(savePath != null ? savePath : _filePath, outputBuffer.Memory, token);

                if (savePath == null)
                {
                    UpdateLastModifiedTime();
                    UpdateEncoding(dstEncoding);
                    UpdateProperties(outputBuffer);
                }
            }

            Existed = true;
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                Existed = false;
            }
            throw;
        }
    }

    public bool TakeAccess()
    {
        lock (_lock)
        {
            if (!_inUse)
            {
                _inUse = true;
                return true;
            }
            return false;
        }
    }

    public void ReleaseAccess()
    {
        lock (_lock)
        {
            _inUse = false;
        }
    }

    protected virtual void UpdateProperties(MemoryOwner<byte>? modifiedFileBuffer)
    {

    }

    protected virtual void ClearProperties()
    {
        LastModifiedTime = null;
        _encoding = null;
        EncodingName = string.Empty;
    }

    protected bool UpdateLastModifiedTime()
    {
        DateTime lastModifiedTime = GetFileLastWriteTime();

        if (LastModifiedTime != lastModifiedTime)
        {
            LastModifiedTime = lastModifiedTime;
            return true;
        }

        return false;
    }

    protected void UpdateEncoding(MemoryOwner<byte> buffer)
    {
        _encoding = TextEncoding.DetectEncoding(buffer);
        EncodingName = _encoding == null ? string.Empty : TextEncoding.GetEncodingName(_encoding) ?? string.Empty;
    }

    protected void UpdateEncoding(Encoding encoding)
    {
        _encoding = encoding;
        EncodingName = TextEncoding.GetEncodingName(_encoding) ?? string.Empty;
    }

    protected void UncheckChangeProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, string? propertyName = null)
    {
        OnPropertyChanging(propertyName);
        field = newValue;
        OnPropertyChanged(propertyName);
    }

    private DateTime GetFileLastWriteTime()
    {
        try
        {
            DateTime time = File.GetLastWriteTime(_filePath);

            // path not found
            if (time == _LocalTimeWithZeroFileTime)
            {
                throw new FileNotFoundException($"Could not find file '{_filePath}'.");
            }

            Existed = true;
            return time;
        }
        catch
        {
            Existed = false;
            throw;
        }
    }

    private Encoding GetSourceEncoding(Encoding? srcEncoding)
    {
        if (srcEncoding == null)
        {
            if (_encoding == null)
            {
                throw new ArgumentException($"Current encoding of file is unknown.\n{_filePath}");
            }

            return _encoding;
        }
        
        if (_encoding != null && TextEncoding.IsUnicodeCodePage(_encoding.CodePage) && _encoding.CodePage != srcEncoding.CodePage)
        {
            // if _encoding is not null, srcEncoding should use the same code page as the _encoding
            throw new ArgumentException($"Current encoding of file is not '{srcEncoding.EncodingName}'.\n{_filePath}");
        }

        return srcEncoding;
    }
}
