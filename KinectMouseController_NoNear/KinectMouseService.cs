using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;

namespace KinectMouseController_NoNear
{
    public class KinectMouseService : IDisposable, IInteractionClient
    {
        public event EventHandler<string> StatusChanged;
        public event EventHandler<ColorFrameEventArgs> ColorFrameArrived;
        public event EventHandler<HandDataEventArgs> HandDataUpdated;

        public KinectSensor Sensor { get; private set; }

        private InteractionStream _interaction;
        private DepthImagePixel[] _depthPixels;
        private Skeleton[] _skeletons;
        private byte[] _colorPixels;
        private bool _started;

        // smoothing and deltas
        private double _sx, _sy;
        private bool _hasSmoothed;
        private const double Alpha = 0.35;
        private readonly Dictionary<long, bool> _grip = new Dictionary<long, bool>();

        public void Start()
        {
            if (_started) return;

            Sensor = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if (Sensor == null) { OnStatus("No Kinect sensor found."); return; }

            try
            {
                // Enable streams
                Sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                Sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                Sensor.SkeletonStream.Enable();
                // Upper-body seated only
                Sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

                Sensor.Start();

                // Create InteractionStream after sensor is running
                _interaction = new InteractionStream(Sensor, this);
                _interaction.InteractionFrameReady += OnInteractionFrameReady;
                Sensor.AllFramesReady += OnAllFramesReady;

                _started = true;
                OnStatus("Kinect started. Seated mode enabled. (Near mode disabled by design)");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.IndexOf("KinectInteraction", StringComparison.OrdinalIgnoreCase) >= 0)
                    msg += " (Ensure Kinect Developer Toolkit 1.8 is installed and KinectInteraction180_32.dll is in PATH or next to the exe)";
                OnStatus("Failed to start Kinect: " + msg);
                Stop();
            }
        }

        public void Stop()
        {
            if (!_started) return;

            if (_interaction != null)
            {
                _interaction.InteractionFrameReady -= OnInteractionFrameReady;
                _interaction.Dispose();
                _interaction = null;
            }

            if (Sensor != null)
            {
                Sensor.AllFramesReady -= OnAllFramesReady;
                if (Sensor.IsRunning) Sensor.Stop();
                Sensor = null;
            }

            _started = false;
            OnStatus("Kinect stopped.");
        }

        private void OnAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (var cf = e.OpenColorImageFrame())
            {
                if (cf != null)
                {
                    if (_colorPixels == null || _colorPixels.Length != cf.PixelDataLength)
                        _colorPixels = new byte[cf.PixelDataLength];

                    cf.CopyPixelDataTo(_colorPixels);
                    ColorFrameArrived?.Invoke(this, new ColorFrameEventArgs
                    {
                        Width = cf.Width,
                        Height = cf.Height,
                        Pixels = _colorPixels
                    });
                }
            }

            using (var df = e.OpenDepthImageFrame())
            using (var sf = e.OpenSkeletonFrame())
            {
                if (df == null || sf == null) return;

                if (_depthPixels == null || _depthPixels.Length != df.PixelDataLength)
                    _depthPixels = new DepthImagePixel[df.PixelDataLength];
                if (_skeletons == null || _skeletons.Length != sf.SkeletonArrayLength)
                    _skeletons = new Skeleton[sf.SkeletonArrayLength];

                df.CopyDepthImagePixelDataTo(_depthPixels);
                sf.CopySkeletonDataTo(_skeletons);

                _interaction.ProcessDepth(_depthPixels, df.Timestamp);
                var accel = Sensor.AccelerometerGetCurrentReading();
                _interaction.ProcessSkeleton(_skeletons, accel, sf.Timestamp);
            }
        }

        private void OnInteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (var frame = e.OpenInteractionFrame())
            {
                if (frame == null) return;

                var userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
                frame.CopyInteractionDataTo(userInfos);

                foreach (var user in userInfos)
                {
                    if (user.SkeletonTrackingId == 0) continue;

                    InteractionHandPointer hp = user.HandPointers
                        .OrderByDescending(h => h.HandType == InteractionHandType.Right) // prefer right
                        .FirstOrDefault();

                    if (hp == null) continue;

                    double nx = hp.X;
                    double ny = hp.Y;

                    double dx = 0, dy = 0;
                    if (!_hasSmoothed) { _sx = nx; _sy = ny; _hasSmoothed = true; }
                    else
                    {
                        dx = nx - _sx; dy = ny - _sy;
                        _sx = _sx + Alpha * (nx - _sx);
                        _sy = _sy + Alpha * (ny - _sy);
                    }

                    if (hp.HandEventType == InteractionHandEventType.Grip) _grip[user.SkeletonTrackingId] = true;
                    if (hp.HandEventType == InteractionHandEventType.GripRelease) _grip[user.SkeletonTrackingId] = false;

                    bool isGrip = _grip.ContainsKey(user.SkeletonTrackingId) ? _grip[user.SkeletonTrackingId] : false;

                    HandDataUpdated?.Invoke(this, new HandDataEventArgs
                    {
                        X = _sx,
                        Y = _sy,
                        DeltaX = dx,
                        DeltaY = dy,
                        IsGripped = isGrip,
                        IsTracked = true,
                        Status = isGrip ? "Grip/Scroll" : "Mouse"
                    });
                    return;
                }

                HandDataUpdated?.Invoke(this, new HandDataEventArgs
                {
                    IsTracked = false,
                    Status = "No user tracked"
                });
            }
        }

        private void OnStatus(string s) => StatusChanged?.Invoke(this, s);
        public void Dispose() => Stop();

        // Everything is a valid interaction target so the stream stays engaged
        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            return new InteractionInfo
            {
                IsGripTarget = true,
                IsPressTarget = true,
                PressTargetControlId = 1,
                PressAttractionPointX = x,
                PressAttractionPointY = y
            };
        }
    }
}
