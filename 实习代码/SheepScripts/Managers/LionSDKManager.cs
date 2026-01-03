// Define this to enable Lion SDK (only if SDK is properly installed)
#define ENABLE_LION_SDK

using System;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_LION_SDK && !UNITY_EDITOR
using LionStudios.Suite.Analytics;
// MaxSDK is available globally
#endif

namespace WoolyPath
{
    /// <summary>
    /// Lion SDK Manager - Centralized manager for Lion Analytics and Ads
    /// 管理Lion分析和广告的中心化管理器
    ///
    /// 使用说明：
    /// 1. 如果Lion SDK已经导入，取消文件第一行的注释
    /// 2. 如果出现编译错误，说明Lion SDK未正确导入
    /// 3. 在编辑器中会使用模拟模式，真机会使用真实SDK
    /// </summary>
    public class LionSDKManager : MonoBehaviour
    {
        // SDK Configuration
        // SDK 配置
        private const string LION_SDK_KEY = "68e8be398d03";
        private const string REWARDED_AD_UNIT_ID = "27576f1e829b799c";

        private static LionSDKManager _instance;
        public static LionSDKManager Instance
        {
            get
            {
                // Auto-create instance if it doesn't exist
                // 如果实例不存在则自动创建
                if (_instance == null)
                {
                    // Try to find existing instance
                    _instance = FindObjectOfType<LionSDKManager>();

                    // If still not found, create new one
                    if (_instance == null)
                    {
                        GameObject managerObject = new GameObject("LionSDKManager_AutoCreated");
                        _instance = managerObject.AddComponent<LionSDKManager>();
                        DontDestroyOnLoad(managerObject);
                        _instance.InitializeSDK();
                        Debug.Log("[LionSDKManager] Auto-created instance");
                    }
                }
                return _instance;
            }
            private set { _instance = value; }
        }

        // Mission attempt tracking - persisted across sessions
        // 任务尝试次数追踪 - 跨会话持久化
        private Dictionary<int, int> missionAttempts = new Dictionary<int, int>();
        private const string ATTEMPT_KEY_PREFIX = "WoolyPath_MissionAttempt_";

        // Rewarded ad completion callbacks
        // 激励广告完成回调
        private System.Action onRewardedAdCompleted;
        private System.Action onRewardedAdFailed;

        // Debug mode for testing
        // 调试模式用于测试
        [SerializeField] private bool debugMode = true;

        // Test mode - set to false for production
        // 测试模式 - 生产环境设为false
        [SerializeField] private bool useTestAds = true;

        private bool isInitialized = false;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSDK();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeSDK()
        {
            if (isInitialized) return;
            isInitialized = true;

            DebugLog("Initializing Lion SDK Manager...");

#if ENABLE_LION_SDK && !UNITY_EDITOR
            // Initialize Lion Analytics for real devices
            // 为真实设备初始化 Lion Analytics - 自动初始化
            DebugLog("Lion Analytics auto-initializes with the SDK");

            // Initialize MaxSDK for ads
            // 初始化 MaxSDK 广告
            try
            {
                MaxSdkCallbacks.OnSdkInitializedEvent += OnMaxSdkInitialized;
                MaxSdk.SetSdkKey(LION_SDK_KEY);
                MaxSdk.InitializeSdk();
                DebugLog("Max Ads SDK initializing...");

                // Set test device if in test mode
                // 如果在测试模式，设置测试设备
                if (useTestAds)
                {
                    DebugLog("Using test ads mode");
                    // MaxSdk.ShowMediationDebugger(); will be called after initialization
                }
            }
            catch (Exception e)
            {
                DebugLog($"Failed to initialize Max Ads: {e.Message}");
            }
#else
            DebugLog("Lion SDK not enabled or running in Editor - Using simulation mode");
            DebugLog("在编辑器中运行或Lion SDK未启用 - 使用模拟模式");
#endif

            // Load mission attempts from PlayerPrefs
            // 从 PlayerPrefs 加载任务尝试次数
            LoadMissionAttempts();
        }

#if ENABLE_LION_SDK && !UNITY_EDITOR
        private void OnMaxSdkInitialized(MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
            DebugLog("Max Ads SDK initialized successfully");

            // Show mediation debugger in test mode
            if (useTestAds)
            {
                MaxSdk.ShowMediationDebugger();
            }

            SetupAdCallbacks();
            // Load the first rewarded ad
            LoadRewardedAd();
        }

