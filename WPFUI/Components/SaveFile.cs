using System.Collections.Generic;
using WPFUI.Models;

namespace WPFUI.Components;

public sealed class SaveFile
{
    public int Version { get; set; } = 1;

    public List<Conversation> Conversations { get; set; } = new();

    public string OriginalEncoding { get; set; } = string.Empty;

    public string ChosenEncoding { get; set; } = string.Empty;

    public string AudioPath { get; set; } = string.Empty;
}