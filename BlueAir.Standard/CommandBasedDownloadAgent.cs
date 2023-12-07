using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace BlueAir
{
    /// <summary>
    ///     Class used for all command-based agents, none will work on mobile platforms.
    /// </summary>
    public class CommandBasedDownloadAgent : DownloadAgent
    {
        private string _name;

        private string foundFile;

        /// <summary>
        ///     Creates a new command-based download agent by reading XML data from <paramref name="file" />.
        /// </summary>
        /// <param name="file">XML file that contains the required information.</param>
        public CommandBasedDownloadAgent(string file)
        {
            if (!System.IO.File.Exists(file)) throw new FileNotFoundException(null, file);
            File = file;
            var doc = new XmlDocument();
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    doc.LoadXml(reader.ReadToEnd());
                }
            }

            if (doc.DocumentElement != null) ParseXml(doc.DocumentElement);
        }

        /// <summary>
        ///     Creates a new command-based download agent by reading XML data from <paramref name="node" />.
        /// </summary>
        /// <param name="node">XML node that contains the required information.</param>
        public CommandBasedDownloadAgent(XmlNode node)
        {
            ParseXml(node);
        }

        /// <summary>
        ///     Command to execute when starting to download.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public string Command { get; set; } = string.Empty;

        /// <summary>
        ///     Files to search to check if this download agent's command can be accessed.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public string[] FilesToSearch { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     File that contains the information about this agent.
        /// </summary>
        public string File { get; set; }

        public override string Name => _name;

        private void ParseXml(XmlNode root)
        {
            foreach (XmlNode node in root.ChildNodes)
                switch (node.Name.ToLowerInvariant())
                {
                    case "name":
                        _name = node.InnerXml;
                        break;
                    case "command":
                        Command = node.InnerXml;
                        break;

                    case "files":
                        var filesToSearch = new List<string>();
                        foreach (XmlNode fileSearchNode in node.ChildNodes) filesToSearch.Add(fileSearchNode.InnerXml);

                        FilesToSearch = filesToSearch.ToArray();
                        break;
                }
        }

        public override void Run(string fileName, string url, bool fileNameIsFolder, Action<float> progress,
            Action<string> output)
        {
            if (BlueAir.DisableCommands) throw new Exception("Commands are disabled.");
            var before_info = new ProcessStartInfo(Command.Replace("$path$", foundFile).Replace("$url$", url)
                .Replace("$file$", fileName))
            {
                UseShellExecute = true,
                RedirectStandardOutput = true
            };
            var before_process = new Process { StartInfo = before_info };
            before_process.ErrorDataReceived += (_, args) => output?.Invoke(args.Data);
            before_process.OutputDataReceived += (_, args) => output?.Invoke(args.Data);
            before_process.Start();
            before_process.WaitForExit();
        }

        public override bool Exists()
        {
            foreach (var file in FilesToSearch)
                if (System.IO.File.Exists(file))
                {
                    foundFile = file;
                    return true;
                }

            return false;
        }
    }
}