        private void SetupAdCallbacks()
        {
            // Setup rewarded ad callbacks using MaxSdkCallbacks
            // 设置激励广告回调
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHidden;
        }

        private void LoadRewardedAd()
        {
            MaxSdk.LoadRewardedAd(REWARDED_AD_UNIT_ID);
        }
#endif

        #region Analytics Events - 分析事件

        /// <summary>
        /// Log mission started event
        /// 记录任务开始事件
        /// </summary>
        public void LogMissionStarted(int levelIndex)
        {
            int attempt = GetMissionAttempt(levelIndex);

            var parameters = new Dictionary<string, object>
            {
                { "mission_type", "main" },
                { "mission_name", "main" },
                { "mission_id", levelIndex + 1 },
                { "mission_attempt", attempt }
            };

#if ENABLE_LION_SDK && !UNITY_EDITOR
            try
            {
                // Use LionAnalytics for progression events
                // MissionStarted(missionType, missionName, missionID, missionAttempt)
                LionAnalytics.MissionStarted("main", $"main_{levelIndex + 1}", levelIndex + 1, attempt);
            }
            catch (Exception e)
            {
                DebugLog($"Failed to log mission_started event: {e.Message}");
            }
#endif
            DebugLog($"Analytics: mission_started - Level {levelIndex + 1}, Attempt {attempt}");
        }

        /// <summary>
        /// Log mission step event - Only for specific Lion Analytics step types
        /// 记录任务步骤事件 - 仅用于特定的 Lion Analytics 步骤类型
        /// Valid step types: soft_fail, revive, ftue (first time user experience)
        /// </summary>
        public void LogMissionStep(int levelIndex, string stepName, object additionalData = null)
        {
            // Lion Analytics only accepts specific step_name formats
            // Only send mission_step for: soft_fail, revive, ftue
            bool isValidStepType = stepName.StartsWith("soft_fail") ||
                                   stepName.StartsWith("revive") ||
                                   stepName.StartsWith("ftue");

            if (!isValidStepType)
            {
                // For other game progress events, just log locally
                DebugLog($"Game Progress: Level {levelIndex + 1}, Step: {stepName}");
                return;
            }

            int attempt = GetMissionAttempt(levelIndex);

            var parameters = new Dictionary<string, object>
            {
                { "mission_type", "main" },
                { "mission_name", $"main_{levelIndex + 1}" },
                { "mission_id", levelIndex + 1 },
                { "mission_attempt", attempt },
                { "step_name", stepName }
            };

            if (additionalData != null)
            {
                parameters.Add("additional_data", additionalData);
            }

#if ENABLE_LION_SDK && !UNITY_EDITOR
            try
            {
                // Use LionAnalytics for progression events
                // MissionStep(missionType, missionName, missionID, userScore, missionAttempt, additionalData, reward, stepName)
                Dictionary<string, object> stepData = new Dictionary<string, object> { { "step", stepName } };
                LionAnalytics.MissionStep("main", $"main_{levelIndex + 1}", levelIndex + 1, null, attempt, stepData, null, stepName);
            }
            catch (Exception e)
            {
                DebugLog($"Failed to log mission_step event: {e.Message}");
            }
#endif
            DebugLog($"Analytics: mission_step - Level {levelIndex + 1}, Step: {stepName}");
        }

