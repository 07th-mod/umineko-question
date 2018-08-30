using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    internal class CharacterCountInserter
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
            {
                return;
            }

            System.Console.WriteLine(line);

            //iterate through all the tokens in the line
            lp.LoadLine(line);
            foreach (Token token in lp.Iter())
            {
                HandleToken(token);
            }
        }


        public void HandleToken(Token token)
        {
            switch (token)
            {
                case TextToken textToken:
                    characterCounter.AddPhrase(token.RawString);
                    break;

                case GenericToken genericToken:
                    Console.WriteLine($"Got generic token {token.RawString}");
                    break;

                default:
                    throw new NotImplementedException();

                case null:
                    throw new ArgumentNullException();
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


