using WPFUI.Components;

namespace WPFUI.Models;

public sealed class Conversation
{
    public string Name { get; set; }
    public string Text { get; set; }
    public string Sound { get; set; }
    public string Context { get; set; }
    public string NpcName { get; set; }
    public ConversationType Type { get; set; }
    public int Voice { get; set; }
    public int Number { get; set; }
}