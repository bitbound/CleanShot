using CleanShot.Models;
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

namespace CleanShot.Windows
{
    /// <summary>
    /// Interaction logic for Meme.xaml
    /// </summary>
    public partial class Meme : Window
    {
        public Meme()
        {
            InitializeComponent();
            this.DataContext = Settings.Current;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var family in System.Drawing.FontFamily.Families)
            {
                var combo = new ComboBoxItem();
                combo.Content = family.Name;
                comboFont.Items.Add(combo);
            }
        }
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Editor.Current.TopText = textTop.Text;
            Editor.Current.BottomText = textBottom.Text;
            Editor.Current.FontName = comboFont.SelectionBoxItem.ToString();
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Editor.Current.TopText = null;
            Editor.Current.BottomText = null;
            Editor.Current.FontName = null;
            this.Close();
        }
    }
}
