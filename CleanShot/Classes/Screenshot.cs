using CleanShot.Controls;
using CleanShot.Models;
using CleanShot.Win32;
using CleanShot.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace CleanShot.Classes
{
    public static class Screenshot
    {
        public static Bitmap GetCapture(Rect CaptureRegion)
        {
            var bitmap = new Bitmap((int)CaptureRegion.Width, (int)CaptureRegion.Height);
            var graphic = Graphics.FromImage(bitmap);
            var screen = SystemInformation.VirtualScreen;

            IntPtr hWnd = IntPtr.Zero;
            IntPtr hDC = IntPtr.Zero;
            IntPtr graphDC = IntPtr.Zero;
            try
            {
                hWnd = User32.GetDesktopWindow();
                hDC = User32.GetWindowDC(hWnd);
                graphDC = graphic.GetHdc();
                var copyResult = GDI32.BitBlt(graphDC, 0, 0, (int)CaptureRegion.Width, (int)CaptureRegion.Height, hDC, (int)CaptureRegion.Left, (int)CaptureRegion.Top, GDI32.TernaryRasterOperations.SRCCOPY | GDI32.TernaryRasterOperations.CAPTUREBLT);
                if (!copyResult)
                {
                    throw new Exception("Screen capture failed.");
                }
                graphic.ReleaseHdc(graphDC);
                User32.ReleaseDC(hWnd, hDC);
                // Get cursor information to draw on the screenshot.
                var ci = new User32.CursorInfo();
                ci.cbSize = Marshal.SizeOf(ci);
                User32.GetCursorInfo(out ci);
                if (ci.flags == User32.CURSOR_SHOWING)
                {
                    using (var icon = System.Drawing.Icon.FromHandle(ci.hCursor))
                    {
                        graphic.DrawIcon(icon, ci.ptScreenPos.x - screen.Left, ci.ptScreenPos.y - screen.Top);
                    }
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                graphic.ReleaseHdc(graphDC);
                User32.ReleaseDC(hWnd, hDC);
                throw ex;
            }
        }
        public static void SaveCapture(Bitmap CaptureBitmap)
        {
            if (Settings.Current.SaveToDisk)
            {
                var count = 0;
                var saveFile = Path.Combine(Settings.Current.ImageSaveFolder, "CleanShot_" + DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss"));
                if (File.Exists(saveFile + ".png"))
                {
                    while (File.Exists(saveFile + "_" + count.ToString() + ".png"))
                    {
                        count++;
                    }
                    saveFile += "_" + count.ToString();
                }
                Directory.CreateDirectory(Settings.Current.ImageSaveFolder);
                CaptureBitmap.Save(saveFile + ".png", System.Drawing.Imaging.ImageFormat.Png);
                TrayIcon.Icon.ShowCustomBalloon(new CaptureFileBalloon(), PopupAnimation.Fade, 5000);
            }
            if (Settings.Current.CopyToClipboard)
            {
                System.Windows.Forms.Clipboard.SetImage(CaptureBitmap);
                if (!Settings.Current.SaveToDisk)
                {
                    TrayIcon.Icon.ShowCustomBalloon(new CaptureClipboardBalloon(), PopupAnimation.Fade, 5000);
                }
            }
            if (Settings.Current.OpenInEditor)
            {
                Editor.Create(CaptureBitmap);
            }
        }
    }
}
