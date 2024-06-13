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
    public string CompareWindowTitle { get; set; }

    public FilterType SelectedFilterTypeCompareMode { get; set; }
    public bool IsEnabledFilterCompareModeIsInspected { get; set; }
    public bool IsEnabledIgnoreInspectedWhileTransfer { get; set; }
}

public sealed class DataService : IDataService
{
    public RangeObservableCollection<Conversation> Data { get; set; } = [];
    public HashSet<Conversation> ConversationsToCompare { get; set; } = [];
    public string AudioFilesPath { get; set; } = string.Empty;
    public Conversation CurrentConversation { get; set; } = new();
    public string CompareWindowTitle { get; set; } = string.Empty;

    public FilterType SelectedFilterTypeCompareMode { get; set; } = FilterType.HideAll;
    public bool IsEnabledFilterCompareModeIsInspected { get; set; } = true;
    public bool IsEnabledIgnoreInspectedWhileTransfer { get; set; } = true;
}
