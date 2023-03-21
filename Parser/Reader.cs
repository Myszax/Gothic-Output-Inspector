using Parser.Enums;
using Parser.FileBlocks;
using System.Text;

namespace Parser
{
    sealed internal class Reader
    {

        private HashTableEntry[] _hashTableEntries;

        private readonly byte[] _typeSizes = new byte[] {
            0,                        // ?            = 0x00
	        0,                        // String       = 0x01,
	        sizeof(int),              // Int          = 0x02,
	        sizeof(float),            // Float        = 0x03,
	        sizeof(byte),             // Byte         = 0x04,
	        sizeof(ushort),           // Word         = 0x05,
	        sizeof(uint),             // Bool         = 0x06,
	        sizeof(float) * 3,        // Vec3         = 0x07,
	        sizeof(byte) * 4,         // Color        = 0x08,
	        0,                        // Raw          = 0x09,
	        0,                        // ?            = 0x0A
	        0,                        // ?            = 0x0B
	        0,                        // ?            = 0x0C
	        0,                        // ?            = 0x0D
	        0,                        // ?            = 0x0E
	        0,                        // ?            = 0x0F
	        0,                        // RawFloat     = 0x10,
	        sizeof(uint),             // Enum         = 0x11,
	        sizeof(uint),             // Hash         = 0x12,
        };

        private BinaryReader _reader;

        private ArchiveHeader _header;

        private readonly string _path;

        private readonly int _codePage;

        public Reader(string path, int codePage)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _path = path;
            _codePage = codePage;
            _hashTableEntries = Array.Empty<HashTableEntry>();
        }

        public List<Dialogue> Parse()
        {
            _reader = new BinaryReader(File.Open(_path, FileMode.Open), Encoding.GetEncoding(1252), false);
            //_reader = new BinaryReader(File.Open(path, FileMode.Open));

            _header = ParseHeader();

            var obj = new ArchiveObject();
            ReadObjectBegin(ref obj);

            if (!obj.ClassName.Equals("zCCSLib"))
            {
                throw new ParserException("root object is not 'zCCSLib'");
            }

            var itemCount = ReadInt(); // NumOfItems

            var listOfDialogues = ParseAllDialogues(itemCount);

            if (!ReadObjectEnd())
            {
                SkipObject(true);
                throw new ParserException("file not fully parsed");
            }

            _reader.Dispose();
            _reader.Close();

            return listOfDialogues;
        }

        private List<Dialogue> ParseAllDialogues(int itemCount)
        {
            var list = new List<Dialogue>();
            var obj = new ArchiveObject();
            for (int i = 0; i < itemCount; i++)
            {
                if (!ReadObjectBegin(ref obj) || !obj.ClassName.Equals("zCCSBlock"))
                {
                    throw new ParserException("expected 'zCCSBlock' but didn't find it");
                }

                var name = ReadString(false);
                var blockCount = ReadInt();
                ReadFloat();

                if (blockCount != 1)
                {
                    throw new ParserException($"expected only one block but got {blockCount} for {name}");
                }

                if (!ReadObjectBegin(ref obj) || !obj.ClassName.Equals("zCCSAtomicBlock"))
                {
                    throw new ParserException($"expected atomic block not found for {name}");
                }

                if (!ReadObjectBegin(ref obj) || !obj.ClassName.Equals("oCMsgConversation:oCNpcMessage:zCEventMessage"))
                {
                    throw new ParserException($"expected oCMsgConversation not found for {name}");
                }

                var type = ReadEnum();
                var text = ReadString();
                var soundName = ReadString(false);

                var dialogue = new Dialogue { Name = name, Text = text, Sound = soundName };
                list.Add(dialogue);

                if (!ReadObjectEnd())
                {
                    SkipObject(true);
                    throw new ParserException("oCMsgConversation not fully parsed");
                }

                if (!ReadObjectEnd())
                {
                    SkipObject(true);
                    throw new ParserException("zCCSAtomicBlock not fully parsed");
                }

                if (!ReadObjectEnd())
                {
                    SkipObject(true);
                    throw new ParserException("zCCSBlock not fully parsed");
                }
            }

            return list;
        }

