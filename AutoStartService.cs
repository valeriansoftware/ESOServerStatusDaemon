using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ESOServerStatusDaemon
{
    public interface IAutoStartService
    {
        bool IsEnabled();
        void Enable();
        void Disable();
    }

    public sealed class AutoStartService : IAutoStartService
    {
        private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private readonly string _appName = "ESOServerStatusDaemon";
        private readonly string _exePath;

        public AutoStartService()
        {
            _exePath = Process.GetCurrentProcess().MainModule?.FileName ?? System.Reflection.Assembly.GetEntryAssembly()!.Location;
        }

        public bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var value = key?.GetValue(_appName) as string;
            return !string.IsNullOrEmpty(value);
        }

        public void Enable()
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            key.SetValue(_appName, '"' + _exePath + '"');
        }

        public void Disable()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(_appName, false);
        }
    }
}


