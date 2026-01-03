using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

namespace WoolyPath
{
    // 定义所有UI面板类型的枚举，用于标识和切换不同面板
    public enum UIPanel
    {
        MainMenu,       // 主菜单面板
        GameHUD,        // 游戏中的HUD面板（显示分数、关卡等）
        PauseMenu,      // 暂停菜单面板
        GameOverPanel,  // 游戏结束面板
        VictoryPanel,   // 胜利面板
        SettingsPanel,  // 设置面板
        LoadingPanel,   // 加载界面
         BuyPanel   //购买界面
    }

    // UI管理器：负责所有UI面板的显示、隐藏、更新，以及按钮事件处理
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI面板引用")]
        [SerializeField] private Canvas mainCanvas;             // 主画布（所有UI的父级）

        [Header("HUD元素")]
        [SerializeField] private GameObject gameHUD;            // 游戏中的HUD面板


        [Header("菜单面板")]
        [SerializeField] private GameObject mainMenuPanel;       // 主菜单面板
        [SerializeField] private GameObject gameOverPanel;       // 游戏结束面板
        [SerializeField] private GameObject victoryPanel;        // 胜利面板
        [SerializeField] private GameObject pauseMenuPanel;        // 暂停面板
        [SerializeField] private GameObject settingsPanel;       // 设置面板
        [SerializeField] private GameObject loadingPanel;       // 设置面板
        [SerializeField] private GameObject buyPanel;       // 购买面板

        // 面板字典：将UIPanel枚举与对应的GameObject关联，方便快速查找和操作
        private Dictionary<UIPanel, GameObject> panelDictionary;
        // 当前显示的面板，用于切换面板时先隐藏当前面板

        [HideInInspector] public UIPanel currentPanel = UIPanel.MainMenu;

        // 初始化单例，确保场景中只有一个UIManager实例
        private void Awake()
        {
            // 单例模式：如果实例不存在，设置为当前实例并初始化UI；否则销毁重复实例
            if (Instance == null)
            {
                Instance = this;
                buyPanel.SetActive(true);
                InitializeUI();  // 初始化UI面板字典和画布
            }
            else
            {
                Destroy(gameObject);
            }
           
           
        }

        // 启动时设置事件监听，并默认显示主菜单
        private void Start()
        {
            // 初始显示主菜单
            ShowPanel(UIPanel.MainMenu);

            // 确保监听游戏状态
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnConveyorBeltFull += OnBeltFull;
        }

        // 初始化UI系统：创建面板字典，确保主画布存在
        private void InitializeUI()
        {
            // 初始化面板字典，关联枚举与对应面板对象
            panelDictionary = new Dictionary<UIPanel, GameObject>
            {
                { UIPanel.MainMenu, mainMenuPanel },
                { UIPanel.GameHUD, gameHUD },
                 { UIPanel.PauseMenu, pauseMenuPanel },
                { UIPanel.GameOverPanel, gameOverPanel },
                { UIPanel.VictoryPanel, victoryPanel },
                { UIPanel.SettingsPanel, settingsPanel },
                { UIPanel.LoadingPanel , loadingPanel },
                { UIPanel.BuyPanel , buyPanel }
            };

            // 确保主画布存在（所有UI的根节点）
            if (mainCanvas == null)
            {
                mainCanvas = FindObjectOfType<Canvas>();  // 查找场景中已有的Canvas
                if (mainCanvas == null)
                {
                    Debug.LogError("无Canvas");
                }
            }
            HideAllPanels();
        }
        //失败判定
        public void OnBeltFull()
        {
            ShowPanel(UIPanel.GameOverPanel);
            GameManager.Instance.ChangeGameState(GameState.GameOver);
        }
        // 显示指定的面板（先隐藏当前面板，再显示目标面板）
        public void ShowPanel(UIPanel panel)
        {
            // 先隐藏所有面板
          //  HideAllPanels();
            // 隐藏当前显示的面板（如果存在）
            if (panelDictionary.ContainsKey(currentPanel) && panelDictionary[currentPanel] != null)
            {
                panelDictionary[currentPanel].SetActive(false);
            }
            // 显示目标面板（如果存在）
            if (panelDictionary.ContainsKey(panel) && panelDictionary[panel] != null)
            {
                panelDictionary[panel].SetActive(true);
                currentPanel = panel;  // 更新当前面板为目标面板
                //Debug.Log($"[UIManager] 显示面板: {panel}");
            }
            else
            {
                Debug.LogWarning($"[UIManager] 面板不存在: {panel}");
            }
        }

        // 隐藏指定的面板
        public void HidePanel(UIPanel panel)
        {
            // 检查面板是否在字典中且引用有效，然后隐藏
            if (panelDictionary.ContainsKey(panel) && panelDictionary[panel] != null)
            {
                panelDictionary[panel].SetActive(false);
                Debug.Log($"[UIManager] 隐藏面板: {panel}");
            }
        }

        // 隐藏所有面板
        public void HideAllPanels()
        {
            // 遍历字典中所有面板并隐藏
            foreach (var panel in panelDictionary.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }

        // 处理游戏状态变化：根据新状态切换对应的UI面板
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    ShowPanel(UIPanel.MainMenu);  // 主菜单状态显示主菜单面板
                    break;

                case GameState.Playing:
                    ShowPanel(UIPanel.GameHUD);   // 游戏进行中显示HUD面板
                    break;

                case GameState.Paused:
                    ShowPanel(UIPanel.PauseMenu); // 暂停状态显示暂停菜单
                    break;

                case GameState.GameOver:
                    ShowPanel(UIPanel.GameOverPanel); // 游戏结束显示结束面板
                    break;

                case GameState.Victory:
                    ShowPanel(UIPanel.VictoryPanel);  // 胜利状态显示胜利面板
                    break;
                case GameState.Loading:
                    ShowPanel(UIPanel.LoadingPanel);  // 加载状态显示加载面板
                    break;
                case GameState.Buying:
                    ShowPanel(UIPanel.BuyPanel);  // 加载状态显示加载面板
                    break;
            }
        }
    }
}