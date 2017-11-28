using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for Install.xaml
    /// </summary>
    public partial class Install : Window
    {
        public Install()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var client = new WebClient();
            var filePath = Path.Combine(Path.GetTempPath(), "Encoder_en.exe");
            if (!File.Exists(filePath))
            {
                client.DownloadProgressChanged += (send, arg) =>
                {
                    progressBar.Value = arg.ProgressPercentage;
                };
                await client.DownloadFileTaskAsync(new Uri("http://invis.me/Downloads/Encoder_en.exe"), filePath);
            }
            progressBar.Value = 0;
            textProcess.Text = "Installing";
            progressBar.IsIndeterminate = true;
            await Task.Run(()=> {
                var proc = Process.Start(filePath, "-q");
                proc.WaitForExit();
            });
            MessageBox.Show("Installation completed. You can now record video.", "Install Completed", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
