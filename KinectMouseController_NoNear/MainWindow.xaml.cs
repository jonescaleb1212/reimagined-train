using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectMouseController_NoNear
{
    public partial class MainWindow : Window
    {
        private KinectMouseService _svc = new KinectMouseService();
        private VoiceCommandService _voice;
        private WriteableBitmap _colorBitmap;
        private byte[] _colorPixels;
        private bool _controlEnabled = true;
        private Ellipse _cursor;
        private double _cursorRadius = 12;
        private bool _isMouseDown;

        public MainWindow() { InitializeComponent(); }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _svc.StatusChanged += (_, msg) => Dispatcher.Invoke(() => StatusText.Text = msg);
            _svc.ColorFrameArrived += OnColorFrame;
            _svc.HandDataUpdated += OnHandDataUpdated;
            _svc.Start();

            _voice = new VoiceCommandService();
            _voice.Start();

            SetupCursorVisual();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _svc.Dispose();
            _voice?.Dispose();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                _controlEnabled = !_controlEnabled;
                StatusText.Text = _controlEnabled ? "Control ENABLED" : "Control DISABLED";
            }
        }

        private void SetupCursorVisual()
        {
            _cursor = new Ellipse { Width = _cursorRadius*2, Height = _cursorRadius*2, Fill = Brushes.Cyan, Opacity = 0.85 };
            Overlay.Children.Add(_cursor);
        }

        private void OnColorFrame(object sender, ColorFrameEventArgs e)
        {
            if (_colorPixels == null)
            {
                _colorPixels = new byte[e.Width * e.Height * 4];
                _colorBitmap = new WriteableBitmap(e.Width, e.Height, 96, 96, PixelFormats.Bgr32, null);
                ColorView.Source = _colorBitmap;
            }

            _colorBitmap.WritePixels(new Int32Rect(0, 0, e.Width, e.Height), e.Pixels, e.Width * 4, 0);
        }

        private void OnHandDataUpdated(object sender, HandDataEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var w = Overlay.ActualWidth > 0 ? Overlay.ActualWidth : this.ActualWidth;
                var h = Overlay.ActualHeight > 0 ? Overlay.ActualHeight : this.ActualHeight;
                double px = e.X * w;
                double py = e.Y * h;

                Canvas.SetLeft(_cursor, px - _cursorRadius);
                Canvas.SetTop(_cursor,  py - _cursorRadius);
                _cursor.Fill = e.IsGripped ? Brushes.OrangeRed : Brushes.Cyan;

                if (!_controlEnabled) return;

                if (!e.IsTracked)
                {
                    if (_isMouseDown)
                    {
                        InputInjector.MouseLeftUp();
                        _isMouseDown = false;
                    }
                    return;
                }

                InputInjector.MoveMouseToNormalized(e.X, e.Y);

                if (e.IsGripped)
                {
                    if (!_isMouseDown)
                    {
                        InputInjector.MouseLeftDown();
                        _isMouseDown = true;
                    }
                }
                else if (_isMouseDown)
                {
                    InputInjector.MouseLeftUp();
                    _isMouseDown = false;
                }
            });
        }
    }
}
