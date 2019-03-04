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
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace ImageProcessing
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FindImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "Image files (*.jpg)|*.jpg|All Files (*.*)|*.*";
            dlg.RestoreDirectory = true;

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(dlg.FileName);
                bitmap.EndInit();
                NotProcessedImage.Source = bitmap;
                ManageProcessing(bitmap.UriSource.OriginalString, bitmap.PixelWidth, bitmap.PixelHeight);
            }
        }

        private Dictionary<int, int> InitializeDictionary(int width, int height)
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();

            const int limit = 256;

            for (int i = 0; i < limit; i++)
            {
                dictionary[i] = 0;
            }

            return dictionary;
        }

        public void CreateShadesCsvWithoutThreads(string path, string headerLine = "X,Y,R,G,B,A")
        {
            Bitmap img = new Bitmap(path);
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine(headerLine);
            var rShades = InitializeDictionary(img.Width, img.Height);
            var gShades = InitializeDictionary(img.Width, img.Height);
            var bShades = InitializeDictionary(img.Width, img.Height);

            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    System.Drawing.Color pixel = img.GetPixel(i, j);
                    strBuilder.AppendLine(String.Format("{0},{1},{2},{3},{4},{5}", i, j, pixel.R, pixel.G, pixel.B, pixel.A));
                    rShades[pixel.R] = rShades[pixel.R] + 1;
                    gShades[pixel.G] = gShades[pixel.G] + 1;
                    bShades[pixel.B] = bShades[pixel.B] + 1;
                }
            }

            SaveCsv(path, strBuilder);

            var countsStrBuilder = new StringBuilder();
            countsStrBuilder.AppendLine("RGB COUNTS");
            countsStrBuilder.AppendLine("RCounts, Value");
            PrintShades(rShades, countsStrBuilder);
            countsStrBuilder.AppendLine("GCounts, Value");
            PrintShades(gShades, countsStrBuilder);
            countsStrBuilder.AppendLine("BCounts, Value");
            PrintShades(bShades, countsStrBuilder);

            SaveCsv(path, countsStrBuilder, "rgba.csv");
        }

        public void CreateShadesWithMultipleThreads(string path, int threadsNumber, string headerLine = "X,Y,R,G,B,A")
        {
            Bitmap img = new Bitmap(path);
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine(headerLine);
            var rShades = InitializeDictionary(img.Width, img.Height);
            var gShades = InitializeDictionary(img.Width, img.Height);
            var bShades = InitializeDictionary(img.Width, img.Height);

            int widthJump = img.Width / threadsNumber;
            int heightJump = img.Height / threadsNumber;

            int moduloWidth = img.Width % threadsNumber;
            int moduloHeight = img.Height % threadsNumber;

            int[] widthBreakpoints = new int[threadsNumber+1];
            int[] heightBreakpoints = new int[threadsNumber+1];
            widthBreakpoints[0] = 0;
            heightBreakpoints[0] = 0;

            for (int i = 2; i < threadsNumber+2; i++)
            {
                widthBreakpoints[i - 1] = widthJump*(i-1);
                heightBreakpoints[i - 1] = heightJump *(i-1);
            }

            if (moduloWidth != 0)
            {
                widthBreakpoints[threadsNumber] = widthBreakpoints[threadsNumber] + moduloWidth;
            } 

            if (moduloHeight != 0)
            {
                heightBreakpoints[threadsNumber] = heightBreakpoints[threadsNumber] + moduloHeight;
            }


            Parallel.For(0, threadsNumber,
                index =>
                {
                    Bitmap clonedImage = (Bitmap)img.Clone();
                    int widthStartIndex = widthBreakpoints[index];
                    int heightStartIndex = heightBreakpoints[index];
                    int widthLimit = widthBreakpoints[index+1];
                    int heightLimit = heightBreakpoints[index+1];

                    for (int i = widthStartIndex; i < widthLimit; i++)
                    {
                        for (int j = heightStartIndex; j < heightLimit; j++)
                        {
                            System.Drawing.Color pixel = clonedImage.GetPixel(i, j);
                            strBuilder.AppendLine(String.Format("{0},{1},{2},{3},{4},{5}", i, j, pixel.R, pixel.G, pixel.B, pixel.A));
                            rShades[pixel.R] = rShades[pixel.R] + 1;
                            gShades[pixel.G] = gShades[pixel.G] + 1;
                            bShades[pixel.B] = bShades[pixel.B] + 1;
                        }
                    }
                });

            SaveCsv(path, strBuilder);

            var countsStrBuilder = new StringBuilder();
            countsStrBuilder.AppendLine("RGB COUNTS");
            countsStrBuilder.AppendLine("RCounts, Value");
            PrintShades(rShades, countsStrBuilder);
            countsStrBuilder.AppendLine("GCounts, Value");
            PrintShades(gShades, countsStrBuilder);
            countsStrBuilder.AppendLine("BCounts, Value");
            PrintShades(bShades, countsStrBuilder);

            SaveCsv(path, countsStrBuilder, "rgba.csv");
        }

        public void PrintShades(Dictionary<int, int> shades, StringBuilder stringBuilder)
        {
            foreach (var el in shades)
            {
                stringBuilder.AppendLine(String.Format("{0}, {1}", el.Key, el.Value));
            }
        }

        public void SaveCsv(string path, StringBuilder strBuilder, string fileName = "data.csv")
        {
            string pathToSave = Directory.GetParent(path).FullName + "\\" + fileName;

            if (File.Exists(pathToSave))
            {
                File.Delete(pathToSave);
            }

            File.AppendAllText(pathToSave, strBuilder.ToString());
        }

        private async void ManageProcessing(string path, int pixelWidth, int pixelHeight)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            SaveToCsvTime.Text = "...";
            NotProcessedImageHeight.Text = pixelHeight.ToString() + "px";
            NotProcessedImageWidth.Text = pixelWidth.ToString() + "px";
            CurrentState.Text = "is loading...";

            string threadsNumberText = ThreadsNumber.Text;

            await Task.Run(() =>
            {
                int threadsNumber;
                if (int.TryParse(threadsNumberText, out threadsNumber))
                {
                    CreateShadesWithMultipleThreads(path, threadsNumber);
                }
                else
                {
                    CreateShadesCsvWithoutThreads(path);
                }


                Dispatcher.Invoke(() =>
                {
                    sw.Stop();
                    SaveToCsvTime.Text = sw.Elapsed.Seconds.ToString() + "s";
                    CurrentState.Text = "finished!";
                });
            });
            
        }
    }
}
