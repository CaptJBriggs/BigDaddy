using System.Diagnostics;

namespace BigDaddy
{
    // Uses Windows' normal shutdown command after the session state has been saved.
    internal static class BigDaddyShutdownService
    {
        public static bool TryShutdown()
        {
            try
            {
                Process.Start(new ProcessStartInfo("shutdown.exe", "/s /t 0")
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
