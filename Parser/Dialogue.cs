namespace Parser
{
    internal sealed record Dialogue
    {
        public string? Name { get; set; }
        public string? Text { get; set; }
        public string? Sound { get; set; }
    }
}