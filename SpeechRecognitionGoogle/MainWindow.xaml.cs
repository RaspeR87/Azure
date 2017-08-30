using System;
using System.Collections.Generic;
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
using Google.Cloud.Speech.V1;
using System.Threading;
using System.Diagnostics;

namespace SpeechRecognitionGoogle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string credentialPath = @".\{path-to-credential-file}.json";
        private SpeechClient speech;
        private NAudio.Wave.WaveInEvent waveIn;
        private object writeLock;
        private bool writeMore;
        private SpeechClient.StreamingRecognizeStream streamingCall;
        private Task printResponses;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);

            await FromMicrophone();
        }

        private async Task<object> FromMicrophone()
        {
            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                WriteLine("No microphone!");
                return -1;
            }

            speech = SpeechClient.Create();

            streamingCall = speech.StreamingRecognize();
            // Write the initial request with the config.
            await streamingCall.WriteAsync(
                new StreamingRecognizeRequest()
                {
                    StreamingConfig = new StreamingRecognitionConfig()
                    {
                        Config = new RecognitionConfig()
                        {
                            Encoding =
                            RecognitionConfig.Types.AudioEncoding.Linear16,
                            SampleRateHertz = 16000,
                            LanguageCode = "sl",
                        },
                        InterimResults = true,
                    }
                });
            
            // Print responses as they arrive.
            printResponses = Task.Run(async () =>
            {
                while (await streamingCall.ResponseStream.MoveNext(
                    default(CancellationToken)))
                {
                    foreach (var result in streamingCall.ResponseStream
                        .Current.Results.Where(x => x.IsFinal = true))
                    {
                        WriteLine(result.Alternatives.OrderByDescending(x => x.Confidence).First().Transcript);
                    }
                }
            });

            // Read from the microphone and stream to API.
            writeLock = new object();
            writeMore = true;
            waveIn = new NAudio.Wave.WaveInEvent();
            waveIn.DeviceNumber = 0;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(16000, 1);
            waveIn.DataAvailable +=
                (object sender, NAudio.Wave.WaveInEventArgs args) =>
                {
                    try
                    {
                        lock (writeLock)
                        {
                            if (!writeMore) return;
                            streamingCall.WriteAsync(
                                new StreamingRecognizeRequest()
                                {
                                    AudioContent = Google.Protobuf.ByteString
                                        .CopyFrom(args.Buffer, 0, args.BytesRecorded)
                                }).Wait();
                        }
                    }
                    catch { }
                };
            
            return 0;
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

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            waveIn.StartRecording();
            WriteLine("Please start speaking ... :)");
            WriteLine("");
        }

        private async void btnEnd_Click(object sender, RoutedEventArgs e)
        {
            waveIn.StopRecording();
            lock (writeLock) writeMore = false;
            await streamingCall.WriteCompleteAsync();
            await printResponses;
        }
    }
}
