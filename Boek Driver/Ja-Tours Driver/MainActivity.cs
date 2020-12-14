using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using Android.Support.V4.View;
using Com.Ittianyu.Bottomnavigationviewex;
using System;
using Ja_Tours_Driver.Adapter;
using Ja_Tours_Driver.Fragments;
using Android.Graphics;
using Android;
using Android.Support.V4.App;
using Ja_Tours_Driver.EventListeners;
using Android.Gms.Maps.Model;
using Ja_Tours_Driver.Helpers;
using Android.Support.V4.Content;
using Ja_Tours_Driver.DataModels;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Android.Media;
using Android.Content;

namespace Ja_Tours_Driver
{
    [Activity(Label = "@string/app_name", Theme = "@style/TourTheme", MainLauncher = false)]
    public class MainActivity : AppCompatActivity
    {
        //Button
        Button goOnlineButton;
        //views
        ViewPager viewpager;
        BottomNavigationViewEx bnve;

        //Fragments 
        HomeFragment homeFragment = new HomeFragment();
        RatingsFragment ratingsFragment = new RatingsFragment();
        EarningsFragment earningsFragment = new EarningsFragment();
        AccountFragment accountFragment = new AccountFragment();
        NewRequestFragment requestFoundDialogue;
      

        //PermissionRequest
        const int RequestID = 0;
        readonly string[] permissionGroup =
        {
            Manifest.Permission.AccessCoarseLocation,
            Manifest.Permission.AccessFineLocation,
        };

        //EventListeners
        ProfileEventListener profileEventListener = new ProfileEventListener();
        AvailabilityListener AvailabilityListener;
        RideDetailsListener rideDetailsListener;
        NewTripEventListener newTripEventListener;

        //Map Stuffs
        Android.Locations.Location mLastLocation;
        LatLng mLastLatLng;

        //flags
        bool availabilityStatus;
        bool isBackground;
        bool newRideAssigned;
        string status = "NORMAL";//REQUESTED, ACCEPTED, ONTRIP
        //Datamodels
        RideDetails newRideDetails;

        //MediaPlayer
        MediaPlayer player;

        //Helpers
        MapFunctionHelper mapHelper;

        Android.Support.V7.App.AlertDialog.Builder alert;
        Android.Support.V7.App.AlertDialog alertDialog;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            Connectviews();
            CheckSpecialPermision();
            profileEventListener.Create();
        }
        void ShowProgressDialogue()
        {
            alert = new Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetView(Resource.Layout.progress);
            alert.SetCancelable(false);
            alertDialog = alert.Show();
        }

        void CloseProgressDialogue()
        {
            if (alert != null)
            {
                alertDialog.Dismiss();
                alertDialog = null;
                alert = null;
            }
        }
        void Connectviews()
        {
            goOnlineButton = (Button)FindViewById(Resource.Id.goOnlineButton);
            bnve = (BottomNavigationViewEx)FindViewById(Resource.Id.bnve);
#pragma warning disable CS0618 // Type or member is obsolete
            bnve.EnableItemShiftingMode(false);
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            bnve.EnableShiftingMode(false);
#pragma warning restore CS0618 // Type or member is obsolete

            goOnlineButton.Click += GoOnlineButton_Click;
            bnve.NavigationItemSelected += Bnve_NavigationItemSelected;


            var img0 = bnve.GetIconAt(0);
            var txt0 = bnve.GetLargeLabelAt(0);
            img0.SetColorFilter(Color.Rgb(24, 191, 242));
            txt0.SetTextColor(Color.Rgb(24, 191, 242));

            viewpager = (ViewPager)FindViewById(Resource.Id.viewpager);
            viewpager.OffscreenPageLimit = 3;
            viewpager.BeginFakeDrag();

            SetupViewPager();

            homeFragment.CurrentLocation += HomeFragment_CurrentLocation;
            homeFragment.TripActionArrived += HomeFragment_TripActionArrived;
            homeFragment.callRider += HomeFragment_callRider;
            homeFragment.Navigate += HomeFragment_Navigate;
            homeFragment.TripActionStartTrip += HomeFragment_TripActionStartTrip;
            homeFragment.TripActionEndTrip += HomeFragment_TripActionEndTripAsync;
        }

