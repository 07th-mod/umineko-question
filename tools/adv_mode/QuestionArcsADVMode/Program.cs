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
        public static readonly Regex whitespaceFrontRegex = new Regex(@"^(\s*)(.*?$)");

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

        //the game script sometimes has spaces at the start of a text phrase (^   this is an example)
        //If we do a pagebreak before this text phrase, it can cause misaligned text like:
        //
        //This is line one
        //   then line two
        //  then line three
        //
        //store leading spaces in clickwait type command
        //NOTE: pagewaits don't need to store leading spaces, since spaces would look dumb after a pagewait
        //NOTE: this function also inserts DLE everywhere. should refactor so that the ^ and DLE insertion is done somewhere else.
        public static void StoreLeadingSpacesInClickwaitAndInsertDLE(List<Token> allTokens)
        {
            string lastTextTokensWhitespace = String.Empty;

            for (int i = allTokens.Count - 1; i >= 0; i--)
            {
                Token tokenAnyType = allTokens[i];

                switch(tokenAnyType)
                {
                    case TextToken textToken:
                        //this regex should always match.
                        Match match = whitespaceFrontRegex.Match(textToken.GetTextWithoutHats());

                        //Update the token's text by removing the front whitespace
                        lastTextTokensWhitespace = match.Groups[1].Value;
                        textToken.RawString = $"^\x10{match.Groups[2].Value}^"; //Note: this might result in double DLE sometimes TODO: remove double DLE
                        break;

                    case ClickWait clickWait:
                        clickWait.leadingWhiteSpace = lastTextTokensWhitespace;
                        lastTextTokensWhitespace = String.Empty;
                        break;
                }
            }
        }
    }

    //TODO: exclude regions near start of script and end of script where text shouldn't be modified
    //TODO: instead of using above space reordering, add another command/argument which says "emit 2 spaces" at the correct locations in the script
    //then the script can decide whether to emit the spaces or not. Could just be a string argument containing the number of spaces (that is definitely easiest).
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

                CharacterCountInserter.StoreLeadingSpacesInClickwaitAndInsertDLE(allTokens);

                //write out all tokens
                foreach (Token t in allTokens)
                {
                    outputFile.Write(t.ToString());
                }
            }

        }

    }

}


