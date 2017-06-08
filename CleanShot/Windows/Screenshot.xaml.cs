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

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for ScreenshotWindow.xaml
    /// </summary>
    public partial class Screenshot : Window
    {
        System.Windows.Controls.ToolTip ConfirmTooltip { get; set; } = new System.Windows.Controls.ToolTip();
        System.Windows.Point StartPoint { get; set; }
        bool CaptureStarted { get; set; }
        bool CaptureCompleted { get; set; }
        Bitmap CaptureBitmap { get; set; }
        Graphics CaptureGraphic { get; set; }
        double dpiScale = 1;
        public Screenshot()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dpiScale = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            if (Screen.AllScreens.Length % 2 == 0)
            {
                labelHeader.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            }
            var width = (int)Math.Round(SystemInformation.VirtualScreen.Width * dpiScale);
            var height = (int)Math.Round(SystemInformation.VirtualScreen.Height * dpiScale);
            var left = (int)Math.Round(SystemInformation.VirtualScreen.Left * dpiScale);
            var top = (int)Math.Round(SystemInformation.VirtualScreen.Top * dpiScale);
            CaptureBitmap = new Bitmap((int)width, (int)height);
            CaptureGraphic = Graphics.FromImage(CaptureBitmap);
            CaptureGraphic.CopyFromScreen(left, top, 0, 0, new System.Drawing.Size(width, height));
            using (var ms = new MemoryStream())
            {
                CaptureBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                gridMain.Background = new ImageBrush(bi);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Width = SystemInformation.VirtualScreen.Width;
            this.Height = SystemInformation.VirtualScreen.Height;
            this.Left = SystemInformation.VirtualScreen.Left;
            this.Top = SystemInformation.VirtualScreen.Top;
            rectClip1.Rect = new Rect(0, 0, this.Width, this.Height);
        }
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (ConfirmTooltip.IsOpen == true)
                {
                    ConfirmTooltip.IsOpen = false;
                    rectClip2.Rect = new Rect(0, 0, 0, 0);
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
                        CaptureCompleted = true;
                        ConfirmTooltip.IsOpen = false;
                        borderClip.Visibility = Visibility.Collapsed;
                        labelHeader.Visibility = Visibility.Collapsed;
                        borderCapture.Visibility = Visibility.Collapsed;
                        while (borderClip.IsVisible || labelHeader.IsVisible || ConfirmTooltip.IsVisible || borderCapture.IsVisible)
                        {
                            await Task.Delay(1);
                        }
                        var scaledRect = getScaledRect();
                        CaptureBitmap = new Bitmap((int)scaledRect.Width, (int)scaledRect.Height);
                        CaptureGraphic = Graphics.FromImage(CaptureBitmap);
                        CaptureGraphic.CopyFromScreen(new System.Drawing.Point((int)scaledRect.X + (int)this.Left, (int)scaledRect.Y + (int)this.Top), System.Drawing.Point.Empty, new System.Drawing.Size((int)scaledRect.Width, (int)scaledRect.Height));
                        CaptureGraphic.Save();
                        if (Settings.Current.SaveToDisk)
                        {
                            var count = 0;
                            var saveFile = System.IO.Path.Combine(Settings.Current.SaveFolder, "CleanShot_" + DateTime.Now.ToString().Replace("/", "-").Replace(":", "."));
                            if (File.Exists(saveFile + ".jpg"))
                            {
                                while (File.Exists(saveFile + "_" + count.ToString() + ".jpg"))
                                {
                                    count++;
                                }
                                saveFile += "_" + count.ToString();
                            }

                            CaptureBitmap.Save(saveFile + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        if (Settings.Current.CopyToClipboard)
                        {
                            System.Windows.Forms.Clipboard.SetImage(CaptureBitmap);
                        }
                        App.Current.MainWindow.Visibility = Visibility.Visible;
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
                CaptureCompleted = false;
                CaptureStarted = true;
                StartPoint = e.GetPosition(borderClip);
                rectClip2.Rect = new Rect(StartPoint, StartPoint);
                borderCapture.Margin = new Thickness(0);
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
            var scaledRect = getScaledRect();
            ConfirmTooltip.PlacementRectangle = new Rect(scaledRect.X + this.Left, scaledRect.Y + this.Top, scaledRect.Width, scaledRect.Height);
            ConfirmTooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            ConfirmTooltip.IsOpen = true;
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (CaptureStarted && e.LeftButton == MouseButtonState.Pressed)
            {
                rectClip2.Rect = new Rect(StartPoint, e.GetPosition(borderClip));
                borderCapture.Margin = new Thickness(StartPoint.X, StartPoint.Y, borderClip.ActualWidth - StartPoint.X - rectClip2.Rect.Width, borderClip.ActualHeight - StartPoint.Y - rectClip2.Rect.Height);
            }
        }
        private Rect getScaledRect()
        {
            return new Rect(Math.Round(rectClip2.Rect.X * dpiScale, 0), Math.Round(rectClip2.Rect.Y * dpiScale, 0), Math.Round(rectClip2.Rect.Width * dpiScale, 0), Math.Round(rectClip2.Rect.Height * dpiScale, 0));
        }

    }
}
