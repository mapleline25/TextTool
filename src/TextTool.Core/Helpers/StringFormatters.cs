using TextTool.Library.Models;

namespace TextTool.Core.Helpers;

public class AddProgressFormatter : CustomStringFormatter
{
    public AddProgressFormatter() { }
    public override string ToString(params object[] args)
    {
        if (args == null || args.Length != 2)
        {
            throw new FormatException(UnsupportedArgumentsLength);
        }
        if (args[0] is double complete && args[1] is double total)
        {
            return $"Loading items...({complete}/{total})";
        }
        else
        {
            throw new FormatException(UnsupportedArgumentsType);
        }
    }
}

public class RemoveProgressFormatter : CustomStringFormatter
{
    public RemoveProgressFormatter() { }
    public override string ToString(params object[] args)
    {
        if (args == null || args.Length != 2)
        {
            throw new FormatException(UnsupportedArgumentsLength);
        }
        if (args[0] is double && args[1] is double total)
        {
            return $"Deleting {total} items...";
        }
        else
        {
            throw new FormatException(UnsupportedArgumentsType);
        }
    }
}

public class RefreshProgressFormatter : CustomStringFormatter
{
    public RefreshProgressFormatter() { }
    public override string ToString(params object[] args)
    {
        if (args == null || args.Length != 2)
        {
            throw new FormatException(UnsupportedArgumentsLength);
        }
        if (args[0] is double complete && args[1] is double total)
        {
            return $"Refreshing all items...({complete / total})";
        }
        else
        {
            throw new FormatException(UnsupportedArgumentsType);
        }
    }
}

public class ConvertProgressFormatter : CustomStringFormatter
{
    public ConvertProgressFormatter() { }
    public override string ToString(params object[] args)
    {
        if (args == null || args.Length != 2)
        {
            throw new FormatException(UnsupportedArgumentsLength);
        }
        if (args[0] is double complete && args[1] is double total)
        {
            return $"Processing selected items...({complete}/{total})";
        }
        else
        {
            throw new FormatException(UnsupportedArgumentsType);
        }
    }
}

public class WorkerProgressFormatter : CustomStringFormatter
{
    public WorkerProgressFormatter() { }
    public override string ToString(params object[] args)
    {
        if (args == null || args.Length != 2)
        {
            throw new FormatException(UnsupportedArgumentsLength);
        }
        if (args[0] is double complete && args[1] is double total)
        {
            return $"Processing selected items...({complete}/{total})";
        }
        else
        {
            throw new FormatException(UnsupportedArgumentsType);
        }
    }
}
