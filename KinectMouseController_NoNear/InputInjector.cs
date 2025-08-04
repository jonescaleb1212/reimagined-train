using System;
using System.Runtime.InteropServices;

namespace KinectMouseController_NoNear
{
    public static class InputInjector
    {
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        const uint MOUSEEVENTF_WHEEL = 0x0800;
        const uint MOUSEEVENTF_HWHEEL = 0x01000;

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        public static void MoveMouseToNormalized(double nx, double ny)
        {
            nx = Math.Min(1.0, Math.Max(0.0, nx));
            ny = Math.Min(1.0, Math.Max(0.0, ny));

            int x = (int)Math.Round(nx * 65535.0);
            int y = (int)Math.Round(ny * 65535.0);

            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        dx = x,
                        dy = y,
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                    }
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void MouseVerticalWheel(int delta)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = (uint)delta,
                        dwFlags = MOUSEEVENTF_WHEEL
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void MouseHorizontalWheel(int delta)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                mkhi = new MOUSEKEYBDHARDWAREINPUT
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = (uint)delta,
                        dwFlags = MOUSEEVENTF_HWHEEL
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
