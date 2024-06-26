namespace WPFUI.Components;

public sealed class SaveFileDialogSettings
{
    public bool AddExtension { get; set; } = true;

    public bool AddToRecent { get; set; } = true;

    public bool AutoUpgradeEnabled { get; set; } = true;

    public bool CheckFileExists { get; set; } = false;

    public bool CheckPathExists { get; set; } = true;

    public bool CheckWriteAccess { get; set; } = true;

    public bool CreatePrompt { get; set; } = false;

    public string DefaultExt { get; set; } = string.Empty;

    public bool DereferenceLinks { get; set; } = true;

    public bool ExpandedMode { get; set; } = true;

    public string Filter { get; set; } = string.Empty;

    public int FilterIndex { get; set; } = 1;

    public string InitialDirectory { get; set; } = string.Empty;

    public bool OkRequiresInteraction { get; set; } = false;

    public bool OverwritePrompt { get; set; } = true;

    public bool RestoreDirectory { get; set; } = false;

    public bool ShowHelp { get; set; } = false;

    public bool ShowHiddenFiles { get; set; } = false;

    public bool ShowPinnedPlaces { get; set; } = true;

    public bool SupportMultiDottedExtensions { get; set; } = false;

    public string Title { get; set; } = string.Empty;

    public bool ValidateNames { get; set; } = true;
}
