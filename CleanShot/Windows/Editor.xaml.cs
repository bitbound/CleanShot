using CleanShot.Classes;
using CleanShot.Controls;
using CleanShot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : Window
    {
        public static Editor Current { get; set; }
        public Bitmap OriginalImage { get; set; }
        public Bitmap EditedImage { get; set; }
        public BitmapFrame ImageSourceFrame { get; set; }
        public System.Drawing.Size SourceSize { get; set; }
        public Graphics Graphic { get; set; }
        public string TopText { get; set; }
        public string BottomText { get; set; }
        public string FontName { get; set; }
        private Editor()
        {
            InitializeComponent();
            this.DataContext = Settings.Current;
            Current = this;
        }
        public static void Create(Bitmap Image)
        {
            var editor = new Editor();
            editor.SourceSize = Image.Size;
            using (var ms = new MemoryStream())
            {
                Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                editor.OriginalImage = Image;
                editor.EditedImage = (Bitmap)Image.Clone();
                editor.ImageSourceFrame = BitmapFrame.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                editor.imageMain.Source = editor.ImageSourceFrame;
            }
            editor.Show();
        }

        private void buttonCreateMeme_Click(object sender, RoutedEventArgs e)
        {
            var meme = new Meme();
            meme.textTop.Text = TopText;
            meme.textBottom.Text = BottomText;
            meme.Owner = this;
            meme.ShowDialog();
            if (String.IsNullOrWhiteSpace(TopText) && String.IsNullOrWhiteSpace(BottomText))
            {
                return;
            }
            var fontFamily = System.Drawing.FontFamily.Families.FirstOrDefault(ff => ff.Name == FontName);
            if (fontFamily == null)
            {
                fontFamily = System.Drawing.FontFamily.GenericSansSerif;
            }
            EditedImage = (Bitmap)OriginalImage.Clone();
            Graphic = Graphics.FromImage(EditedImage);
            Graphic.SmoothingMode = SmoothingMode.AntiAlias;
            int pointSize = 1;
            Font font = new Font(fontFamily, pointSize, System.Drawing.FontStyle.Bold);
            SizeF lastMeasurement = SizeF.Empty;
            System.Drawing.Point drawPoint = System.Drawing.Point.Empty;
            GraphicsPath path = new GraphicsPath();
            if (!String.IsNullOrWhiteSpace(TopText))
            {
                while (lastMeasurement.Width < SourceSize.Width - 25 && pointSize < Settings.Current.MemeMaxFontSize)
                {
                    pointSize++;
                    font = new Font(fontFamily, pointSize, System.Drawing.FontStyle.Bold);
                    lastMeasurement = Graphic.MeasureString(TopText, font);
                }
                pointSize--;
                font = new Font(fontFamily, pointSize, System.Drawing.FontStyle.Bold);
                lastMeasurement = Graphic.MeasureString(TopText, font);
                drawPoint = new System.Drawing.Point((int)(SourceSize.Width / 2), 0);
                path = new GraphicsPath();
                path.AddString(TopText, fontFamily, (int)System.Drawing.FontStyle.Bold, Graphic.DpiY * pointSize / 72, drawPoint, new StringFormat() { Alignment = StringAlignment.Center });
                Graphic.DrawPath(new System.Drawing.Pen(System.Drawing.Brushes.Black, pointSize / 4), path);
                Graphic.FillPath(System.Drawing.Brushes.White, path);
                pointSize = 1;
                font = new Font(fontFamily, pointSize, System.Drawing.FontStyle.Bold);
                lastMeasurement = SizeF.Empty;
            }
           
            if (!String.IsNullOrWhiteSpace(BottomText))
            {
                while (lastMeasurement.Width < SourceSize.Width - 25 && pointSize < Settings.Current.MemeMaxFontSize)
                {
                    pointSize++;
                    font = new Font(fontFamily, pointSize, System.Drawing.FontStyle.Bold);
                    lastMeasurement = Graphic.MeasureString(BottomText, font);
                }
                pointSize--;
                font = new Font(fontFamily, pointSize, System.Drawing.FontStyle.Bold);
                lastMeasurement = Graphic.MeasureString(BottomText, font);
                drawPoint = new System.Drawing.Point((int)(SourceSize.Width / 2), (int)SourceSize.Height - (int)lastMeasurement.Height);
                path = new GraphicsPath();
                path.AddString(BottomText, fontFamily, (int)System.Drawing.FontStyle.Bold, Graphic.DpiY * pointSize / 72, drawPoint, new StringFormat() { Alignment = StringAlignment.Center });
                Graphic.DrawPath(new System.Drawing.Pen(System.Drawing.Brushes.Black, pointSize / 4), path);
                Graphic.FillPath(System.Drawing.Brushes.White, path);
            }

            Graphic.Save();
            using (var ms = new MemoryStream())
            {
                EditedImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ImageSourceFrame = BitmapFrame.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                imageMain.Source = ImageSourceFrame;
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = Settings.Current.SaveFolder;
            dialog.AddExtension = true;
            dialog.Filter = "Image Files (*.png)|*.png";
            dialog.DefaultExt = ".png";
            dialog.ShowDialog();
            
            if (!String.IsNullOrWhiteSpace(dialog.FileName))
            {
                EditedImage.Save(dialog.FileName);
            }
        }

        private void buttonCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(ImageSourceFrame);
            TrayIcon.Icon.ShowCustomBalloon(new CaptureClipboardBalloon(), PopupAnimation.Fade, 5000);
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
            var savePath = Path.Combine(Path.GetTempPath(), "CleanShot_Image.png");
            EditedImage.Save(savePath, ImageFormat.Png);
            var client = new System.Net.WebClient();
            client.UploadProgressChanged += (send, arg) => {
                progress.Value = arg.ProgressPercentage;
            };
            byte[] response = new byte[0];
            try
            {
                response = await client.UploadFileTaskAsync(new Uri("http://invis.me/Services/ImageShare"), savePath);
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
