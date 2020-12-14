using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Firebase;
using Firebase.Database;
using Android.Views;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android;
using Android.Support.V4.App;
using Android.Content.PM;
using Android.Gms.Location;
using Ja_Tour.Helpers;
using Android.Content;
using Google.Places;
using System.Collections.Generic;
using Android.Graphics;
using Android.Support.Design.Widget;
using Ja_Tour.EventListeners;
using Ja_Tour.Fragments;
using Ja_Tour.DataModels;
using System;
using Android.Media;

namespace Ja_Tour
{
    [Activity(Label = "@string/app_name", Theme = "@style/TourTheme", MainLauncher = false)] 
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {
        //Firebase
        UserProfileEventListener profileEventListener = new UserProfileEventListener();
        CreateRequestEventListener requestListener;
        FindDriverListener findDriverListener;

        //booking
        private ListView lv;
        private CustomAdapter adapter;
        private JavaList<Spacecraft> spacecrafts;

        //views
        Android.Support.V7.Widget.Toolbar mainToolbar;
        Android.Support.V4.Widget.DrawerLayout drawerLayout;
        //textviews
        TextView pickupLocationText;
        TextView destinationText;
        TextView driverNameText;
        TextView tripStatusText;
        //Buttons
        Button favouritePlacesButton;
        Button locationSetButton;
        Button requestDriverButton;
        RadioButton pickupRadio;
        RadioButton destinationRadio;
        ImageButton callDriverButton;
        ImageButton cancelTripButton;


        //imageview
        ImageView centerMarker;
        //layouts
        RelativeLayout layoutPickUp;
        RelativeLayout layoutDestination;
       // RelativeLayout layoutBook;


        //Bottomsheets
        BottomSheetBehavior tripDetailsBottonsheetBehaviour;
        BottomSheetBehavior driverAssignedBottomSheetBehaviour;
        GoogleMap mainMap;

        readonly string[] permissionGroupLocation = { Manifest.Permission.AccessCoarseLocation, Manifest.Permission.AccessFineLocation };
        const int requestLocationId = 0;

        LocationRequest mLocationRequest;
        FusedLocationProviderClient locationClient;
        Android.Locations.Location mLastLocation;
        LocationCallbackHelper mLocationCallback;

        static int UPDATE_INTERVAL = 5; // 5 SECONDS
        static int FASTER_INTERVAL = 5;
        static int DISPLACEMENT = 3; // meters

        //Helpers
        MapFunctionHelper mapHelper;
        //tripsdetail
        LatLng pickupLocationLatlng;
        LatLng destinationLatLng;
        string pickupAddress;
        string destinationAddress;
        //flags
        int addressRequest = 1;
        //1 = set address as pickup location
        //2 = set address as destination location

        //set address from place search and ignore calling FindAdressFromCordinate method when CameraIdle Event is fired
        bool takeAddressFromSearch;

        //Fragements
        RequestDriver requestDriverFragment;

        //DataModels
        NewTripDetails newTripDetails;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            ConnectControl();

            SupportMapFragment mapFragment = (SupportMapFragment)SupportFragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            CheckLocationPermission();

            CreateLocationRequest();
            GetMyLocation();
            StartLocationUpdates();
            profileEventListener.Create();

            if (!PlacesApi.IsInitialized)
            {
                string mapkey = Resources.GetString(Resource.String.mapkey);
                PlacesApi.Initialize(this, mapkey);
            }
        }

