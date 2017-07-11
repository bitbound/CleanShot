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
using CleanShot.Models;
using System.IO;
using System.Windows.Interop;
using CleanShot.Windows;
using CleanShot.Controls;
using System.Windows.Media.Animation;
using CleanShot.Classes;
using System.Windows.Controls.Primitives;
using System.Net;

namespace CleanShot
{
    public partial class MainWindow : Window
    {
        public static MainWindow Current { get; set; }
        public MainWindow()
        {
            App.Current.DispatcherUnhandledException += (send, arg) =>
            {
                arg.Handled = true;
                WriteToLog(arg.Exception);
                System.Windows.MessageBox.Show("An unhandled error has occurred.  If the issue persists, please contact translucency@outlook.com.", "Unhandled Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            App.Current.Exit += (send, arg) =>
            {
                Settings.Save();
                if (!TrayIcon.Icon.IsDisposed)
                {
                    TrayIcon.Icon.Dispose();
                }
            };
            App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            foreach (var proc in Process.GetProcessesByName("CleanShot"))
            {
                if (proc.Id != Process.GetCurrentProcess().Id)
                {
                    proc.Kill();
                }
            }
            while (Process.GetProcessesByName("CleanShot").Any(proc=>proc.Id != Process.GetCurrentProcess().Id))
            {
                System.Threading.Thread.Sleep(1);
            }
            InitializeComponent();
            Current = this;
            this.DataContext = Settings.Current;
            WPF_Auto_Update.Updater.ServiceURI = "https://translucency.azurewebsites.net/Services/VersionCheck.cshtml?Path=/Downloads/CleanShot.exe";
            WPF_Auto_Update.Updater.RemoteFileURI = "https://translucency.azurewebsites.net/Downloads/CleanShot.exe";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.CommandLine.Contains("-hidden"))
            {
                this.Hide();
            }
            Settings.Load();
            CheckInstallItems();
            TrayIcon.Create();
            WPF_Auto_Update.Updater.CheckCommandLineArgs();
            await WPF_Auto_Update.Updater.CheckForUpdates(true);
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            if (Settings.Current.IsTrayNotificationEnabled)
            {
                TrayIcon.Icon.ShowCustomBalloon(new TrayBalloon(), PopupAnimation.Fade, 5000);
            }
            this.Hide();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            Hotkey.Set();
            base.OnSourceInitialized(e);
        }
        private void buttonCapture_Click(object sender, RoutedEventArgs e)
        {
            InitiateCapture();
        }
        private void menuOpenImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Settings.Current.ImageSaveFolder;
            dialog.AddExtension = true;
            dialog.Filter = "Image Files (*.png;*.jpg)|*.png;*.jpg";
            dialog.DefaultExt = ".png";
            dialog.ShowDialog();
            if (!String.IsNullOrWhiteSpace(dialog.FileName))
            {
                var bitmap = (Bitmap)Bitmap.FromFile(dialog.FileName);
                Editor.Create(bitmap);
            }
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
        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown(0);
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

        public async void InitiateCapture()
        {
            if (Settings.Current.CaptureMode == Settings.CaptureModes.Video)
            {
                var expKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Expression\Encoder\4.0", false);
                if (expKey == null)
                {
                    var result = System.Windows.MessageBox.Show("The video recording feature requires Microsoft's Expression Encoder 4 SDK.  Would you like to download and install it now (~25MB)?", "Expression Encoder Required", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var install = new Install();
                        install.Owner = this;
                        install.ShowDialog();
                    }
                    return;
                }
            }
            var win = new Windows.Capture();
            this.WindowState = WindowState.Minimized;
            await Task.Delay(500);
            if (Settings.Current.CaptureMode == Settings.CaptureModes.Image)
            {
                var screen = SystemInformation.VirtualScreen;
                win.BackgroundImage = Classes.Screenshot.GetCapture(new Rect(screen.Left, screen.Top, screen.Width, screen.Height), Settings.Current.CaptureCursor);
            }
            win.ShowDialog();
        }
        private void CheckInstallItems()
        {
            var di = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\");
            if (!File.Exists(di.FullName + "CleanShot.exe"))
            {
                File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, di.FullName + "CleanShot.exe", true);
            }
            if (Settings.Current.StartWithWindows)
            {
                var runKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (runKey.GetValue("CleanShot") == null)
                {
                    runKey.SetValue("CleanShot", @"""%appdata%\CleanShot\CleanShot.exe"" -hidden", Microsoft.Win32.RegistryValueKind.ExpandString);
                }
            }
           
            using (var mrs = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CleanShot.Assets.CleanShot.lnk"))
            {
                var buffer = new byte[mrs.Length];
                mrs.Read(buffer, 0, buffer.Length);
                if (Settings.Current.CreateDesktopShortcut)
                {
                    var desktopPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "CleanShot.lnk");
                    if (!File.Exists(desktopPath))
                    {
                        File.WriteAllBytes(desktopPath, buffer);
                    }
                }
                if (Settings.Current.CreateStartMenuItem)
                {
                    var startDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "CleanShot");
                    if (!File.Exists(System.IO.Path.Combine(startDir, "CleanShot.lnk")))
                    {
                        Directory.CreateDirectory(startDir);
                        File.WriteAllBytes(System.IO.Path.Combine(startDir, "CleanShot.lnk"), buffer);
                    }
                }
               
            }
        }
        public void WriteToLog(Exception ExMessage)
        {
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\");
            var ex = ExMessage;
            while (ex != null)
            {
                File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\Log.txt", DateTime.Now.ToString() + "\t" + ex.Message + "\t" + ex.StackTrace + Environment.NewLine);
                ex = ex.InnerException;
            }
        }

        private void CaptureModeToggled(object sender, RoutedEventArgs e)
        {
            buttonImage.IsChecked = false;
            buttonVideo.IsChecked = false;
            (sender as ToggleButton).IsChecked = true;
            Settings.Current.CaptureMode = (Settings.CaptureModes)Enum.Parse(typeof(Settings.CaptureModes), (sender as ToggleButton).Tag.ToString());
        }

        private void buttonCapture_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ellipseCaptureBackground.Fill = new SolidColorBrush(Colors.AliceBlue);
        }

        private void buttonCapture_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ellipseCaptureBackground.Fill = null;
        }
    }
}
