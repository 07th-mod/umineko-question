using System.Text.RegularExpressions;

namespace QuestionArcsADVMode
{
    class CharacterCountInserter
    {
        public static readonly Regex langEnAtStartOfLine = new Regex(@"^\s*langen", RegexOptions.IgnoreCase);
        public LineParser lp;
        public PhraseCharacterCounter characterCounter;

        public CharacterCountInserter()
        {
            lp = new LineParser();
            characterCounter = new PhraseCharacterCounter();
        }

        public void ProcessOneLine(string line)
        {
            //skip any non 'langen' lines
            if (!langEnAtStartOfLine.IsMatch(line))
                return;

            System.Console.WriteLine(line);

            //iterate through all the tokens in the line
            lp.LoadLine(line);
            foreach (NamedMatch token in lp.iter())
            {
                HandleToken(token);
            }
        }


        public void HandleToken(NamedMatch token)
        {
            if (token.Type == MatchType.text)
            {
                characterCounter.AddPhrase(token.Match.Value);
            }
        }
    }

    internal class Program
    {

        private static void Main(string[] args)
        {
            string input_script = @"C:\drojf\large_projects\umineko\umineko_question_repo\InDevelopment\ManualUpdates\0.utf";
            System.IO.StreamReader file = new System.IO.StreamReader(input_script);
            string line;

            CharacterCountInserter characterCountInserter = new CharacterCountInserter();

            int line_count = 0;
            while ((line = file.ReadLine()) != null)
            {
                characterCountInserter.ProcessOneLine(line);
                line_count++;
            }

        }

    }

}


