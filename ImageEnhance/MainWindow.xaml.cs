using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
            OnInit();
        }
        public BitmapImage Image { get; set; }
        public int Counter { get; set; }
        public Stopwatch Stopwatch { get; set; } = new Stopwatch();
        public System.Timers.Timer Timer { get; set; } = new System.Timers.Timer(1000);

        private void OnInit()
        {
            Time.Text = DateTime.Now.ToLongTimeString();
            Timer.Start();
            Timer.Elapsed += ((a,b) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Time.Text = DateTime.Now.ToLongTimeString();
                }));
            });
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog Dialog = new OpenFileDialog();
            Dialog.Filter = "JPEG|*.jpg;";
            if ((bool)Dialog.ShowDialog(this))
            {
                using (var Stream = File.Open(Dialog.FileName, FileMode.Open, FileAccess.Read))
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
                var Value = Adjuster.Value;
                Adjuster.IsEnabled = false;
                Stopwatch.Restart();
                ProcessPixles(Value);
            }
        }
        private void ProcessPixles(double Value)
        {
            var Column = 4;
            var Row = Column / 2;
            var Source = new WriteableBitmap(Image.PixelWidth, Image.PixelHeight, Image.DpiX, Image.DpiY, Image.Format, Image.Palette);
            var Width = Image.PixelWidth / Column;
            var Height = Image.PixelHeight / Row;
            var Stride = Width * ((Image.Format.BitsPerPixel + 7) / 8);
            var Size = Height * Stride;
            Counter = Column * Row;

            for (int i = 0; i < Column; i++)
            {
                for (int j = 0; j < Row; j++)
                {
                    byte[] Pixels = new byte[Size];

                    var Rect = new Int32Rect(i * Width, j * Height, Width, Height);

                    Image.CopyPixels(Rect, Pixels, Stride, 0);

                    new Thread(() => InvokeChangeContrast(Pixels, Value, Rect, Stride, Source, HandleChangeContrast))
                       .Start();

                    // InvokeChangeContrast(Pixels, Value, Rect, Stride, Source, HandleChangeContrast);

                    // Task.Factory.StartNew(() => InvokeChangeContrast(Pixels, Value, Rect, Stride, Source, HandleChangeContrast));
                }
            }
        }
        private void HandleChangeContrast(byte[] Pixels, Int32Rect Rect, int Stride, WriteableBitmap Source)
        {
            Dispatcher.BeginInvoke((Action)(() =>
             {
                 Counter--;

                 SetPixles(Pixels, Rect, Stride, Source);

                 if (Counter == 0)
                 {
                     Previewer.Source = null;
                     Previewer.Source = Source;
                     Source = null;
                     Stopwatch.Stop();
                     StopCount.Text = Stopwatch.Elapsed.TotalMilliseconds + "";
                     Adjuster.IsEnabled = true;

                 //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                 //GC.Collect();
                 //GC.WaitForPendingFinalizers();
             }
             }));
        }
        private void SetPixles(byte[] Pixels, Int32Rect Rect, int Stride, WriteableBitmap Source)
        {
            Source.WritePixels(Rect, Pixels, Stride, 0);
            Array.Resize(ref Pixels, 0);
        }

        private static void InvokeChangeContrast(byte[] Pixels, double AdjustValue, Int32Rect Rect, int Stride, WriteableBitmap Source, Action<byte[], Int32Rect, int, WriteableBitmap> Callback)
        {
            Callback(ChangeContrast(Pixels, AdjustValue), Rect, Stride, Source);
        }

        private static byte[] ChangeContrast(byte[] Pixels, double AdjustValue)
        {
            var ContrastLevel = Math.Pow((100.0 + AdjustValue) / 100.0, 2);

            for (int i = 0; i + 4 < Pixels.Length; i += 4)
            {
                Pixels[i] = CorrectContrast(GetContrast(Pixels[i], ContrastLevel));
                Pixels[i + 1] = CorrectContrast(GetContrast(Pixels[i + 1], ContrastLevel));
                Pixels[i + 2] = CorrectContrast(GetContrast(Pixels[i + 2], ContrastLevel));
            }

            return Pixels;
        }
        private static double GetContrast(byte Pixel, double ContrastLevel) => ((((Pixel / 255.0) - 0.5) * ContrastLevel) + 0.5) * 255.0;
        private static byte CorrectContrast(double Color) => (byte)(Color > 255 ? 255 : Color < 0 ? 0 : Color);
    }
}
