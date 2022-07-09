using CleanShot.Models;
using CleanShot.Properties;
using CleanShot.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CleanShot.Classes
{
    public static class TrayIcon
    {
        public static NotifyIcon Icon { get; set; }
        public static void Create()
        {
            Icon?.Dispose();
            Icon = new NotifyIcon();

            using (var iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CleanShot.Assets.CameraEmbedded.ico"))
            {
                Icon.Icon = new Icon(iconStream);
            }

            CreateContextMenu();

            Icon.MouseDoubleClick += (send, arg) => {
                if (!App.Current.MainWindow.IsVisible)
                {
                    App.Current.MainWindow.Show();
                }
            };

            Icon.Visible = true;
        }

        private static void CreateContextMenu()
        {
            Icon.ContextMenu = new System.Windows.Forms.ContextMenu();
            MenuItem item;
            item = new MenuItem() { Text = "Show" };
            item.Click += (send, arg) =>
            {
                MainWindow.Current.Show();
            };
            Icon.ContextMenu.MenuItems.Add(item);
            item = new MenuItem() { Text = "Capture Image" };
            item.Click += async (send, arg) =>
            {
                await Capture.Start(Models.CaptureMode.PNG);
            };
            Icon.ContextMenu.MenuItems.Add(item);
            item = new MenuItem() { Text = "Capture GIF" };
            item.Click += async (send, arg) =>
            {
                await Capture.Start(Models.CaptureMode.GIF);
            };
            Icon.ContextMenu.MenuItems.Add(item);
            item = new MenuItem() { Text = "Exit" };
            item.Click += (send, arg) =>
            {
                App.Current.Shutdown(0);
            };
            Icon.ContextMenu.MenuItems.Add(item);
        }
    }
}
