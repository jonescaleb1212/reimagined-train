using System;
using System.Speech.Recognition;

namespace KinectMouseController_NoNear
{
    public class VoiceCommandService : IDisposable
    {
        private readonly SpeechRecognitionEngine _recognizer;

        public VoiceCommandService()
        {
            _recognizer = new SpeechRecognitionEngine();
            var choices = new Choices("minimize window", "maximize window", "close window");
            var gb = new GrammarBuilder(choices);
            var grammar = new Grammar(gb);
            _recognizer.LoadGrammar(grammar);
            _recognizer.SpeechRecognized += OnSpeechRecognized;
        }

        public void Start()
        {
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Stop()
        {
            _recognizer.RecognizeAsyncStop();
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result.Confidence < 0.8)
            {
                return;
            }

            switch (e.Result.Text.ToLowerInvariant())
            {
                case "minimize window":
                    WindowControl.MinimizeActiveWindow();
                    break;
                case "maximize window":
                    WindowControl.MaximizeActiveWindow();
                    break;
                case "close window":
                    WindowControl.CloseActiveWindow();
                    break;
            }
        }

        public void Dispose()
        {
            Stop();
            _recognizer.Dispose();
        }
    }
}
