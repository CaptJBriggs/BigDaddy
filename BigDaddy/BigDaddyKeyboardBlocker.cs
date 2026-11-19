using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BigDaddy
{
    // Suppresses ordinary keyboard input while preserving Windows security shortcuts.
    internal class BigDaddyKeyboardBlocker : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private readonly LowLevelKeyboardProc hookCallback;
        private IntPtr hookHandle;

        public BigDaddyKeyboardBlocker()
        {
            hookCallback = HookCallback;
        }

        public void Start()
        {
            if (hookHandle != IntPtr.Zero)
            {
                return;
            }

            hookHandle = SetWindowsHookEx(WhKeyboardLl, hookCallback, IntPtr.Zero, 0);
        }

        public void Stop()
        {
            if (hookHandle == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(hookHandle);
            hookHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            Stop();
        }

        private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
        {
            int message = wParam.ToInt32();

            if (code >= 0 &&
                (message == WmKeyDown || message == WmKeyUp ||
                 message == WmSysKeyDown || message == WmSysKeyUp))
            {
                return (IntPtr)1;
            }

            return CallNextHookEx(hookHandle, code, wParam, lParam);
        }

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook, LowLevelKeyboardProc callback, IntPtr moduleHandle, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam);
    }
}
