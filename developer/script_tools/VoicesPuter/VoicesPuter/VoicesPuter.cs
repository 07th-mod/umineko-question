using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace VoicesPuter
{
    /// <summary>
    /// Put voice scripts into langjp using langen.
    /// Then change script's function name of each language.
    /// </summary>
    public class VoicesPuter
    {
        private readonly Regex COMMENT_OR_BLANK_LINE = new Regex(@"^\s*(;.*)?$");
        private readonly Regex langjpRegex = new Regex(@"(.*langjp)(.*)");
        private readonly VoicesDatabase voicesDatabase;

        #region Members
        #region JAPANESE_LINE_IDENTIFIER
        /// <summary>
        /// Represent identifier of line of Japanese.
        /// </summary>
        private const string JAPANESE_LINE_IDENTIFIER = "langjp";
        #endregion

        #region ENGLISH_LINE_IDENTIFIER
        /// <summary>
        /// Represent identifier of line of English.
        /// </summary>
        private const string ENGLISH_LINE_IDENTIFIER = "langen";
        #endregion

        #region VOICE_SCRIPT_FUNCTION_NAME_ORG
        /// <summary>
        /// Represent voice script's function name.
        /// </summary>
        public const string VOICE_SCRIPT_FUNCTION_NAME_ORG = "dwave";
        #endregion

        #region VOICE_SCRIPT_FUNCTION_NAME_JP
        /// <summary>
        /// Represent voice script's function name of Japanese.
        /// </summary>
        public const string VOICE_SCRIPT_FUNCTION_NAME_JP = VOICE_SCRIPT_FUNCTION_NAME_ORG + "_jp";
        #endregion

        #region VOICE_SCRIPT_FUNCTION_NAME_ENG
        /// <summary>
        /// Represent voice script's function name of English.
        /// </summary>
        public const string VOICE_SCRIPT_FUNCTION_NAME_ENG = VOICE_SCRIPT_FUNCTION_NAME_ORG + "_eng";
        #endregion

        #region BEGINNING_OF_VOICE_SCRIPT
        /// <summary>
        /// Represent beginning of voice script.
        /// </summary>
        private const string BEGINNING_OF_VOICE_SCRIPT = ":" + VOICE_SCRIPT_FUNCTION_NAME_ORG;
        #endregion

        #region REGEX_OF_VOICE_SCRIPT
        /// <summary>
        /// Represent regular expression of voice script.
        /// </summary>
        private const string REGEX_OF_VOICE_SCRIPT = BEGINNING_OF_VOICE_SCRIPT + @"[^:]*:";
        #endregion

        #region TypeOfLanguage
        /// <summary>
        /// Represent whether which language.
        /// </summary>
        private enum TypeOfLanguage
        {
            /// <summary>
            /// Represent English.
            /// </summary>
            ENGLISH,

            /// <summary>
            /// Represent Japanese.
            /// </summary>
            JAPANESE,

            /// <summary>
            /// Represent lines that exist between langjp and langen.
            /// </summary>
            OTHER_STATEMENTS
        }
        #endregion

        #region LOG_DIRECTORY_NAME
        /// <summary>
        /// Path that output the log.
        /// </summary>
        private const string LOG_DIRECTORY_NAME = "Log";
        #endregion

        #region logger
        /// <summary>
        /// This logger outputs into the folder that a user specified the game script.
        /// </summary>
        private Logger logger;
        private Logger warningLogger;
        private int errorCount;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Initialize the logger.
        /// </summary>
        /// <param name="gameScriptPath">Path of the game script.</param>
        public VoicesPuter(string gameScriptPath, bool overwrite, VoicesDatabase voicesDatabase)
        {
            this.voicesDatabase = voicesDatabase;

            string errorLogFilePath = Path.Combine(Path.GetDirectoryName(gameScriptPath), LOG_DIRECTORY_NAME, $"errlog {DateTime.Now.ToString(@"yyyy MM dd yyyy h mm ss tt")}.utf");
            string warningFilePath = Path.Combine(Path.GetDirectoryName(gameScriptPath), LOG_DIRECTORY_NAME, $"warnlog {DateTime.Now.ToString(@"yyyy MM dd yyyy h mm ss tt")}.utf");

            if (overwrite)
            {
                errorLogFilePath = Path.Combine(Path.GetDirectoryName(gameScriptPath), LOG_DIRECTORY_NAME, $"errlog.utf");
                warningFilePath = Path.Combine(Path.GetDirectoryName(gameScriptPath), LOG_DIRECTORY_NAME, $"warnlog.utf");
                Utils.DeleteIfExists(errorLogFilePath);
                Utils.DeleteIfExists(warningFilePath);
            }

            string outputTemplate = ">>> [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            logger        = new LoggerConfiguration().WriteTo.File(errorLogFilePath, outputTemplate: outputTemplate).CreateLogger();
            warningLogger = new LoggerConfiguration().WriteTo.File(warningFilePath,  outputTemplate: outputTemplate).CreateLogger();

            errorCount = 0;
        }
        #endregion

        #region Methods

        public string GetScriptSample(List<string> allLines, int regionCenter, int numContext)
        {
            int regionStart = Math.Max(0, regionCenter - numContext/2);
            int regionEnd = Math.Min(allLines.Count, regionCenter + numContext / 2);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<Script Sample>");

            for (int i = regionStart; i < regionEnd; i++)
            {
                sb.AppendLine("    " + allLines[i]);
            }
            sb.AppendLine("<End Script Sample>");

            return sb.ToString();
        }

        public void LogFormattedError(List<string> allLines, int index, string errorInformation)
        {
            string scriptSample = GetScriptSample(allLines, index, 10);
            /*if(scriptSample.Contains("TODO"))
            {
                logger.Error(">>>>> NOTE: Error collapsed as marked for manual fix.");
                return;
            }*/

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n-----------------------------------");
            sb.AppendLine(errorInformation);
            sb.Append(scriptSample);
            sb.AppendLine("-----------------------------------");

            logger.Error(sb.ToString());
        }

        public void HandleErrorLangenWithoutLangjp(List<string> allLines, int currentLineIndex)
        {
            string currentLine = allLines[currentLineIndex];
            string warningOfLangenWithoutLangjp = "This langen starts without langjp.\n";
            warningOfLangenWithoutLangjp += $"{currentLine}\n";
            if (LineOrLinesHaveDwave(currentLine))
            {
                LogFormattedError(allLines, currentLineIndex, warningOfLangenWithoutLangjp);
                errorCount++;
            }
            else
            {
                warningLogger.Error(warningOfLangenWithoutLangjp);
            }
        }

        public void HandleErrorUnmatchedCountOfLangjpAndLangen(List<string> allLines, int currentLineIndex, List<string> tempEnglishLines, List<string> tempJapaneseLines)
        {
            string warningOfUnmatchedCountOfLangjpAndLangen = String.Empty;

            warningOfUnmatchedCountOfLangjpAndLangen += $"Number of Langen lines {tempEnglishLines.Count} and Langjp lines {tempJapaneseLines.Count} not equal\n";
            //Tell user if we think english line was reflowed
            if (CheckEnglishLinesHadReflow(tempEnglishLines, tempJapaneseLines))
            {
                warningOfUnmatchedCountOfLangjpAndLangen += $"NOTE: It appears english line was reflowed.\n";
            }

            foreach (string langjp in tempJapaneseLines)
            {
                warningOfUnmatchedCountOfLangjpAndLangen += $"{langjp}\n";
            }
            foreach (string langen in tempEnglishLines)
            {
                warningOfUnmatchedCountOfLangjpAndLangen += $"{langen}\n";
            }

            if (LineOrLinesHaveDwave(tempEnglishLines))
            {
                LogFormattedError(allLines, currentLineIndex, warningOfUnmatchedCountOfLangjpAndLangen);
                errorCount++;
            }
            else
            {
                //we don't care about english lines with no dwave. Just log it for now.
                warningLogger.Information(warningOfUnmatchedCountOfLangjpAndLangen);
            }
        }

        //This is called from GetInsertedVoiceScriptsFromEnglishIntoJapanese()
        public void HandleErrorUnmatchedCountOfLangjpAndLangenAtSign(List<string> allLines, int currentLineIndex, string englishLine, string japaneseLine, string[] splitEnglishLine, string[] splitJapaneseLine, string fixedJapaneseLine)
        {
            bool ErrorWasNotFixed = fixedJapaneseLine == null;

            StringBuilder sb = new StringBuilder();
            string autoFixComment = ErrorWasNotFixed ? "Could not auto-fix line" : "Will try to auto-fix";

            if (splitJapaneseLine.Length == splitEnglishLine.Length + 1)
            {
                sb.AppendLine("NOTE: Probably english line is missing a voice");
            }
            sb.AppendLine($"Num '@' doesn't match. Num Sections EN: {splitEnglishLine.Length} JP: {splitJapaneseLine.Length}. {autoFixComment}");
            sb.AppendLine(japaneseLine);
            sb.AppendLine(englishLine);
            if(ErrorWasNotFixed)
            {
                if ((splitJapaneseLine.Length - 1 == splitEnglishLine.Length) && splitJapaneseLine[splitJapaneseLine.Length - 1].Length < 4)
                {
                    sb.Append("NOTE: Probably @ at end of line has been replaced with / or \\...");
                }
                LogFormattedError(allLines, currentLineIndex, sb.ToString());
                errorCount++;
            }
            else
            {
                sb.AppendLine($"fixed_line: {fixedJapaneseLine}");
                warningLogger.Warning(sb.ToString());
            }
        }

        #region PutVoiceScriptsIntoLines
        /// <summary>
        /// Put voice scripts into lines that includes langen or lanjp while changing properly voice script's function name.
        /// </summary>
        /// <param name="allOfLines">Lines that is used by the game.</param>
        /// <returns>Lines that put voice scripts into langjp and changed properly voice script's function name of each language.</returns>
        public List<string> PutVoiceScriptsIntoLines(List<string> allOfLines, VoicesDatabase voicesDatabase)
        {
            List<string> newAllOfLines = new List<string>();

            // Make new all of lines from original all of lines while putting voice scripts into Japanese line and changing to voice script's function name of English and Japan.
            List<TypeOfLanguage> orderOfAddedTypeOfLanguage = new List<TypeOfLanguage>();
            List<string> tempJapaneseLines = new List<string>();
            List<string> tempEnglishLines = new List<string>();
            List<string> tempOtherStatementLines = new List<string>();
            int lineIndex = -1; //need to count lineIndex this way because sometimes the for loop is continue'd
            foreach (string currentLine in allOfLines)
            {
                lineIndex++;
                // In the first place, search Japanese line.
                // Then if a Japanese line is found, retain the Japanese line until disappearing it.
                // retain also the English line like a way of retained Japanese lines.
                // If there are lines that exist between Japanese lines and English ones, retain lines as other statement, then it would add between Japanese and English ones.
                // If counts of both lines are not mutch, don't put voice scripts into Japanese line and just add Japanese lines to new all of lines and add English line to it after changing to voice script's function name of English.
                // If it is not able to search Japanese lines and it found English lines, add English line after changing to voice script's function name of English.
                // fix matching some lines which are entirely comments.
                if (!COMMENT_OR_BLANK_LINE.IsMatch(currentLine) && currentLine.Contains(ENGLISH_LINE_IDENTIFIER))
                {
                    if (tempJapaneseLines.Count <= 0)
                    {
                        newAllOfLines.Add(ChangeToVoiceScriptFunctionNameOfEnglish(currentLine));

                        HandleErrorLangenWithoutLangjp(allOfLines, lineIndex);
                        continue;
                    }
                    else
                    {
                        tempEnglishLines.Add(currentLine);
                        orderOfAddedTypeOfLanguage.Add(TypeOfLanguage.ENGLISH);
                        continue;
                    }
                }
                else if (tempJapaneseLines.Count > 0)
                {
                    bool shouldClearRetainedLines = false;
                    if (tempEnglishLines.Count <= 0)
                    {
                        if (!currentLine.Contains(JAPANESE_LINE_IDENTIFIER))
                        {
                            tempOtherStatementLines.Add(currentLine);
                            orderOfAddedTypeOfLanguage.Add(TypeOfLanguage.OTHER_STATEMENTS);
                            continue;
                        }
                    }
                    else if (tempJapaneseLines.Count == tempEnglishLines.Count)
                    {
                        // Put voice scripts into Japanese line and change to voice script's function name of English and Japan.
                        List<string> convertedVoiceScriptsToJapanese = new List<string>();
                        List<string> convertedVoiceScriptsToEnglish = new List<string>();
                        for (int japaneseLinesIndex = 0; japaneseLinesIndex < tempJapaneseLines.Count; japaneseLinesIndex++)
                        {
                            string insertedVoiceScriptsJapaneseLine = GetInsertedVoiceScriptsFromEnglishIntoJapanese(allOfLines, lineIndex, tempEnglishLines[japaneseLinesIndex], tempJapaneseLines[japaneseLinesIndex], voicesDatabase);
                            convertedVoiceScriptsToJapanese.Add(ChangeToVoiceScriptFunctionNameOfJapan(insertedVoiceScriptsJapaneseLine));
                            convertedVoiceScriptsToEnglish.Add(ChangeToVoiceScriptFunctionNameOfEnglish(tempEnglishLines[japaneseLinesIndex]));
                        }
                        List<string> orderedLines = GetOrderedLines(orderOfAddedTypeOfLanguage, convertedVoiceScriptsToEnglish, convertedVoiceScriptsToJapanese, tempOtherStatementLines, false);
                        newAllOfLines.AddRange(orderedLines);
                        shouldClearRetainedLines = true;
                    }
                    else if (COMMENT_OR_BLANK_LINE.IsMatch(currentLine)) //comment or whitespace line - don't end current section
                    {
                        tempOtherStatementLines.Add(currentLine);
                        orderOfAddedTypeOfLanguage.Add(TypeOfLanguage.OTHER_STATEMENTS);
                        continue;
                    }
                    else
                    {
                        List<string> orderedLines = GetOrderedLines(orderOfAddedTypeOfLanguage, tempEnglishLines, tempJapaneseLines, tempOtherStatementLines);
                        newAllOfLines.AddRange(orderedLines);
                        shouldClearRetainedLines = true;

                        HandleErrorUnmatchedCountOfLangjpAndLangen(allOfLines, lineIndex, tempEnglishLines, tempJapaneseLines);
                    }

                    // If each line are added, clear them.
                    if (shouldClearRetainedLines)
                    {
                        orderOfAddedTypeOfLanguage.Clear();
                        tempJapaneseLines.Clear();
                        tempEnglishLines.Clear();
                        tempOtherStatementLines.Clear();
                    }
                }

                // Japanese line is added here because chunk of Japanese and English line should be added one by one.
                if (!COMMENT_OR_BLANK_LINE.IsMatch(currentLine) && currentLine.Contains(JAPANESE_LINE_IDENTIFIER))
                {
                    tempJapaneseLines.Add(currentLine);
                    orderOfAddedTypeOfLanguage.Add(TypeOfLanguage.JAPANESE);
                    continue;
                }

                // Put line that is not English line and Japanese line.
                newAllOfLines.Add(currentLine);
            }

            // If counts of new all lines and original ones don't match, notice error.
            if (newAllOfLines.Count != allOfLines.Count)
            {
                throw new UnmatchedNewLinesWithOriginalLinesException("Unmatch changed count of new all of lines and count of original ones.");
            }
            logger.Information($"Number of Errors: {errorCount}");

            return newAllOfLines;
        }
        #endregion

        //check whether the english lines has one or more dwave on it
        private bool LineOrLinesHaveDwave(List<string> englishLines)
        {
            foreach (string englishLine in englishLines)
            {
                List<string> dwave_commands = GetVoiceScripts(englishLine);
                if (dwave_commands.Count > 0)
                    return true;
            }

            return false;
        }

        //overload for a single string
        private bool LineOrLinesHaveDwave(string englishLine)
        {
            return LineOrLinesHaveDwave(new List<string> { englishLine });
        }

        private bool CheckEnglishLinesHadReflow(List<string> englishLines, List<string> japaneseLines)
        {
            //count the number of @s total across english/japanese lines. If equal, english line probably was split as it was too long.
            int enCount = 0;
            foreach(string line in englishLines)
            {
                enCount += line.Count(f => f == '@');
            }

            int jpCount= 0;
            foreach (string line in japaneseLines)
            {
                jpCount += line.Count(f => f == '@');
            }

            return enCount != 0 && enCount == jpCount;
        }



        #region GetOrderedLines
        /// <summary>
        /// Return ordered list of string as is specified with list of TypeOfLanguage.
        /// </summary>
        /// <param name="orderOfAddedTypeOfLanguage">Represent order of lines.</param>
        /// <param name="tempEnglishLines">List of English line.</param>
        /// <param name="tempJapaneseLines">List of Japanese line.</param>
        /// <param name="tempOtherStatementLines">List of other line.</param>
        /// <returns>Ordered list of string as is specified with list of TypeOfLanguage.</returns>
        private List<string> GetOrderedLines(List<TypeOfLanguage> orderOfAddedTypeOfLanguage, List<string> tempEnglishLines, List<string> tempJapaneseLines, List<string> tempOtherStatementLines, bool shouldChangeToVoiceScriptFunctionNameOfEnglish = true)
        {
            List<string> orderedLines = new List<string>();
            int countOfEnglish = 0;
            int countOfJapanese = 0;
            int countOfOtherStatement = 0;
            foreach (TypeOfLanguage typeOfLanguage in orderOfAddedTypeOfLanguage)
            {
                switch (typeOfLanguage)
                {
                    case TypeOfLanguage.ENGLISH:
                        if (shouldChangeToVoiceScriptFunctionNameOfEnglish)
                        {
                            orderedLines.Add(ChangeToVoiceScriptFunctionNameOfEnglish(tempEnglishLines[countOfEnglish]));
                        }
                        else
                        {
                            orderedLines.Add(tempEnglishLines[countOfEnglish]);
                        }
                        countOfEnglish++;
                        break;

                    case TypeOfLanguage.JAPANESE:
                        orderedLines.Add(tempJapaneseLines[countOfJapanese]);
                        countOfJapanese++;
                        break;

                    case TypeOfLanguage.OTHER_STATEMENTS:
                        orderedLines.Add(tempOtherStatementLines[countOfOtherStatement]);
                        countOfOtherStatement++;
                        break;
                }
            }
            return orderedLines;
        }
        #endregion


        private List<string> SplitAndKeepSpliton(string input, string splitOn)
        {
            string[] splitString = input.Split(separator: new string[] { splitOn }, options: new StringSplitOptions());

            for(int i = 0; i < splitString.Length-1; i++)
            {
                splitString[i] = splitString[i] + splitOn;
            }
            return splitString.ToList();
        }

        private List<string> SplitJapaneseLineOnInsertionPoints(string s)
        {
            List<string> splitString = new List<string>();
            string working_string = s;

            //split after langjp
            List<string> splitLangJP = SplitAndKeepSpliton(working_string, "langjp");
            if(splitLangJP.Count == 2)
            {
                splitString.Add(splitLangJP[0]);
                working_string = splitLangJP[1];
            }
            else if(splitLangJP.Count > 2) //there shouldn't be more than one lanjp on each line
            {
                throw new Exception();
            }

            //split just after each @ symbol, EXCEPT if there is an @ on the last 3 characters of the line, ignore it
            List<string> splitATSymbol = SplitAndKeepSpliton(working_string, "@");

            //check for @ at end of line. If it exists, merge the last two entries
            if(splitATSymbol.Last().TrimEnd().Length < 3)
            {
                splitATSymbol[splitATSymbol.Count - 2] += splitATSymbol.Last();
                splitATSymbol.RemoveAt(splitATSymbol.Count - 1);
            }

            //add the rest of the parts to the splitstring
            splitString.AddRange(splitATSymbol);

            return splitString;
        }

        #region GetInsertedVoiceScriptsFromEnglishIntoJapanese
        /// <summary>
        /// Return line that put voice scripts from English line into Japanese line.
        /// </summary>
        /// <param name="englishLine">English line that includes voice scripts.</param>
        /// <param name="japaneseLine">Japanese line.</param>
        /// <returns>Line that put voice scripts from English line into Japanese line.</returns>
        private string GetInsertedVoiceScriptsFromEnglishIntoJapanese(List<string> allLines, int currentLineIndex, string englishLine, string japaneseLine, VoicesDatabase voicesDatabase)
        {
            string insertedJapaneseLine = string.Empty;

            // Get voice scripts with regix.
            List<string> voiceScripts = GetVoiceScripts(englishLine);
            if (voiceScripts.Count > 0)
            {
                string[] splitEnglishLine = englishLine.Split('@');
                string[] splitJapaneseLine = japaneseLine.Split('@');

                // If English line and Japanese one's structure is not same, just return not changing Japanese line.
                if ((splitEnglishLine.Length != splitJapaneseLine.Length))
                {
                    List<string> customSplit = SplitJapaneseLineOnInsertionPoints(japaneseLine);

                    //note:returned dwaves are just 'dwave 0, thing' not 'dwave_jp 0, thing'
                    List<string> fixedDwaves = new List<string>();
                    DwaveDatabase.AutoFixResult fixResult;
                    try
                    {
                        fixResult = voicesDatabase.DwaveDatabase.FixMissingDwaves(voiceScripts, customSplit.Count - 1, out fixedDwaves);
                    }
                    catch(DwaveArgument.LastCharacterOfDwaveNotDigitException e)
                    {
                        logger.Error("An error occured while trying to fix the below:\n" + e.ToString());
                        fixResult = DwaveDatabase.AutoFixResult.Failure;
                    }
                    catch(DwaveDatabase.PrefixOfDwaveArgNotTheSame e)
                    {
                        logger.Error("An error occured while trying to fix the below:\n" + e.ToString());
                        fixResult = DwaveDatabase.AutoFixResult.Failure;
                    }

                    switch (fixResult)
                    {
                        //if fix was successful, return the new japanese line
                        case DwaveDatabase.AutoFixResult.OK: //don't count error, but log
                            StringBuilder sb = new StringBuilder();
                            int i = 0;
                            for (; i < fixedDwaves.Count; i++)
                            {
                                sb.Append(customSplit[i]);
                                sb.Append(fixedDwaves[i]);
                            }
                            sb.Append(customSplit[i]);

                            string fixedJapaneseLine = sb.ToString();

                            HandleErrorUnmatchedCountOfLangjpAndLangenAtSign(allLines, currentLineIndex, englishLine, japaneseLine, splitEnglishLine, splitJapaneseLine, fixedJapaneseLine);
                            Console.WriteLine($"Auto-fixed line: ");
                            Console.WriteLine($"   (EN): {englishLine}");
                            Console.WriteLine($"   (JP): {japaneseLine}");
                            Console.WriteLine($"(JPFIX): {fixedJapaneseLine}\n");
                            return fixedJapaneseLine;

                        //if fix was unsuccessful, return the original japanese line
                        case DwaveDatabase.AutoFixResult.NeedsManualCheck: //don't apply fix, print, count as error, and log (return original japanese line)
                        case DwaveDatabase.AutoFixResult.Failure: //don't apply fix, count as error, and log (return original japanese line)
                            HandleErrorUnmatchedCountOfLangjpAndLangenAtSign(allLines, currentLineIndex, englishLine, japaneseLine, splitEnglishLine, splitJapaneseLine, null);
                            return japaneseLine;
                    }
                }

                // If a split english string has voice script, append voice script to a split japanese string.
                int countOfVoiceScript = 0;
                for (int englishLineIndex = 0; englishLineIndex < splitEnglishLine.Length; englishLineIndex++)
                {
                    if (englishLineIndex > 0 && !string.IsNullOrEmpty(splitJapaneseLine[englishLineIndex])) insertedJapaneseLine += "@";
                    if (splitEnglishLine[englishLineIndex].Contains(BEGINNING_OF_VOICE_SCRIPT))
                    {
                        if (splitJapaneseLine[englishLineIndex].StartsWith(JAPANESE_LINE_IDENTIFIER))
                        {
                            insertedJapaneseLine = splitJapaneseLine[englishLineIndex].Replace(JAPANESE_LINE_IDENTIFIER, $"{JAPANESE_LINE_IDENTIFIER}{voiceScripts[countOfVoiceScript]}");
                            countOfVoiceScript++;
                        }
                        else
                        {
                            insertedJapaneseLine += $"{voiceScripts[countOfVoiceScript]}{splitJapaneseLine[englishLineIndex]}";
                            countOfVoiceScript++;
                        }
                    }
                    else
                    {
                        insertedJapaneseLine += splitJapaneseLine[englishLineIndex];
                    }
                }

                // If count of voice scripts and count of used it don't match, log about it.
                if (countOfVoiceScript != voiceScripts.Count)
                {
                    // TODO Have to log about count of voice scripts and count of used it don't match, log about it.
                }

                // If japanese line has at sign in the end of line, add at sign because of split line with at sign.
                Regex hasAtSignInEndOfLineRegex = new Regex(@"[\w\W]*@$");
                if (hasAtSignInEndOfLineRegex.IsMatch(japaneseLine)) insertedJapaneseLine += "@";
            }

            if (string.IsNullOrEmpty(insertedJapaneseLine))
            {
                return japaneseLine;
            }
            else
            {
                return insertedJapaneseLine;
            }
        }
        #endregion

        #region GetVoiceScripts
        /// <summary>
        /// Return list of voice scripts from sentence.
        /// </summary>
        /// <param name="sentence">Sentence that includes voice scripts.</param>
        /// <returns>List of voice scripts from sentence.</returns>
        private List<string> GetVoiceScripts(string sentence)
        {
            Regex voicesRegex = new Regex(REGEX_OF_VOICE_SCRIPT);
            MatchCollection voies = voicesRegex.Matches(sentence);
            List<string> voiceScripts = new List<string>();
            foreach(Match voice in voies)
            {
                voiceScripts.Add(voice.Value);
            }
            return voiceScripts;
        }
        #endregion

        #region ChangeVoiceScriptFunctionNameToJapanese
        /// <summary>
        /// Return a line that changed voice script's function name to Japanese.
        /// </summary>
        /// <param name="japaneseLine">Japanese line that includes voice script's function name.</param>
        /// <returns>A line that changed voice script's function name to Japanese.</returns>
        private string ChangeToVoiceScriptFunctionNameOfJapan(string japaneseLine)
        {
            return japaneseLine.Replace(VOICE_SCRIPT_FUNCTION_NAME_ORG, VOICE_SCRIPT_FUNCTION_NAME_JP);
        }
        #endregion

        #region ChangeVoiceScriptFunctionNameToEnglish
        /// <summary>
        /// Return a line that changed voice script's function name to English.
        /// </summary>
        /// <param name="englishLine">English line that includes voice script's function name.</param>
        /// <returns>A line that changed voice script's function name to English.</returns>
        private string ChangeToVoiceScriptFunctionNameOfEnglish(string englishLine)
        {
            return englishLine.Replace(VOICE_SCRIPT_FUNCTION_NAME_ORG, VOICE_SCRIPT_FUNCTION_NAME_ENG);
        }
        #endregion
        #endregion
    }

    /// <summary>
    /// This exception occures when count of original lines and changed lines unmatch.
    /// </summary>
    public class UnmatchedNewLinesWithOriginalLinesException : InvalidOperationException
    {
        #region Constructors
        /// <summary>
        /// Set the error message.
        /// </summary>
        /// <param name="message">Error message.</param>
        public UnmatchedNewLinesWithOriginalLinesException(string message) : base(message) { }
        #endregion
    }
}
