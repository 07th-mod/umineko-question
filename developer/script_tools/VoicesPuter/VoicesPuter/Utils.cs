using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoicesPuter
{
    class Utils
    {
        public static void DeleteIfExists(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {

            }
        }

    }
}
