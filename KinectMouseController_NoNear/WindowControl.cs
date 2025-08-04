using System;
using System.Runtime.InteropServices;

namespace KinectMouseController_NoNear
{
    internal static class WindowControl
    {
        private const int SW_MINIMIZE = 6;
        private const int SW_MAXIMIZE = 3;
        private const uint WM_CLOSE = 0x0010;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        public static void MinimizeActiveWindow()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_MINIMIZE);
            }
        }

        public static void MaximizeActiveWindow()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_MAXIMIZE);
            }
        }

        public static void CloseActiveWindow()
        {
            var hWnd = GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
