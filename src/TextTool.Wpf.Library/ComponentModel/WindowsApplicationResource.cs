using System.IO;
using System.Windows;
using TextTool.Library.ComponentModel;

namespace TextTool.Wpf.Library.ComponentModel;

public class WindowsApplicationResource : IApplicationResource
{
    public Stream OpenResource(Uri uri)
    {
        return Application.GetResourceStream(uri).Stream;
    }
}
