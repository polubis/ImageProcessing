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
using System.Collections.Concurrent;

namespace ImageProcessing
{
    public partial class MainWindow : Window
    {
        BitmapImage image;
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

        public async void GenerateReport(object sender, RoutedEventArgs e)
        {
            if (NotProcessedImage.Source != null)
            {
                SaveToCsvTime.Text = "...";
                CurrentState.Text = "generating report it can be while..";
                string stringDate = DateTime.Now.ToString("MM/dd/yyyy h:mm tt");
                string path = Directory.GetParent(image.UriSource.OriginalString).FullName + "\\" + "Report_from_" + stringDate.Replace(':', '_').Replace('/', '_');

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                int threadsLimit = 20;
                await HandleGeneratingReport(threadsLimit, path);
            }
        }

        private async Task HandleGeneratingReport(int threadsLimit, string path)
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < threadsLimit; i++)
                {
                    string originalString = "";
                    Dispatcher.Invoke(() =>
                    {
                        originalString = image.UriSource.OriginalString;
                    });

                    string pathToThreadFolder = path + "\\" + (i + 1) + "threads" + "\\";
                    Directory.CreateDirectory(pathToThreadFolder);
                    CreateShadesWithMultipleThreads(originalString, i + 1, pathToThreadFolder);
                }
                Dispatcher.Invoke(() =>
                {
                    CurrentState.Text = "finished in " + lastSavedImageTime;
                });
            });
        }

        private async void CountFromCsvFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = "c:\\";
            dlg.RestoreDirectory = false;
            dlg.Filter = "Text files (*.csv)|*.csv";

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveToCsvTime.Text = "...";
                CurrentState.Text = "reading pixel csv...";

                var sr = new StreamReader(dlg.FileName);
                string content = await sr.ReadToEndAsync();

                CurrentState.Text = "pixel csv readed";
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
                image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(dlg.FileName);
                image.EndInit();
                NotProcessedImageHeight.Text = image.PixelHeight.ToString() + "px";
                NotProcessedImageWidth.Text = image.PixelWidth.ToString() + "px";
                NotProcessedImage.Source = image;
            }
        }
        public void CreateShadesWithMultipleThreads(string path, int threadsNumber, string pathToSaveData, string headerLine = "X,Y,R,G,B,A")
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Bitmap img = new Bitmap(path);
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine(headerLine);
            ConcurrentDictionary<string, int> shades = new ConcurrentDictionary<string, int>();

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
                    
                    int widthStartIndex = widthBreakpoints[index];
                    int heightStartIndex = heightBreakpoints[index];
                    int widthLimit = widthBreakpoints[index+1];
                    int heightLimit = heightBreakpoints[index+1];

                    for (int i = widthStartIndex; i < widthLimit; i++)
                    {
                        for (int j = heightStartIndex; j < heightLimit; j++)
                        {
                            System.Drawing.Color pixel;
                            lock (img)
                            {
                                pixel = img.GetPixel(i, j);
                            }
                            strBuilder.AppendLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}", 
                                i, j,
                                pixel.R.ToString(), 
                                pixel.G.ToString(), 
                                pixel.B.ToString(), 
                                pixel.A.ToString()));

                            string color = $"{pixel.R.ToString()}, {pixel.G.ToString()}, {pixel.B.ToString()}";
                            if (shades.ContainsKey(color))
                            {
                                shades[color]++; 
                            }
                            else
                            {
                                shades[color] = 1;
                            }
                        }
                    }
                });

            if (pathToSaveData != null)
            {
                SaveCsv(pathToSaveData, strBuilder);
            }
            else
            {
                SaveCsv(path, strBuilder);
            }

            var countsStrBuilder = new StringBuilder();
            countsStrBuilder.AppendLine("RGB COUNTS");
            countsStrBuilder.AppendLine("R, G, B, Counts");
            PrintShades(shades, countsStrBuilder);

            sw.Stop();
            countsStrBuilder.AppendLine("Total time, Number of threads");
            countsStrBuilder.AppendLine(String.Format("{0}, {1}", sw.Elapsed + "s", threadsNumber.ToString()));
            lastSavedImageTime = sw.Elapsed.ToString();

            if (pathToSaveData != null)
            {
                SaveCsv(pathToSaveData, countsStrBuilder, "rgba.csv");
            }
            else
            {
                SaveCsv(path, countsStrBuilder, "rgba.csv");
            }

        }

        public void PrintShades(ConcurrentDictionary<string, int> shades, StringBuilder stringBuilder)
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
                    CreateShadesWithMultipleThreads(path, threadsNumber, null);
                }
                else
                {
                    CreateShadesWithMultipleThreads(path, 1, null);
                }

                Dispatcher.Invoke(() =>
                {
                    CurrentState.Text = "finished in " + lastSavedImageTime;
                });
            });
            
        }
    }
}
