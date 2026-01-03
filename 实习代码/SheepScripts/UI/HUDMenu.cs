using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WoolyPath
{
    public class HUDMenu : MonoBehaviour
    {
        public Button SettingButton;
        public TMP_Text _LevelText;
        public TMP_Text _SheepCount;
        public Button SwapButton;
        public Button RefreshButton;
        public Button CatchButton;
        //金币逻辑
        public TMP_Text goldCoinText;
        public Button AddGoldCoin;
        public TMP_Text SheepCapacity;

        private bool isInitialized = false;

        private void Start()
        {
            Initialize();
            BindButtonEvents();
            isInitialized = true;
        }

        private void Update()
        {
            // 只在需要时更新，避免每帧调用
            if (isInitialized)
            {
                UpdateSheepCount();
                UpdateGoldCoin();
                UpdateLevelText();
            }
            UpdateSheepCapacity();
        }

        private void Initialize()
        {
            UpdateLevelText();
            UpdateSheepCount();
            UpdateGoldCoin();
            UpdateSheepCapacity();
        }

        private void BindButtonEvents()
        {
            // 注册设置按钮事件
            if (SettingButton != null)
            {
                SettingButton.onClick.RemoveAllListeners();
                SettingButton.onClick.AddListener(OnSettingButtonClick);
            }
            if (SwapButton != null)
            {
                SwapButton.onClick.RemoveAllListeners();
                SwapButton.onClick.AddListener(OnSwapButtonClick);
            }
            if (RefreshButton != null)
            {
                RefreshButton.onClick.RemoveAllListeners();
                RefreshButton.onClick.AddListener(OnRefreshButtonClick);
            }
            if (CatchButton != null)
            {
                CatchButton.onClick.RemoveAllListeners();
                CatchButton.onClick.AddListener(OnCatchButtonClick);
            }
            if (AddGoldCoin != null)
            {
                AddGoldCoin.onClick.RemoveAllListeners();
                AddGoldCoin.onClick.AddListener(OnAddGoldCoinClick);
            }
        }

        #region UI数据显示
        public void UpdateLevelText()
        {
            if (_LevelText != null && GameManager.Instance != null)
            {
                _LevelText.text = "LV." + (LevelManager.Instance.HighestCompletedLevel+1);
            }
        }

        private void UpdateGoldCoin()
        {
            if (goldCoinText != null && GoldCoin.Instance != null)
            {
                goldCoinText.text = GoldCoin.Instance.GetCurrentGold().ToString();
            }
        }

        private void UpdateSheepCount()
        {
            if (_SheepCount != null && SheepSpawner.instance != null)
            {
                int activeCount = SheepSpawner.instance.GetRemainingSheepCount();
                int totalCount = SheepSpawner.instance.totalSheepForLevel;
                _SheepCount.text = $"{activeCount}/{totalCount}";
            }
        }
        private void UpdateSheepCapacity()
        {
            if (SheepCapacity != null &&ConveyorBelt.Instance!=null)
            {
                int ConveyorSheepCount = ConveyorBelt.Instance.woolsOnBelt.Count;
                int CapacityCount = ConveyorBelt.Instance.maxCapacity;
                SheepCapacity.text = $"{ConveyorSheepCount}/{CapacityCount}";
            }
        }
        #endregion

        #region 按钮功能实现
        private void OnSettingButtonClick()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeGameState(GameState.Paused);
            }
        }

        private void OnSwapButtonClick()
        {
            if (BuyMenu.Instance != null)
            {
                // 确保BuyMenu已经初始化
                if (!BuyMenu.Instance.IsInitialized)
                {
                    BuyMenu.Instance.ForceInitialize();
                }
                BuyMenu.Instance.ChangeProps(Props.SwapProp);
                GameManager.Instance.ChangeGameState(GameState.Buying);
            }
        }

        private void OnRefreshButtonClick()
        {
            if (BuyMenu.Instance != null)
            {
                if (!BuyMenu.Instance.IsInitialized)
                {
                    BuyMenu.Instance.ForceInitialize();
                }
                BuyMenu.Instance.ChangeProps(Props.RefreshProp);
                GameManager.Instance.ChangeGameState(GameState.Buying);
            }
        }

        private void OnCatchButtonClick()
        {
            if (BuyMenu.Instance != null)
            {
                if (!BuyMenu.Instance.IsInitialized)
                {
                    BuyMenu.Instance.ForceInitialize();
                }
                BuyMenu.Instance.ChangeProps(Props.CatchProp);
                GameManager.Instance.ChangeGameState(GameState.Buying);
            }
        }

        private void OnAddGoldCoinClick()
        {
            if (GoldCoin.Instance != null)
            {
                GoldCoin.Instance.AddGold(100);
            }
        }
        #endregion
    }
}