namespace Parser.FileBlocks
{
    sealed internal class HashTableEntry
    {
        public string Key { get; set; }
        public uint Hash { get; set; }
    }
}
