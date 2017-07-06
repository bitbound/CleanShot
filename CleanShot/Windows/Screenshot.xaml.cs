using System.Resources;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.IO;
using CleanShot.Models;
using CleanShot.Win32;
using System.Runtime.InteropServices;
using CleanShot.Controls;
using CleanShot.Classes;
using Microsoft.Expression.Encoder.ScreenCapture;
using System.Windows.Controls.Primitives;

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for ScreenshotWindow.xaml
    /// </summary>
    public partial class Screenshot : Window
    {
        public static Screenshot Current { get; set; }
        System.Windows.Controls.ToolTip ConfirmTooltip { get; set; } = new System.Windows.Controls.ToolTip();
        System.Windows.Point StartPoint { get; set; }
        double DpiScale { get; set; } = 1;
        bool ManualRegionSelection { get; set; } = false;
        ScreenCaptureJob CaptureJob { get; set; }
        public Bitmap BackgroundImage { get; set; }
        public Screenshot()
        {
            InitializeComponent();
            Current = this;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            var screen = SystemInformation.VirtualScreen;
            labelHeader.Visibility = Visibility.Visible;
            borderCapture.Visibility = Visibility.Visible;
            this.Width = screen.Width;
            this.Height = screen.Height;
            this.Left = screen.Left;
            this.Top = screen.Top;
            if (Settings.Current.CaptureMode == Settings.CaptureModes.Image)
            {
                using (var ms = new MemoryStream())
                {
                    BackgroundImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = ms;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    bi.Freeze();
                    gridMain.Background = new ImageBrush(bi);
                }
            }
            FrameWindowUnderCursor();
            this.Activate();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (CaptureControls.Current?.IsVisible != true)
            {
                MainWindow.Current.WindowState = WindowState.Normal;
                if (Editor.Current.IsVisible)
                {
                    Editor.Current.Activate();
                }
            }
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ConfirmTooltip.IsOpen == true)
                {
                    ConfirmTooltip.IsOpen = false;
                    ManualRegionSelection = false;
                    FrameWindowUnderCursor();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
            if (e.ChangedButton == MouseButton.Left)
            {
                if (ConfirmTooltip.IsOpen)
                {
                    ConfirmTooltip.IsOpen = false;
                }
                StartPoint = e.GetPosition(this);
            }
            
        }

        private async void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (ConfirmTooltip.IsOpen == true)
                {
                    if (Settings.Current.CaptureMode == Settings.CaptureModes.Image)
                    {
                        try
                        {
                            await HideSelf();
                            Capture.SaveCapture(Capture.GetCapture(GetDrawnRegion()));
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            var thisEx = ex;
                            var errorMessage = "There was an error capturing the screenshot.  If the issue persists, please contact me with the below error." + Environment.NewLine + Environment.NewLine + "Error: " + ex.Message + Environment.NewLine + Environment.NewLine + ex.StackTrace;
                            while (thisEx.InnerException != null)
                            {
                                errorMessage += Environment.NewLine + Environment.NewLine + ex.InnerException?.Message + Environment.NewLine + Environment.NewLine + ex.InnerException?.StackTrace;
                                thisEx = thisEx.InnerException;
                            }
                            System.Windows.MessageBox.Show(errorMessage, "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            this.Close();
                        }
                    }
                    else if (Settings.Current.CaptureMode == Settings.CaptureModes.Video)
                    {
                        await HideSelf();
                        CaptureJob = new ScreenCaptureJob();
                        var captureRect = GetDrawnRegion();
                        CaptureJob.CaptureRectangle = new System.Drawing.Rectangle(Math.Max(SystemInformation.VirtualScreen.Left, (int)captureRect.X), Math.Max(SystemInformation.VirtualScreen.Top, (int)captureRect.Y), Math.Min(SystemInformation.VirtualScreen.Width, (int)captureRect.Width - ((int)captureRect.Width % 4)), Math.Min(SystemInformation.VirtualScreen.Height, (int)captureRect.Height) - ((int)captureRect.Height % 4));
                        CaptureJob.OutputPath = System.IO.Path.Combine(Settings.Current.VideoSaveFolder);
                        CaptureJob.ShowCountdown = true;
                        CaptureJob.CaptureMouseCursor = true;
                        CaptureJob.Start();
                        var controls = CaptureControls.Create(this.CaptureJob);
                        controls.Top = Math.Min(captureRect.Bottom + 5, Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea.Bottom - controls.Height);
                        controls.Left = captureRect.Left + (captureRect.Width / 2) - (controls.Width / 2);
                        controls.Show();
                        this.Close();
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                ManualRegionSelection = true;
                ConfirmTooltip.Content = "Right-click to confirm capture region.  Press Esc to cancel.";
                ConfirmTooltip.FontSize = 15;
                ConfirmTooltip.FontWeight = FontWeights.Bold;
                ConfirmTooltip.Foreground = new SolidColorBrush(Colors.Blue);
                var scaledRect = GetDrawnRegion();
                ConfirmTooltip.PlacementRectangle = new Rect(scaledRect.X, scaledRect.Y, scaledRect.Width, scaledRect.Height);
                ConfirmTooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
                ConfirmTooltip.IsOpen = true;
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ManualRegionSelection = true;
                var pos = e.GetPosition(this);
                var left = Math.Min(pos.X, StartPoint.X);
                var top = Math.Min(pos.Y, StartPoint.Y);
                var width = Math.Abs(StartPoint.X - pos.X);
                var height = Math.Abs(StartPoint.Y - pos.Y);
                borderCapture.Margin = new Thickness(left, top, 0, 0);
                borderCapture.Width = width;
                borderCapture.Height = height;
            }
        }
        private Rect GetDrawnRegion()
        {
            return new Rect(Math.Round(borderCapture.Margin.Left * DpiScale + SystemInformation.VirtualScreen.Left, 0), Math.Round(borderCapture.Margin.Top * DpiScale + SystemInformation.VirtualScreen.Top, 0), Math.Round(borderCapture.Width * DpiScale, 0), Math.Round(borderCapture.Height * DpiScale, 0));
        }
        private async Task HideSelf()
        {
            ConfirmTooltip.IsOpen = false;
            labelHeader.Visibility = Visibility.Collapsed;
            borderCapture.Visibility = Visibility.Collapsed;
            while (labelHeader.IsVisible || ConfirmTooltip.IsVisible || borderCapture.IsVisible)
            {
                await Task.Delay(100);
            }
        }
        private async void FrameWindowUnderCursor()
        {
            var point = new System.Drawing.Point();
            var rect = new User32.RECT();
            var screen = SystemInformation.VirtualScreen;
            var winList = new List<IntPtr>();
            var thisHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            var shellHandle = User32.GetShellWindow();
            var desktopHandle = User32.GetDesktopWindow();
            while (this.IsVisible && ManualRegionSelection == false)
            {
                User32.GetCursorPos(out point);
                winList.Clear();
                var devenv = System.Diagnostics.Process.GetProcesses().FirstOrDefault(proc => proc.MainWindowTitle.Contains("CleanShot") && proc.MainWindowTitle.Contains("Microsoft"));
                User32.EnumWindows((hWin, lParam) => {
                    if (hWin == thisHandle || hWin == shellHandle || hWin == desktopHandle || !User32.IsWindowVisible(hWin))
                    {
                        return true;
                    }
                    User32.GetWindowRect(hWin, out rect);
                    if (rect.Width == screen.Width && rect.Height == screen.Height)
                    {
                        return true;
                    }
                    if (rect.Left < point.X && rect.Top < point.Y && rect.Right > point.X && rect.Bottom > point.Y)
                    {
                        winList.Add(hWin);
                    }
                    return true;
                }, IntPtr.Zero);
                if (winList.Count > 0)
                {

                    User32.GetWindowRect(winList.First(), out rect);
                }
                else
                {
                    User32.GetWindowRect(shellHandle, out rect);
                }
                borderCapture.Margin = new Thickness(rect.Left - screen.Left, rect.Top - screen.Top, 0, 0);
                borderCapture.Width = rect.Width;
                borderCapture.Height = rect.Height;
                await Task.Delay(100);
            }
        }

    
    }
}
