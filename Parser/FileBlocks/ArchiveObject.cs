namespace Parser.FileBlocks
{
    internal sealed class ArchiveObject
    {
        public string ObjectName { get; set; }
        public string ClassName { get; set; }
        public short Version { get; set; }
        public int Index { get; set; }
    }
}