       void ConnectControl()
        {
            //DrawerLayout
            drawerLayout = (Android.Support.V4.Widget.DrawerLayout)FindViewById(Resource.Id.drawerLayout);
            //ToolBar
            mainToolbar = (Android.Support.V7.Widget.Toolbar)FindViewById(Resource.Id.mainToolbar);
            SetSupportActionBar(mainToolbar);
            SupportActionBar.Title = "";
            Android.Support.V7.App.ActionBar actionBar = SupportActionBar;
            actionBar.SetHomeAsUpIndicator(Resource.Mipmap.ic_menu_action);
            actionBar.SetDisplayHomeAsUpEnabled(true);
            //TextView
            pickupLocationText = (TextView)FindViewById(Resource.Id.pickupLocationText);
            destinationText = (TextView)FindViewById(Resource.Id.destinationText);
            tripStatusText = (TextView)FindViewById(Resource.Id.tripStatusText);
            driverNameText = (TextView)FindViewById(Resource.Id.driverNameText);
            //Buttons
            favouritePlacesButton = (Button)FindViewById(Resource.Id.favouritePlacesButton);
            locationSetButton = (Button)FindViewById(Resource.Id.locationSetButton);
            requestDriverButton = (Button)FindViewById(Resource.Id.requestDriverButton);
            pickupRadio = (RadioButton)FindViewById(Resource.Id.pickupRadio);
            destinationRadio = (RadioButton)FindViewById(Resource.Id.DestinationRadio);

            callDriverButton = (ImageButton)FindViewById(Resource.Id.callDriverButton);
            cancelTripButton = (ImageButton)FindViewById(Resource.Id.cancelTripButton);

            favouritePlacesButton.Click += FavouritePlacesButton_Click;
            locationSetButton.Click += LocationSetButton_Click;
            requestDriverButton.Click += RequestDriverButton_Click;
            pickupRadio.Click += PickupRadio_Click;
            destinationRadio.Click += DestinationRadio_Click;
            //Layouts
            layoutPickUp = (RelativeLayout)FindViewById(Resource.Id.layoutPickUp);
            layoutDestination = (RelativeLayout)FindViewById(Resource.Id.layoutDestination);
            

            layoutPickUp.Click += LayoutPickUp_Click;
            layoutDestination.Click += LayoutDestination_Click;

            //imageview
            centerMarker = (ImageView)FindViewById(Resource.Id.centerMarker);

            //Bottomsheet
            FrameLayout tripDetailsView = (FrameLayout)FindViewById(Resource.Id.tripdetails_bottomsheet);
            FrameLayout rideInfoSheet = (FrameLayout)FindViewById(Resource.Id.bottom_sheet_trip);
            tripDetailsBottonsheetBehaviour = BottomSheetBehavior.From(tripDetailsView);
            driverAssignedBottomSheetBehaviour = BottomSheetBehavior.From(rideInfoSheet);


            lv = FindViewById<ListView>(Resource.Id.lv);

            //BIND ADAPTER
            adapter = new CustomAdapter(this, GetSpacecrafts());

            lv.Adapter = adapter;

            lv.ItemClick += lv_ItemClick;
        }


