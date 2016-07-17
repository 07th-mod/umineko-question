using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UminekoVoices
{
    static class UminekoVoiceParser
    {
        static String oldScriptPath = "..\\..\\Input\\umineko_fullvoice.txt"; //original script
        static String newScriptPath = "..\\..\\Input\\0.utf"; //base script from steam version
        static String aliasesPath = "..\\..\\Input\\straliases.txt"; //aliases section from original script
        static String mergedScriptPath = "..\\..\\Output\\0.utf"; //final output, steam script with voice tags inserted
        static String tagsMapping = "..\\..\\Output\\umineko_voice_tags.utf"; //file containing mapping of japanese text to voice lines
        static String readingErrorsPath = "..\\..\\Output\\reading_errors.utf"; //errors encountered while parsing original script
        static String writingErrorsPath = "..\\..\\Output\\writing_errors.utf"; //errors encountered while modifying steam script

        static void Main(string[] args)
        {
            Dictionary<String, List<voiceLine>> voiceDictionary = buildVoiceDictionary();

            outputVoiceMapping(voiceDictionary);

            insertVoiceTags(voiceDictionary);

        }

        public static Dictionary<String, List<voiceLine>> buildVoiceDictionary()
        {
            Dictionary<String, List<voiceLine>> voiceDictionary = new Dictionary<String, List<voiceLine>>();

            int lineNumber = 0;
            ArrayList JapaneseSentences = new ArrayList();
            String lastLineType = "";
            int sentenceIndex = 0;
            String line;

            // Read the file and display it line by line.
            System.IO.StreamReader oldScript =
                new System.IO.StreamReader(oldScriptPath);

            System.IO.StreamWriter errorFile =
                new System.IO.StreamWriter(readingErrorsPath, false, Encoding.UTF8);

            while ((line = oldScript.ReadLine()) != null)
            {
                try
                {
                    if (isOldJapaneseTextLine(line))
                    {
                        if (lastLineType != "japanesetext")
                            JapaneseSentences.Clear();

                        String formattedLine = line.Substring(1, line.Length - 2); //remove the leading ; and the trailing \
                        formattedLine = formattedLine.Trim('@'); //remove any leading or trailing @s

                        String[] sentences = formattedLine.Split('@');
                        foreach (String s in sentences)
                        {
                            if (s.Equals("!sd")) //!sd is some kind of non-text directive, it never maps to a voiced line
                                continue;

                            JapaneseSentences.Add(s); //maintain a list of all recent japanese sentences, maintains sentences from multiple lines
                        }
                        lastLineType = "japanesetext";
                    }
                    else if (isOldEnglishVoiceLine(line))
                    {
                        if (lastLineType != "englishvoice")
                            sentenceIndex = 0;

                        Boolean ignoreFirstItem = true;
                        String formattedLine = line.Trim('@');

                        string[] stringSeparators = new string[] { "dwave" };
                        String[] sentences = formattedLine.Split(stringSeparators, StringSplitOptions.None);

                        foreach (String englishSentence in sentences)
                        {
                            if (ignoreFirstItem) //any text before the first dwave is irrelevant
                            {
                                ignoreFirstItem = false;
                                continue;
                            }
                            if (englishSentence[1] != '0') //this code only handles dwave 0.  Other audio channels are rare and can be manually fixed
                            {
                                errorFile.WriteLine("DWAVE NOT 0: " + line);
                                break;
                            }
                            else
                            {
                                int tagEnding = englishSentence.IndexOf(':');
                                if (tagEnding == -1 || englishSentence.IndexOf('`') < tagEnding)
                                    tagEnding = englishSentence.IndexOf('`'); //dwave command will end on a : or ` so use whichever is smaller

                                String voiceTag = englishSentence.Substring(4, tagEnding - 4);
                                String japaneseSentence = (String)JapaneseSentences[sentenceIndex];  //this is the nth english sentence, so get the nth japanese sentence
                                sentenceIndex++;

                                if (voiceTag.Length > 30)
                                {
                                    errorFile.WriteLine("WEIRDLY LONG LINE: " + line); //can happen if dwave is not terminated correctly
                                    break;
                                }

                                List<voiceLine> tagList;
                                if (voiceDictionary.TryGetValue(japaneseSentence, out tagList))
                                {
                                    //japanese line is in dictionary, so add this voicetag to the taglist
                                    tagList.Add(new voiceLine(voiceTag, lineNumber));
                                    voiceDictionary.Remove(japaneseSentence);
                                    voiceDictionary.Add(japaneseSentence, tagList);
                                }
                                else
                                {
                                    //japanese line is not in dictionary, so create a new entry
                                    tagList = new List<voiceLine>();
                                    tagList.Add(new voiceLine(voiceTag, lineNumber));
                                    voiceDictionary.Add(japaneseSentence, tagList);
                                }
                            }
                        }
                        lastLineType = "englishvoice";
                    }
                    else if (isJapaneseCharacterLine(line))
                    {
                        //ignore non-text Japanese lines
                    }
                    else if (line.StartsWith("E_N`") || line.StartsWith("`"))
                    {
                        //English sentences without dwave commands
                        sentenceIndex = 0;
                        lastLineType = "";
                        JapaneseSentences.Clear();
                    }
                    else if (line.Trim().Equals("") || line.Trim().Equals("E_N") || line.StartsWith("chvol") || line.StartsWith("delay"))
                    {
                        //Treat empty lines as though they were never there
                    }
                    else
                    {
                        sentenceIndex = 0;
                        lastLineType = "";
                        JapaneseSentences.Clear();
                    }
                }
                catch (Exception e)
                {
                    sentenceIndex = 0;
                    JapaneseSentences.Clear();
                    lastLineType = "";
                    errorFile.WriteLine("EXCEPTION AT LINE: " + line);
                }
                lineNumber++;
            }
            oldScript.Close();
            errorFile.Close();

            return voiceDictionary;
        }

        public static void outputVoiceMapping(Dictionary<String, List<voiceLine>> voiceDictionary)
        {
            //Outputting this file is not necessary for inserting the voice tags,
            //but it's a handy reference for how the tags are matched to sentences

            System.IO.StreamWriter outputFile =
                new System.IO.StreamWriter(tagsMapping, false, Encoding.UTF8);

            foreach (KeyValuePair<String, List<voiceLine>> kvp in voiceDictionary)
            {
                outputFile.WriteLine(kvp.Key);
                foreach (voiceLine l in kvp.Value)
                {
                    outputFile.WriteLine("                        " + l.tag + " - " + l.index);
                }
                outputFile.Flush();
            }

            outputFile.Close();
        }

        public static void insertVoiceTags(Dictionary<String, List<voiceLine>> voiceDictionary)
        {
            // Read the file and display it line by line.
            System.IO.StreamReader newScript =
                new System.IO.StreamReader(newScriptPath);

            System.IO.StreamWriter mergedScript =
                new System.IO.StreamWriter(mergedScriptPath, false, Encoding.UTF8);

            System.IO.StreamWriter errorFile =
                new System.IO.StreamWriter(writingErrorsPath, false, Encoding.UTF8);

            List<String> usedTags = new List<String>();
            int lastVoiceLineNum = 0;
            ArrayList JapaneseSentences = new ArrayList();
            String lastLine = "";
            int sentenceIndex = 0;
            String line;
            while ((line = newScript.ReadLine()) != null)
            {
                if (line.Equals("roff")) //all the string aliases will be written at the line labeled roff, because the old script put them before this line
                {
                    writeStrAliases(mergedScript);
                }
                else if (isNewJapaneseTextLine(line))
                {
                    if (lastLine != "japanesetext")
                        JapaneseSentences.Clear();
                    sentenceIndex = 0;
                    String formattedLine = line.Substring(6, line.Length - 7); //remove the leading langjp and the trailing \

                    formattedLine = formattedLine.Trim('@'); //remove any leading or trailing @s
                    String[] sentences = formattedLine.Split('@');

                    if (sentences[0].StartsWith("^^")) //Some japnese lines in the new script have an unnecessary ^^ at the start of the line, remove it
                        sentences[0] = sentences[0].Substring(2);

                    foreach (String s in sentences)
                    {
                        JapaneseSentences.Add(s);
                    }
                    lastLine = "japanesetext";
                    mergedScript.WriteLine(line);
                }
                else if (isNewEnglishLine(line))
                {
                    try
                    {
                        StringBuilder sb = new StringBuilder();
                        if (lastLine != "englishvoice")
                            sentenceIndex = 0;

                        String formattedLine = line.Substring(6, line.Length - 6); //remove the leading langen
                        sb.Append("langen");
                        string[] stringSeparators = new string[] { "@" };
                        String[] sentences = formattedLine.Split(stringSeparators, StringSplitOptions.None);

                        int i = 0;
                        foreach (String englishSentence in sentences)
                        {
                            i++;

                            if (englishSentence.Equals("") || englishSentence.Equals("/"))
                            {
                                sb.Append(englishSentence);
                                continue;
                            }

                            String japaneseSentence = (String)JapaneseSentences[sentenceIndex];
                            List<voiceLine> l;
                            if (voiceDictionary.TryGetValue(japaneseSentence, out l))
                            {
                                sentenceIndex++;
                                if (l.Count == 1)
                                {
                                    sb.Append(":dwave 0, " + l[0].tag + ":" + englishSentence);
                                    lastVoiceLineNum = l[0].index;

                                    if (usedTags.Contains(l[0].tag))
                                    {
                                        errorFile.WriteLine("DUPLICATED LINE: " + l[0].tag);
                                        errorFile.Flush();
                                    }
                                    else
                                    {
                                        usedTags.Add(l[0].tag);
                                    }
                                }
                                else if (l.Count > 1)
                                {
                                    int difference = 999999;
                                    voiceLine matchingLine = l[0];
                                    foreach (voiceLine v in l)
                                    {
                                        if (Math.Abs(v.index - lastVoiceLineNum) < difference)
                                        {
                                            difference = Math.Abs(v.index - lastVoiceLineNum);
                                            matchingLine = v;
                                        }
                                    }
                                    sb.Append(":dwave 0, " + matchingLine.tag + ":" + englishSentence);
                                    lastVoiceLineNum = matchingLine.index;

                                    if (usedTags.Contains(matchingLine.tag))
                                    {
                                        errorFile.WriteLine("DUPLICATED LINE: " + matchingLine.tag);
                                        errorFile.Flush();
                                    }
                                    else
                                    {
                                        usedTags.Add(matchingLine.tag);
                                    }
                                }
                            }
                            else
                            {
                                sb.Append(englishSentence);
                            }
                            if (i < sentences.Length)
                                sb.Append("@");

                        }
                        mergedScript.WriteLine(sb.ToString());
                        mergedScript.Flush();

                        lastLine = "englishvoice";
                    } catch (Exception e)
                    {
                        lastLine = "englishvoice";
                        mergedScript.WriteLine(line);
                        mergedScript.Flush();
                    }
                }
                else if (line.Trim().Equals(""))
                {
                    mergedScript.WriteLine(line);
                }
                else
                {
                    mergedScript.WriteLine(line);
                    sentenceIndex = 0;
                }
            }

            //After writing out everything, check if there are gaps in the tags used
            usedTags.Sort(voiceTagComparator);
            for (int i = 1; i < usedTags.Count; i++)
            {
                String currentTag = usedTags[i];
                String previousTag = usedTags[i - 1];

                if (currentTag.Substring(0, 6).Equals(previousTag.Substring(0, 6)))
                {
                    if (currentTag.StartsWith("\"") || previousTag.StartsWith("\""))
                        continue;

                    try {

                        double currentindex = parseTagNum(currentTag);
                        double previousindex = parseTagNum(previousTag);

                        if ((currentindex - previousindex) > 1.9)
                        {
                            errorFile.WriteLine("MISSING TAG BETWEEN: " + previousTag + " AND " + currentTag);
                            errorFile.Flush();
                        }
                    } catch (Exception e)
                    {

                    }
                }
            }
            mergedScript.Close();
            errorFile.Close();
        }

        /*
         * Prints all the string aliases to the merged output file
         * */
        static void writeStrAliases(System.IO.StreamWriter mergedScript)
        {
            // Read the file and display it line by line.
            System.IO.StreamReader strAliases =
                new System.IO.StreamReader(aliasesPath);

            String line;
            while ((line = strAliases.ReadLine()) != null)
            {
                mergedScript.WriteLine(line);
            }
            mergedScript.Flush();
            strAliases.Close();
        }

        /*
         * Comparator used to sort voice tags by the number in its suffix
         * eva_1e215 < eva_1e216
         * */
        private static int voiceTagComparator(string x, string y)
        {
            if (x.StartsWith("\"") || y.StartsWith("\""))
                return 1;

            int characterComp = x.Substring(0, 6).CompareTo(y.Substring(0, 6));
            if (characterComp != 0)
                return characterComp;
            else
            {
                try
                {
                    double xindex = parseTagNum(x);
                    double yindex = parseTagNum(y);

                    return xindex.CompareTo(yindex);

                }
                catch (Exception e)
                {
                    return characterComp;
                }
            }
        }

        /*
         * Japanese text lines in the original script start with a ; and end with an @, \, or /
         * ;「んん～？@　戦人ぁ。」@
         * */
        static Boolean isOldJapaneseTextLine(String line)
        {
            if (!line.StartsWith(";"))
                return false;
            if (!line.EndsWith("@") && !line.EndsWith("\\") && !line.EndsWith("/"))
                return false;

            return true;
        }

        /*
         * Japanese text lines in the new script start with langjp and end with an @, \, or /
         * langjp　それが７人で２１０本となり、それらは一斉に爆ぜて、/
         * */
        static Boolean isNewJapaneseTextLine(String line)
        {
            if (!line.StartsWith("langjp"))
                return false;
            if (!line.EndsWith("@") && !line.EndsWith("\\") && !line.EndsWith("/"))
                return false;

            if (line.StartsWith("langjp!sd") || line.StartsWith("langjp!d"))
                return false;

            return true;
        }

        /*
         * There are other japanese lines that lack the line terminator, which are never voiced
         * They start with a ; and end with a japenese character
         * ;＜霧江
         * */
        static Boolean isJapaneseCharacterLine(String line)
        {
            if (line.StartsWith(";") && isJapanese(line[line.Length-1]))
                return true;
            else
                return false;
        }
        

        /*
         * Can determine if a character is Japanese by checking if it's hiragana, katakana, or kanji
         * */
        private static readonly Regex cjkCharRegex = new Regex(@"\p{IsCJKUnifiedIdeographs}");
        private static readonly Regex hiraganaRegex = new Regex(@"\p{IsHiragana}");
        private static readonly Regex katakanaRegex = new Regex(@"\p{IsKatakana}");
        public static bool isJapanese(this char c)
        {
            if (cjkCharRegex.IsMatch(c.ToString()))
                return true;
            else if (hiraganaRegex.IsMatch(c.ToString()))
                return true;
            else if (katakanaRegex.IsMatch(c.ToString()))
                return true;
            else
                return false;
        }

        /*
         * English voiced lines in the old script have a "dwave" command that plays the audio clip
         * rud:dwave 0, rud_1e16`"You're one to talk, Aneki, abusing your little brother like that.  `@/
         * */
        static Boolean isOldEnglishVoiceLine(String line)
        {
            if (line.Contains("dwave"))
                return true;
            else
                return false;
        }

        /*
         * English voiced lines in the new script start with langen
         * langen^...Which made 210 for the 7 giants.  And all of those burst at once, ^/
         * */
        static Boolean isNewEnglishLine(String line)
        {
            if (line.StartsWith("langen") && !line.StartsWith("langen!sd") && !line.StartsWith("langen!d"))
                return true;

            return false;
        }

        /*
         * Converts a voice tag's suffix into a comparable number which can be used to check if
         * geo_1e69 => 69
         * kum_1e65_5 => 65.5
         * kum_1e41_a => 41.1
         * */
        static Double parseTagNum(String tag)
        {
            if (tag.Substring(6).Contains("_"))
            {
                string[] s = tag.Substring(6).Split('_');
                String result = s[0] + ".";
                if (!Char.IsDigit(s[1][0]))
                {
                    s[1] = (s[1][0] - 'a' + 1).ToString();
                }
                return Double.Parse(s[0] + "." + s[1]);
            }
            else
                return Double.Parse(tag.Substring(6));
        }
    }

    /*
     * Class that stores a mapping between a voice tag like nat_1e88
     * and the line number it was used on
     * */
    public class voiceLine
    {
        public String tag;
        public int index;
        public voiceLine(String t, int i)
        {
            tag = t;
            index = i;
        }
    }

    
}

