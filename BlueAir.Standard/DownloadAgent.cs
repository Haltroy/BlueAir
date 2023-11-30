using System;
using System.IO;
using System.Xml;

namespace BlueAir
{
    public class DownloadAgent
    {
        public DownloadAgent()
        {
        }

        public DownloadAgent(string command, string[] filesToSearch)
        {
            Command = command;
            FilesToSearch = filesToSearch;
        }

        public DownloadAgent(string file)
        {
            if (!File.Exists(file)) throw new FileNotFoundException(null, file);
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

        public DownloadAgent(XmlNode node)
        {
            ParseXml(node);
        }

        public bool IsEnabled { get; set; } = true;
        public string Name { get; set; }
        public string Command { get; set; } = string.Empty;
        public string[] FilesToSearch { get; set; } = Array.Empty<string>();

        private void ParseXml(XmlNode node)
        {
            throw new NotImplementedException();
        }

        public string ToXml()
        {
            throw new NotImplementedException();
        }

        public bool Exists()
        {
            throw new NotImplementedException();
        }
    }
}