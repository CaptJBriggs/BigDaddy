using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace BigDaddy
{
    // Saves and restores the user's last valid settings in a small local JSON file.
    internal class BigDaddySettingsStore
    {
        // Full path to this application's per-user settings file.
        private readonly string filePath;

        public BigDaddySettingsStore()
        {
            filePath = Path.Combine(Application.LocalUserAppDataPath, "settings.json");
        }

        // Attempts to load saved settings. False means there are no usable saved settings.
        public bool TryLoad(out BigDaddySettings settings)
        {
            settings = new BigDaddySettings();

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                string savedSettings = File.ReadAllText(filePath);
                BigDaddySettings? loadedSettings =
                    JsonSerializer.Deserialize<BigDaddySettings>(savedSettings);

                if (loadedSettings == null || !loadedSettings.TryValidate(out _))
                {
                    return false;
                }

                settings = loadedSettings;
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        // Attempts to save settings and supplies a friendly message if Windows blocks the save.
        public bool TrySave(BigDaddySettings settings, out string errorMessage)
        {
            try
            {
                string? folderPath = Path.GetDirectoryName(filePath);

                if (folderPath != null)
                {
                    Directory.CreateDirectory(folderPath);
                }

                string savedSettings = JsonSerializer.Serialize(settings);
                File.WriteAllText(filePath, savedSettings);

                errorMessage = string.Empty;
                return true;
            }
            catch (IOException)
            {
                errorMessage = "The settings are valid, but they could not be saved.";
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "The settings are valid, but this Windows account cannot save them.";
                return false;
            }
        }
    }
}
