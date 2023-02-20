using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.Compression;


namespace MediaRecorder_FFT.ViewModel
{
    internal class MicroPhRecorder
    {
        public WaveFormat recordingFormat;
        public WaveIn microphoneWave;
        public BufferedWaveProvider audioBuffer;
        public DispatcherTimer timer;
        //public WaveFileWriter  writer;

        private readonly int _sampleRate;
        private readonly int _channels;
        

        public MicroPhRecorder(int samplerate = 48000, int channels = 1, int buffermilliseconds = 5000)
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
        private Queue<byte[]> audioData { get; set; }= new Queue<byte[]>();

        // Button Commands
        public ICommand Start => new RelayCommand(StartRecording, statusRecording);
        public ICommand Stop => new RelayCommand(StopRecording, statusRecording);

        private void StartRecording(object parameter)
        {
            microphoneWave.DataAvailable += AudioDataAvailable;

            // Start recording
            microphoneWave.StartRecording();
            timer.Tick += DispatcherTimer_Tick;
            timer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            var data = new byte[audioBuffer.BufferLength];
            var bytesCount = audioBuffer.Read(buffer:data, offset:0, count:data.Length);

            if (bytesCount > 0)
            {
                Console.WriteLine("Processing Data");
                var writer = new WaveFileWriter(@"D:\\Audio\\Test0001.wav", recordingFormat);
                if (writer != null)
                {
                    
                    writer.Write(data, 0, bytesCount);
                    writer.Flush();
                    writer.Dispose();
                }
                //audioBuffer.ClearBuffer();
            }
            
        }

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            audioBuffer.AddSamples(buffer: e.Buffer, offset: 0, count: e.BytesRecorded);
            // WaveFileWriter writer = (WaveFileWriter)sender;


        }

        private bool statusRecording(object parameter)
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
