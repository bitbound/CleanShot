using Microsoft.Expression.Encoder.ScreenCapture;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for CaptureControls.xaml
    /// </summary>
    public partial class CaptureControls : Window
    {
        public static CaptureControls Current { get; set; }
        public ScreenCaptureJob CaptureJob { get; set; }

        public static CaptureControls Create(ScreenCaptureJob Job)
        {
            var controls = new CaptureControls() { CaptureJob = Job };
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (send, arg) =>
            {
                if (CaptureControls.Current.CaptureJob.Status == RecordStatus.Stopped)
                {
                    (send as System.Windows.Threading.DispatcherTimer).Stop();
                    return;
                }
                controls.textTimer.Text = controls.CaptureJob.Statistics.Duration.ToString("hh':'mm':'ss':'ff");
            };
            timer.Start();
            return controls;
        }
        public CaptureControls()
        {
            Current = this;
            InitializeComponent();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.Current.WindowState = WindowState.Normal;
        }
        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            CaptureJob.Pause();
            buttonPause.Visibility = Visibility.Collapsed;
            buttonResume.Visibility = Visibility.Visible;
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            CaptureJob.Resume();
            buttonPause.Visibility = Visibility.Visible;
            buttonResume.Visibility = Visibility.Collapsed;
        }
        private async void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            textWait.Visibility = Visibility.Visible;
            await Task.Delay(100);
            buttonStop.IsEnabled = false;
            buttonResume.IsEnabled = false;
            buttonPause.IsEnabled = false;
            CaptureJob.Stop();
            var mediaItem = new Microsoft.Expression.Encoder.MediaItem(CaptureJob.ScreenCaptureFileName);
            var encodeJob = new Microsoft.Expression.Encoder.Job();
            encodeJob.OutputDirectory = CaptureJob.OutputPath;
            encodeJob.CreateSubfolder = false;
            encodeJob.MediaItems.Add(mediaItem);
            mediaItem.OutputFormat = new Microsoft.Expression.Encoder.WindowsMediaOutputFormat()
            {
                VideoProfile = new Microsoft.Expression.Encoder.Profiles.AdvancedVC1VideoProfile()
                {
                    Size = CaptureJob.CaptureRectangle.Size
                }
            };
            encodeJob.Encode();
            System.Diagnostics.Process.Start("explorer.exe", encodeJob.ActualOutputDirectory);
            File.Delete(CaptureJob.ScreenCaptureFileName);
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
