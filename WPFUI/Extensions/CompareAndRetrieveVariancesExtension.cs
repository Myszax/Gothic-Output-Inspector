using System.Collections.Generic;
using System.Reflection;
using WPFUI.Comparer;

namespace WPFUI.Extensions;

public static class CompareAndRetrieveVariancesExtension
{
    public static Dictionary<string, ComparisonVariance> CompareAndRetrieveVariances<T>(this T valA, T valB)
    {
        var variancesDict = new Dictionary<string, ComparisonVariance>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var variance = new ComparisonVariance
            {
                PropertyName = property.Name,
                ValA = property.GetValue(valA),
                ValB = property.GetValue(valB)
            };

            var valAIsNull = variance.ValA is null;
            var valBIsNull = variance.ValB is null;

            if (valAIsNull && valBIsNull)
                continue;

            if (valAIsNull && !valBIsNull || !valAIsNull && valBIsNull)
            {
                variancesDict.Add(variance.PropertyName, variance);
                continue;
            }

            if (!valAIsNull && !variance.ValA.Equals(variance.ValB))
                variancesDict.Add(variance.PropertyName, variance);
        }

        return variancesDict;
    }
}