using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    public class Token
    {
        public string RawString { get; set; }

        public Token(string text)
        {
            RawString = text;
        }

        public override string ToString()
        {
            return RawString;
        }
    }

    public class TextToken : Token
    {
        public readonly int count;

        public TextToken(string text) : base(text)
        {
            count = PhraseCharacterCounter.GetCharacterCount(text);
        }

        public string GetTextWithoutHats()
        {
            return RawString.Trim(new char[] {'^'});
        }
    }

    public class GenericToken : Token
    {
        public GenericToken(string text) : base(text)
        {
        }
    }

    public class ColonToken : Token
    {
        public ColonToken(string text) : base(text)
        {
        }
    }

    public class WaitToken : Token
    {
        public int textAfterClick;

        public WaitToken(string text) : base(text)
        {

        }

        public override string ToString()
        {
            return $"{RawString}{textAfterClick}";
        }
    }

    public class PageWait : WaitToken
    {
        public PageWait(string text) : base(text)
        {
        }

        public override string ToString()
        {
            return $"/\nadv_page_wait {textAfterClick}:";
        }
    }

    public class ClickWait : WaitToken
    {
        public bool? isLastClickWaitOnLine; //assigned externally
        public string leadingWhiteSpace;

        public ClickWait(string text) : base(text)
        {
            leadingWhiteSpace = string.Empty;
        }

        public override string ToString()
        {
            return $"/\nadv_click_wait {textAfterClick}, {(isLastClickWaitOnLine.Value ? 1 : 0)}, \"{leadingWhiteSpace}\":";
        }
    }

    //Special token emitted at the end of every line
    public class NewLineToken : Token
    {
        public NewLineToken(string text) : base(text)
        {
        }
    }

    public class DisableNewLine : Token
    {
        public DisableNewLine(string text) : base(text)
        {
        }
    }
}
