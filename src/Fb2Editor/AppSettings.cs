namespace Fb2Editor
{
    using System;
    using System.IO;
    using System.Text.Json;

    // Settings kept between sessions in %AppData%\Fb2Editor\settings.json
    internal sealed class AppSettings
    {
        private static readonly String FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fb2Editor", "settings.json");

        public String RootFolder { get; set; }

        // Missing or damaged settings give the defaults
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(AppSettings.FilePath))
                {
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppSettings.FilePath)) ?? new AppSettings();
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                // The settings are not essential
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppSettings.FilePath));
                File.WriteAllText(AppSettings.FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // The settings are not essential
            }
        }
    }
}
