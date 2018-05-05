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
using System.Windows.Controls.Primitives;

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for ScreenshotWindow.xaml
    /// </summary>
    public partial class Capture : Window
    {
        public static Capture Current { get; set; }
        public Bitmap BackgroundImage { get; set; }
        private System.Windows.Controls.ToolTip confirmTooltip { get; set; } = new System.Windows.Controls.ToolTip();
        private System.Windows.Point startPoint { get; set; }
        private double dpiScale { get; set; } = 1;
        private bool manualRegionSelection { get; set; } = false;
        private Models.CaptureMode captureMode { get; set; }

        private Capture()
        {
            InitializeComponent();
            Current = this;
        }
        public static async Task Start(Models.CaptureMode captureMode)
        {
            var capture = new Capture();
            capture.captureMode = captureMode;
            MainWindow.Current.WindowState = WindowState.Minimized;
            if (captureMode == Models.CaptureMode.PNG)
            {
                await Task.Delay(500);
                var screen = SystemInformation.VirtualScreen;
                capture.BackgroundImage = Classes.Screenshot.GetCapture(new Rect(screen.Left, screen.Top, screen.Width, screen.Height), Settings.Current.CaptureCursor);
            }
            capture.ShowDialog();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            var screen = SystemInformation.VirtualScreen;
            labelHeader.Visibility = Visibility.Visible;
            borderCapture.Visibility = Visibility.Visible;
            this.Width = screen.Width;
            this.Height = screen.Height;
            this.Left = screen.Left;
            this.Top = screen.Top;
            if (captureMode == Models.CaptureMode.PNG)
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
                if (Editor.Current?.IsVisible == true)
                {
                    Editor.Current.Activate();
                }
            }
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (confirmTooltip.IsOpen == true)
                {
                    confirmTooltip.IsOpen = false;
                    manualRegionSelection = false;
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
                if (confirmTooltip.IsOpen)
                {
                    confirmTooltip.IsOpen = false;
                }
                startPoint = e.GetPosition(this);
            }

        }

        private async void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (confirmTooltip.IsOpen == true)
                {
                    if (captureMode == Models.CaptureMode.PNG)
                    {
                        try
                        {
                            await HideAllButBackground();
                            Screenshot.SaveCapture(Screenshot.GetCapture(GetDrawnRegion(true), false));
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show("There was an error capturing the screenshot.  If the issue persists, please contact translucency@outlook.com.", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            MainWindow.Current.WriteToLog(ex);
                            this.Close();
                        }
                    }
                    else if (captureMode == Models.CaptureMode.GIF)
                    {
                        CaptureControls controls = null;
                        try
                        {
                            await HideAllButBackground();
                            var region = GetDrawnRegion(false);
                            await CaptureControls.Create(region);
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            controls?.Close();
                            System.Windows.MessageBox.Show("There was an error recording the video.  If the issue persists, please contact translucency@outlook.com.", "Capture Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            MainWindow.Current.WriteToLog(ex);
                            this.Close();
                        }

                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                manualRegionSelection = true;
                confirmTooltip.Content = "Right-click to confirm capture region.  Press Esc to cancel.";
                confirmTooltip.FontSize = 15;
                confirmTooltip.FontWeight = FontWeights.Bold;
                confirmTooltip.Foreground = new SolidColorBrush(Colors.Blue);
                var scaledRect = GetDrawnRegion(true);
                confirmTooltip.PlacementRectangle = new Rect(scaledRect.X, scaledRect.Y, scaledRect.Width, scaledRect.Height);
                confirmTooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
                confirmTooltip.IsOpen = true;
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                manualRegionSelection = true;
                var pos = e.GetPosition(this);
                var left = Math.Min(pos.X, startPoint.X);
                var top = Math.Min(pos.Y, startPoint.Y);
                var width = Math.Abs(startPoint.X - pos.X);
                var height = Math.Abs(startPoint.Y - pos.Y);
                borderCapture.Margin = new Thickness(left, top, 0, 0);
                borderCapture.Width = width;
                borderCapture.Height = height;
            }
        }
        public Rect GetDrawnRegion(bool scaleWithDPI)
        {
            if (scaleWithDPI)
            {
                return new Rect(Math.Round(borderCapture.Margin.Left * dpiScale + SystemInformation.VirtualScreen.Left, 0), Math.Round(borderCapture.Margin.Top * dpiScale + SystemInformation.VirtualScreen.Top, 0), Math.Round(borderCapture.Width * dpiScale, 0), Math.Round(borderCapture.Height * dpiScale, 0));
            }
            else
            {
                return new Rect(Math.Round(borderCapture.Margin.Left + SystemInformation.VirtualScreen.Left, 0), Math.Round(borderCapture.Margin.Top + SystemInformation.VirtualScreen.Top, 0), Math.Round(borderCapture.Width, 0), Math.Round(borderCapture.Height, 0));
            }
        }
        private async Task HideAllButBackground()
        {
            confirmTooltip.IsOpen = false;
            labelHeader.Visibility = Visibility.Collapsed;
            borderCapture.Visibility = Visibility.Collapsed;
            while (labelHeader.IsVisible || confirmTooltip.IsVisible || borderCapture.IsVisible)
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
            while (this.IsVisible && manualRegionSelection == false)
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
