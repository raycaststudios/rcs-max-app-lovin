using System.Collections.Generic;

namespace RCS.PluginMediation
{
    public interface IAppLovin
    {
        void Initialize(string interstitialID = null, string rewardedVideoID = null, string appOpenID = null, string bannerID = null, List<string> mrecID = null,
            MaxSdk.AdViewPosition[] adPlacement = null);
        void InitializeCallbackEvents();
    }
}
