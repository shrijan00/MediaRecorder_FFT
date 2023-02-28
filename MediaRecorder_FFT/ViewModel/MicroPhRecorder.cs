using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using MediaRecorder_FFT.ViewModel;
using NAudio.Gui;
using NAudio.Wave;
using NAudio.Wave.Compression;
using ScottPlot;
using ScottPlot.Plottable;

namespace MediaRecorder_FFT
{
    internal class MicroPhRecorder
    {
        private WaveFormat _recordingFormat;
        private WaveIn _microphoneWave;
        private BufferedWaveProvider _audioBuffer;
        private DispatcherTimer _timer;
        private WpfPlot _plt;

        private readonly int _sampleRate;
        private readonly int _channels;

        

        public MicroPhRecorder(WpfPlot plt, int samplerate = 48000, int channels = 1, int buffermilliseconds = 100)
        {
            
            _sampleRate = samplerate; //in KHZ
            _channels = channels; // mono channel
            


            _recordingFormat = new WaveFormat(_sampleRate, _channels);

            // For storing the incoming Audio data into buffer
            _audioBuffer = new BufferedWaveProvider(_recordingFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(buffermilliseconds),
                DiscardOnBufferOverflow= true
            };

            _plt = plt;
            _plt.Plot.XLabel("Time (s)");
            _plt.Plot.YLabel("Amplitude");
            _plt.Plot.Title("Recorded Audio");
            //_plt.AxisAuto(0);
            _microphoneWave = new WaveIn
            {
                DeviceNumber= 0,
                WaveFormat = _recordingFormat,
                BufferMilliseconds = buffermilliseconds

            };

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(buffermilliseconds),
            };
            //WaveFileWriter writer = new WaveFileWriter("output.wav", _recordingFormat);

        }
        

        // Button Commands
        public ICommand Start => new RelayCommand(StartRecording, StatusRecording);
        public ICommand Stop => new RelayCommand(StopRecording, StatusRecording);
        
        private void StartRecording(object parameter)
        {
            // Create Event when new Audio data is available
            _microphoneWave.DataAvailable += AudioDataAvailable;

            //
            // Start recording
            _timer.Tick += DispatcherTimer_Tick;
            _microphoneWave.StartRecording();
            _timer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Extract Audio data
            var data = new byte[_audioBuffer.BufferLength];
            
            var bytesCount = _audioBuffer.Read(buffer:data, offset:0, count:data.Length);
            _plt.Plot.Clear();
            if (bytesCount > 0)
            {

                Console.WriteLine("Processing Data");
               
                // convert audio bytes to doubles
                var audioDoubles = new double[bytesCount/ 2]; //Total Samples
                for (int i = 0; i < bytesCount; i += 2)
                {
                    short audioSample = BitConverter.ToInt16(data, i);
                    audioDoubles[i / 2] = (double)audioSample / (double)short.MaxValue;
                }
              
                _plt.Plot.AddSignal(ys:audioDoubles, sampleRate: _recordingFormat.SampleRate, color:System.Drawing.Color.Red);
                //CurrentXEdge = _plt.GetAxisLimits().XMax;
                //_plt.SetAxisLimits(xMin:CurrentXEdge,xMax:CurrentXEdge + 0.1);
                _plt.Plot.AxisAuto();
                _plt.Plot.SetAxisLimitsY(yMin: -1, yMax: 1);
                _plt.Render();

                //Save Plot
                //_plt.SaveFig(@"D:\\Audio\\audio.png");

               _audioBuffer.ClearBuffer();
               

            }

        }

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            // Add New Audio data to Buffer whenever Available
            _audioBuffer.AddSamples(buffer: e.Buffer, offset: 0, count: e.BytesRecorded);

        }

        private bool StatusRecording(object parameter)
        {
            return true;
        }



        private void StopRecording(object parameter)
        {
            _microphoneWave.StopRecording();
            _timer.Stop();
        }
    }
}
