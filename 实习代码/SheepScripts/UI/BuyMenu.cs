
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WoolyPath
{
    public enum Props
    {
        None,
        SwapProp,
        RefreshProp,
        CatchProp
    }

    public delegate void PropChangedEventHandler(Props newProp);

    public class BuyMenu : MonoBehaviour
    {
        public static BuyMenu Instance { get; private set; }
        public Button ExitButton;
        public Button SwapBuyButton;
        public Button RefreshBuyButton;
        public Button CatchBuyButton;

        // Rewarded Ad Buttons - 激励广告按钮
        public Button SwapAdButton;
        public Button RefreshAdButton;
        public Button CatchAdButton;

        public GameObject Swap;
        public GameObject Refresh;
        public GameObject Catch;

        public TMP_Text goldCoinText;
        public Button AddGoldCoin;

        public Props CurrentProp { get; private set; } = Props.None;
        public bool IsInitialized { get; private set; } = false;

        public event PropChangedEventHandler OnPropChanged;

        private Dictionary<Props, GameObject> PropsMapping = new Dictionary<Props, GameObject>();

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

            InitializePropsMapping();

          
            OnPropChanged -= ShowProps;
            OnPropChanged += ShowProps;
        }

        private void Start()
        {
            
            BindButtonEvents();
            UpdateGoldCoin();
        }

        public void ForceInitialize()
        {
            if (!IsInitialized)
            {
                HideAllProps();
                IsInitialized = true;
            }
        }

        private void Update()
        {
            UpdateGoldCoin();
        }

        private void InitializePropsMapping()
        {
            PropsMapping.Clear();
            PropsMapping.Add(Props.SwapProp, Swap);
            PropsMapping.Add(Props.RefreshProp, Refresh);
            PropsMapping.Add(Props.CatchProp, Catch);
        }

        private void BindButtonEvents()
        {
            if (ExitButton != null)
                ExitButton.onClick.AddListener(OnExitButtonClicked);

            if (AddGoldCoin != null)
                AddGoldCoin.onClick.AddListener(OnAddGoldCoinClick);

            if (SwapBuyButton != null)
                SwapBuyButton.onClick.AddListener(OnSwapBuyButtonClick);

            if (RefreshBuyButton != null)
                RefreshBuyButton.onClick.AddListener(OnRefreshBuyButtonClick);

            if (CatchBuyButton != null)
                CatchBuyButton.onClick.AddListener(OnCatchBuyButtonClick);

            // Bind rewarded ad buttons - 绑定激励广告按钮
            if (SwapAdButton != null)
                SwapAdButton.onClick.AddListener(OnSwapAdButtonClick);

            if (RefreshAdButton != null)
                RefreshAdButton.onClick.AddListener(OnRefreshAdButtonClick);

            if (CatchAdButton != null)
                CatchAdButton.onClick.AddListener(OnCatchAdButtonClick);
        }

        private void UpdateGoldCoin()
        {
            if (goldCoinText != null && GoldCoin.Instance != null)
            {
                goldCoinText.text = GoldCoin.Instance.GetCurrentGold().ToString();
            }
        }

        #region 
        public void ChangeProps(Props _props)
        {
            // ȷ���Ѿ���ʼ��
            if (!IsInitialized)
            {
                ForceInitialize();
            }

            CurrentProp = _props;
            OnPropChanged?.Invoke(CurrentProp);
        }

        public void ShowProps(Props newProp)
        {
            if (PropsMapping.ContainsKey(newProp) && PropsMapping[newProp] != null)
            {
                HideAllProps();
                PropsMapping[newProp].SetActive(true);
                CurrentProp = newProp;
            }
        }

        public void HideAllProps()
        {
            foreach (var prop in PropsMapping.Values)
            {
                if (prop != null)
                    prop.SetActive(false);
            }
        }
        #endregion

        #region ��ť����ʵ��
        public void OnExitButtonClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeGameState(GameState.Playing);
            }
        }

        private void OnAddGoldCoinClick()
        {
            if (GoldCoin.Instance != null)
            {
                GoldCoin.Instance.AddGold(100);
            }
        }

        private void OnSwapBuyButtonClick()
        {
            if (GoldCoin.Instance != null && GoldCoin.Instance.GetCurrentGold() >= 100)
            {
                GoldCoin.Instance.SpendGold(100);

                // 道具使用不符合 Lion Analytics 的 MissionStep 要求
                // Power-ups don't fit Lion Analytics MissionStep requirements
                // (MissionStep only accepts: soft_fail, revive, ftue)

                
                if (SwapManager.Instance != null)
                {
                    SwapManager.Instance.StartSwapMode();
                }

               
                OnExitButtonClicked();
            }
            else
            {
                Debug.LogError("金币不足100");
            }
        }

        private void OnRefreshBuyButtonClick()
        {
            if (GoldCoin.Instance != null && GoldCoin.Instance.GetCurrentGold() >= 150)
            {
                GoldCoin.Instance.SpendGold(150);

                // 道具使用不符合 Lion Analytics 的 MissionStep 要求
                // Power-ups don't fit Lion Analytics MissionStep requirements

                
                if (RefreshManager.Instance != null)
                {
                    RefreshManager.Instance.TriggerRandomSwapAllSheep();
                }
               
                OnExitButtonClicked();
            }
            else
            {
                Debug.LogError("金币不足150");
            }
        }

        private void OnCatchBuyButtonClick()
        {
            if (GoldCoin.Instance != null && GoldCoin.Instance.GetCurrentGold() >= 200)
            {
                GoldCoin.Instance.SpendGold(200);

                // Lion Analytics: Log power-up usage as revive event
                // Lion分析：将道具使用记录为复活类事件
                if (LionSDKManager.Instance != null && GameManager.Instance != null)
                {
                    LionSDKManager.Instance.LogMissionStep(GameManager.Instance.CurrentLevel, "revive_catch_powerup");
                }

                
                if (HelpCatchManger.Instance != null)
                {
                    HelpCatchManger.Instance.HelpYouProp();


                }
              
                OnExitButtonClicked();
            }
            else
            {
                Debug.LogError("金币不足200");
            }
        }

        #region 激励广告按钮实现 - Rewarded Ad Button Implementations

        private void OnSwapAdButtonClick()
        {
            // Check if Lion SDK Manager and ad is available
            // 检查 Lion SDK 管理器和广告是否可用
            if (LionSDKManager.Instance == null)
            {
                Debug.LogError("LionSDKManager not initialized!");
                return;
            }

            // Disable button during ad
            // 广告期间禁用按钮
            if (SwapAdButton != null) SwapAdButton.interactable = false;

            // Show rewarded ad with callbacks
            // 显示激励广告并设置回调
            LionSDKManager.Instance.ShowRewardedAd(
                onSuccess: () =>
                {
                    // Ad completed successfully, give swap prop
                    // 广告成功完成，给予交换道具
                    Debug.Log("Rewarded ad completed - Granting Swap Prop");

                    // Lion Analytics: Log power-up usage as revive event (ad-based)
                    // Lion分析：将广告获得的道具使用记录为复活类事件
                    if (LionSDKManager.Instance != null && GameManager.Instance != null)
                    {
                        LionSDKManager.Instance.LogMissionStep(GameManager.Instance.CurrentLevel, "revive_swap_ad");
                    }

                    // 激活交换模式
                    if (SwapManager.Instance != null)
                    {
                        SwapManager.Instance.StartSwapMode();
                    }

                    // 关闭购买菜单
                    OnExitButtonClicked();

                    // Re-enable button
                    if (SwapAdButton != null) SwapAdButton.interactable = true;
                },
                onFailed: () =>
                {
                    // Ad failed or skipped
                    // 广告失败或跳过
                    Debug.LogError("Rewarded ad failed or was skipped");

                    // Re-enable button
                    if (SwapAdButton != null) SwapAdButton.interactable = true;
                }
            );
        }

        private void OnRefreshAdButtonClick()
        {
            // Check if Lion SDK Manager and ad is available
            // 检查 Lion SDK 管理器和广告是否可用
            if (LionSDKManager.Instance == null)
            {
                Debug.LogError("LionSDKManager not initialized!");
                return;
            }

            // Disable button during ad
            // 广告期间禁用按钮
            if (RefreshAdButton != null) RefreshAdButton.interactable = false;

            // Show rewarded ad with callbacks
            // 显示激励广告并设置回调
            LionSDKManager.Instance.ShowRewardedAd(
                onSuccess: () =>
                {
                    // Ad completed successfully, give refresh prop
                    // 广告成功完成，给予刷新道具
                    Debug.Log("Rewarded ad completed - Granting Refresh Prop");

                    // Lion Analytics: Log power-up usage as revive event (ad-based)
                    // Lion分析：将广告获得的道具使用记录为复活类事件
                    if (LionSDKManager.Instance != null && GameManager.Instance != null)
                    {
                        LionSDKManager.Instance.LogMissionStep(GameManager.Instance.CurrentLevel, "revive_refresh_ad");
                    }

                    // 激活刷新模式
                    if (RefreshManager.Instance != null)
                    {
                        RefreshManager.Instance.TriggerRandomSwapAllSheep();
                    }

                    // 关闭购买菜单
                    OnExitButtonClicked();

                    // Re-enable button
                    if (RefreshAdButton != null) RefreshAdButton.interactable = true;
                },
                onFailed: () =>
                {
                    // Ad failed or skipped
                    // 广告失败或跳过
                    Debug.LogError("Rewarded ad failed or was skipped");

                    // Re-enable button
                    if (RefreshAdButton != null) RefreshAdButton.interactable = true;
                }
            );
        }

        private void OnCatchAdButtonClick()
        {
            // Check if Lion SDK Manager and ad is available
            // 检查 Lion SDK 管理器和广告是否可用
            if (LionSDKManager.Instance == null)
            {
                Debug.LogError("LionSDKManager not initialized!");
                return;
            }

            // Disable button during ad
            // 广告期间禁用按钮
            if (CatchAdButton != null) CatchAdButton.interactable = false;

            // Show rewarded ad with callbacks
            // 显示激励广告并设置回调
            LionSDKManager.Instance.ShowRewardedAd(
                onSuccess: () =>
                {
                    // Ad completed successfully, give catch prop
                    // 广告成功完成，给予捕捉道具
                    Debug.Log("Rewarded ad completed - Granting Catch Prop");

                    // Lion Analytics: Log power-up usage as revive event (ad-based)
                    // Lion分析：将广告获得的道具使用记录为复活类事件
                    if (LionSDKManager.Instance != null && GameManager.Instance != null)
                    {
                        LionSDKManager.Instance.LogMissionStep(GameManager.Instance.CurrentLevel, "revive_catch_ad");
                    }

                    // 激活捕捉模式
                    if (HelpCatchManger.Instance != null)
                    {
                        HelpCatchManger.Instance.HelpYouProp();
                    }

                    // 关闭购买菜单
                    OnExitButtonClicked();

                    // Re-enable button
                    if (CatchAdButton != null) CatchAdButton.interactable = true;
                },
                onFailed: () =>
                {
                    // Ad failed or skipped
                    // 广告失败或跳过
                    Debug.LogError("Rewarded ad failed or was skipped");

                    // Re-enable button
                    if (CatchAdButton != null) CatchAdButton.interactable = true;
                }
            );
        }

        #endregion

        #endregion
    }
}