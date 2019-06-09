using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
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
using Xamarin.Controls;
using Xamarin.Essentials;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace SigAuth
{
    [Activity(Label = "Signature Pad", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private AppService service;
        private int currentSigNum;

        private List<KeyValuePair<int, string>>
            SignatureNumbers;

        protected override void OnResume()
        {
            base.OnResume();
            var spinner = FindViewById<Spinner>(Resource.Id.spinner);
            ReInitSpinner(spinner);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            PreferenceManager.SetDefaultValues(this, Resource.Xml.Preferences, false);
            service = new AppService(PreferenceManager.GetDefaultSharedPreferences(this));
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
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
            ReInitSpinner(spinner);

            var signatureView = FindViewById<SignaturePadView>(Resource.Id.signatureView);
            var btnTeach = FindViewById<Button>(Resource.Id.btnTeach);
            btnTeach.Click += delegate
            {
                service.TrainSignature(signatureView.RawPoints, currentSigNum);
                Toast.MakeText(this, "Vector signature saved to memory.", ToastLength.Short).Show();
                ReInitSpinner(spinner);
            };

            var btnCheck = FindViewById<Button>(Resource.Id.btnCheck);
            btnCheck.Click += delegate
            {
                var res = service.CheckSignature(signatureView.RawPoints, currentSigNum);
                Toast.MakeText(this, res.ToString(), ToastLength.Long).Show();
                ReInitSpinner(spinner);
            };
        }

        private void ReInitSpinner(Spinner spinner)
        {
            var ids = service.GetSavedSignaturesIds();
            SignatureNumbers = new List<KeyValuePair<int, string>>();
            var lastId = 0;
            foreach (var id in ids)
            {
                var keyValue = new KeyValuePair<int, string>(id, $"#{id}: {service.GetSignatureName(id) ?? "Unknown"}");
                SignatureNumbers.Add(keyValue);
                lastId = id;
            }

            if (ids.Any()) lastId++;

            var newKeyValue = new KeyValuePair<int, string>(lastId, $"#{lastId}: NEW SIGNATURE");
            SignatureNumbers.Add(newKeyValue);

            var signatureNames = new List<string>();
            foreach (var item in SignatureNumbers)
                signatureNames.Add(item.Value);
            var adapter = new ArrayAdapter<string>(this,
                Android.Resource.Layout.SimpleSpinnerItem, signatureNames);

            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            spinner.SetSelection(SignatureNumbers.FindIndex(n => n.Key == currentSigNum));
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

        public bool OnNavigationItemSelected(IMenuItem item)
        {
            var id = item.ItemId;

            if (id == Resource.Id.nav_admin)
            {
                var intent = new Intent(this, typeof(AdminActivity));
                intent.AddFlags(ActivityFlags.ReorderToFront);
                StartActivity(intent);
            }
            else if (id == Resource.Id.nav_settings)
            {
                var intent = new Intent(this, typeof(SettingsActivity));
                intent.AddFlags(ActivityFlags.ReorderToFront);
                StartActivity(intent);
            }

            var drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var spinner = (Spinner) sender;
            var item = spinner.GetItemAtPosition(e.Position);
            currentSigNum = SignatureNumbers.First(i => i.Value.Equals(item.ToString())).Key;
        }
    }
}