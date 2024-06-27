namespace Parser.FileBlocks;

internal sealed class AtomicMessage
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public uint Type { get; set; } = 0;
}
