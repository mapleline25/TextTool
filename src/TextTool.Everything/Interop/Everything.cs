using System.Runtime.InteropServices;

namespace TextTool.Interop;

internal static partial class Everything
{
    public const string LibraryPath = "Everything64.dll";

    [LibraryImport(LibraryPath, EntryPoint = "Everything_CleanUp")]
    public static partial void CleanUp();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_DeleteRunHistory")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteRunHistory();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_Exit")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Exit();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetBuildNumber")]
    public static partial uint GetBuildNumber();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetLastError")]
    public static partial uint GetLastError();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetMajorVersion")]
    public static partial uint GetMajorVersion();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetMatchCase")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMatchCase();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetMatchPath")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMatchPath();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetMatchWholeWord")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMatchWholeWord();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetMax")]
    public static partial uint GetMax();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetMinorVersion")]
    public static partial uint GetMinorVersion();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetNumFileResults")]
    public static partial uint GetNumFileResults();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetNumFolderResults")]
    public static partial uint GetNumFolderResults();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetNumResults")]
    public static partial uint GetNumResults();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetOffset")]
    public static partial uint GetOffset();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetRegex")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetRegex();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetReplyID")]
    public static partial uint GetReplyID();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetReplyWindow")]
    public static partial IntPtr GetReplyWindow();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetRequestFlags")]
    public static partial uint GetRequestFlags();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultAttributes")]
    public static partial uint GetResultAttributes(uint dwIndex);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultDateAccessed")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetResultDateAccessed(uint dwIndex, IntPtr lpDateAccessed);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultDateCreated")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetResultDateCreated(uint dwIndex, IntPtr lpDateCreated);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultDateModified")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetResultDateModified(uint dwIndex, IntPtr lpDateModified);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultDateRecentlyChanged")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetResultDateRecentlyChanged(uint dwIndex, IntPtr lpDateRecentlyChanged);
    
    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultDateRun")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetResultDateRun(uint dwIndex, IntPtr lpDateRun);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultExtensionW")]
    public static partial IntPtr GetResultExtensionW(uint dwIndex);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultFileListFileNameW")]
    public static partial IntPtr GetResultFileListFileNameW(uint dwIndex);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultFileNameW")]
    public static partial IntPtr GetResultFileNameW(uint index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultFullPathNameW", StringMarshalling = StringMarshalling.Utf16)]
    public static unsafe partial uint GetResultFullPathNameW(uint index, char* lpString, uint nMaxCount);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultHighlightedFileNameW")]
    public static partial IntPtr GetResultHighlightedFileNameW(int index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultHighlightedFullPathAndFileNameW")]
    public static partial IntPtr GetResultHighlightedFullPathAndFileNameW(int index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultHighlightedPathW")]
    public static partial IntPtr GetResultHighlightedPathW(int index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultListRequestFlags")]
    public static partial uint GetResultListRequestFlags();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultListSort")]
    public static partial uint GetResultListSort();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultPathW")]
    public static partial IntPtr GetResultPathW(uint index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultRunCount")]
    public static partial uint GetResultRunCount(uint dwIndex);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetResultSize")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetResultSize(uint dwIndex, IntPtr lpSize);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetRevision")]
    public static partial uint GetRevision();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetRunCountFromFileNameW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint GetRunCountFromFileNameW(string lpFileName);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetSearchW")]
    public static partial IntPtr GetSearchW();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetSort")]
    public static partial uint GetSort();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetTargetMachine")]
    public static partial uint GetTargetMachine();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetTotFileResults")]
    public static partial uint GetTotFileResults();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetTotFolderResults")]
    public static partial uint GetTotFolderResults();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_GetTotResults")]
    public static partial uint GetTotResults();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IncRunCountFromFileNameW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint IncRunCountFromFileNameW(string lpFileName);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsAdmin")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsAdmin();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsAppData")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsAppData();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsDBLoaded")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsDBLoaded();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsFastSort")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsFastSort();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsFileInfoIndexed")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsFileInfoIndexed(uint fileInfoType);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsFileResult")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsFileResult(uint index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsFolderResult")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsFolderResult(uint index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsQueryReply", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsQueryReply(uint message, IntPtr wParam, string lParam, uint nId);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_IsVolumeResult")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsVolumeResult(uint index);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_QueryW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool QueryW([MarshalAs(UnmanagedType.Bool)] bool bWait);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_RebuildDB")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RebuildDB();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_Reset")]
    public static partial void Reset();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SaveDB")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SaveDB();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SaveRunHistory")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SaveRunHistory();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetMatchCase")]
    public static partial void SetMatchCase([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetMatchPath")]
    public static partial void SetMatchPath([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetMatchWholeWord")]
    public static partial void SetMatchWholeWord([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetMax")]
    public static partial void SetMax(uint dwMaxResults);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetOffset")]
    public static partial void SetOffset(uint dwOffset);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetRegex")]
    public static partial void SetRegex([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetReplyID")]
    public static partial void SetReplyID(uint nId);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetReplyWindow")]
    public static partial void SetReplyWindow(IntPtr hWnd);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetRequestFlags")]
    public static partial void SetRequestFlags(uint dwRequestFlags);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetRunCountFromFileNameW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetRunCountFromFileNameW(string lpFileName, uint dwRunCount);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetSearchW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void SetSearchW(string lpString);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SetSort")]
    public static partial void SetSort(uint dwSortType);

    [LibraryImport(LibraryPath, EntryPoint = "Everything_SortResultsByPath")]
    public static partial void SortResultsByPath();

    [LibraryImport(LibraryPath, EntryPoint = "Everything_UpdateAllFolderIndexes")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateAllFolderIndexes();

}
