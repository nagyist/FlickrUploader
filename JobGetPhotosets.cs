using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlickrUploader
{
    class JobGetPhotosets : JobFlickrUploader
    {
        public JobGetPhotosets(DC context) : base(context, "None") { }

        protected override void FlickrExecuteOffThread()
        {
            try
            {
                FlickrNet.Photosets photosets = Flickr.PhotosetsGetList();
                if (photosets != null)
                {
                    foreach (var PS in photosets.PhotosetCollection)
                    {
                        Album A = new Album();

                        string s = PS.Title; // Format is Title(Month, Year)
                        var ss = s.Split('(');

                        A.Name = ss[0].Trim();

                        if (ss.Length == 2)
                        {
                            ss = ss[1].Split(',');
                            if (ss.Length == 2)
                            {
                                A.Month = Photo.FromMonth(ss[0]).ToString();

                                ss = ss[1].Split(')');
                                if (ss.Length == 2 && A.Month != "0")
                                {
                                    A.Year = ss[0];
                                    A.PhotosetId = PS.PhotosetId;
                                    Context.DSD.Albums.Add(A);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        protected override void FlickrExecuteOnThread()
        {
        }
    }
}
