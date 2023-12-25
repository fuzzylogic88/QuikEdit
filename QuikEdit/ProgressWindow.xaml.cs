using System.Windows;

namespace QuikEdit
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        public static bool ProcessInProgress = false;
        public static int CurrentFile = 0;
        public static int TotalFiles = 0;

        public ProgressWindow()
        {
            InitializeComponent();
            Task.Factory.StartNew(UpdateProgressBar);
        }

        public void UpdateProgressBar()
        {
            while (ProcessInProgress)
            {
                try
                {
                    int pbarVal = (int)Math.Round(CurrentFile / (decimal)TotalFiles * 100m, 0,MidpointRounding.AwayFromZero);
                    Dispatcher.Invoke(() =>
                    {
                        PBar.Value = pbarVal;
                    });
                    Thread.Sleep(150);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            Thread.Sleep(250);
            Dispatcher.Invoke(Close);
        }
    }
}
