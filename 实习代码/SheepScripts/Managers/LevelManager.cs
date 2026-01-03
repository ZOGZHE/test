using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WoolyPath
{
    // 关卡管理器（单例模式），负责关卡生命周期、羊和收集器管理、交互事件处理及进度维护
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }
        // 添加关卡数据变更事件
        // public static event System.Action<LevelData> OnLevelDataChanged;

        #region 配置参数（Inspector可配置）
        [Header("关卡基础数据")]
        [SerializeField] private LevelData[] levels;
        private int currentAllowedTier = 1;

        [Header("场景层级管理")]
        // 羊对象的父节点（用于场景对象归类）
        [SerializeField] private Transform sheepGridParent;
        // 收集器对象的父节点（用于场景对象归类）
        [SerializeField] private Transform collectorsParent;

        [Header("预制体引用")]
        // 羊对象的预制体
        [SerializeField] private GameObject sheepPrefab;
        // 收集器对象的预制体
        [SerializeField] private GameObject collectorPrefab;

        [Header("生成器引用")]
        // 羊批量生成器（优先使用此生成逻辑）
        [SerializeField] private SheepSpawner sheepSpawner;
        #endregion

        #region 状态数据（运行时维护）
        // 当前加载关卡的配置数据
        public LevelData CurrentLevelData { get; private set; }

        // 当前关卡的索引（用于关卡切换与进度记录）
        public int CurrentLevelIndex { get; private set; }

        // 存储已完成的最高关卡索引（核心进度变量）
        [SerializeField] private int _highestCompletedLevel = 0;
        [SerializeField] private int _actualHighestCompletedLevel = 0;
        public int HighestCompletedLevel => _highestCompletedLevel;
        public int ActualHighestCompletedLevel => _actualHighestCompletedLevel;
        // 定义实际关卡总数常量（方便修改）
        [SerializeField] public  int ACTUAL_LEVEL_COUNT = 20;


        // 当前场景中活跃的羊对象列表（用于统一清理与状态查询）
        private List<SheepController> activeSheep = new List<SheepController>();
        // 当前场景中活跃的收集器对象列表（用于统一清理与状态查询）
        private List<CollectorPlate> activeCollectors = new List<CollectorPlate>();
        #endregion


        #region 初始化逻辑
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                AutoFindSpawner(); // 自动查找羊生成器（处理Inspector未配置的情况）
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject); // 销毁重复实例，保证单例唯一性
            }
            GetActualLevelIndex();
            // 启动时加载之前保存的进度
            LoadProgress();
        }

        private void Start()
        {
            StartCoroutine(DelayedStart());
            GetActualLevelIndex();
        }

        private void Update()
        {
            GetActualLevelIndex();
        }
        private void GetActualLevelIndex()
        {
            _actualHighestCompletedLevel=(_highestCompletedLevel ) % ACTUAL_LEVEL_COUNT;
        }


        // 自动查找羊生成器，当Inspector中未手动配置sheepSpawner时，自动在场景中查找
        private void AutoFindSpawner()
        {
            if (sheepSpawner == null)
            {
                sheepSpawner = FindObjectOfType<SheepSpawner>();
            }
        }

        // 延迟启动协程：1.等待一帧确保所有组件Awake/Start执行完毕；2.额外延迟0.5秒确保外部配置完成
        private IEnumerator DelayedStart()
        {
            yield return null; // 等待当前帧所有Start执行完成
            yield return new WaitForSeconds(0.5f); // 额外延迟保证配置就绪

            // Debug.Log($"[LevelManager] 延迟启动 - 总关卡数量: {(levels == null ? "0" : levels.Length.ToString())}");

            // 校验关卡数据有效性
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("[LevelManager] 错误：未配置任何关卡数据！请检查GameSceneSetup组件");
                yield break;
            }
        }
        #endregion


        #region 外部配置接口（供其他组件调用）
        // 设置关卡数据数组，由GameSceneSetup组件调用，用于外部注入关卡配置
        // 参数levelArray：完整的关卡配置数据数组
        public void SetLevels(LevelData[] levelArray)
        {
            levels = levelArray;
        }

        // 设置预制体引用，由GameSceneSetup组件调用，用于外部注入预制体资源
        // 参数sheepPrefabRef：羊对象的预制体引用；参数collectorPrefabRef：收集器对象的预制体引用
        public void SetPrefabs(GameObject sheepPrefabRef, GameObject collectorPrefabRef)
        {
            sheepPrefab = sheepPrefabRef;
            collectorPrefab = collectorPrefabRef;
        }

        // 设置父节点引用，由GameSceneSetup组件调用，用于外部指定对象归类父节点
        // 参数sheepParent：羊对象的父节点；参数collectorsParent：收集器对象的父节点
        public void SetParentReferences(Transform sheepParent, Transform collectorsParent)
        {
            sheepGridParent = sheepParent;
            this.collectorsParent = collectorsParent;
        }
        #endregion


        #region 关卡核心管理逻辑
        // 加载下一关（关键修改：最后一关后循环到第一关）
        public void LoadNextLevel()
        {
            // 1. 下一关索引 = 当前原始索引 + 1（无限递增，如20→21→22...）
            int nextLevelIndex = CurrentLevelIndex + 1;
            // 2. 直接加载（模运算在LoadLevel内部处理，无需判断是否小于levels.Length）
            LoadLevel(nextLevelIndex);

            // 可选：打印日志验证映射关系（如21→1，22→2）
            int mapped = nextLevelIndex % ACTUAL_LEVEL_COUNT;
            Debug.Log($"[LevelManager] 加载下一关：原始索引{nextLevelIndex} → 映射数据源{mapped + 1}关");
        }

        // 加载指定索引的关卡
        // 参数levelIndex：目标关卡的索引（从0开始计数）
        public void LoadLevel(int levelIndex)
        {
            // 1. 索引合法性校验（允许无限大的索引，只需模20取数据源）
            if (levelIndex < 0)
            {
                Debug.LogError($"[LevelManager] 错误：无效的关卡索引 {levelIndex}");
                return;
            }

            // 2. 关键修改：目标关卡索引模20，获取对应数据源（实现无限循环）
            int mappedLevelIndex = levelIndex % ACTUAL_LEVEL_COUNT;
            // 防错：若模后索引超出levels数组（理论上不会，因ACTUAL_LEVEL_COUNT=levels.Length）
            mappedLevelIndex = Mathf.Clamp(mappedLevelIndex, 0, levels.Length - 1);

            // 3. 同步当前关卡索引（原始索引，如21、41，用于进度记录）
            CurrentLevelIndex = levelIndex;
            // 4. 加载正确的关卡数据（用模后的索引取数据源）
            CurrentLevelData = levels[mappedLevelIndex];

            // 5. 后续原有逻辑（清理关卡、刷新地形等）...
            SheepSpawner.instance.totalSheepForLevel = CurrentLevelData.totalSheepCount;
            ClearCurrentLevel();
            TerrainGeneration.Instance.RefreshTerrain();
            if (ConveyorBelt.Instance != null)
            {
                ConveyorBelt.Instance.OnLevelChanged();
            }
            ConveyorBelt.Instance.RefreshFromLevelData();
            SetupLevel(CurrentLevelData);
            GameManager.Instance.ChangeGameState(GameState.Loading);
            LoadingMenu.instance.StartLoading();
        }

        // 清理当前关卡的所有活跃对象，包括：所有羊对象、所有收集器对象，并重置层级限制
        private void ClearCurrentLevel()
        {
            // 清理所有活跃羊对象
            foreach (var sheep in activeSheep)
            {
                if (sheep != null) DestroyImmediate(sheep.gameObject);
            }
            activeSheep.Clear();

            // 清理所有活跃收集器对象
            foreach (var collector in activeCollectors)
            {
                if (collector != null) DestroyImmediate(collector.gameObject);
            }
            activeCollectors.Clear();

            ConveyorBelt.Instance.ClearAllWools();
            // 重置层级交互限制
            currentAllowedTier = 1;
        }

        // 根据关卡配置数据初始化关卡内容，顺序：生成羊 → 生成收集器 → 配置传送带（若存在）
        // 参数levelData：当前要初始化的关卡配置数据
        private void SetupLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogError("[LevelManager] 错误：关卡初始化失败，关卡数据为空");
                return;
            }

            // 生成羊（优先使用生成器，生成器不存在时使用传统网格生成）
            if (sheepSpawner != null)
            {
                sheepSpawner.StartLevelSpawn(levelData);
            }

            // 生成收集器
            SetupCollectors(levelData.collectors);
        }
        #endregion


        #region 收集器管理逻辑
        // 根据收集器配置数据初始化收集器
        // 参数collectors：收集器配置数据数组（包含每个收集器的位置、层级、目标等）
        private void SetupCollectors(CollectorData[] collectors)
        {
            if (collectors == null) return;

            // 遍历配置数据，生成每个收集器
            foreach (var collectorData in collectors)
            {
                // 实例化收集器（使用配置中的自定义预制体，父节点设为collectorsParent）
                GameObject collectorObj = Instantiate(
                    collectorData.Prefab,
                    collectorData.position,
                    Quaternion.identity,
                    collectorsParent
                );
                collectorObj.SetActive(true); // 强制激活，确保初始状态可见

                // 初始化收集器并加入活跃列表
                var collectorPlate = collectorObj.GetComponent<CollectorPlate>();
                if (collectorPlate != null)
                {
                    collectorPlate.Initialize(collectorData);
                    activeCollectors.Add(collectorPlate);
                }
            }
        }

        // 从活跃收集器列表中移除指定收集器
        // 参数collector：要移除的收集器对象
        public void RemoveCollector(CollectorPlate collector)
        {
            if (activeCollectors.Contains(collector))
            {
                activeCollectors.Remove(collector);
            }
        }

        // 启用指定层级的所有收集器，用于层级解锁时激活对应层级的收集器
        // 参数tier：要启用的目标层级
        private void EnableCollectorsByTier(int tier)
        {
            foreach (var collector in activeCollectors)
            {
                // 仅启用目标层级且存在的收集器
                if (collector != null && collector.GetData().Tier == tier)
                {
                    collector.UnlockCollector(); // 调用收集器自身的解锁方法
                    collector.UpdateVisuals(true); // 更新收集器视觉状态（解锁后）
                    collector.OnTierChange(); // 通知收集器层级变化
                }
            }
        }
        #endregion


        #region 进度与层级解锁逻辑
        // 获取当前允许交互的最高层级，供外部组件查询层级限制
        // 返回值：当前可交互的最高层级
        public int GetCurrentAllowedTier()
        {
            return currentAllowedTier;
        }

        // 检查指定层级的所有收集器是否全部完成，用于判断是否满足层级解锁条件
        // 参数targetTier：要检查的目标层级
        // 返回值：全部完成返回true，否则返回false（无收集器时视为完成）
        private bool CheckAllCollectorsInTierCompleted(int targetTier)
        {
            // 筛选目标层级的有效收集器（存在且激活）
            var tierCollectors = activeCollectors
                .Where(collector => collector != null
                               && collector.GetData().Tier == targetTier
                               && collector.gameObject.activeInHierarchy)
                .ToList();

            // 无收集器时视为当前层级完成
            if (tierCollectors.Count == 0)
            {
                Debug.Log($"[LevelManager] 层级 {targetTier} 无有效收集器，自动判定为完成");
                return true;
            }

            // 检查该层级所有收集器是否都已完成
            bool allCompleted = tierCollectors.All(collector => collector.IsComplete());
            if (allCompleted)
            {
                Debug.Log($"[LevelManager] 层级 {targetTier} 所有收集器已完成");
            }
            return allCompleted;
        }

        // 检查当前关卡是否完全完成，判定条件：所有收集器（无论层级）均已完成
        // 返回值：关卡完全完成返回true，否则返回false
        public bool IsLevelComplete()
        {
            foreach (var collector in activeCollectors)
            {
                // 存在未完成的收集器，关卡未完成
                if (collector != null && !collector.IsComplete())
                {
                    return false;
                }
            }
            return true;
        }
        #endregion


        #region 交互事件处理逻辑
        // 处理羊对象被点击的事件，由SheepController调用，触发羊的点击响应
        // 参数sheep：被点击的羊对象
        public void OnSheepClicked(SheepController sheep)
        {
            // 仅处理活跃状态的羊
            if (sheep != null && sheep.IsActive())
            {
                sheep.OnClicked(); // 调用羊自身的点击处理逻辑
            }
        }

        // 处理收集器完成的事件，由CollectorPlate调用，触发层级解锁与胜利条件检查
        // 参数collector：已完成的收集器对象
        public void OnCollectorCompleted(CollectorPlate collector)
        {
            if (collector == null) return;

            int completedTier = collector.GetData().Tier;

            // Lion Analytics: Log mission step when completing a collector
            // Lion分析：完成收集器时记录任务步骤
            if (LionSDKManager.Instance != null)
            {
                string stepName = $"collector_tier_{completedTier}_complete";
                LionSDKManager.Instance.LogMissionStep(CurrentLevelIndex, stepName);
            }

            // 原有层级解锁逻辑
            if (CheckAllCollectorsInTierCompleted(completedTier))
            {
                currentAllowedTier = completedTier + 1;
                EnableCollectorsByTier(currentAllowedTier);
            }

            // 检查当前关卡是否完全完成
            if (IsLevelComplete())
            {
                // 更新进度
                if (CurrentLevelIndex >= _highestCompletedLevel)
                {
                    _highestCompletedLevel = CurrentLevelIndex;
                    SaveProgress();
                }

                // 通知GameManager关卡完成
                GameManager.Instance.CompleteLevel();
            }
        }
        #endregion


        #region 辅助功能（存档、解锁判断、编辑器辅助）
        // 实现具体的进度保存逻辑（存储到PlayerPrefs）
        public void SaveProgress()
        {
            // 定义唯一键名（避免与其他数据冲突，建议加游戏/模块前缀）
            const string PROGRESS_KEY = "WoolyPath_HighestCompletedLevel";
            // 保存「已完成的最高关卡索引」到本地
            PlayerPrefs.SetInt(PROGRESS_KEY, _highestCompletedLevel);
            // 强制写入磁盘（确保数据立即保存，避免程序崩溃丢失）
            PlayerPrefs.Save();
            Debug.Log($"[LevelManager] 进度已保存：最高完成关卡 {_highestCompletedLevel + 1}");
        }

        // 在 LevelManager 的“状态数据”区域添加公开方法
        public int GetHighestCompletedLevel()
        {
            return _highestCompletedLevel;
        }

        public void reProgress()
        {
            const string PROGRESS_KEY = "WoolyPath_HighestCompletedLevel";

            // 1. 删除旧存档（彻底清除，避免缓存干扰）
            PlayerPrefs.DeleteKey(PROGRESS_KEY);

            // 2. 重置内存中的进度变量为-1，表示没有完成任何关卡
            _highestCompletedLevel = 0;

            // 3. 写入新的存档值
            PlayerPrefs.SetInt(PROGRESS_KEY, 0);
            PlayerPrefs.Save(); // 强制写入磁盘

            // 调试：打印内存和存档的实际值
            int savedValue = PlayerPrefs.GetInt(PROGRESS_KEY, -999);
            Debug.Log($"[LevelManager] 重置后 → 内存索引：{_highestCompletedLevel}，存档值：{savedValue}（-1表示未完成任何关卡）");
        }

        // 从本地加载进度
        public void LoadProgress()
        {
            const string PROGRESS_KEY = "WoolyPath_HighestCompletedLevel";

            // 读取存档（默认值-1，表示没有完成任何关卡）
            int loadedValue = PlayerPrefs.GetInt(PROGRESS_KEY, 0);
            _highestCompletedLevel = loadedValue;

            // 防错：若存档索引超过关卡总数，重置为最后一关
            if (levels != null && _highestCompletedLevel >= levels.Length)
            {
                _highestCompletedLevel = levels.Length;
            }

            Debug.Log($"[LevelManager] 进度加载 → 最高完成关卡索引：{_highestCompletedLevel}（-1表示未完成任何关卡，0表示完成第1关）");
        }

        // 基于保存的进度判断关卡是否解锁
        public bool IsLevelUnlocked(int levelIndex)
        {
            // 第一关（索引0）始终解锁
            if (levelIndex == 0) return true;

            // 其他关卡：只有完成了前一关才能解锁
            // 例如：要解锁第2关（索引1），需要完成第1关（_highestCompletedLevel >= 0）
            return levelIndex <= _highestCompletedLevel + 1;
        }

        public void OnPassTheLevel()
        {
            // Math.Max(a, b) 直接返回 a 和 b 中的较大值，替代原三元运算符判断
            _highestCompletedLevel = Math.Max(_highestCompletedLevel, GameManager.Instance.CurrentLevel + 1);
            GoldCoin.Instance.AddGold(10);
            SaveProgress();
        }
        #endregion
    }
}