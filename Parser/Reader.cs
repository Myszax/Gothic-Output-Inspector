﻿using Parser.Enums;
using Parser.FileBlocks;
using System.Text;
using static Parser.Constants;

namespace Parser
{
    internal sealed class Reader
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

            if (!obj.ClassName.Equals(zCCSLib))
            {
                throw new ParserException($"root object is not '{zCCSLib}'");
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
                if (!ReadObjectBegin(ref obj) || !obj.ClassName.Equals(zCCSBlock))
                {
                    throw new ParserException($"expected '{zCCSBlock}' but didn't find it");
                }

                var name = ReadString(false);
                var blockCount = ReadInt();
                ReadFloat(); // subBlock0

                if (blockCount != MAXIMUM_BLOCK_COUNT_READED)
                {
                    throw new ParserException($"expected only one block but got {blockCount} for {name}");
                }

                if (!ReadObjectBegin(ref obj) || !obj.ClassName.Equals(zCCSAtomicBlock))
                {
                    throw new ParserException($"expected atomic block, not found for {name}");
                }

                if (!ReadObjectBegin(ref obj) || !obj.ClassName.Equals(zCEventMessage))

                {
                    throw new ParserException($"expected {zCEventMessage} not found for {name}");
                }

                var type = ReadEnum();
                var text = ReadString();
                var soundName = ReadString(false);

                var dialogue = new Dialogue { Name = name, Text = text, Sound = soundName };
                list.Add(dialogue);

                if (!ReadObjectEnd())
                {
                    SkipObject(true);
                    throw new ParserException($"{zCEventMessage} not fully parsed");
                }

                if (!ReadObjectEnd())
                {
                    SkipObject(true);
                    throw new ParserException($"{zCCSAtomicBlock} not fully parsed");
                }

                if (!ReadObjectEnd())
                {
                    SkipObject(true);
                    throw new ParserException($"{zCCSBlock} not fully parsed");
                }
            }

