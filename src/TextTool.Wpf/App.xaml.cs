using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TextTool.Library.ComponentModel;
using TextTool.Library.Utils;
using TextTool.Wpf.Library.ComponentModel;

namespace TextTool.Wpf;

public partial class App : Application
{
    public App()
    {
        UnhandledExceptionService.Initialize();
        WindowsMessageService.Initialize();

        Ioc.Default.ConfigureServices(
            new ServiceCollection()
            .AddSingleton<IApplicationResource, WindowsApplicationResource>()
            .AddTransient<IFileSearchNative, WindowsFileSearcher>()
            .AddTransient<ICommandLine, WindowsCommandLine>()
            .BuildServiceProvider()
            );
    }
}
