using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace VoicesPuter
{
    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        #region Methods
        #region Main
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // If the game script is not specified, notify usage.
            if (args.Length <= 0)
            {
                Console.WriteLine("Please specify file path of the game script that want to change.");
                Console.WriteLine("Usage: VoicesPuter <file path>");
                return;
            }

            // Read all of the game script lines.
            string gameScriptPath = args[0];
            ChangedGameScriptMaker changedGameScriptMaker = new ChangedGameScriptMaker(gameScriptPath);
            List<string> gameScriptLines = changedGameScriptMaker.ReadGameScript();

            // Put voice scripts into Japanese line and change voice script's function name of both.
            VoicesPuter voicesPuter = new VoicesPuter(gameScriptPath);
            List<string> changedGameScriptLines = voicesPuter.PutVoiceScriptsIntoLines(gameScriptLines);

            // Make the changed game script into output directory.
            changedGameScriptMaker.MakeChangedGameScript(changedGameScriptLines);
            Console.WriteLine("Completed putting voice scripts into Japanese lines.");
            Console.ReadKey();
        }
        #endregion
        #endregion
    }
}
