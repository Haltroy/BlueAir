using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace BlueAir
{
    /// <summary>
    ///     Static class that handles settings and loaded agents.
    /// </summary>
    public static class BlueAir
    {
        #region Private Properties

        private static CustomSetting[] CustomSettings { get; set; }

        #endregion Private Properties

        #region Private Voids

        private static void LoadXml(XmlNode root)
        {
            if (root.ChildNodes.Count <= 0) return;
            foreach (XmlNode node in root.ChildNodes)
                switch (node.Name.ToLowerInvariant())
                {
                    case "customenvironments":
                        foreach (XmlNode env_node in node.ChildNodes)
                        {
                            if (env_node.Attributes is null) continue;
                            Environment.SpecialFolder env = 0;
                            var val = string.Empty;
                            foreach (XmlAttribute env_attr in env_node.Attributes)
                                switch (env_attr.Name.ToLowerInvariant())
                                {
                                    case "id":
                                        if (int.TryParse(env_attr.InnerXml, NumberStyles.None, null, out var envID))
                                            env = (Environment.SpecialFolder)envID;
                                        break;

                                    case "value":
                                        val = env_attr.InnerXml;
                                        break;
                                }

                            var envs = CustomFolders.Where(it => it.SpecialFolder == env).ToArray();
                            foreach (var envs_env in envs) envs_env.NewPath = val;
                        }

                        break;

                    case "settings":
                        var settings = new List<CustomSetting>();
                        foreach (XmlNode setting_node in node.ChildNodes)
                        {
                            if (setting_node.Attributes is null) continue;
                            var name = string.Empty;
                            var sval = string.Empty;
                            foreach (XmlAttribute setting_attr in setting_node.Attributes)
                                switch (setting_attr.Name.ToLowerInvariant())
                                {
                                    case "name":
                                        name = setting_attr.InnerXml;
                                        break;

                                    case "value":
                                        sval = setting_attr.InnerXml;
                                        break;
                                }

                            settings.Add(new CustomSetting { Name = name, Value = sval });
                        }

                        CustomSettings = settings.ToArray();
                        break;
                }
        }

        #endregion Rpivate Voids

        #region Public Voids

        /// <summary>
        ///     Initializes the BlueAir system.
        /// </summary>
        public static void Init()
        {
            var folders = new List<EnvironmentFolder>();
            foreach (Environment.SpecialFolder env_var in Enum.GetValues(typeof(Environment.SpecialFolder)))
                folders.Add(new EnvironmentFolder(env_var, string.Empty));

            CustomFolders = folders.ToArray();

            var agents = new List<DownloadAgent>();

            if (Directory.Exists(SystemAgentsPath))
            {
                var systemAgents = Directory.GetFiles(SystemAgentsPath, "*.*", SearchOption.AllDirectories);
                foreach (var agent_file in systemAgents) agents.Add(new DownloadAgent(agent_file));
            }

            if (Directory.Exists(UserAgentsPath))
            {
                var userAgents = Directory.GetFiles(UserAgentsPath, "*.*", SearchOption.AllDirectories);
                foreach (var agent_file in userAgents) agents.Add(new DownloadAgent(agent_file));
            }

            Agents = agents.ToArray();

            if (File.Exists(SystemSettingsPath))
            {
                var doc = new XmlDocument();
                using (var systemFile = new FileStream(SystemSettingsPath, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite))
                {
                    doc.Load(systemFile);
                }

                if (doc.DocumentElement != null) LoadXml(doc.DocumentElement);
            }

            if (!File.Exists(UserSettingsPath)) return;

            var userDoc = new XmlDocument();
            using (var userFile = new FileStream(UserSettingsPath, FileMode.Open, FileAccess.Read,
                       FileShare.ReadWrite))
            {
                userDoc.Load(userFile);
            }

            if (userDoc.DocumentElement != null) LoadXml(userDoc.DocumentElement);
        }

        /// <summary>
        ///     Saves the current settings.
        /// </summary>
        public static void Save()
        {
            var xml = $"<?xml version='1.0' encoding='utf-8' ?>{Environment.NewLine}<root>{Environment.NewLine}";

            xml += $"<CustomEnvironments>{Environment.NewLine}";
            foreach (var env_var in CustomFolders)
                if (!string.IsNullOrWhiteSpace(env_var.NewPath))
                    xml +=
                        $"<CustomEnv ID=\"{(int)env_var.SpecialFolder}\" Value=\"{env_var.NewPath}\" />{Environment.NewLine}";
            xml += $"</CustomEnvironments>{Environment.NewLine}";

            xml += $"<Settings>{Environment.NewLine}";

            foreach (var settings in CustomSettings)
                xml += $"<Setting Name=\"{settings.Name}\" Value=\"{settings.Value}\" />{Environment.NewLine}";

            xml += $"</Settings>{Environment.NewLine}</root>";

            using (var stream = new FileStream(UserSettingsPath,
                       File.Exists(UserSettingsPath) ? FileMode.Create : FileMode.Truncate, FileAccess.Write,
                       FileShare.ReadWrite))
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.WriteLine(xml);
                }
            }
        }

        /// <summary>
        ///     Installs an agent.
        /// </summary>
        /// <param name="file">Path of the agent.</param>
        /// <returns>
        ///     <see cref="DownloadAgent" />
        /// </returns>
        public static DownloadAgent InstallAgent(string file)
        {
            if (!Directory.Exists(UserAgentsPath)) Directory.CreateDirectory(UserAgentsPath);
            var newFile = Path.Combine(UserAgentsPath, Path.GetFileName(file));
            File.Copy(file, newFile);
            var agents = Agents;
            Array.Resize(ref agents, agents.Length + 1);
            var agent = new DownloadAgent(newFile);
            agents[agents.Length - 1] = agent;
            Agents = agents;
            return agent;
        }

        /// <summary>
        ///     Uninstalls an agent.
        /// </summary>
        /// <param name="agent">Agent to uninstall.</param>
        public static void UninstallAgent(DownloadAgent agent)
        {
            Agents = Agents.Except(new[] { agent }).ToArray();
            if (File.Exists(agent.File)) File.Delete(agent.File);
        }

        #endregion Public Voids

        #region Paths

        private static string UserRootPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haltroy",
                "blueair");

        private static string SystemRootPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "haltroy",
                "blueair");

        private static string UserAgentsPath =>
            Path.Combine(UserRootPath, "agents");

        private static string SystemAgentsPath =>
            Path.Combine(SystemRootPath, "agents");


        private static string UserSettingsPath =>
            Path.Combine(UserRootPath, "settings.xml");

        private static string SystemSettingsPath =>
            Path.Combine(SystemRootPath, "settings.xml");

        #endregion Paths

        #region Public Properties

        /// <summary>
        ///     Disables commands to execute.
        /// </summary>
        public static bool DisableCommands { get; set; }

        /// <summary>
        ///     Array of all agents currently loaded.
        /// </summary>
        public static DownloadAgent[] Agents { get; private set; } = { };

        /// <summary>
        ///     Array of all usable agents currently loaded.
        /// </summary>
        public static DownloadAgent[] WorkingAgents => Agents.Where(it => it.Exists() && it.IsEnabled).ToArray();

        /// <summary>
        ///     List of custom folders.
        /// </summary>
        public static EnvironmentFolder[] CustomFolders { get; private set; }

        #endregion Public Properties

        #region Settings

        /// <summary>
        ///     Tries to find a specific setting, if not ofund creates and registers it.
        /// </summary>
        /// <param name="settingName">Name of the setting.</param>
        /// <param name="defaultValue">Default value of the setting if not found.</param>
        /// <returns>
        ///     <see cref="CustomSetting" />
        /// </returns>
        public static CustomSetting FindSetting(string settingName, object defaultValue = null)
        {
            if (CustomSettings is null)
            {
                CustomSettings = Array.Empty<CustomSetting>();
                return NewSetting(settingName, defaultValue);
            }

            if (CustomSettings.Length <= 0) return NewSetting(settingName);
            var setting = CustomSettings.Where(it =>
                string.Equals(it.Name, settingName, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            if (setting.Length > 0)
                return setting[0];

            return NewSetting(settingName, defaultValue);
        }

        private static CustomSetting NewSetting(string settingName, object defaultValue = null)
        {
            var newSetting = new CustomSetting { Name = settingName, Value = defaultValue };
            var newCustomSettings = CustomSettings;
            Array.Resize(ref newCustomSettings, CustomSettings.Length + 1);
            newCustomSettings[newCustomSettings.Length - 1] = newSetting;
            CustomSettings = newCustomSettings;
            return newSetting;
        }

        #endregion Settings
    }

    #region Classes

    /// <summary>
    ///     Custom Environment Folder.
    /// </summary>
    public class EnvironmentFolder
    {
        /// <summary>
        ///     Creates a new <see cref="EnvironmentFolder" />.
        /// </summary>
        public EnvironmentFolder()
        {
        }

        /// <summary>
        ///     Creates a new <see cref="EnvironmentFolder" />.
        /// </summary>
        /// <param name="specialFolder">Special folder itself.</param>
        /// <param name="newPath">Path to use when called.</param>
        public EnvironmentFolder(Environment.SpecialFolder specialFolder, string newPath)
        {
            SpecialFolder = specialFolder;
            NewPath = newPath;
        }

        /// <summary>
        ///     <see cref="Environment.SpecialFolder" />.
        /// </summary>
        public Environment.SpecialFolder SpecialFolder { get; set; } = 0;

        /// <summary>
        ///     New Path to use when called.
        /// </summary>
        public string NewPath { get; set; } = string.Empty;

        /// <summary>
        ///     Gets the path of the environment folder.
        /// </summary>
        public string GetPath =>
            string.IsNullOrWhiteSpace(NewPath) ? Environment.GetFolderPath(SpecialFolder) : NewPath;
    }

    /// <summary>
    ///     Setting that can be applied by applications that use BlueAir on a BlueAir related option.
    /// </summary>
    public class CustomSetting
    {
        private object _value;

        /// <summary>
        ///     Name of the setting.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Value of the setting. Setting this property auto-saves the BlueAir.
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                BlueAir.Save();
            }
        }
    }

    #endregion Classes
}