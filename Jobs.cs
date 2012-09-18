using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading;

namespace FlickrUploader
{
    public class TerminateThreadJob : Job
    {
        static TerminateThreadJob() { Priority = 100; }
        public TerminateThreadJob(DC context) : base(context, "The End!") { }

        protected override void ExecuteOffThread() { }
        protected override void ExecuteOnThread() { }
        protected override bool IsEqualOverride(object other) { return true; }
    }

    public abstract class Job : INPC
    {
        public Job(DC context, string fullPath) { _context = context; FullPath = fullPath;  IsSuccessful = false; }
        public DC Context { get { return _context; } }
        public static uint Priority { get; set; }
        public bool IsSuccessful { get; protected set; }
        protected string FullPath { get; private set; }

        private delegate void JobDoneDelegate(Job j);
        private static void JobDone(Job mThis)
        {
            mThis.ExecuteOnThread();
            mThis._context.CurrentJob = null;
            mThis._context.DispatchNextJob();   
        }

        protected abstract void ExecuteOffThread();
        protected abstract void ExecuteOnThread();
        protected abstract bool IsEqualOverride(object other);
        public    virtual void Abandon() {}

        public bool IsEqual(Type expectedType, object other)
        {
            return ((this.GetType() == expectedType) && IsEqualOverride(other));
        }

        internal void Execute()
        {
            ExecuteOffThread();
            MainWindow.Main.Dispatcher.BeginInvoke(new JobDoneDelegate(JobDone), this);
        }

        private DC _context;
    }

    public class Jobs : ObservableCollection<Job> 
    {
        public Jobs(DC context)
        {
            _context = context;
        }

        private bool InExecutionPeriod()
        {
            if (Pause) { return false; }

#if false
            DateTime now = DateTime.Now;

            // 1am to 4am on all days is allowed
            if (now.Hour >= 1 && now.Hour < 4)
            {
                return true;
            }

            return false;
#else
            return true;
#endif
        }

        public void JobsDispatcherThread()
        {
            while (true)
            {
                _context.NewJobEvent.WaitOne();

                bool fTerminate = _context.CurrentJob.GetType() == typeof(TerminateThreadJob);

                if (!fTerminate)
                {
                    while (!InExecutionPeriod())
                    {
                        // Check every 2 minutes
                        Thread.Sleep(new TimeSpan(0, 2, 0));
                    }
                }

                _context.CurrentJob.Execute();

                // Main thread asked us to terminate the thread...
                if (fTerminate)
                {
                    break;
                }
            }
        }

        public bool Pause 
        { 
            get { return _pause; }
            set
            {
                _pause = value;
                if (_pause && _context.CurrentJob != null)
                {
                    _context.CurrentJob.Abandon();
                }
            }
        }
        private bool _pause;
        private DC _context;
    }
}
