using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CleanShot.Controls
{
    /// <summary>
    /// Interaction logic for LinkClipboardBalloon.xaml
    /// </summary>
    public partial class LinkClipboardBalloon : UserControl
    {
        public LinkClipboardBalloon()
        {
            InitializeComponent();
        }
        private void UserControl_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            (this.Parent as Popup).IsOpen = false;
        }
    }
}
