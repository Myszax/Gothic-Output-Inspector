namespace WPFUI.Components;

public sealed class FolderBrowserDialogSettings
{
    public bool AddToRecent { get; set; } = true;

    public bool AutoUpgradeEnabled { get; set; } = true;

    public string Description { get; set; } = string.Empty;

    public string InitialDirectory { get; set; } = string.Empty;

    public bool OkRequiresInteraction { get; set; } = false;

    public System.Environment.SpecialFolder RootFolder { get; set; } = System.Environment.SpecialFolder.Desktop;

    public bool ShowHiddenFiles { get; set; } = false;

    public bool ShowNewFolderButton { get; set; } = true;

    public bool ShowPinnedPlaces { get; set; } = true;

    public bool UseDescriptionForTitle { get; set; } = false;
}
