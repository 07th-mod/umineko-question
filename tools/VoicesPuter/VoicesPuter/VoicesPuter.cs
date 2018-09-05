using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VoicesPuter
{
    /// <summary>
    /// Put voice scripts into langjp using langen.
    /// Then change script's function name of each language.
    /// </summary>
    public class VoicesPuter
    {
        private readonly Regex COMMENT_OR_BLANK_LINE = new Regex(@"^\s*(;.*)?$");

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
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Initialize the logger.
        /// </summary>
        /// <param name="gameScriptPath">Path of the game script.</param>
        public VoicesPuter(string gameScriptPath)
        {
            string logFilePath = Path.Combine(new string[] { Path.GetDirectoryName(gameScriptPath), LOG_DIRECTORY_NAME, "log.txt", });
            logger = new LoggerConfiguration().WriteTo.File(logFilePath).CreateLogger();
        }
        #endregion

        #region Methods
        #region PutVoiceScriptsIntoLines
        /// <summary>
        /// Put voice scripts into lines that includes langen or lanjp while changing properly voice script's function name.
        /// </summary>
        /// <param name="allOfLines">Lines that is used by the game.</param>
        /// <returns>Lines that put voice scripts into langjp and changed properly voice script's function name of each language.</returns>
        public List<string> PutVoiceScriptsIntoLines(List<string> allOfLines)
        {
            List<string> newAllOfLines = new List<string>();

            // Make new all of lines from original all of lines while putting voice scripts into Japanese line and changing to voice script's function name of English and Japan.
            List<TypeOfLanguage> orderOfAddedTypeOfLanguage = new List<TypeOfLanguage>();
            List<string> tempJapaneseLines = new List<string>();
            List<string> tempEnglishLines = new List<string>();
            List<string> tempOtherStatementLines = new List<string>();
            foreach (string currentLine in allOfLines)
            {
                // In the first place, search Japanese line.
                // Then if a Japanese line is found, retain the Japanese line until disappearing it.
                // retain also the English line like a way of retained Japanese lines.
                // If there are lines that exist between Japanese lines and English ones, retain lines as other statement, then it would add between Japanese and English ones.
                // If counts of both lines are not mutch, don't put voice scripts into Japanese line and just add Japanese lines to new all of lines and add English line to it after changing to voice script's function name of English.
                // If it is not able to search Japanese lines and it found English lines, add English line after changing to voice script's function name of English.
                if (currentLine.Contains(ENGLISH_LINE_IDENTIFIER))
                {
                    if (tempJapaneseLines.Count <= 0)
                    {
                        newAllOfLines.Add(ChangeToVoiceScriptFunctionNameOfEnglish(currentLine));

                        string warningOfLangenWithoutLangjp = "This langen starts without langjp.\n";
                        warningOfLangenWithoutLangjp += $"{currentLine}\n";
                        logger.Warning(warningOfLangenWithoutLangjp);
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
                            string insertedVoiceScriptsJapaneseLine = GetInsertedVoiceScriptsFromEnglishIntoJapanese(tempEnglishLines[japaneseLinesIndex], tempJapaneseLines[japaneseLinesIndex]);
                            convertedVoiceScriptsToJapanese.Add(ChangeToVoiceScriptFunctionNameOfJapan(insertedVoiceScriptsJapaneseLine));
                            convertedVoiceScriptsToEnglish.Add(ChangeToVoiceScriptFunctionNameOfEnglish(tempEnglishLines[japaneseLinesIndex]));
                        }
                        List<string> orderedLines = GetOrderedLines(orderOfAddedTypeOfLanguage, convertedVoiceScriptsToEnglish, convertedVoiceScriptsToJapanese, tempOtherStatementLines, false);
                        newAllOfLines.AddRange(orderedLines);
                        shouldClearRetainedLines = true;
                    }
                    else if(COMMENT_OR_BLANK_LINE.IsMatch(currentLine)) //comment or whitespace line - don't end current section
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

                        string warningOfUnmatchedCountOfLangjpAndLangen = "Count of langjp and langen is not same.\n";
                        foreach (string langjp in tempJapaneseLines)
                        {
                            warningOfUnmatchedCountOfLangjpAndLangen += $"{langjp}\n";
                        }
                        foreach (string langen in tempEnglishLines)
                        {
                            warningOfUnmatchedCountOfLangjpAndLangen += $"{langen}\n";
                        }
                        logger.Warning(warningOfUnmatchedCountOfLangjpAndLangen);
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
                if (currentLine.Contains(JAPANESE_LINE_IDENTIFIER))
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
            return newAllOfLines;
        }
        #endregion

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

        #region GetInsertedVoiceScriptsFromEnglishIntoJapanese
        /// <summary>
        /// Return line that put voice scripts from English line into Japanese line.
        /// </summary>
        /// <param name="englishLine">English line that includes voice scripts.</param>
        /// <param name="japaneseLine">Japanese line.</param>
        /// <returns>Line that put voice scripts from English line into Japanese line.</returns>
        private string GetInsertedVoiceScriptsFromEnglishIntoJapanese(string englishLine, string japaneseLine)
        {
            string insertedJapaneseLine = string.Empty;

            // Get voice scripts with regix.
            List<string> voiceScripts = GetVoiceScripts(englishLine);
            if (voiceScripts.Count > 0)
            {
                string[] splitEnglishLine = englishLine.Split('@');
                string[] splitJapaneseLine = japaneseLine.Split('@');

                // If English line and Japanese one's structure is not same, just return not changing Japanese line.
                if (splitEnglishLine.Length != splitJapaneseLine.Length)
                {
                    string warningOfUnmatchedCountOfLangjpAndLangenAtSign = "Count of langen and langjp's '@' is not same.\n";
                    warningOfUnmatchedCountOfLangjpAndLangenAtSign += $"{englishLine}\n";
                    warningOfUnmatchedCountOfLangjpAndLangenAtSign += $"{japaneseLine}\n";
                    logger.Warning(warningOfUnmatchedCountOfLangjpAndLangenAtSign);
                    return japaneseLine;
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
