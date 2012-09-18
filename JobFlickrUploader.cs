using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlickrUploader
{
    public abstract class JobFlickrUploader : Job
    {
        private static FlickrNet.Flickr g_FR;
        private static FlickrNet.Auth g_Auth;
        //private static string g_AuthToken = "72157612276635695-47b1eb63b137deb9";
        private static string g_AuthToken = "72157627522316731-90ebed85c2a0a5df";

        static JobFlickrUploader()
        {
            g_FR = new FlickrNet.Flickr("1d39fd2053d23729f64981fd2dcd3cdb", "3dd5b228858000ed", g_AuthToken);
            g_Auth = g_FR.AuthCheckToken(g_AuthToken);
            g_FR.OnUploadProgress += new FlickrNet.Flickr.UploadProgressHandler(OffThreadOnUploadProgress);
        }

        private delegate void OnUploadProgressDelegate(uint progress);
        private void OnThreadOnUploadProgress(uint progress)
        {
            OnFlickrProgress(progress);
        }

        private static bool OffThreadOnUploadProgress(object sender, FlickrNet.UploadProgressEventArgs e)
        {
            if (_abandonJob)
            {
                return false;
            }
            else
            {
                uint progress = ((uint)e.Bytes * 100) / ((uint)e.TotalBytes);
                MainWindow.Main.Dispatcher.BeginInvoke(new OnUploadProgressDelegate(_currentJob.OnThreadOnUploadProgress), progress);
                return true;
            }
        }

        public JobFlickrUploader(DC context, string fullPath) : base(context, fullPath) { }

        protected abstract void FlickrExecuteOffThread();
        protected abstract void FlickrExecuteOnThread();
        protected virtual void OnFlickrProgress(uint progress) 
        {
            Progress = progress;
        }

        protected override sealed void ExecuteOffThread()
        {
            try
            {
                _abandonJob = false;
                _currentJob = this;
                Progress = 0;
                FlickrExecuteOffThread();
            }
            finally
            {
                Progress = 100;
                _abandonJob = false;
                _currentJob = null;
            }
        }

        protected override sealed void ExecuteOnThread()
        {
            try
            {
                _abandonJob = false;
                _currentJob = this;
                FlickrExecuteOnThread();
            }
            finally
            {
                _abandonJob = false;
                _currentJob = null;
            }
        }

        protected override bool IsEqualOverride(object other)
        {
            return (this.GetType() == typeof(JobFlickrUploader));
        }

        private static JobFlickrUploader _currentJob;
        public override void Abandon()
        {
            if (_currentJob == this)
            {
                _abandonJob = true;
            }
        }
        private static bool _abandonJob;
        public FlickrNet.Flickr Flickr { get { return g_FR; } }
        public uint Progress { get { return _progress; } set { _progress = value; FPC("Progress"); } }
        private uint _progress;
    }
}
