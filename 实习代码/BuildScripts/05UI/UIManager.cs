using LionStudios.Suite.Ads;
using LionStudios.Suite.Analytics;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

namespace ConnectMaster
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        #region 序列化UI引用（在Inspector中拖入对应面板/组件）
        [Tooltip("提示数量")]
        public int hintnum = 4;
        
        [Header("基础面板")]
        [Tooltip("暂停面板")]
        [SerializeField] private GameObject pausePanel;
        [Tooltip("通关面板")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject OtherwinPanel;
        [Tooltip("失败面板")]
        [SerializeField] private GameObject losePanel;
        [Tooltip("加载面板")]
        [SerializeField] private GameObject loadPanel;
        [Tooltip("HUD面板")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject GoldCoinsPanel;
        [Tooltip("提示面板")]
        [SerializeField] private GameObject hintPanel1;
        [SerializeField] private GameObject hintPanel2;
        [Tooltip("游玩面板")]
        [SerializeField] private GameObject gameplay;

        [Header("功能按钮")]
        [Header("暂停显示的按钮")]
        [SerializeField] private Button retryButtonInPasue; //重试
        [SerializeField] private Button continueInPasue; //继续
        [SerializeField] private Toggle bgmToggle;//背景乐
        [SerializeField] private Toggle sfxToggle;//效果乐
        [SerializeField] private Toggle vibrationToggle;//振动
        public Color _normalColor = Color.white;
        public Color _grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 不透明灰色

        [Header("通关显示的按钮")]
        [SerializeField] private Button winToNextLevel; // 下一关
        [SerializeField] private Button OtherwinToNextLevel; // 下一关
        [SerializeField] public GameObject Rainbow; // 特效

        [Header("失败显示的按钮")]
        [SerializeField] private Button addTimeButtonInLose; //加时
        [SerializeField] private Button useAdvertisingreaddTimeButton; //广告加时
        [SerializeField] private Button retryButtonInLose; //重试
        
        [Header("提示显示1的按钮")]
        [SerializeField] private Button useHint1Button; // 提示1
        [SerializeField] private Button useAdvertisingHint1Button; // 广告提示1
        [SerializeField] private Button exitHint1Button; //退出提示1

        [Header("提示显示2的按钮")]
        [SerializeField] private Button useHint2Button; // 提示2
        [SerializeField] private Button useAdvertisingHint2Button; // 广告提示2
        [SerializeField] private Button exitHint2Button; //退出提示2

        [Header("HUD显示的按钮")]
        [SerializeField] private Button pauseButton;//暂停按钮
        [SerializeField] private Button hint1Button;//提示1按钮
        [SerializeField] private Button hint2Button;//提示2按钮
        [SerializeField] private Button AddCoinButton;//加钱(测试用)
      

        [Header("信息组件")]
        [Header("HUD中显示的信息")]
        [SerializeField] private TMP_Text timeText; // 显示剩余时间的文本（00:00格式）
        [SerializeField] private TMP_Text levelText;//当前关卡数
        [SerializeField] private TMP_Text goldCoins;//当前金币数
        [SerializeField] private TMP_Text ObjectiveRows;//关卡通关目标数
        [SerializeField] private Image Progressbar;//进度条

        [Header("倒计时缩放动画（最后20秒）")]
        [SerializeField] private float TimeScaleAmplitude = 0.1f; // 缩放幅度（1 + 0.3 = 最大1.3倍）
        [SerializeField] private float TimeScaleFrequency = 1.5f; // 缩放频率（每秒2次放大缩小循环）
        private RectTransform timeTextRect; // 时间文本的RectTransform（用于缩放）
        public bool isWarning = false;//本关是否警告过
        #endregion

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            InitSettingPanel();//初始化设置里的开关

        }

        private void Start()
        {
            GameStateManager.Instance.StartCountdown();
            // 绑定按钮点击事件
            BindButtonEvents();
            // 订阅游戏状态和倒计时事件
            SubscribeToEvents();
            // 初始化面板状态
            ResetAllPanels();
            if (timeText != null)
            {
                timeTextRect = timeText.GetComponent<RectTransform>();
                // 初始化缩放为1（防止异常）
                if (timeTextRect != null)
                    timeTextRect.localScale = Vector3.one;
            }
            //RedImage.enabled = false;

        }
        private void Update()
        {
            UpdateGoldCoin();//更新金币数
            UpdatelevelObjectiveRows(); //更新关卡组数
            UpdateLevelText();//更新当前的关卡数
            UpdateAdvertisingIcon();//更新广告按钮
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents(); // 取消事件订阅，避免内存泄漏
        }
        #endregion

        #region 事件订阅/取消
        private void SubscribeToEvents()
        {
            GameStateManager.Instance.OnCountdownUpdated += UpdateCountdownDisplay;
            GameStateManager.Instance.OnCountdownEnd += OnCountdownEnded;
        }
        private void UnsubscribeFromEvents()
        {
            GameStateManager.Instance.OnCountdownUpdated -= UpdateCountdownDisplay;
            GameStateManager.Instance.OnCountdownEnd -= OnCountdownEnded;
        }
        #endregion

        #region --按钮点击事件绑定--
        private void BindButtonEvents()
        {
            #region 暂停显示的按钮
            retryButtonInPasue?.onClick.AddListener(OnRetryButtonInPasueClicked);//重试
            continueInPasue?.onClick.AddListener(OnContinueInPasueClicked);//退出暂停面板
            // 绑定Toggle事件
            bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);//音乐
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);//效果乐
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);//振动
            #endregion

            #region 通关显示的按钮
            winToNextLevel?.onClick.AddListener(OnWinToNextLevelClicked);//下一关
            OtherwinToNextLevel?.onClick.AddListener(OnOtherwinToNextLevelClicked);//下一关
            #endregion

            #region 失败显示的按钮
            addTimeButtonInLose?.onClick.AddListener(OnaddTimeButtonInLoseClicked);//加时
            useAdvertisingreaddTimeButton?.onClick.AddListener(OnuseAdvertisingreaddTimeButtonClicked);//广告加时
            retryButtonInLose?.onClick.AddListener(OnRetryButtonInLoseClicked);//重试
            #endregion

            #region HUD显示的按钮
            pauseButton?.onClick.AddListener(OnPauseButtonClicked);//打开暂停面板
            hint1Button?.onClick.AddListener(OnHint1ButtonClicked);//打开提示1面板
            hint2Button?.onClick.AddListener(OnHint2ButtonClicked);//打开提示2面板
            AddCoinButton?.onClick.AddListener(OnAddCoinButtonClicked);//测试加金币
            #endregion

            #region 提示显示的按钮
            useHint1Button?.onClick.AddListener(OnuseHint1ButtonClicked);//使用提示1
            useAdvertisingHint1Button?.onClick.AddListener(OnuseAdvertisingHint1ButtonClicked);//使用广告提示1
            exitHint1Button?.onClick.AddListener(OnexitHint1ButtonClicked);//退出提示1面板

            useHint2Button?.onClick.AddListener(OnuseHint2ButtonClicked);//使用提示2
            useAdvertisingHint2Button?.onClick.AddListener(OnuseAdvertisingHint2ButtonClicked);//使用广告提示2
            exitHint2Button?.onClick.AddListener(OnexitHint2ButtonClicked);//退出提示2面板
            #endregion
        }

        #endregion

        #region --按钮功能实现--

        #region 暂停面板功能实现

        #region Toggle相关
        private void InitSettingPanel()
        {
            // 默认全开启
            bgmToggle.isOn = true;
            sfxToggle.isOn = true;
            vibrationToggle.isOn = true;
            // 初始化时设置Toggle图标颜色
            UpdateToggleColor(bgmToggle, bgmToggle.isOn);
            UpdateToggleColor(sfxToggle, sfxToggle.isOn);
            UpdateToggleColor(vibrationToggle, vibrationToggle.isOn);
        }

        // BGM开关回调
        private void OnBgmToggleChanged(bool isOn)
        {
            SoundManager.Instance.IsBgmEnabled = isOn;
            SoundManager.Instance.SyncBgmState();
            UpdateToggleColor(bgmToggle, isOn); // 更新图标颜色
            Debug.Log($"[设置面板] BGM开关状态变更为：{(isOn ? "开启" : "关闭")}");
        }

        // 音效开关回调
        private void OnSfxToggleChanged(bool isOn)
        {
            SoundManager.Instance.IsSfxEnabled = isOn;
            UpdateToggleColor(sfxToggle, isOn); // 更新图标颜色
            Debug.Log($"[设置面板] 音效开关状态变更为：{(isOn ? "开启" : "关闭")}");
        }

        // 震动开关回调
        private void OnVibrationToggleChanged(bool isOn)
        {
            VibrationManager.IsVibrationEnabled = isOn;
            UpdateToggleColor(vibrationToggle, isOn); //更新图标颜色
            Debug.Log($"[设置面板] 震动开关状态变更为：{(isOn ? "开启" : "关闭")}");
        }
        // 通用方法 - 更新Toggle的图标颜色
        private void UpdateToggleColor(Toggle toggle, bool isOn)
        {
            // 获取Toggle下所有的Image组件（包括背景和勾选标记）
            Image[] toggleImages = toggle.GetComponentsInChildren<Image>(true);
            foreach (Image img in toggleImages)
            {
                // 开启=正常颜色，关闭=灰色
                img.color = isOn ? _normalColor : _grayColor;
            }
        }
        #endregion

        private void OnRetryButtonInPasueClicked()
        {
            if(ItemGenerate.Instance.IsAnimating)
            {
                return;
            }
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            LevelManager.Instance.LoadCurrentLevel();
            SetPausePanel(false);
            // 重试时展示加载界面
            NewLoading();
            SoundManager.Instance.PlaySound("34");

            //---------数据检测别删------------
            //放弃重启
            LionAnalytics.MissionAbandoned(
            missionType: "main",
            missionName: $"main_level_{LevelManager.Instance.currentLevelIndex + 1}",
            missionID: LevelManager.Instance.currentLevelIndex + 1,
            missionAttempt: 1,
            additionalData: null,
            isGamePlay: true
            );
            //---------数据检测别删------------

        }
        private void OnExitToMainInPasueClicked()
        {
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            
            SoundManager.Instance.PlaySound("34");
            //InputManager.Instance.SetInput(true);

            //---------数据检测别删------------
            //var levelNumber = LevelManager.Instance.CurrentLevelIndex + 1;
            //LionAnalytics.MissionAbandoned(
            // missionType: "main",
            //missionName: $"main_level_{levelNumber}",
            //missionID: levelNumber,
            //missionAttempt: null,
            //additionalData: null,
            //isGamePlay: null
            //);
            //---------数据检测别删------------
        }
        private void OnContinueInPasueClicked()
        {
            SetPausePanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
         
        }
        #endregion

        #region 通关面板功能实现
        private void OnWinToNextLevelClicked()
        {
            // 加载前强制重置通关状态和计数
            LevelManager.Instance.isLevelCompleted = false;
            LevelManager.Instance.TargetRows = 0;
            LevelManager.Instance.HasPairRows = 0;
            //恢复时间
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            //关卡加载
            LevelManager.Instance.currentLevelIndex++;
            LevelManager.Instance.LoadCurrentLevel();
            //面板显现
            SetWinPanel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            //金币增加
            GoldCoinManager.Instance.AddFixedGold(50);
            //音效
            SoundManager.Instance.PlaySound("34");
        }
        private void OnOtherwinToNextLevelClicked()
        {
            //停止旋转
            LevelManager.Instance._houseControl.StopShake();
            // 加载前强制重置通关状态和计数
            LevelManager.Instance.isLevelCompleted = false;
            LevelManager.Instance.TargetRows = 0;
            LevelManager.Instance.HasPairRows = 0;
            //恢复时间
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            //关卡加载
            LevelManager.Instance.currentLevelIndex++;
            LevelManager.Instance.LoadCurrentLevel();
            //面板显现
            SetOtherWinPanel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            SetGamePlayPanel(true);//重点
            //金币增加
            GoldCoinManager.Instance.AddFixedGold(50);
            //音效
            SoundManager.Instance.PlaySound("34");
        }
        

        #endregion

        #region 失败面板功能实现
        //加时
        private void OnaddTimeButtonInLoseClicked()
        {
            //扣钱
            GoldCoinManager.Instance.CostFixedGold(250);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            GameStateManager.Instance.RestartCountdown(60);
            SetLosePanel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            SoundManager.Instance.PlaySound("34");

            LionAnalytics.MissionStep(
                missionType: "main",
                missionName: $"main_level_{LevelManager.Instance.currentLevelIndex + 1}",
                missionID: LevelManager.Instance.currentLevelIndex + 1,
                missionAttempt:1, 
                stepName: "revive", 
                isGamePlay: true, 
                additionalData: null 
            );
        }
        //广告加时
        private void OnuseAdvertisingreaddTimeButtonClicked()
        {
            var additionalData = new Dictionary<string, object>
            {
                ["mission_data"] = new Dictionary<string, object>
                {
                    ["mission_type"] = "main", // 必须是"main"（字符串）
                    ["mission_name"] = $"main_level_{LevelManager.Instance.currentLevelIndex + 1}", // 必须以"main_"开头（字符串）
                    ["mission_id"] = LevelManager.Instance.currentLevelIndex + 1, // 必须是≥1的整数（比如当前关卡是1，这里就是1）
                    ["mission_attempt"] = 1 // 必须是≥1的整数（先填1，后续可根据重试次数递增）
                }
            };
            bool isAdShown = LionAds.TryShowRewarded(
             placement: "5102d43ffb984675", // 这里是广告位ID，不是ad_unit（别和ad_info搞混）
            onRewarded: OnAddTimeRewardReceived,
            onClosed: () => { },
             reward: new LionStudios.Suite.Analytics.Reward("time_extension", "game_resource", 60),
            additionalData: additionalData // 只传包含mission_data的字典
        );
            // 金币不足：尝试显示激励广告（仅触发广告，不立即变更状态）
            //bool isAdShown = LionAds.TryShowRewarded("5102d43ffb984675", OnAddTimeRewardReceived, OnAddTimeAdFailed);



            // 若广告成功触发显示，暂停游戏、保持当前面板状态（避免遮挡广告）
            if (isAdShown)
            {
                SoundManager.Instance.PlaySound("34"); // 仅播放广告触发音效
                SoundManager.Instance.ToggleBGM(false);
            }
            else
            {
                OnAddTimeAdFailed();
                
            }
        }

             // 广告播放成功回调
        private void OnAddTimeRewardReceived()
        {
            Debug.Log("广告激励1回调触发！准备关闭提示面板"); // 加日志
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            //-------加时-------
            GameStateManager.Instance.RestartCountdown(60);
            //-------加时-------
            SetLosePanel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            SoundManager.Instance.PlaySound("34");
            SoundManager.Instance.ToggleBGM(false);

            LionAnalytics.MissionStep(
                missionType: "main",
                missionName: $"main_level_{LevelManager.Instance.currentLevelIndex + 1}",
                missionID: LevelManager.Instance.currentLevelIndex + 1,
                missionAttempt: 1,
                stepName: "revive",
                isGamePlay: true,
                additionalData: null
                );
        }

        // 广告播放失败回调
        private void OnAddTimeAdFailed()
        {
            Debug.Log("广告播放失败/被跳过！");
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            
            SetLosePanel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            SoundManager.Instance.PlaySound("34");
            SoundManager.Instance.ToggleBGM(true);
        }
        
        //重试
        private void OnRetryButtonInLoseClicked()
        {
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            LevelManager.Instance.LoadCurrentLevel();
            SetLosePanel(false);
            SetGoldCoinsPanel(false);
            SoundManager.Instance.PlaySound("34");

            //---------数据检测别删------------
            //失败
            LionAnalytics.MissionFailed(
            missionType: "main",
            missionName: $"main_level_{LevelManager.Instance.currentLevelIndex+1}",
            missionID: LevelManager.Instance.currentLevelIndex+1,
            missionAttempt: 1,
            additionalData: null,
            failReason: "time_out", // 失败原因示例：时间耗尽
            isGamePlay: true
            );
            //---------数据检测别删------------
        }
   
        private void OnCountdownEnded()
        {
            // 停止倒计时，暂停游戏
            LevelManager.Instance.OnLevelLose();
            GameStateManager.Instance.StopCountdown();

            // 隐藏HUD，显示失败面板
            SetHudPanel(false);
            SetLosePanel(true);
            SetGoldCoinsPanel(true);

        }
        #endregion

        #region HUD功能实现
        private void OnPauseButtonClicked()
        {
            GameStateManager.Instance.StopCountdown();
            SetHudPanel(false);
            SetPausePanel(true);
            SoundManager.Instance.PlaySound("34");
        }
        private void OnHint1ButtonClicked()
        {

           GameStateManager.Instance.StopCountdown();
            SetHudPanel(false);
            SetGoldCoinsPanel(true);
            SetHint1Panel(true);
            SoundManager.Instance.PlaySound("34");
        }
        private void OnHint2ButtonClicked()
        {

            GameStateManager.Instance.StopCountdown();
            SetHudPanel(false);
            SetGoldCoinsPanel(true);
            SetHint2Panel(true);
            SoundManager.Instance.PlaySound("34");
        }
        private void OnAddCoinButtonClicked()
        {
            GoldCoinManager.Instance.AddFixedGold(1000);
            SoundManager.Instance.PlaySound("34");
        }

        #endregion

        #region 提示功能实现
        //金币提示一
        private void OnuseHint1ButtonClicked()
        {
            //扣钱
            GoldCoinManager.Instance.CostFixedGold(300);
            HintManager.Instance.HintKeywords();
            SetHint1Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");

            
        }

        #region 广告提示一
        //广告提示一
        private void OnuseAdvertisingHint1ButtonClicked()
        {
            // 构造仅包含mission_data的参数
            var additionalData = new Dictionary<string, object>
            {
                ["mission_data"] = new Dictionary<string, object>
                {
                    ["mission_type"] = "main",
                    ["mission_name"] = $"main_level_{LevelManager.Instance.currentLevelIndex + 1}",
                    ["mission_id"] = LevelManager.Instance.currentLevelIndex + 1,
                    ["mission_attempt"] = 1
                }
            };

            bool isAdShown = LionAds.TryShowRewarded(
            placement: "5102d43ffb984675",
            onRewarded: OnHint1RewardReceived,
            onClosed: () => { },
            reward: new LionStudios.Suite.Analytics.Reward("hint1", "game_resource", 1),
            additionalData: additionalData
    );
            // 金币不足：尝试显示激励广告（仅触发广告，不立即变更状态）
            // bool isAdShown = LionAds.TryShowRewarded("5102d43ffb984675", OnHint1RewardReceived, OnHint1AdFailed);


            // 若广告成功触发显示，暂停游戏、保持当前面板状态（避免遮挡广告）
            if (isAdShown)
            {
                SoundManager.Instance.PlaySound("34"); // 仅播放广告触发音效
                SoundManager.Instance.ToggleBGM(false);
            }
            else
            {
                OnHint1AdFailed();
            }
        }
        // 广告播放成功回调
        private void OnHint1RewardReceived()
        {
            Debug.Log("广告激励1回调触发！准备关闭提示面板"); // 加日志
            //-------提示1-------
            HintManager.Instance.HintKeywords();
            //-------提示1-------
            SetHint1Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
            SoundManager.Instance.ToggleBGM(false);
        }

        // 广告播放失败回调
        private void OnHint1AdFailed()
        {
            Debug.Log("广告播放失败/被跳过！");
            SetHint1Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
            SoundManager.Instance.ToggleBGM(true);
        }
        #endregion

           //退出提示一
        private void OnexitHint1ButtonClicked()
        {
            SetHint1Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
        }

        //金币提示二
        private void OnuseHint2ButtonClicked()
        {
            //扣钱
            GoldCoinManager.Instance.CostFixedGold(300);
            //只提示两个
            HintManager.Instance.HintCustomItems(hintnum); //HintManager.Instance.HintItem();
            SetHint2Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");

            
        }

        #region 广告提示二
        //广告提示二
        private void OnuseAdvertisingHint2ButtonClicked()
        {
            // 构造仅包含mission_data的参数
            var additionalData = new Dictionary<string, object>
            {
                ["mission_data"] = new Dictionary<string, object>
                {
                    ["mission_type"] = "main",
                    ["mission_name"] = $"main_level_{LevelManager.Instance.currentLevelIndex + 1}",
                    ["mission_id"] = LevelManager.Instance.currentLevelIndex + 1,
                    ["mission_attempt"] = 1
                }
            };
            bool isAdShown = LionAds.TryShowRewarded(
            placement: "5102d43ffb984675",
            onRewarded: OnHint2RewardReceived,
            onClosed: () => { },
            reward: new LionStudios.Suite.Analytics.Reward("hint2", "game_resource", 1),
            additionalData: additionalData
            );

            // 金币不足：尝试显示激励广告（仅触发广告，不立即变更状态）
            // bool isAdShown = LionAds.TryShowRewarded("5102d43ffb984675", OnHint2RewardReceived, OnHint2AdFailed);

            // 若广告成功触发显示，暂停游戏、保持当前面板状态（避免遮挡广告）
            if (isAdShown)
            {
                SoundManager.Instance.PlaySound("34"); // 仅播放广告触发音效
                SoundManager.Instance.ToggleBGM(false);
            }
            else
            {
                OnHint2AdFailed();
            }
        }
              // 广告播放成功回调
        private void OnHint2RewardReceived()
        {
            Debug.Log("广告激励2回调触发！准备关闭提示面板"); // 加日志
            //-------提示2-------
            HintManager.Instance.HintCustomItems(hintnum);
            //-------提示2-------
            SetHint2Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
            SoundManager.Instance.ToggleBGM(false);
        }

        // 广告播放失败回调
        private void OnHint2AdFailed()
        {
            Debug.Log("广告播放失败/被跳过！");
            SetHint2Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
            SoundManager.Instance.ToggleBGM(true);
        }
        #endregion

        //退出提示二
        private void OnexitHint2ButtonClicked()
        {
            SetHint2Panel(false);
            SetGoldCoinsPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeCountdown();
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
        }

        #endregion

        #region 加载界面功能实现
        public void NewLoading()
        {
            SetLoadPanel(true);
            LoadingMenu.Instance.StartLoading();
        }

        #endregion

        #endregion

        #region --面板显示与隐藏--
        public void ResetAllPanels()
        {
            gameplay?.SetActive(true);
            pausePanel?.SetActive(false);
            winPanel?.SetActive(false);
            OtherwinPanel?.SetActive(false);
            losePanel?.SetActive(false);
            loadPanel?.SetActive(false);
            hintPanel1?.SetActive(false);
            hintPanel2?.SetActive(false);
            GoldCoinsPanel?.SetActive(false);

        }
        public void SetGamePlayPanel(bool enable)
        {
            gameplay?.SetActive(enable);
        }
        public void SetPausePanel(bool active)
        {
            pausePanel?.SetActive(active);
        }

        public void SetWinPanel(bool active)
        {
            winPanel?.SetActive(active);
        }
        public void SetOtherWinPanel(bool active)
        {
            OtherwinPanel?.SetActive(active);
        }

        public void SetLosePanel(bool active)
        {
            losePanel?.SetActive(active);
        }
        public void SetLoadPanel(bool active)
        {
            loadPanel?.SetActive(active);
        }

        public void SetHudPanel(bool active)
        {
            hudPanel?.SetActive(active);
        }
        public void SetHint1Panel(bool active)
        {
            hintPanel1?.SetActive(active);
        }
        public void SetHint2Panel(bool active)
        {
            hintPanel2?.SetActive(active);
        }
        public void SetGoldCoinsPanel(bool active)
        {
            GoldCoinsPanel?.SetActive(active);
        }
    
        #endregion

        #region 常态UI更新
        //广告图标
        private void UpdateAdvertisingIcon()
        {
            //复活
            if (GoldCoinManager.Instance.GoldCoin <= 250)
            {
                addTimeButtonInLose.gameObject.SetActive(false);
                useAdvertisingreaddTimeButton.gameObject.SetActive(true);
            }
            else
            {
                addTimeButtonInLose.gameObject.SetActive(true);
                useAdvertisingreaddTimeButton.gameObject.SetActive(false);
            }
            //提示1
            if (GoldCoinManager.Instance.GoldCoin <= 300)
            {
                useHint1Button.gameObject.SetActive(false);
                useAdvertisingHint1Button.gameObject.SetActive(true);
            }
            else
            {
                useHint1Button.gameObject.SetActive(true);
                useAdvertisingHint1Button.gameObject.SetActive(false);
            }
            //提示2
            if (GoldCoinManager.Instance.GoldCoin <= 300)
            {
                useHint2Button.gameObject.SetActive(false);
                useAdvertisingHint2Button.gameObject.SetActive(true);
            }
            else
            {
                useHint2Button.gameObject.SetActive(true);
                useAdvertisingHint2Button.gameObject.SetActive(false);
            }
        }
        //关卡
        public void UpdateLevelText()
        {
            if (levelText != null)
            {
                levelText.text = $"Level {LevelManager.Instance.currentLevelIndex + 1}";
            }
        }
        //金币数
        private void UpdateGoldCoin()
        {
            if (goldCoins != null)
            {
                goldCoins.text = $" {GoldCoinManager.Instance.GoldCoin}";
            }
        }
        //更新关卡配对组数与进度条
        public void UpdatelevelObjectiveRows()
        {
            if (ObjectiveRows != null)
            {
                ObjectiveRows.text = $" {LevelManager.Instance.HasPairRows} /{ LevelManager.Instance.TargetRows}";//关卡通关目标数
               
            }
            if (Progressbar != null && LevelManager.Instance != null)
            {
                // 避免除以0的异常（目标行数为0时进度设为0）
                if (LevelManager.Instance.TargetRows <= 0)
                {
                    Progressbar.fillAmount = 0f;
                    return;
                }

                // 计算进度比例：已配对行数 / 目标行数（自动限制在0~1之间）
                float progress = (float)LevelManager.Instance.HasPairRows / LevelManager.Instance.TargetRows;

                // 赋值给进度条（fillAmount范围是0~1，超过会自动截取）
                Progressbar.fillAmount = Mathf.Clamp01(progress);
            }

        }
        // 更新倒计时显示为00:00格式
        private void UpdateCountdownDisplay(float remainingTime)
        {
            if (timeText == null) return;

            // 转换为整数秒（向上取整，确保最后1秒完整显示）
            int totalSeconds = Mathf.CeilToInt(remainingTime);
            // 计算分钟和秒数
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            // 格式化为00:00（不足两位补0）
            timeText.text = $"{minutes:D2}:{seconds:D2}";
            // 2.最后20秒触发缩放动画
            if (remainingTime <= 20f && remainingTime > 0f)
            {
                //RedImage.enabled = true;
                // 正弦函数计算缩放因子：1 + 幅度×正弦（时间×频率×2π），实现循环放大缩小
                float scaleFactor = 1f + TimeScaleAmplitude * Mathf.Sin(Time.time * TimeScaleFrequency * Mathf.PI * 2f);
                // 应用缩放到文本
                timeTextRect.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
            }
            else
            {
                // 非最后20秒：重置缩放为1（避免残留放大状态）
                timeTextRect.localScale = Vector3.one;
            }
            if (remainingTime <= 10f && remainingTime > 0f)
            {
                timeText.color = Color.red;
            }
            else
            {
                timeText.color = Color.white;
            }
            if (remainingTime <= 30f && remainingTime > 0f && !isWarning)
            {
                isWarning = true;
                SoundManager.Instance.PlaySound("6");
            }
        }
        #endregion

    }

}