        /// <summary>
        /// Log mission completed event
        /// 记录任务完成事件
        /// </summary>
        public void LogMissionComplete(int levelIndex, int goldReward = 10)
        {
            int attempt = GetMissionAttempt(levelIndex);

            var parameters = new Dictionary<string, object>
            {
                { "mission_type", "main" },
                { "mission_name", $"main_{levelIndex + 1}" },
                { "mission_id", levelIndex + 1 },
                { "mission_attempt", attempt },
                { "reward", goldReward },
                { "rewardName", "gold" },
                { "rewardProducts", $"{goldReward} gold coins" }
            };

#if ENABLE_LION_SDK && !UNITY_EDITOR
            try
            {
                // Use LionAnalytics for progression events
                // MissionCompleted(missionType, missionName, missionID, userScore, missionAttempt, additionalData, reward)
                LionAnalytics.MissionCompleted("main", $"main_{levelIndex + 1}", levelIndex + 1, null, attempt);
            }
            catch (Exception e)
            {
                DebugLog($"Failed to log mission_complete event: {e.Message}");
            }
#endif
            DebugLog($"Analytics: mission_complete - Level {levelIndex + 1}, Attempt {attempt}");

            // Reset attempt count for this level after completion
            // 完成后重置此关卡的尝试次数
            ResetMissionAttempt(levelIndex);
        }

        /// <summary>
        /// Log mission abandoned event (when player quits from settings menu)
        /// 记录任务放弃事件（玩家从设置菜单退出时）
        /// NOTE: In Lion Analytics, this appears as mission_fail with fail_reason="abandoned"
        /// 注意：在 Lion Analytics 中，这显示为带有 fail_reason="abandoned" 的 mission_fail
        /// </summary>
        public void LogMissionAbandoned(int levelIndex)
        {
            // Add more detailed logging
            Debug.Log($"[LionSDKManager] LogMissionAbandoned called for level {levelIndex} - Player quit from settings");

            int attempt = GetMissionAttempt(levelIndex);

            var parameters = new Dictionary<string, object>
            {
                { "mission_type", "main" },
                { "mission_name", $"main_{levelIndex + 1}" },
                { "mission_id", levelIndex + 1 },
                { "mission_attempt", attempt },
                { "fail_reason", "abandoned" }
            };

#if ENABLE_LION_SDK && !UNITY_EDITOR
            try
            {
                // Lion Analytics uses MissionFailed with fail_reason="abandoned" for abandonment
                // 放弃 = MissionFailed with fail_reason="abandoned"
                // 失败 = MissionFailed with fail_reason="game_over" or other reasons
                Debug.Log($"[LionSDKManager] Sending MissionFailed with reason='abandoned' for level {levelIndex + 1}");
                LionAnalytics.MissionFailed("main", $"main_{levelIndex + 1}", levelIndex + 1, null, attempt, null, "abandoned");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LionSDKManager] Failed to log mission_abandoned event: {e.Message}");
                DebugLog($"Failed to log mission_abandoned event: {e.Message}");
            }
#endif
            DebugLog($"Analytics: mission_abandoned (quit from settings) - Level {levelIndex + 1}, Attempt {attempt}");

            // Increment attempt for next try
            // 为下次尝试增加尝试次数
            IncrementMissionAttempt(levelIndex);
        }

