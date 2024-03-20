using System;
using System.Collections.Generic;
using System.IO;
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

        //given the list of tokens in a line, marks the last clickwait in the line, UNLESS there is a pagewait or disable newline after last clickwait
        //in which case none of the items get marked as the 'last' clickwait
        //in cases like: ^this is a test @^sdrfkasdfjasd;lfkjas;fld @\n
        //however cases like ^this is a test @^sdrfkasdfjasd;lfkjas;fld\n  should not emit a new line, since the last text will emit a new line
        public static void MarkClickWaitHasNewlineAfterIt(List<Token> tokensOnSingleLine)
        {
            //get position of last clickwait, while marking all tokens as NO NEW LINE
            ClickWait lastClickWaitToken = null;
            int previousCWPosition = Int32.MinValue;

            for (int i = 0; i < tokensOnSingleLine.Count; i++)
            {
                if(tokensOnSingleLine[i].RawString.Contains("old physician let out a"))
                {
                    Console.WriteLine();
                }
                switch (tokensOnSingleLine[i])
                {
                    case ClickWait cw:
                        cw.isLastClickWaitOnLine = false;
                        previousCWPosition = i;
                        lastClickWaitToken = cw;
                        break;
                }
            }

            //finish here if there were no clickwaits on the line
            if (lastClickWaitToken == null)
                return;

            //Mark a newline after the last clickwait token, if a newline exists
            //Note that the tokenizer will eliminate any whitespace before the last newline, so we don't have to worry about that
            try
            {
                if (tokensOnSingleLine[previousCWPosition + 1] is NewLineToken)
                {
                    lastClickWaitToken.isLastClickWaitOnLine = true;
                }
            }
            catch(IndexOutOfRangeException)
            {
                throw new Exception($"Clickwait was last token on the line {tokensOnSingleLine} (the last token should always be a newline)");
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
        static Regex setwindowRegex = new Regex(@"^\s*setwindow");

        private static void oldMain(string[] args)
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
                foreach (List<Token> oneLinesTokens in allTokensByLine)
                {
                    CharacterCountInserter.MarkClickWaitHasNewlineAfterIt(oneLinesTokens);
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

        private static bool lineShouldBeModified(string line)
        {
            if(!setwindowRegex.IsMatch(line)) {
                return false;
            }

            string trimmedLine = line.Trim();

            //skip commented lines
            if (trimmedLine[0] == ';')
            {
                return false;
            }

            //manual marker indicating the line shouldn't be changed
            if (line.Contains("utmeta-nomodify"))
            {
                return false;
            }

            return true;
        }

        private static void replaceSetwindowCommands(string input_script)
        {
            HashSet<string> uniqueLines = new HashSet<string>();

            string[] lines = File.ReadAllLines(input_script);
            List<string> outputLines = new List<string>(200000);
            foreach (string line in lines)
            {
                if (!setwindowRegex.IsMatch(line))
                {
                    continue;
                }

                uniqueLines.Add(line.Trim());


                /*                string newLine = line;

                                if(lineShouldBeModified(line))
                                {
                                    newLine = "advsetwindow_initial";
                                }*/

                //outputLines.Add(newLine);
            }

            Console.WriteLine("\nUnique Lines: \n\n");
            foreach (string line in uniqueLines)
            {
                Console.WriteLine(line);
            }

            //File.WriteAllLines(output_script, outputLines);

            Console.ReadKey();
        }

        /// <summary>
        /// </summary>
        /// <param name="allLines"></param>
        /// <returns></returns>
        private static List<string> fixCharacterChangeOnLinesWithNoText(string[] allLines)
        {
            Regex blankDialogueLineRegex = new Regex(@"^\s*((langen)|(langjp))(!s((d)|(\d*)))*[\^@\\/ \t]*$");
            string advcharSearchString = @"advchar ""-1""";

            List<string> outputLines = new List<string>(200000);

            bool holdingADVChar = false;

            int numMatches = 0;
            for (int i = 0; i < allLines.Count(); i++)
            {
                string line = allLines[i];

                bool currentLineIsADVChar = line.Trim() == advcharSearchString;
                bool nextLineIsBlankDialogue = (i+1) < allLines.Count() ? blankDialogueLineRegex.IsMatch(allLines[i + 1]) : false;
                

                if(holdingADVChar)
                {
                    //State 2: waiting to release the held ADVchar command

                    //Always output the current line in this mode.
                    outputLines.Add(line);

                    //if the next line is no longer a blank dialogue, it is time to output the held advchar and revert to State 1
                    if (!nextLineIsBlankDialogue)
                    {
                        outputLines.Add(advcharSearchString);
                        holdingADVChar = false;
                    }
                }
                else
                {
                    //State 1: waiting to get a line with advchar "-1" while the next line is a blank dialogue
                    if(currentLineIsADVChar && nextLineIsBlankDialogue)
                    {
                        //if you get a match hold the advchar "-1" command
                        holdingADVChar = true;
                    }
                    else
                    {
                        //if you don't get a match, just output the line
                        outputLines.Add(line);
                    }
                }


                //for debugging only
                if(nextLineIsBlankDialogue)
                {
                    Console.WriteLine(line);
                    numMatches++;
                }
            }

            Console.WriteLine($"Got {numMatches} matches.");
            return outputLines;
        }

        private static void Main(string[] args)
        {
            string input_script = @"C:\drojf\large_projects\umineko\umineko-question\InDevelopment\ManualUpdates\0.utf";
            //string input_script = @"C:\drojf\large_projects\umineko\umineko-question\dev\umineko_fullvoice.txt";
            string output_script = @"C:\drojf\large_projects\umineko\umineko-question\InDevelopment\ManualUpdates\0_output.utf";

            TextboxNameInserter inserter = new TextboxNameInserter();
            inserter.ReadVoiceToNameIDFile(@"C:\drojf\Dropbox\current programming\umineko_port_umitweak_to_mg\ADV_Mode\name_list.txt");
            string[] allLines = File.ReadAllLines(input_script);


            List<string> outputLines = fixCharacterChangeOnLinesWithNoText(allLines);


            ////Uncomment to insert textboxes
            //inserter.ReadVoiceAliases(allLines);
            //List<string> outputLines = inserter.InsertIntoScript(allLines);



            File.WriteAllLines(output_script, outputLines);
            Console.WriteLine("\n\nProgram Finished");
            Console.ReadKey();
            return;
        }
    }

}


