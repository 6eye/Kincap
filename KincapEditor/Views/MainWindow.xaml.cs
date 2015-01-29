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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace Kincap.Views
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }

        public void button_settings_Click(object sender, RoutedEventArgs e)
        {
            Views.SettingsWindow sw = new Views.SettingsWindow();
            sw.Owner = this;
            sw.ShowDialog();
        }

        public void button_infos_Click(object sender, RoutedEventArgs e)
        {
            infosFlyout.IsOpen = true;
        }
    }
}
