using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.Database;
using Ja_Tours_Driver.Helpers;

namespace Ja_Tours_Driver.EventListeners
{
    public class NewTripEventListener : Java.Lang.Object, IValueEventListener
    {
        string mRideID;
        Android.Locations.Location mLastlocation;
        FirebaseDatabase database;
        DatabaseReference tripRef;

        //flag
        bool isAccepted;
        public NewTripEventListener(string ride_id, Android.Locations.Location lastlocation)
        {
            mRideID = ride_id;
            mLastlocation = lastlocation;
            database = AppDataHelper.GetDatabase();
        }
        public void OnCancelled(DatabaseError error)
        {
            throw new NotImplementedException();
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if(snapshot.Value != null)
            {
                if (!isAccepted)
                {
                    isAccepted = true;
                    Accept();
                }
            }
        }

        public void Create()
        {
            tripRef = database.GetReference("rideRequest/" + mRideID);
            tripRef.AddValueEventListener(this);
        }
        void Accept()
        {
            tripRef.Child("status").SetValue("accepted");
            tripRef.Child("driver_name").SetValue(AppDataHelper.GetFullName());
            tripRef.Child("driver_phone").SetValue(AppDataHelper.GetPhone());
            tripRef.Child("driver_location").Child("latitude").SetValue(mLastlocation.Latitude);
            tripRef.Child("driver_location").Child("longitude").SetValue(mLastlocation.Longitude);
            tripRef.Child("driver_id").SetValue(AppDataHelper.GetCurrentUser().Uid);
        }

        public void UpdateLocation(Android.Locations.Location lastlocation)
        {
            mLastlocation = lastlocation;
            tripRef.Child("driver_location").Child("latitude").SetValue(mLastlocation.Latitude);
            tripRef.Child("driver_location").Child("longitude").SetValue(mLastlocation.Longitude);

        }

        public void UpdateStatus(string status)
        {
            tripRef.Child("status").SetValue(status);
        }

        public void EndTrip(double fares)
        {
            if (tripRef != null)
            {
                tripRef.Child("fares").SetValue(fares);
                tripRef.Child("status").SetValue("ended");
                tripRef.RemoveEventListener(this);
                tripRef = null;

            }
        }
    }
}