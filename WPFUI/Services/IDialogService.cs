using System.Windows.Forms;
using WPFUI.Components;

namespace WPFUI.Services;

public interface IDialogService
{
    public DialogResult ShowMessageBox(string? text);
    public DialogResult ShowMessageBox(string? text, string? caption);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);
    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options);

    public DialogResult ShowFileDialog(FileDialogSettings fileDialogSettings, out string file, bool safeFileName);
    public DialogResult ShowFileDialogMulti(FileDialogSettings fileDialogSettings, out string[] files, bool safeFileNames);
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

    public DialogResult ShowFileDialog(FileDialogSettings fileDialogSettings, out string file, bool safeFileName)
    {
        var dialog = MapFileDialogSettingsToOpenFileDialog(fileDialogSettings);
        dialog.Multiselect = false;

        var result = dialog.ShowDialog();

        if (safeFileName)
            file = dialog.SafeFileName;
        else
            file = dialog.FileName;

        return result;
    }

    public DialogResult ShowFileDialogMulti(FileDialogSettings fileDialogSettings, out string[] files, bool safeFileNames)
    {
        var dialog = MapFileDialogSettingsToOpenFileDialog(fileDialogSettings);
        dialog.Multiselect = true;

        var result = dialog.ShowDialog();

        if (safeFileNames)
            files = dialog.SafeFileNames;
        else
            files = dialog.FileNames;

        return result;
    }

    private static OpenFileDialog MapFileDialogSettingsToOpenFileDialog(FileDialogSettings fileDialogSettings)
    {
        return new OpenFileDialog
        {
            AddExtension = fileDialogSettings.AddExtension,
            AddToRecent = fileDialogSettings.AddToRecent,
            AutoUpgradeEnabled = fileDialogSettings.AutoUpgradeEnabled,
            CheckFileExists = fileDialogSettings.CheckFileExists,
            CheckPathExists = fileDialogSettings.CheckPathExists,
            DefaultExt = fileDialogSettings.DefaultExt,
            DereferenceLinks = fileDialogSettings.DereferenceLinks,
            Filter = fileDialogSettings.Filter,
            FilterIndex = fileDialogSettings.FilterIndex,
            InitialDirectory = fileDialogSettings.InitialDirectory,
            OkRequiresInteraction = fileDialogSettings.OkRequiresInteraction,
            ReadOnlyChecked = fileDialogSettings.ReadOnlyChecked,
            RestoreDirectory = fileDialogSettings.RestoreDirectory,
            SelectReadOnly = fileDialogSettings.SelectReadOnly,
            ShowHelp = fileDialogSettings.ShowHelp,
            ShowHiddenFiles = fileDialogSettings.ShowHiddenFiles,
            ShowPinnedPlaces = fileDialogSettings.ShowPinnedPlaces,
            ShowPreview = fileDialogSettings.ShowPreview,
            ShowReadOnly = fileDialogSettings.ShowReadOnly,
            SupportMultiDottedExtensions = fileDialogSettings.SupportMultiDottedExtensions,
            Title = fileDialogSettings.Title,
            ValidateNames = fileDialogSettings.ValidateNames
        };
    }
}
