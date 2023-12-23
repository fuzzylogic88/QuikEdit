using ImageMagick;
using Microsoft.Win32;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuikEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage loadedBitmapImage;
        public static byte[] initialState;
        public static byte[] modifiedImage;

        public MainWindow()
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            if (openFileDialog.ShowDialog() == true)
            {
                loadedBitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                imageControl.Source = loadedBitmapImage;

                using (MemoryStream stream = new MemoryStream())
                {
                    BitmapEncoder encoder = new JpegBitmapEncoder(); // Change the encoder based on your image format
                    encoder.Frames.Add(BitmapFrame.Create(loadedBitmapImage));
                    encoder.Save(stream);
                    initialState = stream.ToArray();
                }
            }
        }

        static BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bitmapImage = new();
            using (MemoryStream stream = new(byteArray))
            {
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); 
            }
            return bitmapImage;
        }

        private void GrayscaleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool? state = GrayscaleCheckBox.IsChecked;
                if (loadedBitmapImage != null && state.HasValue)
                {
                    if (state.Value)
                    {
                        using MagickImage image = new(initialState);
                        {
                            image.Grayscale();
                            modifiedImage = image.Clone().ToByteArray();
                            imageControl.Source = ByteArrayToBitmapImage(image.ToByteArray());
                        }
                    }
                    else 
                    { 
                        imageControl.Source = ByteArrayToBitmapImage(initialState); 
                    }
                }
            }
            catch
            {
                Console.WriteLine("Grayscale change failed.");
            }
        }


        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "PNG Files (*.png)|*.png|JPEG Files (*.jpg)|*.jpg|All files (*.*)|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                if (modifiedImage == null) 
                { 
                    modifiedImage = initialState; 
                }

                SaveImage(modifiedImage, saveFileDialog.FileName, GetImageFormatFromFileName(saveFileDialog.FileName));
            }
        }

        private void SaveImage(byte[] imagedata, string filename, ImageFormat format)
        {
            if (imagedata != null && imagedata.Length > 0)
            {
                // Save the byte array to a file with the specified image format
                using (MemoryStream stream = new(imagedata))
                {
                    using Bitmap bitmap = new(stream);
                    bitmap.Save(filename, format);
                }
                MessageBox.Show("Image saved successfully.");
            }
            else
            {
                MessageBox.Show("Image data is null or empty. Unable to save.");
            }
        }
        private ImageFormat GetImageFormatFromFileName(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".gif":
                    return ImageFormat.Gif;
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".png":
                    return ImageFormat.Png;
                default:
                    // Default to PNG if the extension is not recognized
                    return ImageFormat.Png;
            }
        }
    }
}