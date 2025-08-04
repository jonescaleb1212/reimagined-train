using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace KinectMouseController_NoNear
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Include the EXE directory in DLL search path
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            SetDllDirectory(exeDir);

            // Try adding Toolkit Redist folder to PATH
            string[] candidates = new[] {
                @"C:\Program Files\Microsoft SDKs\Kinect\Developer Toolkit v1.8.0\Redist",
                @"C:\Program Files (x86)\Microsoft SDKs\Kinect\Developer Toolkit v1.8.0\Redist"
            };
            foreach (var c in candidates)
            {
                if (Directory.Exists(c))
                {
                    var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    var parts = (path ?? string.Empty).Split(';');
                    bool has = Array.Exists(parts, p => string.Equals(p, c, StringComparison.OrdinalIgnoreCase));
                    if (!has)
                        Environment.SetEnvironmentVariable("PATH", c + ";" + path);
                    break;
                }
            }

            // Preflight: try to load native Interaction DLL so we fail early with a clear message
            var dllName = "KinectInteraction180_32.dll";
            if (IntPtr.Zero == LoadLibrary(dllName))
            {
                MessageBox.Show(
                    $"{dllName} not found. Put it next to the EXE (bin\\Debug) or add the Kinect Developer Toolkit v1.8 Redist folder to PATH.",
                    "Native DLL Missing",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
