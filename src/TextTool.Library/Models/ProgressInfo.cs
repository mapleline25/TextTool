namespace TextTool.Library.Models;

public class ProgressInfo(double complete, double total)
{
    public double Complete { get; } = complete;
    public double Total { get; } = total;
}
