using System.Collections.Generic;
using WPFUI.Components;
using WPFUI.Models;

namespace WPFUI.Services;

public interface IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; }
    public HashSet<Conversation> ConversationsToCompare { get; set; }
    public Conversation CurrentConversation { get; set; }
    public string CompareWindowTitle { get; set; }
}

public sealed class DataService : IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; } = [];
    public HashSet<Conversation> ConversationsToCompare { get; set; } = [];
    public Conversation CurrentConversation { get; set; } = new();
    public string CompareWindowTitle { get; set; } = string.Empty;
}
