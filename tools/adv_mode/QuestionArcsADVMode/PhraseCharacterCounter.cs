using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    //keeps track of phrase count
    internal class PhraseCharacterCounter
    {
        public PhraseCharacterCounter()
        {
        }

        public int GetCharacterCount(string phrase)
        {
            return phrase.Length;
        }
    }

}
