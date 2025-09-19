namespace TextTool.Library.Models;

public abstract class CustomStringFormatter
{
    protected const string UnsupportedArgumentsLength = "Unsupported length of arguments.";
    protected const string UnsupportedArgumentsType = "Unsupported type of arguments.";

    public abstract string ToString(params object[] args);
}
