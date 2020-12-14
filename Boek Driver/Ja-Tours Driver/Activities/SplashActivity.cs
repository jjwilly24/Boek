﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Firebase.Auth;
using Ja_Tours_Driver.Helpers;

namespace Ja_Tours_Driver.Activities
{
    [Activity(Label = "Boek Driver", Theme ="@style/MyTheme.Splash", MainLauncher =true, Icon = "@drawable/iconimage")]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
        }
        protected override void OnResume()
        {
            base.OnResume();

            FirebaseUser currentUser = AppDataHelper.GetCurrentUser();
            if(currentUser == null)
            {
                StartActivity(typeof(LoginActivity));
            }
            else
            {
                StartActivity(typeof(MainActivity));
            }
        }
    }
}