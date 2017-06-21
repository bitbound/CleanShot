using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CleanShot.Controls
{
    /// <summary>
    /// Interaction logic for WindowSelectionBorder.xaml
    /// </summary>
    public partial class WindowSelectionBorder : Window
    {
        public static WindowSelectionBorder Current { get; set; }
        public WindowSelectionBorder()
        {
            InitializeComponent();
            Current = this;
        }
    }
}
