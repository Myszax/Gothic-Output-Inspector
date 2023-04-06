namespace Parser;

public sealed record Dialogue
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Sound { get; set; } = string.Empty;
}