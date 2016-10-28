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

namespace CleanShot
{
    /// <summary>
    /// Interaction logic for ScreenshotWindow.xaml
    /// </summary>
    public partial class ScreenshotWindow : Window
    {
        System.Windows.Controls.ToolTip toolTip = new System.Windows.Controls.ToolTip();
        System.Windows.Point startPoint { get; set; }
        bool captureStarted { get; set; }
        bool captureCompleted { get; set; }
        double dpiScale = 1;
        public ScreenshotWindow()
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
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Width = SystemInformation.VirtualScreen.Width;
            this.Height = SystemInformation.VirtualScreen.Height;
            this.Left = SystemInformation.VirtualScreen.Left;
            this.Top = SystemInformation.VirtualScreen.Top;
            rectClip1.Rect = new Rect(SystemInformation.VirtualScreen.Left, SystemInformation.VirtualScreen.Top, SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        }
        private async void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (toolTip.IsOpen == true)
                {
                    toolTip.IsOpen = false;
                    rectClip2.Rect = new Rect(0, 0, 0, 0);
                }
                else
                {
                    App.Current.MainWindow.Visibility = Visibility.Visible;
                    this.Close();
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (toolTip.IsOpen == true)
                {
                    try
                    {
                        toolTip.IsOpen = false;
                        this.Visibility = Visibility.Collapsed;
                        while (this.IsVisible || toolTip.IsVisible)
                        {
                            await Task.Delay(1);
                        }
                        captureCompleted = true;
                        var scaledRect = getScaledRect();
                        var bitmap = new System.Drawing.Bitmap((int)scaledRect.Width, (int)scaledRect.Height);
                        var graphic = Graphics.FromImage(bitmap);
                        graphic.CopyFromScreen(new System.Drawing.Point((int)scaledRect.X + (int)this.Left, (int)scaledRect.Y + (int)this.Top), System.Drawing.Point.Empty, new System.Drawing.Size((int)scaledRect.Width, (int)scaledRect.Height));
                        graphic.Save();
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

                            bitmap.Save(saveFile + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        if (Settings.Current.CopyToClipboard)
                        {
                            System.Windows.Forms.Clipboard.SetImage(bitmap);
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
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (toolTip.IsOpen)
            {
                toolTip.IsOpen = false;
            }
            captureCompleted = false;
            captureStarted = true;
            startPoint = e.GetPosition(borderClip);
            rectClip2.Rect = new Rect(startPoint, startPoint);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!captureStarted)
            {
                return;
            }
            toolTip.Content = "Press Enter to confirm capture region.  Press Esc to cancel.";
            toolTip.FontSize = 18;
            toolTip.FontWeight = FontWeights.Bold;
            toolTip.Foreground = new SolidColorBrush(Colors.Red);
            toolTip.PlacementRectangle = getScaledRect();
            toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
            toolTip.IsOpen = true;
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (captureStarted && e.LeftButton == MouseButtonState.Pressed)
            {
                rectClip2.Rect = new Rect(startPoint, e.GetPosition(borderClip));
            }
        }
        private Rect getScaledRect()
        {
            return new Rect(Math.Round(rectClip2.Rect.X * dpiScale, 0), Math.Round(rectClip2.Rect.Y * dpiScale, 0), Math.Round(rectClip2.Rect.Width * dpiScale, 0), Math.Round(rectClip2.Rect.Height * dpiScale, 0));
        }

    }
}
