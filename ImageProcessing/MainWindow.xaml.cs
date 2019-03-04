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
        BitmapImage image = new BitmapImage();
        string lastSavedImageTime;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessImage(object sender, RoutedEventArgs e)
        {
            if (NotProcessedImage.Source != null)
            {
                ManageProcessing(image.UriSource.OriginalString);
            }

        }

        public void GenerateReport(object sender, RoutedEventArgs e)
        {
            if (NotProcessedImage.Source != null)
            {
                string stringDate = DateTime.Now.ToString("MM/dd/yyyy h:mm tt");
                string path = Directory.GetParent(image.UriSource.OriginalString).FullName + "\\" + "Report_from_" + stringDate.Replace(':', '_').Replace('/', '_');

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

        }

        private void FindImage(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.Filter = "Image files (*.jpg)|*.jpg|All Files (*.*)|*.*";
            dlg.RestoreDirectory = false;

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                image.BeginInit();
                image.UriSource = new Uri(dlg.FileName);
                image.EndInit();
                NotProcessedImageHeight.Text = image.PixelHeight.ToString() + "px";
                NotProcessedImageWidth.Text = image.PixelWidth.ToString() + "px";
                NotProcessedImage.Source = image;
            }
        }
        public void CreateShadesWithMultipleThreads(string path, int threadsNumber, string headerLine = "X,Y,R,G,B,A")
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Bitmap img = new Bitmap(path);
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine(headerLine);
            var r = new Shade("R color shades", img.Width, img.Height);
            var g = new Shade("G color shades", img.Width, img.Height);
            var b = new Shade("B color shades", img.Width, img.Height);

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
                            r.Shades[pixel.R] = r.Shades[pixel.R] + 1;
                            g.Shades[pixel.G] = g.Shades[pixel.G] + 1;
                            b.Shades[pixel.B] = b.Shades[pixel.B] + 1;
                        }
                    }
                });

            SaveCsv(path, strBuilder);

            var countsStrBuilder = new StringBuilder();
            countsStrBuilder.AppendLine("RGB COUNTS");
            countsStrBuilder.AppendLine("RCounts, Value");
            PrintShades(r.Shades, countsStrBuilder);
            countsStrBuilder.AppendLine("GCounts, Value");
            PrintShades(g.Shades, countsStrBuilder);
            countsStrBuilder.AppendLine("BCounts, Value");
            PrintShades(b.Shades, countsStrBuilder);

            sw.Stop();
            countsStrBuilder.AppendLine("Total time, Number of threads");
            countsStrBuilder.AppendLine(String.Format("{0}, {1}", sw.Elapsed + "s", threadsNumber.ToString()));
            lastSavedImageTime = sw.Elapsed.ToString();

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

        private async void ManageProcessing(string path)
        {
            SaveToCsvTime.Text = "...";
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
                    CreateShadesWithMultipleThreads(path, 1);
                }

                Dispatcher.Invoke(() =>
                {
                    CurrentState.Text = "finished in " + lastSavedImageTime;
                });
            });
            
        }
    }
}
