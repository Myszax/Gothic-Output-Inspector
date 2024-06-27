namespace Parser.FileBlocks;

internal sealed class HashTableEntry
{
    public uint Hash { get; set; }
    public string Key { get; set; } = string.Empty;
}
