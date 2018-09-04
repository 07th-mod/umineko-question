using System;
using System.Collections.Generic;

namespace VoicesPuter
{
    /// <summary>
    /// This tool sets voice scripts into langjp using langen's voice scripts.
    /// It also changes voice script's function name of each language.
    /// If the line is langen, it will change dwave_eng.
    /// If the line is langjp, it will change dwave_jp.
    /// </summary>
    public class Program
    {
        #region Methods
        #region Main
        /// <summary>
        /// How to use:
        /// Specify file path of the game script that want to change.
        /// Then this tool will make directory as 'Output' to the same directory that specified it, output the changed game script to 'Output' directory.
        /// If there are some problem, output log to 'Log' directory.
        /// </summary>
        /// <param name="args">Specify file path of the game script that want to change.</param>
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