        /*
        * LISTVIEW ITEM CLICK
        */
        void lv_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            Toast.MakeText(this, spacecrafts[e.Position].Name, ToastLength.Short).Show();
        }

        /*
         * DATA SOURCE 
         */
        private JavaList<Spacecraft> GetSpacecrafts()
        {
            spacecrafts = new JavaList<Spacecraft>();

            Spacecraft s;


            s = new Spacecraft("Luxuries bed Price:30k", Resource.Drawable.enterprise);
            spacecrafts.Add(s);

            s = new Spacecraft("Couple get-away Price:12k", Resource.Drawable.hubble);
            spacecrafts.Add(s);

            s = new Spacecraft("weekend vybz Price:20k", Resource.Drawable.kepler);
            spacecrafts.Add(s);

            s = new Spacecraft("College students Price:12k", Resource.Drawable.spitzer);
            spacecrafts.Add(s);

            s = new Spacecraft("Affordable rooms Price:15k", Resource.Drawable.rosetta);
            spacecrafts.Add(s);

            s = new Spacecraft("Opportunity formula Price:20k", Resource.Drawable.voyager);
            spacecrafts.Add(s);



            return spacecrafts;

        }

        #region CLICK EVENT HANDLERS
        private void RequestDriverButton_Click(object sender, System.EventArgs e)
        {
            requestDriverFragment = new RequestDriver(mapHelper.EstimateFares());
            requestDriverFragment.Cancelable = false;
            var trans = SupportFragmentManager.BeginTransaction();
            requestDriverFragment.Show(trans, "Request");
            requestDriverFragment.CancelRequest += RequestDriverFragment_CancelRequest;

            newTripDetails = new NewTripDetails();
            newTripDetails.DestinationAddress = destinationAddress;
            newTripDetails.PickupAddress = pickupAddress;
            newTripDetails.DestinationLat = destinationLatLng.Latitude;
            newTripDetails.DestinationLng = destinationLatLng.Longitude;
            newTripDetails.DistanceString = mapHelper.distanceString;
            newTripDetails.DistanceValue = mapHelper.distance;
            newTripDetails.DurationString = mapHelper.durationString;
            newTripDetails.DurationValue = mapHelper.duration;
            newTripDetails.EstimateFare = mapHelper.EstimateFares();
            newTripDetails.Paymentmethod = "cash";
            newTripDetails.PickupLat = pickupLocationLatlng.Latitude;
            newTripDetails.PickupLng = pickupLocationLatlng.Longitude;
            newTripDetails.Timestamp = DateTime.Now;

            requestListener = new CreateRequestEventListener(newTripDetails);
            requestListener.NoDriverAcceptedRequest += RequestListener_NoDriverAcceptedRequest;
            requestListener.DriverAccepted += RequestListener_DriverAccepted;
            requestListener.TripUpdates += RequestListener_TripUpdates;
            requestListener.CreateRequest();

            findDriverListener = new FindDriverListener(pickupLocationLatlng, newTripDetails.RideID);
            findDriverListener.DriversFound += FindDriverListener_DriversFound;
            findDriverListener.DriverNotFound += FindDriverListener_DriverNotFound;
            findDriverListener.Create();
        }

        void RequestListener_TripUpdates(object sender, CreateRequestEventListener.TripUpdatesEventArgs e)
        {
            if (e.Status == "accepted")
            {
                tripStatusText.Text = "Coming";
                mapHelper.UpdateDriverLocationToPickUp(pickupLocationLatlng, e.DriverLocation);
            }
            else if (e.Status == "arrived")
            {
                tripStatusText.Text = "Driver Arrived";
                mapHelper.UpdateDriverArrived();
                MediaPlayer player = MediaPlayer.Create(this, Resource.Raw.alert);
                player.Start();
            }
            else if (e.Status == "ontrip")
            {
                tripStatusText.Text = "On Trip";
                mapHelper.UpdateLocationToDestination(e.DriverLocation, destinationLatLng);
            }
            else if (e.Status == "ended")
            {
                requestListener.EndTrip();
                requestListener = null;
                TripLocationUnset();

                driverAssignedBottomSheetBehaviour.State = BottomSheetBehavior.StateHidden;

                MakePaymentFragment makePaymentFragment = new MakePaymentFragment(e.Fares);
                makePaymentFragment.Cancelable = false;
                var trans = SupportFragmentManager.BeginTransaction();
                makePaymentFragment.Show(trans, "payment");
                makePaymentFragment.PaymentCompleted += (i, p) =>
                {
                    makePaymentFragment.Dismiss();
                };
            }

        }

        void RequestListener_DriverAccepted(object sender, CreateRequestEventListener.DriverAcceptedEventArgs e)
        {
            if (requestDriverFragment != null)
            {
                requestDriverFragment.Dismiss();
                requestDriverFragment = null;
            }

            driverNameText.Text = e.acceptedDriver.Fullname;
            tripStatusText.Text = "Coming";

            tripDetailsBottonsheetBehaviour.State = BottomSheetBehavior.StateHidden;
            driverAssignedBottomSheetBehaviour.State = BottomSheetBehavior.StateExpanded;
        }

        void RequestListener_NoDriverAcceptedRequest(object sender, EventArgs e)
        {
            RunOnUiThread(() =>
            {
                if (requestDriverFragment != null && requestListener != null)
                {
                    requestListener.CancelrequestOnTimeout();
                    requestListener = null;
                    requestDriverFragment.Dismiss();
                    requestDriverFragment = null;

                    Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                    alert.SetTitle("Message");
                    alert.SetMessage("Available drivers Couldn't Accept Your Ride Request, Try again in a few moment ");
                    alert.Show();
                }
            });
            
           
        }

        private void FindDriverListener_DriverNotFound(object sender, EventArgs e)
        {
            if(requestDriverFragment != null && requestListener != null)
            {
                requestListener.Cancelrequest();
                requestListener = null;
                requestDriverFragment.Dismiss();
                requestDriverFragment = null;

                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("Message");
                alert.SetMessage("No Available driver found, try again in a few moments");
            }
        }

        private void FindDriverListener_DriversFound(object sender, FindDriverListener.DriverFoundEventArgs e)
        {
            //notify
            if(requestListener != null)
            {
                requestListener.NotifyDriver(e.Drivers);
            }
        }

        private void RequestDriverFragment_CancelRequest(object sender, EventArgs e)
        {
            //User cancels request before driver accepts it
            if(requestDriverFragment !=null && requestListener != null)
            {
                requestListener.Cancelrequest();
                requestListener = null;
                requestDriverFragment.Dismiss();
                requestDriverFragment = null;
            }
        }

        async void LocationSetButton_Click(object sender, System.EventArgs e)
        {
            locationSetButton.Text = "Please wait...";
            locationSetButton.Enabled = false;

            string json;
            json = await mapHelper.GetDirectionJsonAsync(pickupLocationLatlng, destinationLatLng);

            if (!string.IsNullOrEmpty(json))
            {
                TextView txtFare = (TextView)FindViewById(Resource.Id.tripEstimateFareText);
                TextView txtTime = (TextView)FindViewById(Resource.Id.newTripTimeText);

                mapHelper.DrawTripOnMap(json);
                //set estimate fares and time 
                txtFare.Text = "$" + mapHelper.EstimateFares().ToString();
                txtTime.Text = mapHelper.durationString;

                //display bottomsheet
                tripDetailsBottonsheetBehaviour.State = BottomSheetBehavior.StateExpanded;

                //disableviews 
                TripDrawnOnMap();
            }
            locationSetButton.Text = "Done";
            locationSetButton.Enabled = true;
            
        }

        private void FavouritePlacesButton_Click(object sender, System.EventArgs e)
        {
            
        }

        private void PickupRadio_Click(object sender, System.EventArgs e)
        {
            addressRequest = 1;
            pickupRadio.Checked = true;
            destinationRadio.Checked = false;
            takeAddressFromSearch = false;
            centerMarker.SetColorFilter(Color.DarkGreen);
        }
        private void DestinationRadio_Click(object sender, System.EventArgs e)
        {
            addressRequest = 2;
            pickupRadio.Checked = false;
            destinationRadio.Checked = true;
            takeAddressFromSearch = false;
            centerMarker.SetColorFilter(Color.Red);
        }

    
        private void LayoutPickUp_Click(object sender, System.EventArgs e)
        {
            List<Place.Field> fields = new List<Place.Field>();

            fields.Add(Place.Field.Id);
            fields.Add(Place.Field.Name);
            fields.Add(Place.Field.LatLng);
            fields.Add(Place.Field.Address);

            Intent intent= new Autocomplete.IntentBuilder(AutocompleteActivityMode.Overlay, fields)
                .SetCountry("JM")
                .Build(this);
            StartActivityForResult(intent, 1);
        }
        private void LayoutDestination_Click(object sender, System.EventArgs e)
        {
            List<Place.Field> fields = new List<Place.Field>();

            fields.Add(Place.Field.Id);
            fields.Add(Place.Field.Name);
            fields.Add(Place.Field.LatLng);
            fields.Add(Place.Field.Address);

            Intent intent = new Autocomplete.IntentBuilder(AutocompleteActivityMode.Overlay, fields)
                .SetCountry("JM")
               .Build(this);
            StartActivityForResult(intent, 2);
        }
        #endregion


        #region MAP AND LOCATION SERVICES
        public void OnMapReady(GoogleMap googleMap)
        {
           
            mainMap = googleMap;
            mainMap.CameraIdle += MainMap_CameraIdle;
            string mapkey = Resources.GetString(Resource.String.mapkey);
            mapHelper = new MapFunctionHelper(mapkey, mainMap);
   
        }

        async void MainMap_CameraIdle(object sender, System.EventArgs e)
        {
            if (!takeAddressFromSearch)
            {
                if (addressRequest == 1)
                {
                    pickupLocationLatlng = mainMap.CameraPosition.Target;
                    pickupAddress = await mapHelper.FindCordinateAddress(pickupLocationLatlng);
                    pickupLocationText.Text = pickupAddress;
                }
                else if (addressRequest == 2)
                {
                    destinationLatLng = mainMap.CameraPosition.Target;
                    destinationAddress = await mapHelper.FindCordinateAddress(destinationLatLng);
                    destinationText.Text = destinationAddress;
                    TripleLocationsSet();
                }
            }
           
        }

        bool CheckLocationPermission()
        {
            bool permissionGranted = false;

            if(ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation)!= Android.Content.PM.Permission.Granted && 
                ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation)!= Android.Content.PM.Permission.Granted)
            {
                permissionGranted = false;
                RequestPermissions(permissionGroupLocation, requestLocationId);
            }
            else
            {
                permissionGranted = true;
            }
            return permissionGranted;
        }

        void CreateLocationRequest()
        {
            mLocationRequest = new LocationRequest();
            mLocationRequest.SetInterval(UPDATE_INTERVAL);
            mLocationRequest.SetFastestInterval(FASTER_INTERVAL);
            mLocationRequest.SetPriority(LocationRequest.PriorityHighAccuracy);
            mLocationRequest.SetSmallestDisplacement(DISPLACEMENT);
            locationClient = LocationServices.GetFusedLocationProviderClient(this);
            mLocationCallback = new LocationCallbackHelper();
            mLocationCallback.MyLocation += MLocationCallback_MyLocation;
        }
        void MLocationCallback_MyLocation(object sender, LocationCallbackHelper.OnLocationCapturedEventArgs e)
        {
            mLastLocation = e.Location;
            LatLng myposition = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);
            mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 12));
        }

        void StartLocationUpdates()
        {
            if (CheckLocationPermission())
            {
                locationClient.RequestLocationUpdates(mLocationRequest, mLocationCallback, null);
            }
        }

        void StopLocationUpdates()
        {
            if(locationClient != null && mLocationCallback != null)
            {
                locationClient.RemoveLocationUpdates(mLocationCallback);
            }
        }
       async void GetMyLocation()
        {
            if (!CheckLocationPermission())
            {
                return;
            }
            mLastLocation = await locationClient.GetLastLocationAsync();
            if(mLastLocation != null)
            {
                LatLng myposition = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);
                mainMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(myposition, 17));
            }
        }
        #endregion

        #region TRIP CONFIGURATIONS
        void TripleLocationsSet()
        {
            favouritePlacesButton.Visibility = ViewStates.Invisible;
            locationSetButton.Visibility = ViewStates.Visible;
        }
        void TripLocationUnset()
        {
            mainMap.Clear();
            layoutPickUp.Clickable = true;
            layoutDestination.Clickable = true;
            pickupRadio.Enabled = true;
            destinationRadio.Enabled = true;
            takeAddressFromSearch = false;
            centerMarker.Visibility = ViewStates.Visible;
            favouritePlacesButton.Visibility = ViewStates.Visible;
            locationSetButton.Visibility = ViewStates.Invisible;
            tripDetailsBottonsheetBehaviour.State = BottomSheetBehavior.StateHidden;
            GetMyLocation();
        }
        void TripDrawnOnMap()
        {
            layoutDestination.Clickable = false;
            layoutPickUp.Clickable = false;
            pickupRadio.Enabled = false;
            destinationRadio.Enabled = false;
            takeAddressFromSearch = true;
            centerMarker.Visibility = ViewStates.Invisible;

        }
        #endregion

        #region OVERRIDE METHODS
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(grantResults.Length < 1)
            {
                return;
            }
            if(grantResults[0] == (int) Android.Content.PM.Permission.Granted) 
            {
                StartLocationUpdates();
            }
            else
            {
                
            }
        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if(requestCode == 1)
            {
                if(resultCode == Android.App.Result.Ok)
                {
                    takeAddressFromSearch = true;
                    pickupRadio.Checked = false;
                    destinationRadio.Checked = false;

                    var place = Autocomplete.GetPlaceFromIntent(data);
                    pickupLocationText.Text = place.Address;
                    pickupAddress = place.Address;
                    pickupLocationLatlng = place.LatLng;
                    mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(place.LatLng, 15));
                    centerMarker.SetColorFilter(Color.DarkGreen);
                }
            }
            if (requestCode == 2)
            {
                if (resultCode == Android.App.Result.Ok)
                {
                    takeAddressFromSearch = true;
                    pickupRadio.Checked = false;
                    destinationRadio.Checked = false;

                    var place =Autocomplete.GetPlaceFromIntent(data);
                    destinationText.Text = place.Address;
                    destinationAddress = place.Address;
                    destinationLatLng = place.LatLng;
                    mainMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(place.LatLng, 15));
                    centerMarker.SetColorFilter(Color.Red);
                    TripleLocationsSet();
                }
            }
        }
        #endregion

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    drawerLayout.OpenDrawer((int)GravityFlags.Left);
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }

        }


    }
} 