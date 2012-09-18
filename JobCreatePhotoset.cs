using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlickrUploader
{
    class JobCreatePhotoset : JobFlickrUploader
    {
        public JobCreatePhotoset(DC context, string fullPath, string name, string month, string year, string photoid, string story)
            : base(context, fullPath)
        {
            Photoset = new Album();
            Photoset.Name = name;
            Photoset.Month = month;
            Photoset.Year = year;
            Photoset.Story = story;
            m_photoid = photoid;
        }

        protected override void FlickrExecuteOffThread()
        {
            if (m_photoid != null && m_photoid != "")
            {
                string story = Photoset.Story == null ? null : (Photoset.Story == "" ? null : Photoset.Story);
                string t = Photoset.Name + "(" + Photo.GetMonth(Photoset.Month) + "," + Photoset.Year + ")";
                Photoset.PhotosetId = (Flickr.PhotosetsCreate(t, story, m_photoid)).PhotosetId;
                IsSuccessful = true;
            }
        }

        protected override void FlickrExecuteOnThread()
        {
            if (IsSuccessful == true)
            {
                Context.DSD.Albums.Add(Photoset);
                Context.AddUpdatePhotoWithPhotosetIdJob(Photoset);
            }
            else
            {
                Context.Log = "Failed to create set " + Photoset.Name + " with photoid=(" + m_photoid + ") in folder " + FullPath + "\r\n" + Context.Log;
            }
        }

        public Album Photoset { get; set; }
        private string m_photoid;
    }
}
