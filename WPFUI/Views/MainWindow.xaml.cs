using System.Windows;
using WPFUI.ViewModels;

namespace WPFUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        Closing += ((MainWindowViewModel)DataContext).OnWindowClosing;
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

    private void TextBox_CopyToClipboard(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var textBox = sender as System.Windows.Controls.TextBox;
        if (textBox is null || string.IsNullOrEmpty(textBox.Text))
            return;

        Clipboard.SetText(textBox.Text);
    }
}