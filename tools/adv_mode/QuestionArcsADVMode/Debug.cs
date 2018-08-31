using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestionArcsADVMode
{
    class Debug
    {
        public static bool enabled = false;

        public static void Print(string s)
        {
            if (enabled)
            {
                Console.WriteLine(s);
            }
        }
    }
}
