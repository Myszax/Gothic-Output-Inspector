namespace Parser.FileBlocks;

internal sealed class MessageBlock
{
    public AtomicMessage Message { get; set; } = new();
    public string Name { get; set; } = string.Empty;
}
