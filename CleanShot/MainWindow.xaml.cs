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

namespace CleanShot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            App.Current.DispatcherUnhandledException += DispatcherUnhandledException;
            InitializeComponent();
            this.DataContext = Settings.Current;
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && System.IO.File.Exists(args[1]))
            {
                var count = 0;
                var success = false;
                while (success == false)
                {
                    System.Threading.Thread.Sleep(200);
                    count++;
                    if (count > 25)
                    {
                        break;
                    }
                    try
                    {
                        System.IO.File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, args[1], true);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        WriteToLog(ex.Message + "\t" +  ex.StackTrace);
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

        private void DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            WriteToLog(e.Exception.Message + e.Exception.StackTrace + Environment.NewLine);
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(true);
            var fileInfo = new System.IO.FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\Settings.json");
            if (fileInfo.Exists)
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Settings));
                var fs = new System.IO.FileStream(fileInfo.FullName, System.IO.FileMode.OpenOrCreate);
                var settings = (Settings)serializer.ReadObject(fs);
                Settings.Current.CopyToClipboard = settings.CopyToClipboard;
                Settings.Current.SaveToDisk = settings.SaveToDisk;
                Settings.Current.SaveFolder = settings.SaveFolder;
                Settings.Current.AlwaysOnTop = settings.AlwaysOnTop;
            }
        }

        private async void buttonCapture_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            await Task.Delay(500);
            new ScreenshotWindow().ShowDialog();
        }

        private async void menuFeature_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("CleanShot is already packed with an exhaustive list of features, so I'm not taking any additional requests.  Thanks for your understanding.", "Features Maxed", MessageBoxButton.OK, MessageBoxImage.Information);
            await Task.Delay(4000);
            AskIfFunny();
        }

        private async void AskIfFunny()
        {
            var result = System.Windows.MessageBox.Show("Did you enjoy the previous message?  In your opinion, was it humorous?", "Feature Survey", MessageBoxButton.YesNo, MessageBoxImage.Question);
            await Task.Delay(500);
            AskToSend();
        }
        private async void AskToSend()
        {
            var result = System.Windows.MessageBox.Show("Thank you for your feedback.  Can I send your response to the developer?", "Send Feedback", MessageBoxButton.YesNo, MessageBoxImage.Question);
            await Task.Delay(500);
            if (result == MessageBoxResult.Yes)
            {
                System.Windows.MessageBox.Show("Just kidding.  I'm not really going to send your responses.  But seriously, feel free to send me any suggestions via the contact form on my website (https://translucency.info).", "Notification of Joke", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show("Well, I'm going to send it anyway.", "Disregarding User Input Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                await Task.Delay(4000);
                AskIfFunny();
            }
        }

        private void menuOptions_Click(object sender, RoutedEventArgs e)
        {
            var options = new OptionsWindow();
            options.Owner = this;
            options.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            options.ShowDialog();
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
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
            var result = await httpClient.GetAsync("https://translucency.info/Services/VersionCheck.cshtml?Path=/Downloads/CleanShot.exe");
            var serverVersion = Version.Parse(await result.Content.ReadAsStringAsync());
            var thisVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (serverVersion > thisVersion)
            {
                var strFilePath = System.IO.Path.GetTempPath() + "CleanShot.exe";
                var msgResult = System.Windows.MessageBox.Show("A new version of CleanShot is available!  Would you like to download it now?  It's a no-fuss, instant process.", "New Version Available", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (msgResult == MessageBoxResult.Yes)
                {
                    if (System.IO.File.Exists(strFilePath))
                    {
                        System.IO.File.Delete(strFilePath);
                    }
                    try
                    {
                        
                        await webClient.DownloadFileTaskAsync(new Uri("https://translucency.info/Downloads/CleanShot.exe"), strFilePath);
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

        private async void menuUpdate_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdates(false);
        }
        private void WriteToLog(string Message)
        {
            System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\");
            System.IO.File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CleanShot\Log.txt", DateTime.Now.ToString() + "\t-\t" + Message + Environment.NewLine);
        }
    }
}
