using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using WPFUI.Comparer;

namespace WPFUI.Extensions;

public static class TransferToExtension
{
    public static void TransferTo<T>(this T source, T target, IDictionary<string, ComparisonVariance> values)
    {
        if (values is null || !values.Any())
            return;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        if (properties.Length == 0)
            return;

        var propertiesDict = properties.ToImmutableDictionary(k => k.Name, v => v);

        foreach (var value in values)
        {
            if (!propertiesDict.TryGetValue(value.Key, out var property))
                continue;

            property.SetValue(target, property.GetValue(source));
        }
    }
}