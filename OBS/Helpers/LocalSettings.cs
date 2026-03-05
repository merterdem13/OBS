using System;
using System.IO;
using Newtonsoft.Json;

namespace OBS.Helpers
{
    public class SettingsData
    {
        public string? LastSeenReleaseNotesVersion { get; set; }
    }

    public static class LocalSettings
    {
        private static readonly string SettingsFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlparslanOBS");

        private static readonly string SettingsFilePath = Path.Combine(SettingsFolderPath, "settings.json");

        private static SettingsData _current = new SettingsData();

        public static SettingsData Current
        {
            get => _current;
        }

        static LocalSettings()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var data = JsonConvert.DeserializeObject<SettingsData>(json);
                    if (data != null)
                    {
                        _current = data;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ayarlar okunamadı: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsFolderPath))
                {
                    Directory.CreateDirectory(SettingsFolderPath);
                }

                string json = JsonConvert.SerializeObject(_current, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ayarlar kaydedilemedi: {ex.Message}");
            }
        }
    }
}