        private bool ReadObjectBegin(ref ArchiveObject obj)
        {
            if (_reader.BaseStream.Length - _reader.BaseStream.Position < 6)
                return false;

            var mark = _reader.BaseStream.Position;
            if (_reader.ReadByte() != (byte)ArchiveBinSafeType.String)
            {
                ResetStream(mark);
                return false;
            }

            var line = new string(_reader.ReadChars(_reader.ReadUInt16()));

            if (line.Length <= 2)
            {
                ResetStream(mark);
                return false;
            }

            var parsedElements = line.Split(" ");
            if (parsedElements.Length != 4)
            {
                ResetStream(mark);
                return false;
            }

            obj.ObjectName = parsedElements[0][1..];
            obj.ClassName = parsedElements[1];
            obj.Version = short.Parse(parsedElements[2]);
            obj.Index = int.Parse(parsedElements[3].Substring(0, parsedElements[3].Length - 1));

            return true;
        }

        private bool ReadObjectEnd()
        {
            if (_reader.BaseStream.Length - _reader.BaseStream.Position < 6)
                return false;

            var mark = _reader.BaseStream.Position;
            if (_reader.ReadByte() != (byte)ArchiveBinSafeType.String)
            {
                ResetStream(mark);
                return false;
            }

            if (_reader.ReadUInt16() != 2)
            {
                ResetStream(mark);
                return false;
            }

            if (!ReadString(false, 2).Equals("[]"))
            {
                ResetStream(mark);
                return false;
            }

            return true;
        }

        private void SkipObject(bool skipCurrent)
        {
            var tmp = new ArchiveObject();
            var level = skipCurrent ? 1 : 0;

            do
            {
                if (ReadObjectBegin(ref tmp))
                {
                    ++level;
                }
                else if (ReadObjectEnd())
                {
                    --level;
                }
                else
                {
                    SkipEntry();
                }
            } while (level > 0);
        }

        private void SkipEntry()
        {
            var type = (ArchiveBinSafeType)_reader.ReadByte();

            switch (type)
            {
                case ArchiveBinSafeType.String:
                case ArchiveBinSafeType.Raw:
                case ArchiveBinSafeType.RawFloat:
                    SkipStreamBytesPosition(_reader.ReadUInt16());
                    break;
                case ArchiveBinSafeType.Enum:
                case ArchiveBinSafeType.Hash:
                case ArchiveBinSafeType.Int:
                case ArchiveBinSafeType.Float:
                case ArchiveBinSafeType.Bool:
                case ArchiveBinSafeType.Color:
                    _reader.ReadUInt32();
                    break;
                case ArchiveBinSafeType.Byte:
                    _reader.ReadByte();
                    break;
                case ArchiveBinSafeType.Word:
                    _reader.ReadUInt16();
                    break;
                case ArchiveBinSafeType.Vec3:
                    ReadFloat();
                    ReadFloat();
                    ReadFloat();
                    break;
            }
        }

        private void ResetStream(long mark)
        {
            _reader.BaseStream.Position = mark;
        }

        private void PareseHeaderBinSafe()
        {
            var version = _reader.ReadUInt32();
            var objCount = _reader.ReadUInt32();
            var offset = _reader.ReadUInt32();
            var markPos = _reader.BaseStream.Position;
            _reader.BaseStream.Position = offset;
            var hashTableSize = _reader.ReadUInt32();
            Array.Resize(ref _hashTableEntries, (int)hashTableSize);

            for (int i = 0; i < hashTableSize; i++)
            {
                var keyLength = _reader.ReadUInt16();
                var insertionIndex = _reader.ReadUInt16();
                var hashValue = _reader.ReadUInt32();
                var key = _reader.ReadChars(keyLength);

                _hashTableEntries[insertionIndex] = new HashTableEntry { Hash = hashValue, Key = new String(key) };
            }
            _reader.BaseStream.Position = markPos;
        }

