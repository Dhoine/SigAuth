using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
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
using Xamarin.Essentials;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace SigAuth
{
    [Activity(Label = "Admin Signatures", Theme = "@style/AppTheme.NoActionBar", LaunchMode = LaunchMode.SingleTask)]
    public class AdminActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private int currentSampleNum = -1;
        private int currentSigNum = -1;

        private List<KeyValuePair<int, string>>
            samplesNumbers;

        private AppService service;

        private List<KeyValuePair<int, string>>
            signatureNumbers;

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            var id = item.ItemId;

            switch (id)
            {
                case Resource.Id.nav_pad:
                {
                    var intent = new Intent(this, typeof(MainActivity));
                    intent.AddFlags(ActivityFlags.ReorderToFront);
                    StartActivity(intent);
                    break;
                }

                case Resource.Id.nav_admin:
                    break;
                case Resource.Id.nav_settings:
                {
                    var intent = new Intent(this, typeof(SettingsActivity));
                    intent.AddFlags(ActivityFlags.ReorderToFront);
                    StartActivity(intent);
                    break;
                }
            }

            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            service = new AppService(PreferenceManager.GetDefaultSharedPreferences(this));
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_admin);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            var toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open,
                Resource.String.navigation_drawer_close);
            drawer.AddDrawerListener(toggle);
            toggle.SyncState();

            var navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            var spinner = FindViewById<Spinner>(Resource.Id.spinner);
            spinner.ItemSelected += spinner_ItemSelected;
            var sampleSpinner = FindViewById<Spinner>(Resource.Id.spinner_samples);
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
            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            if (drawer.IsDrawerOpen(GravityCompat.Start))
                drawer.CloseDrawer(GravityCompat.Start);
            else
                base.OnBackPressed();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                var intent = new Intent(this, typeof(SettingsActivity));
                intent.AddFlags(ActivityFlags.ReorderToFront);
                StartActivity(intent);
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ReInitSpinner(Spinner spinner)
        {
            var ids = service.GetSavedSignaturesIds();
            signatureNumbers = new List<KeyValuePair<int, string>>();
            foreach (var id in ids)
            {
                var keyValue = new KeyValuePair<int, string>(id, $"#{id}: {service.GetSignatureName(id) ?? "Unknown"}");
                signatureNumbers.Add(keyValue);
            }

            if (!ids.Any())
            {
                var newKeyValue = new KeyValuePair<int, string>(-1, "No signatures");
                signatureNumbers.Add(newKeyValue);
            }


            var signatureNames = new List<string>();
            foreach (var item in signatureNumbers)
                signatureNames.Add(item.Value);
            var adapter = new ArrayAdapter<string>(this,
                Android.Resource.Layout.SimpleSpinnerItem, signatureNames);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            var currentPosition = signatureNumbers.FindIndex(n => n.Key == currentSigNum);
            if (currentPosition == -1)
                currentPosition = 0;

            spinner.SetSelection(currentPosition);
            ReInitSampleSpinner();
        }

        private void ReInitSampleSpinner()
        {
            var sampleSpinner = FindViewById<Spinner>(Resource.Id.spinner_samples);
            var sampleNumbers = service.GetSampleNumbersForId(currentSigNum);
            samplesNumbers = new List<KeyValuePair<int, string>>();


            foreach (var id in sampleNumbers)
            {
                var keyValue = new KeyValuePair<int, string>(id, $"{id}");
                samplesNumbers.Add(keyValue);
            }

            if (!sampleNumbers.Any())
            {
                var newKeyValue = new KeyValuePair<int, string>(-1, "No samples");
                samplesNumbers.Add(newKeyValue);
            }

            var sampleNumbersArray = new List<string>();
            foreach (var item in samplesNumbers)
                sampleNumbersArray.Add(item.Value);

            var sampleAdapter = new ArrayAdapter<string>(this,
                Android.Resource.Layout.SimpleSpinnerItem, sampleNumbersArray);

            sampleAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sampleSpinner.Adapter = sampleAdapter;
            var currentSampleNumPos = samplesNumbers.FindIndex(n => n.Key == currentSampleNum);
            if (currentSampleNumPos == -1)
                currentSampleNumPos = 0;
            sampleSpinner.SetSelection(currentSampleNumPos);
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var spinner = (Spinner) sender;
            var item = spinner.GetItemAtPosition(e.Position);
            currentSigNum = signatureNumbers.First(i => i.Value.Equals(item.ToString())).Key;
            currentSampleNum = -1;
            ReInitSampleSpinner();
        }

        private void samples_spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var spinner = (Spinner) sender;
            var item = spinner.GetItemAtPosition(e.Position);
            currentSampleNum = samplesNumbers.First(i => i.Value.Equals(item.ToString())).Key;
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