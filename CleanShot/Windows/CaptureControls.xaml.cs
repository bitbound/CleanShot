using CleanShot.Classes;
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
using System.Windows.Forms;
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
        public TimeSpan CaptureDuration { get; set; }

        private CaptureRecordingFrame captureRecordingFrame { get; set; }

        public static async Task Create(Rect Region)
        {
            var controls = new CaptureControls();
            controls.captureRecordingFrame = new CaptureRecordingFrame();
            controls.Top = Math.Min(Region.Bottom + 5, Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea.Bottom - controls.Height);
            controls.Left = Region.Left + (Region.Width / 2) - (controls.Width / 2);
            controls.captureRecordingFrame.Top = Math.Max(0, Region.Top - 5);
            controls.captureRecordingFrame.Left = Math.Max(0, Region.Left - 5);
            controls.captureRecordingFrame.Width = Region.Width + 10;
            controls.captureRecordingFrame.Height = Region.Height + 10;
            controls.Show();
            controls.captureRecordingFrame.Show();
            await Task.Delay(1000);
            while (controls.captureRecordingFrame.countdownText.Text != "0")
            {
                controls.captureRecordingFrame.countdownText.Text = (int.Parse(controls.captureRecordingFrame.countdownText.Text) - 1).ToString();
                await Task.Delay(1000);
            }
            controls.captureRecordingFrame.countdownText.Visibility = Visibility.Collapsed;
            do
            {
                await Task.Delay(100);
            }
            while (controls.captureRecordingFrame.countdownText.IsVisible);

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            var lastTick = DateTime.Now;
            timer.Tick += (send, arg) =>
            {
                if (GIFRecorder.State == GIFRecorder.RecordingState.Stopped)
                {
                    (send as System.Windows.Threading.DispatcherTimer).Stop();
                    return;
                }
                if (GIFRecorder.State == GIFRecorder.RecordingState.Recording)
                {
                    controls.CaptureDuration += DateTime.Now - lastTick;
                    controls.textTimer.Text = controls.CaptureDuration.ToString("hh':'mm':'ss':'ff");
                    lastTick = DateTime.Now;
                }
            };
            timer.Start();

            GIFRecorder.Record(Capture.Current.GetDrawnRegion(true));
        }
        private CaptureControls()
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
            GIFRecorder.State = GIFRecorder.RecordingState.Paused;
            buttonPause.Visibility = Visibility.Collapsed;
            buttonResume.Visibility = Visibility.Visible;
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            GIFRecorder.State = GIFRecorder.RecordingState.Recording;
            buttonPause.Visibility = Visibility.Visible;
            buttonResume.Visibility = Visibility.Collapsed;
        }
        private async void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            textWait.Visibility = Visibility.Visible;
            GIFRecorder.State = GIFRecorder.RecordingState.Stopped;
            await Task.Delay(100);
            buttonStop.IsEnabled = false;
            buttonResume.IsEnabled = false;
            buttonPause.IsEnabled = false;
            GIFRecorder.Encode();
            captureRecordingFrame.Close();
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
