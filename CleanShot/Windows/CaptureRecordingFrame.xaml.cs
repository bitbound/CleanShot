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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for CaptureRecordingFrame.xaml
    /// </summary>
    public partial class CaptureRecordingFrame : Window
    {
        public CaptureRecordingFrame()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var tick = 0;
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (send, args) =>
            {
                if (tick == 2)
                {
                    topLeftFrame.Visibility = Visibility.Collapsed;
                    topRightFrame.Visibility = Visibility.Collapsed;
                    bottomRightFrame.Visibility = Visibility.Collapsed;
                    bottomLeftFrame.Visibility = Visibility.Collapsed;
                }
                else if (tick == 3)
                {
                    topLeftFrame.Visibility = Visibility.Visible;
                    topRightFrame.Visibility = Visibility.Visible;
                    bottomRightFrame.Visibility = Visibility.Visible;
                    bottomLeftFrame.Visibility = Visibility.Visible;
                    tick = 0;
                    return;
                }
                tick++;
            };
            timer.Start();
        }
    }
}
