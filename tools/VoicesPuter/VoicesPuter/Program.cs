using System;
using System.Collections.Generic;
using System.IO;

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
        private const string ANSWER_ARCS_RELATIVE_PATH = @"C:\drojf\large_projects\umineko\umineko_answer_repo\0.utf";
        private const string GIT_GAME_SCRIPT_RELATIVE_PATH = @"..\..\..\..\..\InDevelopment\ManualUpdates\0.utf";
        private const string defaultGamePath = GIT_GAME_SCRIPT_RELATIVE_PATH;
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
            // Read all of the game script lines.
            string gameScriptPath = null;

            // If the game script is not specified, notify usage.
            if (args.Length <= 0)
            {
                //if no arguments specified, look in the repository for the game file
                if(File.Exists(defaultGamePath))
                {
                    Console.WriteLine("Detected program is run from git repository - Using latest question arcs script");
                    gameScriptPath = defaultGamePath;
                }
                else
                {
                    Console.WriteLine("ERROR: No arguments provided, and game script not found in default path.");
                    Console.WriteLine("Please specify file path of the game script that want to change.");
                    Console.WriteLine("Usage: VoicesPuter <file path>");
                    return;
                }
            }
            else
            {
                gameScriptPath = args[0];
            }

            ChangedGameScriptMaker changedGameScriptMaker = new ChangedGameScriptMaker(gameScriptPath);
            List<string> gameScriptLines = changedGameScriptMaker.ReadGameScript();

            //scan script for dwave commands and construct a database of all dwave commands
            VoicesDatabase voicesDatabase = new VoicesDatabase(gameScriptPath);

            // Put voice scripts into Japanese line and change voice script's function name of both.
            VoicesPuter voicesPuter = new VoicesPuter(gameScriptPath, overwrite: true, voicesDatabase: voicesDatabase);
            List<string> changedGameScriptLines = voicesPuter.PutVoiceScriptsIntoLines(gameScriptLines, voicesDatabase);

            FixVoiceDelay.FixVoiceDelaysInScript(changedGameScriptLines);

            // Make the changed game script into output directory.
            changedGameScriptMaker.MakeChangedGameScript(changedGameScriptLines);
            Console.WriteLine("Completed putting voice scripts into Japanese lines.");
            Console.WriteLine("Press any key to close this window...");
            Console.ReadKey();
        }
        #endregion
        #endregion
    }
}
