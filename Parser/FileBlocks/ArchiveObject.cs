namespace Parser.FileBlocks;

internal sealed class ArchiveObject
{
    public string ObjectName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public short Version { get; set; }
    public int Index { get; set; }
}