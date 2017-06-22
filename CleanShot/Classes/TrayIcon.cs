using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CleanShot.Classes
{
    public static class TrayIcon
    {
        public static TaskbarIcon Icon { get; set; }
        public static void Create()
        {
            if (Icon != null)
            {
                return;
            }
            Icon = new TaskbarIcon();
            Icon.IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Camera.ico"));
            CreateContextMenu();
            Icon.TrayMouseDoubleClick += (send, arg) => {
                if (!App.Current.MainWindow.IsVisible)
                {
                    App.Current.MainWindow.Show();
                }
            };
            Icon.TrayRightMouseUp += (send, arg) => {
                Icon.ContextMenu.IsOpen = true;
            };
        }

        private static void CreateContextMenu()
        {
            Icon.ContextMenu = new ContextMenu();
            var item = new MenuItem() { Header = "Show" };
            item.Click += (send, arg) =>
            {
                MainWindow.Current.Show();
            };
            Icon.ContextMenu.Items.Add(item);
            item = new MenuItem() { Header = "Exit" };
            item.Click += (send, arg) =>
            {
                App.Current.Shutdown(0);
            };
            Icon.ContextMenu.Items.Add(item);
        }
    }
}
