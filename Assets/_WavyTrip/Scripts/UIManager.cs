using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
using SgLib;

#if EASY_MOBILE
using EasyMobile;
#endif

public class UIManager : MonoBehaviour
{
    [Header("UI Config")]
    public bool showDailyRewardAtStart = true;
    public bool showWatchForCoinsAtStart = true;

    [Header("Object References")]
    public GameManager gameManager;
    public GameObject header;
    public Text score;
    public Text bestScore;
    public Text coinText;
    public GameObject tapToStart;
    public GameObject characterSelectBtn;
    public GameObject menuButtons;
    public GameObject dailyRewardBtn;
    public Text dailyRewardBtnText;
    public GameObject toastMsg;
    public GameObject rewardUI;
    public GameObject settingsUI;
    public GameObject soundOnBtn;
    public GameObject soundOffBtn;
    public GameObject musicOnBtn;
    public GameObject musicOffBtn;

    [Header("Premium Features Buttons")]
    public GameObject watchForCoinsBtn;
    public GameObject leaderboardBtn;
    public GameObject shareBtn;
    public GameObject iapPurchaseBtn;
    public GameObject removeAdsBtn;
    public GameObject restorePurchaseBtn;

    [Header("In-App Purchase Store")]
    public GameObject storeUI;

    [Header("Sharing-Specific")]
    public GameObject shareUI;
    public ShareUIController shareUIController;

    Animator scoreAnimator;
    Animator dailyRewardAnimator;
    bool isWatchAdsForCoinBtnActive;
    bool isShowShareUI;

    void OnEnable()
    {
        GameManager.GameStateChanged += GameManager_GameStateChanged;
        ScoreManager.ScoreUpdated += OnScoreUpdated;
    }

    void OnDisable()
    {
        GameManager.GameStateChanged -= GameManager_GameStateChanged;
        ScoreManager.ScoreUpdated -= OnScoreUpdated;
    }

    // Use this for initialization
    void Start()
    {
        scoreAnimator = score.GetComponent<Animator>();
        dailyRewardAnimator = dailyRewardBtn.GetComponent<Animator>();

        Reset();
        ShowStartUI();
    }

    // Update is called once per frame
    void Update()
    {
        score.text = ScoreManager.Instance.Score.ToString();
        bestScore.text = ScoreManager.Instance.HighScore.ToString();
        coinText.text = CoinManager.Instance.Coins.ToString();

        if (!DailyRewardController.Instance.disable && dailyRewardBtn.gameObject.activeSelf)
        {
            if (DailyRewardController.Instance.CanRewardNow())
            {
                dailyRewardBtnText.text = "GRAB YOUR REWARD!";
                dailyRewardAnimator.SetTrigger("activate");
            }
            else
            {
                TimeSpan timeToReward = DailyRewardController.Instance.TimeUntilReward;
                dailyRewardBtnText.text = string.Format("REWARD IN {0:00}:{1:00}:{2:00}", timeToReward.Hours, timeToReward.Minutes, timeToReward.Seconds);
                dailyRewardAnimator.SetTrigger("deactivate");
            }
        }

        if (settingsUI.activeSelf)
        {
            UpdateSoundButtons();
            UpdateMusicButtons();
        }
    }

