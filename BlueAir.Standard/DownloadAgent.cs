using System;

namespace BlueAir
{
    /// <summary>
    ///     Base class for all download agents.
    /// </summary>
    public abstract class DownloadAgent
    {
        /// <summary>
        ///     Name of the agent
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        ///     Runs the agent to download a specific file.
        /// </summary>
        /// <param name="fileName">Name of the file (if given) or path to download this file to.</param>
        /// <param name="url">URL of the file.</param>
        /// <param name="fileNameIsFolder">Determines if the <paramref name="fileName" /> refers to a folder.</param>
        /// <param name="progress">Action that will be invoked when progress changes.</param>
        /// <param name="output">Actio nthat will be invoked when a console output occurs.</param>
        public abstract void Run(string fileName, string url, bool fileNameIsFolder, Action<float> progress,
            Action<string> output);


        /// <summary>
        ///     Checks if the agent does exist and can be used.
        /// </summary>
        /// <returns><c>true</c> if the agent does exists, otherwise <c>false</c>.</returns>
        public abstract bool Exists();
    }
}