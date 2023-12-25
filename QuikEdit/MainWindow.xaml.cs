using ImageMagick;
using Microsoft.VisualBasic.Devices;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static QuikEdit.ImageOps;

namespace QuikEdit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BitmapImage initialBitmapImage;
        public static byte[]? initialState;
        public static byte[]? modifiedImage;

        private CancellationTokenSource cTokenSrc;

        public MainWindow()
        {
            RenderOptions.ProcessRenderMode = RenderMode.Default;
            ResourceLimits.LimitMemory(new Percentage(95));

            InitializeComponent();
        }

        private async void FileCollectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileCollectionListBox.SelectedIndex != -1)
            {
                await PreviewNewImageAsync((string)FileCollectionListBox.SelectedItem);
            }
        }

        private async Task PreviewNewImageAsync(string filepath)
        {
            initialBitmapImage = new BitmapImage(new Uri(filepath));

            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    GrayscaleCheckBox.IsChecked = false;
                    imageControl.Source = initialBitmapImage;
                });

                using MemoryStream stream = new();
                BitmapEncoder encoder = new JpegBitmapEncoder(); // Change the encoder based on your image format
                encoder.Frames.Add(BitmapFrame.Create(initialBitmapImage));
                encoder.Save(stream);

                initialState = stream.ToArray();
                modifiedImage = initialState;
            });
        }

        private void GrayscaleCheckBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool? state = GrayscaleCheckBox.IsChecked;
                if (initialBitmapImage != null && state.HasValue)
                {
                    if (state.Value)
                    {
                        using MagickImage image = new(initialState);
                        {
                            image.Grayscale();
                            modifiedImage = image.ToByteArray();
                            imageControl.Source = ByteArrayToBitmapImage(modifiedImage);
                        }
                    }
                    else
                    {
                        modifiedImage = initialState;
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
            if (modifiedImage != null)
            {
                Microsoft.Win32.SaveFileDialog saveFileDialog = new()
                {
                    Filter = "BMP Files (*.bmp)|*.bmp|GIF Files (*.gif)|*.gif|JPEG Files (*.jpg;*.jpeg)|*.jpg;*.jpeg|PNG Files (*.png)|*.png|All files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (modifiedImage.Length > 0)
                    {
                        // Save the byte array to a file with the specified image format
                        using (MemoryStream stream = new(modifiedImage))
                        {
                            ImageFormat ift = GetImageFormatFromFileName(saveFileDialog.FileName);

                            using Bitmap bitmap = new(stream);
                            bitmap.Save(saveFileDialog.FileName, ift);
                        }
                        System.Windows.MessageBox.Show("Image saved successfully.");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Image data is null or empty. Unable to save.");
                    }
                }
            }
        }


        private void RotateLeft90Button_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedImage != null)
            {
                using MagickImage image = new(modifiedImage);
                {
                    image.Rotate(-90);
                    modifiedImage = image.ToByteArray();
                    initialState = modifiedImage;
                    imageControl.Source = ByteArrayToBitmapImage(image.ToByteArray());
                }
            }
        }

        private void RotateRight90Button_Click(object sender, RoutedEventArgs e)
        {
            if (modifiedImage != null)
            {
                using MagickImage image = new(modifiedImage);
                {
                    image.Rotate(90);
                    modifiedImage = image.ToByteArray();
                    initialState = modifiedImage;
                    imageControl.Source = ByteArrayToBitmapImage(image.ToByteArray());
                }
            }
        }

        private void LoadFilesButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDlg = new()
            {
                ShowNewFolderButton = true,
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString(),
            };

            // Show the FolderBrowserDialog.  
            DialogResult result = folderDlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var imgCollection = Directory.EnumerateFiles(folderDlg.SelectedPath, "*", SearchOption.AllDirectories).Where(IsImageFile);
                foreach (string image in imgCollection)
                {
                    FileCollectionListBox.Items.Add(image);
                }
                FileCollectionListBox.SelectedIndex = 0;
            }
        }

        private void ConvertFilesButton_Click(object sender, RoutedEventArgs e)
        {
            cTokenSrc = new();

            FolderBrowserDialog folderDlg = new()
            {
                ShowNewFolderButton = true,
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString(),
            };

            // Show the FolderBrowserDialog.  
            DialogResult result = new();
            Dispatcher.Invoke(() =>
            {
                result = folderDlg.ShowDialog();
            });

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                List<string> convertItems = FileCollectionListBox.Items.Cast<string>().ToList();

                ProgressWindow.TotalFiles = convertItems.Count;
                ProgressWindow.ProcessInProgress = true;
                ProgressWindow pw = new(this) { Owner = this, };

                pw.Show();

                Task.Run(() => ConvertFiles(convertItems, folderDlg.SelectedPath, cTokenSrc.Token));
            }
        }
        public void CancelOperation()
        {
            cTokenSrc?.Cancel();
        }

        private static void ConvertFiles(List<string> convertItems, string outputPath, CancellationToken cToken)
        {
            try
            {
                string fileType = ".jpg"; //temp

                Parallel.ForEach(convertItems, (image, parallelLoopState) =>
                {
                    using (MagickImage img = new(image))
                    {
                        string outFName = FilenameGenerator(outputPath, Path.GetFileNameWithoutExtension(image) + fileType);

                        if (!string.IsNullOrEmpty(outFName))
                        {
                            img.Quality = 100;
                            img.Write(outFName);
                            img.Dispose();                      
                        }
                        cToken.ThrowIfCancellationRequested();
                    }
                    Interlocked.Increment(ref ProgressWindow.CurrentFile);
                });
            }
            finally
            {
                ProgressWindow.ProcessInProgress = false;
                Thread.Sleep(1000);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        /// <summary>
        /// Generates a unique filename when provided destination folder and desired name.
        /// </summary>
        /// <param name="folder">Destination folder of file</param>
        /// <param name="fileName">Desired file name</param>
        /// <param name="maxAttempts">Maximum number of attempts to achieve uniqueness</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string FilenameGenerator(string folder, string fileName, int maxAttempts = 1024)
        {
            var fileBase = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);

            // Build hash set of filenames for performance
            var files = new HashSet<string>(Directory.GetFiles(folder));

            for (var index = 0; index < maxAttempts; index++)
            {
                // First try with the original filename, else try incrementally adding an index
                var name = (index == 0)
                    ? fileName
                    : string.Format("{0} ({1}){2}", fileBase, index, ext);

                // Check if exists
                var fullPath = Path.Combine(folder, name);
                if (files.Contains(fullPath))
                    continue;

                // Try to create the file
                try
                {
                    return fullPath;
                }
                catch (DirectoryNotFoundException) { throw; }
                catch (DriveNotFoundException) { throw; }
                catch (IOException)
                {
                    // Will occur if another thread created a file with this name since we created the HashSet.
                    // Ignore this and just try with the next filename.
                }
            }
            throw new Exception("Could not create unique filename in " + maxAttempts + " attempts");
        }
    }
}