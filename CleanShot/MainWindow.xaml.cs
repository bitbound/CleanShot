using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CleanShot.Models;
using System.IO;
using System.Windows.Interop;
using CleanShot.Windows;
using CleanShot.Controls;
using System.Windows.Media.Animation;
using CleanShot.Classes;
using System.Windows.Controls.Primitives;

namespace CleanShot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Current { get; set; }
        public MainWindow()
        {
            App.Current.DispatcherUnhandledException += (send, arg) =>
            {
                arg.Handled = true;
                WriteToLog(arg.Exception);
            };
            App.Current.Exit += (send, arg) =>
            {
                TrayIcon.Icon.Dispose();
                Settings.Save();
            };
            App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            foreach (var proc in Process.GetProcessesByName("CleanShot"))
            {
                if (proc.Id != Process.GetCurrentProcess().Id)
                {
                    proc.Kill();
                }
            }
            InitializeComponent();
            Current = this;
            this.DataContext = Settings.Current;
            CheckArgs();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(true);
            Settings.Load();
            TrayIcon.Create();
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Settings.Current.IsTrayNotificationEnabled)
            {
                TrayIcon.Icon.ShowCustomBalloon(new TrayBalloon(), System.Windows.Controls.Primitives.PopupAnimation.Fade, 5000);
            }
            this.Hide();
            base.OnClosed(e);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            Hotkey.Set();
            base.OnSourceInitialized(e);
        }
        private async void buttonCapture_Click(object sender, RoutedEventArgs e)
        {
            await InitiateCapture();
        }
        private async void menuUpdate_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(false);
        }
        private void menuOptions_Click(object sender, RoutedEventArgs e)
        {
            var options = new Options();
            options.Owner = this;
            options.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            options.ShowDialog();
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new About();
            about.Owner = this;
            about.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            about.ShowDialog();
        }

        private void buttonMenu_Click(object sender, RoutedEventArgs e)
        {
            buttonMenu.ContextMenu.IsOpen = true;
        }

        private async Task CheckForUpdates(bool Silent)
        {
            System.Net.WebClient webClient = new System.Net.WebClient();
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            var result = await httpClient.GetAsync("https://translucency.azurewebsites.net/Services/VersionCheck.cshtml?Path=/Downloads/CleanShot.exe");
            var serverVersion = Version.Parse(await result.Content.ReadAsStringAsync());
            var thisVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (serverVersion > thisVersion)
            {
                var strFilePath = System.IO.Path.GetTempPath() + "CleanShot.exe";
                var msgResult = System.Windows.MessageBox.Show("A new version of CleanShot is available!  Would you like to download it now?  It's a no-fuss, instant process.", "New Version Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (msgResult == MessageBoxResult.Yes)
                {
                    if (File.Exists(strFilePath))
                    {
                        File.Delete(strFilePath);
                    }
                    try
                    {
                        
                        await webClient.DownloadFileTaskAsync(new Uri("https://translucency.azurewebsites.net/Downloads/CleanShot.exe"), strFilePath);
                    }
                    catch
                    {
                        if (!Silent)
                        {
                            System.Windows.MessageBox.Show("Unable to contact the server.  Check your network connection or try again later.", "Server Unreachable", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                        return;
                    }
                    Process.Start(strFilePath, "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                    App.Current.Shutdown();
                    return;
                }
               
            }
            else
            {
                if (!Silent)
                {
                    System.Windows.MessageBox.Show("CleanShot is up-to-date.", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void CheckArgs()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && File.Exists(args[1]))
            {
                var success = false;
                var startTime = DateTime.Now;
                while (success == false)
                {
                    System.Threading.Thread.Sleep(200);
                    if (DateTime.Now - startTime > TimeSpan.FromSeconds(5))
                    {
                        break;
                    }
                    try
                    {
                        File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, args[1], true);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex);
                        continue;
                    }
                }
                if (success == false)
                {
                    System.Windows.MessageBox.Show("Update failed.  Please close all CleanShot windows, then try again.", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    Process.GetProcessesByName("CleanShot").ToList().ForEach(p => p.Kill());
                }
                else
                {
                    System.Windows.MessageBox.Show("Update successful!  CleanShot will now restart.", "Update Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(args[1]);
                }
                App.Current.Shutdown();
                return;
            }
        }

        public async Task InitiateCapture()
        {
            if (Settings.Current.CaptureMode == Settings.CaptureModes.Image)
            {
                this.Visibility = Visibility.Collapsed;
                await Task.Delay(500);
                new Screenshot().ShowDialog();
            }
            else if (Settings.Current.CaptureMode == Settings.CaptureModes.Video)
            {

            }
        }
        private void WriteToLog(Exception ExMessage)
        {
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\");
            var ex = ExMessage;
            while (ex != null)
            {
                File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\Log.txt", DateTime.Now.ToString() + "\t" + ex.Message + "\t" + ex.Source + "\t" + ex.StackTrace + Environment.NewLine);
                ex = ex.InnerException;
            }
        }

        private void CaptureModeToggled(object sender, RoutedEventArgs e)
        {
            buttonImage.IsChecked = false;
            buttonVideo.IsChecked = false;
            (sender as ToggleButton).IsChecked = true;
        }
    }
}
