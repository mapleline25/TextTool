using CommunityToolkit.Mvvm.Messaging;
using System.Text;
using TextTool.Core.Helpers;
using TextTool.Core.Models;
using TextTool.Library.Models;
using TextTool.Library.Utils;

namespace TextTool.Core.ComponentModel;

public static class SettingsService
{
    private static readonly string _ConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config.ini");
    private static bool _init = false;

    public static void Initialize()
    {
        if (_init)
        {
            return;
        }

        _init = true;
        InitDefaultSettings();
        LoadExternalSettings();
    }

    public static IList<ExternalToolWorker> DefaultExternalToolWorkers { get; } = [];
    public static IList<ExternalToolWorker> CustomExternalToolWorkers { get; } = [];

    private static void InitDefaultSettings()
    {
        if (NativeCommandService.OpenDirectory is NativeCommandInfo info)
        {
            DefaultExternalToolWorkers.Add(new ExternalToolWorker(
                new ExternalToolInfo("Open directory", info) { UseBatch = false }));
        }
    }

    private static void LoadExternalSettings()
    {
        if (!File.Exists(_ConfigPath))
        {
            return;
        }
        
        try
        {
            IniFile ini = new(_ConfigPath);
            foreach (KeyValuePair<string, IList<IniSection>> group in ini.SectionGroupTable)
            {
                switch (group.Key)
                {
                    case "command":
                        TryAddCustomToolWorker(group.Value);
                        break;
                    default:
                        break;
                }
            }
        }
        catch (Exception e)
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage($"Cannot load config.ini.\n{e.Message}"));
        }
    }

    private static void TryAddCustomToolWorker(IEnumerable<IniSection> sections)
    {
        StringBuilder missingPaths = new();
        bool hasError = false;

        int n = 0;
        foreach (IniSection section in sections)
        {
            Dictionary<string, string> propertyTable = section.PropertyTable;

            if (!propertyTable.TryGetValue("path", out string? path))
            {
                hasError = true;
                continue;
            }

            path = NativeCommandService.TrimPath(path);

            if (FileService.GetFullPath(path) is not string fullPath)
            {
                hasError = true;
                missingPaths.AppendLine($"'{path}'");
                continue;
            }

            propertyTable.TryGetValue("title", out string? title);
            propertyTable.TryGetValue("preArguments", out string? preArguments);
            propertyTable.TryGetValue("wait", out string? wait);
            propertyTable.TryGetValue("refresh", out string? refresh);
            propertyTable.TryGetValue("batch", out string? batch);

            title = string.IsNullOrEmpty(title) ? $"New command ({++n})" : title;

            NativeCommandInfo info = new()
            {
                Path = fullPath,
                PreArguments = preArguments ?? string.Empty,
            };

            CustomExternalToolWorkers.Add(new ExternalToolWorker(
                new ExternalToolInfo(title, info)
                {
                    WaitForExit = wait == "1",
                    ShouldRefresh = refresh == "1",
                    UseBatch = batch == "1",
                }));
        }

        if (hasError)
        {
            WeakReferenceMessenger.Default.Send(new SystemMessage(
                $"Cannot find one or more command paths{(missingPaths.Length > 0 ? $":\n{missingPaths}" : ".")}"));
        }
    }
}
