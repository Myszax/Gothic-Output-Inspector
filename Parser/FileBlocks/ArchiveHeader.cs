using Parser.Enums;

namespace Parser.FileBlocks;

internal sealed class ArchiveHeader
{
    public int Version { get; set; }
    public string Archiver { get; set; } = string.Empty;
    public ArchiveFormat Format { get; set; }
    public bool Save { get; set; } = false;
    public string User { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}