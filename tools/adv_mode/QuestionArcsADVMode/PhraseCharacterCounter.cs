using System;

namespace QuestionArcsADVMode
{
    //keeps track of phrase count
    internal class PhraseCharacterCounter
    {
        private int LastPhraseCharacterCount { get; }  //the number of characters in the most recently parsed phrase

        public PhraseCharacterCounter()
        {
            LastPhraseCharacterCount = 0;
        }

        public void AddPhrase(string match)
        {
            Console.WriteLine($"[{match}]");
        }

    }

}
