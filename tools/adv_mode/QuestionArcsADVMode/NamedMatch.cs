using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    public enum MatchType
    {
        langen,
        colon,
        dwaveAlias,
        dwavePath,
        voiceDelayOrWait,
        text,
        endOfLine,
        textSpeed,
        waitOrDelay,
        disableNewLine,
        changeColor,
        comment,
        whitespace,
    }

    public class NamedMatch
    {
        public MatchType Type { get; }
        public Match Match { get; }

        public NamedMatch(MatchType type, Match match)
        {
            Type = type;
            Match = match;
        }
    }
}
