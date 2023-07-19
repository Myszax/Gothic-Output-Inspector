using CommunityToolkit.Mvvm.ComponentModel;
using Parser;
using System;
using System.Text;
using WPFUI.Enums;

namespace WPFUI.Models;

public partial class Conversation : ObservableObject, IEquatable<Conversation>
{
    public string Name { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string EditedText { get; set; } = string.Empty;
    public string Sound { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string NpcName { get; set; } = string.Empty;
    public ConversationType Type { get; set; }
    public int Voice { get; set; }
    public int Number { get; set; }

    [ObservableProperty]
    private bool _isEdited = false;

    [ObservableProperty]
    private bool _isInspected = false;

    public static Conversation CreateConversationFromDialogue(Dialogue dialogue)
    {
        var conversation = new Conversation();
        var nameParts = dialogue.Name.Split('_');

        conversation.Type = GetTypeFromName(nameParts[0]);
        conversation.NpcName = GetNpcNameFromName(conversation.Type, nameParts);
        conversation.Voice = GetVoiceFromName(nameParts[^2]);
        conversation.Context = GetContextFromName(conversation.Type, nameParts);
        conversation.Number = GetNumberFromName(nameParts[^1]);
        conversation.Name = dialogue.Name;
        conversation.OriginalText = conversation.EditedText = dialogue.Text;
        conversation.Sound = dialogue.Sound;

        return conversation;
    }

    public override bool Equals(object? obj) => obj is not null && Equals(obj as Conversation);

    public override int GetHashCode() => Name.GetHashCode();

    public bool Equals(Conversation? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (!Name.Equals(other.Name, StringComparison.Ordinal))
            return false;
        if (Voice != other.Voice)
            return false;
        if (Number != other.Number)
            return false;
        if (Type != other.Type)
            return false;
        if (IsEdited != other.IsEdited)
            return false;
        if (IsInspected != other.IsInspected)
            return false;
        if (!OriginalText.Equals(other.OriginalText, StringComparison.Ordinal))
            return false;
        if (!EditedText.Equals(other.EditedText, StringComparison.Ordinal))
            return false;
        if (!Sound.Equals(other.Sound, StringComparison.Ordinal))
            return false;
        if (!Context.Equals(other.Context, StringComparison.Ordinal))
            return false;
        if (!NpcName.Equals(other.NpcName, StringComparison.Ordinal))
            return false;

        return true;
    }

    private static string GetContextFromName(ConversationType type, string[] nameParts)
    {
        if (type == ConversationType.Svm)
            return GetNpcNameFromName(type, nameParts);

        var sb = new StringBuilder();
        var count = GetIterationCountForDesiredType(type, nameParts.Length);
        for (var i = 2; i < count; i++)
        {
            sb.Append(nameParts[i]);
            sb.Append('_');
        }

        return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : GetNpcNameFromName(type, nameParts);
    }

    private static int GetIterationCountForDesiredType(ConversationType type, int length)
    {
        return type switch
        {
            ConversationType.Dialogue or ConversationType.Trialogue => length - 2,
            _ => length
        };
    }

    private static string GetNpcNameFromName(ConversationType type, string[] nameParts)
    {
        if (type == ConversationType.Svm)
            return "SVM";
        else
        {
            var npcName = int.TryParse(nameParts[1], out _) ? nameParts[2] : nameParts[1];
            return npcName;
        }
    }

    private static int GetNumberFromName(string number)
    {
        return int.TryParse(number, out var parsedInt) ? parsedInt : 0;
    }

    private static ConversationType GetTypeFromName(string typeName)
    {
        if (typeName.StartsWith("dia", StringComparison.OrdinalIgnoreCase))
            return ConversationType.Dialogue;
        else if (typeName.StartsWith("tria", StringComparison.OrdinalIgnoreCase))
            return ConversationType.Trialogue;
        else if (typeName.StartsWith("svm", StringComparison.OrdinalIgnoreCase))
            return ConversationType.Svm;
        else
            return ConversationType.Other;
    }

    private static int GetVoiceFromName(string voice)
    {
        return int.TryParse(voice, out var parsedInt) ? parsedInt : 0;
    }
}