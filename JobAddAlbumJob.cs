using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlickrUploader
{
    class JobAddAlbumJob : Job
    {
        public JobAddAlbumJob(DC context, string fullPath) : base(context, fullPath) { }
        protected override void ExecuteOffThread() { }
        protected override void ExecuteOnThread() { Context.AddCreateAlbumJobs(); }
        protected override bool IsEqualOverride(object other) { return true; }
    }

    class NotUsedNowJobCreateTagList : Job
    {
        public NotUsedNowJobCreateTagList(DC context, DirectoryToUpload CurrentDirectory) : base(context, CurrentDirectory.DI.FullName) 
        {
            m_CurrentDirectory = CurrentDirectory;
            m_tagsforalbum = "";
        }
        protected override bool IsEqualOverride(object other) { return true; }

        protected override void ExecuteOffThread()
        {
            bool first = true;
            foreach (Photo P in m_CurrentDirectory.Photos)
            {
                {
                    var AL = from al in m_CurrentDirectory.Albums where al.Name == P.AlbumT select al;
                    Album A = null;
                    foreach (var a in AL)
                    {
                        A = a;
                        break;
                    }
                    if (A != null)
                    {
                        string ps = P.GetTags(A, Context.DSD.IgnoreWords);
                        if (ps != null && ps != "")
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                m_tagsforalbum += ",";
                            }
                            m_tagsforalbum += ps;
                        }
                    }
                }
            }
        }

        protected override void ExecuteOnThread() 
        {
            if (m_tagsforalbum != null)
            {
                Context.DSD.AddTags(m_tagsforalbum);

                Context.Log = "Count of words = " + Context.DSD.TagWords.Count().ToString() + "\r\n" + Context.Log;
            }
        }

        private DirectoryToUpload m_CurrentDirectory;
        private string m_tagsforalbum;
    }
}
