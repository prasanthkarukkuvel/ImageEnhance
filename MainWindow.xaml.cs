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
        public Int32Rect ImageRect => Image != null ? new Int32Rect(0, 0, (int)Image.PixelWidth, (int)Image.PixelHeight) : Int32Rect.Empty;

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
        private void Adjuster_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Previewer.Source = ChangeContrast(Adjuster.Value);
        }

        private ImageSource ChangeContrast(double AdjustValue)
        {
            if (Image != null)
            {
                byte[] Pixels = GetPixles(Image, ImageRect);
                double ContrastLevel = Math.Pow((100.0 + AdjustValue) / 100.0, 2);

                for (int k = 0; k + 4 < Pixels.Length; k += 4)
                {                  
                    Pixels[k] = CorrectContrast(GetContrast(Pixels[k], ContrastLevel));
                    Pixels[k + 1] = CorrectContrast(GetContrast(Pixels[k + 1], ContrastLevel));
                    Pixels[k + 2] = CorrectContrast(GetContrast(Pixels[k + 2], ContrastLevel));
                }

                return SetPixles(Image, ImageRect, Pixels);
            }

            return null;
        }
        private static double GetContrast(byte Pixel, double ContrastLevel) => ((((Pixel / 255.0) - 0.5) * ContrastLevel) + 0.5) * 255.0;
        private static byte CorrectContrast(double Color) => (byte)(Color > 255 ? 255 : Color < 0 ? 0 : Color);

        private static byte[] GetPixles(BitmapImage Image, Int32Rect ImageRect)
        {
            var Source = new WriteableBitmap(Image);
            var Stride = Source.PixelWidth * ((Source.Format.BitsPerPixel + 7) / 8); 

            byte[] Pixels = new byte[(int)Source.Height * Stride];
            Source.CopyPixels(ImageRect, Pixels, Stride, 0);
            return Pixels;
        }
        private static ImageSource SetPixles(BitmapImage Image, Int32Rect ImageRect, byte[] Pixels)
        {
            var Source = new WriteableBitmap(Image);
            Source.WritePixels(ImageRect, Pixels, (Source.PixelWidth * ((Source.Format.BitsPerPixel + 7) / 8)), 0);
            return Source;
        }
    }
}
