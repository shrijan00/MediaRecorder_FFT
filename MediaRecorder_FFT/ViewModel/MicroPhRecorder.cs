using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using MediaRecorder_FFT.ViewModel;
using NAudio.Dsp;
using NAudio.Gui;
using NAudio.Wave;
using NAudio.Wave.Compression;
using ScottPlot;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Plottable;



namespace MediaRecorder_FFT
{
    internal class MicroPhRecorder
    {
        private WaveFormat _recordingFormat;
        private WaveIn _microphoneWave;
        private BufferedWaveProvider _audioBuffer;
        private DispatcherTimer _timer;
        private WpfPlot _plt, _fftplt;

        private readonly int _sampleRate;
        private readonly int _channels;



        public MicroPhRecorder(WpfPlot plt, WpfPlot fftplt, int samplerate = 48000, int channels = 1, int buffermilliseconds = 100)
        {

            _sampleRate = samplerate; //in KHZ
            _channels = channels; // mono channelx`



            _recordingFormat = new WaveFormat(_sampleRate, _channels);

            // For storing the incoming Audio data into buffer
            _audioBuffer = new BufferedWaveProvider(_recordingFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(buffermilliseconds),
                DiscardOnBufferOverflow = true
            };

            _plt = plt;
            _plt.Plot.XLabel("Time (s)");
            _plt.Plot.YLabel("Amplitude");
            _plt.Plot.Title("Recorded Audio");
            _plt.Plot.Grid(false);
            _plt.Plot.Style(figureBackground: System.Drawing.Color.Ivory, dataBackground: System.Drawing.Color.DarkSlateBlue);

            _fftplt = fftplt;
            _fftplt.Plot.XLabel("Frequency (kHz)");
            _fftplt.Plot.YLabel("Magnitude");
            _fftplt.Plot.Title("FFT  Audio");
            _fftplt.Plot.Grid(false);
            _fftplt.Plot.Style(figureBackground: System.Drawing.Color.Ivory, dataBackground: System.Drawing.Color.LightCyan);
            //_plt.AxisAuto(0);
            _microphoneWave = new WaveIn
            {
                DeviceNumber = 0,
                WaveFormat = _recordingFormat,
                BufferMilliseconds = buffermilliseconds

            };

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(buffermilliseconds),
            };
            _timer.Tick += DispatcherTimer_Tick;
            //WaveFileWriter writer = new WaveFileWriter("output.wav", _recordingFormat);

        }


        // Button Commands
        public ICommand Start => new RelayCommand(StartRecording, StatusRecording);
        public ICommand Stop => new RelayCommand(StopRecording, StatusRecording);

        private void StartRecording(object parameter)
        {
            // Create Event when new Audio data is available
            // Reset the plot


            //
            // Start recording

            
            _microphoneWave.DataAvailable += AudioDataAvailable;
            _microphoneWave.StartRecording();
            
            _timer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Extract Audio data
            var data = new byte[_audioBuffer.BufferLength];

            var bytesCount = _audioBuffer.Read(buffer: data, offset: 0, count: data.Length);
            _plt.Plot.Clear();
            _fftplt.Plot.Clear();
            if (bytesCount > 0)
            {

                Console.WriteLine("Processing Data");

                // convert audio bytes to doubles
                var audioDoubles = new double[bytesCount / 2]; //Total Samples
                for (int i = 0; i < bytesCount; i += 2)
                {
                    short audioSample = BitConverter.ToInt16(data, i);
                    audioDoubles[i / 2] = (double)audioSample / (double)short.MaxValue;
                }

                Complex[] fftBuffer = CalculateFFT(audioDoubles);
                double[] frequencies = new double[fftBuffer.Length / 2];
                double[] magnitudes = new double[fftBuffer.Length / 2];

                for (int i = 0; i < fftBuffer.Length/2; i++)
                {
                    frequencies[i] = i * _recordingFormat.SampleRate / (double)fftBuffer.Length / 1000; // Convert to kHz
                    magnitudes[i] = Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
                }

                _plt.Plot.AddSignal(ys: audioDoubles, sampleRate: _recordingFormat.SampleRate, color: System.Drawing.Color.Red);
                _fftplt.Plot.AddScatter(xs:frequencies, ys: magnitudes, color:System.Drawing.Color.Orange, markerSize: 1, markerShape: MarkerShape.none);
               // _fftplt.Plot.AddSignal(ys: magnitudes, sampleRate: _recordingFormat.SampleRate, color: System.Drawing.Color.Red);
                //CurrentXEdge = _plt.GetAxisLimits().XMax;
                //_plt.SetAxisLimits(xMin:CurrentXEdge,xMax:CurrentXEdge + 0.1);
                _plt.Plot.AxisAuto();
                _fftplt.Plot.AxisAuto();
                _plt.Plot.SetAxisLimitsY(yMin: -0.5, yMax: 0.5);
                _plt.Render();
                _fftplt.Render();
                //Save Plot

                // _plt.Plot.SaveFig(@"D:\\Audio\\audio.png");

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

        private Complex[] CalculateFFT(double[] audioData)
        {
            // get the length
            int fftLength = (int)Math.Pow(2, Math.Ceiling(Math.Log(audioData.Length, 2)));

            // To hold FFT results
            Complex[] fftBuffer = new Complex[fftLength];

            for (int i = 0; i < audioData.Length; i++)
            {
                fftBuffer[i].X = (float)(audioData[i] * FastFourierTransform.HammingWindow(i, audioData.Length));
                fftBuffer[i].Y = 0;
            }

            //Calculate FFT
            FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftBuffer);

            return fftBuffer;
        }


        private void StopRecording(object parameter)
        {

           
            _microphoneWave.StopRecording();
            // Unsubscribe from the event
            _microphoneWave.DataAvailable -= AudioDataAvailable;
            // Stop Recording
            _timer.Stop();


        }
    }
}
