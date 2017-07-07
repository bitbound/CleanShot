using CleanShot.Models;
using CleanShot.Windows;
using Microsoft.Expression.Encoder.ScreenCapture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace CleanShot.Classes
{
    public static class Video
    {
        public static void Record(Rect Region)
        {
            var captureJob = new ScreenCaptureJob();
            captureJob.CaptureRectangle = new System.Drawing.Rectangle(Math.Max(SystemInformation.VirtualScreen.Left, (int)Region.X), Math.Max(SystemInformation.VirtualScreen.Top, (int)Region.Y), Math.Min(SystemInformation.VirtualScreen.Width, (int)Region.Width - ((int)Region.Width % 4)), Math.Min(SystemInformation.VirtualScreen.Height, (int)Region.Height) - ((int)Region.Height % 4));
            captureJob.OutputPath = System.IO.Path.Combine(Settings.Current.VideoSaveFolder);
            captureJob.ShowCountdown = true;
            captureJob.CaptureMouseCursor = true;
            captureJob.Start();
            var controls = CaptureControls.Create(captureJob);
            controls.Top = Math.Min(Region.Bottom + 5, Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea.Bottom - controls.Height);
            controls.Left = Region.Left + (Region.Width / 2) - (controls.Width / 2);
            controls.Show();
        }
        public static void Encode(ScreenCaptureJob CaptureJob)
        {
            var mediaItem = new Microsoft.Expression.Encoder.MediaItem(CaptureJob.ScreenCaptureFileName);
            var encodeJob = new Microsoft.Expression.Encoder.Job();
            encodeJob.OutputDirectory = CaptureJob.OutputPath;
            encodeJob.CreateSubfolder = false;
            encodeJob.MediaItems.Add(mediaItem);
            mediaItem.OutputFormat = new Microsoft.Expression.Encoder.WindowsMediaOutputFormat()
            {
                VideoProfile = new Microsoft.Expression.Encoder.Profiles.AdvancedVC1VideoProfile()
                {
                    Size = CaptureJob.CaptureRectangle.Size
                }
            };
            encodeJob.Encode();
            System.Diagnostics.Process.Start("explorer.exe", encodeJob.ActualOutputDirectory);
            File.Delete(CaptureJob.ScreenCaptureFileName);
        }
    }
}