        async void HomeFragment_TripActionEndTripAsync(object sender, EventArgs e)
        {
            status = "NORMAL";
            homeFragment.ResetAfterTrip();
            ShowProgressDialogue();
            LatLng pickupLatLng = new LatLng(newRideDetails.PickupLat, newRideDetails.PickupLng);
            double fares = await mapHelper.CalculateFares(pickupLatLng, mLastLatLng);
            CloseProgressDialogue();

            newTripEventListener.EndTrip(fares);
            newTripEventListener = null;
            

            CollectPaymentFragment collectPaymentFragment = new CollectPaymentFragment(fares);
            collectPaymentFragment.Cancelable = false;
            var trans = SupportFragmentManager.BeginTransaction();
            collectPaymentFragment.Show(trans, "pay");
            collectPaymentFragment.PaymentCollected += (o, u) => {
                collectPaymentFragment.Dismiss();
            };
            AvailabilityListener.ReActivate();
        }

        void HomeFragment_TripActionStartTrip(object sender, EventArgs e)
        {
            Android.Support.V7.App.AlertDialog.Builder startTripAlert = new Android.Support.V7.App.AlertDialog.Builder(this);
            startTripAlert.SetTitle("START TRIP");
            startTripAlert.SetMessage("Are you sure");
            startTripAlert.SetPositiveButton("Continue", (senderAlert, args) =>
            {
                status = "ONTRIP";

                // Update Rider that Driver has started the trip
                newTripEventListener.UpdateStatus("ontrip");
            });

            startTripAlert.SetNegativeButton("Cancel", (senderAlert, args) =>
            {
                startTripAlert.Dispose();
            });

            startTripAlert.Show();
        }

        void HomeFragment_Navigate(object sender, EventArgs e)
        {
            string uriString = "";

            if (status == "ACCEPTED")
            {
                uriString = "google.navigation:q=" + newRideDetails.PickupLat.ToString() + "," + newRideDetails.PickupLng.ToString();
            }
            else
            {
                uriString = "google.navigation:q=" + newRideDetails.DestinationLat.ToString() + "," + newRideDetails.DestinationLng.ToString();
            }

            Android.Net.Uri googleMapIntentUri = Android.Net.Uri.Parse(uriString);
            Intent mapIntent = new Intent(Intent.ActionView, googleMapIntentUri);
            mapIntent.SetPackage("com.google.android.apps.maps");

            try
            {
                StartActivity(mapIntent);
            }
            catch
            {
                Toast.MakeText(this, "Google Map is not Installed on this device", ToastLength.Short).Show();
            }
        }

        void HomeFragment_callRider(object sender, EventArgs e)
        {
            var uri = Android.Net.Uri.Parse("tel:" + newRideDetails.RiderPhone);
            Intent intent = new Intent(Intent.ActionDial, uri);
            StartActivity(intent);
        }

        async void HomeFragment_TripActionArrived(object sender, EventArgs e)
        {
            //notify rider that driver has arrived
            newTripEventListener.UpdateStatus("arrive");
            status = "ARRIVED";

            LatLng pickupLatLng = new LatLng(newRideDetails.PickupLat, newRideDetails.PickupLng);
            LatLng destinationLatLng = new LatLng(newRideDetails.DestinationLat, newRideDetails.DestinationLng);

            ShowProgressDialogue();
            string directionJson = await mapHelper.GetDirectionJsonAsync(pickupLatLng, destinationLatLng);
            CloseProgressDialogue();

            //clear map
            homeFragment.mainMap.Clear();
            mapHelper.DrawTripToDestination(directionJson);
        }

        void HomeFragment_CurrentLocation(object sender, LocationCallBackHelper.OnLocationCapturedEventArgs e)
        {
            mLastLocation = e.Location;
            mLastLatLng = new LatLng(mLastLocation.Latitude, mLastLocation.Longitude);

            if (AvailabilityListener != null)
            {
                AvailabilityListener.UpDateLocation(mLastLocation);
            }

            if (availabilityStatus && AvailabilityListener == null)
            {
                TakeDriverOnline();
            }
            if (status == "ACCEPTED")
            {
                //Update and Animate driver movement to pick up location
                LatLng pickupLatLng = new LatLng(newRideDetails.PickupLat,newRideDetails.PickupLng);
                mapHelper.UpdateMovement(mLastLatLng, pickupLatLng, "Rider");

                //update location in riderrequest table, so that rider can recieve updates
                newTripEventListener.UpdateLocation(mLastLocation);
            }
            else if(status == "ARRIVED")
            {
                newTripEventListener.UpdateLocation(mLastLocation);
            }
            else if(status == "ONTRIP")
            {//Update and animate driver movement to destination
                LatLng destinationLatLng = new LatLng(newRideDetails.DestinationLat, newRideDetails.DestinationLng);
                mapHelper.UpdateMovement(mLastLatLng, destinationLatLng, "Destination");

                newTripEventListener.UpdateLocation(mLastLocation);
            }
        }

