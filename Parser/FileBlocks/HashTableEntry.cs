namespace Parser.FileBlocks;

internal sealed class HashTableEntry
{
    public string Key { get; set; } = string.Empty;
    public uint Hash { get; set; }
}