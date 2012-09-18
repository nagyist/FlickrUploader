using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.ObjectModel;
using ALCRWNS;
using System.ComponentModel;

namespace FlickrUploader
{
    public class DirectoryToUpload 
    {
        private class Writer : ALCRW
        {
            public Writer(DirectoryToUpload context) : base(context.DI) { _context = context;  }
            protected override bool AddOnePhotoOverride(string DateStr, string JustTheName, string Title, string People, string AlbumT, string Place, bool NoShow, bool Favorite, string FlickrId, string FlickrSecret, string FlickrOriginalSecret, string FlickrFarm, string FlickrServer, string Rectangles) { return false; }
            protected override void AddOneAlbumOverride(string Name, string Month, string Year, string Photo, string Story) { }

            protected override bool GetOneAlbumOverride(int i, out string Name, out string Month, out string Year, out string Photo, out string Story)
            {
                if (i < _context.Albums.Count)
                {
                    Name = _context.Albums[i].Name;
                    Month = _context.Albums[i].Month;
                    Year = _context.Albums[i].Year;
                    Photo = _context.Albums[i].Photo;
                    Story = _context.Albums[i].Story;
                    return true;
                }
                else
                {
                    Name = Month = Year = Photo = Story = null;
                    return false;
                }
            }

            protected override bool GetOnePhotoOverride(int i, out string DateStr, out string JustTheName, out string Title, out string People, out string AlbumT, out string Place, out bool NoShow, out bool Favorite, out string FlickrId, out string FlickrSecret, out string FlickrOriginalSecret, out string FlickrFarm, out string FlickrServer, out string Rectangles)
            {
                if (i < _context.Photos.Count)
                {
                    Photo p = _context.Photos[i];
                    DateStr = p.DateStr;
                    JustTheName = p.JustTheName;
                    Title = p.Title;
                    People = p.People;
                    AlbumT = p.AlbumT;
                    Place = p.Place;
                    NoShow = p.NoShow;
                    Favorite = p.Favorite;
                    FlickrId = p.FlickrId;
                    FlickrSecret = p.FlickrSecret;
                    FlickrOriginalSecret = p.FlickrOriginalSecret;
                    FlickrFarm = p.FlickrFarm;
                    FlickrServer = p.FlickrServer;
                    Rectangles = p.Rects;
                    return true;
                }
                else
                {
                    DateStr = JustTheName = Title = People = AlbumT = Place = null;
                    NoShow = Favorite = false;
                    FlickrId = FlickrSecret = FlickrOriginalSecret = FlickrFarm = FlickrServer = Rectangles = null;
                    return false;
                }
            }

            private DirectoryToUpload _context;
        }

        public DirectoryToUpload(DirectoryInfo di) 
        { 
            DI = di;
            Albums = new List<Album>();
            Photos = new List<Photo>();
        }

        public void WriteToDisk()
        {
            Writer writer = new Writer(this);
            writer.Write();
        }

        public DirectoryInfo DI { get; private set; }
        public uint FileCount { get; set; }

        public List<Album> Albums { get; set; }
        public List<Photo> Photos { get; set; }

        public int Year;
        public int Month;
    }

    public class DirectoriesToUpload : ObservableCollection<DirectoryToUpload> 
    {
        protected override void ClearItems()
        {
            TotalCount = 0;
            base.ClearItems();
        }

        protected override void InsertItem(int index, DirectoryToUpload item)
        {
            TotalCount += item.FileCount;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            TotalCount -= this[index].FileCount;
            base.RemoveItem(index);
        }

        public uint TotalCount
        {
            get { return _totalCount; }
            set
            {
                _totalCount = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TotalCount"));
            }
        }
        private uint _totalCount;
    }

    public delegate void OneDirectoryAvailableDelegate(DirectoryToUpload dtu);

