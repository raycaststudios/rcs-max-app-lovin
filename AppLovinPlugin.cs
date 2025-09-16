using System;
using System.Collections.Generic;
using UnityEngine;
using static MaxSdkBase;
using System.Reflection;


#if RCS_SOLAR_ENGINE
using SolarEngine;
#endif
namespace RCS.PluginMediation
{
    public enum AdPlacement
    {
        TopLeft, TopCenter, TopRight, 
        Centered, CenterLeft, CenterRight,
        BottomLeft, BottomCenter, BottomRight, 
    }
    public class AppLovinPlugin : IAppLovin
    {
        private string InterstitialID;
        private string RewardedVideoID;
        private string AppOpenID;
        private string BannerID;

        private List<string> MRecID = new List<string>();

        private bool InitSucceded = false;
        private bool isAdRunning = false;
        private bool adCallbacksInitialized = false;
        private bool enableMaxBanner = false;
        private bool enableMaxMRec = false;

        private Action RewardHandle;
        private Action RewardCloseAction;

        public int interstitialCloseCount = 0;
        public int appOpenCloseCount = 0;
        public int interstitialAdImpressionCount = 0;
        public int rewardedVideoAdImpressionCount = 0;
        public int appOpenAdImpressionCount = 0;

        private string[] AndroidTestDevices = { "63c9001e-9940-49f6-86a6-10ca1ad1abe1", "72520534-fcad-44b4-8467-17ff2e48ba4c", "a37eece2-bb72-434e-83fc-f3ed50921f50" };
        private string[] iOSTestDevices = { "9661A829-819A-4BD5-BF94-A852AAA3ED66" };

        public void Initialize(string interstitialID = null, string rewardedVideoID = null, string appOpenID = null, string bannerID = null, List<string> mrecID = null
            , MaxSdk.AdViewPosition[] adPlacement = null)
        {
#if UNITY_IOS
            MaxSdk.SetTestDeviceAdvertisingIdentifiers(iOSTestDevices);
#elif UNITY_ANDROID
            MaxSdk.SetTestDeviceAdvertisingIdentifiers(AndroidTestDevices);
#endif

            InterstitialID = interstitialID;
            RewardedVideoID = rewardedVideoID;
            AppOpenID = appOpenID;
            BannerID = bannerID;
            if (mrecID != null)
            {
                MRecID.AddRange(mrecID);
            }


            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                InitSucceded = true;
                Debug.Log("AppLovin SDK Initialized Successfully");
                MaxSdk.LoadInterstitial(InterstitialID);
                MaxSdk.LoadRewardedAd(RewardedVideoID);
                MaxSdk.LoadAppOpenAd(AppOpenID);

                if (IsMaxBannerEnabled())
                {
                    Debug.Log("AppLovin Banner Ad Enabled");
                    MaxSdk.LoadBanner(BannerID);

                    var adViewConfiguration = new MaxSdk.AdViewConfiguration(MaxSdk.AdViewPosition.TopCenter);
                    MaxSdk.CreateBanner(BannerID, adViewConfiguration);
                }

                if(IsMaxMRecEnabled())
                {
                    Debug.Log("AppLovin MRec Ad Enabled");
                    for (int i = 0; i < MRecID.Count; i++)
                    {
                        var adViewConfiguration = new MaxSdk.AdViewConfiguration(adPlacement[i]);
                        MaxSdk.CreateMRec(MRecID[i], adViewConfiguration);
                    }
                }
            };
            MaxSdk.InitializeSdk();
        }

        public void InitializeCallbackEvents()
        {
            if (adCallbacksInitialized)
            {
                Debug.LogWarning("Ad callbacks already initialized. Skipping.");
                return;
            }

            adCallbacksInitialized = true;
            Debug.Log("Registering Ad callbacks for the first time.");

            // Interstitial
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstiatialAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstiatialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstiatialDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstiatialDisplayEvent;

            // App Open
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAppOpenAdRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenDisplayEvent;

            // Rewarded
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayEvent;

            // Banner
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;

            // MRec
            MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;
        }

