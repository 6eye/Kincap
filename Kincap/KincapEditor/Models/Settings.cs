using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace Kincap.Models
{
    class Settings
    {
        public delegate void SettingsHandler();
        public static event SettingsHandler OnSetSettings;

        private static TransformSmoothParameters smoothingParam;
        public static TransformSmoothParameters SmoothingParam
        {
            get { return Settings.smoothingParam; }
            set { Settings.smoothingParam = value; }
        }

        private static int fpsSetting;
        public static int FpsSetting
        {
            get { return Settings.fpsSetting; }
            set { Settings.fpsSetting = value; }
        }
        private static string smoothSetting;
        public static string SmoothSetting
        {
            get { return Settings.smoothSetting; }
            set { Settings.smoothSetting = value; }
        }

        private static bool replayEnable;
        public static bool ReplayEnable
        {
            get { return Settings.replayEnable; }
            set { Settings.replayEnable = value; }
        }

        private static bool seatedMode;
        public static bool SeatedMode
        {
            get { return Settings.seatedMode; }
            set { Settings.seatedMode = value; }
        }

        private static bool nearMode;
        public static bool NearMode
        {
            get { return Settings.nearMode; }
            set { Settings.nearMode = value; }
        }

        public static void SetSettings()
        {
            if (SmoothSetting == "Default")
            {
                // Some smoothing with little latency (defaults).
                // Only filters out small jitters.
                // Good for gesture recognition in games.
                smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.5f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.05f;
                    smoothingParam.MaxDeviationRadius = 0.04f;
                };
            }
            else if (SmoothSetting == "High")
            {

                // Smoothed with some latency.
                // Filters out medium jitters.
                // Good for a menu system that needs to be smooth but
                // doesn't need the reduced latency as much as gesture recognition does.
                smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.5f;
                    smoothingParam.Correction = 0.1f;
                    smoothingParam.Prediction = 0.5f;
                    smoothingParam.JitterRadius = 0.1f;
                    smoothingParam.MaxDeviationRadius = 0.1f;
                };
            }
            else if (SmoothSetting == "Very High")
            {
                // Very smooth, but with a lot of latency.
                // Filters out large jitters.
                // Good for situations where smooth data is absolutely required
                // and latency is not an issue.
                smoothingParam = new TransformSmoothParameters();
                {
                    smoothingParam.Smoothing = 0.7f;
                    smoothingParam.Correction = 0.3f;
                    smoothingParam.Prediction = 1.0f;
                    smoothingParam.JitterRadius = 1.0f;
                    smoothingParam.MaxDeviationRadius = 1.0f;
                };
            }

            OnSetSettings.Invoke();
        }
    }
}