        /// <summary>
        /// Log mission failed event (when player loses due to game rules)
        /// 记录任务失败事件（玩家因游戏规则失败时）
        /// Common fail reasons: "timeout", "game_over", "no_moves", etc.
        /// 常见失败原因："timeout"（超时）, "game_over"（游戏结束）, "no_moves"（无法移动）等
        /// </summary>
        public void LogMissionFailed(int levelIndex, string failReason = "game_over")
        {
            int attempt = GetMissionAttempt(levelIndex);

            var parameters = new Dictionary<string, object>
            {
                { "mission_type", "main" },
                { "mission_name", $"main_{levelIndex + 1}" },
                { "mission_id", levelIndex + 1 },
                { "mission_attempt", attempt },
                { "fail_reason", failReason }
            };

#if ENABLE_LION_SDK && !UNITY_EDITOR
            try
            {
                // Use LionAnalytics for progression events
                // MissionFailed(missionType, missionName, missionID, userScore, missionAttempt, additionalData, failReason)
                LionAnalytics.MissionFailed("main", $"main_{levelIndex + 1}", levelIndex + 1, null, attempt, null, failReason);
            }
            catch (Exception e)
            {
                DebugLog($"Failed to log mission_fail event: {e.Message}");
            }
#endif
            DebugLog($"Analytics: mission_fail - Level {levelIndex + 1}, Reason: {failReason}");

            // Increment attempt for next try
            // 为下次尝试增加尝试次数
            IncrementMissionAttempt(levelIndex);
        }

        /// <summary>
        /// Log revive event when player uses revive/continue feature
        /// 记录玩家使用复活/继续功能时的事件
        /// </summary>
        public void LogReviveUsed(int levelIndex)
        {
            LogMissionStep(levelIndex, "revive_used");
        }

        /// <summary>
        /// Log soft fail when player fails but can continue
        /// 记录玩家失败但可以继续时的事件
        /// </summary>
        public void LogSoftFail(int levelIndex, string reason = "")
        {
            string stepName = string.IsNullOrEmpty(reason) ? "soft_fail" : $"soft_fail_{reason}";
            LogMissionStep(levelIndex, stepName);
        }

        /// <summary>
        /// Log FTUE (First Time User Experience) tutorial steps
        /// 记录首次用户体验教程步骤
        /// </summary>
        public void LogTutorialStep(int levelIndex, string tutorialStep)
        {
            LogMissionStep(levelIndex, $"ftue_{tutorialStep}");
        }

        #endregion

        #region Rewarded Ads - 激励广告

        /// <summary>
        /// Check if rewarded ad is available
        /// 检查激励广告是否可用
        /// </summary>
        public bool IsRewardedAdAvailable()
        {
#if UNITY_EDITOR
            // In editor, ad is always available for testing
            // 在编辑器中，广告总是可用以便测试
            return true;
#elif ENABLE_LION_SDK
            try
            {
                return MaxSdk.IsRewardedAdReady(REWARDED_AD_UNIT_ID);
            }
            catch (Exception e)
            {
                DebugLog($"Failed to check rewarded ad availability: {e.Message}");
                return false;
            }
#else
            // Lion SDK not enabled on device - no ads available
            // Lion SDK 在设备上未启用 - 无广告可用
            DebugLog("Lion SDK not enabled - ads not available on device");
            return false;
#endif
        }

        /// <summary>
        /// Show rewarded ad with callbacks
        /// 显示激励广告并设置回调
        /// </summary>
        public void ShowRewardedAd(System.Action onSuccess, System.Action onFailed)
        {
            if (!IsRewardedAdAvailable())
            {
                DebugLog("Rewarded ad not available");
                onFailed?.Invoke();
                return;
            }

            onRewardedAdCompleted = onSuccess;
            onRewardedAdFailed = onFailed;

#if UNITY_EDITOR
            // Simulate ad in editor
            // 在编辑器中模拟广告
            DebugLog("Simulating rewarded ad (Editor mode)");
            StartCoroutine(SimulateRewardedAd());
#elif ENABLE_LION_SDK
            try
            {
                MaxSdk.ShowRewardedAd(REWARDED_AD_UNIT_ID);
                DebugLog("Showing rewarded ad");
            }
            catch (Exception e)
            {
                DebugLog($"Failed to show rewarded ad: {e.Message}");
                onFailed?.Invoke();
                ClearAdCallbacks();
            }
#else
            // Lion SDK not enabled on device - fail the ad request
            // Lion SDK 在设备上未启用 - 广告请求失败
            DebugLog("Lion SDK not enabled - cannot show ads on device");
            onFailed?.Invoke();
            ClearAdCallbacks();
#endif
        }

#if UNITY_EDITOR
        private System.Collections.IEnumerator SimulateRewardedAd()
        {
            DebugLog("Simulated ad started - 模拟广告开始");
            yield return new WaitForSeconds(1f); // Simulate short ad duration for testing

            // In editor, always succeed for easy testing
            // 在编辑器中，总是成功以便于测试
            DebugLog("Simulated ad completed successfully - 模拟广告成功完成");
            onRewardedAdCompleted?.Invoke();
            ClearAdCallbacks();
        }
#endif

#if ENABLE_LION_SDK && !UNITY_EDITOR
        #region Ad Callbacks - 广告回调

