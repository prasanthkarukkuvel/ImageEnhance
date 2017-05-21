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
using Microsoft.Win32;
using System.Threading;
using System.Runtime;
using System.Diagnostics;

namespace ImageEnhance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public BitmapImage Image { get; set; }
        public Int32Rect ImageRect => Image != null ? new Int32Rect(0, 0, Image.PixelWidth, Image.PixelHeight) : Int32Rect.Empty;

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "JPEG|*.jpg;";
            if ((bool)Dialog.ShowDialog(this))
            {
                Image = new BitmapImage(new Uri(Dialog.FileName));
                Previewer.Source = Image;
            }
        }

        private void Adjuster_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (Image != null)
            {
                Adjuster.IsEnabled = false;
                var Value = Adjuster.Value;
                ProcessPixles(Value);
            }
        }

        private void ProcessPixles(double Value)
        {
            var Stride = Image.PixelWidth * ((Image.Format.BitsPerPixel + 7) / 8);
            byte[] Pixels = new byte[(int)Image.Height * Stride];
            Image.CopyPixels(ImageRect, Pixels, Stride, 0);

            new Thread(() => InvokeChangeContrast(Pixels, Value, HandleChangeContrast))
                    .Start();
        }

        private static void InvokeChangeContrast(byte[] Pixels, double AdjustValue, Action<byte[]> Callback)
        {
            Callback(ChangeContrast(Pixels, AdjustValue));
        }

        private void HandleChangeContrast(byte[] Source)
        {
            Dispatcher.BeginInvoke((Action)(() =>
             {               
                 Previewer.Source = SetPixles(Source); 
                 Adjuster.IsEnabled = true;
             }));
        }

        private ImageSource SetPixles(byte[] Pixels)
        {
            var Source = new WriteableBitmap(Image);
            Source.WritePixels(ImageRect, Pixels, (Image.PixelWidth * ((Image.Format.BitsPerPixel + 7) / 8)), 0);
            return Source;
        }

        private static byte[] ChangeContrast(byte[] Pixels, double AdjustValue)
        {
            double ContrastLevel = Math.Pow((100.0 + AdjustValue) / 100.0, 2);

            for (int k = 0; k + 4 < Pixels.Length; k += 4)
            {
                Pixels[k] = CorrectContrast(GetContrast(Pixels[k], ContrastLevel));
                Pixels[k + 1] = CorrectContrast(GetContrast(Pixels[k + 1], ContrastLevel));
                Pixels[k + 2] = CorrectContrast(GetContrast(Pixels[k + 2], ContrastLevel));
            }

            return Pixels;
        }
        private static double GetContrast(byte Pixel, double ContrastLevel) => ((((Pixel / 255.0) - 0.5) * ContrastLevel) + 0.5) * 255.0;
        private static byte CorrectContrast(double Color) => (byte)(Color > 255 ? 255 : Color < 0 ? 0 : Color);
    }
}
