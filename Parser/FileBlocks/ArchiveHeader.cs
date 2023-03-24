using Parser.Enums;

namespace Parser.FileBlocks
{
    sealed internal class ArchiveHeader
    {
        public int Version { get; set; }
        public string Archiver { get; set; }
        public ArchiveFormat Format { get; set; }
        public bool Save { get; set; } = false;
        public string User { get; set; }
        public DateTime Date { get; set; }
    }
}
