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
using Java.Util;

namespace Ja_Tours_Driver.EventListeners
{
    public class AvailabilityListener : Java.Lang.Object, IValueEventListener
    {
        FirebaseDatabase database;
        DatabaseReference availabilityRef;

        public class RideAssignedIDEventArgs : EventArgs
        {
            public string RideId { get; set; }
        }

        public event EventHandler<RideAssignedIDEventArgs> RideAssigned;
        public event EventHandler RideCancelled;
        public event EventHandler RideTimeout;
        public void OnCancelled(DatabaseError error)
        {
            
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
           if(snapshot.Value != null)
            {
                string ride_id = snapshot.Child("ride_id").Value.ToString();
                if(ride_id != "waiting"&& ride_id !="timeout"&& ride_id != "cancelled")
                {
                    //ride assign
                    RideAssigned?.Invoke(this, new RideAssignedIDEventArgs { RideId = ride_id });

                }
                else if (ride_id == "timeout")
                {
                    //Ride Timeout
                    RideTimeout?.Invoke(this, new EventArgs());
                }
                else if (ride_id == "cancelled")
                {
                    //ride cancell
                    RideCancelled?.Invoke(this, new EventArgs());
                }
            }
        }

        public void Create(Android.Locations.Location myLocation)
        {
            database = AppDataHelper.GetDatabase();
            string driverID = AppDataHelper.GetCurrentUser().Uid;

            availabilityRef = database.GetReference("driversAvailable/" + driverID);

            HashMap location = new HashMap();
            location.Put("latitude", myLocation.Latitude);
            location.Put("longitude", myLocation.Longitude);

            HashMap driverInfo = new HashMap();
            driverInfo.Put("location", location);
            driverInfo.Put("ride_id", "waiting");

            availabilityRef.AddValueEventListener(this);
            availabilityRef.SetValue(driverInfo);
        }
        public void RemoveListener()
        {
            availabilityRef.RemoveValue();
            availabilityRef.RemoveEventListener(this);
            availabilityRef = null;
        }

        public void UpDateLocation(Android.Locations.Location mylocation)
        {
            string DriverId = AppDataHelper.GetCurrentUser().Uid;
            if(availabilityRef != null)
            {
                DatabaseReference locationref = database.GetReference("driversAvailable/" + DriverId + "/location");
                HashMap locationMap = new HashMap();
                locationMap.Put("latitude", mylocation.Latitude);
                locationMap.Put("longitude", mylocation.Longitude);
                locationref.SetValue(locationMap);
            }
        }
        public void ReActivate()
        {
            availabilityRef.Child("ride_id").SetValue("waiting");
        }
    }
}