using System.Collections.Generic;
using System.Linq;
using WPFUI.Comparer;

namespace WPFUI.Components;

public partial class ColoredText
{
    public bool Name { get; set; } = false;
    public bool OriginalText { get; set; } = false;
    public bool EditedText { get; set; } = false;
    public bool Sound { get; set; } = false;
    public bool Context { get; set; } = false;
    public bool NpcName { get; set; } = false;
    public bool Type { get; set; } = false;
    public bool Voice { get; set; } = false;
    public bool Number { get; set; } = false;
    public bool Edited { get; set; } = false;
    public bool Inspected { get; set; } = false;

    public static ColoredText Create(IDictionary<string, ComparisonVariance> variances)
    {
        var ct = new ColoredText();

        if (variances is null || !variances.Any())
            return ct;

        ct.Name = variances.ContainsKey("Name");
        ct.OriginalText = variances.ContainsKey("OriginalText");
        ct.EditedText = variances.ContainsKey("EditedText");
        ct.Sound = variances.ContainsKey("Sound");
        ct.Context = variances.ContainsKey("Context");
        ct.NpcName = variances.ContainsKey("NpcName");
        ct.Type = variances.ContainsKey("Type");
        ct.Voice = variances.ContainsKey("Voice");
        ct.Number = variances.ContainsKey("Number");
        ct.Edited = variances.ContainsKey("IsEdited");
        ct.Inspected = variances.ContainsKey("IsInspected");

        return ct;
    }
}