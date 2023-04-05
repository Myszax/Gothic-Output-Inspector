using System.Windows;
using WPFUI.ViewModels;

namespace WPFUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
    public object SelectedTreeViewItem
    {
        get => GetValue(SelectedTreeViewItemProperty);
        set => SetValue(SelectedTreeViewItemProperty, value);
    }

    public static readonly DependencyProperty SelectedTreeViewItemProperty = DependencyProperty.Register(
        nameof(SelectedTreeViewItem),
        typeof(object),
        typeof(MainWindow),
        new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var treeView = sender as System.Windows.Controls.TreeView;
        SelectedTreeViewItem = treeView!.SelectedItem;
    }
}