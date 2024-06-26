using System.Collections.Generic;
using WPFUI.Enums;

namespace WPFUI.Comparer;

public sealed class ComparisonResult<T>(T? original, T? compared, ComparisonResultType type, IDictionary<string, ComparisonVariance> variances)
{
    public T? Original { get; set; } = original;
    public T? Compared { get; set; } = compared;
    public ComparisonResultType Type { get; set; } = type;
    public IDictionary<string, ComparisonVariance> Variances { get; set; } = variances;
}
