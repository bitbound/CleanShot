using CleanShot.Windows;
using Microsoft.Expression.Encoder.ScreenCapture;
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
using System.Windows.Shapes;

namespace CleanShot.Controls
{
    /// <summary>
    /// Interaction logic for CaptureControls.xaml
    /// </summary>
    public partial class CaptureControls : UserControl
    {
        public static CaptureControls Current { get; set; }
        public ScreenCaptureJob CaptureJob { get; set; }
        public CaptureControls()
        {
            Current = this;
            InitializeComponent();
        }

        private void buttonPauseResume_Click(object sender, RoutedEventArgs e)
        {
            if (buttonPauseResume.Content.ToString() == "Pause")
            {
                CaptureJob.Pause();
                buttonPauseResume.Content = "Resume";
            }
            else
            {
                CaptureJob.Resume();
                buttonPauseResume.Content = "Pause";
            }
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            CaptureJob.Stop();
            var outFilePath = new Microsoft.Expression.Encoder.MediaItem(CaptureJob.ScreenCaptureFileName);
            Screenshot.Current.Close();
        }
    }
}