        private ArchiveHeader ParseHeader()
        {
            var header = new ArchiveHeader();

            if (_reader.ReadLine() != "ZenGin Archive")
            {
                throw new ParserException("Header: Missing 'ZenGin Archive' at start.");
            }

            header.Version = HeaderGetVersion();
            header.Archiver = _reader.ReadLine();
            header.Format = HeaderGetFormat();
            header.Save = HeaderGetSave();

            var optionalLine = _reader.ReadLine();
            if (optionalLine.StartsWith("date "))
            {
                header.Date = DateTime.Parse(optionalLine[5..]);
                optionalLine = _reader.ReadLine();
            }

            if (optionalLine.StartsWith("user "))
            {
                header.User = optionalLine.Substring(5);
                optionalLine = _reader.ReadLine();
            }

            if (!optionalLine.Equals("END"))
            {
                throw new ParserException("Header: first END missing");
            }

            PareseHeaderBinSafe();

            return header;
        }

        private int HeaderGetVersion()
        {
            var version = _reader.ReadLine();
            if (!version.StartsWith("ver "))
            {
                throw new ParserException("Header: ver field missing");
            }

            return int.Parse(version[4..]);
        }

        private void SkipStreamBytesPosition(int count)
        {
            _reader.BaseStream.Position += count;
        }

        private ArchiveFormat HeaderGetFormat()
        {
            var format = _reader.ReadLine();
            if (format.Equals("ASCII"))
            {
                return ArchiveFormat.ASCII;
            }
            else if (format.Equals("BINARY"))
            {
                return ArchiveFormat.Binary;
            }
            else if (format.Equals("BIN_SAFE"))
            {
                return ArchiveFormat.BinSafe;
            }
            else
            {
                throw new ParserException("Header: Format not match.");
            }
        }

        private bool HeaderGetSave()
        {
            var saveGame = _reader.ReadLine();
            if (!saveGame.StartsWith("saveGame "))
            {
                throw new ParserException("Header: saveGame field missing");
            }

            return int.Parse(saveGame.Substring(saveGame.Length - 1)) != 0;
        }

        private string GetEntryKey()
        {
            var peekedByte = _reader.ReadByte();
            _reader.BaseStream.Position--;
            if (peekedByte != (byte)ArchiveBinSafeType.Hash)
            {
                throw new ParserException("Reader_BinSafe: invalid format");
            }

            SkipStreamBytesPosition(1);
            var hash = _reader.ReadUInt32();

            return _hashTableEntries[hash].Key;
        }

        private ushort EnsureEntryMeta(ArchiveBinSafeType type)
        {
            GetEntryKey();
            var tmpType = _reader.ReadByte();

            var size = (tmpType == (byte)ArchiveBinSafeType.String ||
                tmpType == (byte)ArchiveBinSafeType.Raw ||
                tmpType == (byte)ArchiveBinSafeType.RawFloat) ?
                _reader.ReadUInt16() : _typeSizes[tmpType];

            if ((byte)type != tmpType)
            {
                SkipStreamBytesPosition(size);
                throw new ParserException($"archive_reader_binsafe: type mismatch expected {type}, got: {tmpType}");
            }

            return size;
        }

        private int ReadInt()
        {
            EnsureEntryMeta(ArchiveBinSafeType.Int);
            return _reader.ReadInt32();
        }

        private float ReadFloat()
        {
            EnsureEntryMeta(ArchiveBinSafeType.Float);
            return _reader.ReadSingle();
        }

        private uint ReadEnum()
        {
            EnsureEntryMeta(ArchiveBinSafeType.Enum);
            return _reader.ReadUInt32();
        }

        private string ReadString(bool encode = true, ushort bytesCount = 0)
        {
            if (bytesCount == 0)
            {
                bytesCount = EnsureEntryMeta(ArchiveBinSafeType.String);
            }

            string ret;

            if (encode)
            {
                var bytesArray = _reader.ReadBytes(bytesCount);
                ret = Encoding.GetEncoding(_codePage).GetString(bytesArray);
            }
            else
            {
                var charsArray = _reader.ReadChars(bytesCount);
                ret = new String(charsArray);
            }

            return ret;
        }
    }
}
