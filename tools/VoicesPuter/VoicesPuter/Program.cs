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
            List<string> gameScriptLines;
            try
            {
                gameScriptLines = changedGameScriptMaker.ReadGameScript();
            }
            catch (ArgumentException argumentNullProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw argumentNullProblem;
            }
            catch (DirectoryNotFoundException directoryNotFoundProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw directoryNotFoundProblem;
            }
            catch (FileNotFoundException fileNotFoundProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw fileNotFoundProblem;
            }
            catch (NotSupportedException notSupportedProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw notSupportedProblem;
            }
            catch (Exception otherProblem)
            {
                // TODO Have to log about the error.
                Console.WriteLine("Occured unexpected error.");
                throw otherProblem;
            }

            // Put voice scripts into Japanese line and change voice script's function name of both.
            List<string> changedGameScriptLines;
            VoicesPuter voicesPuter = new VoicesPuter();
            try
            {
                changedGameScriptLines = voicesPuter.PutVoiceScriptsIntoLines(gameScriptLines);
            }
            catch (UnmatchedNewLinesWithOriginalLinesException unmatchedNewLinesWithOriginalLinesProbelem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw unmatchedNewLinesWithOriginalLinesProbelem;
            }
            catch (Exception otherProblem)
            {
                // TODO Have to log about the error.
                Console.WriteLine("Occured unexpected error.");
                throw otherProblem;
            }

            // Make the changed game script into output directory.
            try
            {
                changedGameScriptMaker.MakeChangedGameScript(changedGameScriptLines);
            }
            catch (SecurityException securityProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw securityProblem;
            }
            catch (UnauthorizedAccessException unauthorizedAccessProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw unauthorizedAccessProblem;
            }
            catch (ArgumentException argumentNullProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw argumentNullProblem;
            }
            catch (DirectoryNotFoundException directoryNotFoundProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw directoryNotFoundProblem;
            }
            catch (PathTooLongException pathTooLongProbelm)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw pathTooLongProbelm;
            }
            catch (IOException ioProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw ioProblem;
            }
            catch (ObjectDisposedException objectDisposedProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw objectDisposedProblem;
            }
            catch (NotSupportedException notSupportedProblem)
            {
                // TODO Have to notify the problem occured in command line.
                // TODO Have to log about the error.
                throw notSupportedProblem;
            }
        }
        #endregion
        #endregion
    }
}
