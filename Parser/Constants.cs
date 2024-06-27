namespace Parser;

public static class Constants
{
    #region Parser_Numbers
    public const int MAXIMUM_BLOCK_COUNT_READED = 1;
    public const int MINIMUM_OBJECT_LENGTH = 2;
    public const long MINIMUM_REMAINING_BYTES_TO_READ_OBJECT = 6;
    public const ushort OBJECT_END_LENGTH = 2;
    public const int PARSED_OBJECT_ELEMENTS_COUNT = 4;
    #endregion

    #region ZenGine_Archive_Formats
    public const string ARCHIVE_FORMAT_ASCII = "ASCII";
    public const string ARCHIVE_FORMAT_BIN_SAFE = "BIN_SAFE";
    public const string ARCHIVE_FORMAT_BINARY = "BINARY";
    #endregion

    #region ZenGine_Archive_Header_Strings
    public const string HEADER_DATE = "date ";
    public const string HEADER_END = "END";
    public const string HEADER_SAVEGAME = "saveGame ";
    public const string HEADER_USER = "user ";
    public const string HEADER_VER = "ver ";
    public const string HEADER_ZENGINE_ARCHIVE = "ZenGin Archive";
    #endregion

    #region ZenGine_Objects_Strings    
    public const string zCCSAtomicBlock = "zCCSAtomicBlock";
    public const string zCCSBlock = "zCCSBlock";
    public const string zCCSLib = "zCCSLib";
    public const string zCEventMessage = "oCMsgConversation:oCNpcMessage:zCEventMessage";
    public const string zOBJECT_END = "[]";
    #endregion
}
