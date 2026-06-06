using UnityEngine;
using GoogleMobileAds.Api;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

#if UNITY_ANDROID
    // RELEASE: swap to ca-app-pub-2063629046992074/4451475383
    private const string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#else
    private const string InterstitialAdUnitId = "unused";
#endif

    private InterstitialAd interstitialAd;
    private int levelCompletionCount;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        MobileAds.Initialize(_ => LoadInterstitialAd());
    }

    public void OnLevelComplete()
    {
        levelCompletionCount++;
        if (levelCompletionCount % 3 == 0)
            ShowInterstitialAd();
    }

    private void LoadInterstitialAd()
    {
        interstitialAd?.Destroy();
        interstitialAd = null;

        InterstitialAd.Load(InterstitialAdUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdsManager] Failed to load interstitial: {error?.GetMessage()}");
                return;
            }
            interstitialAd = ad;
            interstitialAd.OnAdFullScreenContentClosed += LoadInterstitialAd;
            interstitialAd.OnAdFullScreenContentFailed += _ => LoadInterstitialAd();
        });
    }

    private void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
            return;
        }
        Debug.Log("[AdsManager] Interstitial not ready — skipping ad.");
    }
}
