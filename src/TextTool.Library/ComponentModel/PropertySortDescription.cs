using System.ComponentModel;

namespace TextTool.Library.ComponentModel
{
    public readonly struct PropertySortDescription(string propertyName, ListSortDirection direction)
    {
        public string PropertyName { get; } = propertyName;
        public ListSortDirection Direction { get; } = direction;
    }
}
