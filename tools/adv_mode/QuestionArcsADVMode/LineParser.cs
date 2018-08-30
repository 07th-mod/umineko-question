using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    //parases a line, tries to detects phrases and insert line counts
    public class LineParser
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

        public class NamedRegex : Regex
        {
            public MatchType Type { get; }

            public NamedRegex(MatchType type, string pattern, RegexOptions options) : base(pattern, options)
            {
                Type = type;
            }
        }

        private int cursorLocation = 0;
        private string line = null;

        private readonly List<NamedRegex> PossibleMatches;

        public LineParser()
        {
            //NOTE: use https://regex101.com/ for testing/debugging these regexes (or rewrite using another type of pattern matching library)
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
                new NamedRegex(MatchType.text,             @"(\^.*?)(@|\\\x10?\s*|\/\s*$|$)", RegexOptions.IgnoreCase), //@"\G\^.*?(@|\\(\x10)?\s*|(/\s*$)|$)", RegexOptions.IgnoreCase),
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

        public IEnumerable<Token> Iter()
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
                        //Convert match objects to Token classes
                        switch (r.Type)
                        {
                            case MatchType.text:
                                yield return new TextToken(match.Groups[1].Value);
                                if (match.Groups[1].Value.Trim().Length != 0)
                                {
                                    yield return new GenericToken(match.Groups[2].Value);
                                }

                                break;

                            default:
                                yield return new GenericToken(match.Value);
                                break;
                        }

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
