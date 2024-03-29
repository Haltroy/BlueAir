using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace BlueAir
{
    /// <summary>
    ///     Information about a bulk download process.
    /// </summary>
    public class DownloadInfo
    {
        #region XML

        /// <summary>
        ///     Export this <see cref="DownloadInfo" /> to XML.
        /// </summary>
        /// <param name="includeFinished">Includes everything.</param>
        /// <returns>A <see cref="string" />.</returns>
        public string ToXml(bool includeFinished = false)
        {
            var xml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>" + Environment.NewLine + "<root>";

            xml += "<Run-Commands>" + (RunCommands ? "true" : "false") + "</Run-Commands>" + Environment.NewLine;
            xml += "<Command-Before Required=\"" + (!CommandToExecuteBefore.CanFail ? "true" : "false") + "\">" +
                   CommandToExecuteBefore.Command + "</Command-Before>" + Environment.NewLine;
            xml += "<Command-After Required=\"" + (!CommandToExecuteAfter.CanFail ? "true" : "false") + "\">" +
                   CommandToExecuteAfter.Command + "</Command-After>" + Environment.NewLine;
            xml += "<Downloader>" + Downloader.Name + "</Downloader>" + Environment.NewLine;

            xml += "<Items>" + Environment.NewLine;
            foreach (var obj in Downloads) xml += DownloadObjectToXml(obj);

            xml += "</Items>" + Environment.NewLine;

            return xml + "</root>";
        }

        #region XML Helpers

        private string DownloadObjectToXml(DownloadObject downloadObject)
        {
            switch (downloadObject)
            {
                case DownloadFile file:
                    return
                        $"<File {(string.IsNullOrWhiteSpace(file.FileName) ? "" : $"Name=\"{file.FileName}\"")} Link=\"{file.Link}\" />" +
                        Environment.NewLine;

                case DownloadFolder folder:
                    var xml =
                        $"<Folder {(string.IsNullOrWhiteSpace(folder.Name) ? "" : $"Name=\"{folder.Name}\"")}>{Environment.NewLine}";
                    foreach (var obj in folder.Content) xml += DownloadObjectToXml(obj);

                    return xml + $"</Folder>{Environment.NewLine}";
            }

            return string.Empty;
        }

        private void ParseXml(XmlNode root)
        {
            foreach (XmlNode node in root.ChildNodes)
                switch (node.Name.ToLowerInvariant())
                {
                    case "run-commands":
                        RunCommands = node.InnerXml.ToLowerInvariant() == "true";
                        break;

                    case "command-before":
                        if (node.Attributes != null)
                            foreach (XmlAttribute attr in node.Attributes)
                                switch (attr.Name.ToLowerInvariant())
                                {
                                    case "required":
                                        CommandToExecuteBefore.CanFail = attr.InnerXml.ToLowerInvariant() != "true";
                                        break;
                                }

                        CommandToExecuteBefore.Command = node.InnerXml;
                        break;

                    case "command-after":
                        if (node.Attributes != null)
                            foreach (XmlAttribute attr in node.Attributes)
                                switch (attr.Name.ToLowerInvariant())
                                {
                                    case "required":
                                        CommandToExecuteAfter.CanFail = attr.InnerXml.ToLowerInvariant() != "true";
                                        break;
                                }

                        CommandToExecuteAfter.Command = node.InnerXml;
                        break;

                    case "downloader":
                        foreach (var agent in BlueAir.Agents)
                            if (agent.Name.Equals(node.InnerXml, StringComparison.InvariantCultureIgnoreCase))
                            {
                                Downloader = agent;
                                break;
                            }

                        break;

                    case "items":
                        foreach (XmlNode itemNode in node.ChildNodes) ParseXmlItems(itemNode);
                        break;
                }
        }

        private void ParseXmlItems(XmlNode item, DownloadFolder parent = null)
        {
            switch (item.Name.ToLowerInvariant())
            {
                case "folder":
                    if (item.Attributes != null)
                    {
                        var name = string.Empty;
                        foreach (XmlAttribute attr in item.Attributes)
                            switch (attr.Name.ToLowerInvariant())
                            {
                                case "name":
                                    name = attr.InnerXml;
                                    break;
                            }

                        var folder = new DownloadFolder { Name = name };
                        if (item.ChildNodes.Count > 0)
                            foreach (XmlNode itemNode in item.ChildNodes)
                                ParseXmlItems(itemNode, folder);
                        if (parent is null)
                            Downloads.Add(folder);
                        else
                            parent.Content.Add(folder);
                    }

                    break;

                case "file":
                    if (item.Attributes != null)
                    {
                        var fileName = string.Empty;
                        var link = string.Empty;
                        foreach (XmlAttribute attr in item.Attributes)
                            switch (attr.Name.ToLowerInvariant())
                            {
                                case "filename":
                                    fileName = attr.InnerXml;
                                    break;
                                case "link":
                                    link = attr.InnerXml;
                                    break;
                            }

                        if (parent is null)
                            Downloads.Add(new DownloadFile { FileName = fileName, Link = link });
                        else
                            parent.Content.Add(new DownloadFile { FileName = fileName, Link = link });
                    }

                    break;
            }
        }

        #endregion XML Helpers

        #endregion XML

        #region Delegates

        /// <summary>
        ///     Delegate used in <see cref="DownloadInfo.OnConsoleOutput" />.
        /// </summary>
        public delegate void OnConsoleOutputDelegate(string output);

        /// <summary>
        ///     Delegate used in <see cref="DownloadInfo.OnFinishedItem" />.
        /// </summary>
        public delegate void OnFinishedItemDelegate(DownloadObject item);

        /// <summary>
        ///     Delegate used in <see cref="DownloadInfo.OnProgressChanged" />.
        /// </summary>
        public delegate void OnProgressChangedDelegate(float percentage, DownloadObject current_item);

        #endregion Delegates

        #region Constructors

        /// <summary>
        ///     Creates a new empty <see cref="DownloadInfo" />.
        /// </summary>
        public DownloadInfo()
        {
        }

        /// <summary>
        ///     Creates a new <see cref="DownloadInfo" /> by using file (or XML text if <paramref name="threatAsXml" /> is
        ///     <c>true</c>) <paramref name="file" />.
        ///     <param name="file">Path of the file that contains the information or XML text that contains the information.</param>
        /// </summary>
        public DownloadInfo(string file, bool threatAsXml)
        {
            var doc = new XmlDocument();

            if (threatAsXml) doc.LoadXml(file);
            else
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    doc.LoadXml(reader.ReadToEnd());
                }

            if (doc.DocumentElement != null) ParseXml(doc.DocumentElement);
        }

        /// <summary>
        ///     Creates a new <see cref="DownloadInfo" /> by reading information from <paramref name="stream" />.
        /// </summary>
        /// <param name="stream">Stream that contains the information.</param>
        public DownloadInfo(Stream stream)
        {
            var doc = new XmlDocument();
            using (var reader = new StreamReader(stream))
            {
                doc.LoadXml(reader.ReadToEnd());
            }

            if (doc.DocumentElement != null) ParseXml(doc.DocumentElement);
        }

        /// <summary>
        ///     Creates a new <see cref="DownloadInfo" /> by reading information from <paramref name="node" />.
        /// </summary>
        /// <param name="node">XML Node that contains the information.</param>
        public DownloadInfo(XmlNode node)
        {
            ParseXml(node);
        }

        #endregion Constructors

        #region Properties & Events

        /// <summary>
        ///     Determines if <see cref="CommandToExecuteBefore" /> and <see cref="CommandToExecuteAfter" /> should be run.
        /// </summary>
        public bool RunCommands { get; set; }

        /// <summary>
        ///     Code/program that downloads the files.
        /// </summary>
        public DownloadAgent Downloader { get; set; }

        /// <summary>
        ///     Information about a command that will be executed before the download.
        /// </summary>
        public CommandToExecute CommandToExecuteBefore { get; set; } = new CommandToExecute();

        /// <summary>
        ///     Information about a command that will be executed after the download finishes.
        /// </summary>
        public CommandToExecute CommandToExecuteAfter { get; set; } = new CommandToExecute();

        /// <summary>
        ///     List of things to download.
        /// </summary>
        public List<DownloadObject> Downloads { get; set; } = new List<DownloadObject>();

        /// <summary>
        ///     Event raised when a download item's progress changes.
        /// </summary>
        public event OnProgressChangedDelegate OnProgressChanged;

        /// <summary>
        ///     Event raised when a download is complete.
        /// </summary>
        public event OnFinishedItemDelegate OnFinishedItem;

        /// <summary>
        ///     Event raised when a console output is entered by program and/or BlueAir.
        /// </summary>
        public event OnConsoleOutputDelegate OnConsoleOutput;

        #endregion Properties & Events

        #region Start

        /// <summary>
        ///     Starts the download progress.
        ///     <para />
        ///     NOTE: Use an async task if you don't want your GUI to freeze while doing progress.
        /// </summary>
        /// <param name="MainFolder">Folder to download those files to.</param>
        public void Start(string MainFolder)
        {
            var sw = new Stopwatch();
            sw.Start();
            OnConsoleOutput?.Invoke($"Starting to download files to \"{MainFolder}\"...");
            if (!Directory.Exists(MainFolder)) Directory.CreateDirectory(MainFolder);
            foreach (var item in Downloads)
            {
                switch (item)
                {
                    case DownloadFolder folder:
                        StartDownload(MainFolder, folder);
                        break;

                    case DownloadFile file:
                        StartDownloadSingleFile(MainFolder, file);
                        break;
                }

                OnFinishedItem?.Invoke(item);
            }

            sw.Stop();
            OnConsoleOutput?.Invoke($"Finished downloading files to \"{MainFolder}\" in {sw.Elapsed:g}");
        }

        #region Start Helpers

        private void StartDownload(string mainFolder, DownloadFolder parent)
        {
            OnConsoleOutput?.Invoke($"Starting to download folder \"{parent.Name}\"...");
            if (!Directory.Exists(parent.GetPath(mainFolder))) Directory.CreateDirectory(parent.GetPath(mainFolder));

            foreach (var item in parent.Content)
            {
                switch (item)
                {
                    case DownloadFolder folder:
                        StartDownload(mainFolder, folder);
                        break;

                    case DownloadFile file:
                        StartDownloadSingleFile(mainFolder, file);
                        break;
                }

                OnFinishedItem?.Invoke(item);
            }

            OnConsoleOutput?.Invoke($"Finished downloading folder \"{parent.Name}\".");
        }

        private void StartDownloadSingleFile(string MainFolder, DownloadFile file)
        {
            var fileText = string.IsNullOrWhiteSpace(file.FileName)
                ? file.Link
                : file.FileName;
            if (RunCommands && !BlueAir.DisableCommands &&
                !string.IsNullOrWhiteSpace(CommandToExecuteBefore.Command))
            {
                OnConsoleOutput?.Invoke(
                    $"Execute before command to download file \"{fileText}\"...");
                var before_info = new ProcessStartInfo(CommandToExecuteBefore.Command)
                {
                    UseShellExecute = true,
                    RedirectStandardOutput = true
                };
                var before_process = new Process { StartInfo = before_info };
                before_process.ErrorDataReceived += (_, args) => OnConsoleOutput?.Invoke(args.Data);
                before_process.OutputDataReceived += (_, args) => OnConsoleOutput?.Invoke(args.Data);
                before_process.Start();
                before_process.WaitForExit();
                if (!CommandToExecuteBefore.CanFail && before_process.ExitCode != 0)
                {
                    OnConsoleOutput?.Invoke(
                        $"Exit code of before process on file \"{fileText}\" is not 0. Skipping item...");
                    return;
                }
            }

            OnConsoleOutput?.Invoke(
                $"Starting to download file \"{fileText}\"...");
            Downloader.Run(
                string.IsNullOrWhiteSpace(file.FileName)
                    ? MainFolder
                    : Path.Combine(file.Parent != null ? file.Parent.GetPath(MainFolder) : MainFolder, file.FileName),
                file.Link,
                string.IsNullOrWhiteSpace(file.FileName), f => OnProgressChanged?.Invoke(f, file),
                s => OnConsoleOutput?.Invoke(s));
            if (RunCommands && !BlueAir.DisableCommands &&
                !string.IsNullOrWhiteSpace(CommandToExecuteAfter.Command))
            {
                OnConsoleOutput?.Invoke(
                    $"Execute after command to download file \"{fileText}\"...");
                var after_info = new ProcessStartInfo(CommandToExecuteAfter.Command)
                {
                    UseShellExecute = true,
                    RedirectStandardOutput = true
                };
                var after_process = new Process { StartInfo = after_info };
                after_process.ErrorDataReceived += (_, args) => OnConsoleOutput?.Invoke(args.Data);
                after_process.OutputDataReceived += (_, args) => OnConsoleOutput?.Invoke(args.Data);
                after_process.Start();
                after_process.WaitForExit();
                if (!CommandToExecuteAfter.CanFail && after_process.ExitCode != 0)
                {
                    OnConsoleOutput?.Invoke(
                        $"Exit code of after process on file \"{fileText}\" is not 0. Skipping item...");
                    return;
                }
            }

            OnConsoleOutput?.Invoke(
                $"Finished downloading file \"{fileText}\".");
        }

        #endregion Start Helpers

        #endregion Start
    }

    /// <summary>
    ///     Used in <see cref="DownloadInfo.CommandToExecuteBefore" /> and <see cref="DownloadInfo.CommandToExecuteAfter" />.
    /// </summary>
    public class CommandToExecute
    {
        /// <summary>
        ///     The command itself.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        ///     Determines if the command is allowed to fail or not.
        /// </summary>
        public bool CanFail { get; set; }
    }
}