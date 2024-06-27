using System.Collections.Generic;
using System.Linq;
using WPFUI.Comparer;

namespace WPFUI.Components;

public sealed class ColoredText
{
    public bool Context { get; set; } = false;
    public bool Edited { get; set; } = false;
    public bool EditedText { get; set; } = false;
    public bool Inspected { get; set; } = false;
    public bool Name { get; set; } = false;
    public bool NpcName { get; set; } = false;
    public bool Number { get; set; } = false;
    public bool OriginalText { get; set; } = false;
    public bool Sound { get; set; } = false;
    public bool Type { get; set; } = false;
    public bool Voice { get; set; } = false;

    public static ColoredText Create(IDictionary<string, ComparisonVariance> variances)
    {
        var ct = new ColoredText();

        if (variances?.Any() != true)
            return ct;

        ct.Context = variances.ContainsKey("Context");
        ct.Edited = variances.ContainsKey("IsEdited");
        ct.EditedText = variances.ContainsKey("EditedText");
        ct.Inspected = variances.ContainsKey("IsInspected");
        ct.Name = variances.ContainsKey("Name");
        ct.NpcName = variances.ContainsKey("NpcName");
        ct.Number = variances.ContainsKey("Number");
        ct.OriginalText = variances.ContainsKey("OriginalText");
        ct.Sound = variances.ContainsKey("Sound");
        ct.Type = variances.ContainsKey("Type");
        ct.Voice = variances.ContainsKey("Voice");

        return ct;
    }
}
