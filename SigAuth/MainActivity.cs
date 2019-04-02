using System.Drawing;
using System.IO;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using IntermediateLib;
using Xamarin.Controls;
using Color = Android.Graphics.Color;

namespace SigAuth
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private PointF[] points;
        private IAppService appService;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            appService = new AppService();
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            var signatureView = FindViewById<SignaturePadView>(Resource.Id.signatureView);

            var btnTeach = FindViewById<Button>(Resource.Id.btnTeach);
            var btnCheck = FindViewById<Button>(Resource.Id.btnCheck);

            var editId = FindViewById<EditText>(Resource.Id.textNumber);
            var editName = FindViewById<EditText>(Resource.Id.textName);

            btnTeach.Click += delegate
            {
                appService.TrainSignature(signatureView.RawPoints, int.Parse(editId.Text));
                Toast.MakeText(this, "Vector signature saved to memory.", ToastLength.Short).Show();
            };

            btnCheck.Click += delegate
            {
                appService.CheckSignature(signatureView.RawPoints, int.Parse(editId.Text));
            };

        }
    }
}