using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using System.Buffers;
using System.Text;
using TextTool.Core.Helpers;
using TextTool.Core.Models;
using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Core.ComponentModel;

public class ExternalToolWorker
{
    private ExternalToolInfo _toolInfo;
    private Task _asyncTask = Task.CompletedTask;
    
    public ExternalToolWorker(ExternalToolInfo info)
    {
        _toolInfo = info;
    }

    public ExternalToolInfo ToolInfo
    {
        get => _toolInfo;
        set => _toolInfo = value;
    }

    public void Run(IList<FileItem> fileItems, IProgress<ProgressInfo>? progress = null)
    {
        if (!CheckAsyncOperation() || fileItems.Count == 0 || !CheckItems(fileItems))
        {
            return;
        }

        RunCommand(_toolInfo, fileItems, progress);
    }

    public Task RunAsync(IList<FileItem> fileItems, IProgress<ProgressInfo>? progress = null, CancellationToken token = default)
    {
        if (!CheckAsyncOperation() || fileItems.Count == 0 || !CheckItems(fileItems))
        {
            return Task.CompletedTask;
        }

        _asyncTask = RunCommandAsync(_toolInfo, fileItems, progress, token);

        return _asyncTask;
    }

    private static void RunCommand(ExternalToolInfo info, IList<FileItem> fileItems, IProgress<ProgressInfo>? progress = null)
    {
        StringBuilder error = new();
        NativeCommandInfo commandInfo = info.CommandInfo;
        string path = commandInfo.Path;
        bool wait = info.WaitForExit;
        bool refresh = info.ShouldRefresh;
        char[]? buffer = null;
        FileItem? item = null;

        try
        {
            using ICommandLine process = Ioc.Default.GetRequiredService<ICommandLine>();
            process.Start();

            buffer = ArrayPool<char>.Shared.Rent(1024);

            for (int i = 0; i < fileItems.Count; i++)
            {
                item = fileItems[i];
                string filePath = item.FilePath;

                if (!item.TakeAccess() || !FileService.PathExists(filePath))
                {
                    item = null;
                    error.AppendLine($"Cannot access file [{filePath}]");
                    continue;
                }

                int length = CombineArguments(commandInfo, filePath, ref buffer);

                process.Execute(path, new(buffer, 0, length), wait);

                if (refresh)
                {
                    try
                    {
                        item.Refresh();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        error.AppendLine(ex.Message);
                    }
                }

                item.ReleaseAccess();
                progress?.Report(new(i + 1, fileItems.Count));
            }

            process.Exit();
        }
        catch (Exception ex)
        {
            item?.ReleaseAccess();
            SendMessage(ex);
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            CheckMessage(error.ToString());
        }
    }

    private static async Task RunCommandAsync(ExternalToolInfo info, IList<FileItem> fileItems, IProgress<ProgressInfo>? progress = null, CancellationToken token = default)
    {
        StringBuilder error = new();
        NativeCommandInfo commandInfo = info.CommandInfo;
        string path = commandInfo.Path;
        bool wait = info.WaitForExit;
        bool refresh = info.ShouldRefresh;
        char[]? buffer = null;
        FileItem? item = null;

        try
        {
            using ICommandLine process = Ioc.Default.GetRequiredService<ICommandLine>();
            process.Start();

            buffer = ArrayPool<char>.Shared.Rent(10);

            for (int i = 0; i < fileItems.Count; i++)
            {
                item = fileItems[i];
                string filePath = item.FilePath;
                
                if (!item.TakeAccess() || !FileService.PathExists(filePath))
                {
                    item = null;
                    error.AppendLine($"Cannot access file [{filePath}]");
                    continue;
                }

                int length = CombineArguments(commandInfo, filePath, ref buffer);

                await process.ExecuteAsync(path, new(buffer, 0, length), wait, token);

                if (refresh)
                {
                    try
                    {
                        await item.RefreshAsync(token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        error.AppendLine(ex.Message);
                    }
                }

                item.ReleaseAccess();
                progress?.Report(new(i + 1, fileItems.Count));
            }

            await process.ExitAsync();
        }
        catch (Exception ex)
        {
            item?.ReleaseAccess();
            SendMessage(ex);
        }
        finally
        {
            if (buffer != null)
            {
                ArrayPool<char>.Shared.Return(buffer);
            }

            CheckMessage(error.ToString());
        }
    }

    private static int CombineArguments(NativeCommandInfo info, string filePath, ref char[] buffer)
    {
        ReadOnlySpan<char> args = info.PreArguments;
        ReadOnlySpan<char> file = info.ArgumentFileNameTransform == null ? filePath : info.ArgumentFileNameTransform(filePath);
        int length = args.Length + file.Length + 3; // info.Arguments[]["]filePath["]

        if (length > buffer.Length)
        {
            ArrayPool<char>.Shared.Return(buffer);
            buffer = ArrayPool<char>.Shared.Rent(length);
        }

        int count = 0;
        Span<char> span = buffer;

        if (!args.IsEmpty)
        {
            args.CopyTo(span);
            count += args.Length;
            buffer[count++] = ' ';
        }
        buffer[count++] = NativeCommandService.StringQuote;
        file.CopyTo(span.Slice(count));
        count += file.Length;
        buffer[count++] = NativeCommandService.StringQuote;

        return count;
    }

    private bool CheckItems(IList<FileItem> items)
    {
        if (!_toolInfo.UseBatch && items.Count > 1)
        {
            CheckMessage("Cannot run on multiple items when using non-batch mode.");
            return false;
        }
        return true;
    }

    private bool CheckAsyncOperation()
    {
        if (!_asyncTask.IsCompleted)
        {
            CheckMessage("The worker has a running async operation.");
            return false;
        }
        return true;
    }

    private static void SendMessage(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            StringBuilder builder = new();
            builder.AppendLine(aggregate.Message);
            aggregate.Handle((ex) =>
            {
                builder.AppendLine(ex.Message);
                return true;
            });

            CheckMessage(builder.ToString());
        }
        else
        {
            CheckMessage(exception.Message);
        }
    }

    private static void CheckMessage(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage(message));
        }
    }
}
