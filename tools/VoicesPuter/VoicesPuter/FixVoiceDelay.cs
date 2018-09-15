using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoicesPuter
{

    class FixVoiceDelay
    {
        enum LineType
        {
            CommentOrBlank,
            Japanese,
            English,
            Other,
        }

        class LineMetaInfo
        {
            private static readonly Regex COMMENT_OR_BLANK_LINE = new Regex(@"^\s*(;.*)?$");
            private static readonly Regex LANGJP = new Regex("langjp", RegexOptions.IgnoreCase);
            private static readonly Regex LANGEN = new Regex("langen", RegexOptions.IgnoreCase);

            public readonly LineType lineType;
            public readonly int lineIndex;
            public readonly string rawValue;

            public LineMetaInfo(string line, int lineIndex, LineType lineType)
            {
                this.rawValue = line;
                this.lineIndex = lineIndex;
                this.lineType = lineType;
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

                    returnedMetaInfo.Add(new LineMetaInfo(line, i, lineType));
                }

                return returnedMetaInfo;
            }

            public override string ToString()
            {
                return $"{lineIndex} : {lineType} : {rawValue}";
            }

        }

        class ScriptTextChunk
        {
            List<LineMetaInfo> japaneseMetas;
            List<LineMetaInfo> englishMetas;

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

            /// <summary>
            /// call to create chunks from the game script
            /// </summary>
            /// <param name="allLinesMetaInfo"></param>
            public static List<ScriptTextChunk> GetChunks(List<LineMetaInfo> allLinesMetaInfo)
            {
                ScriptTextChunk currentChunk = new ScriptTextChunk();

                List<ScriptTextChunk> allScriptChunks = new List<ScriptTextChunk>();

                //List<LineMetaInfo> japaneseInCurrentChunk = new List<LineMetaInfo>();
                //List<LineMetaInfo> englishInCurrentChunk = new List<LineMetaInfo>();

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
                sb.AppendLine("Japanese Lines:");
                foreach(LineMetaInfo japaneseMetaInfo in japaneseMetas)
                {
                    sb.AppendLine(japaneseMetaInfo.ToString());
                }

                sb.AppendLine("English Lines:");
                foreach (LineMetaInfo englishMetaInfo in englishMetas)
                {
                    sb.AppendLine(englishMetaInfo.ToString());
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Call to copy the 'voicedelay' and 'voicewait' commands from the english lines into the japanese lines.
        /// </summary>
        /// <param name="allScriptLines"></param>
        public static void FixVoiceDelaysInScript(List<string> allScriptLines)
        {
            //for each line, generate a linemetainfo object.
            List<LineMetaInfo> allLinesMetaInfo = LineMetaInfo.GetLineMetaInfoFromRawLines(allScriptLines);

            //feed into chunker to chunk together japanese and english lines
            List<ScriptTextChunk> allChunks = ScriptTextChunk.GetChunks(allLinesMetaInfo);
        }
    }
}