        #region Interstitial Ad

        public bool IsInterstitialAvailable()
        {
            return MaxSdk.IsInterstitialReady(InterstitialID);
        }

        public void ShowInterstitial()
        {
            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                if (!InitSucceded || isAdRunning) return;

                if (MaxSdk.IsInterstitialReady(InterstitialID))
                {
                    TrackAdRequest("max", "interstitial_ad", InterstitialID); // <--- ADD THIS
                    MaxSdk.ShowInterstitial(InterstitialID);
                    Debug.Log("Showing Interstitial Ad");
                }
                else
                {
                    MaxSdk.LoadInterstitial(InterstitialID);
                }
            }
        }

        #endregion

        #region Rewarded Video Ad

        public bool IsRewardedVideoAvailable()
        {
            if (!InitSucceded) return false;
            return MaxSdk.IsRewardedAdReady(RewardedVideoID);
        }

        public void ShowRewardedVideo(Action reward, Action cancelCallBack = null)
        {
            if (!InitSucceded || isAdRunning) return;

            if (MaxSdk.IsRewardedAdReady(RewardedVideoID))
            {
                RewardHandle = reward;
                RewardCloseAction = cancelCallBack;
                TrackAdRequest("max", "rewarded_video_ad", RewardedVideoID); // <--- ADD THIS
                MaxSdk.ShowRewardedAd(RewardedVideoID);
                Debug.Log("Showing Rewarded Video Ad");
            }
            else
            {
                MaxSdk.LoadRewardedAd(RewardedVideoID);
            }
        }

        private void OnRewardedAdReceivedRewardEvent(string arg1, MaxSdkBase.Reward arg2, MaxSdkBase.AdInfo arg3)
        {
            RewardGranted();
            TrackRewardedSuccess(arg2.Label); // <--- ADD THIS
        }

        public void RewardGranted()
        {
            RewardHandle?.Invoke();
            RewardCloseAction?.Invoke();
        }

        #endregion

        #region AppOpen Ad

        public bool isAppOpenAvailable()
        {
            return MaxSdk.IsAppOpenAdReady(AppOpenID);
        }

        public void ShowAppOpen()
        {
            if (PlayerPrefs.GetInt("AdsRemoved") == 0)
            {
                if (!InitSucceded || isAdRunning) return;

                if (MaxSdk.IsAppOpenAdReady(AppOpenID))
                {
                    TrackAdRequest("max", "app_open_ad", AppOpenID);
                    MaxSdk.ShowAppOpenAd(AppOpenID);
                    Debug.Log("Showing App Open Ad");
                }

                MaxSdk.LoadAppOpenAd(AppOpenID);
            }
        }

        #endregion

        #region Banner Ad

        private bool IsMaxBannerEnabled()
        {
            return enableMaxBanner;
        }

        public void EnableMaxBannerState(bool state)
        {
            enableMaxBanner = state;
        }

        public void ShowBanner()
        {
            if (IsMaxBannerEnabled())
            {
                MaxSdk.ShowBanner(BannerID);
                Debug.Log("Showing Banner Ad");
            }
            else
            {
                Debug.LogWarning("Max Banner is not enabled. Skipping ShowBanner call.");
            }
        }

        public void HideBanner()
        {
            MaxSdk.HideBanner(BannerID);
            Debug.Log("Hiding Banner Ad");
        }

        #endregion

        #region MRec Ad
        private bool IsMaxMRecEnabled()
        {
            return enableMaxMRec;
        }

        public void EnableMaxMRecState(bool state)
        {
            enableMaxMRec = state;
        }

        public void ShowMRecAd(AdPlacement placement)
        {
            int index = (int)placement;

            if (index >= 0 && index < MRecID.Count) // Safe check
            {
                MaxSdk.ShowMRec(MRecID[index]);
            }
        }

        public void HideMRec(AdPlacement placement)
        {
            int index = (int)placement;

            if (index >= 0 && index < MRecID.Count) // Correct boundary check
            {
                MaxSdk.HideMRec(MRecID[index]);
            }
        }

        #endregion

        #region Rewarded Ad Callback events

        private void OnRewardedAdHiddenEvent(string arg1, MaxSdkBase.AdInfo arg2)
        {
            isAdRunning = false;
            MaxSdk.LoadRewardedAd(RewardedVideoID);
        }

        private void OnRewardedAdLoadFailedEvent(string arg1, MaxSdkBase.ErrorInfo arg2)
        {
            MaxSdk.LoadRewardedAd(RewardedVideoID);
            TrackAdLoadFailure("MAX", "Rewarded Video Ad", RewardedVideoID, arg2.Message);
        }

        private void OnRewardedAdDisplayEvent(string arg1, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded Video Ad is Displayed");
            isAdRunning = true;

            int adType = 0;
            switch (adInfo.AdFormat)
            {
                case "Rewarded":
                    adType = 2;
                    break;
                case "Interstitial":
                    adType = 1;
                    break;
                case "Banner":
                    adType = 3;
                    break;
                case "AppOpen":
                    adType = 4;
                    break;
                default:
                    adType = 0; // Fallback value
                    break;
            }

#if RCS_SOLAR_ENGINE
            ImpressionAttributes impressionAttributes = new ImpressionAttributes();
            impressionAttributes.ad_platform = adInfo.NetworkName;
            impressionAttributes.mediation_platform = "MAX";
            impressionAttributes.ad_id = adInfo.AdUnitIdentifier;
            impressionAttributes.ad_type = adType;
            impressionAttributes.ad_ecpm = 0.8;
            impressionAttributes.currency_type = "USD";
            impressionAttributes.is_rendered = true;
            SolarEngine.Analytics.trackAdImpression(impressionAttributes);
#endif
        }

        private void OnRewardedAdDisplayFailedEvent(string arg1, MaxSdkBase.ErrorInfo errInfo, MaxSdkBase.AdInfo adInfo)
        {
            isAdRunning = false;
            MaxSdk.LoadRewardedAd(RewardedVideoID);
            TrackAdDisplayFailure(adInfo, errInfo.Message);
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            TrackAdRevenue(adInfo);  // Optional: send to analytics
            TrackAdRevenue_SolarEngine(adInfo);
        }

        #endregion

        #region App Open Ad Callback events

        private void OnAppOpenAdHiddenEvent(string arg1, MaxSdkBase.AdInfo arg2)
        {
            isAdRunning = false;
            MaxSdk.LoadAppOpenAd(AppOpenID);

            appOpenCloseCount++;
            int targetValue = UnityEngine.Random.Range(4, 7);

            if (appOpenCloseCount >= targetValue)
            {
                appOpenCloseCount = 0;
                // call custom event

                //SubscriptionManager.Instance.ShowOfferPanel(Offer.RemoveAds);
            }
        }

        private void OnAppOpenAdLoadFailedEvent(string arg1, MaxSdkBase.ErrorInfo arg2)
        {
            MaxSdk.LoadAppOpenAd(AppOpenID);
            TrackAdLoadFailure("MAX", "App Open Ad", AppOpenID, arg2.Message);
        }

        private void OnAppOpenDisplayEvent(string arg1, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("App Open Ad is Displayed");
            isAdRunning = true;

            int adType = 0;
            switch (adInfo.AdFormat)
            {
                case "Rewarded":
                    adType = 2;
                    break;
                case "Interstitial":
                    adType = 1;
                    break;
                case "Banner":
                    adType = 3;
                    break;
                case "AppOpen":
                    adType = 4;
                    break;
                default:
                    adType = 0; // Fallback value
                    break;
            }

#if RCS_SOLAR_ENGINE
            ImpressionAttributes impressionAttributes = new ImpressionAttributes();
            impressionAttributes.ad_platform = adInfo.NetworkName;
            impressionAttributes.mediation_platform = "MAX";
            impressionAttributes.ad_id = adInfo.AdUnitIdentifier;
            impressionAttributes.ad_type = adType;
            impressionAttributes.ad_ecpm = 0.8;
            impressionAttributes.currency_type = "USD";
            impressionAttributes.is_rendered = true;
            SolarEngine.Analytics.trackAdImpression(impressionAttributes);
#endif
        }

        private void OnAppOpenAdDisplayFailedEvent(string arg1, MaxSdkBase.ErrorInfo errInfo, MaxSdkBase.AdInfo adInfo)
        {
            isAdRunning = false;
            MaxSdk.LoadAppOpenAd(AppOpenID);
            TrackAdDisplayFailure(adInfo, errInfo.Message);
        }

        private void OnAppOpenAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            TrackAdRevenue(adInfo);  // Optional: send to analytics
            TrackAdRevenue_SolarEngine(adInfo);
        }

        #endregion

        #region Interstitial Ad Callback events

        private void OnInterstiatialAdHiddenEvent(string arg1, MaxSdkBase.AdInfo arg2)
        {
            isAdRunning = false;
            MaxSdk.LoadInterstitial(InterstitialID);

            interstitialCloseCount++;
            int targetValue = UnityEngine.Random.Range(4, 7);

            if (interstitialCloseCount >= targetValue)
            {
                interstitialCloseCount = 0;
                // call custom event
                //SubscriptionManager.Instance.ShowOfferPanel(Offer.RemoveAds);
            }
        }

        private void OnInterstiatialFailedEvent(string arg1, MaxSdkBase.ErrorInfo arg2)
        {
            MaxSdk.LoadInterstitial(InterstitialID);
            TrackAdLoadFailure("MAX", "Interstitial Ad", InterstitialID, arg2.Message);
        }

        private void OnInterstiatialDisplayEvent(string arg1, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Interstitial Ad is Displayed");
            isAdRunning = true;

            int adType = 0;
            switch (adInfo.AdFormat)
            {
                case "Rewarded":
                    adType = 2;
                    break;
                case "Interstitial":
                    adType = 1;
                    break;
                case "Banner":
                    adType = 3;
                    break;
                case "AppOpen":
                    adType = 4;
                    break;
                default:
                    adType = 0; // Fallback value
                    break;
            }

#if RCS_SOLAR_ENGINE
            ImpressionAttributes impressionAttributes = new ImpressionAttributes();
            impressionAttributes.ad_platform = adInfo.NetworkName;
            impressionAttributes.mediation_platform = "MAX";
            impressionAttributes.ad_id = adInfo.AdUnitIdentifier;
            impressionAttributes.ad_type = adType;
            impressionAttributes.ad_ecpm = 0.8;
            impressionAttributes.currency_type = "USD";
            impressionAttributes.is_rendered = true;
            SolarEngine.Analytics.trackAdImpression(impressionAttributes);
#endif
        }

        private void OnInterstiatialDisplayFailedEvent(string arg1, MaxSdkBase.ErrorInfo errInfo, MaxSdkBase.AdInfo adInfo)
        {
            isAdRunning = false;
            MaxSdk.LoadInterstitial(InterstitialID);
            TrackAdDisplayFailure(adInfo, errInfo.Message);
        }

        private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            TrackAdRevenue(adInfo);
            TrackAdRevenue_SolarEngine(adInfo);
        }

        #endregion

        #region Banner Ad Callback events

        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            // Banner ad is ready to be shown.
            Debug.Log("Banner ad loaded");
        }

        private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // Banner ad failed to load. MAX will automatically try loading a new ad internally.
            Debug.Log("Banner ad failed to load with error code: " + errorInfo.Code);
            MaxSdk.LoadBanner(BannerID);
            TrackAdLoadFailure("MAX", "Banner Ad", BannerID, errorInfo.Message);
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Banner ad clicked");
        }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Banner ad revenue paid");

            TrackAdRevenue(adInfo);
            TrackAdRevenue_SolarEngine(adInfo);
        }

        #endregion

        #region MRec Ad Callback events

        private void OnMRecAdLoadedEvent(string arg1, MaxSdkBase.AdInfo arg2)
        {
            Debug.Log("MRec ad loaded");
        }

        private void OnMRecAdLoadFailedEvent(string arg1, MaxSdkBase.ErrorInfo arg2)
        {
            //MaxSdk.LoadMRec(MRecLeftID);
            //MaxSdk.LoadMRec(MRecRightID);
            //TrackAdLoadFailure("MAX", "MREC Ad", MRecLeftID, arg2.Message);
            //TrackAdLoadFailure("MAX", "MREC Ad", MRecRightID, arg2.Message);
        }

        private void OnMRecAdClickedEvent(string arg1, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MRec ad clicked");
        }

        private void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            TrackAdRevenue(adInfo);
            TrackAdRevenue_SolarEngine(adInfo);
        }

        #endregion

        #region Ad Tracking - SolarEngine

        private void TrackAdRequest(string adPlatform, string adType, string placementId)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("ad_platform", adPlatform);
            properties.Add("ad_type", adType);
            properties.Add("placement_id", placementId);
