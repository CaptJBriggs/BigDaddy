using Microsoft.Win32;
using System.Windows.Forms;

namespace BigDaddy
{
    // Registers the current executable to start for the signed-in Windows user.
    internal static class BigDaddyAutoStartService
    {
        private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ValueName = "BigDaddy";

        public static bool TryEnable()
        {
            try
            {
                using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                runKey?.SetValue(ValueName, $"\"{Application.ExecutablePath}\" --background");
                return runKey != null;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
