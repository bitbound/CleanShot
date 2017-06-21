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
        bool CaptureStarted { get; set; }
        bool CaptureCompleted { get; set; }
        Bitmap CaptureBitmap { get; set; }
        BitmapImage BackgroundImage { get; set; }
        Graphics CaptureGraphic { get; set; }
        double DpiScale { get; set; } = 1;
        public Screenshot()
        {
            InitializeComponent();
            Current = this;
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            var screen = SystemInformation.VirtualScreen;
            await GetCapture(new Rect(screen.Left, screen.Top, screen.Width, screen.Height));
            borderClip.Visibility = Visibility.Visible;
            labelHeader.Visibility = Visibility.Visible;
            borderCapture.Visibility = Visibility.Visible;
            rectClip1.Rect = new Rect(screen.Left, screen.Top, screen.Width, screen.Height);
            this.Width = screen.Width;
            this.Height = screen.Height;
            this.Left = screen.Left;
            this.Top = screen.Top;
            
            using (var ms = new MemoryStream())
            {
                CaptureBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                BackgroundImage = new BitmapImage();
                BackgroundImage.BeginInit();
                BackgroundImage.StreamSource = ms;
                BackgroundImage.CacheOption = BitmapCacheOption.OnLoad;
                BackgroundImage.EndInit();
                BackgroundImage.Freeze();
                gridMain.Background = new ImageBrush(BackgroundImage);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ConfirmTooltip.IsOpen == true)
                {
                    ConfirmTooltip.IsOpen = false;
                    rectClip2.Rect = new Rect(0, 0, 0, 0);
                    borderCapture.Visibility = Visibility.Collapsed;
                }
                else
                {
                    App.Current.MainWindow.Visibility = Visibility.Visible;
                    this.Close();
                }
            }
        }

        private async void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (ConfirmTooltip.IsOpen == true)
                {
                    try
                    {
                        await GetCapture(GetDrawnRegion());
                        SaveCapture();
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
                        App.Current.MainWindow.Visibility = Visibility.Visible;
                        this.Close();
                    }
                }
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                if (ConfirmTooltip.IsOpen)
                {
                    ConfirmTooltip.IsOpen = false;
                }
                borderCapture.Width = 0;
                borderCapture.Height = 0;
                CaptureCompleted = false;
                CaptureStarted = true;
                StartPoint = e.GetPosition(this);
                rectClip2.Rect = new Rect(StartPoint, StartPoint);
                borderCapture.Margin = new Thickness(StartPoint.X, StartPoint.Y, 0, 0);
                borderCapture.Visibility = Visibility.Visible;
            }
            
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!CaptureStarted || CaptureCompleted)
            {
                return;
            }
            ConfirmTooltip.Content = "Right-click to confirm capture region.  Press Esc to cancel.";
            ConfirmTooltip.FontSize = 18;
            ConfirmTooltip.FontWeight = FontWeights.Bold;
            ConfirmTooltip.Foreground = new SolidColorBrush(Colors.Blue);
            var scaledRect = GetDrawnRegion();
            ConfirmTooltip.PlacementRectangle = new Rect(scaledRect.X + this.Left, scaledRect.Y + this.Top, scaledRect.Width, scaledRect.Height);
            ConfirmTooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            ConfirmTooltip.IsOpen = true;
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (CaptureStarted && e.LeftButton == MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(this);
                rectClip2.Rect = new Rect(StartPoint, pos);

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
            return new Rect(Math.Round(rectClip2.Rect.X * DpiScale, 0), Math.Round(rectClip2.Rect.Y * DpiScale, 0), Math.Round(rectClip2.Rect.Width * DpiScale, 0), Math.Round(rectClip2.Rect.Height * DpiScale, 0));
        }

        private async Task GetCapture(Rect CaptureRegion)
        {
            CaptureCompleted = true;
            ConfirmTooltip.IsOpen = false;
            borderClip.Visibility = Visibility.Collapsed;
            labelHeader.Visibility = Visibility.Collapsed;
            borderCapture.Visibility = Visibility.Collapsed;
            while (borderClip.IsVisible || labelHeader.IsVisible || ConfirmTooltip.IsVisible || borderCapture.IsVisible)
            {
                await Task.Delay(1);
            }
            CaptureBitmap = new Bitmap((int)CaptureRegion.Width, (int)CaptureRegion.Height);
            CaptureGraphic = Graphics.FromImage(CaptureBitmap);
            var screen = SystemInformation.VirtualScreen;

            IntPtr hWnd = IntPtr.Zero;
            IntPtr hDC = IntPtr.Zero;
            IntPtr graphDC = IntPtr.Zero;
            try
            {
                hWnd = User32.GetDesktopWindow();
                hDC = User32.GetWindowDC(hWnd);
                graphDC = CaptureGraphic.GetHdc();
                var copyResult = GDI32.BitBlt(graphDC, 0, 0, (int)CaptureRegion.Width, (int)CaptureRegion.Height, hDC, (int)CaptureRegion.Left, (int)CaptureRegion.Top, GDI32.TernaryRasterOperations.SRCCOPY | GDI32.TernaryRasterOperations.CAPTUREBLT);
                if (!copyResult)
                {
                    throw new Exception("Screen capture failed.");
                }
                CaptureGraphic.ReleaseHdc(graphDC);
                User32.ReleaseDC(hWnd, hDC);
                // Get cursor information to draw on the screenshot.
                var ci = new User32.CursorInfo();
                ci.cbSize = Marshal.SizeOf(ci);
                User32.GetCursorInfo(out ci);
                if (ci.flags == User32.CURSOR_SHOWING)
                {
                    using (var icon = System.Drawing.Icon.FromHandle(ci.hCursor))
                    {
                        CaptureGraphic.DrawIcon(icon, ci.ptScreenPos.x, ci.ptScreenPos.y);
                    }
                }
            }
            catch (Exception ex)
            {
                CaptureGraphic.ReleaseHdc(graphDC);
                User32.ReleaseDC(hWnd, hDC);
                throw ex;
            }
        }
        private void SaveCapture()
        {
            if (Settings.Current.SaveToDisk)
            {
                var count = 0;
                var saveFile = System.IO.Path.Combine(Settings.Current.SaveFolder, "CleanShot_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"));
                if (File.Exists(saveFile + ".png"))
                {
                    while (File.Exists(saveFile + "_" + count.ToString() + ".png"))
                    {
                        count++;
                    }
                    saveFile += "_" + count.ToString();
                }
                Directory.CreateDirectory(Settings.Current.SaveFolder);
                CaptureBitmap.Save(saveFile + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            if (Settings.Current.CopyToClipboard)
            {
                System.Windows.Forms.Clipboard.SetImage(CaptureBitmap);
            }
            App.Current.MainWindow.Visibility = Visibility.Visible;
            this.Close();
        }
        private async void WatchCursor()
        {
            new WindowSelectionBorder().Show();
            while (true)
            {
                CheckCursorPos();
                await Task.Delay(5);
            }
        }
        private void CheckCursorPos()
        {
            var point = new System.Drawing.Point();
            User32.GetCursorPos(out point);
            var hWin = User32.WindowFromPoint(point);
            var rect = new User32.RECT();
            User32.GetWindowRect(hWin, out rect);
            WindowSelectionBorder.Current.Left = rect.Left;
            WindowSelectionBorder.Current.Top = rect.Top;
            WindowSelectionBorder.Current.Height = rect.Height;
            WindowSelectionBorder.Current.Width = rect.Width;
        }
    }
}
