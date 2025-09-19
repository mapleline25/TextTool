# TextTool
TextTool is a batch tool that can detect and convert the Unicode encoding type of multiple files.
It detects whether each file is encoded by UTF8/UTF16/UTF32 and whether it has a BOM (byte-order mark).

## How to use it?
You can directly drop files into the main window area, or import files through `Open...` or `Search...` of the `File` menu on the toolbar, then the software will detect the Unicode encoding type for all imported files.
It may take a longer time for detecting larger files. If it cannot be determined whether the file has a specific Unicode encoding, the `Encoding` field in the main window will be left empty.

![main window](/docs/main.png)

## System Requirements
* Windows 10 64-bit or higher
* .NET Desktop Runtime 9.0 x64 (https://dotnet.microsoft.com/zh-tw/download/dotnet/9.0)

## File search
You can use the `Search` window to search files in the specified folder.
Select the files in the search result area and click the `Import files` button to import them into the main window for detecting the encoding.

![search window](/docs/search.png)

### Search syntax
The `Search name` field supports the following syntax:

  * Operators:  
    | Syntax    | Definition      |
    |-----------|-----------------|
    | `space`   | AND             |
    | `\|`      | OR              |
    | `!`       | NOT             |
    | `< >`     | Grouping        |
    | `" "`     | String brackets |

  * Wildcards:  
    | Syntax    | Definition                      |
    |-----------|---------------------------------|
    | *         | Matches zero or more characters |
    | ?         | Matches one character           |

> [!NOTE]
> When using the string brackets, the operators `space`, `|`, `!`, `<`, and `>` between the left double quote `"` and right double quote `"` are treated as ordinary characters.

### Everything API integration
This software also integrates [Everything](https://www.voidtools.com/) API to search files, which can provide a faster search speed.
When Everything is running, the `Search` window will automatically uses Everything to retrieve search results.

## Encoding convertion
You can use the `Convert to...` menu in the right-click menu to convert the encoding of file to a specified Unicode encoding.
The software will ask you if you want to overwrite the original file or save it with the original name appended by a suffix in the original folder.

![convert menu](/docs/convert_menu.png)

For those files with unknown encodings, use `More...` to open `Convert encoding` window, then select a proper `Source encoding` and a `Destination encoding` to perform the convertion.
You can preview a partial of the convertion result in the `Result preview` area of convert encoding window.

![convert window](/docs/convert_window.png)

> [!NOTE]
> Since the font family currently used in this software does not contains all Unicode characters, some Unicode characters may not be displayed in the `Result preview` area.

## Customize the right-click menu
You can add custom file processing commands into the right-click menu by following the steps below.
* First create `config.ini` under the same folder of this software, the `config.ini` should be saved by using UTF-8 encoding.
* Add one or more `[Command]` section in `config.ini`, each command starts with `[Command]` followed by the following syntax:
  * `title`  
    The command name, it can be any text.
  * `path`  
    The executable file path. The path without left and right double quotes `"` is also accepted. It can be a relative path if the file can be found in the `PATH` environment variable.
  * `prearguments`  
    The execution arguments. When executing the command, the selected file name will be appended after the execution arguments.
  * `batch`  
    Use 1 to enable the command for multiple selected files, otherwise 0.
  * `wait`  
    Use 1 to indicate that the command can process the next selected file only after it completes the processing of current selected file, otherwise 0.
  * `refresh`  
    Use 1 to indicate that the selected file information should be refreshed in the main window after running the command, otherwise 0.

The following example can be used for opening multiple selected files by `notepad.exe`.
```
[Command]
title = Notepad
path = notepad.exe
batch = 1
```

> [!CAUTION]
> **Be careful to use `batch`! Some applications may become unstable or even crash when called frequently in a short period of time.**

> [!NOTE]
> `wait` is only meaningful when it is used with `batch` or `refresh`, use it if you want to wait for the command to complete the operation on each file and exit.

> [!NOTE]
> You can use `refresh` to refresh the information of the selected file (including re-detect the encoding) if the command would overwrite the files, in particular if it would change the file encoding.

## Special thanks
This project includes the components/libraries of the following projects:

* andrewlock/StronglyTypedId  
  https://github.com/andrewlock/StronglyTypedId
* AutoItConsulting/text-encoding-detect  
  https://github.com/AutoItConsulting/text-encoding-detect
* CommunityToolkit/dotnet  
  https://github.com/CommunityToolkit/dotnet
* DragonSpit/HPCsharp  
  https://github.com/DragonSpit/HPCsharp
* voidtools/Everything  
  https://www.voidtools.com/support/everything/sdk/
* Windows Presentation Foundation (WPF) (dotnet/wpf)  
  https://github.com/dotnet/wpf

This project also uses the NuGet packages from the following projects:

* CommunityToolkit.HighPerformance (CommunityToolkit/dotnet)  
  https://github.com/CommunityToolkit/dotnet/tree/main/src/CommunityToolkit.HighPerformance
* CommunityToolkit.Mvvm (CommunityToolkit/dotnet)  
  https://github.com/CommunityToolkit/dotnet/tree/main/src/CommunityToolkit.Mvvm
* DragonSpit/HPCsharp  
  https://github.com/DragonSpit/HPCsharp
* Microsoft.Bcl.HashCode (dotnet/maintenance-packages)  
  https://github.com/dotnet/maintenance-packages/tree/main/src/Microsoft.Bcl.HashCode/src
* Microsoft.CodeAnalysis.CSharp (dotnet/roslyn)  
  https://github.com/dotnet/roslyn
* Microsoft.CodeAnalysis (dotnet/roslyn)  
  https://github.com/dotnet/roslyn
* Microsoft.Extensions.DependencyInjection (dotnet/runtime)  
  https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Extensions.DependencyInjection
* Sergio0694/PolySharp  
  https://github.com/Sergio0694/PolySharp

