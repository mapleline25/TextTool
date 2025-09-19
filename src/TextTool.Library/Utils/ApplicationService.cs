using CommunityToolkit.HighPerformance.Buffers;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using TextTool.Library.ComponentModel;

namespace TextTool.Library.Utils;

public static class ApplicationService
{
    private static readonly Assembly? _EntryAssembly;
    private static readonly AssemblyProductAttribute? _ProductAttribute;
    private static readonly AssemblyInformationalVersionAttribute? _VersionAttribute;
    private static readonly IApplicationResource _AppResource;

    static ApplicationService()
    {
        _AppResource = Ioc.Default.GetRequiredService<IApplicationResource>();

        if (Assembly.GetEntryAssembly() is Assembly assembly)
        {
            _EntryAssembly = assembly;
            _ProductAttribute = _EntryAssembly?.GetCustomAttributes<AssemblyProductAttribute>().FirstOrDefault();
            _VersionAttribute = _EntryAssembly?.GetCustomAttributes<AssemblyInformationalVersionAttribute>().FirstOrDefault();
        }
        else
        {
            _EntryAssembly = null;
            _ProductAttribute = null;
            _VersionAttribute = null;
        }
    }

    public static string? ProductName => _ProductAttribute?.Product;

    public static string? Version => _VersionAttribute?.InformationalVersion;

    public static IApplicationResource Resource => _AppResource;

    public static void OpenUrl(string url)
    {
        using Process process = new()
        {
            StartInfo = new(url) { UseShellExecute = true }
        };
        process.Start();
    }

    public static Stream? GetResourceStream(Uri uri)
    {
        try
        {
            return _AppResource.OpenResource(uri);
        }
        catch
        {
            return null;
        }
    }

    public static MemoryOwner<char>? GetResourceBuffer(Uri uri)
    {
        if (GetResourceStream(uri) is not Stream stream)
        {
            return null;
        }

        using (stream)
        {
            Encoding encoding = Encoding.UTF8;
            using StreamReader reader = new(stream, encoding);

            int length = encoding.GetMaxCharCount((int)stream.Length);
            MemoryOwner<char> chars = MemoryOwner<char>.Allocate(length);

            int count = reader.Read(chars.Span);

            return chars.Slice(0, count);
        }
    }

    public static IEnumerable<KeyValuePair<string, Stream>> EnumerateResources(string path)
    {
        if (GetResourceSet() is ResourceSet resourceSet)
        {
            using (resourceSet)
            {
                IDictionaryEnumerator id = resourceSet.GetEnumerator();
                while (id.MoveNext())
                {
                    if (id.Key is string name && name.StartsWith(path, StringComparison.InvariantCultureIgnoreCase) && id.Value is Stream stream)
                    {
                        yield return new(name, stream);
                    }
                }
            }
        }
    }

    public static ResourceSet? GetResourceSet(CultureInfo? culture = null)
    {
        if (_EntryAssembly is Assembly assembly)
        {
            return new ResourceManager($"{assembly.GetName().Name}.g", assembly).GetResourceSet(culture ?? CultureInfo.CurrentUICulture, true, true);
        }
        return null;
    }
}
