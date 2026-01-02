using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoicesPuter
{
    #region FixVoiceDelayClass
    class FixVoiceDelay
    {
        private static readonly Regex VOICE_WAIT_REGEX = new Regex(@"voicedelay\s*\d+", RegexOptions.IgnoreCase);
        private static readonly Regex OLD_DELAY_REGEX = new Regex(@"!d\d+", RegexOptions.IgnoreCase);
        private static readonly Regex LANGJP_REGEX = new Regex(@"langjp", RegexOptions.IgnoreCase);
        private static readonly Regex MULTI_COLON_REGEX = new Regex(@":+");
        enum LineType
        {
            CommentOrBlank,
            Japanese,
            English,
            Other,
        }

        #region LineMetaInfoSubClass
        class LineMetaInfo
        {
            private static readonly Regex COMMENT_OR_BLANK_LINE = new Regex(@"^\s*(;.*)?$");
            private static readonly Regex LANGJP = new Regex("langjp", RegexOptions.IgnoreCase);
            private static readonly Regex LANGEN = new Regex("langen", RegexOptions.IgnoreCase);

            public readonly LineType lineType;
            public readonly int lineIndex;
            private readonly List<string> allScriptLines;

            public LineMetaInfo(List<string> allScriptLines, int lineIndex, LineType lineType)
            {
                this.lineIndex = lineIndex;
                this.lineType = lineType;
                this.allScriptLines = allScriptLines;
            }

            public string Get()
            {
                return allScriptLines[lineIndex];
            }

            public void Set(string s)
            {
                allScriptLines[lineIndex] = s;
            }

            /// <summary>
            /// Call to create line meta info objects from raw script lines
            /// </summary>
            /// <param name="allScriptLines"></param>
            /// <returns></returns>
            public static List<LineMetaInfo> GetLineMetaInfoFromRawLines(List<string> allScriptLines)
            {
                List<LineMetaInfo> returnedMetaInfo = new List<LineMetaInfo>();

                for (int i = 0; i < allScriptLines.Count; i++)
                {
                    string line = allScriptLines[i];

                    LineType lineType;

                    if (COMMENT_OR_BLANK_LINE.IsMatch(line))
                    {
                        lineType = LineType.CommentOrBlank;
                    }
                    else if (LANGJP.IsMatch(line))
                    {
                        lineType = LineType.Japanese;
                    }
                    else if (LANGEN.IsMatch(line))
                    {
                        lineType = LineType.English;
                    }
                    else
                    {
                        lineType = LineType.Other;
                    }

                    returnedMetaInfo.Add(new LineMetaInfo(allScriptLines, i, lineType));
                }

                return returnedMetaInfo;
            }

            public override string ToString()
            {
                return $"{lineIndex} : {lineType} : {Get()}";
            }

        }
        #endregion LineMetaInfoSubClass

        #region ScriptTextChunkSubClass
        class ScriptTextChunk
        {
            public List<LineMetaInfo> japaneseMetas;
            public List<LineMetaInfo> englishMetas;

            ScriptTextChunk()
            {
                this.japaneseMetas = new List<LineMetaInfo>();
                this.englishMetas = new List<LineMetaInfo>();
            }

            void AddJapaneseMeta(LineMetaInfo metaInfo)
            {
                japaneseMetas.Add(metaInfo);
            }

            void AddEnglishMeta(LineMetaInfo metaInfo)
            {
                englishMetas.Add(metaInfo);
            }

            bool Empty()
            {
                return (japaneseMetas.Count + englishMetas.Count) == 0;
            }

            public bool NumJapaneseAndEnglishLinesEqual()
            {
                return japaneseMetas.Count == englishMetas.Count;
            }

            /// <summary>
            /// call to create chunks from the game script
            /// </summary>
            /// <param name="allLinesMetaInfo"></param>
            public static List<ScriptTextChunk> GetChunks(List<LineMetaInfo> allLinesMetaInfo)
            {
                ScriptTextChunk currentChunk = new ScriptTextChunk();

                List<ScriptTextChunk> allScriptChunks = new List<ScriptTextChunk>();

                foreach(LineMetaInfo metaInfo in allLinesMetaInfo)
                {
                    switch(metaInfo.lineType)
                    {
                        case LineType.Japanese:
                            currentChunk.AddJapaneseMeta(metaInfo);
                            break;

                        case LineType.English:
                            currentChunk.AddEnglishMeta(metaInfo);
                            break;

                        case LineType.Other:
                            if (!currentChunk.Empty())
                            {
                                //if see any other type of line, end the chunk
                                allScriptChunks.Add(currentChunk);
                                currentChunk = new ScriptTextChunk();
                            }
                            break;

                        case LineType.CommentOrBlank:
                            //ignore comment or blank lines
                            break;

                        default:
                            throw new Exception("Unknown metaInfo type in GetChunks() of ScriptTextChunker");
                    }
                }

                return allScriptChunks;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach(LineMetaInfo japaneseMetaInfo in japaneseMetas)
                {
                    sb.AppendLine(japaneseMetaInfo.ToString());
                }
                foreach (LineMetaInfo englishMetaInfo in englishMetas)
                {
                    sb.AppendLine(englishMetaInfo.ToString());
                }

                return sb.ToString();
            }
        }
        #endregion ScriptTextChunkSubClass

        #region FixVoiceDelayMethods
        private static List<string> GenerateMatchesFromMetaInfos(List<LineMetaInfo> metaInfos, Regex regexToMatch, out bool gotAtLeastOneMatch)
        {
            List<string> outStrings = new List<string>();
            gotAtLeastOneMatch = false;

            foreach (LineMetaInfo lineMeta in metaInfos)
            {
                Match matchResult = regexToMatch.Match(lineMeta.Get());

                if (matchResult.Success)
                {
                    gotAtLeastOneMatch = true;
                    outStrings.Add(matchResult.Value);
                }
                else
                {
                    outStrings.Add(null);
                }
            }

            return outStrings;
        }

        /// <summary>
        /// Call to copy the 'voicedelay' and 'voicewait' commands from the english lines into the japanese lines.
        /// NOTE: there are only 13 voicewait comamnds in the script, and they are outside the langen/langjp markers, so they will occur for both languages automatically.
        /// </summary>
        /// <param name="allScriptLines"></param>
        public static void FixVoiceDelaysInScript(List<string> allScriptLines, Logger logger, bool logNoOldDelay, bool logSuccessfulInsertions)
        {
            //for each line, generate a linemetainfo object.
            List<LineMetaInfo> allLinesMetaInfo = LineMetaInfo.GetLineMetaInfoFromRawLines(allScriptLines);

            //feed into chunker to chunk together japanese and english lines
            List<ScriptTextChunk> allChunks = ScriptTextChunk.GetChunks(allLinesMetaInfo);

            foreach(ScriptTextChunk chunk in allChunks)
            {
                List<string> voiceDelayStrings = GenerateMatchesFromMetaInfos(chunk.englishMetas, VOICE_WAIT_REGEX, out bool gotAtLeastOneVoiceDelay);

                //exit here if no voice delays were found
                if (!gotAtLeastOneVoiceDelay)
                {
                    continue;
                }

                //verify number of japanese and english lines are the same.
                if (!chunk.NumJapaneseAndEnglishLinesEqual())
                {
                    logger.Error($"Chunk has unequal lines. Tried to fix anyway\n{chunk.ToString()}\n");
                }

                //identify any delay types (!d100, !w100, delay, wait, etc) already existing on japanese lines. If more than one on a line, log error (or just error out)
                List<string> oldDelayStrings = GenerateMatchesFromMetaInfos(chunk.japaneseMetas, OLD_DELAY_REGEX, out bool gotAtLeastOneOldDelayRegex);

                //check that the japanese line's delays match the english delays
                bool japaneseAndEnglishDontMatch = false;
                for(int i = 0; (i < voiceDelayStrings.Count) && (i < oldDelayStrings.Count); i++)
                {
                    bool voiceDelayNull = voiceDelayStrings[i] == null;
                    bool oldDelayNull = oldDelayStrings[i] == null;

                    if ( voiceDelayNull && !oldDelayNull || !voiceDelayNull && oldDelayNull)
                    {
                        japaneseAndEnglishDontMatch = true;
                        break;
                    }
                }

                if(japaneseAndEnglishDontMatch && logNoOldDelay)
                {
                    logger.Warning($"WARNING: Japanese !d dont match English voiceDelay\n");
                }

                //remove the existing delays (replace?) on correspondiing japanese lines, and replace with english delays.
                for(int i = 0; (i < chunk.englishMetas.Count) && (i < chunk.japaneseMetas.Count); i++)
                {
                    //skip lines without voicedelay on them
                    if (voiceDelayStrings[i] == null)
                        continue;

                    //get the corresponding japanese line metainfo
                    LineMetaInfo japaneseMetaInfo = chunk.japaneseMetas[i];

                    string workingString = japaneseMetaInfo.Get();

                    //remove any !d100 etc. on the line, if it exists
                    workingString = OLD_DELAY_REGEX.Replace(workingString, "");
                    //replace 'langjp' with 'langjp:voicedelay [delayAmount]:' exactly once
                    workingString = LANGJP_REGEX.Replace(workingString, $"langjp:{voiceDelayStrings[i]}:", count:1);
                    //collapse multiple colons ':::' on the line
                    workingString = MULTI_COLON_REGEX.Replace(workingString, ":");

                    //replace the string in the 'allLines' list
                    japaneseMetaInfo.Set(workingString);
                }

                if (logSuccessfulInsertions)
                {
                    logger.Information($"Succesfully converted chunk:\n{chunk.ToString()}\n");
                }
            }
        }
        #endregion FixVoiceDelayMethods

    }
    #endregion FixVoiceDelayClass
}
