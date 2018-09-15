using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoicesPuter
{
    class DwaveArgument
    {
        readonly bool isLiteralPath;
        //readonly string rawValue;
        readonly string normalizedValue;
        //readonly public int trailingDigitsAsInt;
        //readonly public string head;

        //takes a dwave argument like
        //     ev2_3e515
        //or   "voice\99\awase0020.ogg"
        //and uses it to construct a DwaveArgument object
        public DwaveArgument(string dwaveArgument)
        {
            //rawValue = dwaveArgument;

            isLiteralPath = dwaveArgument.Contains('\\');//use this so that even a path like "voice\99\awase0020" can be detected as a literal path
            if (isLiteralPath)
            {
                normalizedValue = dwaveArgument.Trim(new char[] { '"' }).ToLower().Replace(".ogg","");
            }
            else
            {
                normalizedValue = dwaveArgument.ToLower();
            }
        }

        public string GetNormalizedValue() => normalizedValue;
        public string GetUnNormalizedValue()
        {
            return MakeRawValueFromNormalizedValue(normalizedValue);
        }

        public string GetRelativeAudioPath(int difference)
        {
            return MakeRawValueFromNormalizedValue(ChangeTrailingDigits(normalizedValue, difference));
        }

        public string ChangeTrailingDigits(string normalizedArgument, int difference)
        {
            SplitStringOnTrailingDigits(normalizedArgument, out string head, out string trailingDigits);

            string newTrailingDigits = (Int32.Parse(trailingDigits) + difference).ToString();

            return head + newTrailingDigits;
        }

        public int GetTrailingDigits()
        {
            SplitStringOnTrailingDigits(normalizedValue, out string head, out string trailingDigits);
            return Int32.Parse(trailingDigits);
        }

        public string GetHead()
        {
            SplitStringOnTrailingDigits(normalizedValue, out string head, out string trailingDigits);
            return head;
        }


        //returned value will be all lowercase. Game shouldn't mind this.
        public string MakeRawValueFromNormalizedValue(string normalizedValue)
        {
            if(isLiteralPath)
            {
                return $"\"{normalizedValue}.ogg\"";
            }
            else
            {
                return normalizedValue;
            }
        }

        //splits a string into the section before any trailing digits, and the trailing digits
        //TODO: need to handle the cases like: kum_1e65_i? just return false or with an error.
        public void SplitStringOnTrailingDigits(string s, out string head, out string trailingDigits)
        {
            //count from end of string to start of string, stopping at the first non-digit
            int firstNonDigitIndex = s.Length - 1;
            for(; firstNonDigitIndex >= 0; firstNonDigitIndex--)
            {
                if(!Char.IsDigit(s[firstNonDigitIndex]))
                {
                    break;
                }
            }

            if (firstNonDigitIndex == s.Length - 1)
            {
                throw new IndexOutOfRangeException("SplitStringOnTrailingDigits: the last character of the string is not a digit!");
            }

            head = s.Substring(0, firstNonDigitIndex + 1); //length of the non-digit section is lastNonDigitIndex + 1
            trailingDigits = s.Substring(firstNonDigitIndex + 1);            //the last non-digit is at index 'lastNonDigitIndex', therefore the first digit is at 'lastNonDigitIndex + 1'
        }
    }

    //need to be able to query which dwaves are used in the script (like "enj1001")
    public class DwaveDatabase
    {
        bool debugEnabled = false;

        public enum AutoFixResult
        {
            Failure,
            NeedsManualCheck,
            OK,
        }

        //HashSet<string> allDwaveArgs;
        Dictionary<string, DwaveArgument> allDwaveArgs = new Dictionary<string, DwaveArgument>();

        private readonly Regex DwaveRegex = new Regex(@"dwave\s+\d+\s*,\s*(.*?):", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        //might change this later to accept a full dwave command line dwave 0, sadf;alskdfj
        public bool CheckDwaveUsed(string dwaveArgString)
        {
            DwaveArgument dwaveArgumentObject = new DwaveArgument(dwaveArgString);
            return allDwaveArgs.ContainsKey(dwaveArgumentObject.GetNormalizedValue());
        }

        public void AddAllDwavesOnLine(string line)
        {
            foreach (Match match in DwaveRegex.Matches(line))
            {
                //Note: this variable does not have quotes! eg ["test\path"] is converted to [test\path]
                string dwavePathOrAlias = match.Groups[1].Value;

                DwaveArgument dwaveArgumentObject = new DwaveArgument(dwavePathOrAlias);
                allDwaveArgs.Add(dwaveArgumentObject.GetNormalizedValue(), dwaveArgumentObject);
            }
        }

        //expects a list of dwave strings like:
        //{ ":dwave 0, beae14:", ":dwave 0, bea21:",  }
        //outputs a new list with missing voices added in
        public AutoFixResult FixMissingDwaves(List<string> dwaveStrings, int numberOfDwaveToGenerate, out List<string> fixedDwaves)
        {
            List<DwaveArgument> argumentsOnly = new List<DwaveArgument>();
            //get only the arguments of each dwave
            foreach (string s in dwaveStrings)
            {
                Match match = DwaveRegex.Match(s);
                DwaveArgument dwaveArgument = new DwaveArgument(match.Groups[1].Value);
                argumentsOnly.Add(dwaveArgument);
            }

            //check the 'head' of all the dwaves are the same - stop if not the same
            string firstHead = argumentsOnly[0].GetHead();
            foreach (DwaveArgument dwaveArgument in argumentsOnly)
            {
                if(dwaveArgument.GetHead() != firstHead)
                {
                    throw new Exception($"ERROR: head {dwaveArgument.GetHead()} is not the same as {firstHead}!");
                }
            }

            //find the min and max values in the list. Also, create a hashset
            //containing all the dwave arguments in the orginal japanese dwave list
            HashSet<int> digitsInOriginalList = new HashSet<int>();
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;

            foreach(DwaveArgument dwaveArgument in argumentsOnly)
            {
                int trailingDigits = dwaveArgument.GetTrailingDigits();
                digitsInOriginalList.Add(trailingDigits);
                minValue = Math.Min(trailingDigits, minValue);
                maxValue = Math.Max(trailingDigits, maxValue);
            }

            //DEBUG: check the dwaves one before, one after, and inbetween the min and max dwaves.
            //this debugging for printing.
            if (debugEnabled)
            {
                Console.WriteLine("----------------------------");
                for (int i = minValue - 1; i <= maxValue + 1; i++)
                {
                    string generatedDwaveArgument = $"{firstHead}{i}";
                    Console.Write(generatedDwaveArgument);

                    if (i == minValue - 1 || i == maxValue + 1)
                    {
                        Console.Write(" <PREPOST> ");
                    }

                    if (digitsInOriginalList.Contains(i))
                    {
                        Console.WriteLine(": Already in current line.");
                    }
                    else if (CheckDwaveUsed(generatedDwaveArgument))
                    {
                        Console.WriteLine(": Appears in script.");
                    }
                    else
                    {
                        Console.WriteLine(": NOT USED");
                    }
                }
            }

            //Make a new list of dwaves of length "numberOfDwaveToGenerate"
            List<DwaveArgument> generatedDwaveArguments = new List<DwaveArgument>();

            for (int i = minValue - 1; i <= maxValue + 1; i++)
            {
                string generatedDwaveArgumentString = $"{firstHead}{i}";

                //accept the dwave argument if it was already in the current line, OR if it doesn't exist anywhere in the script
                if (!CheckDwaveUsed(generatedDwaveArgumentString) || digitsInOriginalList.Contains(i))
                {
                    generatedDwaveArguments.Add(new DwaveArgument(generatedDwaveArgumentString));
                }

                if(generatedDwaveArguments.Count == numberOfDwaveToGenerate)
                {
                    break;
                }
            }

            if (debugEnabled)
            {
                Console.WriteLine("Generated Dwaves:");
                foreach (DwaveArgument i in generatedDwaveArguments)
                {
                    Console.WriteLine(i.GetNormalizedValue());
                }
            }

            //regenerate the full dwave strings
            fixedDwaves = new List<string>();
            foreach(DwaveArgument dwaveArgument in generatedDwaveArguments)
            {
                fixedDwaves.Add($":dwave 0, {dwaveArgument.GetUnNormalizedValue()}:");
            }

            //DEBUG: print the two dwave lists
            if (debugEnabled)
            {
                foreach (string dwaveCommand in fixedDwaves)
                {
                    Console.Write($"{dwaveCommand},");
                }
                Console.WriteLine();
                foreach (string dwaveCommand in dwaveStrings)
                {
                    Console.Write($"{dwaveCommand},");
                }
                Console.WriteLine();
            }

            //Return whether fixing was sucessfull or unsuccesful
            if (generatedDwaveArguments.Count < numberOfDwaveToGenerate)
            {
                //Couldn't generate enough DWaves
                return AutoFixResult.Failure;
            }
            else if(numberOfDwaveToGenerate < dwaveStrings.Count)
            {
                //was asked to 'remove' dwaves from english line - needs manual check
                return AutoFixResult.NeedsManualCheck;
            }
            else if(generatedDwaveArguments.Count == numberOfDwaveToGenerate)
            {
                //was asked to add more dwaves to japanese line, and succesfully generated the number of dwaves asked.
                return AutoFixResult.OK;
            }

            throw new Exception("FixMissingDwaves: shouldn't be able to reach here");
        }
    }

    //it was intended for this class to also handle voice aliases. perhaps can be removed later.
    public class VoicesDatabase
    {
        public DwaveDatabase DwaveDatabase = new DwaveDatabase();

        public VoicesDatabase(string scriptToScan)
        {
            using (StreamReader reader = new StreamReader(scriptToScan))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    DwaveDatabase.AddAllDwavesOnLine(line);
                }
            }
        }
    }

}

