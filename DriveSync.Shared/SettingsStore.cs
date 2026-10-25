using System;
using System.IO;
using Newtonsoft.Json;

namespace DriveSync.Shared
{
    public static class SettingsStore
    {
        public static string FilePath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "DriveSync", "settings.json");
            }
        }

        public static SyncSettings Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new SyncSettings();

                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<SyncSettings>(json) ?? new SyncSettings();
            }
            catch
            {
                return new SyncSettings();
            }
        }

        public static void Save(SyncSettings settings)
        {
            var directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }
    }
}
