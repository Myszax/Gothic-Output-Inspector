using System.Collections.Generic;
using WPFUI.Components;
using WPFUI.Models;

namespace WPFUI.Services;

public interface IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; }
    public void AddItem(Conversation item);

    public void AddRange(IEnumerable<Conversation> items);
}

public sealed class DataService : IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; } = [];

    public void AddItem(Conversation item) => Data.Add(item);

    public void AddRange(IEnumerable<Conversation> items) => Data.AddRange(items);
}
