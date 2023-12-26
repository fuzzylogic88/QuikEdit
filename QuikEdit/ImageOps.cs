using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace QuikEdit
{
    internal class ImageOps
    {
        public static BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
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

        public static ImageFormat GetImageFormatFromFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                ".png" => ImageFormat.Png,
                ".heic" => ImageFormat.Heif,
                ".tif" or ".tiff" => ImageFormat.Tiff,
                ".webp" => ImageFormat.Webp,
                _ => ImageFormat.Png,// Default to PNG if the extension is not recognized
            };
        }

        public static bool IsImageFile(string filePath)
        {
            // Define your list of valid image file extensions
            string[] validExtensions = [".BMP", ".JPG", ".JPEG", ".GIF", ".PNG", ".HEIC", ".TIFF", ".TIF", "WEBP"];
            return validExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase);
        }
    }
}
