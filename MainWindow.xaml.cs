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
using System.IO;
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
        public string FileName { get; set; }
        public BitmapImage Image { get; set; }
        public Int32Rect ImageRect => Image != null ? new Int32Rect(0, 0, Image.PixelWidth, Image.PixelHeight) : Int32Rect.Empty;
        public int Counter { get; set; }
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "JPEG|*.jpg;";
            if ((bool)Dialog.ShowDialog(this))
            {
                FileName = Dialog.FileName;
                using (var Stream = File.Open(FileName, FileMode.Open, FileAccess.Read))
                {
                    Image = new BitmapImage();
                    Image.BeginInit();
                    Image.CacheOption = BitmapCacheOption.OnLoad;
                    Image.StreamSource = Stream;
                    Image.EndInit();
                }
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
            var Source = new WriteableBitmap(Image);
            var Width = Image.PixelWidth / 4;
            var Height = Image.PixelHeight / 2;
            var Stride = Width * ((Image.Format.BitsPerPixel + 7) / 8);
            var Size = Height * Stride;
            Counter = 4 * 2;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    byte[] Pixels = new byte[Size];

                    var Rect = new Int32Rect(i * Width, j * Height, Width, Height);

                    Image.CopyPixels(Rect, Pixels, Stride, 0);

                    new Thread(() => InvokeChangeContrast(Pixels, Value, Rect, Stride, Source, HandleChangeContrast))
                       .Start();
                }               
            }
        }
        private static void InvokeChangeContrast(byte[] Pixels, double AdjustValue, Int32Rect Rect, int Stride, WriteableBitmap Source, Action<byte[], Int32Rect, int, WriteableBitmap> Callback)
        {
            Callback(ChangeContrast(Pixels, AdjustValue), Rect, Stride, Source);
        }
        private void HandleChangeContrast(byte[] Pixels, Int32Rect Rect, int Stride, WriteableBitmap Source)
        {
            Dispatcher.BeginInvoke((Action)(() =>
             {
                 Counter--;

                 SetPixles(Pixels, Rect, Stride, Source);

                 if (Counter == 0)
                 {
                     Previewer.Source = Source;
                     Adjuster.IsEnabled = true;
                 }
             }));
        }
        private void SetPixles(byte[] Pixels, Int32Rect Rect, int Stride, WriteableBitmap Source)
        {
            Source.WritePixels(Rect, Pixels, Stride, 0);
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
