using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Win32;
using System.IO.Enumeration;
using TextTool.Library.Models;

namespace TextTool.Library.Utils;

public static class FileService
{
    public static readonly bool IsSystemCaseSensitive =
        !(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS());

    public static readonly int MaxPathLength = GetMaxPathLength();

    public static MemoryOwner<byte> ReadAllBytes(string path)
    {
        using Stream stream = File.OpenRead(path);
        MemoryOwner<byte> buffer = MemoryOwner<byte>.Allocate((int)stream.Length);
        _ = stream.Read(buffer.Span);
        return buffer;
    }
    
    public static bool PathExists(string? path)
    {
        return GetFullPath(path) != null;
    }

    public static string? GetFullPath(string? path)
    {
        if (path == null)
        {
            return null;
        }
        
        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        for (int i = 0; i < _EnvironmentPaths.Length; i++)
        {
            string fullPath = Path.Combine(_EnvironmentPaths[i], path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    public static IEnumerable<string> EnumerateDirectories(string path, EnumerationOptions options, CancellationToken token = default)
    {
        ValidateDirectory(path);

        return new FileSystemEnumerable<string>(
            path,
            static (ref FileSystemEntry entry) => entry.ToFullPath(),
            options)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                token.ThrowIfCancellationRequested();
                return entry.IsDirectory;
            }
        };
    }

    public static int CountEntries(string path, EnumerationOptions? options, CancellationToken token = default)
    {
        ValidateDirectory(path);

        FileSystemEnumerable<bool> entries = new(path, static (ref FileSystemEntry entry) => true, options);
        IEnumerator<bool> enumerator = entries.GetEnumerator();

        int count = 0;
        while (enumerator.MoveNext())
        {
            token.ThrowIfCancellationRequested();
            count++;
        }

        return count;
    }

    public static IEnumerable<string> EnumerateFiles(string path, string searchExpression, EnumerationOptions? options = null, CancellationToken token = default)
    {
        return EnumerateFilesCore(path, searchExpression, options ?? new EnumerationOptions(), token);
    }

    public static void ValidateDirectory(ReadOnlySpan<char> directoryPath)
    {
        if (directoryPath.Contains('\0'))
            throw new ArgumentException("Null character in directory path.");

        if (directoryPath.IndexOfAny(_InvalidPathChars.AsSpan()) != -1)
            throw new ArgumentException("Invalid characters in directory path.");
    }

    public static void ValidateFileName(ReadOnlySpan<char> expression)
    {
        if (Path.IsPathRooted(expression))
            throw new ArgumentException("File name contains a root.");

        if (expression.Contains('\0'))
            throw new ArgumentException("Null character in file name.");

        if (!Path.GetDirectoryName(expression).IsEmpty)
            throw new ArgumentNullException(nameof(expression), "File name includes a directory.");
    }

    public static bool NormalizeIgnoreCase(EnumerationOptions options)
    {
        return options.MatchCasing == MatchCasing.PlatformDefault && !IsSystemCaseSensitive || options.MatchCasing == MatchCasing.CaseInsensitive;
    }

    private static FileSystemEnumerable<string> EnumerateFilesCore(string path, string expression, EnumerationOptions options, CancellationToken token)
    {
        ValidateDirectory(path);

        FileNameExpression exp = new(expression, NormalizeIgnoreCase(options));
        
        return new FileSystemEnumerable<string>(
            path,
            (ref FileSystemEntry entry) => entry.ToFullPath(),
            options)
        {
            ShouldIncludePredicate = (ref FileSystemEntry entry) =>
            {
                token.ThrowIfCancellationRequested();
                return !entry.IsDirectory && exp.IsMatch(entry.FileName);
            }
        };
    }

    private static string[] GetEnvironmentPaths()
    {
        try
        {
            return Environment.GetEnvironmentVariable("PATH") is string path ? path.Split(Path.PathSeparator) : [];
        }
        catch
        {
            return [];
        }
    }

    private static int GetMaxPathLength()
    {
        if (OperatingSystem.IsWindows())
        {
            int defaultLength = 260; // MAX_PATH
            int longLength = 32767; // long path with prefix "\?"

            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\FileSystem");
                return key?.GetValue("LongPathsEnabled") is int value && value == 1 ? longLength : defaultLength;
            }
            catch
            {
                return defaultLength;
            }
        }

        if (OperatingSystem.IsMacOS())
        {
            // PATH_MAX(1024) - len(/private)
            return 1016;
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            // PATH_MAX
            return 4096;
        }

        return 255;
    }

    private static readonly string[] _EnvironmentPaths = GetEnvironmentPaths();
    private static readonly char[] _InvalidPathChars = Path.GetInvalidPathChars();
}
