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
using System.Windows.Shapes;

namespace ImageProcessing
{
    /// <summary>
    /// Interaction logic for PixelGraph.xaml
    /// </summary>
    public partial class PixelGraph : Window
    {
        Canvas canvas;
        public PixelGraph()
        {
            InitializeComponent();
            PlotDot(13, 13);
        }

        public void PlotDot(int x, int y)
        {
            /*
            Ellipse ellipse = new Ellipse();

            ellipse.Width = 10;

            ellipse.Height = 10;

            ellipse.Fill = Brushes.Blue;

            Canvas.SetLeft(ellipse, x);

            Canvas.SetTop(ellipse, y);

            canvas.Children.Add(ellipse);
            */
        }

        public void PlotCanvasWindow()
        {
            Title = "Dot Plot";

            Width = 600;

            Height = 400;

            WindowStyle = WindowStyle.ToolWindow;

            ResizeMode = ResizeMode.NoResize;

            canvas.Background = Brushes.AliceBlue;

            Content = canvas;
        }

        private void DrawText(int x, int y, string text)
        {

            TextBlock textBlock = new TextBlock();

            textBlock.Text = text;

            Canvas.SetLeft(textBlock, x);

            Canvas.SetTop(textBlock, y);

            canvas.Children.Add(textBlock);

            // punk slash
        }
    }
}
