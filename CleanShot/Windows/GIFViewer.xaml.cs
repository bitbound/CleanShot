using CleanShot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for GIFViewer.xaml
    /// </summary>
    public partial class GIFViewer : Window
    {
#if DEBUG
        private readonly string _uploadUrl = "https://localhost:5001/api/file";
#else
        private readonly string _uploadUrl = "https://clipshare.jaredg.dev/api/file";
#endif

        private GIFViewer()
        {
            InitializeComponent();
        }
        private string SaveFilePath { get; set; }
        internal static void Create(string saveFile)
        {
            var gifViewer = new GIFViewer();
            gifViewer.SaveFilePath = saveFile;
            gifViewer.mediaElement.Source = new Uri(saveFile);
            gifViewer.Show();
        }
        private void buttonOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Settings.Current.SaveFolder);
        }

        private async void buttonShare_Click(object sender, RoutedEventArgs e)
        {
            var popup = new ToolTip();
            popup.BorderBrush = new SolidColorBrush(Colors.LightGray);
            popup.BorderThickness = new Thickness(2);
            popup.Background = new SolidColorBrush(Colors.Black);
            var stack = new StackPanel();
            stack.Margin = new Thickness(5);
            var text = new TextBlock() { Text = "Uploading image...", Foreground = new SolidColorBrush(Colors.White), FontSize = 20, Margin = new Thickness(0, 5, 0, 5) };
            var progress = new ProgressBar() { Foreground = Foreground = new SolidColorBrush(Colors.SteelBlue), Margin = new Thickness(0, 5, 0, 5), Height = 15 };
            stack.Children.Add(text);
            stack.Children.Add(progress);
            popup.Content = stack;
            popup.PlacementTarget = this;
            popup.Placement = PlacementMode.Center;
            popup.IsOpen = true;
            var client = new System.Net.WebClient();
            client.UploadProgressChanged += (send, arg) => {
                progress.Value = arg.ProgressPercentage;
            };

            byte[] response = new byte[0];
            try
            {
                response = await client.UploadFileTaskAsync(new Uri(_uploadUrl), SaveFilePath);
            }
            catch (System.Net.WebException)
            {
                popup.IsOpen = false;
                MessageBox.Show("There was a problem uploading the image.  Your internet connection may not be working, or the web service may be temporarily unavailable.", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var strResponse = Encoding.UTF8.GetString(response);
            popup.IsOpen = false;
            Process.Start(strResponse);
        }

    }
}
