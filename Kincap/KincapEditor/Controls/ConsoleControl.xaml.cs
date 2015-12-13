using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace Kincap.Controls
{
    /// <summary>
    /// Interaction logic for ConsoleControl.xaml
    /// </summary>
    public partial class ConsoleControl : UserControl
    {
        public static int index;
        public static ScrollViewer ScrollViewer;

        public static ObservableCollection<LogEntry> LogEntries { get; set; }

        public ConsoleControl()
        {
            InitializeComponent();

            DataContext = LogEntries = new ObservableCollection<LogEntry>();

            ContentPresenter cp = scroller_logs.ItemContainerGenerator.ContainerFromIndex(0) as ContentPresenter;
            ScrollViewer sv = FindVisualChild<ScrollViewer>(cp);
            if (sv != null)
            {
                ScrollViewer = sv;
            }
        }



        public static T FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        return (T)child;
                    }

                    T childItem = FindVisualChild<T>(child);
                    if (childItem != null) return childItem;
                }
            }
            return null;
        }
    }

    public class LogEntry : PropertyChangedBase
    {
        public DateTime DateTime { get; set; }

        public int Index { get; set; }

        public string Message { get; set; }

        public LogEntry(DateTime _date, string _message)
        {
            Index = ++ConsoleControl.index;
            DateTime = _date;
            Message = _message;

            //auto scroll
            if(ConsoleControl.ScrollViewer != null)
                ConsoleControl.ScrollViewer.ScrollToBottom();
        }
    }

    public class PropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
            }));
        }
    }
}
