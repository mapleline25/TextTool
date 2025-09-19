namespace TextTool.Interop;

[Flags]
public enum EverythingRequestFlags
{
    FILE_NAME = 1,
    PATH = 1 << 1,
    FULL_PATH_AND_FILE_NAME = 1 << 2,
    EXTENSION = 1 << 3,
    SIZE = 1 << 4,
    DATE_CREATED = 1 << 5,
    DATE_MODIFIED = 1 << 6,
    DATE_ACCESSED = 1 << 7,
    ATTRIBUTES = 1 << 8,
    FILE_LIST_FILE_NAME = 1 << 9,
    RUN_COUNT = 1 << 10,
    DATE_RUN = 1 << 11,
    DATE_RECENTLY_CHANGED = 1 << 12,
    HIGHLIGHTED_FILE_NAME = 1 << 13,
    HIGHLIGHTED_PATH = 1 << 14,
    HIGHLIGHTED_FULL_PATH_AND_FILE_NAME = 1 << 15,
}
