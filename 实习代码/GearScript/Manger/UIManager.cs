using LionStudios.Suite.Ads;
using LionStudios.Suite.Analytics;
using SuperGear;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems; // 新增：引入事件系统命名空间
using UnityEngine.UI;

namespace SuperGear
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;

        #region 序列化UI引用（在Inspector中拖入对应面板/组件）
        [Header("基础面板")]
        [Tooltip("开始面板")]
        [SerializeField] private GameObject startPanel;
        [Tooltip("暂停面板")]
        [SerializeField] private GameObject pausePanel;
        [Tooltip("通关面板")]
        [SerializeField] private GameObject winPanel;
        [Tooltip("失败面板")]
        [SerializeField] private GameObject losePanel;
        [Tooltip("加载面板")]
        [SerializeField] private GameObject loadPanel;
        [Tooltip("HUD面板")]
        [SerializeField] private GameObject hudPanel;
        [Tooltip("提示面板")]
        [SerializeField] private GameObject hintPanel;
        [Tooltip("每关目标面板")]
        [SerializeField] private GameObject ObjectivePanel;
        [SerializeField] private float ObjectivePaneltime=2.5f;
        private bool isObjectivePanelActive = false; // 标记目标面板是否正在显示
        private Coroutine objectiveAutoCloseCoroutine; // 存储自动关闭协程引用，用于手动关闭时停止

        [Header("功能按钮")]
        [Header("主界面显示的按钮")]
        [SerializeField] private Button quicklyGame; // 快速游戏
        [SerializeField] private Button startGame; //开始游戏
        [SerializeField] private Button DifficultyquicklyGame; // 快速游戏
        [SerializeField] private Button DifficultystartGame; //开始游戏
        [Header("暂停显示的按钮")]
        [SerializeField] private Button retryButtonInPasue; // 重试
        [SerializeField] private Button exitToMainInPasue; //退回主界面
        [SerializeField] private Button continueInPasue; //继续游戏
        [SerializeField] private Toggle bgmToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle vibrationToggle;
        public Color _normalColor = Color.white;
        public Color _grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 不透明灰色
        [Header("通关显示的按钮")]
        [SerializeField] private Button winToNextLevel; // 下一关
        [Header("失败显示的按钮")]
        [SerializeField] private Button retryButtonInLose; //重试
        [SerializeField] private Button exitToMainInLose; //退回主界面
        [Header("提示显示的按钮")]
        [SerializeField] private Button useHintButton; // 提示
        [SerializeField] private Button useAdvertisingHintButton; // 广告提示
        [SerializeField] private Button usepreviouslyHintButton; // 以前的提示
        [SerializeField] private Button exitHintButton; //退出提示
        [SerializeField] private Button AddCoinButtonInHint; //提示面板加钱(测试用)
        [Header("HUD显示的按钮")]
        [SerializeField] private Button leftButton; // 左切换按钮
        [SerializeField] private Button rightButton; // 右切换按钮
        [SerializeField] private Button rotateButton; // 旋转按钮
        [SerializeField] private Button pauseButton;//暂停按钮
        [SerializeField] private Button hintButton;//提示按钮
        [SerializeField] private Button IncreaseCameraSizeButton;//增加摄像机尺寸
        [SerializeField] private Button DecreaseCameraSizeButton;//减少摄像机尺寸
        [SerializeField] private Button AddCoinButton;//加钱(测试用)
        [SerializeField] private Image AdvertisingIcon;//广告图标

        private Camera _mainCamera;
        private float _currentCameraSize = 10f; // 初始尺寸设为10
        private float _minSize = 10f;
        private float _maxSize = 15f;
        private Coroutine _cameraSmoothCoroutine; // 控制摄像机平滑过渡的协程
        private const float _smoothDuration = 0.2f; // 平滑过渡总时长（可调整，值越小越快）

        [Header("信息组件")]
        [Header("主界面中显示的信息")]
        [SerializeField] private TMP_Text goldCoinsInMain;//当前金币数
        [SerializeField] private TMP_Text CurrentLevel;//当前关卡
        [SerializeField] private TMP_Text DifficultyCurrentLevel;//当前关卡
        [SerializeField] private TMP_Text QCurrentLevel;//快速当前关卡
        [SerializeField] private TMP_Text DifficultyQCurrentLevel;//快速当前关卡
        [SerializeField] private TMP_Text NextLevel;//下一关卡
        [SerializeField] private TMP_Text NextNextLevel;//下下关卡
        [Header("提示中显示的信息")]
        [SerializeField] private TMP_Text goldCoinsInHint;//当前金币数
        [Header("HUD中显示的信息")]
        [SerializeField] private TMP_Text timeText; // 显示剩余时间的文本（00:00格式）
        [SerializeField] private Image RedImage; // 最后20s的危险警示
        [Header("倒计时缩放动画（最后20秒）")]
        [SerializeField] private float TimeScaleAmplitude = 0.1f; // 缩放幅度（1 + 0.3 = 最大1.3倍）
        [SerializeField] private float TimeScaleFrequency = 1.5f; // 缩放频率（每秒2次放大缩小循环）
        private RectTransform timeTextRect; // 时间文本的RectTransform（用于缩放）
        public bool isWarning = false;

        [SerializeField] private TMP_Text levelText;//当前关卡数
        [SerializeField] private TMP_Text goldCoins;//当前金币数
        [SerializeField] private TMP_Text levelObjectiveGear;//关卡目标齿轮数
        [SerializeField] private GameObject GearAllRight;//关卡目标完成后的勾勾
        #endregion

        #region 提示功能相关
        [Header("提示功能")]
        [SerializeField] private Transform blockParent;      // 积木父对象
        [SerializeField] private float displayDuration = 2f; // 放置后提示积木显示时长
        [SerializeField] private float scaleFrequency = 2f;  // 缩放频率（每秒完成几次缩放循环）
        [SerializeField] private float scaleAmplitude = 0.1f; // 缩放幅度（基于原始大小的比例）

        [Header("类型与预制体映射")]
        [SerializeField] public List<BlockTypeMapping> typeMappings = new List<BlockTypeMapping>();
        public Dictionary<BlockType, GameObject> typeToPrefabDict = new Dictionary<BlockType, GameObject>();

        // 逐个提示专用：进度跟踪和实例管理（新增）
        private int currentOrdinaryHintProgress = 0; // 当前逐个提示进度（第N次显示前N个）
        private List<GameObject> currentOrdinaryHints = new List<GameObject>(); // 存储当前逐个提示的积木
        private Dictionary<GameObject, Coroutine> ordinaryPulseCoroutines = new Dictionary<GameObject, Coroutine>();
        private List<(GameObject hintBlock, Coroutine pulseCoroutine)> pendingHintDestroys = new List<(GameObject, Coroutine)>();

        private void InitializeTypeMappingDict()
        {
            typeToPrefabDict.Clear();
            foreach (var mapping in typeMappings)
            {
                if (mapping.blockPrefab != null && !typeToPrefabDict.ContainsKey(mapping.blockType))
                {
                    typeToPrefabDict.Add(mapping.blockType, mapping.blockPrefab);

                    if (mapping.blockPrefab.GetComponent<BlockControl>() == null)
                    {
                        Debug.LogError($"预制体 {mapping.blockPrefab.name} 缺少 BlockControl 组件！");
                    }
                }
            }
        }

        //实现逐个提示功能(不提升)
        private void PreviouslyHint()
        {
            var currentLevelData = LevelManager.Instance.allLevelDatas[(LevelManager.Instance.CurrentLevelIndex) % (LevelManager.Instance.allLevelDatas.Count)];
            if (currentLevelData == null || currentLevelData.demonstrationBlocks == null || currentLevelData.demonstrationBlocks.Count == 0)
            {
                Debug.LogWarning("当前关卡没有提示积木数据");
                return;
            }

            // 1. 清理上一次逐个提示的积木（避免重复叠加）
            ClearOrdinaryHints();

            // 2. 计算本次显示数量
            int totalHintCount = currentLevelData.demonstrationBlocks.Count;
            currentOrdinaryHintProgress = Mathf.Min(currentOrdinaryHintProgress, totalHintCount);

            // 3. 实例化前N个积木（增量显示）
            for (int i = 0; i < currentOrdinaryHintProgress; i++)
            {
                var demoBlock = currentLevelData.demonstrationBlocks[i];
                if (typeToPrefabDict.TryGetValue(demoBlock.blockType, out GameObject blockPrefab))
                {
                    // 实例化提示积木（逻辑与一次性提示一致）
                    GameObject hintBlock = Instantiate(blockPrefab, blockParent);
                    hintBlock.transform.position = demoBlock.worldPosition;
                    hintBlock.transform.eulerAngles = demoBlock.worldRotation;
                    Vector3 originalScale = blockPrefab.transform.localScale;
                    // 关键：设置层级（替换为你的自定义层级名称）
                    int hintLayer = LayerMask.NameToLayer("HintBlock");
                    if (hintLayer != -1)
                    {
                        SetLayerRecursively(hintBlock, hintLayer);
                    }
                    else
                    {
                        Debug.LogError("未找到HintBlock层级，请先在Tags and Layers中创建");
                        // 降级处理：使用忽略射线检测层
                        SetLayerRecursively(hintBlock, LayerMask.NameToLayer("Ignore Raycast"));
                    }

                    // 启动缩放动画并记录协程
                    Coroutine pulseCoroutine = StartCoroutine(PlayPulseAnimation(hintBlock.transform, originalScale));
                    ordinaryPulseCoroutines[hintBlock] = pulseCoroutine;

                    // 记录当前显示的积木，用于后续清理
                    currentOrdinaryHints.Add(hintBlock);
                    //配置齿轮启用/禁用
                    BlockGenerateItem generateItem = null;
                    foreach (var item in currentLevelData._blockGenerateData.generateItems)
                    {
                        if (item.blockType == demoBlock.blockType)
                        {
                            generateItem = item;
                            break;
                        }
                    }
                    if (generateItem != null)
                    {
                        ConfigureBlockGears(hintBlock, demoBlock);
                    }
                    // 新增：将提示积木+缩放协程存入暂存列表（按索引i对应真实积木）
                    pendingHintDestroys.Add((hintBlock, pulseCoroutine));
                    currentOrdinaryHints.Add(hintBlock);
                    //// 延迟销毁（总显示时长后自动清理）
                    //StartCoroutine(DestroyAfterDelay(hintBlock, displayDuration, pulseCoroutine));
                }
                else
                {
                    Debug.LogWarning($"没有找到{demoBlock.blockType}对应的预制体");
                }
            }
        }
        //实现逐个提示功能(逐个提升)
        private void OrdinaryHint()
        {
            var currentLevelData = LevelManager.Instance.allLevelDatas[(LevelManager.Instance.CurrentLevelIndex) % (LevelManager.Instance.allLevelDatas.Count)];
            if (currentLevelData == null || currentLevelData.demonstrationBlocks == null || currentLevelData.demonstrationBlocks.Count == 0)
            {
                Debug.LogWarning("当前关卡没有提示积木数据");
                return;
            }

            // 1. 清理上一次逐个提示的积木（避免重复叠加）
            ClearOrdinaryHints();

            // 2. 计算本次显示数量（进度+1，不超过总数量）
            int totalHintCount = currentLevelData.demonstrationBlocks.Count;
            currentOrdinaryHintProgress = Mathf.Min(currentOrdinaryHintProgress + 1, totalHintCount);

            // 3. 实例化前N个积木（增量显示）
            for (int i = 0; i < currentOrdinaryHintProgress; i++)
            {
                var demoBlock = currentLevelData.demonstrationBlocks[i];
                if (typeToPrefabDict.TryGetValue(demoBlock.blockType, out GameObject blockPrefab))
                {
                    // 实例化提示积木（逻辑与一次性提示一致）
                    GameObject hintBlock = Instantiate(blockPrefab, blockParent);
                    hintBlock.transform.position = demoBlock.worldPosition;
                    hintBlock.transform.eulerAngles = demoBlock.worldRotation;
                    Vector3 originalScale = blockPrefab.transform.localScale;
                    // 关键：设置层级（替换为你的自定义层级名称）
                    int hintLayer = LayerMask.NameToLayer("HintBlock");
                    if (hintLayer != -1)
                    {
                        SetLayerRecursively(hintBlock, hintLayer);
                    }
                    else
                    {
                        Debug.LogError("未找到HintBlock层级，请先在Tags and Layers中创建");
                        // 降级处理：使用忽略射线检测层
                        SetLayerRecursively(hintBlock, LayerMask.NameToLayer("Ignore Raycast"));
                    }

                    // 启动缩放动画并记录协程
                    Coroutine pulseCoroutine = StartCoroutine(PlayPulseAnimation(hintBlock.transform, originalScale));
                    ordinaryPulseCoroutines[hintBlock] = pulseCoroutine;

                
                    //配置齿轮启用/禁用
                    BlockGenerateItem generateItem = null;
                    foreach (var item in currentLevelData._blockGenerateData.generateItems)
                    {
                        if (item.blockType == demoBlock.blockType)
                        {
                            generateItem = item;
                            break;
                        }
                    }
                    if (generateItem != null)
                    {
                        ConfigureBlockGears(hintBlock, demoBlock);
                    }
                    // 将提示积木+缩放协程存入暂存列表（按索引i对应真实积木）
                    pendingHintDestroys.Add((hintBlock, pulseCoroutine));
                    currentOrdinaryHints.Add(hintBlock);
                    //// 延迟销毁（总显示时长后自动清理）
                    //StartCoroutine(DestroyAfterDelay(hintBlock, displayDuration, pulseCoroutine));
                }
                else
                {
                    Debug.LogWarning($"没有找到{demoBlock.blockType}对应的预制体");
                }
            }

            // 4. 显示完所有后直接显示所有的提示
            if (currentOrdinaryHintProgress >= totalHintCount)
            {
                currentOrdinaryHintProgress = totalHintCount; 
            }
        }
        private void ConfigureBlockGears(GameObject blockObj, DemonstrationBlock demoBlock)
        {
            // 假设积木预制体上有BlockData组件（存储齿轮数据） 
            BlockControl blockControl = blockObj.GetComponent<BlockControl>();
            BlockData blockData = blockControl._blockData;
            if (blockData == null || blockData._gearobject == null || blockData._gearobject.Length == 0)
            {
                Debug.LogWarning($"BlockPreviewManager：积木 {blockObj.name} 未配置BlockData或齿轮数据！");
                return;
            }

            // 遍历积木的齿轮，按配置启用/禁用（gear1对应索引0，gear2对应索引1，以此类推）
            for (int j = 0; j < blockData._gearobject.Length; j++)
            {
                GearData gearData = blockData._gearobject[j];
                if (gearData == null || gearData.GearObject == null)
                    continue;

                // 根据齿轮索引匹配配置（j=0→gear1，j=1→gear2，j=2→gear3，j=3→gear4）
                bool isGearEnabled = j switch
                {
                    0 => demoBlock.gear1,
                    1 => demoBlock.gear2,
                    2 => demoBlock.gear3,
                    3 => demoBlock.gear4,
                    _ => false // 超过4个齿轮默认禁用
                };

                // 1. 设置齿轮的启用状态（激活/隐藏）
                gearData.GearObject.SetActive(isGearEnabled);
                gearData.IsActive = false; // 提示齿轮禁用交互

                // 2. 关键：标记为「提示展示用齿轮」，禁止旋转
                gearData.IsShowForHint = true; // 同步BlockData中的标记

                // 3. 找到齿轮上的GearControl组件，设置IsShowForHint（旋转逻辑的核心判断）
                GearControl gearControl = gearData.GearObject.GetComponent<GearControl>();
                if (gearControl != null)
                {
                    gearControl.IsShowForHint = true;
                    // 额外保险：强制停止旋转（防止残留状态）
                    gearControl.StopRotation();
                }

            }

            // -------------------------- 禁用BlockControl功能 --------------------------
            if (blockControl != null)
            {
                
                // 1. 禁用BlockControl组件，使其生命周期和接口逻辑完全失效
                blockControl.enabled = false;
                Debug.Log($"禁用BlockControl组件: {blockObj.name} - 状态: {blockControl.enabled}");

                // 2. 移除组件上的事件监听（避免残留逻辑触发，如OnRemoved关联的齿轮停止）
                blockControl.OnRemoved -= blockControl.OnBlockRemovedStopGear;

                // 3. 额外保险：禁用积木的碰撞体（防止射线检测触发交互）
                Collider collider = blockObj.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }
        }
        // 设置物体及其所有子物体的层级
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null) return;

            // 设置自身层级
            obj.layer = layer;

            // 递归设置所有子物体层级
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        public void ClearOrdinaryHints()
        {
            // 停止缩放协程+销毁当前提示积木
            foreach (var coroutine in ordinaryPulseCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }
            ordinaryPulseCoroutines.Clear();

            // 清理暂存列表中的提示积木和协程
            foreach (var (hintBlock, pulseCoroutine) in pendingHintDestroys)
            {
                if (pulseCoroutine != null)
                    StopCoroutine(pulseCoroutine);
                if (hintBlock != null)
                    Destroy(hintBlock);
            }
            pendingHintDestroys.Clear();

            // 销毁当前提示积木
            foreach (var hintBlock in currentOrdinaryHints)
            {
                if (hintBlock != null)
                    Destroy(hintBlock);
            }
            currentOrdinaryHints.Clear();

        }
        public void InitializeHints()
        {
            currentOrdinaryHintProgress = 0;
        }

        // 脉动缩放动画：基于频率和幅度持续缩放
        private IEnumerator PlayPulseAnimation(Transform target, Vector3 originalScale)
        {
            float time = 0;
            // 计算角频率（2π×频率）
            float angularFrequency = 2 * Mathf.PI * scaleFrequency;

            while (true)
            {
                // 额外安全检查：如果目标已销毁则立即退出协程
                if (target == null)
                    yield break;

                // 使用正弦函数实现平滑缩放（1 + 幅度×正弦曲线）
                float scaleFactor = 1 + scaleAmplitude * Mathf.Sin(angularFrequency * time);
                target.localScale = originalScale * scaleFactor;

                time += Time.deltaTime;
                yield return null;
            }
        }

        // 延迟销毁协程（销毁前停止缩放动画）
        private IEnumerator DestroyAfterDelay(GameObject hintBlock, float delay, Coroutine pulseCoroutine)
        {
            yield return new WaitForSeconds(delay);

            // 停止缩放协程（防止访问已销毁对象）
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);

            Destroy(hintBlock);
        }
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
            _mainCamera = Camera.main;

            InitializeTypeMappingDict();
            InitSettingPanel();
        }

        private void Start()
        {
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
            RedImage.enabled=false;

        }
        private void Update()
        {
            UpdateGoldCoin();
            
            UpdateLevelVisual();
            CheckManualCloseObjectivePanel();//手动关闭目标面板
            UpdateAdvertisingIcon();
            UpdateLevelDifficulty();
            //Debug.Log($"当前输入状态：{InputManager.Instance.IsInputEnabled()}");
            //Debug.Log("当前摄像机Size"+_currentCameraSize);


        }

        private void OnDestroy()
        {
            // 取消事件订阅，避免内存泄漏
            UnsubscribeFromEvents();

        }
        #endregion

        #region 事件订阅/取消
        private void SubscribeToEvents()
        {
            if (GameStateManager.Instance != null)
            {
                // 监听倒计时更新事件
                GameStateManager.Instance.OnCountdownUpdated += UpdateCountdownDisplay;

                GameStateManager.Instance.OnCountdownEnd += OnCountdownEnded;

            }
        }
        private void UnsubscribeFromEvents()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnCountdownUpdated -= UpdateCountdownDisplay;
                GameStateManager.Instance.OnCountdownEnd -= OnCountdownEnded;

            }
        }
        #endregion

        #region 按钮点击事件绑定
        private void BindButtonEvents()
        {
            #region 主界面显示的按钮
            quicklyGame?.onClick.AddListener(OnQuicklyGameClicked);
            startGame?.onClick.AddListener(OnStartGameClicked);
            DifficultyquicklyGame?.onClick.AddListener(OnQuicklyGameClicked);
            DifficultystartGame?.onClick.AddListener(OnStartGameClicked);
            #endregion

            #region 暂停显示的按钮
            retryButtonInPasue?.onClick.AddListener(OnRetryButtonInPasueClicked);
            exitToMainInPasue?.onClick.AddListener(OnExitToMainInPasueClicked);
            continueInPasue?.onClick.AddListener(OnContinueInPasueClicked);
            // 绑定Toggle事件
            bgmToggle.onValueChanged.AddListener(OnBgmToggleChanged);
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
            #endregion

            #region 通关显示的按钮
            winToNextLevel?.onClick.AddListener(OnWinToNextLevelClicked);
            #endregion

            #region 失败显示的按钮
            retryButtonInLose?.onClick.AddListener(OnRetryButtonInLoseClicked);
            exitToMainInLose?.onClick.AddListener(OnExitToMainInLoseClicked);
            #endregion

            #region 提示显示的按钮
            useHintButton?.onClick.AddListener(OnUseHintButtonClicked);
            useAdvertisingHintButton?.onClick.AddListener(useAdvertisingHintButtonClicked);
            usepreviouslyHintButton?.onClick.AddListener(OnUsepreviouslyHintButtonClicked);
            exitHintButton?.onClick.AddListener(OnExitHintButtonClicked);
            AddCoinButtonInHint?.onClick.AddListener(OnAddCoinButtonInHintClicked);
            #endregion

            #region HUD显示的按钮
            //leftButton?.onClick.AddListener(BlockGenerate.Instance.PreviousBlock);
            leftButton?.onClick.AddListener(() => SoundManager.Instance.PlaySound("34"));

            //rightButton?.onClick.AddListener(BlockGenerate.Instance.NextBlock);
            rightButton?.onClick.AddListener(() => SoundManager.Instance.PlaySound("34"));

            //rotateButton?.onClick.AddListener(BlockGenerate.Instance.RotateCurrentBlock);
            rotateButton?.onClick.AddListener(() => SoundManager.Instance.PlaySound("34"));

            IncreaseCameraSizeButton?.onClick.AddListener(IncreaseCameraSize);
            DecreaseCameraSizeButton?.onClick.AddListener(DecreaseCameraSize);
            pauseButton?.onClick.AddListener(OnPauseButtonClicked);
            hintButton?.onClick.AddListener(OnHintButtonClicked);
            AddCoinButton?.onClick.AddListener(OnAddCoinButtonClicked);
            #endregion
        }

        #endregion

        #region 按钮功能实现

        #region 主界面功能实现
        private void OnQuicklyGameClicked()
        {
            LevelManager.Instance.LoadCurrentLevel();
            SetStartPanel(false);
            SetHudPanel(true);
            // 移除加载界面,直接开始游戏
            GameStateManager.Instance.StartCountdown();
            GameStateManager.Instance.StopCountdown(); // 瞬间暂停等到拖拽积木时再启动
            SoundManager.Instance.PlaySound("34");
        }
        private void OnStartGameClicked()
        {
            LevelManager.Instance.LoadCurrentLevel();
            SetStartPanel(false);
            SetHudPanel(true);
            // 移除加载界面,直接开始游戏
            GameStateManager.Instance.StartCountdown();
            GameStateManager.Instance.StopCountdown(); // 瞬间暂停等到拖拽积木时再启动
            SoundManager.Instance.PlaySound("34");
        }
        #endregion

        #region 暂停面板功能实现
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


        private void OnRetryButtonInPasueClicked()
        {
            GameStateManager.Instance.ResumeGame();
            LevelManager.Instance.LoadCurrentLevel();
            SetPausePanel(false);
            // 重试时展示加载界面
            NewLoading();
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);
        }
        private void OnExitToMainInPasueClicked()
        {
            GameStateManager.Instance.ResumeGame();
            ResetAllPanels();
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);

            var levelNumber = LevelManager.Instance.CurrentLevelIndex + 1;
            LionAnalytics.MissionAbandoned(
             missionType: "main",
            missionName: $"main_level_{levelNumber}",
            missionID: levelNumber,
            missionAttempt: null,
            additionalData: null,
            isGamePlay: null
            );


        }
        private void OnContinueInPasueClicked()
        {
            SetPausePanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);
        }
        #endregion

        #region 通关面板功能实现
        private void OnWinToNextLevelClicked()
        {
            // 核心：加载前强制重置通关状态和计数
            LevelManager.Instance.isLevelCompleted = false;
            LevelManager.Instance.totalTargetGearsNumber = 0;
            LevelManager.Instance.activatedTargetGearsNumber = 0;
            GameStateManager.Instance.ResumeGame();
            GameStateManager.Instance.ResumeCountdown();
            // 移除加载界面,直接加载下一关
            LevelManager.Instance.LoadCurrentLevel();
            SetWinPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.StartCountdown();
            GameStateManager.Instance.StopCountdown(); // 瞬间暂停等到拖拽积木时再启动
            GoldCoinManager.Instance.AddFixedGold(40);
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);
        }

        #endregion

        #region 失败面板功能实现
        private void OnRetryButtonInLoseClicked()
        {
            GameStateManager.Instance.ResumeGame();
            LevelManager.Instance.LoadCurrentLevel();
            SetLosePanel(false);
            // 失败重试时展示加载界面
            NewLoading();
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);

        }
        private void OnExitToMainInLoseClicked()
        {
            
            GameStateManager.Instance.ResumeGame();
            ResetAllPanels();
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);

            var levelNumber = LevelManager.Instance.CurrentLevelIndex + 1;
            LionAnalytics.MissionAbandoned(
             missionType: "main",
            missionName: $"main_level_{levelNumber}",
            missionID: levelNumber,
            missionAttempt: null,
            additionalData: null,
            isGamePlay: null
            );

        }
        private void OnCountdownEnded()
        {
            // 停止倒计时，暂停游戏
            GameStateManager.Instance.StopCountdown();
            GameStateManager.Instance.PauseGame();
            InputManager.Instance.SetInput(false);
            // 隐藏HUD，显示失败面板
            SetHudPanel(false);
            SetLosePanel(true);

            var levelNumber = LevelManager.Instance.CurrentLevelIndex + 1;
            LionAnalytics.MissionFailed(
             missionType: "main",
            missionName: $"main_level_{levelNumber}",
            missionID: levelNumber,
            missionAttempt: null,
            additionalData: null,
            failReason: "time_out", // 失败原因示例：时间耗尽
            isGamePlay: null
);

        }
        #endregion

        #region 提示面板功能实现
        private void OnUseHintButtonClicked()
        {
            if (GoldCoinManager.Instance.GoldCoin >= 100)
            {
                // 金币足够：直接扣钱并显示提示
                GoldCoinManager.Instance.AddFixedGold(-100);
                CloseHintPanelAndResumeGame(); //公共逻辑
                OrdinaryHint();
            }
        }
        private void useAdvertisingHintButtonClicked()
        {
            // 金币不足：尝试显示激励广告（仅触发广告，不立即变更状态）
            bool isAdShown = LionAds.TryShowRewarded("abdecaaea20f46c7", OnRewardReceived, OnAdFailed);

            // 若广告成功触发显示，暂停游戏、保持当前面板状态（避免遮挡广告）
            if (isAdShown)
            {
                //GameStateManager.Instance.PauseGame(); // 广告播放时暂停游戏
                InputManager.Instance.SetInput(false); // 禁用输入，防止误操作
                SoundManager.Instance.PlaySound("34"); // 仅播放广告触发音效
                SoundManager.Instance.ToggleBGM(true);
            }
            else
            {
                Debug.Log("广告暂时不可用，请稍后再试");
            }
        }

        // 广告播放成功回调
        private void OnRewardReceived()
        {
            Debug.Log("广告激励回调触发！准备关闭提示面板"); // 加日志
           
            OrdinaryHint(); // 发放“提示”奖励
            CloseHintPanelAndResumeGame(); // 切换面板、恢复游戏状态

        }

        // 广告播放失败回调
        private void OnAdFailed()
        {
            Debug.Log("广告播放失败/被跳过！");

            CloseHintPanelAndResumeGame();
            
        }

        // 抽取公共逻辑：关闭提示面板+恢复游戏状态（避免重复代码）
        private void CloseHintPanelAndResumeGame()
        {
            //Debug.LogError("关闭提示面板+恢复游戏状态");
            SetHintPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeGame(); // 恢复游戏
            GameStateManager.Instance.ResumeCountdown(); // 恢复倒计时（之前停止了，这里要同步恢复）
            InputManager.Instance.SetInput(true); // 开启输入
            SoundManager.Instance.ToggleBGM(false);

        }
        private void OnUsepreviouslyHintButtonClicked()
        {   
                SetHintPanel(false);
                SetHudPanel(true);
                GameStateManager.Instance.ResumeGame();
                //BlockGenerate.Instance.RecycleAllPlacedBlocks();
                SoundManager.Instance.PlaySound("34");
                InputManager.Instance.SetInput(true);
                PreviouslyHint();
                //StartCoroutine(HintTimeStopInput());    
        }
        private IEnumerator HintTimeStopInput()
        {
            try
            {
                InputManager.Instance.SetInput(false);
                yield return new WaitForSecondsRealtime(0.3f); // 替换为Realtime，不受timeScale影响
            }
            finally
            {
                // 无论是否报错，最终都恢复输入
                InputManager.Instance.SetInput(true);
            }
        }
        private void OnExitHintButtonClicked()
        {
            SetHintPanel(false);
            SetHudPanel(true);
            GameStateManager.Instance.ResumeGame();
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(true);
        }
        private void OnAddCoinButtonInHintClicked()
        {
            InputManager.Instance.SetInput(true);
            GoldCoinManager.Instance.AddFixedGold(1000);
            SoundManager.Instance.PlaySound("34");
        }
        #endregion

        #region HUD功能实现
        private void OnPauseButtonClicked()
        {
            GameStateManager.Instance.PauseGame();
            SetHudPanel(false);
            SetPausePanel(true);
            SoundManager.Instance.PlaySound("34");
            InputManager.Instance.SetInput(false);
        }
        private void OnHintButtonClicked()
        {
            
                GameStateManager.Instance.PauseGame();
                SetHudPanel(false);
                SetHintPanel(true);
                SoundManager.Instance.PlaySound("34");
                InputManager.Instance.SetInput(false);

        }
        private void OnAddCoinButtonClicked()
        {
            GoldCoinManager.Instance.AddFixedGold(1000);
            SoundManager.Instance.PlaySound("34");
        }

        public void IncreaseCameraSize()
        {
            // 1. 计算目标尺寸（不超过最大值）
            float targetSize = Mathf.Min(_currentCameraSize + 1f, _maxSize);
            // 2. 启动平滑协程（先停止之前的协程，避免冲突）
            StartCameraSmoothTransition(targetSize);
        }

        // 减少摄像机尺寸（平滑版）
        public void DecreaseCameraSize()
        {
            // 1. 计算目标尺寸（不低于最小值）
            float targetSize = Mathf.Max(_currentCameraSize - 1f, _minSize);
            // 2. 启动平滑协程
            StartCameraSmoothTransition(targetSize);
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

        #region 摄像机缩放
        //摄像机尺寸平滑过渡的核心协程
        public void StartCameraSmoothTransition(float targetSize)
        {
            // 1. 如果当前已经在过渡，先停止之前的协程（避免多次点击导致混乱）
            if (_cameraSmoothCoroutine != null)
            {
                StopCoroutine(_cameraSmoothCoroutine);
            }

            // 2. 启动新协程并记录引用
            _cameraSmoothCoroutine = StartCoroutine(SmoothTransitionCoroutine(targetSize));
        }

        private IEnumerator SmoothTransitionCoroutine(float targetSize)
        {
            float elapsedTime = 0f;
            float startSize = _currentCameraSize;
            // 计算总差值（确保步长为正）
            float totalDelta = Mathf.Abs(targetSize - startSize);
            // 每帧步长 = 总差值 / 总帧数（总帧数 = 过渡时长 / 每帧时间）
            float step = totalDelta / (_smoothDuration / Time.deltaTime);

            while (Mathf.Abs(_currentCameraSize - targetSize) > 0.01f)
            {
                elapsedTime += Time.deltaTime;
                // 按每帧步长移动到目标值
                _currentCameraSize = Mathf.MoveTowards(_currentCameraSize, targetSize, step);
                UpdateCameraSize();
                yield return null;
            }

            // 强制赋值避免残留误差
            _currentCameraSize = targetSize;
            UpdateCameraSize();
            _cameraSmoothCoroutine = null;
        }

        private void UpdateCameraSize()
        {
            if (_mainCamera != null && _mainCamera.orthographic)
            {
                _mainCamera.orthographicSize = _currentCameraSize;
            }
        }
        public float GetCurrentCameraSize()
        {
            return _currentCameraSize; // 返回当前摄像机size值
        }
        #endregion 

        #region 面板显示与隐藏
        public void ResetAllPanels()
        {
            startPanel?.SetActive(true); // 初始显示开始面板
            pausePanel?.SetActive(false);
            winPanel?.SetActive(false);
            losePanel?.SetActive(false);
            loadPanel?.SetActive(false);
            hudPanel?.SetActive(false); // 游戏未开始时隐藏HUD
            hintPanel?.SetActive(false);
            ObjectivePanel?.SetActive(false);
        }

        public void SetStartPanel(bool active)
        {
            startPanel?.SetActive(active);
        }

        public void SetPausePanel(bool active)
        {
            pausePanel?.SetActive(active);
        }

        public void SetWinPanel(bool active)
        {
            winPanel?.SetActive(active);
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
        public void SetHintPanel(bool active)
        {
            hintPanel?.SetActive(active);
        }
        public void SetObjectivePanel(bool active)
        {
            if (ObjectivePanel == null) return;

            ObjectivePanel.SetActive(active);
            isObjectivePanelActive = active; // 同步状态标记

            // 若面板被手动关闭，停止自动关闭协程
            if (!active && objectiveAutoCloseCoroutine != null)
            {
                StopCoroutine(objectiveAutoCloseCoroutine);
                objectiveAutoCloseCoroutine = null;
            }
        }
        public void StartObjectiveHint()
        {
            // 停止之前可能残留的协程，避免重复执行
            if (objectiveAutoCloseCoroutine != null)
            {
                StopCoroutine(objectiveAutoCloseCoroutine);
            }
            // 启动新协程并记录引用
            objectiveAutoCloseCoroutine = StartCoroutine(ObjectiveHint(ObjectivePaneltime));
        }

        private IEnumerator ObjectiveHint(float delaySeconds)
        {
            if (ObjectivePanel == null)
            {
                Debug.LogWarning("ObjectivePanel未在Inspector中赋值，无法显示目标面板！");
                yield break;
            }
            SetObjectivePanel(true);
            float waitTime = Mathf.Max(0, delaySeconds);
            yield return new WaitForSeconds(waitTime);
           
            SetObjectivePanel(false);
        }
        // 手动关闭目标面板的输入检测逻辑
        private void CheckManualCloseObjectivePanel()
        {
            // 仅当目标面板显示时才检测输入
            if (!isObjectivePanelActive || ObjectivePanel == null) return;

            // 1. 检测鼠标左键点击（PC端）
            if (Input.GetMouseButtonDown(0))
            {
                SetObjectivePanel(false); // 关闭面板
               
            }

            // 2. 检测触屏点击（移动端）
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                   SetObjectivePanel(false); // 关闭面板
                   
                }
            }
        }

        #endregion

        #region 常态UI更新

        public void UpdateLevelDifficulty()
        {
            LevelData leveldata = LevelManager.Instance.allLevelDatas[LevelManager.Instance.CurrentLevelIndex];
            if (leveldata.IsDifficultyLevel)
            {
                quicklyGame.gameObject.SetActive(false);
                startGame.gameObject.SetActive(false);
                DifficultyquicklyGame.gameObject.SetActive(true);
                DifficultystartGame.gameObject.SetActive(true);
            }
            else
            {
                quicklyGame.gameObject.SetActive(true);
                startGame.gameObject.SetActive(true);
                DifficultyquicklyGame.gameObject.SetActive(false);
                DifficultystartGame.gameObject.SetActive(false);
            }
        }
        private void UpdateAdvertisingIcon()
        {
            if(GoldCoinManager.Instance.GoldCoin >= 100)
            {
                AdvertisingIcon.gameObject.SetActive(false);
                useHintButton.gameObject.SetActive(true);
                useAdvertisingHintButton.gameObject.SetActive(false);
            }
            else
            {
                AdvertisingIcon.gameObject.SetActive(true);
                useHintButton.gameObject.SetActive(false);
                useAdvertisingHintButton.gameObject.SetActive(true);
            }
        }
        public void UpdateLevelText()
        {
            if (levelText != null)
            {
                levelText.text = $"Level {LevelManager.Instance.CurrentLevelIndex + 1}";
            }
        }
        private void UpdateGoldCoin()
        {
            if (goldCoins != null)
            {
                goldCoins.text = $" {GoldCoinManager.Instance.GoldCoin}";
            }
            if (goldCoinsInHint != null)
            {
                goldCoinsInHint.text = $" {GoldCoinManager.Instance.GoldCoin}";
            }
        }
        public void UpdatelevelObjectiveGear()
        {
            LevelManager.Instance.CheckTargetNumber();
            // 1. 计算当前剩余的目标齿轮数
            int remainingGearCount = LevelManager.Instance.totalTargetGearsNumber - LevelManager.Instance.activatedTargetGearsNumber;

            if (levelObjectiveGear != null)
            {
                // 2. 更新剩余齿轮数文本
                levelObjectiveGear.text = $" {remainingGearCount}";
                // 3. 控制levelObjectiveGear显示：剩余数≠0时显示，=0时隐藏
                levelObjectiveGear.gameObject.SetActive(remainingGearCount != 0);

                // 更新ObjectivePanel中的目标文本
                TMP_Text targetText = ObjectivePanel.GetComponentInChildren<TMP_Text>(includeInactive: true);
                if (targetText != null) // 增加targetText空值检查，避免报错
                {
                    targetText.text = $" {remainingGearCount}";
                }
            }

            // 4. 控制GearAllRight显示：剩余数=0时显示，≠0时隐藏（增加空值检查）
            if (GearAllRight != null)
            {
                GearAllRight.SetActive(remainingGearCount == 0);
            }
        }
        private void UpdateLevelVisual()
        {
            QCurrentLevel.text = $"Level {LevelManager.Instance.CurrentLevelIndex+1}";
            CurrentLevel.text = $"{LevelManager.Instance.CurrentLevelIndex+1}";
            NextLevel.text = $"{LevelManager.Instance.CurrentLevelIndex+2}";
            NextNextLevel.text = $"{LevelManager.Instance.CurrentLevelIndex+3}";
            DifficultyQCurrentLevel.text = $"Level {LevelManager.Instance.CurrentLevelIndex + 1}";
            DifficultyCurrentLevel.text = $"{LevelManager.Instance.CurrentLevelIndex + 1}";
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
            if (remainingTime <= 30f && remainingTime > 0f&& !isWarning)
            {
                isWarning = true;
                SoundManager.Instance.PlaySound("6");
            }
        }


        #endregion

        #region 辅助工具方法 
        //public void UpdateButtonStates()
        //{
        //    // 只有备选区数量>1时才允许切换
        //    bool hasMultipleBlocks = BlockGenerate.Instance.AlternativeArea.Count > 1;
        //    leftButton.interactable = hasMultipleBlocks;
        //    rightButton.interactable = hasMultipleBlocks;

        //    // 只有备选区有积木时才允许旋转
        //    rotateButton.interactable = BlockGenerate.Instance.AlternativeArea.Count > 0;
        //}
        #endregion
    }
}