namespace Parser.FileBlocks
{
    sealed internal class AtomicMessage
    {
        public uint Type { get; set; } = 0;
        public string Text { get; set; }
        public string Name { get; set; }
    }
}
