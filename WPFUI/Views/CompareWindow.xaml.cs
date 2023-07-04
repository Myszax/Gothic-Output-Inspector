using System.Windows;
using WPFUI.ViewModels;

namespace WPFUI;

public partial class CompareWindow : Window
{
    public CompareWindow(MainWindowViewModel mainWindowVM)
    {
        InitializeComponent();
        DataContext = mainWindowVM;
    }

    private void TextBox_CopyToClipboard(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox || string.IsNullOrEmpty(textBox.Text))
            return;

        Clipboard.SetText(textBox.Text);
    }
}