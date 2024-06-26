namespace Parser.FileBlocks;

internal sealed class MessageBlock
{
    public string Name { get; set; } = string.Empty;
    public AtomicMessage Message { get; set; } = new();
}