using Microsoft.CognitiveServices.SpeechRecognition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpeechRecognition
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MicrophoneRecognitionClient micClient;

        private string SubscriptionKey = "{your-subscription-key}";
        private SpeechRecognitionMode Mode = SpeechRecognitionMode.LongDictation;
        private string DefaultLocale = "en-US";

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (null != micClient)
            {
                micClient.Dispose();
            }

            base.OnClosed(e);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (micClient == null)
                CreateMicrophoneRecoClient();

            micClient.StartMicAndRecognition();
        }

        private void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            micClient.EndMicAndRecognition();
        }

        private void CreateMicrophoneRecoClient()
        {
            micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                Mode,
                DefaultLocale,
                SubscriptionKey);
            micClient.AuthenticationUri = "";

            // Event handlers for speech recognition results
            micClient.OnMicrophoneStatus += OnMicrophoneStatus;
            micClient.OnPartialResponseReceived += OnPartialResponseReceivedHandler;
            micClient.OnResponseReceived += OnMicDictationResponseReceivedHandler;
            micClient.OnConversationError += OnConversationErrorHandler;
        }

        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Recording)
                {
                    WriteLine("Please start speaking ... :)");
                }

                WriteLine("");
            });
        }

        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            WriteLine("{0}", e.PartialResult);
            WriteLine("");
        }

        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke(
                    (Action)(() =>
                    {
                        micClient.EndMicAndRecognition();
                    }));
            }

            if (e.PhraseResponse.Results.Length > 0)
            {
                WriteLine(e.PhraseResponse.Results.OrderByDescending(x => x.Confidence).First().DisplayText);
            }
        }

        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
            WriteLine("Error: {0}", e.SpeechErrorText);
            WriteLine("");
        }

        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                tbLogs.Text += formattedStr + "\n";
                tbLogs.ScrollToEnd();
            });
        }
    }
}
