using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using MediaRecorder_FFT.ViewModel;
using NAudio.Wave;
using NAudio.Wave.Compression;
using ScottPlot;


namespace MediaRecorder_FFT
{
    internal class MicroPhRecorder
    {
        public WaveFormat recordingFormat;
        public WaveIn microphoneWave;
        public BufferedWaveProvider audioBuffer;
        public DispatcherTimer timer;
        private Plot _plt;

        
        
        //public WaveFileWriter  writer;

        private readonly int _sampleRate;
        private readonly int _channels;

        

        public MicroPhRecorder(Plot plt, int samplerate = 48000, int channels = 1, int buffermilliseconds = 100)
        {
            
            _sampleRate = samplerate; //in KHZ
            _channels = channels; // mono channel

            recordingFormat = new WaveFormat(_sampleRate, _channels);

            // For storing the incoming Audio data into buffer
            audioBuffer = new BufferedWaveProvider(recordingFormat)
            {
                BufferDuration = TimeSpan.FromMilliseconds(buffermilliseconds),
                DiscardOnBufferOverflow= true
            };

            _plt = plt;
            microphoneWave = new WaveIn
            {
                DeviceNumber= 0,
                WaveFormat = recordingFormat,
                BufferMilliseconds = buffermilliseconds

            };

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(buffermilliseconds),
            };
            //WaveFileWriter writer = new WaveFileWriter("output.wav", recordingFormat);

        }
        
        // queue to hold the recorded audio data
        private Queue<byte[]> AudioDataQueue { get; set; }= new Queue<byte[]>();

        // Button Commands
        public ICommand Start => new RelayCommand(StartRecording, StatusRecording);
        public ICommand Stop => new RelayCommand(StopRecording, StatusRecording);
        
        private void StartRecording(object parameter)
        {
            //var _plt = new Plot(600, 400);
            microphoneWave.DataAvailable += AudioDataAvailable;
            

            // Start recording
            timer.Tick += DispatcherTimer_Tick;
            microphoneWave.StartRecording();
            timer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            var data = new byte[audioBuffer.BufferLength];
            var bytesCount = audioBuffer.Read(buffer:data, offset:0, count:data.Length);

            if (bytesCount > 0)
            {

                Console.WriteLine("Processing Data");
                //AudioDataQueue.Enqueue(data);
               //var audioBytes = AudioDataQueue.Dequeue();
               
                // convert audio bytes to doubles
                var audioDoubles = new double[bytesCount/ 2]; //Total Samples
                for (int i = 0; i < bytesCount; i += 2)
                {
                    short audioSample = BitConverter.ToInt16(data, i);
                    audioDoubles[i / 2] = (double)audioSample / (double)short.MaxValue;
                }
                //var _plt = new Plot(600, 400);
                //// create new SignalPlot with audio data
                _plt.AddSignal(audioDoubles, recordingFormat.SampleRate);

                //// set plot axis labels
                _plt.XLabel("Time (s)");
                _plt.YLabel("Amplitude");
                _plt.Title("Recorded Audio");
                _plt.AxisAuto(0);
                //_plt.AxisAuto(1);
                


                //// render plot
                _plt.Render();
                _plt.SaveFig(@"D:\\Audio\\audio.png");
                ////var writer = new WaveFileWriter(@"D:\\Audio\\Test0001.wav", recordingFormat);
                ////if (writer != null)
                ////{

                ////    writer.Write(data, 0, bytesCount);
                ////    writer.Flush();
                ////    writer.Dispose();
                ////}
                //audioBuffer.ClearBuffer();

            }

        }

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            AudioDataQueue.Enqueue(e.Buffer);
            audioBuffer.AddSamples(buffer: e.Buffer, offset: 0, count: e.BytesRecorded);
            


        }

        private bool StatusRecording(object parameter)
        {
            return true;
        }



        private void StopRecording(object parameter)
        {
            microphoneWave.StopRecording();
            timer.Stop();
        }
    }
}
