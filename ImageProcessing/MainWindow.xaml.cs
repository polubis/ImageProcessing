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
                ReadImageAndWriteToCSV(bitmap.UriSource.OriginalString, bitmap.PixelWidth, bitmap.PixelHeight);
                NotProcessedImage.Source = bitmap;
            }
        }

        private int ManagePixel(Dictionary<int, int> dictionary, int pixelValue)
        {
            if (dictionary.ContainsKey(pixelValue))
            {
                return dictionary[pixelValue] + 1;
            }
            else
            {
                return 1;
            }
        }

        private async void ReadImageAndWriteToCSV(string path, int pixelWidth, int pixelHeight)
        {
            NotProcessedImageHeight.Text = pixelHeight.ToString() + "px";
            NotProcessedImageWidth.Text = pixelWidth.ToString() + "px";

            Bitmap img = new Bitmap(path);
            var content = new StringBuilder();
            content.AppendLine("X,Y,R,G,B,A");

            var countsContent = new StringBuilder();
            countsContent.AppendLine("Colors counts");
            countsContent.AppendLine("RKey, RCounts, GKey, GCounts, BKey, BCounts");

            await Task.Run(() =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Dictionary<int, int> RCounts = new Dictionary<int, int>();
                Dictionary<int, int> GCounts = new Dictionary<int, int>();
                Dictionary<int, int> BCounts = new Dictionary<int, int>();

                for (int i = 0; i < img.Width; i++)
                {
                    for (int j = 0; j < img.Height; j++)
                    {
                        System.Drawing.Color pixel = img.GetPixel(i, j);
                        content.AppendLine(String.Format("{0},{1},{2},{3},{4},{5}", i, j, pixel.R, pixel.G, pixel.B, pixel.A));
                        RCounts[pixel.R] = ManagePixel(RCounts, pixel.R);
                        GCounts[pixel.G] = ManagePixel(GCounts, pixel.G);
                        BCounts[pixel.B] = ManagePixel(BCounts, pixel.B);
                    }
                }

                for(int i = 0; i < RCounts.Count; i++)
                {
                    countsContent.AppendLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}", 
                        RCounts.ElementAt(i).Key, RCounts.ElementAt(i).Value,
                        GCounts.ElementAt(i).Key, GCounts.ElementAt(i).Value,
                        BCounts.ElementAt(i).Key, BCounts.ElementAt(i).Value));
                }

                Dispatcher.Invoke(() =>
                {
                    sw.Stop();
                    SaveToCsvTime.Text = sw.Elapsed.Seconds.ToString();
                    // PixelGraph pixelGraph = new PixelGraph();
                    // pixelGraph.Show();
                });
            });

            string pathToSave = Directory.GetParent(path).FullName + "\\data.csv";
            string pathToSaveR = Directory.GetParent(path).FullName + "\\pixogramR.csv";

            if (File.Exists(pathToSave))
            {
                File.Delete(pathToSave);
            }

            if (File.Exists(pathToSaveR))
            {
                File.Delete(pathToSaveR);
            }

            File.AppendAllText(pathToSave, content.ToString());
            File.AppendAllText(pathToSaveR, countsContent.ToString());
        }
    }
}
