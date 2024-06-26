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

    public DialogResult ShowOpenFileDialog(OpenFileDialogSettings fileDialogSettings, out string file, bool safeFileName);
    public DialogResult ShowOpenFileDialogMulti(OpenFileDialogSettings fileDialogSettings, out string[] files, bool safeFileNames);

    public DialogResult ShowSaveFileDialog(SaveFileDialogSettings fileDialogSettings, out string file);
    public DialogResult ShowSaveFileDialogMulti(SaveFileDialogSettings fileDialogSettings, out string[] files);

    public DialogResult ShowFolderBrowserDialog(FolderBrowserDialogSettings fileDialogSettings, out string path);
}

public sealed class DialogService : IDialogService
{
    public DialogResult ShowMessageBox(string? text) => MessageBox.Show(text);

    public DialogResult ShowMessageBox(string? text, string? caption) => MessageBox.Show(text, caption);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons) => MessageBox.Show(text, caption, buttons);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon) => MessageBox.Show(text, caption, buttons, icon);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton) =>
        MessageBox.Show(text, caption, buttons, icon, defaultButton);

    public DialogResult ShowMessageBox(string? text, string? caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, MessageBoxOptions options) =>
        MessageBox.Show(text, caption, buttons, icon, defaultButton, options);

    public DialogResult ShowOpenFileDialog(OpenFileDialogSettings fileDialogSettings, out string file, bool safeFileName)
    {
        var dialog = MapOpenFileDialogSettingsToOpenFileDialog(fileDialogSettings);
        dialog.Multiselect = false;

        var result = dialog.ShowDialog();

        if (safeFileName)
            file = dialog.SafeFileName;
        else
            file = dialog.FileName;

        return result;
    }

    public DialogResult ShowOpenFileDialogMulti(OpenFileDialogSettings fileDialogSettings, out string[] files, bool safeFileNames)
    {
        var dialog = MapOpenFileDialogSettingsToOpenFileDialog(fileDialogSettings);
        dialog.Multiselect = true;

        var result = dialog.ShowDialog();

        if (safeFileNames)
            files = dialog.SafeFileNames;
        else
            files = dialog.FileNames;

        return result;
    }

    public DialogResult ShowSaveFileDialog(SaveFileDialogSettings fileDialogSettings, out string file)
    {
        var dialog = MapSaveFileDialogSettingsToSaveFileDialog(fileDialogSettings);

        var result = dialog.ShowDialog();
        file = dialog.FileName;

        return result;
    }

    public DialogResult ShowSaveFileDialogMulti(SaveFileDialogSettings fileDialogSettings, out string[] files)
    {
        var dialog = MapSaveFileDialogSettingsToSaveFileDialog(fileDialogSettings);

        var result = dialog.ShowDialog();
        files = dialog.FileNames;

        return result;
    }

    public DialogResult ShowFolderBrowserDialog(FolderBrowserDialogSettings fileDialogSettings, out string path)
    {
        var dialog = MapFolderBrowserDialogSettingsToFolderBrowserDialog(fileDialogSettings);

        var result = dialog.ShowDialog();
        path = dialog.SelectedPath;

        return result;
    }

    private static OpenFileDialog MapOpenFileDialogSettingsToOpenFileDialog(OpenFileDialogSettings fileDialogSettings)
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

    private static SaveFileDialog MapSaveFileDialogSettingsToSaveFileDialog(SaveFileDialogSettings fileDialogSettings)
    {
        return new SaveFileDialog
        {
            AddExtension = fileDialogSettings.AddExtension,
            AddToRecent = fileDialogSettings.AddToRecent,
            AutoUpgradeEnabled = fileDialogSettings.AutoUpgradeEnabled,
            CheckFileExists = fileDialogSettings.CheckFileExists,
            CheckPathExists = fileDialogSettings.CheckPathExists,
            CheckWriteAccess = fileDialogSettings.CheckWriteAccess,
            CreatePrompt = fileDialogSettings.CreatePrompt,
            DefaultExt = fileDialogSettings.DefaultExt,
            DereferenceLinks = fileDialogSettings.DereferenceLinks,
            ExpandedMode = fileDialogSettings.ExpandedMode,
            Filter = fileDialogSettings.Filter,
            FilterIndex = fileDialogSettings.FilterIndex,
            InitialDirectory = fileDialogSettings.InitialDirectory,
            OkRequiresInteraction = fileDialogSettings.OkRequiresInteraction,
            OverwritePrompt = fileDialogSettings.OverwritePrompt,
            RestoreDirectory = fileDialogSettings.RestoreDirectory,
            ShowHelp = fileDialogSettings.ShowHelp,
            ShowHiddenFiles = fileDialogSettings.ShowHiddenFiles,
            ShowPinnedPlaces = fileDialogSettings.ShowPinnedPlaces,
            SupportMultiDottedExtensions = fileDialogSettings.SupportMultiDottedExtensions,
            Title = fileDialogSettings.Title,
            ValidateNames = fileDialogSettings.ValidateNames
        };
    }

    private static FolderBrowserDialog MapFolderBrowserDialogSettingsToFolderBrowserDialog(FolderBrowserDialogSettings fileDialogSettings)
    {
        return new FolderBrowserDialog
        {
            AddToRecent = fileDialogSettings.AddToRecent,
            AutoUpgradeEnabled = fileDialogSettings.AutoUpgradeEnabled,
            Description = fileDialogSettings.Description,
            InitialDirectory = fileDialogSettings.InitialDirectory,
            OkRequiresInteraction = fileDialogSettings.OkRequiresInteraction,
            RootFolder = fileDialogSettings.RootFolder,
            ShowHiddenFiles = fileDialogSettings.ShowHiddenFiles,
            ShowNewFolderButton = fileDialogSettings.ShowNewFolderButton,
            ShowPinnedPlaces = fileDialogSettings.ShowPinnedPlaces,
            UseDescriptionForTitle = fileDialogSettings.UseDescriptionForTitle
        };
    }
}
