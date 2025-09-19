using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TextTool.Library.Extensions;
using TextTool.Wpf.Library.ComponentModel;

namespace TextTool.Wpf.Library.Converters;

public class ToolTipConverter : StaticMultiValueConverter
{
    public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        double actualWidth = (double)values[0];
        TextBlock textBlock = (TextBlock)values[1];
        return actualWidth.EqualsEx(0) || MeasureTextWidth(textBlock).EqualsEx(actualWidth) ? null! : textBlock.Text;
    }

    private static double MeasureTextWidth(TextBlock textBlock)
    {
        return new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            1
        ).Width;
    }
}