        private void OnRewardedAdLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog($"Rewarded ad loaded - Network: {adInfo.NetworkName}");
        }

        private void OnRewardedAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            DebugLog($"Rewarded ad load failed: {errorInfo.Message} (Code: {errorInfo.Code})");
        }

        private void OnRewardedAdDisplayed(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog($"Rewarded ad displayed - Network: {adInfo.NetworkName}");
        }

        private void OnRewardedAdDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog($"Rewarded ad display failed: {errorInfo.Message} (Code: {errorInfo.Code})");
            onRewardedAdFailed?.Invoke();
            ClearAdCallbacks();
        }

        private void OnRewardedAdReceivedReward(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog($"Rewarded ad completed - Reward: {reward.Amount} {reward.Label}, Network: {adInfo.NetworkName}");
            onRewardedAdCompleted?.Invoke();
            ClearAdCallbacks();
        }

        private void OnRewardedAdHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugLog($"Rewarded ad hidden - Network: {adInfo.NetworkName}");
            // Load next ad
            LoadRewardedAd();
        }

        #endregion
#endif

        private void ClearAdCallbacks()
        {
            onRewardedAdCompleted = null;
            onRewardedAdFailed = null;
        }

        #endregion

        #region Mission Attempt Management - 任务尝试管理

        private int GetMissionAttempt(int levelIndex)
        {
            if (!missionAttempts.ContainsKey(levelIndex))
            {
                // Load from PlayerPrefs if not in memory
                int savedAttempt = PlayerPrefs.GetInt(ATTEMPT_KEY_PREFIX + levelIndex, 1);
                missionAttempts[levelIndex] = savedAttempt;
            }
            return missionAttempts[levelIndex];
        }

        private void IncrementMissionAttempt(int levelIndex)
        {
            int currentAttempt = GetMissionAttempt(levelIndex);
            missionAttempts[levelIndex] = currentAttempt + 1;
            SaveMissionAttempt(levelIndex, currentAttempt + 1);
        }

        private void ResetMissionAttempt(int levelIndex)
        {
            missionAttempts[levelIndex] = 1;
            SaveMissionAttempt(levelIndex, 1);
        }

        private void SaveMissionAttempt(int levelIndex, int attempt)
        {
            PlayerPrefs.SetInt(ATTEMPT_KEY_PREFIX + levelIndex, attempt);
            PlayerPrefs.Save();
        }

        private void LoadMissionAttempts()
        {
            // Load attempts for first 50 levels (can be adjusted)
            // 加载前50关的尝试次数（可调整）
            for (int i = 0; i < 50; i++)
            {
                int savedAttempt = PlayerPrefs.GetInt(ATTEMPT_KEY_PREFIX + i, 1);
                if (savedAttempt > 1)
                {
                    missionAttempts[i] = savedAttempt;
                }
            }
            DebugLog("Mission attempts loaded from PlayerPrefs");
        }

        #endregion

        #region Debug - 调试

        private void DebugLog(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[LionSDKManager] {message}");
            }
        }

        #endregion
    }
}