            return list;
        }

        private bool ReadObjectBegin(ref ArchiveObject obj)
        {
            if (_reader.BaseStream.Length - _reader.BaseStream.Position < MINIMUM_REMAINING_BYTES_TO_READ_OBJECT)
                return false;

            var mark = _reader.BaseStream.Position;
            if (_reader.ReadByte() != (byte)ArchiveTypeBinSafe.String)
            {
                ResetStream(mark);
                return false;
            }

            var line = new string(_reader.ReadChars(_reader.ReadUInt16()));

            if (line.Length <= MINIMUM_OBJECT_LENGTH)
            {
                ResetStream(mark);
                return false;
            }

            var parsedElements = line.Split(" ");
            if (parsedElements.Length != PARSED_OBJECT_ELEMENTS_COUNT)
            {
                ResetStream(mark);
                return false;
            }

            obj.ObjectName = parsedElements[0][1..];
            obj.ClassName = parsedElements[1];
            obj.Version = short.Parse(parsedElements[2]);
            obj.Index = int.Parse(parsedElements[3][..^1]);

            return true;
        }

        private bool ReadObjectEnd()
        {
            if (_reader.BaseStream.Length - _reader.BaseStream.Position < MINIMUM_REMAINING_BYTES_TO_READ_OBJECT)
                return false;

            var mark = _reader.BaseStream.Position;
            if (_reader.ReadByte() != (byte)ArchiveTypeBinSafe.String)
            {
                ResetStream(mark);
                return false;
            }

            if (_reader.ReadUInt16() != OBJECT_END_LENGTH)
            {
                ResetStream(mark);
                return false;
            }

            if (!ReadString(false, OBJECT_END_LENGTH).Equals(OBJECT_END))
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
            var type = (ArchiveTypeBinSafe)_reader.ReadByte();

            switch (type)
            {
                case ArchiveTypeBinSafe.String:
                case ArchiveTypeBinSafe.Raw:
                case ArchiveTypeBinSafe.RawFloat:
                    SkipStreamBytesPosition(_reader.ReadUInt16());
                    break;
                case ArchiveTypeBinSafe.Enum:
                case ArchiveTypeBinSafe.Hash:
                case ArchiveTypeBinSafe.Int:
                case ArchiveTypeBinSafe.Float:
                case ArchiveTypeBinSafe.Bool:
                case ArchiveTypeBinSafe.Color:
                    _reader.ReadUInt32();
                    break;
                case ArchiveTypeBinSafe.Byte:
                    _reader.ReadByte();
                    break;
                case ArchiveTypeBinSafe.Word:
                    _reader.ReadUInt16();
                    break;
                case ArchiveTypeBinSafe.Vec3:
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

                _hashTableEntries[insertionIndex] = new HashTableEntry { Hash = hashValue, Key = new string(key) };
            }
            _reader.BaseStream.Position = markPos;
        }

        private ArchiveHeader ParseHeader()
        {
            var header = new ArchiveHeader();

            if (!_reader.ReadLine().Equals(HEADER_ZENGINE_ARCHIVE))
            {
                throw new ParserException($"Header: Missing '{HEADER_ZENGINE_ARCHIVE}' at start.");
            }

            header.Version = HeaderGetVersion();
            header.Archiver = _reader.ReadLine();
            header.Format = HeaderGetFormat();
            header.Save = HeaderGetSave();

            var optionalLine = _reader.ReadLine();
            if (optionalLine.StartsWith(HEADER_DATE))
            {
                header.Date = DateTime.Parse(optionalLine[HEADER_DATE.Length..]);
                optionalLine = _reader.ReadLine();
            }

            if (optionalLine.StartsWith(HEADER_USER))
            {
                header.User = optionalLine[HEADER_USER.Length..];
                optionalLine = _reader.ReadLine();
            }

            if (!optionalLine.Equals(HEADER_END))
            {
                throw new ParserException($"Header: first '{HEADER_END}' missing");
            }

            PareseHeaderBinSafe();

            return header;
        }

        private int HeaderGetVersion()
        {
            var version = _reader.ReadLine();
            if (!version.StartsWith(HEADER_VER))
            {
                throw new ParserException($"Header: '{HEADER_VER}' field missing");
            }

            return int.Parse(version[HEADER_VER.Length..]);
        }

        private void SkipStreamBytesPosition(int count)
        {
            _reader.BaseStream.Position += count;
        }

        private ArchiveFormat HeaderGetFormat()
        {
            var format = _reader.ReadLine();
            if (format.Equals(ARCHIVE_FORMAT_ASCII))
            {
                return ArchiveFormat.ASCII;
            }
            else if (format.Equals(ARCHIVE_FORMAT_BINARY))
            {
                return ArchiveFormat.Binary;
            }
            else if (format.Equals(ARCHIVE_FORMAT_BIN_SAFE))
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
            if (!saveGame.StartsWith(HEADER_SAVEGAME))
            {
                throw new ParserException($"Header: `{HEADER_SAVEGAME}` field missing");
            }

            return int.Parse(saveGame[^1..]) != 0;
        }

        private string GetEntryKey()
        {
            var peekedByte = _reader.ReadByte();
            _reader.BaseStream.Position--;
            if (peekedByte != (byte)ArchiveTypeBinSafe.Hash)
            {
                throw new ParserException("Reader_BinSafe: invalid format");
            }

            SkipStreamBytesPosition(1);
            var hash = _reader.ReadUInt32();

            return _hashTableEntries[hash].Key;
        }

        private ushort EnsureEntryMeta(ArchiveTypeBinSafe type)
        {
            GetEntryKey();
            var tmpType = _reader.ReadByte();

            var size = (ArchiveTypeBinSafe)tmpType switch
            {
                ArchiveTypeBinSafe.String or ArchiveTypeBinSafe.Raw or ArchiveTypeBinSafe.RawFloat => _reader.ReadUInt16(),
                _ => _typeSizes[tmpType],
            };

            if ((byte)type != tmpType)
            {
                SkipStreamBytesPosition(size);
                throw new ParserException($"archive_reader_binsafe: type mismatch expected {type}, got: {tmpType}");
            }

            return size;
        }

        private int ReadInt()
        {
            EnsureEntryMeta(ArchiveTypeBinSafe.Int);
            return _reader.ReadInt32();
        }

        private float ReadFloat()
        {
            EnsureEntryMeta(ArchiveTypeBinSafe.Float);
            return _reader.ReadSingle();
        }

        private uint ReadEnum()
        {
            EnsureEntryMeta(ArchiveTypeBinSafe.Enum);
            return _reader.ReadUInt32();
        }

        private string ReadString(bool encode = true, ushort bytesCount = 0)
        {
            if (bytesCount == 0)
            {
                bytesCount = EnsureEntryMeta(ArchiveTypeBinSafe.String);
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
                ret = new string(charsArray);
            }

            return ret;
        }
    }
}
