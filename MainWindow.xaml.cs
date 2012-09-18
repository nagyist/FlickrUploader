using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Data;
using System.Windows.Controls;

namespace FlickrUploader
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DRThread != null)
            {
                DRThread.Abort();
                JDThread.Abort();
            }
            e.Cancel = false;
        }

        public MainWindow()
        {
            InitializeComponent();

            TheDC = new DC();
            Main = this;
            DataContext = TheDC;

            StartJobDispatcherThread();

            //RescanDirs(null, null);
        }

        private void StartDirectoryReaderThread()
        {
            if (DRThread == null || DRThread.ThreadState != ThreadState.Running)
            {
                TheDC.Directories.Clear();
                DRThread = new Thread(DirectoryReader.DirectoryReaderThread);
                DRThread.Start();
            }
        }

        private void StartJobDispatcherThread()
        {
            if (JDThread == null || JDThread.ThreadState != ThreadState.Running)
            {
                JDThread = new Thread(TheDC.WorkToDo.JobsDispatcherThread);
                JDThread.Start();
            }
        }

        public void OneDirectoryAvailable(DirectoryToUpload dtu)
        {
            if (dtu.DI == null || dtu.FileCount != 0 || CheckingSets)
            {
                TheDC.AddDirectoryToBeProcessed(dtu);
                if (dtu.DI == null)
                {
                    Rescan.IsEnabled = true;
                }
            }
        }

        private void RescanDirs(object sender, RoutedEventArgs e)
        {
            CheckingSets = false;
            RescanDirsImpl();
        }

        private void RescanDirsImpl()
        {
            Rescan.IsEnabled = false;
            TheDC.Log = "";
            StartDirectoryReaderThread();
        }

        private void TogglePauseButton(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            b.Content = b.IsEnabled ? "Play" : "Pause";
            if (TheDC.WorkToDo != null)
            {
                TheDC.WorkToDo.Pause = !TheDC.WorkToDo.Pause;
            }
        }

        private void CheckSets(object sender, RoutedEventArgs e)
        {
            CheckingSets = true;
            RescanDirsImpl();
        }

        public static DC TheDC;
        public static MainWindow Main;

        private Thread DRThread;
        private Thread JDThread;

        private bool CheckingSets;
    }

    public class PTV : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            uint p = (uint)value;
            return p == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
