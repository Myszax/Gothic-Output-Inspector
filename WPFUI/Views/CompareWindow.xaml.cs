using System.Windows;

namespace WPFUI;

public partial class CompareWindow : Window
{
    public CompareWindow()
    {
        InitializeComponent();
    }

    private void TextBox_CopyToClipboard(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox || string.IsNullOrEmpty(textBox.Text))
            return;

        Clipboard.SetText(textBox.Text);
    }
}