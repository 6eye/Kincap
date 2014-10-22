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
using MahApps.Metro.Controls;

namespace Kincap.Views
{
    /// <summary>
    /// Logique d'interaction pour SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : MetroWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();

            this.dropDown_fps.Items.Add("30");
            this.dropDown_fps.Items.Add("15");
            this.dropDown_fps.Items.Add("10");
            this.dropDown_fps.Items.Add("5");
            this.dropDown_fps.Items.Add("1");

            this.dropDown_smooth.Items.Add("Default");
            this.dropDown_smooth.Items.Add("High");
            this.dropDown_smooth.Items.Add("Very High");

        }

        public void button_ok_Click(object sender, RoutedEventArgs e)
        {
            Models.Settings.FpsSetting = int.Parse(this.dropDown_fps.Text);
            Models.Settings.SmoothSetting = this.dropDown_smooth.Text;
            Models.Settings.ReplayEnable = (bool)this.switchButton_replay.IsChecked;
            Models.Settings.SeatedMode = (bool)this.switchButton_seatMode.IsChecked;
            Models.Settings.NearMode = (bool)this.switchButton_nearMode.IsChecked;

            Models.Settings.SetSettings();

            Window window = Window.GetWindow(this);
            window.Close();
        }

        public void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            window.Close();
        }
        

    }
}
