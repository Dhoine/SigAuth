using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using IntermediateLib;
using SharedClasses;

namespace SigAuth
{
    [Activity(Label = "Admin Signatures", Theme = "@style/AppTheme.NoActionBar", LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]
    public class AdminActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private AppService service;
        private int currentSigNum = -1;
        private int currentSampleNum = -1;

        private List<KeyValuePair<int, string>>
            SignatureNumbers;

        private List<KeyValuePair<int, string>>
            SamplesNumbers;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            service = new AppService(PreferenceManager.GetDefaultSharedPreferences(this));
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_admin);
            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            Spinner spinner = FindViewById<Spinner>(Resource.Id.spinner);
            spinner.ItemSelected += spinner_ItemSelected;
            Spinner sampleSpinner = FindViewById<Spinner>(Resource.Id.spinner_samples);
            sampleSpinner.ItemSelected += samples_spinner_ItemSelected;
            ReInitSpinner(spinner);
            var renameButton = FindViewById<Button>(Resource.Id.btnRename);
            renameButton.Click += delegate
            {
                if (currentSigNum != -1)
                {
                    var nameEdit = FindViewById<EditText>(Resource.Id.signature_name);
                    service.SetSignatureName(currentSigNum, nameEdit.Text);
                    ReInitSpinner(spinner);
                }
                else
                {
                    Toast.MakeText(this, "Non-existent signature selected", ToastLength.Short).Show();
                }
            };

            var deleteBtn = FindViewById<Button>(Resource.Id.btnDelete);
            deleteBtn.Click += delegate
            {
                if (currentSigNum != -1)
                {
                    service.DeleteSignature(currentSigNum);
                    ReInitSpinner(spinner);
                }
                else
                {
                    Toast.MakeText(this, "Non-existent signature selected", ToastLength.Short).Show();
                }
            };

            var deleteSampleBtn = FindViewById<Button>(Resource.Id.btnDeleteSample);
            deleteSampleBtn.Click += delegate
            {
                if (currentSampleNum != -1)
                {
                    service.DeleteSignatureSample(currentSigNum, currentSampleNum);
                    ReInitSampleSpinner();
                }
                else
                {
                    Toast.MakeText(this, "Non-existent signature selected", ToastLength.Short).Show();
                }
            };
        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
            {
                drawer.CloseDrawer(GravityCompat.Start);
            }
            else
            {
                base.OnBackPressed();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                Intent intent = new Intent(this, typeof(SettingsActivity));
                intent.AddFlags(ActivityFlags.ReorderToFront);
                StartActivity(intent);
            }

            return base.OnOptionsItemSelected(item);
        }

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            int id = item.ItemId;

            if (id == Resource.Id.nav_pad)
            {
                Intent intent = new Intent(this, typeof(MainActivity));
                intent.AddFlags(ActivityFlags.ReorderToFront);
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_admin)
            {
                
            }
            else if (id == Resource.Id.nav_settings)
            {
                Intent intent = new Intent(this, typeof(SettingsActivity));
                intent.AddFlags(ActivityFlags.ReorderToFront);
                StartActivity(intent);
            }

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ReInitSpinner(Spinner spinner)
        {
            var ids = service.GetSavedSignaturesIds();
            SignatureNumbers = new List<KeyValuePair<int, string>>();
            var isEmpty = true;
            foreach (var id in ids)
            {
                var keyValue = new KeyValuePair<int, string>(id, $"#{id}: {service.GetSignatureName(id) ?? "Unknown"}");
                SignatureNumbers.Add(keyValue);
            }

            if (!ids.Any())
            {
                var newKeyValue = new KeyValuePair<int, string>(-1, $"No signatures");
                SignatureNumbers.Add(newKeyValue);
            }

            

            List<string> signatureNames = new List<string>();
            foreach (var item in SignatureNumbers)
                signatureNames.Add(item.Value);
            var adapter = new ArrayAdapter<string>(this,
                Android.Resource.Layout.SimpleSpinnerItem, signatureNames);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            var currentPosition = SignatureNumbers.FindIndex(n => n.Key == currentSigNum);
            if (currentPosition == -1)
                currentPosition = 0;
           
            spinner.SetSelection(currentPosition);
            ReInitSampleSpinner();
        }

        private void ReInitSampleSpinner()
        {
            Spinner sampleSpinner = FindViewById<Spinner>(Resource.Id.spinner_samples);
            var sampleNumbers = service.GetSampleNumbersForId(currentSigNum);
            SamplesNumbers = new List<KeyValuePair<int, string>>();


            foreach (var id in sampleNumbers)
            {
                var keyValue = new KeyValuePair<int, string>(id, $"{id}");
                SamplesNumbers.Add(keyValue);
            }

            if (sampleNumbers == null || !sampleNumbers.Any())
            {
                var newKeyValue = new KeyValuePair<int, string>(-1, $"No samples");
                SamplesNumbers.Add(newKeyValue);
            }

            List<string> sampleNumbersArray = new List<string>();
            foreach (var item in SamplesNumbers)
                sampleNumbersArray.Add(item.Value);

            var sampleAdapter = new ArrayAdapter<string>(this,
                Android.Resource.Layout.SimpleSpinnerItem, sampleNumbersArray);

            sampleAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sampleSpinner.Adapter = sampleAdapter;
            var currentSampleNumPos = SamplesNumbers.FindIndex(n => n.Key == currentSampleNum);
            if (currentSampleNumPos == -1)
                currentSampleNumPos = 0;
            sampleSpinner.SetSelection(currentSampleNumPos);
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            var item = spinner.GetItemAtPosition(e.Position);
            currentSigNum = SignatureNumbers.First(i => i.Value.Equals(item.ToString())).Key;
            currentSampleNum = -1;
            ReInitSampleSpinner();
        }

        private void samples_spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;
            var item = spinner.GetItemAtPosition(e.Position);
            currentSampleNum = SamplesNumbers.First(i => i.Value.Equals(item.ToString())).Key;
            if (currentSampleNum != -1)
            {
                var imageView = FindViewById<ImageView>(Resource.Id.imageView);
                var imageArray = service.GetSignaturePoints(currentSigNum, currentSampleNum);
                var imgArray = FeatureFunctions.ConvertToArray(imageArray);
                imageView.SetImageBitmap(BitmapFactory.DecodeByteArray(imgArray, 0, imgArray.Length));
            }
            
        }
    }
}