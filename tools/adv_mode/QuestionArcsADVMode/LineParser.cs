using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace QuestionArcsADVMode
{
    //parases a line, tries to detects phrases and insert line counts
    public class LineParser
    {
        public class NamedRegex : Regex
        {
            public MatchType Type { get; }

            public NamedRegex(MatchType type, string pattern, RegexOptions options) : base(pattern, options)
            {
                Type = type;
            }
        }

        int cursorLocation = 0;
        string line = null;

        private readonly List<NamedRegex> PossibleMatches;

        public LineParser()
        {
            PossibleMatches = new List<NamedRegex>()
            {
                //matches the   [langen] command
                new NamedRegex(MatchType.langen,           @"\Glangen\s*", RegexOptions.IgnoreCase),
                //matches       [ : '] with some optional space on either side
                new NamedRegex(MatchType.colon,            @"\G\s*:\s*", RegexOptions.IgnoreCase),
                //matches       [dwave 0, hid_1e139]
                new NamedRegex(MatchType.dwaveAlias,       @"\Gdwave\s+\d+\s*,\s*\w+", RegexOptions.IgnoreCase),
                //matches       [dwave 0, "filepath\goes\here"]
                new NamedRegex(MatchType.dwavePath,        @"\Gdwave\s+\d+\s*,\s*""[^""]+?""", RegexOptions.IgnoreCase),
                //matches       [dwave 0, hid_1e139]
                new NamedRegex(MatchType.voiceDelayOrWait, @"\G((voicedelay)|(voicewait)) \d+", RegexOptions.IgnoreCase),
                //matches text, [^  And this isn't some small amount we're talkin's about.^@]
                new NamedRegex(MatchType.text,             @"\G\^.*?(@|\\(\x10)?\s*|(/\s*$)|$)", RegexOptions.IgnoreCase),
                //matches the text-enders (sometimes there are text enders without a text start
                new NamedRegex(MatchType.endOfLine,        @"\G(@|\\)", RegexOptions.IgnoreCase),
                //matches !sd or !s0,!s100 etc.
                new NamedRegex(MatchType.textSpeed,        @"\G!s(d|(\d+))", RegexOptions.IgnoreCase),
                //matches !w100 or !d1000
                new NamedRegex(MatchType.waitOrDelay,      @"\G((!w)|(!d))\d+", RegexOptions.IgnoreCase),
                //matches a / at the end of a line (which disables the newline)
                new NamedRegex(MatchType.disableNewLine,   @"\G/\s*$", RegexOptions.IgnoreCase),
                //matches a color change command (6 digit hex, starting with #)
                new NamedRegex(MatchType.changeColor,      @"\G#[0-9abcdef]{6}\s*", RegexOptions.IgnoreCase),
                //parse a comment. Might need to remove this later if it incorrctly passes comment, and do as pre-processing step.
                new NamedRegex(MatchType.comment,          @"\G;.*$", RegexOptions.IgnoreCase),
                //ignore whitespace
                new NamedRegex(MatchType.whitespace,       @"\G\s*", RegexOptions.IgnoreCase),
            };
        }

        public void LoadLine(string _line)
        {
            line = _line;
            cursorLocation = 0;
        }

        public IEnumerable<NamedMatch> iter()
        {
            PhraseCharacterCounter characterCounter = new PhraseCharacterCounter();

            while (cursorLocation < line.Length)
            {
                //iterate through all possible regex matches
                bool atLeastOneMatch = false;
                foreach (NamedRegex r in PossibleMatches)
                {
                    Match match = r.Match(line, cursorLocation);
                    if (match.Success)
                    {
                        yield return new NamedMatch(r.Type, match);

                        //advance the cursor to the end of the match if match
                        cursorLocation = match.Index + match.Length;
                        //Console.WriteLine($"Matched ({r.Type}): {match.Groups[0]} newloc:{cursorLocation}");
                        atLeastOneMatch = true;
                        break;
                    }
                }

                if (!atLeastOneMatch)
                {
                    throw new Exception($"Could not parse line {line}");
                }
            }
        }
    }

}
