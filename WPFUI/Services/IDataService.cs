using System.Collections.Generic;
using WPFUI.Components;
using WPFUI.Models;

namespace WPFUI.Services;

public interface IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; }
    public HashSet<Conversation> ConversationsToCompare { get; set; }
    public string AudioFilesPath { get; set; }
    public Conversation CurrentConversation { get; set; }
}

public sealed class DataService : IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; } = [];
    public HashSet<Conversation> ConversationsToCompare { get; set; } = [];
    public string AudioFilesPath { get; set; } = string.Empty;
    public Conversation CurrentConversation { get; set; } = new();
}
