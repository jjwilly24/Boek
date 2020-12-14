﻿using System;
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
    public class ProfileEventListener : Java.Lang.Object, IValueEventListener
    {
        ISharedPreferences preferences = Application.Context.GetSharedPreferences("userinfo", FileCreationMode.Private);
        ISharedPreferencesEditor editor;
        public void OnCancelled(DatabaseError error)
        {
            
        }

        public void OnDataChange(DataSnapshot snapshot)
        {
            if(snapshot.Value != null)
            {
                string fullname, email, phone;
                fullname = (snapshot.Child("fullname") != null) ? snapshot.Child("fullname").Value.ToString() : "";
                email = (snapshot.Child("email") != null) ? snapshot.Child("email").Value.ToString() : "";
                phone = (snapshot.Child("phone") != null) ? snapshot.Child("phone").Value.ToString() : "";

                editor.PutString("fullname", fullname);
                editor.PutString("phone", phone);
                editor.PutString("email", email);
                editor.Apply();
            }
        }
        public void Create()
        {
            editor = preferences.Edit();
            FirebaseDatabase database = AppDataHelper.GetDatabase();
            string driverID = AppDataHelper.GetCurrentUser().Uid;
            DatabaseReference driverRef = database.GetReference("drivers/" + driverID);
            driverRef.AddValueEventListener(this);
        }
    }
}