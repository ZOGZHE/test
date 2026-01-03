using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace WoolyPath
{
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu instance { get; private set; }

        #region UI引用（Inspector配置）
        [Tooltip("金币文本显示")]
        public TMP_Text goldCoinText;
        [Tooltip("主按钮 - 点击进入最高解锁关卡")]
        public Button QuickLevelButton;
        [Tooltip("次按钮 - 点击进入最高解锁关卡")]
        public Button LevelButton;
        [Tooltip("下一关按钮")]
        public Button NextLevelButton;
        [Tooltip("下下关按钮")]
        public Button NextNextLevelButton;
        [Tooltip("添加金币按钮（测试用）")]
        public Button AddGoldCoin;
        [Tooltip("测试重置按钮")]
        public Button test;
        #endregion

        #region Unity生命周期函数
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            BindLevelButtonClick();
            RefreshLevelButtonStates();
        }

        private void Update()
        {
            RefreshLevelButtonStates();
        }
        #endregion


        #region 关卡按钮绑定
        /// <summary>
        /// 绑定关卡按钮的点击事件（防止重复绑定）
        /// </summary>
        private void BindLevelButtonClick()
        {
            if (QuickLevelButton != null)
            {
                QuickLevelButton.onClick.RemoveAllListeners();
                QuickLevelButton.onClick.AddListener(OnQuickLevelButtonClick);
            }
            if (LevelButton != null)
            {
                LevelButton.onClick.RemoveAllListeners();
                LevelButton.onClick.AddListener(OnLevelButtonClick);
            }
            if (AddGoldCoin != null)
            {
                AddGoldCoin.onClick.RemoveAllListeners();
                AddGoldCoin.onClick.AddListener(OnAddGoldCoinClick);
            }
            if (test != null)
            {
                test.onClick.RemoveAllListeners();
                test.onClick.AddListener(OntestClick);
            }
        }
        #endregion


        #region UI刷新逻辑（金币、主按钮、整体状态）
        private void UpdateGoldCoin()
        {
            goldCoinText.text = GoldCoin.Instance.GetCurrentGold().ToString();
        }

        private void UpdateQuickLevelButton()
        {
            int highestUnlocked = LevelManager.Instance.HighestCompletedLevel;
            TMP_Text buttonText = QuickLevelButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"Level {highestUnlocked + 1}";
            }
        }

        private void UpdateLevelButton()
        {
            int highestUnlocked = LevelManager.Instance.HighestCompletedLevel;
            TMP_Text buttonText = LevelButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{highestUnlocked + 1}";
            }
        }

        private void UpdateNextLevelButton()
        {
            int highestUnlocked = LevelManager.Instance.HighestCompletedLevel;
            TMP_Text buttonText = NextLevelButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{highestUnlocked + 2}";
            }
        }

        private void UpdateNextNextLevelButton()
        {
            int highestUnlocked = LevelManager.Instance.HighestCompletedLevel;
            TMP_Text buttonText = NextNextLevelButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = $"{highestUnlocked + 3}";
            }
        }

        private void RefreshLevelButtonStates()
        {
            UpdateGoldCoin();
            UpdateQuickLevelButton();
            UpdateLevelButton();
            UpdateNextLevelButton();
            UpdateNextNextLevelButton();
        }
        #endregion


        #region 按钮点击事件实现（核心交互逻辑）
        /// <summary>
        /// 关卡按钮点击事件（进入指定索引的关卡）
        /// </summary>
        private void OnQuickLevelButtonClick()
        {
            if (LevelManager.Instance == null)
            {
                Debug.LogError("[MainMenu] LevelManager 实例不存在！");
                return;
            }
            // 获取最高解锁关卡
            int highestUnlockedLevel = LevelManager.Instance.HighestCompletedLevel;
            if (highestUnlockedLevel >= 0)
            {
                // 优先用GameManager启动，无则用LevelManager加载
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.StartLevel(highestUnlockedLevel);
                }
                else
                {
                    LevelManager.Instance.LoadLevel(highestUnlockedLevel);
                }
            }
            else
            {
                Debug.LogWarning("[MainMenu] 没有可用的解锁关卡！");
            }
        }

        private void OnLevelButtonClick()
        {
            OnQuickLevelButtonClick();
        }

        public void OntestClick()
        {
            // 记录重置前的进度（用于日志对比）
            int beforeIndex = LevelManager.Instance.GetHighestCompletedLevel();

            // 执行重置逻辑
            LevelManager.Instance.reProgress(); // 重置关卡进度
            LevelManager.Instance.LoadProgress(); // 重新加载存档到内存
            GoldCoin.Instance.CleanGold(); // 清空金币

            // 记录重置后的进度（日志对比）
            int afterIndex = LevelManager.Instance.GetHighestCompletedLevel();
            Debug.Log($"[MainMenu] 关卡进度重置：{beforeIndex + 1} → {afterIndex + 1}");
        }

        /// <summary>
        /// 测试添加金币按钮点击事件（添加100金币，仅测试用）
        /// </summary>
        private void OnAddGoldCoinClick()
        {
            GoldCoin.Instance.AddGold(100);
            Debug.Log("[MainMenu] 测试添加100金币");
        }
        #endregion

    }
}