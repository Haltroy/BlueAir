using System;
using System.IO;
using System.Linq;

namespace BlueAir
{
    public static class BlueAir
    {
        public static string UserRootPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "haltroy",
                "BlueAir");

        public static string SystemRootPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "haltroy",
                "BlueAir");
        public static bool DisableCommands { get; set; } = false;
        public static DownloadAgent[] Agents { get; set; } = { }; // TODO
        public static DownloadAgent[] WorkingAgents => Agents.Where(it => it.Exists() && it.IsEnabled).ToArray();
        public static EnvironmentFolder[] CustomFolders { get; set; }
        public static CustomSetting[] CustomSettings { get; set; }

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

        public static void Init()
        {
            // TODO: Load Agents here

            // TODO: Load Custom Folders here

            // TODO: Load Custom Settings here
        }

        public static void Save()
        {
            // TODO
        }

        public static DownloadAgent InstallAgent(string file)
        {
            // TODO
            throw new NotImplementedException();
        }

        public static void UninstallAgent(DownloadAgent agent)
        {
            // TODO
        }
    }

    public class EnvironmentFolder
    {
        public EnvironmentFolder()
        {
        }

        public EnvironmentFolder(Environment.SpecialFolder specialFolder, string newPath)
        {
            SpecialFolder = specialFolder;
            NewPath = newPath;
        }

        public Environment.SpecialFolder SpecialFolder { get; set; } = 0;
        public string NewPath { get; set; } = string.Empty;

        public string GetPath =>
            string.IsNullOrWhiteSpace(NewPath) ? Environment.GetFolderPath(SpecialFolder) : NewPath;
    }

    public class CustomSetting
    {
        private object _value;
        public string Name { get; set; }

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
}