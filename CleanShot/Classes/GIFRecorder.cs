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
        private static Bitmap previousBitmap { get; set; }
        private static string tempPath { get; set; } = Path.Combine(Path.GetTempPath(), "CleanShot");
        public static void Record(Rect Region)
        {
            if (State == RecordingState.Recording)
            {
                return;
            }
            if (Directory.Exists(tempPath))
            {
                try
                {
                    Directory.Delete(tempPath, true);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Unable to remove temp files.  Try closing and reopening CleanShot.", "File Delete Failure", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            var di = Directory.CreateDirectory(tempPath);
            State = RecordingState.Recording;
            previousBitmap = new Bitmap((int)Region.Width, (int)Region.Height);
            var timer = new DispatcherTimer();
            var ticks = 0;
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (send, arg) =>
            {
                if (State == RecordingState.Stopped)
                {
                    (send as DispatcherTimer).Stop();
                    return;
                }
                if (State == RecordingState.Paused)
                {
                    return;
                }
                using (var screenshot = Screenshot.GetCapture(Region, true))
                {
                    using (var ms = new MemoryStream())
                    {
                        screenshot.Save(Path.Combine(di.FullName, ticks.ToString() + ".png"), ImageFormat.Png);                        
                    }
                    ticks++;
                }
            };
            timer.Start();
        }
        public static void Encode()
        {
            if (!Directory.Exists(tempPath) || Directory.GetFiles(tempPath).Length == 0)
            {
                return;
            }
            var gifEncoder = new GifBitmapEncoder();

            for (var i = 0; i < Directory.GetFiles(tempPath).Length; i++)
            {
                if (i == 0)
                {
                    using (var fs = new FileStream(Path.Combine(tempPath, i.ToString() + ".png"), FileMode.Open, FileAccess.Read))
                    {
                        gifEncoder.Frames.Add(BitmapFrame.Create(fs, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad));
                    }
                }
                else
                {
                    using (var bitmap1 = (Bitmap)Bitmap.FromFile(Path.Combine(tempPath, i.ToString() + ".png")))
                    {
                        using (var bitmap2 = (Bitmap)Bitmap.FromFile(Path.Combine(tempPath, (i - 1).ToString() + ".png")))
                        {
                            var diff = ImageDiff.GetDifference(bitmap1, bitmap2);
                            using (var ms = new MemoryStream())
                            {
                                try
                                {
                                    diff.Save(ms, ImageFormat.Png);
                                }
                                catch
                                {
                                    bitmap1.Save(ms, ImageFormat.Png);
                                }
                                gifEncoder.Frames.Add(BitmapFrame.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad));
                            }
                        }
                    }
                }
            }
            var di = Directory.CreateDirectory(Settings.Current.SaveFolder);
            var saveFile = Path.Combine(di.FullName, "CleanShot_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.ff") + ".gif");
            using (var fs = new FileStream(saveFile, FileMode.Create))
            {
                gifEncoder.Save(fs);
            }
            gifEncoder.Frames.Clear();

            try
            {
                Directory.Delete(tempPath, true);
            }
            catch {
                System.Windows.MessageBox.Show("Unable to remove temp files.  Try closing and reopening CleanShot.", "File Delete Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Process.Start(Settings.Current.SaveFolder);
        }
        public enum RecordingState
        {
            Recording,
            Paused,
            Stopped
        }
    }
}
