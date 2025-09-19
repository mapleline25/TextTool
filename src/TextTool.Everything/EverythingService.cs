using System.Runtime.InteropServices;
using TextTool.Interop;

namespace TextTool;

public static class EverythingService
{
    private static readonly bool _LibraryFileExists = File.Exists(Everything.LibraryPath);

    public static bool IsLoaded => _LibraryFileExists && Everything.IsDBLoaded();

    public static void CleanUp() => Everything.CleanUp();

    public static void Reset() => Everything.Reset();

    public static void SetMatchPath(bool enable) => Everything.SetMatchPath(enable);

    public static bool GetMatchPath() => Everything.GetMatchPath();

    public static void SetMatchCase(bool enable) => Everything.SetMatchCase(enable);

    public static bool GetMatchCase() => Everything.GetMatchCase();

    public static void SetSearchText(string searchString) => Everything.SetSearchW(searchString);

    public static string GetSearchText()
    {
        return Marshal.PtrToStringUni(Everything.GetSearchW()) is string str ? str : throw CreateLastErrorException();
    }

    public static unsafe ReadOnlySpan<char> GetSearchTextSpan()
    {
        nint ptr = Everything.GetSearchW();
        if (ptr == nint.Zero)
        {
            throw CreateLastErrorException();
        }

        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)ptr);
    }

    public static void Query()
    {
        if (!Everything.QueryW(true))
        {
            throw CreateLastErrorException();
        }
    }

    public static void SetRequestFlags(EverythingRequestFlags flags) => Everything.SetRequestFlags((uint)flags);

    public static int GetRequestFlags() => (int)Everything.GetRequestFlags();

    public static void UpdateDB()
    {
        if (!Everything.UpdateAllFolderIndexes())
        {
            throw CreateLastErrorException();
        }
    }

    public static string GetEntryNameAt(int index)
    {
        return Marshal.PtrToStringUni(Everything.GetResultFileNameW((uint)index)) is string str ? str : throw CreateLastErrorException();
    }

    public static unsafe ReadOnlySpan<char> GetEntryNameSpanAt(int index)
    {
        nint ptr = Everything.GetResultFileNameW((uint)index);
        if (ptr == nint.Zero)
        {
            throw CreateLastErrorException();
        }

        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)ptr);
    }

    public static string GetEntryDirectoryAt(int index)
    {
        return Marshal.PtrToStringUni(Everything.GetResultPathW((uint)index)) is string str ? str : throw CreateLastErrorException();
    }

    public static unsafe ReadOnlySpan<char> GetEntryDirectorySpanAt(int index)
    {
        nint ptr = Everything.GetResultPathW((uint)index);
        if (ptr == nint.Zero)
        {
            throw CreateLastErrorException();
        }
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)ptr);
    }

    public static bool IsFileAt(int index)
    {
        bool result = Everything.IsFileResult((uint)index);
        ThrowIfAnyError();
        return result;
    }

    public static int GetCountOfFiles()
    {
        uint count = Everything.GetNumFileResults();
        ThrowIfAnyError();
        return (int)count;
    }

    public static int GetCountOfDirectories()
    {
        uint count = Everything.GetNumFolderResults();
        ThrowIfAnyError();
        return (int)count;
    }

    public static int GetCountOfEntries()
    {
        uint count = Everything.GetNumResults();
        ThrowIfAnyError();
        return (int)count;
    }

    public static int GetCountOfAllFiles()
    {
        uint count = Everything.GetTotFileResults();
        ThrowIfAnyError();
        return (int)count;
    }

    public static int GetCountOfAllDirectories()
    {
        uint count = Everything.GetTotFolderResults();
        ThrowIfAnyError();
        return (int)count;
    }

    public static int GetCountOfAllEntries()
    {
        uint count = Everything.GetTotResults();
        ThrowIfAnyError();
        return (int)count;
    }

    public static void SetSort(EverythingSortType type) => Everything.SetSort((uint)type);

    public static EverythingSortType GetSort() => (EverythingSortType)Everything.GetSort();

    public static int GetFullPathAt(int index, char[] chars, int charCount) => GetFullPathAt(index, chars.AsSpan(0, charCount));

    public static unsafe int GetFullPathAt(int index, Span<char> chars)
    {
        if (chars.IsEmpty)
        {
            return 0;
        }
        
        fixed (char* charsPtr = chars)
        {
            int n = (int)Everything.GetResultFullPathNameW((uint)index, charsPtr, (uint)chars.Length);
            if (n == 0)
            {
                throw CreateLastErrorException();
            }
            return n;
        }
    }

    public static EverythingStatus GetLastError() => (EverythingStatus)Everything.GetLastError();

    private static void ThrowIfAnyError()
    {
        EverythingStatus status = (EverythingStatus)Everything.GetLastError();
        if (status != EverythingStatus.OK)
        {
            throw new InvalidOperationException(status.ToString());
        }
    }

    private static InvalidOperationException CreateLastErrorException() => new(GetLastError().ToString());
}
