using System.Windows.Forms;

namespace WPFUI.Services;

public interface IDialogService
{
    public DialogResult ShowMessageBox(string? text);
    public DialogResult ShowMessageBox(string? text, string? caption);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options);

}

public class DialogService : IDialogService
{
    public DialogResult ShowMessageBox(string? text) => MessageBox.Show(text);

    public DialogResult ShowMessageBox(string? text, string? caption) => MessageBox.Show(text, caption);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons) => MessageBox.Show(text, caption, buttons);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon) => MessageBox.Show(text, caption, buttons, icon);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) =>
        MessageBox.Show(text, caption, buttons, icon, defaultButton);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options) =>
        MessageBox.Show(text, caption, buttons, icon, defaultButton, options);
}
