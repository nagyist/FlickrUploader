using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlickrUploader
{
    class JobUpdatePhotoWithPhotosetId : JobFlickrUploader
    {
        public JobUpdatePhotoWithPhotosetId(DC context, string fullPath, List<Photo> pl, Album a)
            : base(context, fullPath)
        {
            m_pl = pl;
            A = a;
        }

        protected override void FlickrExecuteOffThread()
        {
            try
            {
                if (A.PhotosetId != "" && A.PhotosetId != null)
                {
                    FlickrNet.Photoset PS = Flickr.PhotosetsGetPhotos(A.PhotosetId);

                    foreach (var P in m_pl)
                    {
                        if (P.FlickrId != null && P.FlickrId != "" && P.AlbumT == A.Name)
                        {
                            bool bFound = false;
                            foreach (var PSP in PS.PhotoCollection)
                            {
                                if (PSP.PhotoId == P.FlickrId)
                                {
                                    bFound = true;
                                    break;
                                }
                            }
                            if (!bFound)
                            {
                                try
                                {
                                    Flickr.PhotosetsAddPhoto(A.PhotosetId, P.FlickrId);
                                }
                                catch
                                {
                                    Context.Log = "Failed to add photo " + P.JustTheName + " to Album " + P.AlbumT + " in folder " + FullPath + "\r\n" + Context.Log;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Swallow the exceptions here...
            }
        }

        protected override void FlickrExecuteOnThread() { }

        private List<Photo> m_pl;
        public Album A { get; private set; }
    }
}
