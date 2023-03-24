namespace Parser
{
    internal sealed class ParserException : Exception
    {
        public ParserException(string message) : base(message) { }
        public ParserException() { }
        public ParserException(string message, Exception innerException) : base(message, innerException) { }
    }
}
