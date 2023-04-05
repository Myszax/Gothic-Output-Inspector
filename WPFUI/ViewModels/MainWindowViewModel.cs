using CommunityToolkit.Mvvm.ComponentModel;

namespace WPFUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private object _selectedTreeItem;
}
