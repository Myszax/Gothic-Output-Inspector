namespace Parser.FileBlocks
{
    sealed internal class MessageBlock
    {
        public string Name { get; set; }
        public AtomicMessage Message { get; set; }
    }
}