    public class DirectoryReader
    {
        private class RW : ALCRW
        {
            public RW(DirectoryToUpload dtu) : base(dtu.DI) { Context = dtu; }

            protected override bool AddOnePhotoOverride(string DateStr, string JustTheName, string Title, string People, string AlbumT, string Place, bool NoShow, bool Favorite, string FlickrId, string FlickrSecret, string FlickrOriginalSecret, string FlickrFarm, string FlickrServer, string Rectangles)
            {
                if (FlickrId == "" && NoShow == false)
                //if (NoShow == false)
                {
                    Context.FileCount++;
                }
                Photo p = new Photo();
                p.DateStr = DateStr;
                p.JustTheName = JustTheName;
                p.Title = Title;
                p.People = People;
                p.AlbumT = AlbumT;
                p.Place = Place;
                p.NoShow = NoShow;
                p.Favorite = Favorite;
                p.FlickrId = FlickrId;
                p.FlickrSecret = FlickrSecret;
                p.FlickrOriginalSecret = FlickrOriginalSecret;
                p.FlickrFarm = FlickrFarm;
                p.FlickrServer = FlickrServer;
                p.Rects = Rectangles;
                Context.Photos.Add(p);

                return false;
            }

            protected override void AddOneAlbumOverride(string Name, string Month, string Year, string Photo, string Story)
            {
                Album a = new Album();
                a.Name = Name;
                a.Month = Month;
                a.Year = Year;
                a.Photo = Photo;
                a.Story = Story;
                a.PhotosetId = null; // We will never use this in this context.
                Context.Albums.Add(a);

                int m = 0;
                if (int.TryParse(Month, out m))
                {
                    Context.Month = m;
                }
                int y = 0;
                if (int.TryParse(Year, out y))
                {
                    Context.Year = y;
                }
            }

            protected override bool GetOneAlbumOverride(int i, out string Name, out string Month, out string Year, out string Photo, out string Story)
            {
                throw new NotImplementedException();
            }

            protected override bool GetOnePhotoOverride(int i, out string DateStr, out string JustTheName, out string Title, out string People, out string AlbumT, out string Place, out bool NoShow, out bool Favorite, out string FlickrId, out string FlickrSecret, out string FlickrOriginalSecret, out string FlickrFarm, out string FlickrServer, out string Rectangles)
            {
                throw new NotImplementedException();
            }

            private DirectoryToUpload Context;
        }

        public static void DirectoryReaderThread()
        {
            RecursiveReader(((App)App.Current).StartPath);
            MainWindow.Main.Dispatcher.BeginInvoke(new OneDirectoryAvailableDelegate(MainWindow.Main.OneDirectoryAvailable), 
                new DirectoryToUpload(null));
        }

        private static void RecursiveReader(DirectoryInfo diIn)
        {
            DirectoryInfo[] dil = diIn.GetDirectories();
            foreach (var di in dil)
            {
                if (di.Name != "small" && di.Name != "Originals")
                {
                    DirectoryToUpload dtu = ReadDirectoryInfo(di);
                    if (dtu != null)
                    {
                        // For the time being, ignore all directories where FileCount == 0
                        // if (dtu.FileCount != 0)
                        {
                            MainWindow.Main.Dispatcher.BeginInvoke(new OneDirectoryAvailableDelegate(MainWindow.Main.OneDirectoryAvailable), dtu);
                        }
                    }
                    RecursiveReader(di);
                }
            }
        }

        private static DirectoryToUpload ReadDirectoryInfo(DirectoryInfo di)
        {
            bool fIgnore = di.GetFiles("noflickrupload").Length == 1;
            if (!fIgnore)
            {
                DirectoryToUpload dtu = new DirectoryToUpload(di);
                RW rw = new RW(dtu);
                rw.Read(false);
                return ((dtu.Albums.Count == 0) && (dtu.Photos.Count == 0)) ? null : dtu;
            }
            else
            {
                return null;
            }
        }
    }
}
