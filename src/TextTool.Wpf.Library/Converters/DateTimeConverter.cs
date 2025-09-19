using System.Globalization;
using TextTool.Wpf.Library.ComponentModel;

namespace TextTool.Wpf.Library.Converters;

public class DateTimeConverter : StaticValueConverter
{
    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is DateTime time ? time.ToString("g", CultureInfo.CurrentCulture) : string.Empty;
    }
}
