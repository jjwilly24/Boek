using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Location;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Ja_Tours_Driver.Helpers;
using static Ja_Tours_Driver.Helpers.LocationCallBackHelper;

namespace Ja_Tours_Driver.Fragments
{
    public class HomeFragment : Android.Support.V4.App.Fragment, IOnMapReadyCallback
    {
        public EventHandler<OnLocationCapturedEventArgs> CurrentLocation;
       public GoogleMap mainMap;
        //Marker
        ImageView centerMarker;

        //Location client
        LocationRequest mLocationRequest;
        FusedLocationProviderClient LocationProviderClient;
        Android.Locations.Location mLastlocation;
        LocationCallBackHelper mLocationCallback = new LocationCallBackHelper();

        static int UPDATE_INTERVAL = 5; // Seconds
        static int FAST_INTERVAL = 5; // Seconds
        static int DISPACEMENT = 1;//Meters

        //layout
        LinearLayout rideInfoLayout;

        //textView
        TextView riderNameText;

        //button
        ImageButton cancelTripButton;
        ImageButton callRiderButton;
        ImageButton navigateButton;
        Button tripButton;

        //
        bool tripCreated = false;
        bool driverArrive = false;
        bool tripStarted = false;
        //events
        public event EventHandler callRider;
        public event EventHandler Navigate;
        public event EventHandler TripActionStartTrip;
        public event EventHandler TripActionArrived;
        public event EventHandler TripActionEndTrip;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
            CreateLocationRequest();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
           View view = inflater.Inflate(Resource.Layout.home, container, false);
            SupportMapFragment mapFragment = (SupportMapFragment)ChildFragmentManager.FindFragmentById(Resource.Id.map);
            centerMarker = (ImageView)view.FindViewById(Resource.Id.centerMarker);
            mapFragment.GetMapAsync(this);

            cancelTripButton = (ImageButton)view.FindViewById(Resource.Id.cancelTripButton);
            callRiderButton = (ImageButton)view.FindViewById(Resource.Id.callRiderButton);
            navigateButton = (ImageButton)view.FindViewById(Resource.Id.navigateButton);
            tripButton = (Button)view.FindViewById(Resource.Id.tripButton);
            riderNameText = (TextView)view.FindViewById(Resource.Id.riderNameText);
            rideInfoLayout = (LinearLayout)view.FindViewById(Resource.Id.rideInfoLayout);

            tripButton.Click += TripButton_Click;
            callRiderButton.Click += CallRiderButton_Click;
            navigateButton.Click += NavigateButton_Click;
            return view;
        }

         void NavigateButton_Click(object sender, EventArgs e)
        {
            Navigate.Invoke(this, new EventArgs());
        }

        void CallRiderButton_Click(object sender, EventArgs e)
        {
            callRider.Invoke(this, new EventArgs());
        }

        void TripButton_Click(object sender, EventArgs e)
        {
            if (!driverArrive && tripCreated)
            {
                driverArrive = true;
                TripActionArrived?.Invoke(this, new EventArgs());
                tripButton.Text = "Start Trip";
                return;
            }

            if(!tripStarted && driverArrive)
            {
                tripStarted = true;
                TripActionStartTrip.Invoke(this, new EventArgs());
                tripButton.Text = "End Trip";
                return;
            }

            if (tripStarted)
            {
                TripActionEndTrip.Invoke(this, new EventArgs());
                return;
            }
        }


        public void OnMapReady(GoogleMap googleMap)
        {
            mainMap = googleMap;
        }

        void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest();
            mLocationRequest.SetInterval(UPDATE_INTERVAL);
            mLocationRequest.SetFastestInterval(FAST_INTERVAL);
            mLocationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            mLocationRequest.SetSmallestDisplacement(DISPACEMENT);
            mLocationCallback.MyLocation += MLocationCallback_MyLocation; 
            LocationProviderClient = LocationServices.GetFusedLocationProviderClient(Activity);
            

        }

       void MLocationCallback_MyLocation(object sender, LocationCallBackHelper.OnLocationCapturedEventArgs e)
        {
            mLastlocation = e.Location;

            //update our last location on the map
            LatLng myposition = new LatLng(mLastlocation.Latitude, mLastlocation.Longitude);
            mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 15));

            //send location to mainActivity
            CurrentLocation?.Invoke(this, new OnLocationCapturedEventArgs { Location = e.Location });

        }

        void StartLocationUpdates()
        {
            LocationProviderClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
        }
        void StopLocationUpdates()
        {
            LocationProviderClient.RemoveLocationUpdates(mLocationCallback);
        }
        public void GoOnline()
        {
            centerMarker.Visibility = ViewStates.Visible;
            StartLocationUpdates();
        }
        public void GoOffline()
        {
            centerMarker.Visibility = ViewStates.Invisible;
            StopLocationUpdates();
        }

        public void CreateTrip(string ridername)
        {
            centerMarker.Visibility = ViewStates.Invisible;
            riderNameText.Text = ridername;
            rideInfoLayout.Visibility = ViewStates.Visible;
            tripCreated = true;  
        }

        public void ResetAfterTrip()
        {
            tripButton.Text = "Arrived Pickup";
            centerMarker.Visibility = ViewStates.Visible;
            riderNameText.Text = "";
            rideInfoLayout.Visibility = ViewStates.Invisible;
            tripCreated = false;
            driverArrive = false;
            tripStarted = false;
            mainMap.Clear();
            mainMap.TrafficEnabled = false;
            mainMap.UiSettings.ZoomControlsEnabled = false;

        }
    }
}