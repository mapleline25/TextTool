namespace TextTool.Library.Utils;

public interface ICommandLine : IDisposable
{
    public void Start();

    public void Execute(string fileName, ReadOnlySpan<char> arguments, bool waitForExit);

    public Task ExecuteAsync(string fileName, ReadOnlySpan<char> arguments, bool waitForExit, CancellationToken token);

    public void Exit();

    public Task ExitAsync();
}
