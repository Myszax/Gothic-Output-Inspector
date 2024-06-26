using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using WPFUI.Comparer;
using WPFUI.Enums;

namespace WPFUI.Extensions;

public static class IEnumerableExtension
{
    public static List<ComparisonResult<TSource>> CompareTo<TSource, TKey>(
        this IEnumerable<TSource> original, IEnumerable<TSource> toCompare, Func<TSource, TKey> keySelector) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(nameof(original));
        ArgumentNullException.ThrowIfNull(nameof(toCompare));
        ArgumentNullException.ThrowIfNull(nameof(keySelector));

        var remoteKeyValues = toCompare.ToDictionary(keySelector);

        var result = new List<ComparisonResult<TSource>>();
        var originalKeys = new HashSet<TKey>();

        foreach (var originalItem in original)
        {
            var localKey = keySelector(originalItem);
            originalKeys.Add(localKey);

            if (remoteKeyValues.TryGetValue(localKey, out TSource? changeCandidate))
            {
                if (changeCandidate is null)
                    continue;
                if (changeCandidate.Equals(originalItem))
                    continue;

                result.Add(new ComparisonResult<TSource>
                    (originalItem, changeCandidate, ComparisonResultType.Changed, originalItem.CompareAndRetrieveVariances(changeCandidate)));
            }
            else
                result.Add(new ComparisonResult<TSource>
                    (originalItem, default, ComparisonResultType.Removed, ImmutableDictionary<string, ComparisonVariance>.Empty));
        }
        var added = remoteKeyValues
                    .Where(x => !originalKeys.Contains(x.Key))
                    .Select(x => new ComparisonResult<TSource>(default, x.Value, ComparisonResultType.Added, ImmutableDictionary<string, ComparisonVariance>.Empty))
                    .ToList();

        var mergedResult = new List<ComparisonResult<TSource>>(result.Count + added.Count);
        mergedResult.AddRange(result);
        mergedResult.AddRange(added);

        return mergedResult;
    }
}