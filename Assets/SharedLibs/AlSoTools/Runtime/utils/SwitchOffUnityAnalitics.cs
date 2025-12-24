using UnityEngine;
using UnityEngine.Analytics;

namespace AlSo
{
    class SwitchOffUnityAnalitics
    {
        [RuntimeInitializeOnLoadMethod]
        static void DoSwitchOffAnalytics()
        {
            Analytics.initializeOnStartup = false;
            Analytics.enabled = false;
            PerformanceReporting.enabled = false;
            Analytics.limitUserTracking = true;
            Analytics.deviceStatsEnabled = false;
        }

    }
}