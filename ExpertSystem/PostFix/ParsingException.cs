using System;

namespace ExpertSystem.PostFix
{
    public class ParsingException : Exception
    {
        public ParsingException(string message):base(message)
        {
        }
    }
}