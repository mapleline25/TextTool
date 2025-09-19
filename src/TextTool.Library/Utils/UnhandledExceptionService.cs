namespace TextTool.Library.Utils;

public class UnhandledExceptionService
{
    private static UnhandledExceptionService? _Instance;
    private readonly string? _logPath = null;

    private UnhandledExceptionService()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        _logPath = Path.Join(Directory.GetCurrentDirectory(), $"error_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log");
    }

    public static void Initialize()
    {
        if (_Instance == null)
        {
            _Instance = new();
        }
    }

    public static UnhandledExceptionService Instance
    {
        get
        {
            if (_Instance == null)
            {
                throw new InvalidOperationException("Service is not initialized");
            }

            return _Instance;
        }
    }

    public string? LogPath => _logPath;

    public event EventHandler<ExceptionEventArgs>? UnhandledException;

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Exception lastException = (Exception)args.ExceptionObject;
        string detail = CreateDetailMessage(lastException);

        try
        {
            if (_logPath != null)
            {
                File.AppendAllText(_logPath, detail);
            }
            UnhandledException?.Invoke(null, new(lastException));
        }
        catch { }
    }

    private static string CreateDetailMessage(Exception e)
    {
        return $"""
{DateTime.Now}
---------------------------------------------------
{e}
---------------------------------------------------
""";
    }

    public class ExceptionEventArgs(Exception exception) : EventArgs
    {
        public Exception Exception { get; } = exception;
    }
}