        private void TakeDriverOnline()
        {
            AvailabilityListener = new AvailabilityListener();
            AvailabilityListener.Create(mLastLocation);
            AvailabilityListener.RideAssigned += AvailabilityListener_RideAssigned1;
            AvailabilityListener.RideTimeout += AvailabilityListener_RideTimeout;
            AvailabilityListener.RideCancelled += AvailabilityListener_RideCancelled;
        }

        void AvailabilityListener_RideAssigned1(object sender, AvailabilityListener.RideAssignedIDEventArgs e)
        {
            //Ride Assigned
            //Toast.MakeText(this, "New trip assigned =" + e.RideId, ToastLength.Short).Show();

            //Get Details
            rideDetailsListener = new RideDetailsListener();
            rideDetailsListener.Create(e.RideId);
            rideDetailsListener.RideDetailsFound += RideDetailsListener_RideDetailsFound;
            rideDetailsListener.RideDetailsNotFound += RideDetailsListener_RideDetailsNotFound;
        }

        void RideDetailsListener_RideDetailsNotFound(object sender, EventArgs e)
        {

        }

        void CreateNewRequestDialogue()
        {
            requestFoundDialogue = new NewRequestFragment(newRideDetails.PickupAddress, newRideDetails.DestinationAddress);
            requestFoundDialogue.Cancelable = false;

             var trans = SupportFragmentManager.BeginTransaction();
            requestFoundDialogue.Show(trans, "Request");

            //debug fix
            //trans = SupportFragmentManager.BeginTransaction();
            //trans.Add(requestFoundDialogue, "Request");
            //trans.CommitAllowingStateLoss();

            //Play Alert
            player = MediaPlayer.Create(this, Resource.Raw.alert);
            player.Start();

            Dismiss();
            requestFoundDialogue.RideRejected += RequestFoundDialogue_RideRejected;
            requestFoundDialogue.RideAccepted += RequestFoundDialogue_RideAccepted;
        }

        async void RequestFoundDialogue_RideAccepted(object sender, EventArgs e)
        {
            newTripEventListener = new NewTripEventListener(newRideDetails.RideId, mLastLocation);
            newTripEventListener.Create();

            status = "ACCEPTED";

            //stop Alert
            if(player != null)
            {
                player.Stop();
                player = null;
            }

            //Dissmiss Dialogue
            if(requestFoundDialogue != null)
            {
                requestFoundDialogue.Dismiss();
                requestFoundDialogue = null;
            }

            homeFragment.CreateTrip(newRideDetails.RiderName);
            mapHelper = new MapFunctionHelper(Resources.GetString(Resource.String.mapkey),homeFragment.mainMap);
            LatLng pickupLatLng = new LatLng(newRideDetails.PickupLat, newRideDetails.PickupLng);
            ShowProgressDialogue();
            string directionJson = await mapHelper.GetDirectionJsonAsync(mLastLatLng, pickupLatLng);
            CloseProgressDialogue();
            mapHelper.DrawTripOnMap(directionJson);
        }

        private void RequestFoundDialogue_RideRejected(object sender, EventArgs e)
        {
            //stop Alert
            if (player != null)
            {
                player.Stop();
                player = null;
            }

            //Dissmiss Dialogue
            if (requestFoundDialogue != null)
            {
                requestFoundDialogue.Dismiss();
                requestFoundDialogue = null;
            }

            AvailabilityListener.ReActivate();

            //do other stuff
        }

        void RideDetailsListener_RideDetailsFound(object sender, RideDetailsListener.RideDetailsEventArgs e)
        {
            if (status != "NORMAL")
            {
                return;
            }
            newRideDetails = e.RideDetails;

            if (!isBackground)
            {
                CreateNewRequestDialogue();
            }
            else
            {
                newRideAssigned = true;
                NotificationHelper notificationHelper = new NotificationHelper();
                if ((int)Build.VERSION.SdkInt >= 26)
                {
                    notificationHelper.NotifyVersion26(this, Resources, (NotificationManager)GetSystemService(NotificationService));
                }
                /*else if((int)Build.VERSION.SdkInt < 26)
                {
                    notificationHelper.NotifyOtherVersions(this, Resources, (NotificationManager)GetSystemService(NotificationService));
                }*/
                
            }
        }

        public bool Dismiss()
        {
            if (requestFoundDialogue != null)
            {
          
                return true;
            }
            else
            {
                return false;
            }
            
        }

        void AvailabilityListener_RideTimeout(object sender, EventArgs e)
        {
            if (Dismiss() == true)
            {
                requestFoundDialogue.Dismiss();
                requestFoundDialogue = null;
                player.Stop();
                player = null;
            }
            

            Toast.MakeText(this, "New trip timeout", ToastLength.Short).Show();
            AvailabilityListener.ReActivate();
        }
        void AvailabilityListener_RideCancelled(object sender, EventArgs e)
        {
            if (requestFoundDialogue != null)
            {
                requestFoundDialogue.Dismiss();
                requestFoundDialogue = null;
                player.Stop();
                player = null;
            }
            Toast.MakeText(this, "Trip was cancelled ", ToastLength.Short).Show();
            AvailabilityListener.ReActivate();
        }


