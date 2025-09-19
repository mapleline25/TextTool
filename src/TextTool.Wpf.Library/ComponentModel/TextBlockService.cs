using CommunityToolkit.HighPerformance.Buffers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using TextTool.Library.ComponentModel;
using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Wpf.Library.ComponentModel;

public static class TextBlockService
{
    private static readonly double _DefaultFontSize = SystemFonts.MessageFontSize;
    private static readonly double _Header1FontSize = _DefaultFontSize + 4;
    private static readonly double _Header2FontSize = _DefaultFontSize + 2;

    public static TextBlockViewModel GetViewModelFromResource(Uri uri)
    {
        if (ApplicationService.GetResourceBuffer(uri) is not MemoryOwner<char> buffer)
        {
            return new([]);
        }

        using (buffer)
        {
            return new(buffer.Span);
        }
    }

    public static void SetInlines(InlineCollection inlines, TextBlockViewModel viewModel)
    {
        List<Inline> list = [];

        foreach (DocumentData data in viewModel.DocumentDatas)
        {
            string text = data.Data;
            if (data.Kind == DocumentDataKind.Header1)
            {
                list.Add(new Run(text) { FontSize = _Header1FontSize, FontWeight = FontWeights.Bold });
            }
            else if (data.Kind == DocumentDataKind.Header2)
            {
                list.Add(new Run(text) { FontSize = _Header2FontSize, FontWeight = FontWeights.Bold });
            }
            else if (data.Kind == DocumentDataKind.Bold)
            {
                list.Add(new Run(text) { FontWeight = FontWeights.Bold });
            }
            else if (data.Kind == DocumentDataKind.Hyperlink)
            {
                Hyperlink hyperlink = new(new Run(text)) { NavigateUri = new Uri(text) };
                hyperlink.RequestNavigate += OnRequestNavigate;
                list.Add(hyperlink);
            }
            else // Text
            {
                list.Add(new Run(text));
            }
        }

        inlines.Clear();
        inlines.AddRange(list);
    }

    private static void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        ApplicationService.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}
