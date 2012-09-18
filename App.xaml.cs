using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;

namespace FlickrUploader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Count() == 1)
            {
                string path = e.Args[0] as string;
                StartPath = new DirectoryInfo(path);
            }
            else
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                StartPath = new DirectoryInfo(path);
                StartPath = StartPath.Parent;
            }
            base.OnStartup(e);
        }

        public DirectoryInfo StartPath { get; private set; }
    }
}
