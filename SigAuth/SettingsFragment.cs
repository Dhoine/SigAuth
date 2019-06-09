using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Views;

namespace SigAuth
{
    public class SettingsFragment : PreferenceFragment, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private readonly List<string> NeedToInit = new List<string>
            {"dtw_features", "epw_features", "wavelet_type", "verification_method", "wavelet_level"};

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            InitSummary(sharedPreferences, key);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Xml.Preferences);
            var prefs = PreferenceManager.SharedPreferences;
            prefs.RegisterOnSharedPreferenceChangeListener(this);
            NeedToInit.ForEach(pr => InitSummary(prefs, pr));
            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            // return inflater.Inflate(Resource.Layout.YourFragment, container, false);

            return base.OnCreateView(inflater, container, savedInstanceState);
        }

        private void InitSummary(ISharedPreferences sharedPreferences, string key)
        {
            var pref = FindPreference(key);
            switch (key)
            {
                case "dtw_features":
                case "epw_features":
                    pref.Summary = string.Join(", ", sharedPreferences.GetStringSet(key, null));
                    break;
                case "wavelet_type":
                case "verification_method":
                    pref.Summary = ((ListPreference) pref).Entry;
                    break;
                case "wavelet_level":
                    pref.Summary = sharedPreferences.GetInt(key, 0).ToString();
                    break;
            }
        }
    }
}