using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    public class Token
    {
        public string RawString { get; }

        public Token(string text)
        {
            RawString = text;
        }
    }

    public class TextToken : Token
    {
        public TextToken(string text) : base(text)
        {
        }
    }

    public class GenericToken : Token
    {
        public GenericToken(string text) : base(text)
        {
        }
    }
}
