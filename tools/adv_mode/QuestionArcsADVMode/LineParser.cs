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
        public static readonly Regex langEnAtStartOfLine = new Regex(@"^\s*langen", RegexOptions.IgnoreCase);

        //NOTE: use https://regex101.com/ for testing/debugging these regexes (or rewrite using another type of pattern matching library)
        private static readonly List<NamedRegex> PossibleMatches = new List<NamedRegex>()
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
            new NamedRegex(MatchType.text,             @"(\^.*?)(@|\\\x10?\s*|\/|$)", RegexOptions.IgnoreCase), //@"\G\^.*?(@|\\(\x10)?\s*|(/\s*$)|$)", RegexOptions.IgnoreCase),
            //matches the pagewait symbol "\" (sometimes there are text enders without a text start
            new NamedRegex(MatchType.pageWait,         @"\G\\", RegexOptions.IgnoreCase),
            //matches the clickwait symbol "@"
            new NamedRegex(MatchType.clickWait,        @"\G@", RegexOptions.IgnoreCase),
            //matches !sd or !s0,!s100 etc.
            new NamedRegex(MatchType.textSpeed,        @"\G!s(d|(\d+))", RegexOptions.IgnoreCase),
            //matches !w100 or !d1000
            new NamedRegex(MatchType.waitOrDelay,      @"\G((!w)|(!d))\d+", RegexOptions.IgnoreCase),
            //matches a / at the end of a line (which disables the newline)
            new NamedRegex(MatchType.disableNewLine,   @"\G/\s*$", RegexOptions.IgnoreCase),
            //matches a color change command (6 digit hex, starting with #)
            new NamedRegex(MatchType.changeColor,      @"\G#[0-9abcdef]{6}\s*", RegexOptions.IgnoreCase),
            //semicolon comment at end of line. The game script actually allows semicolons inside text, so can't pre-filter for comments
            new NamedRegex(MatchType.comment, @"\G\s*;.*$", RegexOptions.IgnoreCase),
            //whitespace at end of line without comment
            new NamedRegex(MatchType.comment, @"\G[\s\x10]+$", RegexOptions.IgnoreCase),
        };

        public enum MatchType
        {
            langen,
            colon,
            dwaveAlias,
            dwavePath,
            voiceDelayOrWait,
            text,
            pageWait,
            clickWait,
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

        public LineParser()
        {

        }

        public void LoadLine(string _line)
        {
            line = _line;
            cursorLocation = 0;
        }

        public IEnumerable<Token> Iter()
        {
            while (cursorLocation < line.Length)
            {
                foreach (Token t in MatchTokens(line, ref cursorLocation))
                {
                    yield return t;
                }
            }

            yield return new NewLineToken("\r\n");
        }

        public static List<Token> GetAllTokens(string line)
        {
            List<Token> tokenList = new List<Token>();

            //special case for non Langen lines:
            if (!langEnAtStartOfLine.IsMatch(line))
            {
                tokenList.Add(new GenericToken(line));
            }
            else
            {
                int cursorLocation = 0;

                while (cursorLocation < line.Length)
                {
                    tokenList.AddRange(MatchTokens(line, ref cursorLocation));
                }
            }

            tokenList.Add(new NewLineToken("\r\n"));

            return tokenList;
        }

        //returns one or more tokens
        public static List<Token> MatchTokens(string s, ref int cursorLocation)
        {
            List<Token> tokens = new List<Token>();

            //iterate through all possible regex matches
            foreach (NamedRegex r in PossibleMatches)
            {
                Match match = r.Match(s, cursorLocation);
                if (match.Success)
                {
                    //Convert match objects to Token classes
                    switch (r.Type)
                    {
                        case MatchType.text:
                            tokens.Add(new TextToken(match.Groups[1].Value));

                            //the end of a text can be @, / or \, so recursively call this function on the text-end to get the token
                            if (match.Groups[2].Value.Trim().Length != 0)
                            {
                                int tempCursorLocation = 0;
                                tokens.AddRange(MatchTokens(match.Groups[2].Value, ref tempCursorLocation));
                            }

                            break;

                        case MatchType.colon:
                            tokens.Add(new ColonToken(match.Value));
                            break;

                        case MatchType.clickWait:
                            tokens.Add(new ClickWait(match.Value));
                            break;

                        case MatchType.pageWait:
                            tokens.Add(new PageWait(match.Value));
                            break;

                        case MatchType.disableNewLine:
                            tokens.Add(new DisableNewLine(match.Value));
                            break;

                        default:
                            tokens.Add(new GenericToken(match.Value));
                            break;
                    }

                    //advance the cursor to the end of the match if match
                    cursorLocation = match.Index + match.Length;
                    //Console.WriteLine($"Matched ({r.Type}): {match.Groups[0]} newloc:{cursorLocation}");
                    return tokens;
                }
            }

            throw new Exception($"Could not parse line {s}");
        }

    }

}
