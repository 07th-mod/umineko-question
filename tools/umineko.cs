private string InsertEnglishIntoJapanese(string englishLine, string japaneseLine)
{
    // get voice scripts with regix.
    Regex voicesRegex = new Regex(@":dwave[\w\W]*:");
    MatchCollection voies = voicesRegex.Matches(englishLine);
    if (voies.Count > 0)
    {
        String[] splitEnglishLine = englishLine.Split('@');
        String[] splitJapaneseLine = japaneseLine.Split('@');

        // have to notify English line and japanese line's structure is not same.
        if (splitEnglishLine.Length != splitJapaneseLine.Length)
        {
            return japaneseLine;
        }

        // If a split english string has voice script, append voice script to a split japanese string.
        string newJapaneseLine = string.Empty;
        int voicesCount = 0;
        for (int englishLineIndex = 0; englishLineIndex < splitEnglishLine.Length; englishLineIndex++)
        {
            if (englishLineIndex > 0)
            {
                newJapaneseLine += $"{newJapaneseLine}@"
            }
            if (englishLineIndex[englishLineIndex].Contains(":dwave"))
            {
                newJapaneseLine += $"{voies[voicesCount]}{splitJapaneseLine[englishLineIndex]}";
                voicesCount++;
            }
        }
    }
    return newJapaneseLine;
}