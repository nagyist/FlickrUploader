using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FlickrUploader
{
    // This is a singleton object which represents everything in the program. It is set as the DC
    public class DC : INPC
    {
        public DC()
        {
            Directories = new DirectoriesToUpload();
            DSD = DataSavedToDisk.Read();
            WorkToDo = new Jobs(this);

            NewJobEvent = new AutoResetEvent(false);

            AddGetPhotosetJob();
        }

        private void AddGetPhotosetJob()
        {
            JobGetPhotosets jgp = new JobGetPhotosets(this);
            WorkToDo.Add(jgp);
            DispatchNextJob();
        }

        public void AddDirectoryToBeProcessed(DirectoryToUpload dtu)
        {
            bool fAdded = false;
            for (int i = 0; i < Directories.Count; i++)
            {
                int left = (Directories[i].Year << 4) + Directories[i].Month;
                int right = (dtu.Year << 4) + dtu.Month;
                if (left < right)
                {
                    Directories.Insert(i, dtu);
                    fAdded = true;
                    break;
                }
            }
            if (!fAdded)
            {
                Directories.Add(dtu);
            }
            DispatchNextJob();
        }

        public void DispatchNextJob()
        {
            while (CurrentJob == null)
            {
                if (WorkToDo.Count == 0)
                {
                    if (Directories.Count != 0)
                    {
                        CurrentDirectory = Directories[0];
                        Directories.RemoveAt(0);
                        EnqueueJobs();
                    }
                    else
                    {
                        // No more work at this point...
                        break;
                    }
                }
                if (WorkToDo.Count > 0)
                {
                    CurrentJob = WorkToDo[0];
                    WorkToDo.RemoveAt(0);
                    NewJobEvent.Set();
                }
            }
        }

        private void EnqueueJobs()
        {
            if (CurrentDirectory.DI == null)
            {
                AddTerminateJob();
            }
            else
            {
                // Remove this once you have finished uploading all the metadata and enable the 2 items below
                // TempAddUpdateInfoJob();

                AddCityJobs();
                AddPhotoJobs();
            }
        }

        private void TempAddUpdateInfoJob()
        {
            Log = "";
            foreach (Photo P in CurrentDirectory.Photos)
            {
                if (P.FlickrId != "" && P.NoShow == false)
                {
                    //TempJobUloadPhoto tjup = new TempJobUloadPhoto(this, CurrentDirectory.DI.FullName, P);
                    TempJobUploadDescription tjud = new TempJobUploadDescription(this, CurrentDirectory.DI.FullName, P);
                    WorkToDo.Add(tjud);
                }
            }
        }

        private void AddTerminateJob()
        {
            TerminateThreadJob ttj = new TerminateThreadJob(this);
            WorkToDo.Add(ttj);
        }

        private void AddCityJobs()
        {
            // Add all the city jobs
            foreach (Photo P in CurrentDirectory.Photos)
            {
                string ThePlace = P.Place.Trim();
                if (ThePlace == "")
                {
                    continue;
                }

                var places = from place in DSD.Places where (place.Name == ThePlace) select place;
                bool found = false;
                foreach (var place in places)
                {
                    found = true;
                    break;
                }
                if (!found)
                {
                    var queuedplacejobs = from job in WorkToDo where job.IsEqual(typeof(JobCityInfo), ThePlace) select job;
                    found = false;
                    foreach (var job in queuedplacejobs)
                    {
                        found = true;
                        break;
                    }
                    if (!found)
                    {
                        JobCityInfo jci = new JobCityInfo(this, CurrentDirectory.DI.FullName, ThePlace);
                        WorkToDo.Add(jci);
                    }
                }
            }
        }

        private void AddPhotoJobs()
        {
#if false
            // Let us collect the keywords here 
            // /\/\/\/\/\ This code should be deleted after we have collected the words /\/\/\/\
            JobCreateTagList jctg = new JobCreateTagList(this, CurrentDirectory);
            WorkToDo.Add(jctg);
#else
            foreach (Photo P in CurrentDirectory.Photos)
            {
                if (P.FlickrId == "")
                {
                    JobUploadPhoto jup = new JobUploadPhoto(this, CurrentDirectory.DI.FullName, P);
                    WorkToDo.Add(jup);
                }
            }
            JobAddAlbumJob jaaj = new JobAddAlbumJob(this, CurrentDirectory.DI.FullName);
            WorkToDo.Add(jaaj);
#endif
        }

        public void AddCreateAlbumJobs()
        {
            foreach (Album A in CurrentDirectory.Albums)
            {
                var al = from DSD_A in DSD.Albums where DSD_A.AreEqual(A) select DSD_A;

                // Did we already create the album? If not, do it now...
                if (al.Count() == 0)
                {
                    Photo photo = null;

                    // Does the Album list a photo as its "Main" photo - if so try to find it - make sure its not marked as noshow
                    var pl = from P in CurrentDirectory.Photos where P.JustTheName == A.Photo select P;
                    foreach (var P in pl)
                    {
                        if (P.NoShow == false)
                        {
                            photo = P;
                        }
                        break;
                    }

                    // Now, if the photo was not found, take the first photo which is in the album and not noshow
                    if (photo == null)
                    {
                        var plNew = from P in CurrentDirectory.Photos where P.AlbumT == A.Name select P;
                        foreach (var P in plNew)
                        {
                            if (P.NoShow == false)
                            {
                                photo = P;
                                break;
                            }
                        }
                    }

                    // If we found an appropriate photo - then let us use it...
                    if (photo != null)
                    {
                        JobCreatePhotoset jcp = new JobCreatePhotoset(this, CurrentDirectory.DI.FullName, A.Name, A.Month, A.Year, photo.FlickrId, A.Story);
                        WorkToDo.Add(jcp);
                    }
                }
                else
                {
                    foreach (var PS in al)
                    {
                        // Album exists - let us go add the job to update the photos with photosetid.
                        AddUpdatePhotoWithPhotosetIdJob(PS);
                        break;
                    }
                }
            }
        }

        public void AddUpdatePhotoWithPhotosetIdJob(Album A)
        {
            JobUpdatePhotoWithPhotosetId jupwpi = new JobUpdatePhotoWithPhotosetId(this, CurrentDirectory.DI.FullName, CurrentDirectory.Photos, A);
            WorkToDo.Add(jupwpi);
        }

        public DirectoryToUpload CurrentDirectory { get; set; }
        public Jobs WorkToDo { get; private set;  }

        public Job CurrentJob
        {
            get { return _currentJob; }
            set { _currentJob = value; FPC("CurrentJob"); }
        }
        private Job _currentJob;
        public EventWaitHandle NewJobEvent;

        public string Log
        {
            get { return _log; }
            set { _log = value; FPC("Log"); }
        }
        private string _log;

        public DirectoriesToUpload Directories { get; set; }
        public DataSavedToDisk DSD { get; set; }
    }
}
