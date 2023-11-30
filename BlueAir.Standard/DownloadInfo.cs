using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace BlueAir
{
    public class DownloadInfo
    {
        public delegate void OnProgressChangedDelegate(float percentage, DownloadObject currentItem);

        public DownloadInfo()
        {
        }

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

        public DownloadInfo(Stream stream)
        {
            var doc = new XmlDocument();
            using (var reader = new StreamReader(stream))
            {
                doc.LoadXml(reader.ReadToEnd());
            }

            if (doc.DocumentElement != null) ParseXml(doc.DocumentElement);
        }

        public DownloadInfo(XmlNode node)
        {
            ParseXml(node);
        }

        public bool RunCommands { get; set; } = false;
        public DownloadAgent Downloader { get; set; }
        public CommandToExecute CommandToExecuteBefore { get; set; } = new CommandToExecute();
        public CommandToExecute CommandToExecuteAfter { get; set; } = new CommandToExecute();
        public List<DownloadObject> Downloads { get; set; } = new List<DownloadObject>();

        public event OnProgressChangedDelegate OnProgressChanged;

        private void ParseXml(XmlNode node)
        {
            // TODO
            throw new NotImplementedException();
        }

        public void Start(string MainFolder)
        {
            // TODO
            throw new NotImplementedException();
        }

        public string ToXml(bool includeFinished = false)
        {
            // TODO
            throw new NotImplementedException();
        }
    }

    public class CommandToExecute
    {
        public string Command { get; set; }
        public bool CanFail { get; set; }
    }
}