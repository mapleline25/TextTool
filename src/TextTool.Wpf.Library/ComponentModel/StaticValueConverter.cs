using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace TextTool.Wpf.Library.ComponentModel;

public abstract class StaticValueConverter : MarkupExtension, IValueConverter
{
    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }
}
