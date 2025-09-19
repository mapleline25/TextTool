using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Buffers;
using System.Text;
using TextTool.Library.Utils;

namespace TextTool.Library.ComponentModel;

public class AboutViewModel : ObservableObject
{
    private const string _ResourcePath = "Resources/About/";
    private const string _AboutText = $"{_ResourcePath}About.txt";
    private const string _NoticesPath = $"{_ResourcePath}ThirdPartyNotices/";
    private const string _Separator = "[h1]------------------------------------------------";
    private static readonly string? _Product = ApplicationService.ProductName;
    private static readonly string? _Version = ApplicationService.Version;
    private readonly string _productText;
    private TextBlockViewModel _licenseTextViewModel;

    public AboutViewModel()
    {
        _productText = $"{_Product} {_Version}";
        _licenseTextViewModel = new([]);
        AwaitPrepareResource();
    }

    public string ProductText => _productText;

    public TextBlockViewModel LicenseTextViewModel
    {
        get => _licenseTextViewModel;
        set => SetProperty(ref _licenseTextViewModel, value);
    }

    private async void AwaitPrepareResource()
    {
        await Task.Run(PrepareResourceAsync);
    }

    private async Task PrepareResourceAsync()
    {
        using ArrayPoolBufferWriter<char> writer = new();
        Encoding encoding = Encoding.UTF8;

        if (ApplicationService.GetResourceStream(new(_AboutText, UriKind.Relative)) is Stream stream)
        {
            using (stream)
            {
                await ReadAsync(stream, encoding, writer);
            }
            writer.Write($"\n\n");
        }

        bool first = true;
        
        foreach (KeyValuePair<string, Stream> resource in ApplicationService.EnumerateResources(_NoticesPath).OrderBy(x => x.Key))
        {
            if (first)
            {
                first = false;
            }
            else
            {
                writer.Write($"{_Separator}\n\n\n\n");
            }

            using (resource.Value)
            {
                await ReadAsync(resource.Value, encoding, writer);
            }
        }

        LicenseTextViewModel = new(writer.WrittenSpan);
    }

    private static async Task ReadAsync(Stream stream, Encoding encoding, ArrayPoolBufferWriter<char> buffer)
    {
        using StreamReader reader = new(stream, encoding, true);

        Memory<char> memory = buffer.GetMemory(encoding.GetMaxCharCount((int)stream.Length));
        
        int count = await reader.ReadAsync(memory);
        buffer.Advance(count);
    }
}
