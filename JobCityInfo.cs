using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace FlickrUploader
{
    class JobCityInfo : Job
    {
        static JobCityInfo() { Priority = 0; }
        public JobCityInfo(DC context, string fullPath, string city) : base(context, fullPath) { _city = city; }
        private double _latitude;
        private double _longitude;
        public  string _city;
        public string City { get { return _city; } }

        protected override void ExecuteOffThread() { GetLatLongData(_city); }
        protected override void ExecuteOnThread()
        {
            if (IsSuccessful)
            {
                Context.DSD.AddACity(_city, _latitude, _longitude);
            }
            else
            {
                Context.Log = "Error for city " + _city + " in directory " + FullPath + "\r\n" + Context.Log;
            }
        }

        protected override bool IsEqualOverride(object other)
        {
            if (other.GetType() == typeof(string))
            {
                return (_city == (string)other);
            }
            return false;
        }

        private void GetLatLongData(string city)
        {
            WebClient wc = new WebClient();
            Stream data = wc.OpenRead(@"http://maps.googleapis.com/maps/api/geocode/xml?address=" + city + @"&sensor=false");
            StreamReader reader = new StreamReader(data);

            try
            {
                XmlReader xreader = XmlReader.Create(reader);
                xreader.ReadToDescendant("lat");
                _latitude = double.Parse(xreader.ReadString());
                xreader.ReadToNextSibling("lng");
                _longitude = double.Parse(xreader.ReadString());
                IsSuccessful = true;
            }
            catch
            {
                _latitude = 0;
                _longitude = 0;
            }
        }

        private void OldGetLatLongData(string city)
        {
            try
            {
                string token = GetToken();

                GeoCode.GeocodeRequest greq = new GeoCode.GeocodeRequest();
                greq.Credentials = new GeoCode.Credentials();
                greq.Credentials.Token = token;
                greq.Query = city;

                GeoCode.ConfidenceFilter[] filters = new GeoCode.ConfidenceFilter[1];
                filters[0] = new GeoCode.ConfidenceFilter();
                filters[0].MinimumConfidence = GeoCode.Confidence.Medium;

                GeoCode.GeocodeOptions gopts = new GeoCode.GeocodeOptions();
                gopts.Filters = filters;

                greq.Options = gopts;

                // Make the geocode request
                GeoCode.GeocodeServiceClient geocodeService = new GeoCode.GeocodeServiceClient();
                GeoCode.GeocodeResponse geocodeResponse = geocodeService.Geocode(greq);

                if (geocodeResponse.Results.Count() > 0
                    && geocodeResponse.Results[0].Locations.Count() > 0
                    )
                {
                    _latitude = geocodeResponse.Results[0].Locations[0].Latitude;
                    _longitude = geocodeResponse.Results[0].Locations[0].Longitude;
                    IsSuccessful = true;
                }
            }
            catch
            {
                _latitude = 0;
                _longitude = 0;
            }
        }

        private string GetToken()
        {
            TokenService.CommonService commonService = new TokenService.CommonService();
            commonService.Credentials = new NetworkCredential("138906", "my old password");

            // Set the token specification properties
            TokenService.TokenSpecification tokenSpec = new TokenService.TokenSpecification();
            tokenSpec.ClientIPAddress = "71.112.17.158";
            tokenSpec.TokenValidityDurationMinutes = 60;

            string token = "";

            // Get a token
            try
            {
                token = commonService.GetClientToken(tokenSpec);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return token;
        }
    }
}
