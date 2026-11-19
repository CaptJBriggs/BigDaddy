using System.Diagnostics;

namespace BigDaddy
{
    // Uses standard Windows commands for the two deliberate session actions.
    internal static class BigDaddyWindowsSessionService
    {
        public static bool TrySignOut()
        {
            return TryStart("shutdown.exe", "/l");
        }

        // tsdiscon returns the current desktop to Windows' sign-in screen without ending its session.
        public static bool TrySwitchUser()
        {
            return TryStart("tsdiscon.exe", string.Empty);
        }

        private static bool TryStart(string fileName, string arguments)
        {
            try
            {
                Process.Start(new ProcessStartInfo(fileName, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }
    }
}
