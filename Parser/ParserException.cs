namespace Parser;

public sealed class ParserException : Exception
{
    public ParserException(string message) : base(message) { }
    public ParserException() { }
    public ParserException(string message, Exception innerException) : base(message, innerException) { }
}