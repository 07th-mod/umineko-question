using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    class TextboxNameInserter
    {
        private static Regex straliasRegex = new Regex(@"^\s*stralias[,\s]+([^,\s]+)[,\s]+""([^""]+)""", RegexOptions.IgnoreCase);
        private static Regex dwaveRegex = new Regex(@"dwave_((eng)|(jp))\s*\d\s*,\s*(\w+)", RegexOptions.IgnoreCase);

        private readonly Dictionary<string, string> voiceAliasLowerCaseToNormalizedVoicePathNoExtensionMap;
        private readonly Dictionary<string, string> normalizedVoicePathToIDMap;
        private readonly Dictionary<string, string> normalizedVoiceFilenameToIDMap;
        private readonly Dictionary<string, string> duplicateVoiceFilenameChecker; //maps voice_filename->folder


        private readonly Dictionary<string, string> manualMapping = new Dictionary<string, string>()
        {
            { "gen_1e40", "14" },
            { "kla_1e275","02" },
            { "mar_1e562_1", "13" },
            { "mar_1e563", "13" },
            { "mar_1e614", "13" },
            { "nat_1e396", "03" },
            { "gen_1e241", "14" },
            { "jes_1e698", "04" },
            { "eva_2e78", "05"},
            { "bea_2e369_3", "27" },
            { "bea_2e369_4", "27" },
            { "but_2e1212", "10" },
            { "but_2e1219", "10" },
            { "but_2e1220", "10" },
            { "mar_3e18", "13" },
            { "eva_3e609", "05"},
            { "mar_4e56", "13" },
            { "geo_4e141", "07"},
            { "s45_4e36",  "35"},
            { "ber_9e87",  "28"},
        };

        private readonly Dictionary<string, int> debug_voiceUsageCount;

        public TextboxNameInserter()
        {
            voiceAliasLowerCaseToNormalizedVoicePathNoExtensionMap = new Dictionary<string, string>();
            normalizedVoicePathToIDMap = new Dictionary<string, string>();
            debug_voiceUsageCount = new Dictionary<string, int>();
            normalizedVoiceFilenameToIDMap = new Dictionary<string, string>();
            duplicateVoiceFilenameChecker = new Dictionary<string, string>();
        }

        public void ReadVoiceToNameIDFile(string voiceToNameIDFilePath)
        {
            foreach (string line in File.ReadAllLines(voiceToNameIDFilePath))
            {
                string[] splitLine = line.Split(new char[] {' '});
                
                if (splitLine.Length != 2)
                {
                    throw new FormatException("there is more than one space or no spaces on the line!");
                }

                string voice_path = splitLine[0];
                string voice_name_id = splitLine[1];

                //this matches only using the filename
                string[] splitpath = NormalizeVoicePath(voice_path).Split(new char[] { '\\' });
                string voice_folder = splitpath[0];
                string voice_filename = splitpath[1];
                
                //check for voice filenames which are present in two different folders but with the same filename
                if(duplicateVoiceFilenameChecker.ContainsKey(voice_filename))
                {
                    if(duplicateVoiceFilenameChecker[voice_filename] != voice_folder)
                    {
                        Console.WriteLine($"ERROR: duplicate filename!!!! {voice_filename}");
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }
                }
                else
                {
                    duplicateVoiceFilenameChecker[voice_folder] = voice_folder;
                }

                normalizedVoiceFilenameToIDMap[voice_filename] = voice_name_id;

                normalizedVoicePathToIDMap[NormalizeVoicePath(voice_path)] = voice_name_id;
                debug_voiceUsageCount[NormalizeVoicePath(voice_path)] = 0;
            }
        }

        public void ReadVoiceAliases(string[] lines)//, bool fixUmitweakEV2)
        {
            foreach (string line in lines)
            {
                Match match = straliasRegex.Match(line);
                if (match.Success)
                {
                    string alias = match.Groups[1].Value;
                    string path = match.Groups[2].Value;

                    if (!path.ToLower().Contains("voice"))
                        continue;

                    //if (fixUmitweakEV2)
                    //{
                    //    if (alias.Contains("ev2_3"))
                    //    {
                    //        path = path.Replace("voice\\05\\", "voice\\34\\");
                    //    }
                    //    else if(alias.Contains("ev2_4"))
                    //    {
                    //        path = path.Replace("voice\\05\\", "voice\\34\\");
                    //    }
                    //}

                    //NOTE: this assumes the folder name doesn't have a '.' in it
                    string[] splitPathWithExtension = path.Split(new char[] { '.' });

                    //get only filename
                    string[] splitPath = NormalizeVoicePath(splitPathWithExtension[0]).Split(new char[] { '\\' });

                    // Save [lower case alias -> noramlized voice path without extension] mapping
                    voiceAliasLowerCaseToNormalizedVoicePathNoExtensionMap[alias.ToLower()] = splitPath[1];
                }
            }
        }

        public string getIDForLine(string line)
        {
            //find all dwaves on the line. If no dwaves, skip this line
            string[] dwaveAliasesLowerCase = GetDwavesOnLineLowerCase(line);
            if (dwaveAliasesLowerCase.Length == 0)
            {
                return null;
            }


            foreach (string s in dwaveAliasesLowerCase)
            {
                if (s.Contains("mix_"))
                {
                    //Console.WriteLine($"Should skip {s} as it is a mixed voice");
                    return "-1";
                }

                if(manualMapping.ContainsKey(s))
                {
                    return manualMapping[s];
                }
            }
            //Console.WriteLine();


            //lookup which voice file the alias corresponds to. Exit with error if not found?
            string[] paths = dwaveAliasesLowerCase.Select(x => voiceAliasLowerCaseToNormalizedVoicePathNoExtensionMap[x]).ToArray();

            //foreach (string s in paths)
            //{
            //    Console.Write(s);
            //}
            //Console.WriteLine();

            //mark off the voice path as 'used', so we can see which ps3 voice files have no mappping (maybe this isn't necessary?)
            /*foreach(string path in paths)
            {
                if(debug_voiceUsageCount.ContainsKey(path))
                {
                    debug_voiceUsageCount[path] += 1;
                }
                else
                {
                    Console.WriteLine($"PS3 voice database doesn't contain {path}");
                }
            }*/

            //determine which character is talking via normalizedVoicePathToIDMap. Try all the voices on the line, 
            //prefer '-1' if there is -1, otherwise take majority. For now, if any differ, raise error?
            List<string> characterIDs = new List<string>();

            foreach (string path in paths)
            {
                bool success = normalizedVoiceFilenameToIDMap.TryGetValue(path, out string value);
                if (success)
                {
                    characterIDs.Add(value);
                }
            }

            if (characterIDs.Count == 0)
            {
                Console.WriteLine($"Warning: line has no character IDs: {line}");
                return "-1";
            }

            return characterIDs[0];

            //Console.Write("characterIDs: ");
            //foreach(string characterID in characterIDs)
            //{
            //    Console.Write($"{characterID}, ");
            //}

            //Console.WriteLine();

            //insert character marker above the line
        }

        public List<string> InsertIntoScript(string[] lines)
        {
            List<string> outputLines = new List<string>(200000);
            string lastid = null;
            foreach(string line in lines)
            {
                if (line.Contains("langen") || line.Contains("langjp"))
                {
                    string id = getIDForLine(line);
                    if (id != null && id != lastid)
                    {
                        outputLines.Add($"advchar \"{id}\"");
                    }
                    else if (lastid != null && id == null)
                    {
                        outputLines.Add($"advchar \"-1\"");
                    }
                    lastid = id;
                }

                outputLines.Add(line);
            }

            return outputLines;
        }

        private static string NormalizeVoicePath(string path)
        {
            return path.ToLower().Replace('/', '\\').Replace("voice\\","");
        }
        
        private static string[] GetDwavesOnLineLowerCase(string line)
        {
            MatchCollection matches = dwaveRegex.Matches(line);
            return matches.Cast<Match>().Select(x => x.Groups[4].Value.ToLower()).ToArray();
        }

    }
}