    void GameManager_GameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.Playing)
        {              
            ShowGameUI();
        }
        else if (newState == GameState.PreGameOver)
        {
            // Before game over, i.e. game potentially will be recovered
        }
        else if (newState == GameState.GameOver)
        {
            Invoke("ShowGameOverUI", 1f);
        }
    }

    void OnScoreUpdated(int newScore)
    {
        scoreAnimator.Play("NewScore");
    }

    void Reset()
    {
        settingsUI.SetActive(false);

        header.SetActive(false);
        score.gameObject.SetActive(false);
        tapToStart.SetActive(false);
        characterSelectBtn.SetActive(false);
        menuButtons.SetActive(false);
        dailyRewardBtn.SetActive(false);
        settingsUI.SetActive(false);
        toastMsg.SetActive(false);

        // Enable or disable premium stuff
        bool enablePremium = PremiumFeaturesManager.Instance.enablePremiumFeatures;
        leaderboardBtn.SetActive(enablePremium);
        shareBtn.SetActive(enablePremium);
        iapPurchaseBtn.SetActive(enablePremium);
        removeAdsBtn.SetActive(enablePremium);
        restorePurchaseBtn.SetActive(enablePremium);

        // Hidden by default
        storeUI.SetActive(false);
        settingsUI.SetActive(false);
        shareUI.SetActive(false);

        // These premium feature buttons are hidden by default
        // and shown when certain criteria are met (e.g. rewarded ad is loaded)
        watchForCoinsBtn.gameObject.SetActive(false);
    }

    public void StartGame()
    {
        gameManager.StartGame();
    }

    public void EndGame()
    {
        gameManager.GameOver();
    }

    public void RestartGame()
    {
        gameManager.RestartGame(0.2f);
    }

    void ShowStartUI()
    {
        settingsUI.SetActive(false);

        header.SetActive(true);
        tapToStart.SetActive(true);
        characterSelectBtn.SetActive(true);  

        // If first launch: show "WatchForCoins" and "DailyReward" buttons if the conditions are met
        if (GameManager.GameCount == 0)
        {
            if (showWatchForCoinsAtStart)
                ShowWatchForCoinsBtn();

            if (showDailyRewardAtStart)
                ShowDailyRewardBtn();
        }
    }

    void ShowGameUI()
    {
        header.SetActive(true);
        score.gameObject.SetActive(true);
        tapToStart.SetActive(false);
        characterSelectBtn.SetActive(false);
        dailyRewardBtn.SetActive(false);
        watchForCoinsBtn.SetActive(false);
    }

    void ShowGameOverUI()
    {
        header.SetActive(true);
        score.gameObject.SetActive(true);
        tapToStart.SetActive(false);
        menuButtons.SetActive(true);

        watchForCoinsBtn.gameObject.SetActive(false);
        settingsUI.SetActive(false);

        // Show "WatchForCoins" and "DailyReward" buttons if the conditions are met     
        ShowDailyRewardBtn();

        if (IsPremiumFeaturesEnabled())
        {
            SetUpShareUI();
            ShowWatchForCoinsBtn();
        }
    }

    void ShowWatchForCoinsBtn()
    {
        // Only show "watch for coins button" if a rewarded ad is loaded and premium features are enabled
#if EASY_MOBILE
        if (IsPremiumFeaturesEnabled() && AdDisplayer.Instance.CanShowRewardedAd() && AdDisplayer.Instance.watchAdToEarnCoins)
        {
            watchForCoinsBtn.SetActive(true);
            watchForCoinsBtn.GetComponent<Animator>().SetTrigger("activate");
        }
        else
        {
#if !UNITY_EDITOR
            watchForCoinsBtn.SetActive(false);
#else
            if (IsPremiumFeaturesEnabled())
            {
                watchForCoinsBtn.SetActive(true);   // for testing in the editor
            }
#endif
        }
#elif UNITY_EDITOR
        if (PremiumFeaturesManager.Instance.enablePremiumFeatures)
        {
            watchForCoinsBtn.SetActive(true);   // for testing in the editor
        }
        #endif
    }

    void ShowDailyRewardBtn()
    {
        // Not showing the daily reward button if the feature is disabled
        if (!DailyRewardController.Instance.disable)
        {
            dailyRewardBtn.SetActive(true);
        }
    }

    public void ShowSettingsUI()
    {
        settingsUI.SetActive(true);
    }

    public void HideSettingsUI()
    {
        settingsUI.SetActive(false);
    }

    public void ShowStoreUI()
    {
        storeUI.SetActive(true);
    }

    public void HideStoreUI()
    {
        storeUI.SetActive(false);
    }

    public void WatchAdForCoins()
    {
        #if UNITY_EDITOR
        if (PremiumFeaturesManager.Instance.enablePremiumFeatures)
        {
            // Give the coins right away for testing the rewardUI
            // Hide the button
            watchForCoinsBtn.SetActive(false);
            //        ShowRewardUI(AdDisplayer.Instance.rewardedCoins);
            StartCoroutine(CRIncreaseCoins(50));    // TESTING: increase a random number of coins to test the UI in editor
        }
        #elif EASY_MOBILE
        // Hide the button
        watchForCoinsBtn.SetActive(false);

        AdDisplayer.CompleteRewardedAdToEarnCoins += OnCompleteRewardedAdToEarnCoins;
        AdDisplayer.Instance.ShowRewardedAdToEarnCoins();
        #endif
    }

    void OnCompleteRewardedAdToEarnCoins()
    {
        #if EASY_MOBILE
        // Unsubscribe
        AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToEarnCoins;

        // Give the coins!
//        ShowRewardUI(AdDisplayer.Instance.rewardedCoins);
        StartCoroutine(CRIncreaseCoins(AdDisplayer.Instance.rewardedCoins));

        #endif
    }

    public void GrabDailyReward()
    {
        if (DailyRewardController.Instance.CanRewardNow())
        {
            int reward = DailyRewardController.Instance.GetRandomReward();

            // Round the number and make it mutiplies of 5 only.
            int roundedReward = (reward / 5) * 5;

            // Show the reward UI
            ShowRewardUI(roundedReward);

            // Update next time for the reward
            DailyRewardController.Instance.ResetNextRewardTime();
        }
    }

    public void ShowRewardUI(int reward)
    {
        rewardUI.SetActive(true);
        rewardUI.GetComponent<RewardUIController>().Reward(reward);
    }

    public void HideRewardUI()
    {
        rewardUI.GetComponent<RewardUIController>().Close();
    }

    public void ShowLeaderboardUI()
    {
        #if EASY_MOBILE
        if (GameServices.IsInitialized())
        {
            GameServices.ShowLeaderboardUI();
        }
        else
        {
        #if UNITY_IOS
            NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
        #elif UNITY_ANDROID
            GameServices.Init();
        #endif
        }
        #endif
    }

    public void PurchaseRemoveAds()
    {
        #if EASY_MOBILE
        InAppPurchaser.Instance.Purchase(InAppPurchaser.Instance.removeAds);
        #endif
    }

    public void RestorePurchase()
    {
        #if EASY_MOBILE
        InAppPurchaser.Instance.RestorePurchase();
        #endif
    }

    public void ShowShareUI()
    {
        isShowShareUI = !isShowShareUI;
        shareUI.SetActive(isShowShareUI);
    }

    public void SetUpShareUI()
    {
        if (!ScreenshotSharer.Instance.disableSharing)
        {
            Texture2D texture = ScreenshotSharer.Instance.CapturedScreenshot;
            shareUIController.ImgTex = texture;

#if EASY_MOBILE
            AnimatedClip clip = ScreenshotSharer.Instance.RecordedClip;
            shareUIController.AnimClip = clip;
#endif

        }
    }

    public void HideShareUI()
    {
        shareUI.SetActive(false);
    }

    public void ShowCharacterSelectionScene()
    {
        SoundManager.Instance.StopMusic();
        SceneManager.LoadScene("CharacterSelection");
    }

    public void ToggleSound()
    {
        SoundManager.Instance.ToggleMute();
    }

    public void ToggleMusic()
    {
        SoundManager.Instance.ToggleMusic();
    }

    public void RateApp()
    {
        Utilities.RateApp();
    }

    public void OpenTwitterPage()
    {
        Utilities.OpenTwitterPage();
    }

    public void OpenFacebookPage()
    {
        Utilities.OpenFacebookPage();
    }

    public void ButtonClickSound()
    {
        Utilities.ButtonClickSound();
    }

    void UpdateSoundButtons()
    {
        if (SoundManager.Instance.IsMuted())
        {
            soundOnBtn.gameObject.SetActive(false);
            soundOffBtn.gameObject.SetActive(true);
        }
        else
        {
            soundOnBtn.gameObject.SetActive(true);
            soundOffBtn.gameObject.SetActive(false);
        }
    }

    void UpdateMusicButtons()
    {
        if (SoundManager.Instance.IsMusicOff())
        {
            musicOffBtn.gameObject.SetActive(true);
            musicOnBtn.gameObject.SetActive(false);
        }
        else
        {
            musicOffBtn.gameObject.SetActive(false);
            musicOnBtn.gameObject.SetActive(true);
        }
    }

    IEnumerator CRIncreaseCoins(int amount)
    {
        float playTime = 1f;
        float waitTime = playTime / (float)amount;
    
        for (int i = 0; i < amount; i++)
        {
            CoinManager.Instance.AddCoins(1);
    
            yield return new WaitForSeconds(waitTime);
        }
    
        ShowToastMsg("You have earn " + amount);
    }

    public void ShowToastMsg(string msg, float hideAfter = 4f)
    {
        toastMsg.transform.Find("Text").GetComponent<Text>().text = msg;
        toastMsg.SetActive(true);
        Invoke("HideToastMsg", hideAfter);
    }

    public void HideToastMsg()
    {
        toastMsg.SetActive(false);
    }

    bool IsPremiumFeaturesEnabled()
    {
        return PremiumFeaturesManager.Instance != null && PremiumFeaturesManager.Instance.enablePremiumFeatures;
    }
}
