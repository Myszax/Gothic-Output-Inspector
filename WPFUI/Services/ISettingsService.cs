using System;
using System.Text;
using WPFUI.Enums;

namespace WPFUI.Services;

public interface ISettingsService
{
    public Encoding MainCurrentEncoding { get; set; }
    public StringComparison MainComparisonMethod { get; set; }
    public bool MainIsEnabledFilterName { get; set; }
    public bool MainIsEnabledFilterEditedText { get; set; }
    public bool MainIsEnabledFilterIsEdited { get; set; }
    public bool MainIsEnabledFilterIsInspected { get; set; }
    public bool MainIsEnabledFilterOriginalText { get; set; }
    public FilterType MainFilterType { get; set; }
    public Encoding MainOriginalEncoding { get; set; }
    public string MainPathToSaveFile { get; set; }

    public bool AudioPlayerIsMuted { get; set; }
    public string AudioPlayerPathToFiles { get; set; }
    public float AudioPlayerPreviousVolume { get; set; }
    public float AudioPlayerVolume { get; set; }

    public StringComparison CompareModeComparisonMethod { get; set; }
    public bool CompareModeIsEnabledFilterIsInspected { get; set; }
    public bool CompareModeIsEnabledIgnoreInspectedWhileTransfer { get; set; }
    public FilterType CompareModeSelectedFilterType { get; set; }
}

public sealed class SettingsService : ISettingsService
{
    public Encoding MainCurrentEncoding { get; set; }
    public StringComparison MainComparisonMethod { get; set; } = StringComparison.Ordinal;
    public bool MainIsEnabledFilterName { get; set; } = true;
    public bool MainIsEnabledFilterEditedText { get; set; } = false;
    public bool MainIsEnabledFilterIsEdited { get; set; } = true;
    public bool MainIsEnabledFilterIsInspected { get; set; } = true;
    public bool MainIsEnabledFilterOriginalText { get; set; } = true;
    public FilterType MainFilterType { get; set; } = FilterType.HideAll;
    public Encoding MainOriginalEncoding { get; set; }
    public string MainPathToSaveFile { get; set; } = string.Empty;

    public bool AudioPlayerIsMuted { get; set; } = false;
    public string AudioPlayerPathToFiles { get; set; } = string.Empty;
    public float AudioPlayerPreviousVolume { get; set; } = 0f;
    public float AudioPlayerVolume { get; set; } = 1.0f;

    public StringComparison CompareModeComparisonMethod { get; set; } = StringComparison.Ordinal;
    public bool CompareModeIsEnabledFilterIsInspected { get; set; } = true;
    public bool CompareModeIsEnabledIgnoreInspectedWhileTransfer { get; set; } = true;
    public FilterType CompareModeSelectedFilterType { get; set; } = FilterType.HideAll;

    public SettingsService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // need to register to be able to use Encoding.GetEncoding()

        MainCurrentEncoding = MainOriginalEncoding = Encoding.GetEncoding(1250);
    }
}
