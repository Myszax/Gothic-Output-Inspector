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

    private void TextBox_CopyToClipboard(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var textBox = sender as System.Windows.Controls.TextBox;
        if (textBox is null || string.IsNullOrEmpty(textBox.Text))
            return;

        Clipboard.SetText(textBox.Text);
    }
}