#if RCS_SOLAR_ENGINE
            SolarEngine.Analytics.track("ad_request", properties);
            Debug.Log($"Solar_AdRequest | Platform: {adPlatform}, Type: {adType}, ID: {placementId}");
#endif
        }

        private void TrackAdLoadFailure(string adPlatform, string adType, string placementId, string errorMessage)
        {
            try
            {
                var properties = new Dictionary<string, object>
        {
            { "ad_platform", adPlatform },
            { "ad_type", adType },
            { "placement_id", placementId },
            { "error_message", errorMessage },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        };
#if RCS_SOLAR_ENGINE
                SolarEngine.Analytics.track("Ad_Load_Failure", properties);
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to track ad failure: {e.Message}");
            }
        }

        private void TrackAdDisplayFailure(MaxSdkBase.AdInfo adInfo, string errorMessage)
        {
            try
            {
                var properties = new Dictionary<string, object>
        {
            { "ad_platform", adInfo.NetworkName },
            { "ad_Type", adInfo.AdFormat },
            { "placement_id", adInfo.AdUnitIdentifier },
            { "error_message", errorMessage },
            { "timestamp", System.DateTime.UtcNow.ToString("o") }
        };
#if RCS_SOLAR_ENGINE
                SolarEngine.Analytics.track("Ad_Display_Failure", properties);
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to track ad failure: {e.Message}");
            }
        }

        private void TrackRewardedSuccess(string rewardName)
        {
            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("reward", rewardName);
            properties.Add("success", true);

#if RCS_SOLAR_ENGINE
            SolarEngine.Analytics.track("reward_received", properties);
            Debug.Log("Solar_RewardReceived");
#endif
        }

        private void TrackAdRevenue_SolarEngine(MaxSdkBase.AdInfo adInfo)
        {
            Dictionary<string, object> AdProperties = new Dictionary<string, object>();
            AdProperties.Add("ad_platform", adInfo.NetworkName);
            AdProperties.Add("ad_type", adInfo.AdFormat);
            AdProperties.Add("placement_id", adInfo.AdUnitIdentifier);
            AdProperties.Add("mediation_Platform", "AppLovinbyMAX");

            Dictionary<string, object> RevenueProperties = new Dictionary<string, object>();
            RevenueProperties.Add("_revenue_amount", adInfo.Revenue);      // Revenue generated
            RevenueProperties.Add("_currency_type", "USD");
#if RCS_SOLAR_ENGINE
            SolarEngine.Analytics.track("TrackCustomData", AdProperties, RevenueProperties);
#endif
        }

        #endregion

        #region Ad Tracking - Firebase

        private void TrackAdRevenue(MaxSdkBase.AdInfo impressionData)
        {
#if RCS_FIREBASE
           double revenue = impressionData.Revenue;

        var impressionParameters = new[] {
        new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
        new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
        new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
        new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
        new Firebase.Analytics.Parameter("value", revenue),
        new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
        };
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
#endif
        }

        #endregion
    }
}