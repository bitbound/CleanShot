using CleanShot.Models;
using CleanShot.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CleanShot.Classes
{
    public static class GIFRecorder
    {
        public static RecordingState State { get; set; } = RecordingState.Stopped;
        private static Bitmap PreviousBitmap { get; set; }
        private static GifBitmapEncoder GifEncoder { get; set; }
        private static Bitmap LastFrame { get; set; }
        public static async void Record(Rect Region)
        {
            if (State == RecordingState.Recording)
            {
                return;
            }

            GifEncoder = new GifBitmapEncoder();

            State = RecordingState.Recording;
            PreviousBitmap = new Bitmap((int)Region.Width, (int)Region.Height);
			Stopwatch captureTimer = Stopwatch.StartNew();
            try
            {
                while (State != RecordingState.Stopped)
                {
                    if (State == RecordingState.Paused)
                    {
                        await Task.Delay(10);
                        continue;
                    }

					captureTimer.Restart();
                    using (var screenshot = Screenshot.GetCapture(Region, true))
                    {
                        using (var ms = new MemoryStream())
                        {
                            if (GifEncoder.Frames.Count == 0)
                            {
                                screenshot.Save(ms, ImageFormat.Png);
                            }
                            else
                            {
                                ImageDiff.GetImageDiff(screenshot, LastFrame).Save(ms, ImageFormat.Png);
                            }
                           
                            GifEncoder.Frames.Add(BitmapFrame.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad));
                        }
                      
                        LastFrame = (Bitmap)screenshot.Clone();
                        var delayTime = Math.Max(1, 100 - captureTimer.Elapsed.TotalMilliseconds);
						await Task.Delay((int)delayTime);
					}
                }
            }
            catch (OutOfMemoryException)
            {
                System.Windows.MessageBox.Show("The application has run out of available memory.  Try creating a shorter GIF.", "Out of Memory", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static void Encode()
        {
            try
            {
                if (GifEncoder == null)
                {
                    return;
                }
                var di = Directory.CreateDirectory(Settings.Current.SaveFolder);
                var saveFile = Path.Combine(di.FullName, "CleanShot_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.ff") + ".gif");

          
                using (var ms = new MemoryStream())
                {
                    GifEncoder.Save(ms);
                    var fileBytes = ms.ToArray();
                    var applicationExtension = new byte[] { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
                    var newBytes = new List<byte>();
                    newBytes.AddRange(fileBytes.Take(13));
                    newBytes.AddRange(applicationExtension);
                    newBytes.AddRange(fileBytes.Skip(13));
                    File.WriteAllBytes(saveFile, newBytes.ToArray());
                }
                GifEncoder.Frames.Clear();
                GIFViewer.Create(saveFile);

            }
            catch (OutOfMemoryException)
            {
                System.Windows.MessageBox.Show("The application has run out of available memory.  Try creating a shorter GIF.", "Out of Memory", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public enum RecordingState
        {
            Recording,
            Paused,
            Stopped
        }
    }
}
