using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Globalization;

namespace FlickrUploader
{
    public class Rect64
    {
        private ulong uu;
        private double ww, hh;

        public Rect64(ulong u, double w, double h)
        {
            Rect64Init(u, w, h);
        }

        public Rect64(string us, double w, double h)
        {
            ulong u = 0;
            if (ulong.TryParse(us, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out u))
            {
                uu = u;
            }
            Rect64Init(u, w, h);
        }

        private void Rect64Init(ulong u, double w, double h)
        {
            uu = u;
            ww = w / 0xffff;
            hh = h / 0xffff;
        }

        public double Left { get { return ((double)((uu & 0xffff000000000000) >> 48)) * ww; } }
        public double Top { get { return ((double)((uu & 0x0000ffff00000000) >> 32)) * hh; } }
        public double Right { get { return ((double)((uu & 0x00000000ffff0000) >> 16)) * ww; } }
        public double Bottom { get { return ((double)((uu & 0x000000000000ffff) >> 0)) * hh; } }
        public double Width { get { return Right - Left; } }
        public double Height { get { return Bottom - Top; } }
    }

    public class TempJobUloadPhoto : JobUploadPhoto
    {
        public TempJobUloadPhoto(DC context, string fullPath, Photo p) : base(context, fullPath, p) { }
        protected override void FlickrExecuteOffThread()
        {
            if (P.FlickrId != "")
            {
                Album album = GetAlbum();
                UpdateLocationInformation(album);
            }
        }

        protected override void FlickrExecuteOnThread()
        {
            if (IsSuccessful)
            {
                Context.Log = "AddMetaFor " + P.JustTheName + "\r\n" + Context.Log;
            }
        }
    }

    public class TempJobUploadDescription : JobUploadPhoto
    {
        public TempJobUploadDescription(DC context, string fullPath, Photo p) : base(context, fullPath, p) { }
        protected override void FlickrExecuteOffThread()
        {
            try
            {
                if (P.FlickrId != "")
                {
                    Album album = GetAlbum();
                    FlickrNet.PhotoInfo photoInfo = Flickr.PhotosGetInfo(P.FlickrId);
                    string title = P.GetFlickrTitle();
                    string descr = P.GetFlickrDescription(album.Month, album.Year);
                    descr = descr.Replace("\r", "");
                    if (title != photoInfo.Title || descr != photoInfo.Description)
                    {
                        Flickr.PhotosSetMeta(P.FlickrId, title, descr);
                    }
                    IsSuccessful = true;
                }
            }
            catch { }
        }

        protected override void FlickrExecuteOnThread()
        {
            if (IsSuccessful)
            {
                Context.Log = "AddMetaFor " + P.JustTheName + "\r\n" + Context.Log;
            }
        }
    }
    
    public class JobUploadPhoto : JobFlickrUploader
    {
        public JobUploadPhoto(DC context, string fullPath, Photo p) : base(context, fullPath)
        {
            _photo = p;
        }

        protected Album GetAlbum()
        {
            Album album = null;
            var als = from A in Context.CurrentDirectory.Albums where A.Name == _photo.AlbumT select A;
            foreach (var a in als)
            {
                album = a;
                break;
            }
            return album;
        }

        protected override void FlickrExecuteOffThread()
        {
            Album album = GetAlbum();
            if (album != null)
            {
                try
                {
                    bool isFriends = false;
                    bool isPublic = false;
                    if (!_photo.NoShow)
                    {
                        isFriends = true;
                        if (string.IsNullOrEmpty(_photo.People))
                        {
                            isPublic = true;
                        }
                    }

                    string flickrId = Flickr.UploadPicture(
                        Context.CurrentDirectory.DI.FullName + "\\" + _photo.JustTheName,
                        _photo.GetFlickrTitle(), 
                        _photo.GetFlickrDescription(album.Month, album.Year), 
                        null, 
                        isPublic, 
                        isFriends, 
                        isFriends
                        );
                    if (flickrId != "")
                    {
                        FlickrNet.PhotoInfo pi = Flickr.PhotosGetInfo(flickrId);
                        _photo.FlickrId = flickrId;
                        _photo.FlickrSecret = pi.Secret;
                        _photo.FlickrOriginalSecret = pi.OriginalSecret;
                        _photo.FlickrFarm = pi.Farm;
                        _photo.FlickrServer = pi.Server;

                        UpdateLocationInformation(album);

                        IsSuccessful = true;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is WebException)
                    {
                        WebException wex = ex as WebException;
                        if (wex.Status == WebExceptionStatus.RequestCanceled)
                        {
                        }
                    }
                }
            }
        }

        protected void UpdateLocationInformation(Album album)
        {
            Place placeforphoto = null;
            var places = from PL in Context.DSD.Places where PL.Name == _photo.Place select PL;
            foreach (var place in places)
            {
                placeforphoto = place;
                break;
            }
            if (placeforphoto != null)
            {
                try
                {
                    Flickr.PhotosGeoSetLocation(_photo.FlickrId, placeforphoto.Latitude, placeforphoto.Longitude, FlickrNet.GeoAccuracy.City);
                }
                catch
                {
                    // Consume any exceptions setting the location...
                }
            }
            try
            {
                Flickr.PhotosAddTags(_photo.FlickrId, _photo.GetTags(album, Context.DSD.IgnoreWords));
                UpdateFaceTags(_photo.FlickrId);
            }
            catch
            {
            }
        }

        private void UpdateFaceTags(string flickrId)
        {
            FlickrNet.Size[] szc = Flickr.PhotosGetSizes(flickrId).SizeCollection;

            ulong width = 0;
            ulong height = 0;

            bool fFound = false;

            foreach (var sz in szc)
            {
                if (sz.Width == 500)
                {
                    fFound = true;
                    width = (ulong)sz.Width;
                    height = (ulong)sz.Height;
                    break;
                }
            }

            if (!fFound)
            {
                foreach (var sz in szc)
                {
                    if (sz.Height == 500)
                    {
                        fFound = true;
                        width = (ulong)sz.Width;
                        height = (ulong)sz.Height;
                        break;
                    }
                }
            }

            if (fFound)
            {
                string[] people = _photo.People.Split(',');
                string[] rects = _photo.Rects.Split(',');

                if (people.Count() == rects.Count())
                {
                    for (int i = 0; i < people.Count(); i++)
                    {
                        Rect64 r = new Rect64(rects[i], width, height);
                        if (width != 0 && height != 0 && people[i] != "")
                        {
                            Flickr.NotesAdd(flickrId, (int)r.Left, (int)r.Top, (int)r.Width, (int)r.Height, people[i]);
                        }
                    }      
                }
            }
        }

        protected override void FlickrExecuteOnThread()
        {
            if (IsSuccessful)
            {
                // Flush out the data to the disk
                Context.CurrentDirectory.WriteToDisk();
            }
            else
            {
                Context.Log = "Failed to upload " + _photo.JustTheName + " in directory " + FullPath + ".\r\n" + Context.Log;
            }
        }

        protected override bool IsEqualOverride(object other)
        {
            return other.GetType() == typeof(Photo) && other == _photo;
        }

        public Photo P { get { return _photo; } }
        private Photo _photo;
        public string PhotoPath { get { return Context.CurrentDirectory.DI.FullName + "\\" + P.JustTheName; } }
    }
}
