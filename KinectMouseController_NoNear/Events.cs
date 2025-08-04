using System;

namespace KinectMouseController_NoNear
{
    public class ColorFrameEventArgs : EventArgs
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public byte[] Pixels { get; set; } // Bgr32
    }

    public class HandDataEventArgs : EventArgs
    {
        public double X { get; set; } // normalized 0..1
        public double Y { get; set; }
        public double DeltaX { get; set; } // per-frame delta in normalized space
        public double DeltaY { get; set; }
        public bool IsGripped { get; set; }
        public bool IsTracked { get; set; }
        public string Status { get; set; }
    }
}
