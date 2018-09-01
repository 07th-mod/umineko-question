using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QuestionArcsADVMode
{
    internal class CharacterCountInserter
    {
        public static readonly Regex multipleColonsRegex = new Regex(@":+");

        public bool gotPageWaitBeforeLastText;

        public CharacterCountInserter()
        {
        }

        //given the list of tokens in a line, returns whether the line would emit a new line in the game engine
        public bool LineHasNewLine(List<Token> tokens)
        {
            bool currentLineHasNewLine = true;
            foreach (Token t in tokens)
            {
                switch (t)
                {
                    case DisableNewLine disableNewLineToken:
                    case PageWait pageWaitToken:
                        currentLineHasNewLine = false;
                        break;

                    case null:
                        throw new ArgumentNullException();
                }
            }
            return currentLineHasNewLine;
        }

        public static void MarkCharacterCountOnClickOrPageWaits(List<Token> allTokens)
        {
            WaitToken tokenToBeMarked = null;

            //iterate through the script stream, and sum the counts of all the text between click or page waits
            //upon reaching the next click or page wait, save the sum into current wait, and reassign the current wait
            foreach(Token currentToken in allTokens)
            {
                switch (currentToken)
                {
                    case TextToken textToken:
                        if(tokenToBeMarked != null)
                        {
                            tokenToBeMarked.textAfterClick += textToken.count;
                        }
                        break;

                    case PageWait pageWaitToken:
                        tokenToBeMarked = pageWaitToken;
                        break;

                    case ClickWait clickWaitToken:
                        tokenToBeMarked = clickWaitToken;
                        break;
                }

            }
        }

        //given the list of tokens in a line, marks the last clickwait in the line
        public static void MarkLastClickWait(List<Token> tokensOnSingleLine)
        {
            ClickWait previousCW = null;

            foreach (Token t in tokensOnSingleLine)
            {
                switch (t)
                {
                    case ClickWait cw:
                        cw.isLastClickWaitOnLine = false;
                        previousCW = cw;
                        break;
                }
            }

            if (previousCW != null)
            {
                previousCW.isLastClickWaitOnLine = true;
            }
        }

        //This function takes as input the last two tokens, and outputs a string
        //The returned string is intended to be used to make up the new, modified line
        private string HandleToken(Token token, Token lastToken, bool currentLineHasNewLine)
        {
            switch (token)
            {
                case TextToken textToken:
                    int count = PhraseCharacterCounter.GetCharacterCount(token.RawString);
                    Debug.Print($"Phrase [{token.RawString}] is {count} chars long");
                    //only add a colon if the last token wasn't a colon (should make this work more generically later)

                    //if there was a page wait, indicate to script a page wait occured.
                    string pageWaitIndicator = gotPageWaitBeforeLastText ? "char_count_clear:" : String.Empty;
                    gotPageWaitBeforeLastText = false;

                    string colonAtStartOfLine = lastToken is ColonToken ? string.Empty : ":";

                    return $"{colonAtStartOfLine}{pageWaitIndicator}char_count {count}:{token.RawString}";
                    break;

                case GenericToken genericToken:
                    Debug.Print($"Got generic token {token.RawString}");
                    return token.RawString;
                    break;

                case ColonToken colonToken:
                    return token.RawString;
                    break;

                case PageWait pageWaitToken:
                    gotPageWaitBeforeLastText = true;
                    return "/\nadv_page_wait:"; //token.RawString + Debug.OnDebug("PWAIT");
                    break;

                case ClickWait clickWaitToken:
                    return $"/\nadv_click_wait {(clickWaitToken.isLastClickWaitOnLine.Value && currentLineHasNewLine ? 1 : 0)}:";//clickWaitToken.RawString + Debug.OnDebug("CWAIT");
                    break;

                case NewLineToken newLineToken:
                    //for now, newlines are handled as special cases, so return empty string
                    return String.Empty;
                    break;

                case DisableNewLine disableNewLineToken:
                    return disableNewLineToken.RawString + Debug.OnDebug("DISNL");
                    break;

                default:
                    throw new NotImplementedException();

                case null:
                    throw new ArgumentNullException();
            }
        }
    }

    //TODO: need to fix all text by moving spaces at the start of a text line to the end of the previous text line!
    internal class Program
    {
        private static void Main(string[] args)
        {
            string input_script = @"C:\drojf\large_projects\umineko\umineko_question_repo\InDevelopment\ManualUpdates\0.utf";
            //string output_script = @"C:\drojf\large_projects\umineko\umineko_question_repo\InDevelopment\ManualUpdates\0_new.utf";
            string output_script = @"C:\games\Steam\steamapps\common\Umineko_latest_patch\0.u";
            using (System.IO.StreamReader file = new System.IO.StreamReader(input_script, Encoding.UTF8))
            using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(output_script, append: false, encoding: Encoding.UTF8))
            {
                string line;

                CharacterCountInserter characterCountInserter = new CharacterCountInserter();

                List<Token> allTokens = new List<Token>();
                List<List<Token>> allTokensByLine = new List<List<Token>>();

                int line_count = 0;
                while ((line = file.ReadLine()) != null)
                {
                    List<Token> tokensOnLine = LineParser.GetAllTokens(line);
                    allTokens.AddRange(tokensOnLine);
                    allTokensByLine.Add(tokensOnLine);
                    line_count++;
                }


                //preprocess by line
                foreach(List<Token> oneLinesTokens in allTokensByLine)
                {
                    CharacterCountInserter.MarkLastClickWait(oneLinesTokens);
                }

                //preprocess by line, reverse order to set the amount of text each clickwait
                CharacterCountInserter.MarkCharacterCountOnClickOrPageWaits(allTokens);

                //write out all tokens
                foreach(Token t in allTokens)
                {
                    outputFile.Write(t.ToString());
                }
            }

        }

    }

}


