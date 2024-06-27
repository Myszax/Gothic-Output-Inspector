namespace Parser.FileBlocks;

internal sealed class ArchiveObject
{
    public string ClassName { get; set; } = string.Empty;
    public int Index { get; set; }
    public string ObjectName { get; set; } = string.Empty;
    public short Version { get; set; }
}
