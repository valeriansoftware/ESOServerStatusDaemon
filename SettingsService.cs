using System;
using System.IO;
using System.Text.Json;

namespace ESOServerStatusDaemon
{
    public sealed class AppSettings
    {
        public bool NotificationsEnabled { get; set; } = true;
        public bool AutoStartEnabled { get; set; } = false;
    }

    public interface ISettingsService
    {
        bool NotificationsEnabled { get; set; }
        bool AutoStartEnabled { get; set; }
        void Save();
        event EventHandler? SettingsChanged;
    }

    public sealed class SettingsService : ISettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _settings;

        public SettingsService()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ESOServerStatusDaemon");
            Directory.CreateDirectory(dir);
            _settingsPath = Path.Combine(dir, "settings.json");
            _settings = Load();
        }

        public bool NotificationsEnabled
        {
            get => _settings.NotificationsEnabled;
            set
            {
                if (_settings.NotificationsEnabled == value) return;
                _settings.NotificationsEnabled = value;
                Save();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool AutoStartEnabled
        {
            get => _settings.AutoStartEnabled;
            set
            {
                if (_settings.AutoStartEnabled == value) return;
                _settings.AutoStartEnabled = value;
                Save();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? SettingsChanged;

        public void Save()
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }

        private AppSettings Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var s = JsonSerializer.Deserialize<AppSettings>(json);
                    if (s != null) return s;
                }
            }
            catch
            {
                // ignore malformed files
            }
            return new AppSettings();
        }
    }
}


