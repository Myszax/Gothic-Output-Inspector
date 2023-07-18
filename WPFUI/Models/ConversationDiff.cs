using CommunityToolkit.Mvvm.ComponentModel;
using WPFUI.Comparer;

namespace WPFUI.Models;

public partial class ConversationDiff : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ComparisonResult<Conversation> _diff;
}