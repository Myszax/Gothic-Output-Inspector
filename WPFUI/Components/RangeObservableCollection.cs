using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WPFUI.Components;

public sealed class RangeObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    public void AddRange(IEnumerable<T> list)
    {
        ArgumentNullException.ThrowIfNull(nameof(list));

        _suppressNotification = true;

        foreach (T item in list)
            Add(item);

        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
            base.OnCollectionChanged(e);
    }
}
