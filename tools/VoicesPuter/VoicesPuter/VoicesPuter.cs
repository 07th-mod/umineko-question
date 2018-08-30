using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VoicesPuter
{
    public class VoicesPuter
    {
        private string InsertVoicesFromEnglishIntoJapanese(string englishLine, string japaneseLine)
        {
            string insertedJapaneseLine = string.Empty;

            // Get voice scripts with regix.
            Regex voicesRegex = new Regex(@":dwave[\w\W]*:");
            MatchCollection voies = voicesRegex.Matches(englishLine);
            if (voies.Count > 0)
            {
                string[] splitEnglishLine = englishLine.Split('@');
                string[] splitJapaneseLine = japaneseLine.Split('@');

                // Have to notify English line and japanese line's structure is not same.
                if (splitEnglishLine.Length != splitJapaneseLine.Length)
                {
                    return japaneseLine;
                }

                // If a split english string has voice script, append voice script to a split japanese string.
                int voicesCount = 0;
                for (int englishLineIndex = 0; englishLineIndex < splitEnglishLine.Length; englishLineIndex++)
                {
                    if (englishLineIndex > 0)
                    {
                        insertedJapaneseLine += $"{insertedJapaneseLine}@";
                    }
                    if (splitEnglishLine[englishLineIndex].Contains(":dwave"))
                    {
                        insertedJapaneseLine += $"{voies[voicesCount]}{splitJapaneseLine[englishLineIndex]}";
                        voicesCount++;
                    }
                }
            }
            return insertedJapaneseLine;
        }
    }
}
