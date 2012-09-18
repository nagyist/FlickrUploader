using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using ALCRWNS;

namespace FlickrUploader
{
    public class Album
    {
        public string Name { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string Photo { get; set; }
        public string Story { get; set; }

        public string PhotosetId { get; set; }

        public bool AreEqual(Album other)
        {
            return (Name == other.Name && Month == other.Month && Year == other.Year);
        }
    }

    public class Photo
    {
        public static int FromMonth(string p)
        {
            switch (p)
            {
                case "January": return 1;
                case "February": return 2;
                case "March": return 3;
                case "April": return 4;
                case "May": return 5;
                case "June": return 6;
                case "July": return 7;
                case "August": return 8;
                case "September": return 9;
                case "October": return 10;
                case "November": return 11;
                case "December": return 12;
            }
            return 0;
        }

        public static string GetMonth(string m)
        {
            int month;
            if (int.TryParse(m, out month))
            {
                return GetMonth(month, m);
            }
            else
            {
                return m;
            }
        }

        public static string GetMonth(int month, string defaults)
        {
            switch (month)
            {
                case 1: return "January";
                case 2: return "February";
                case 3: return "March";
                case 4: return "April";
                case 5: return "May";
                case 6: return "June";
                case 7: return "July";
                case 8: return "August";
                case 9: return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
                default: return defaults;
            }
        }
        
        public string DateStr { get; set; }
        public string JustTheName { get; set; }
        public string Title { get; set; }
        public string People { get; set; }
        public string AlbumT { get; set; }
        public string Place { get; set; }
        public bool   NoShow { get; set; }
        public bool   Favorite { get; set; }
        public string FlickrId { get; set; }
        public string FlickrSecret { get; set; }
        public string FlickrOriginalSecret { get; set; }
        public string FlickrFarm { get; set; }
        public string FlickrServer { get; set; }
        public string Rects { get; set; }

        public string GetFlickrTitle() { return (Title == null || Title == "") ? AlbumT : Title; }
        public string GetFlickrDescription(string month, string year)
        {
            string CR = "\r\n";
            string S = AlbumT + " (" + GetMonth(month) + ", " + year + ")" + CR +
                "Title:" + Title + CR +
                "People:" + People + CR +
                "Place:" + Place + CR +
                "Date:" + DateStr + CR +
                "File:" + JustTheName + CR;
            return S;
        }

        public string GetTags(Album A, List<string> ignorethese)
        {
            List<string> TagList = new List<string>();

            // Month, Date, Year
            if (DateStr != "")
            {
                try
                {
                    DateTime dt = DateTime.Parse(DateStr);
                    TagList.Add(Photo.GetMonth(dt.Month, ""));
                    TagList.Add(dt.Year.ToString());
                }
                catch
                {
                    TagList.Add(Photo.GetMonth(A.Month));
                    TagList.Add(A.Year);
                }
            }
            else
            {
                TagList.Add(Photo.GetMonth(A.Month));
                TagList.Add(A.Year);
            }

            // Title
            AddWordsFromStringToList(TagList, ignorethese, Title);

            // People
            AddNamesFromStringToList(TagList, People);

            // Album Title
            AddWordsFromStringToList(TagList, ignorethese, AlbumT);

            // Place
            AddWordsFromStringToList(TagList, ignorethese, Place);

            // Album story
            AddWordsFromStringToList(TagList, ignorethese, A.Story);

            return ListToString(TagList);
        }

        private void AddNamesFromStringToList(List<string> los, string s)
        {
            if (s != null && s != "")
            {
                var sa = s.Split(',');
                foreach (var si in sa)
                {
                    los.Add(si);
                }
            }
        }

        private void AddWordsFromStringToList(List<string> los, List<string> ignorethese, string s)
        {
            if (s != null && s != "")
            {
                var sa = s.Split(' ', ':', ';', ',', '\'');
                if (sa.Count() != 0)
                {
                    foreach (var si in sa)
                    {
                        var siv = si.Trim();
                        siv = siv.Trim('!', '.', '?', '-', '"', '(', ')', '~', '*');
                        var sivlower = siv.ToLower();
                        if (siv.Length > 3)
                        {
                            if (!ignorethese.Contains(sivlower)
                                && !los.Contains(siv)
                               )
                            {
                                los.Add(siv);
                            }
                        }
                    }
                }
            }
        }

        private string ListToString(List<String> los)
        {
            string s = "";
            bool first = true;
            foreach (var rvi in los)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    s += ",";
                }
                s += rvi;
            }
            return s;
        }
    }

    public class Place
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public double Latitude { get; set; }

        [XmlAttribute]
        public double Longitude { get; set; }
    }

    public class DataSavedToDisk
    {
        [XmlIgnore]
        public List<Album> Albums;

        [XmlArray]
        public List<Place> Places;

        [XmlArray]
        public List<String> TagWords;

        [XmlArray]
        public List<String> IgnoreWords;

        [XmlIgnore]
        public DateTime LastUpdatedTime;

        private DataSavedToDisk() { }

        private static string FileName
        {
            get
            {
                DirectoryInfo di = ((App)App.Current).StartPath;
                string fname = di.FullName + "FlickUploadInformation.XML";
                return fname;
            }
        }

        public static DataSavedToDisk Read()
        {
            DataSavedToDisk dsd;
            DateTime lut;

            if (File.Exists(FileName))
            {
                lut = File.GetLastWriteTime(FileName);
                using (FileStream fs = new FileStream(FileName, FileMode.Open))
                {
                    XmlSerializer ds = new XmlSerializer(typeof(DataSavedToDisk));
                    dsd = (DataSavedToDisk)ds.Deserialize(fs);
                }
            }
            else
            {
                lut = new DateTime(1800, 1, 1);
                dsd = new DataSavedToDisk();
            }

            if (dsd.Albums == null)
            {
                dsd.Albums = new List<Album>();
            }
            if (dsd.Places == null)
            {
                dsd.Places = new List<Place>();
            }
            //if (dsd.TagWords == null)
            {
                dsd.TagWords = new List<string>();
            }
            if (dsd.IgnoreWords == null)
            {
                dsd.IgnoreWords = new List<string>();
            }

            dsd.LastUpdatedTime = lut;
            return dsd;
        }

        public void Write()
        {
            using (FileStream fs = new FileStream(FileName, FileMode.Create))
            {
                XmlSerializer se = new XmlSerializer(typeof(DataSavedToDisk));
                se.Serialize(fs, this);
            }
        }

        public void AddACity(string city, double latitude, double longitude)
        {
            Place p = new Place();
            p.Name = city;
            p.Latitude = latitude;
            p.Longitude = longitude;
            Places.Add(p);
            Write();
        }

        public void AddTags(string tags)
        {
            if (tags != null && tags != "")
            {
                var ss = tags.Split(',');
                foreach (var s in ss)
                {
                    if (!TagWords.Contains(s))
                    {
                        TagWords.Add(s);
                    }
                }
                Write();
            }
        }
    }
}
