using System.Runtime.CompilerServices;

namespace TextTool.Library.Extensions;

public static class NumericExtensions
{
    private const double _7 = 0.0000001;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsEx(this double left, double right)
    {
        return Math.Abs(left - right) < _7;
    }
}
