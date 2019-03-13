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
        private BitmapImage _image;
        private string _time;
        public const int ThreadsNumber = 10;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessImage(object sender, RoutedEventArgs e)
        {
            if (Image.Source != null)
            {
                ManageProcessing(_image.UriSource.OriginalString);
            }
        }

        public async void GenerateReport(object sender, RoutedEventArgs e)
        {
            var stringDate = DateTime.Now.ToString("MM/dd/yyyy h:mm tt");
            if (Image.Source == null) return;
            //SaveToCsvTime.Text = "...";
            CurrentState.Text = "generating report it can be while..";
            var path = Directory.GetParent(_image.UriSource.OriginalString).FullName + "\\" + "Report_from_" + stringDate.Replace(':', '_').Replace('/', '_');

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            const int threadsLimit = 20;
            await HandleGeneratingReport(threadsLimit, path);
        }

        private async Task HandleGeneratingReport(int threadsLimit, string path)
        {
            await Task.Run(() =>
            {
                for (var i = 0; i < threadsLimit; i++)
                {
                    var originalString = "";
                    Dispatcher.Invoke(() =>
                    {
                        originalString = _image.UriSource.OriginalString;
                    });

                    var pathToThreadFolder = path + "\\" + (i + 1) + "threads" + "\\";
                    Directory.CreateDirectory(pathToThreadFolder);
                    CreateShadesWithMultipleThreads(originalString, i + 1, pathToThreadFolder);
                }
                Dispatcher.Invoke(() =>
                {
                    CurrentState.Text = "finished in " + _time;
                });
            });
        }

        private async void CountFromCsvFile(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                InitialDirectory = "c:\\", RestoreDirectory = false, Filter = @"Text files (*.csv)|*.csv"
            };

            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            //SaveToCsvTime.Text = "...";
            CurrentState.Text = "reading pixel csv...";

            var sr = new StreamReader(dlg.FileName);
            var content = await sr.ReadToEndAsync();

            CurrentState.Text = "pixel csv";

        }

        private void ChooseImage(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = @"Image files (*.jpg)|*.jpg|All Files (*.*)|*.*",
                RestoreDirectory = false
            };

            if (fileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            _image = new BitmapImage();
            _image.BeginInit();
            _image.UriSource = new Uri(fileDialog.FileName);
            _image.EndInit();

            ImageHeight.Text = _image.PixelHeight.ToString() + " px";
            ImageWidth.Text = _image.PixelWidth.ToString() + " px";
            ImageSize.Text = new System.IO.FileInfo(fileDialog.FileName).Length.ToString() + " bytes";

            Image.Source = _image;
        }

        private void RemoveImage(object sender, RoutedEventArgs e)
        {
            Image.Source = null;
            ImageHeight.Text = "0 px";
            ImageWidth.Text = "0 px";
            ImageSize.Text = "0 bytes";
        }

        public void CreateShadesWithMultipleThreads(string path, int threadsNumber, string pathToSaveData, string headerLine = "X,Y,R,G,B,A")
        {
            var sw = new Stopwatch();
            sw.Start();

            var img = new Bitmap(path);
            var strBuilder = new StringBuilder();
            strBuilder.AppendLine(headerLine);
            var shades = new ConcurrentDictionary<string, int>();

            var widthJump = img.Width / threadsNumber;
            var heightJump = img.Height / threadsNumber;

            var moduloWidth = img.Width % threadsNumber;
            var moduloHeight = img.Height % threadsNumber;

            var widthBreakpoints = new int[threadsNumber+1];
            var heightBreakpoints = new int[threadsNumber+1];
            widthBreakpoints[0] = 0;
            heightBreakpoints[0] = 0;

            for (var i = 2; i < threadsNumber+2; i++)
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
                    
                    var widthStartIndex = widthBreakpoints[index];
                    var heightStartIndex = heightBreakpoints[index];
                    var widthLimit = widthBreakpoints[index+1];
                    var heightLimit = heightBreakpoints[index+1];

                    for (var i = widthStartIndex; i < widthLimit; i++)
                    {
                        for (var j = heightStartIndex; j < heightLimit; j++)
                        {
                            System.Drawing.Color pixel;
                            lock (img)
                            {
                                pixel = img.GetPixel(i, j);
                            }
                            strBuilder.AppendLine(
                                $"{i}, {j}, {pixel.R.ToString()}, {pixel.G.ToString()}, {pixel.B.ToString()}, {pixel.A.ToString()}");

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
            countsStrBuilder.AppendLine("R, G, B, Count");
            PrintShades(shades, countsStrBuilder);

            sw.Stop();
            countsStrBuilder.AppendLine("Time, Threads");
            countsStrBuilder.AppendLine($"{sw.Elapsed + "s"}, {threadsNumber.ToString()}");
            _time = sw.Elapsed.ToString();

            SaveCsv(pathToSaveData ?? path, countsStrBuilder, "rgba_data.csv");
        }

        public void PrintShades(ConcurrentDictionary<string, int> shades, StringBuilder stringBuilder)
        {
            foreach (var el in shades)
            {
                stringBuilder.AppendLine($"{el.Key}, {el.Value}");
            }
        }

        public void SaveCsv(string path, StringBuilder strBuilder, string fileName = "data.csv")
        {
            var pathToSave = Directory.GetParent(path).FullName + "\\" + fileName;

            if (File.Exists(pathToSave))
            {
                File.Delete(pathToSave);
            }

            File.AppendAllText(pathToSave, strBuilder.ToString());
        }

        private async void ManageProcessing(string path)
        {
            //SaveToCsvTime.Text = "...";
            CurrentState.Text = "is loading...";

            await Task.Run(() =>
            {
                CreateShadesWithMultipleThreads(path,
                     ThreadsNumber, null);

                Dispatcher.Invoke(() =>
                {
                    CurrentState.Text = "Time: " + _time;
                });
            });
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
