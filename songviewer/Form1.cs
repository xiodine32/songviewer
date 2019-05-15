using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace songviewer
{
    public partial class Form1 : Form
    {
        private Orchestrator orchestrator = new Orchestrator();
        private List<Complex[]> list;
        private List<double[]> values;
        private double max;
        private Stopwatch stopwatch = new Stopwatch();
        private Bitmap bitmap;
        private int width = 1200;
        private byte[] pixels;
        private int lastTake = 0;
        private WaveOut waveOut;

        public Form1()
        {
            InitializeComponent();

            
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                label1.Text = openFileDialog1.FileName;
                list = orchestrator.Load(openFileDialog1.FileName);
                values = new List<double[]>(list.Count);
                max = 0;
                for (int col = 0; col < list.Count; col++)
                {
                    double [] vals = new double[list[col].Length];
                    for (int row = 0; row < list[col].Length; row++)
                    {
                        double value = Math.Sqrt(list[col][row].X * list[col][row].X + list[col][row].Y * list[col][row].Y);
                        max = Math.Max(max, value);
                        vals[row] = value;
                    }
                    values.Add(vals);
                }

                bitmap = new Bitmap(width, list[0].Length, PixelFormat.Format8bppIndexed);
                // modify the indexed palette to make it grayscale
                ColorPalette pal = bitmap.Palette;
                for (int i = 0; i < 256; i++)
                { 
                    if (i == 255)
                    {
                        pal.Entries[i] = Color.FromArgb(255, 255, 0, 0);
                    } else
                    {
                        pal.Entries[i] = Color.FromArgb(255, i, i, i);
                    }
                }
                bitmap.Palette = pal;
                pictureBox1.Image = bitmap;

                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                var reader = new AudioFileReader(openFileDialog1.FileName);
                waveOut = new WaveOut();
                waveOut.Init(reader);
                waveOut.Play();

                if (timer1.Enabled) {
                    timer1.Tick -= Timer1_Tick;
                    timer1.Stop();
                }
                lastTake = 0;
                stopwatch.Restart();
                timer1.Interval = 1000 / 30;
                timer1.Tick += Timer1_Tick;
                timer1.Start();
            }
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            
            // prepare to access data via the bitmapdata object
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            // create a byte array to reflect each pixel in the image
            if (pixels == null) {
                pixels = new byte[bitmapData.Stride * bitmap.Height];
            }
            // fill pixel array with data
            var take = (stopwatch.ElapsedMilliseconds * 6) / 100;
            for (; lastTake < take; lastTake++) {
                var colStart = lastTake % width;
                for (int row = 0; row < list[colStart].Length; row++)
                {
                    int pos = lastTake;
                    int bytePosition = row * bitmapData.Stride + colStart;
                    double val = values[pos][row];
                        //Math.Abs(list[col + (colStart - BACKTRACK)][row].X + list[col + (colStart - BACKTRACK)][row].Y);
                    double pixelVal = 255 * (val / max) * 32;
                    pixelVal = Math.Max(0, pixelVal);
                    pixelVal = Math.Min(254, pixelVal);
                    pixels[bytePosition] = (byte)pixelVal;
                }
            }
            for (int row = 0; row < list[(int)take].Length; row++)
            {
                int bytePosition = row * bitmapData.Stride + ((int)take + 1) % width;
                pixels[bytePosition] = 0;
            }

            // turn the byte array back into a bitmap
            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            bitmap.UnlockBits(bitmapData);
            pictureBox1.Image = bitmap;
        }
    }
}
