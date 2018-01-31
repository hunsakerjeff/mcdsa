using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation.Diagnostics;
using DSA.Data.Interfaces;
using DSA.Model.Dto;
using Salesforce.SDK.Adaptation;

namespace DSA.Data.Services
{
    public class GeolocationService : IGeolocationService
    {
        private readonly Geolocator _geolocator;

        public GeolocationService()
        {
            _geolocator = new Geolocator();
        }

        public async Task<GeolocationInfo> GetLocation()
        {
            try
            {
                Geoposition pos = await _geolocator.GetGeopositionAsync();
                var latitude = pos.Coordinate.Point.Position.Latitude.ToString();
                var longitude = pos.Coordinate.Point.Position.Longitude.ToString();
                return new GeolocationInfo { Latitude = latitude, Longitude = longitude };
            }
            catch(Exception e)
            {
                PlatformAdapter.SendToCustomLogger(e, LoggingLevel.Error);
                return new GeolocationInfo();
            }
        }
    }
}
