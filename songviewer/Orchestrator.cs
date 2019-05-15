using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace songviewer
{
    internal class Orchestrator
    {
        internal const int FFT_SIZE = 1024;
        internal const int FPS = 60;

        public Orchestrator()
        {
        }

        internal List<Complex[]> Load(string fileName)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (AudioFileReader reader = new AudioFileReader(fileName))
            {
                var list = new List<Complex[]>();
                var provider = reader.ToMono().ToWaveProvider().ToSampleProvider();
                while (true) { 
                    // grab first second ONLY.
                    for (var frame = 0; frame < FPS; frame++) {
                        var BUFFER_SIZE = provider.WaveFormat.SampleRate / FPS;
                        float[] buffer = new float[BUFFER_SIZE];
                        var count = provider.Read(buffer, 0, BUFFER_SIZE);
                        if (count != BUFFER_SIZE)
                        {
                            return list;
                        }
                        Complex[] fft_buffer = new Complex[FFT_SIZE];
                        for (int i = 0; i < BUFFER_SIZE; i++)
                        {
                            fft_buffer[i].X = (float)(buffer[i] * FastFourierTransform.HannWindow(i, BUFFER_SIZE));
                            fft_buffer[i].Y = 0;
                        }
                        FastFourierTransform.FFT(true, (int)Math.Log(FFT_SIZE, 2.0), fft_buffer);

                        //list.Add(fft_buffer.Skip(FFT_SIZE / 4 * 3).ToArray());
                        list.Add(fft_buffer.Skip(FFT_SIZE / 2).ToArray());
                    }
                }
            }
        }
    }
}