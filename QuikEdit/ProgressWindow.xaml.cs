﻿/* 
 * QuikEdit
 * Daniel Green, 2023
 * 
 * A small photo viewer/editor/conversion tool using WPF
 */
 
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

        private readonly MainWindow mwInstc;
        public bool CancellationCalled = false;

        public ProgressWindow(MainWindow _mwInstc)
        {
            InitializeComponent();
            PBar.Value = 0;

            Task.Factory.StartNew(UpdateProgressBar);
            mwInstc = _mwInstc;
        }

        public void UpdateProgressBar()
        {
            while (ProcessInProgress && !CancellationCalled)
            {
                try
                {
                    int pbarVal = (int)Math.Round(CurrentFile / (decimal)TotalFiles * 100m, 0,MidpointRounding.AwayFromZero);
                    Dispatcher.Invoke(() =>
                    {
                        StatusLabel.Text = "Saving images... (" + CurrentFile.ToString() + " of " + TotalFiles.ToString() + ")";
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

        private void CancelProcessButton_Click(object sender, RoutedEventArgs e)
        {
            CancellationCalled = true;
            Thread.Sleep(200);
            StatusLabel.Text = "Cancelling...";
            mwInstc.CancelOperation();
        }
    }
}
