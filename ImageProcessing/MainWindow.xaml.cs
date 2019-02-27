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

        public void CreateShadesCsvWithoutTreads(string path, string headerLine = "X,Y,R,G,B,A")
        {
            Bitmap img = new Bitmap(path);
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine(headerLine);
            var dictionaryShades = InitializeDictionary(img.Width, img.Height);
        
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    System.Drawing.Color pixel = img.GetPixel(i, j);
                    strBuilder.AppendLine(String.Format("{0},{1},{2},{3},{4},{5}", i, j, pixel.R, pixel.G, pixel.B, pixel.A));
                    dictionaryShades[pixel.R] = dictionaryShades[pixel.R] + 1;
                }
            }

            SaveCsv(path, strBuilder);

            var countsStrBuilder = new StringBuilder();
            countsStrBuilder.AppendLine("RGBA COUNTS");
            countsStrBuilder.AppendLine("Key, Value");

            foreach (var el in dictionaryShades)
            {
                countsStrBuilder.AppendLine(String.Format("{0}, {1}", el.Key, el.Value));
            }

            SaveCsv(path, countsStrBuilder, "rgba.csv");
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

            await Task.Run(() =>
            {
                CreateShadesCsvWithoutTreads(path);

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
