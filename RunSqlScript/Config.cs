using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace RunSqlScript
{
    public static class Config
    {
        private const string SettingFile = "config.json";

        public static Settings GetSettings()
        {
            Settings settings;
            if (File.Exists(SettingFile))
            {
                var json = File.ReadAllText(SettingFile);
                settings = JsonConvert.DeserializeObject<Settings>(json);
            }
            else
            {
                settings = new Settings();
                settings.Init();
            }

            return settings;
        }

        public static void Save(Settings settings)
        {
            var serialized = JsonConvert.SerializeObject(settings, Formatting.Indented);
            try
            {
                File.WriteAllText(SettingFile, serialized);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK);
            }
        }
    }
}
