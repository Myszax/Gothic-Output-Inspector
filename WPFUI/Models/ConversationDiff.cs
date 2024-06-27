using CommunityToolkit.Mvvm.ComponentModel;
using WPFUI.Comparer;

namespace WPFUI.Models;

public sealed partial class ConversationDiff : ObservableObject
{
    [ObservableProperty]
    private ComparisonResult<Conversation>? _diff;

    [ObservableProperty]
    private string _name = string.Empty;
}
