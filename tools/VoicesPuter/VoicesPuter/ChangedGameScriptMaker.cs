using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VoicesPuter
{
    /// <summary>
    /// 
    /// </summary>
    public class ChangedGameScriptMaker
    {
        #region Members
        #region GAME_SCRIPT_FILE_NAME
        /// <summary>
        /// 
        /// </summary>
        private const string GAME_SCRIPT_FILE_NAME = "0.utf";
        #endregion

        #region CHANGED_SCRIPT_OUTPUT_DIRECTORY_NAME
        /// <summary>
        /// 
        /// </summary>
        private const string CHANGED_SCRIPT_OUTPUT_DIRECTORY_NAME = "Output";
        #endregion

        #region CHANGED_SCRIPT_OUTPUT_DIRECTORY_NAME
        /// <summary>
        /// 
        /// </summary>
        private readonly string GameScriptPath;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameScriptPath"></param>
        public ChangedGameScriptMaker(string gameScriptPath)
        {
            GameScriptPath = gameScriptPath;
        }
        #endregion

        #region Methods
        #region ReadGameScript
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> ReadGameScript()
        {
            List<string> gameScriptLines = new List<string>();
            using (StreamReader gameScriptReader = new StreamReader(GameScriptPath, Encoding.UTF8))
            {
                string currentLine;
                while ((currentLine = gameScriptReader.ReadLine()) != null)
                {
                    gameScriptLines.Add(currentLine);
                }
            }
            return gameScriptLines;
        }
        #endregion

        #region MakeChangedGameScript
        /// <summary>
        /// 
        /// </summary>
        /// <param name="changedGameScriptLines"></param>
        public void MakeChangedGameScript(List<string> changedGameScriptLines)
        {
            // If output directory does not exsit, make the directory.
            string outputDirectoryPath = Path.Combine(new string[] { Path.GetDirectoryName(GameScriptPath), CHANGED_SCRIPT_OUTPUT_DIRECTORY_NAME, });
            if (!Directory.Exists(outputDirectoryPath))
            {
                Directory.CreateDirectory(outputDirectoryPath);
            }

            // output the changed game scriptinto the directory.
            string outputFilePath = Path.Combine(new string[] { outputDirectoryPath, GAME_SCRIPT_FILE_NAME, });
            using (StreamWriter gameScriptWriter = new StreamWriter(outputFilePath, false, Encoding.UTF8))
            {
                foreach (string currentLine in changedGameScriptLines)
                {
                    gameScriptWriter.WriteLine(currentLine);
                }
            }
        }
        #endregion
        #endregion
    }
}
