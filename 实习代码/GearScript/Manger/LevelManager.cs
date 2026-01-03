using LionSDK;
using LionStudios.Suite.Analytics;
using SuperGear;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    #region 数据配置
    [Header("关卡配置")]
    [Tooltip("拖入所有关卡的LevelData（按索引顺序排列）")]
    public List<LevelData> allLevelDatas; // 存储所有关卡数据
    [HideInInspector] public int totalTargetGearsNumber = 0;       // 总目标齿轮数量（有效且为目标类型）
    [HideInInspector] public int activatedTargetGearsNumber = 0;   // 已激活（旋转）的目标齿轮数量
    [HideInInspector] public bool isLevelCompleted = false; // 标记当前关卡是否已完成
    [Header("UI引用")]
    public Button resetButton; // 重置按钮
    [Header("新手引导")]
    public GameObject IndicatorRotation; // 指引手指
    public GameObject RotationFingerUp; // 手指弯指示
    public GameObject RotationFingerDown; // 手指直指示
    public float fingerToggleInterval = 0.5f; //手指切换间隔（单位：秒）
    public GameObject RotationFingerUp2; // 手指弯指示
    public GameObject RotationFingerDown2; // 手指直指示
    private Coroutine fingerToggleCoroutine;// 协程引用，用于停止切换                                    
    private Coroutine fingerToggleCoroutine2;// 协程引用，用于停止切换
    public bool IsSecondState = false;
    [Header("手指移动配置")]
    public float fingerMoveSpeed = 0.5f; // 移动速度（值越小越慢）
    public float fingerMoveDistance = 50f; // 上下移动的总距离（像素）
    private Coroutine fingerMoveCoroutine; // 移动协程引用，用于停止
    private Vector3 fingerOriginalPos; // 手指初始位置，用于计算移动范围
    [Header("当前关卡")]
    [SerializeField]private int currentLevelIndex = 0; // 当前关卡索引
    public int CurrentLevelIndex => currentLevelIndex;
    #endregion

    #region 生命周期函数
    private void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            LoadLevelProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        resetButton?.onClick.AddListener(ResetLevelProgress);

        // 初始化时隐藏所有引导UI（防止重启Unity时显示残留）
        HideGuidance();
     }
    private void Update()
    {
        //一键胜利
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
        {
            GameStateManager.Instance.StopCountdown();
            // 显示通关面板
            UIManager.Instance.SetWinPanel(true);
            InputManager.Instance.SetInput(false);
            isLevelCompleted = true;
            CompleteTheLevel();

        }
#endif

        if (!isLevelCompleted)
        {
            CheckTheTargetNumber();
        }
       
    }
    private void OnDestroy()
    {
        IsSecondState = false;
    }

    #endregion

    #region 新手指引
    // 启动
    private void FirstTimeGuide()
    {
        // 仅在第0关（新手关）显示指引
        if (currentLevelIndex != 0) return;
        // 先隐藏可能存在的旧指引，避免重复显示
        HideGuidance();

        // 启动切换协程
        if (RotationFingerUp != null && RotationFingerDown != null)
        {
            fingerToggleCoroutine = StartCoroutine(ToggleFingerIndicators());
        }
    }
    private void SecondTimeGuide()
    {
        // 仅在第1关（新手关）显示指引
        if (currentLevelIndex != 1) return;
        // 先隐藏可能存在的旧指引，避免重复显示
        HideGuidance();

        // 启动切换协程
        if (RotationFingerUp != null && RotationFingerDown != null)
        {
            fingerToggleCoroutine = StartCoroutine(ToggleFingerIndicators2());
        }
    }
    // 手指图片间隔切换协程
    private IEnumerator ToggleFingerIndicators()
    {
        // 空引用保护：确保两个手指对象都已赋值
        if (RotationFingerUp == null || RotationFingerDown == null)
        {
            Debug.LogError("LevelManager：手指指示对象未赋值！");
            yield break;
        }

        // 初始状态：先显示弯曲手指，隐藏伸直手指
        RotationFingerUp.SetActive(true);
        RotationFingerDown.SetActive(false);

        // 循环切换
        while (true)
        {
            // 等待设定的间隔时间
            yield return new WaitForSeconds(fingerToggleInterval);

            // 切换显示状态（弯曲→伸直，或伸直→弯曲）
            RotationFingerUp.SetActive(!RotationFingerUp.activeSelf);
            RotationFingerDown.SetActive(!RotationFingerDown.activeSelf);

        }
    }
    private IEnumerator ToggleFingerIndicators2()
    {
        // 空引用保护：确保两个手指对象都已赋值
        if (RotationFingerUp2 == null || RotationFingerDown2 == null)
        {
            Debug.LogError("LevelManager：手指指示对象未赋值！");
            yield break;
        }

        // 初始状态：先显示弯曲手指，隐藏伸直手指
        RotationFingerUp2.SetActive(true);
        RotationFingerDown2.SetActive(false);

        // 循环切换
        while (true)
        {
            // 等待设定的间隔时间
            yield return new WaitForSeconds(fingerToggleInterval);

            // 切换显示状态（弯曲→伸直，或伸直→弯曲）
            RotationFingerUp2.SetActive(!RotationFingerUp2.activeSelf);
            RotationFingerDown2.SetActive(!RotationFingerDown2.activeSelf);

        }
    }

    // 指引手指上下滑动
    private IEnumerator MoveFingerUpAndDown(GameObject fingerTarget)
    {
        // 空引用保护
        if (fingerTarget == null)
        {
            Debug.LogError("LevelManager：移动的手指对象未赋值！");
            yield break;
        }

        // 记录初始位置，作为移动的基准点
        fingerOriginalPos = fingerTarget.transform.localPosition;
        // 计算上下两个端点位置（基于初始位置偏移）
        Vector3 topPos = fingerOriginalPos + new Vector3(0, 0,fingerMoveDistance / 2);
        Vector3 bottomPos = fingerOriginalPos - new Vector3(0, 0,fingerMoveDistance / 2);

        float moveTime = 0; // 用于计算移动进度

        while (true)
        {
            // 1. 从下端移动到上端
            while (moveTime < 1)
            {
                moveTime += Time.deltaTime *fingerMoveSpeed;
                // 平滑插值计算当前位置
                fingerTarget.transform.localPosition = Vector3.Lerp(bottomPos, topPos, moveTime);
                yield return null; // 等待下一帧
            }

            // 2. 重置进度，从上端移动到下端
            moveTime = 0;
            while (moveTime < 1)
            {
                moveTime += Time.deltaTime * fingerMoveSpeed;
                fingerTarget.transform.localPosition = Vector3.Lerp(topPos, bottomPos, moveTime);
                yield return null;
            }

            // 3. 重置进度，循环往复
            moveTime = 0;
        }
    }
    private IEnumerator MoveFingerUpOnly(GameObject fingerTarget)
    {
        // 空引用保护
        if (fingerTarget == null)
        {
            Debug.LogError("LevelManager：移动的手指对象未赋值！");
            yield break;
        }

        // 记录初始位置（作为单向滑动的起点）
        fingerOriginalPos = fingerTarget.transform.localPosition;
        // 计算向上端点位置（基于初始位置偏移）
        Vector3 topPos = fingerOriginalPos + new Vector3(0, 0, fingerMoveDistance / 2);

        float moveTime = 0; // 用于计算移动进度

        while (true) // 无限循环执行“滑动→复位→等待”
        {
            // 1. 单向向上滑动：从初始位置 → 上端点（平滑插值）
            moveTime = 0; // 重置进度（避免上一轮残留值）
            while (moveTime < 1)
            {
                moveTime += Time.deltaTime * fingerMoveSpeed;
                // 平滑插值计算当前位置（仅向上移动）
                fingerTarget.transform.localPosition = Vector3.Lerp(fingerOriginalPos, topPos, moveTime);
                yield return null; // 等待下一帧，保证滑动流畅
            }

            // 2. 到达上端点后：瞬间复位到初始位置（如需平滑复位，可替换为Lerp插值）
            fingerTarget.transform.localPosition = fingerOriginalPos;
            fingerTarget.SetActive(false);
            // 3. 关键：间隔0.5秒后，再执行下一次滑动
            yield return new WaitForSeconds(0.5f);
            fingerTarget.SetActive(true);
        }
    }
    //隐藏方法
    public void HideGuidance()
    {
        // 1. 隐藏指引手指（核心操作）
        if (IndicatorRotation != null)
        {
            IndicatorRotation.SetActive(false);
         
        }

        // 2. 停止手指切换协程（ToggleFingerIndicators 系列）
        if (fingerToggleCoroutine != null)
        {
            StopCoroutine(fingerToggleCoroutine);
            fingerToggleCoroutine = null;
        }
        if (fingerToggleCoroutine2 != null)
        {
            StopCoroutine(fingerToggleCoroutine2);
            fingerToggleCoroutine2 = null;
        }

        // 3. 关键修复：停止手指移动协程（MoveFingerUpOnly）
        if (fingerMoveCoroutine != null)
        {
            StopCoroutine(fingerMoveCoroutine);
            fingerMoveCoroutine = null; // 重置引用，避免重复停止
            Debug.Log("HideGuidance: 已停止 MoveFingerUpOnly 协程");
        }

        // 4. 隐藏其他手指指示
        RotationFingerUp?.SetActive(false);
        RotationFingerDown?.SetActive(false);
        RotationFingerUp2?.SetActive(false);
        RotationFingerDown2?.SetActive(false);
    }
    //第一关引导的第一步完成后触发第二步
    public void AfterRotation()
    {
        if (IndicatorRotation != null)
        {
            // 启动手指移动协程
            IndicatorRotation.SetActive(true);
            fingerMoveCoroutine = StartCoroutine(MoveFingerUpOnly(IndicatorRotation));
        }
        // 停止手指切换协程，避免隐藏后仍在运行
        if (fingerToggleCoroutine != null)
        {
            StopCoroutine(fingerToggleCoroutine);
            fingerToggleCoroutine = null;
        }

        // 处理手指指示（隐藏）
        if (RotationFingerUp != null)
        {
            RotationFingerUp.SetActive(false);
        }
        if (RotationFingerDown != null)
        {
            RotationFingerDown.SetActive(false);
        }
    }

    #endregion

    #region 通关成功失败条件
    //特效与通关检测(已废弃,特效现在由GearControl.PlayTargetGearEffects()直接处理)
    public void TargetGearEffect()
    {
        // 此方法已不再使用,保留只是为了兼容旧的事件引用
        // 实际的音效、震动、特效已移至GearControl.PlayTargetGearEffects()中处理
        // 避免重复播放和遍历所有齿轮的性能问题
    }

    //更新目标齿轮数
    public void CheckTargetNumber()
    {
        // 若当前关卡已完成，直接返回，避免重复处理
        if (isLevelCompleted)
            return;
        // 获取场景中所有齿轮组件
        GearControl[] allGears = UnityEngine.Object.FindObjectsOfType<GearControl>();

        // 场景中没有任何齿轮时，返回未完成
        if (allGears == null || allGears.Length == 0)
        {
            return;
        }
        int totalTargetGears = 0;// 总目标齿轮数量（有效且为目标类型）
        int activatedTargetGears = 0;   // 已激活（旋转）的目标齿轮数量

        // 遍历所有齿轮，统计目标齿轮总数及已激活数量
        foreach (GearControl gear in allGears)
        {
            // 跳过空引用或游戏对象未激活的齿轮（无效齿轮）
            if (gear == null || !gear.gameObject.activeInHierarchy)
                continue;

            // 仅处理目标类型齿轮
            if (gear.gearType == GearType.Target)
            {
                totalTargetGears++;

                // 检查该目标齿轮是否处于激活（旋转）状态
                if (gear.IsRotating)
                {
                    activatedTargetGears++;
                }
            }
        }
        // 没有目标齿轮时，返回未完成（根据业务需求调整）
        if (totalTargetGears == 0)
        {
            return;
        }

        activatedTargetGearsNumber = activatedTargetGears;
        totalTargetGearsNumber = totalTargetGears;

        return;
    }

    //通关检测  检测场景中所有目标齿轮是否全部被激活
    public void CheckTheTargetNumber()
    {
        // 若当前关卡已完成，直接返回，避免重复处理
        if (isLevelCompleted)
            return;
        // 获取场景中所有齿轮组件
        GearControl[] allGears = UnityEngine.Object.FindObjectsOfType<GearControl>();

        // 场景中没有任何齿轮时，返回未完成
        if (allGears == null || allGears.Length == 0)
        {
            return;
        }
        int totalTargetGears = 0;// 总目标齿轮数量（有效且为目标类型）
        int activatedTargetGears = 0;   // 已激活（旋转）的目标齿轮数量

        // 遍历所有齿轮，统计目标齿轮总数及已激活数量
        foreach (GearControl gear in allGears)
        {
            // 跳过空引用或游戏对象未激活的齿轮（无效齿轮）
            if (gear == null || !gear.gameObject.activeInHierarchy)
                continue;

            // 仅处理目标类型齿轮
            if (gear.gearType == GearType.Target)
            {
                totalTargetGears++;

                // 检查该目标齿轮是否处于激活（旋转）状态
                if (gear.IsRotating)
                {
                    activatedTargetGears++;
                }
            }
        }
        // 没有目标齿轮时，返回未完成（根据业务需求调整）
        if (totalTargetGears == 0)
        {
            return ;
        }
        // 所有目标齿轮都激活时返回true，否则返回false
        bool allActivated = totalTargetGears == activatedTargetGears;
        activatedTargetGearsNumber = activatedTargetGears;
        totalTargetGearsNumber = totalTargetGears;
        if (allActivated)
        {
            isLevelCompleted = true;
            GameStateManager.Instance.StopCountdown();
            StartCoroutine(YouWin(2f));
        }       
        return;
    }
    private IEnumerator YouWin(float time)
    {
        yield return new WaitForSeconds(time);
        GameStateManager.Instance.StopCountdown();// 暂停游戏 + 停止倒计时
        UIManager.Instance.SetWinPanel(true);  // 显示通关面板
        InputManager.Instance.SetInput(false);
        CompleteTheLevel();
    }
    #endregion

    #region 关卡加载
    public void LoadCurrentLevel()
    {
        LoadLevel(currentLevelIndex);
    }
    public void LoadLevel(int targetLevelIndex)
    {

        int levelNumber = currentLevelIndex + 1;
        string missionName = $"main_level_{levelNumber}";
        int missionID = levelNumber;
        
        LionAnalytics.MissionStarted(
            missionType: "main",
            missionName: $"main_level_{levelNumber}",
            missionID: levelNumber,
            missionAttempt: null, // 不需要尝试次数时传null
            additionalData: null,
            isGamePlay: null
        );


        // 重置通关状态，允许新关卡检测
        isLevelCompleted = false;
        totalTargetGearsNumber = 0;
        activatedTargetGearsNumber = 0;
        UIManager.Instance.isWarning = false;

        // 校验索引合法性 + 空值保护
        if (allLevelDatas == null || allLevelDatas.Count == 0 )
        {
            Debug.LogError("LevelManager：allLevelDatas未赋值或为空！");
            return;
        }
        targetLevelIndex = (targetLevelIndex) % allLevelDatas.Count;
        if (targetLevelIndex < 0 || targetLevelIndex >= allLevelDatas.Count)
        {
            Debug.LogError($"LevelManager：关卡索引{targetLevelIndex}无效！");
            return;
        }

        // 获取目标关卡数据并更新索引
        LevelData targetLevelData = allLevelDatas[targetLevelIndex];

        // 初始化生成器
        BlockGenerate.Instance?.InitializeBlockGenerateData(targetLevelData);
        ReceivingBinGenerate.Instance?.InitReceivingBinGenerateData(targetLevelData);
        TerrainChange.Instance?.ChangeToPresetMaterial(targetLevelData._terrain);
        UIManager.Instance?.InitializeHints();
        UIManager.Instance.StartCameraSmoothTransition(targetLevelData.CameraSize);

        // 关键：加载关卡后，通知UI更新关卡文本
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLevelText();
            UIManager.Instance.UpdatelevelObjectiveGear();
        }

        Debug.Log($"LevelManager：成功加载关卡 {targetLevelData.LevelIndex}");
        
        SaveLevelProgress();
        FirstTimeGuide();
        SecondTimeGuide(); ;
        if(currentLevelIndex<=9)
        {
            UIManager.Instance.StartObjectiveHint();
        }
}

    public void CompleteTheLevel()
    {
        int completedLevelIndex = currentLevelIndex; // 记录当前完成的关卡
        currentLevelIndex++; // 解锁下一关
        SaveLevelProgress();
        UIManager.Instance.SetHudPanel(false);


        // 如果完成的是新手关（第0关），隐藏指引
        if (completedLevelIndex == 0)
        {
            HideGuidance();
            //Debug.LogError($" 如果完成的是新手关（第0关），隐藏指引");
        }
        if (completedLevelIndex ==1)
        {
            HideGuidance();
           // Debug.LogError($" 如果完成的是新手关（第1关），隐藏指引");
        }

        // 假设通关时奖励为40金币，需构造Reward对象
        var rewardProduct = new LionStudios.Suite.Analytics.Product();
        rewardProduct.virtualCurrencies = new List<VirtualCurrency> { new VirtualCurrency("coins", "gold", 40) };
        Reward reward = new Reward(rewardProduct);
        int levelNumber = currentLevelIndex + 1;
        LionAnalytics.MissionCompleted(
            missionType: "main",
            missionName: $"main_level_{levelNumber}",
            missionID: levelNumber,
            missionAttempt: null,
            additionalData: null,
            reward: reward,
            isGamePlay: null
        );
    }
    public void ResetLevelCompletionStatus()
    {
        isLevelCompleted = false;
        // 同时重置目标齿轮计数（避免上一关数据残留）
        totalTargetGearsNumber = 0;
        activatedTargetGearsNumber = 0;
        Debug.Log($"LevelManager：已重置通关状态和目标齿轮计数");
    }
    #endregion

    #region 数据持久化

    private void SaveLevelProgress()
    {
        // 用唯一键存储当前关卡索引（确保与其他数据不冲突）
        PlayerPrefs.SetInt("CurrentLevelIndex", currentLevelIndex);
        PlayerPrefs.Save(); // 立即写入磁盘（可选，PlayerPrefs会在应用退出时自动保存）
        Debug.Log($"已保存关卡进度：{currentLevelIndex}");
    }

    private void LoadLevelProgress()
    {
        // 如果有保存的数据，则加载；否则使用默认值0
        if (PlayerPrefs.HasKey("CurrentLevelIndex"))
        {
            currentLevelIndex = PlayerPrefs.GetInt("CurrentLevelIndex");
            // 同步UI显示的关卡索引（确保与当前关卡索引一致）
           
            Debug.Log($"已加载关卡进度：{currentLevelIndex}");
        }
        else
        {
            // 首次运行游戏，使用初始值
            currentLevelIndex = 0;
            ;
            Debug.Log("无历史进度，使用初始关卡");
        }
    }
    private void ResetLevelProgress()
    {
        // 1. 重置关卡索引到第一关（索引0）
        currentLevelIndex = 0;
        GoldCoinManager.Instance.GoldCoin=100;

        // 2. 清除本地保存的进度数据
        PlayerPrefs.DeleteKey("CurrentLevelIndex");
        SaveLevelProgress(); // 保存重置后的初始状态
        LoadLevelProgress();
    }
    #endregion
}