        void TakeDriverOffline()
        {
            AvailabilityListener.RemoveListener();
            AvailabilityListener = null;
        }

        private void GoOnlineButton_Click(object sender, EventArgs e)
        {
            if (!CheckSpecialPermision())
            {
                return;
            }
            if (availabilityStatus)
            {
                Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                alert.SetTitle("GO OFFLINE");
                alert.SetMessage("You will not able to receive ride Request");
                alert.SetPositiveButton("Continue", (senderAlert, args) =>
                {
                    homeFragment.GoOffline();
                    goOnlineButton.Text = "Go Online";
                    goOnlineButton.Background = ContextCompat.GetDrawable(this, Resource.Drawable.tourroundButton_online);
                    availabilityStatus = false;
                    TakeDriverOffline();
                });

                alert.SetNegativeButton("Cancel", (senderAlert, args) =>
                {
                    alert.Dispose();
                });
                alert.Show();
            }
            else
            {
                availabilityStatus = true;
                homeFragment.GoOnline();
                goOnlineButton.Text = "Go offline";
                goOnlineButton.Background = ContextCompat.GetDrawable(this, Resource.Drawable.tourroundButton_green);
            }
        }

        private void Bnve_NavigationItemSelected(object sender, Android.Support.Design.Widget.BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            if (e.Item.ItemId == Resource.Id.action_earning)
            {
                viewpager.SetCurrentItem(1, true);
                BnveToAccentColor(1);
            }
            else if (e.Item.ItemId == Resource.Id.action_home)
            {
                viewpager.SetCurrentItem(0, true);
                BnveToAccentColor(0);
            }
            else if (e.Item.ItemId == Resource.Id.action_rating)
            {
                viewpager.SetCurrentItem(2, true);
                BnveToAccentColor(2);
            }
            else if (e.Item.ItemId == Resource.Id.action_account)
            {
                viewpager.SetCurrentItem(3, true);
                BnveToAccentColor(3);
            }
        }

        void BnveToAccentColor(int index)
        {
            var img = bnve.GetIconAt(1);
            var txt = bnve.GetLargeLabelAt(1);
            img.SetColorFilter(Color.Rgb(255, 255, 255));
            txt.SetTextColor(Color.Rgb(255, 255, 255));

            var img0 = bnve.GetIconAt(0);
            var txt0 = bnve.GetLargeLabelAt(0);
            img0.SetColorFilter(Color.Rgb(255, 255, 255));
            txt0.SetTextColor(Color.Rgb(255, 255, 255));

            var img2 = bnve.GetIconAt(2);
            var txt2 = bnve.GetLargeLabelAt(2);
            img2.SetColorFilter(Color.Rgb(255, 255, 255));
            txt2.SetTextColor(Color.Rgb(255, 255, 255));

            var img3 = bnve.GetIconAt(3);
            var txt3 = bnve.GetLargeLabelAt(3);
            img3.SetColorFilter(Color.Rgb(255, 255, 255));
            txt3.SetTextColor(Color.Rgb(255, 255, 255));

            //Sets Accent   Color
            var imgindex = bnve.GetIconAt(index);
            var textindex = bnve.GetLargeLabelAt(index);
            imgindex.SetColorFilter(Color.Rgb(24, 191, 242));
            textindex.SetTextColor(Color.Rgb(24, 191, 242));
        }

        private void SetupViewPager()
        {
            ViewPagerAdapter adapter = new ViewPagerAdapter(SupportFragmentManager);
            adapter.AddFragment(homeFragment, "Home");
            adapter.AddFragment(earningsFragment, "Earnings");
            adapter.AddFragment(ratingsFragment, "Rating");
            adapter.AddFragment(accountFragment, "Account");
            viewpager.Adapter = adapter;
        }

        bool CheckSpecialPermision()
        {
            bool permissionGranted = false;
            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != Android.Content.PM.Permission.Granted &&
                ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != Android.Content.PM.Permission.Granted)
            {
                RequestPermissions(permissionGroup, RequestID);
            }
            else
            {
                permissionGranted = true;
            }
            return permissionGranted;
        }

        protected override void OnPause()
        {
            isBackground = true;
            base.OnPause();
        }
        protected override void OnResume()
        {
            isBackground = false;
            if (newRideAssigned)
            {
                CreateNewRequestDialogue();
                newRideAssigned = false;
            }
            base.OnResume();
        }
    }
}