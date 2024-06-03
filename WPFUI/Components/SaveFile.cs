using System;
using System.Collections.Generic;
using WPFUI.Models;

namespace WPFUI.Components;

public sealed class SaveFile
{
    public int Version { get; set; } = 1;

    public IEnumerable<Conversation> Conversations { get; set; } = [];

    public string OriginalEncoding { get; set; } = string.Empty;

    public string ChosenEncoding { get; set; } = string.Empty;

    public string AudioPath { get; set; } = string.Empty;

    public StringComparison ComparisonMethod { get; set; } = StringComparison.Ordinal;

    public bool EnabledFilterName { get; set; } = true;

    public bool EnabledFilterOriginalText { get; set; } = true;

    public bool EnabledFilterEditedText { get; set; } = false;

    public FilterType FilterType { get; set; } = FilterType.HideAll;

    public FilterType FilterTypeCompareMode { get; set; } = FilterType.HideAll;

    public bool EnabledFilterIsInspected { get; set; } = true;

    public bool EnabledFilterIsEdited { get; set; } = true;

    public bool EnabledFilterCompareModeIsInspected { get; set; } = true;

    public bool EnabledIgnoreInspectedWhileTransfer { get; set; } = true;

    public float AudioPlayerVolume { get; set; } = 1.0f;

    public float AudioPlayerPreviousVolume { get; set; } = 0f;

    public bool AudioPlayerMuted { get; set; } = false;
}