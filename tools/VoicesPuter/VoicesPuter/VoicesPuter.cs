using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VoicesPuter
{
    /// <summary>
    /// 
    /// </summary>
    public class VoicesPuter
    {
        #region Members
        #region JAPANESE_LINE_IDENTIFIER
        /// <summary>
        /// 
        /// </summary>
        private const string JAPANESE_LINE_IDENTIFIER = "langjp";
        #endregion

        #region ENGLISH_LINE_IDENTIFIER
        /// <summary>
        /// 
        /// </summary>
        private const string ENGLISH_LINE_IDENTIFIER = "langen";
        #endregion

        #region VOICE_SCRIPT_FUNCTION_NAME_ORG
        /// <summary>
        /// 
        /// </summary>
        public const string VOICE_SCRIPT_FUNCTION_NAME_ORG = "dwave";
        #endregion

        #region VOICE_SCRIPT_FUNCTION_NAME_JP
        /// <summary>
        /// 
        /// </summary>
        public const string VOICE_SCRIPT_FUNCTION_NAME_JP = VOICE_SCRIPT_FUNCTION_NAME_ORG + "_jp";
        #endregion

        #region VOICE_SCRIPT_FUNCTION_NAME_ENG
        /// <summary>
        /// 
        /// </summary>
        public const string VOICE_SCRIPT_FUNCTION_NAME_ENG = VOICE_SCRIPT_FUNCTION_NAME_ORG + "_eng";
        #endregion

        #region BEGINNING_OF_VOICE_SCRIPT
        /// <summary>
        /// 
        /// </summary>
        private const string BEGINNING_OF_VOICE_SCRIPT = ":" + VOICE_SCRIPT_FUNCTION_NAME_ORG;
        #endregion

        #region REGEX_OF_VOICE_SCRIPT
        /// <summary>
        /// 
        /// </summary>
        private const string REGEX_OF_VOICE_SCRIPT = BEGINNING_OF_VOICE_SCRIPT + @"[^:]*:";
        #endregion

        #region REGEX_OF_VOICE_SCRIPT
        /// <summary>
        /// 
        /// </summary>
        private enum TypeOfLanguage
        {
            /// <summary>
            /// 
            /// </summary>
            ENGLISH,

            /// <summary>
            /// 
            /// </summary>
            JAPANESE,

            /// <summary>
            /// Representlines that exist between langjp and langen.
            /// </summary>
            OTHER_STATEMENTS
        }
        #endregion

        #region CHANGED_SCRIPT_OUTPUT_DIRECTORY_NAME
        /// <summary>
        /// 
        /// </summary>
        private const string LOG_DIRECTORY_NAME = "Log";
        #endregion

        #region logger
        /// <summary>
        /// 
        /// </summary>
        private Logger logger;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameScriptPath"></param>
        public VoicesPuter(string gameScriptPath)
        {
            string logFilePath = Path.Combine(new string[] { Path.GetDirectoryName(gameScriptPath), LOG_DIRECTORY_NAME, "log.txt", });
            logger = new LoggerConfiguration().WriteTo.File(logFilePath).CreateLogger();
        }
        #endregion

        #region Methods
        #region PutVoiceScriptsIntoLines
        /// <summary>
        /// 
        /// </summary>
        /// <param name="allOfLines"></param>
        /// <returns></returns>
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
                            string insertedVoiceScriptsJapaneseLine = GetInsertedVoiceScroptsFromEnglishIntoJapanese(tempEnglishLines[japaneseLinesIndex], tempJapaneseLines[japaneseLinesIndex]);
                            convertedVoiceScriptsToJapanese.Add(ChangeToVoiceScriptFunctionNameOfJapan(insertedVoiceScriptsJapaneseLine));
                            convertedVoiceScriptsToEnglish.Add(ChangeToVoiceScriptFunctionNameOfEnglish(tempEnglishLines[japaneseLinesIndex]));
                        }
                        List<string> orderedLines = GetOrderedLines(orderOfAddedTypeOfLanguage, convertedVoiceScriptsToEnglish, convertedVoiceScriptsToJapanese, tempOtherStatementLines, false);
                        newAllOfLines.AddRange(orderedLines);
                        shouldClearRetainedLines = true;
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

        #region GetInsertedVoiceScroptsFromEnglishIntoJapanese
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderOfAddedTypeOfLanguage"></param>
        /// <param name="tempEnglishLines"></param>
        /// <param name="tempJapaneseLines"></param>
        /// <param name="tempOtherStatementLines"></param>
        /// <returns></returns>
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

        #region GetInsertedVoiceScroptsFromEnglishIntoJapanese
        /// <summary>
        /// 
        /// </summary>
        /// <param name="englishLine"></param>
        /// <param name="japaneseLine"></param>
        /// <returns></returns>
        private string GetInsertedVoiceScroptsFromEnglishIntoJapanese(string englishLine, string japaneseLine)
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
        /// 
        /// </summary>
        /// <param name="sentence"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="japaneseLine"></param>
        /// <returns></returns>
        private string ChangeToVoiceScriptFunctionNameOfJapan(string japaneseLine)
        {
            return japaneseLine.Replace(VOICE_SCRIPT_FUNCTION_NAME_ORG, VOICE_SCRIPT_FUNCTION_NAME_JP);
        }
        #endregion

        #region ChangeVoiceScriptFunctionNameToEnglish
        /// <summary>
        /// 
        /// </summary>
        /// <param name="englishLine"></param>
        /// <returns></returns>
        private string ChangeToVoiceScriptFunctionNameOfEnglish(string englishLine)
        {
            return englishLine.Replace(VOICE_SCRIPT_FUNCTION_NAME_ORG, VOICE_SCRIPT_FUNCTION_NAME_ENG);
        }
        #endregion
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class UnmatchedNewLinesWithOriginalLinesException : InvalidOperationException
    {
        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public UnmatchedNewLinesWithOriginalLinesException(string message) : base(message) { }
        #endregion
    }
}
