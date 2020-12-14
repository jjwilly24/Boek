using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Google.Maps.Android;
using Java.Util;
using Newtonsoft.Json;
using ufinix.Helpers;
using yucee.Helpers;
using Polyline = Android.Gms.Maps.Model.Polyline;

namespace Ja_Tour.Helpers
{
    public class MapFunctionHelper
    {   
        string mapkey;
        GoogleMap map;
        public double distance;
        public double duration;
        public string distanceString;
        public string durationString;
        Marker pickupMarker;
        Marker driverLocationMarker;
        bool isRequestingDirection;
        public MapFunctionHelper(string mMapkey, GoogleMap mmap)
        {
            mapkey = mMapkey;
            map = mmap;
        }
        public string GetGeoCodeUrl(double lat, double lng)
        {
            string url = "https://maps.googleapis.com/maps/api/geocode/json?latlng="+ lat + "," + lng + "&key=" + mapkey;
            return url;
        }

        public async Task <string> GetGeoJsonAsync(string url)
        {
            var handler = new HttpClientHandler();
            HttpClient client = new HttpClient(handler);
            string result = await client.GetStringAsync(url);
            return result;
        }

        public async Task <string> FindCordinateAddress(LatLng position)
        {
            string url = GetGeoCodeUrl(position.Latitude, position.Longitude);
            string json = "";
            string placeAddress = "";


            //check for internet connection


            json = await GetGeoJsonAsync(url);

            if (!string.IsNullOrEmpty(json))
            {
                var geoCodeData = JsonConvert.DeserializeObject<GeocodingParser>(json);
                if (!geoCodeData.status.Contains("ZERO"))
                {
                    if(geoCodeData.results[0] != null)
                    {
                        placeAddress = geoCodeData.results[0].formatted_address;
                    }
                }
            }
            return placeAddress;
        }
        public async Task<string> GetDirectionJsonAsync(LatLng location, LatLng destination)
        {
            //Orign of route
            string str_origin = "origin=" + location.Latitude + "," + location.Longitude;

            //Destination of route 
            string str_destination = "destination=" + destination.Latitude + "," + destination.Longitude;

            //define mode
            string mode = "mode=driving";

            //Building the parameters to the webservice
            string parameters = str_origin + "&" + str_destination + "&" + "&" + mode + "&key=";

            //Output format
            string output = "json";

            string key = mapkey;

            //building the final url string
            string url = "https://maps.googleapis.com/maps/api/directions/" + output + "?" + parameters + key;

            string json = "";
            json = await GetGeoJsonAsync(url);

            return json;
        }

        public void DrawTripOnMap(string json)
        {
            var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);

            //Decode Encoded Route
            var points = directionData.routes[0].overview_polyline.points;
            var line = PolyUtil.Decode(points);

            ArrayList routeList = new ArrayList();
            foreach (LatLng item in line)
            {
                routeList.Add(item);
            }
            //Draw polylines on Map
            PolylineOptions polylineOptions = new PolylineOptions()
                .AddAll(routeList)
                .InvokeWidth(10)
                .InvokeColor(Color.Teal)
                .InvokeStartCap(new SquareCap())
                .InvokeEndCap(new SquareCap())
                .InvokeJointType(JointType.Round)
                .Geodesic(true);

            Polyline mPolyline = map.AddPolyline(polylineOptions);

            //Get first point and lastpoint
            LatLng firstpoint = line[0];
            LatLng lastpoint = line[line.Count - 1];

            //pickup marker options
            MarkerOptions pickupMarkerOptions = new MarkerOptions();
            pickupMarkerOptions.SetPosition(firstpoint);
            pickupMarkerOptions.SetTitle("Pickup Location");
            pickupMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));

            //Destination marker options
            MarkerOptions destinationMarkerOptions = new MarkerOptions();
            destinationMarkerOptions.SetPosition(lastpoint);
            destinationMarkerOptions.SetTitle("Destination");
            destinationMarkerOptions.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));

            MarkerOptions driverMarkerOptions = new MarkerOptions();
            driverMarkerOptions.SetPosition(firstpoint);
            driverMarkerOptions.SetTitle("Current Location");
           // driverMarkerOptions.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.position));
            driverMarkerOptions.Visible(false);

            pickupMarker = map.AddMarker(pickupMarkerOptions);
            Marker destinationMarker = map.AddMarker(destinationMarkerOptions);
            driverLocationMarker = map.AddMarker(driverMarkerOptions);


            //Get trip Bounds
            double southlng = directionData.routes[0].bounds.southwest.lng;
            double southlat = directionData.routes[0].bounds.southwest.lat;
            double northlng = directionData.routes[0].bounds.northeast.lng;
            double northlat = directionData.routes[0].bounds.northeast.lat;

            LatLng southwest = new LatLng(southlat, southlng);
            LatLng northeast = new LatLng(northlat, northlng);
            LatLngBounds tripBound = new LatLngBounds(southwest, northeast);

            map.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(tripBound, 0));
            map.SetPadding(40, 70, 40, 70);
            pickupMarker.ShowInfoWindow();

            duration = directionData.routes[0].legs[0].duration.value;
            distance = directionData.routes[0].legs[0].distance.value;
            durationString = directionData.routes[0].legs[0].duration.text;
            distanceString = directionData.routes[0].legs[0].distance.text;
        }
        public double EstimateFares()
        {
            double basefare = 50; //JMB
            double distanceFare = 20;//JMB per kilometer
            double timefare = 5; //JMB perminute
            double km =(distance / 1000) * distanceFare;
            double mins = (duration / 60) * timefare;

            double amount = km + mins + basefare;
            double fares = Math.Floor(amount / 10) * 10;

            return fares;
        }
        public async void UpdateDriverLocationToPickUp(LatLng firstposition, LatLng secondposition)
        {
            if (!isRequestingDirection)
            {
                isRequestingDirection = true;
                string json = await GetDirectionJsonAsync(firstposition, secondposition);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                pickupMarker.Title = "Pickup Location";
                pickupMarker.Snippet = "Your Driver is " + duration + " Away";
                pickupMarker.ShowInfoWindow();
                isRequestingDirection = false;
            }


        }

        public void UpdateDriverArrived()
        {
            pickupMarker.Title = "Pickup Location";
            pickupMarker.Snippet = "Your Driver has Arrived";
            pickupMarker.ShowInfoWindow();
        }
        public async void UpdateLocationToDestination(LatLng firstposition, LatLng secondposition)
        {
            driverLocationMarker.Visible = true;
            driverLocationMarker.Position = firstposition;
            map.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(firstposition, 15));

            if (!isRequestingDirection)
            {
                //Check Connection
                isRequestingDirection = true;
                string json = await GetDirectionJsonAsync(firstposition, secondposition);
                var directionData = JsonConvert.DeserializeObject<DirectionParser>(json);
                string duration = directionData.routes[0].legs[0].duration.text;
                driverLocationMarker.Title = "Current Location";
                driverLocationMarker.Snippet = "Your Destination is " + duration + " Away";
                driverLocationMarker.ShowInfoWindow();
                isRequestingDirection = false;
            }

        }
    }
}