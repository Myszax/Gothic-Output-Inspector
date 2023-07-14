using System.Collections.Generic;
using WPFUI.Enums;

namespace WPFUI.Comparer;

public sealed class ComparisonResult<T>
{
    public T? Original { get; set; }
    public T? Compared { get; set; }
    public ComparisonResultType Type { get; set; }
    public IDictionary<string, ComparisonVariance> Variances { get; set; }

    public ComparisonResult(T? original, T? compared, ComparisonResultType type, IDictionary<string, ComparisonVariance> variances)
    {
        Original = original;
        Compared = compared;
        Type = type;
        Variances = variances;
    }
}