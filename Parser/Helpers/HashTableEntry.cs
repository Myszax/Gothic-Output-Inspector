namespace Parser.Helpers
{
    sealed internal record HashTableEntry
    {
        public string? Key { get; set; }
        public uint Hash { get; set; }
    }
}
