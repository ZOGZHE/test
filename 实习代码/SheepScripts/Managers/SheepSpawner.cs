using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WoolyPath
{
    // 羊群生成管理器：负责动态生成、管理和补充羊群，含网格管理、关卡适配、自动补充
    public class SheepSpawner : MonoBehaviour
    {
        #region 核心配置与实例（单例、序列化字段、事件）
        public static SheepSpawner instance { get; private set; }

        [Header("开局保护设置")]
        [SerializeField] private float startProtectionTime = 3f; // 开局保护时间（秒，期间羊不可点击）
        private bool PassStartProtected = false; // 是否通过开局保护状态

        [Header("本地羊颜色权重生成设置")]
        [SerializeField] private SheepPrefabsWeight[] sheepPrefabsWeight; // 本地羊预制体颜色权重配置（含预制体、颜色、权重）
        [Tooltip("黑羊遮罩权重")]
        [SerializeField] private BlackSheepMask  _blackSheepMask;

        [Header("是否使用本地颜色权重配置")]
        public bool UseLocalSheepPrefabsWeight = true; // 本地配置优先级高于关卡配置

        [Tooltip("是否使用数量生成")]
        public bool UseQuantity = true; // 是否按数量配置生成羊

        [Header("生成设置")]
        [SerializeField] private Transform sheepParent; // 所有羊的父节点（用于Hierarchy层级管理）
        // [SerializeField] private int maxSheepOnGrid = 25; // 网格上同时显示的最大羊数量（不超过网格总容量）
        [SerializeField] public int totalSheepForLevel = 100; // 关卡总羊数（可超过网格显示数量）

        [Header("网格配置")]
        private Vector2Int gridSize;
        private Vector2 gridSpacing;
        private Vector3 gridStartPosition;

        [Header("列移动设置")]
        [SerializeField] private float columnMoveSpeed = 3f; // 整列羊移动速度
        [SerializeField] private AnimationCurve columnMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 移动平滑曲线
        [SerializeField] private float moveDelayBetweenSheep = 0.01f; // 羊之间的移动间隔（避免重叠）

        [Header("补充设置")]
        //[SerializeField] private float spawnDelay = 0.5f; // 单只羊生成间隔时间（秒，预留）

        // 羊数量变化事件（参数：当前活跃数、关卡总数，供外部更新UI）
        public System.Action<int, int> OnSheepCountChanged;
        #endregion

        #region 管理集合与状态标记
        // 当前活跃的羊控制器列表（记录网格上所有羊）
        private List<SheepController> activeSheep = new List<SheepController>();
        // 可用的网格位置列表（记录未被占用的网格坐标）
        private List<Vector2Int> availableGridPositions = new List<Vector2Int>();
        // 已生成的总羊数（不超过关卡总羊数限制）
        private int totalSpawnedSheep = 0;
        // 生成状态标记（防止重复触发生成协程）
        private bool isSpawning = false;
        // 当前关卡数据（存储关卡专属配置）
        private LevelData currentLevelData;
        // 记录下一个要生成的位置索引
        private int currentSpawnIndex = 0;

        // 按数量生成的配置项计数字典（key=配置项，value=已生成数量）
        private Dictionary<SheepPrefabsWeight, int> _quantityConfigSpawnedCount;
        // 按列存储羊的队列（x为列索引，队列内按y轴顺序排列）
        private Dictionary<int, Queue<SheepController>> columnSheepQueues = new Dictionary<int, Queue<SheepController>>();


        // 列级移动锁：key=列索引，value=是否正在移动（true=锁定，禁止新移动）
        private Dictionary<int, bool> isColumnMoving = new Dictionary<int, bool>();
        // 跟踪黑羊是否已变换的字典（key=列索引，value=是否变换）
        //private Dictionary<int, bool> blackSheepTransformed = new Dictionary<int, bool>();
        #endregion

        #region Unity生命周期方法
        // Unity唤醒阶段：初始化网格位置、列队列、数量计数字典
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
            InitializeGridPositions(); // 初始化可用网格位置
            InitializeColumnQueues(); // 初始化列队列
            _quantityConfigSpawnedCount = new Dictionary<SheepPrefabsWeight, int>(); // 初始化数量计数字典
        }

        // Unity启动阶段：注册全局事件、启动开局保护计时器
        private void Start()
        {
            GameEvents.OnSheepClicked += OnSheepClicked; // 注册羊点击事件
            GameEvents.OnSheepMoveCompleted += OnSheepRemoved; // 注册羊移除事件
            StartCoroutine(StartProtectionTimer()); // 启动开局保护计时器
        }

        private void Update()
        {
            UpdateVisual();
        }

        // Unity销毁阶段：取消全局事件注册、停止所有协程
        private void OnDestroy()
        {
            GameEvents.OnSheepClicked -= OnSheepClicked; // 取消羊点击事件注册
            GameEvents.OnSheepMoveCompleted -= OnSheepRemoved; // 取消羊移除事件注册
            StopAllCoroutines(); // 停止所有协程
        }
        #endregion

        #region 开局保护与视觉更新
        // 开局保护计时器协程
        private IEnumerator StartProtectionTimer()
        {
            PassStartProtected = false;
            UpdateAllColumnsClickable();

            yield return new WaitForSeconds(startProtectionTime);

            PassStartProtected = true;
            // 保护结束后恢复各列的正确可点击状态
            UpdateAllColumnsClickable();
        }

        public void UpdateVisual()
        {
            foreach (var sheep in activeSheep)
            {
                sheep.UpdateVisualStateWithProtection();
            }
        }
        #endregion

        #region 列队列与可点击状态管理
        // 初始化列羊队列字典（含移动锁、黑羊变换状态）
        private void InitializeColumnQueues()
        {
            columnSheepQueues.Clear();
           // blackSheepTransformed.Clear();
            isColumnMoving.Clear(); // 清空移动锁字典
            for (int x = 0; x < gridSize.x; x++)
            {
                columnSheepQueues[x] = new Queue<SheepController>(); // 初始化每列队列
               // blackSheepTransformed[x] = false; // 初始化黑羊未变换
                isColumnMoving[x] = false; // 初始化列未锁定
            }
        }

        // 更新列内羊的可点击状态（处理列首黑羊变换、控制仅列首可点击）
        private void UpdateColumnClickable(int columnIndex, Queue<SheepController> columnQueue)
        {
            if (columnQueue.Count > 0)
            {
                var frontSheep = columnQueue.Peek(); // 获取列首羊
                // 列首为黑羊且未变换时，随机转为其他颜色并标记
                if (frontSheep != null && frontSheep.sheepData.isblackMasked )
                {
                    frontSheep.RemoveBlackMaskToOtherSheep();
                    //blackSheepTransformed[columnIndex] = true;
                }

                var sheepArray = columnQueue.ToArray(); // 队列转数组（方便遍历）
                for (int i = 0; i < sheepArray.Length; i++)
                {
                    // 仅列首羊可点击（i==0），且非移动中、非保护期
                    bool canClick = (i == 0) && PassStartProtected;
                    sheepArray[i].SetClickableByLogic(canClick); // 设置可点击状态'
                }
            }
           // else blackSheepTransformed[columnIndex] = false; // 队列为空时重置黑羊变换状态
        }

        // 更新所有列的羊可点击状态（批量调用单列更新）
        public void UpdateAllColumnsClickable()
        {
            foreach (var kvp in columnSheepQueues)
            {
                UpdateColumnClickable(kvp.Key, kvp.Value);
            }
        }

        // 解锁列（清除移动锁，更新可点击状态）
        private void UnlockColumn(int columnIndex)
        {
            if (isColumnMoving.ContainsKey(columnIndex))
            {
                isColumnMoving[columnIndex] = false; // 清除移动锁
                if (columnSheepQueues.ContainsKey(columnIndex))
                    UpdateColumnClickable(columnIndex, columnSheepQueues[columnIndex]); // 更新可点击状态
            }
        }
        #endregion

        #region 羊点击与移动处理
        // 羊点击事件处理（移除列首羊、启动列移动、更新状态）
        private void OnSheepClicked(SheepController sheep, Vector3 position)
        {
            if (!PassStartProtected) { Debug.Log($"[SheepSpawner] 开局保护中，点击被拒绝"); return; } // 保护期拒绝点击

            Vector2Int gridPos = sheep.GetGridPosition();
            int column = gridPos.x; // 获取羊所在列

            if (columnSheepQueues.ContainsKey(column))
            {
                var columnQueue = columnSheepQueues[column];
                bool isColumnLocked = isColumnMoving.ContainsKey(column) && isColumnMoving[column]; // 列是否锁定

                // 列锁定或羊移动中，拒绝点击
                if (isColumnLocked || sheep._isMoving)
                {
                    return;
                }

                // 验证是否为列首且可点击
                if (columnQueue.Count > 0 && columnQueue.Peek() == sheep)
                {
                    isColumnMoving[column] = true; // 锁定列
                    sheep._isMoving = true; // 标记羊开始移动
                    columnQueue.Dequeue(); // 从队列移除列首羊
                    UpdateColumnClickable(column, columnQueue); // 更新列可点击状态
                    StartCoroutine(MoveColumnSheepForward(column, columnQueue)); // 启动列移动
                }
                else Debug.LogWarning($"[SheepSpawner] 无效点击 - 不是列首或不可点击");
            }
        }

        // 整列羊向前移动协程（填补列首羊移除后的空缺）
        private IEnumerator MoveColumnSheepForward(int columnIndex, Queue<SheepController> columnQueue)
        {
            if (columnQueue == null) { Debug.LogError($"[SheepSpawner] 列 {columnIndex} 队列为null"); UnlockColumn(columnIndex); yield break; }

            var sheepInColumn = columnQueue.ToArray(); // 队列快照（避免移动中修改）
            if (sheepInColumn.Length == 0) { UnlockColumn(columnIndex); yield break; }

            try
            {
                // 预先标记所有羊为移动中
                foreach (var sheep in sheepInColumn)
                {
                    if (sheep != null) sheep._isMoving = true;
                }

                // 依次移动每只羊
                for (int i = 0; i < sheepInColumn.Length; i++)
                {
                    var sheep = sheepInColumn[i];
                    if (sheep == null || sheep.gameObject == null) continue; // 跳过空引用

                    Vector2Int oldGridPos = sheep.GetGridPosition();
                    Vector2Int newGridPos = new Vector2Int(columnIndex, oldGridPos.y - 1); // 新位置（y轴-1，向前移动）

                    if (newGridPos.y < 0) { Debug.LogError($"[SheepSpawner] 列 {columnIndex} 羊的新位置 {newGridPos} 无效"); continue; }

                    Vector3 newWorldPos = GridToWorldPosition(newGridPos); // 网格坐标转世界坐标
                    UpdateGridPosition(oldGridPos, newGridPos); // 更新网格位置占用状态
                    StartCoroutine(MoveSheepToTarget(sheep, newWorldPos)); // 启动单羊移动
                    sheep.SetGridPosition(newGridPos); // 更新羊的网格位置数据
                }
            }
            finally
            {
                UnlockColumn(columnIndex); // 无论是否异常，最终解锁列
            }
        }

        // 更新网格位置占用状态（释放旧位置、占用新位置）
        private void UpdateGridPosition(Vector2Int oldPos, Vector2Int newPos)
        {
            if (!availableGridPositions.Contains(oldPos)) availableGridPositions.Add(oldPos); // 释放旧位置
            if (availableGridPositions.Contains(newPos)) availableGridPositions.Remove(newPos); // 占用新位置
        }

        // 单只羊平滑移动到目标世界位置协程（带动画曲线）
        private IEnumerator MoveSheepToTarget(SheepController sheep, Vector3 targetPos)
        {
            if (sheep == null || sheep.gameObject == null) yield break;

            Vector3 startPos = sheep.transform.position;
            float moveDistance = Vector3.Distance(startPos, targetPos);

            if (moveDistance < 0.01f) // 距离过近，直接到位
            {
                sheep.transform.position = targetPos;
                sheep._isMoving = false;
                yield break;
            }

            float totalTime = moveDistance / columnMoveSpeed; // 预估移动时间
            float timer = 0f;
            sheep._isMoving = true; // 标记羊移动中

            try
            {
                // 按动画曲线平滑移动
                while (timer < totalTime)
                {
                    if (sheep == null || sheep.gameObject == null) yield break; // 羊被销毁则退出

                    timer += Time.deltaTime;
                    float progress = Mathf.Clamp01(timer / totalTime);
                    float curveProgress = columnMoveCurve.Evaluate(progress);
                    sheep.transform.position = Vector3.Lerp(startPos, targetPos, curveProgress);
                    yield return null;
                }

                if (sheep != null && sheep.gameObject != null) sheep.transform.position = targetPos; // 确保最终位置准确
            }
            finally
            {
                if (sheep != null) sheep._isMoving = false; // 无论是否异常，最终标记移动结束
            }
        }


        #endregion

        #region 外部触发羊移除的公共方法
        /// <summary>
        /// 从队列中移除指定的羊，并重新补齐该列的空缺（不销毁羊对象）
        /// </summary>
        /// <param name="sheepToRemove">要移除的羊</param>
        public void RemoveSheepFromQueue(SheepController sheepToRemove)
        {
            if (sheepToRemove == null)
            {
                Debug.LogWarning("[SheepSpawner] 尝试移除的羊为null");
                return;
            }

            Vector2Int gridPos = sheepToRemove.GetGridPosition();
            int column = gridPos.x; // 获取羊所在列

            if (!columnSheepQueues.ContainsKey(column))
            {
                Debug.LogWarning($"[SheepSpawner] 列 {column} 不存在");
                return;
            }

            var columnQueue = columnSheepQueues[column];

            // 检查列是否正在移动
            bool isColumnLocked = isColumnMoving.ContainsKey(column) && isColumnMoving[column];
            if (isColumnLocked)
            {
                Debug.LogWarning($"[SheepSpawner] 列 {column} 正在移动，无法移除羊");
                return;
            }

            // 检查羊是否在队列中
            if (!columnQueue.Contains(sheepToRemove))
            {
                Debug.LogWarning($"[SheepSpawner] 羊不在列 {column} 的队列中");
                return;
            }

            // 锁定列
            isColumnMoving[column] = true;

            try
            {
                // 从队列中移除指定的羊（但不销毁）
                var newQueue = new Queue<SheepController>();
                bool foundTarget = false;

                foreach (var sheep in columnQueue)
                {
                    if (sheep == sheepToRemove)
                    {
                        foundTarget = true;
                        continue; // 跳过要移除的羊
                    }
                    newQueue.Enqueue(sheep);
                }

                if (!foundTarget)
                {
                    Debug.LogWarning($"[SheepSpawner] 未在列 {column} 中找到要移除的羊");
                    return;
                }

                // 更新队列
                columnSheepQueues[column] = newQueue;

                // 从活跃列表中移除（可选，根据需求决定是否保留在活跃列表中）
                // if (activeSheep.Contains(sheepToRemove))
                // {
                //     activeSheep.Remove(sheepToRemove);
                // }

                // 释放网格位置
                if (!availableGridPositions.Contains(gridPos))
                {
                    availableGridPositions.Add(gridPos);
                }

                // 触发数量变化事件（如果需要）
                // OnSheepCountChanged?.Invoke(activeSheep.Count, totalSheepForLevel);

                // 移动该列中位于被移除羊后面的所有羊向前补齐空缺
                StartCoroutine(MoveColumnSheepForwardAfterRemoval(column, newQueue, gridPos.y, sheepToRemove));
            }
            finally
            {
                // 确保在发生异常时也能解锁列
                UnlockColumn(column);
            }
        }

        /// <summary>
        /// 移除羊后移动该列羊向前补齐空缺（不销毁羊）
        /// </summary>
        private IEnumerator MoveColumnSheepForwardAfterRemoval(int columnIndex, Queue<SheepController> columnQueue, int removalYPosition, SheepController removedSheep)
        {
            if (columnQueue == null)
            {
                Debug.LogError($"[SheepSpawner] 列 {columnIndex} 队列为null");
                UnlockColumn(columnIndex);
                yield break;
            }

            var sheepInColumn = columnQueue.ToArray();

            // 只移动在移除位置后面的羊（y坐标大于移除位置的羊）
            var sheepToMove = sheepInColumn.Where(sheep =>
                sheep != null && sheep.GetGridPosition().y > removalYPosition).ToArray();

            if (sheepToMove.Length == 0)
            {
                // 没有需要移动的羊，直接更新可点击状态
                UpdateColumnClickable(columnIndex, columnQueue);

                // 可选：对移除的羊进行额外处理（比如禁用、移动到特定位置等）
                // OnSheepRemovedFromQueue(removedSheep);

                yield break;
            }

            try
            {
                // 预先标记所有需要移动的羊为移动中
                foreach (var sheep in sheepToMove)
                {
                    if (sheep != null) sheep._isMoving = true;
                }

                // 按y坐标升序排列，从最靠近移除位置的羊开始移动
                var orderedSheepToMove = sheepToMove.OrderBy(sheep => sheep.GetGridPosition().y).ToArray();

                foreach (var sheep in orderedSheepToMove)
                {
                    if (sheep == null || sheep.gameObject == null) continue;

                    Vector2Int oldGridPos = sheep.GetGridPosition();
                    Vector2Int newGridPos = new Vector2Int(columnIndex, oldGridPos.y - 1); // 向前移动一格

                    if (newGridPos.y < 0)
                    {
                        Debug.LogError($"[SheepSpawner] 列 {columnIndex} 羊的新位置 {newGridPos} 无效");
                        continue;
                    }

                    Vector3 newWorldPos = GridToWorldPosition(newGridPos);

                    // 更新网格位置占用状态
                    UpdateGridPosition(oldGridPos, newGridPos);

                    // 启动移动
                    StartCoroutine(MoveSheepToTarget(sheep, newWorldPos));

                    // 更新羊的网格位置数据
                    sheep.SetGridPosition(newGridPos);

                    // 短暂延迟，避免所有羊同时移动
                   // yield return new WaitForSeconds(moveDelayBetweenSheep);
                }

                // 等待所有移动完成
                bool allStoppedMoving = false;
                while (!allStoppedMoving)
                {
                    allStoppedMoving = true;
                    foreach (var sheep in orderedSheepToMove)
                    {
                        if (sheep != null && sheep._isMoving)
                        {
                            allStoppedMoving = false;
                            break;
                        }
                    }
                    yield return null;
                }
            }
            finally
            {
                // 更新列的可点击状态
                UpdateColumnClickable(columnIndex, columnQueue);

                // 可选：对移除的羊进行额外处理
                // OnSheepRemovedFromQueue(removedSheep);
            }
        }

        /// <summary>
        /// 羊从队列中移除后的回调（可选，用于外部处理）
        /// </summary>
        public System.Action<SheepController> OnSheepRemovedFromQueue;
        #endregion

        #region 关卡启动与配置同步
        // 开始当前关卡羊群生成（接收关卡数据，初始化配置并启动生成）
        public void StartLevelSpawn(LevelData levelData)
        {
            currentLevelData = levelData;
            ClearAllSheep();

            // 1. 先同步LevelData的网格配置（核心：优先使用关卡网格）
            if (levelData != null)
            {
                gridSize = levelData.GridSize;
                gridSpacing = levelData.GridSpacing;
                gridStartPosition = levelData.GridStartPosition;
            }
            else
            {
                Debug.LogError("[SheepSpawner] LevelData为空，无法获取网格配置！使用默认值");
                // 兜底默认值（避免空引用，可选）
                gridSize = new Vector2Int(7, 7);
                gridSpacing = new Vector2(1f, 1f);
                gridStartPosition = new Vector3(-3f, 0f, -3f);
            }
            // 2. 重新初始化列队列和网格位置
            InitializeColumnQueues();
            InitializeGridPositions(); // 关键：在网格配置后重新初始化位置

            Debug.Log($"[SheepSpawner] 初始化网格位置完成，可用位置数: {availableGridPositions.Count}");

            StartCoroutine(StartProtectionTimer());

            // 3. 同步其他配置
            if (levelData != null && !UseLocalSheepPrefabsWeight)
                ConfigureFromLevelData(levelData);
            else
            {
                if (sheepPrefabsWeight == null || sheepPrefabsWeight.Length == 0)
                {
                    Debug.LogError("[SheepSpawner] 使用本地配置时，sheepPrefabsWeight 为空或无效！");
                    return;
                }
            }
            InitializeQuantityConfigCount();
            StartCoroutine(SpawnInitialSheep());
        }

        // 根据关卡数据配置生成参数（动态生成开关控制是否使用关卡配置）
        private void ConfigureFromLevelData(LevelData levelData)
        {
            if (levelData.useDynamicSheepSpawn)
            {
                totalSheepForLevel = levelData.totalSheepCount;            
                sheepPrefabsWeight = levelData.levelSheepPrefabsWeight;
                UseQuantity = levelData.UseQuantity;
                _blackSheepMask = levelData.blackSheepMask;
            }
        }

        #endregion

        #region 羊生成核心逻辑

        private IEnumerator SpawnInitialSheep()
        {
            isSpawning = true;
            int targetSheepCount = totalSheepForLevel;
            //Debug.Log($"[SheepSpawner] 开始生成普通羊，目标数量：{targetSheepCount}");

            // 第一步：批量生成「六种正常颜色的普通羊」（排除原生黑羊，遮罩后续添加）
            while (totalSpawnedSheep < targetSheepCount && CanSpawnMoreSheep())
            {
                SpawnSingleNormalSheep(); // 生成单只普通羊（强制排除黑羊）
                yield return null;
            }
           // Debug.Log($"[SheepSpawner] 普通羊生成完成，实际生成：{totalSpawnedSheep} 只");
            // 第二步：为「非第一排」的羊批量添加黑羊遮罩
            if (_blackSheepMask != null)
            {
                BlackMaskTheSheep();
                //Debug.Log($"[SheepSpawner] 黑羊遮罩处理完成");
            }
            isSpawning = false;
            UpdateAllColumnsClickable(); // 最终更新一次可点击状态
        }

        /// <summary>
        /// 生成单只「普通羊」（强制排除原生黑羊，仅六种正常颜色）
        /// </summary>
        private void SpawnSingleNormalSheep()
        {
            if (!CanSpawnMoreSheep()) return;

            Vector2Int gridPos = GetNextOrderedPosition();
            if (gridPos == Vector2Int.one * -1) return;

            Vector3 worldPos = GridToWorldPosition(gridPos);
            // 强制排除黑羊（无论是否第一排，普通羊都不包含原生黑羊）
            SheepPrefabsWeight selectedConfig = UseQuantity
                ? GetSheepPrefabConfig(excludeBlackSheep: true)
                : GetRandomSheepPrefabConfig(excludeBlackSheep: true);

            if (selectedConfig == null || selectedConfig.Prefab == null)
            {
                Debug.LogError("[SheepSpawner] 未找到有效普通羊配置（排除黑羊后）");
                return;
            }

            // （以下实例化、初始化逻辑与原SpawnSingleSheep一致，仅新增遮罩状态初始化）
            GameObject sheepObj = Instantiate(selectedConfig.Prefab, worldPos, Quaternion.identity, sheepParent);
            SheepController sheep = sheepObj.GetComponent<SheepController>();
            if (sheep == null)
            {
                Debug.LogError($"[SheepSpawner] 预制体 {selectedConfig.Prefab.name} 缺少 SheepController");
                Destroy(sheepObj);
                return;
            }

            // 初始化羊数据（默认无遮罩）
            SheepData sheepData = new SheepData(
                color: selectedConfig.GetSheepPrefabsColors(),
                gridPosition: gridPos,
                isActive: true
            );
            sheepData.isblackMasked = false; // 明确初始无遮罩
            sheep.Initialize(sheepData);

            // 加入管理集合
            activeSheep.Add(sheep);
            totalSpawnedSheep++;
            int column = gridPos.x;
            if (columnSheepQueues.ContainsKey(column))
                columnSheepQueues[column].Enqueue(sheep);

            availableGridPositions.Remove(gridPos);
            OnSheepCountChanged?.Invoke(activeSheep.Count, totalSheepForLevel);
        }

        /// <summary>
        /// 黑羊遮罩处理核心方法：非第一排羊按配置加遮罩
        /// </summary>
        private void BlackMaskTheSheep()
        {
            // 1. 筛选目标羊：非第一排（y≠0）、无遮罩、活跃状态
            List<SheepController> targetSheepList = activeSheep
                .Where(sheep =>
                    sheep != null
                    && sheep.gameObject.activeInHierarchy
                    && sheep.GetGridPosition().y != 0 // 排除第一排
                    && !sheep.sheepData.isblackMasked) // 排除已加遮罩的羊
                .ToList();

            if (targetSheepList.Count == 0)
            {
                Debug.Log("[SheepSpawner] 无符合条件的羊（非第一排），无需添加遮罩");
                return;
            }

            // 2. 根据配置计算需要加遮罩的羊数量
            int maskTargetCount = CalculateMaskTargetCount(targetSheepList.Count);
            if (maskTargetCount <= 0)
            {
                Debug.Log("[SheepSpawner] 计算遮罩数量为0，跳过处理");
                return;
            }
            // 确保遮罩数量不超过目标羊总数
            maskTargetCount = Mathf.Min(maskTargetCount, targetSheepList.Count);

            // 3. 按模式选择羊并添加遮罩
            List<SheepController> sheepToMask = UseQuantity
                ? SelectSheepByMaskQuantity(targetSheepList, maskTargetCount)
                : SelectSheepByMaskWeight(targetSheepList, maskTargetCount);

            // 4. 执行遮罩添加
            foreach (var sheep in sheepToMask)
            {
                if (sheep != null && sheep.sheepData != null)
                {
                    sheep.ApplyBlackMask();




                    sheep.UpdateVisualStateWithProtection(); // 立即更新视觉（如材质切换）



                    //Debug.Log($"[SheepSpawner] 羊（位置：{sheep.GetGridPosition()}）添加黑羊遮罩");
                }
            }
        }

        /// <summary>
        /// 计算需要加遮罩的羊数量（根据配置和目标羊总数）
        /// </summary>
        private int CalculateMaskTargetCount(int totalTargetSheep)
        {
            if (_blackSheepMask == null) return 0;

            // 数量模式：直接取配置数量（不超过目标羊总数）
            if (UseQuantity)
            {
                return Mathf.Min(_blackSheepMask.blackSheepMaskQuantity, totalTargetSheep);
            }
            // 权重模式：按权重占比计算（权重总和为参考，避免遮罩过多）
            else
            {
                float totalSheepWeight = sheepPrefabsWeight.Sum(config => config.weight);
                if (totalSheepWeight <= 0) return 0;
                // 遮罩数量 = 目标羊总数 × (遮罩权重 / 总羊权重)，向上取整
                float maskRatio = _blackSheepMask.blackSheepMaskweight / totalSheepWeight;
                return Mathf.CeilToInt(totalTargetSheep * maskRatio);
            }
        }

        /// <summary>
        /// 按「数量模式」选择要加遮罩的羊（随机选指定数量）
        /// </summary>
        private List<SheepController> SelectSheepByMaskQuantity(List<SheepController> targetList, int count)
        {
            List<SheepController> selected = new List<SheepController>();
            List<SheepController> tempList = new List<SheepController>(targetList); // 临时列表避免修改原数据

            for (int i = 0; i < count && tempList.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, tempList.Count);
                selected.Add(tempList[randomIndex]);
                tempList.RemoveAt(randomIndex); // 避免重复选择
            }
            return selected;
        }

        /// <summary>
        /// 按「权重模式」选择要加遮罩的羊（累计权重法随机）
        /// </summary>
        private List<SheepController> SelectSheepByMaskWeight(List<SheepController> targetList, int count)
        {
            List<SheepController> selected = new List<SheepController>();
            List<SheepController> tempList = new List<SheepController>(targetList);

            for (int i = 0; i < count && tempList.Count > 0; i++)
            {
                // 为每只羊分配「基础权重+遮罩权重」（确保遮罩权重影响概率）
                float totalWeight = tempList.Sum(sheep =>
                {
                    var config = sheepPrefabsWeight.FirstOrDefault(c => c.color == sheep.sheepData.color);
                    return (config?.weight ?? 1f) + _blackSheepMask.blackSheepMaskweight;
                });

                float randomValue = Random.Range(0f, totalWeight);
                float currentWeightSum = 0f;

                foreach (var sheep in tempList)
                {
                    var config = sheepPrefabsWeight.FirstOrDefault(c => c.color == sheep.sheepData.color);
                    float sheepWeight = (config?.weight ?? 1f) + _blackSheepMask.blackSheepMaskweight;

                    currentWeightSum += sheepWeight;
                    if (randomValue <= currentWeightSum)
                    {
                        selected.Add(sheep);
                        tempList.Remove(sheep); // 避免重复选择
                        break;
                    }
                }
            }
            return selected;
        }

        // （原SpawnSingleSheep方法可保留，作为备用；原SpawnSheep方法可删除或注释，避免冲突）
        

        // 初始化按数量生成的配置项计数（重置为0）
        private void InitializeQuantityConfigCount()
        {
            _quantityConfigSpawnedCount.Clear();
            // 筛选有效数量配置（非空、预制体非空、启用数量模式、数量>0）
            var validQuantityConfigs = sheepPrefabsWeight
                .Where(config => config != null && config.Prefab != null && UseQuantity && config.Quantity > 0)
                .ToList();

            foreach (var config in validQuantityConfigs) _quantityConfigSpawnedCount[config] = 0; // 初始计数为0
        }

        // 按数量配置选择羊（优先未达数量上限的配置，全部达标后按权重随机）
        private SheepPrefabsWeight GetSheepPrefabConfig(bool excludeBlackSheep = false)
        {
            // 筛选未达数量上限的配置，排除黑羊（如果需要）
            var remainingQuantityConfigs = _quantityConfigSpawnedCount
                .Where(kvp => kvp.Value < kvp.Key.Quantity &&
                      (!excludeBlackSheep || kvp.Key.GetSheepPrefabsColors() != WoolColor.Black))
                .Select(kvp => kvp.Key)
                .ToList();

            // 存在未达数量的配置时，随机选择并更新计数
            if (remainingQuantityConfigs.Count > 0)
            {
                int randomConfigIndex = Random.Range(0, remainingQuantityConfigs.Count);
                var targetConfig = remainingQuantityConfigs[randomConfigIndex];
                _quantityConfigSpawnedCount[targetConfig]++;
                return targetConfig;
            }

            // 所有配置达标后，降级为按权重随机（排除黑羊）
            return GetRandomSheepPrefabConfig(excludeBlackSheep);
        }

        // 按权重随机选择羊配置（过滤无效配置，权重为0时平均随机）
        private SheepPrefabsWeight GetRandomSheepPrefabConfig(bool excludeBlackSheep = false)
        {
            if (sheepPrefabsWeight == null || sheepPrefabsWeight.Length == 0)
            {
                Debug.LogError("[SheepSpawner] 没有配置羊预制体！");
                return null;
            }

            // 过滤无效配置（排除空配置或空预制体），排除黑羊（如果需要）
            var validConfigs = sheepPrefabsWeight
                .Where(config => config != null && config.Prefab != null &&
                       (!excludeBlackSheep || config.GetSheepPrefabsColors() != WoolColor.Black))
                .ToList();

            if (validConfigs.Count == 0)
            {
                // 如果是排除黑羊导致没有有效配置，则放宽条件允许黑羊
                if (excludeBlackSheep)
                {
                    Debug.LogWarning("[SheepSpawner] 排除黑羊后没有有效配置，将放宽条件允许黑羊");
                    validConfigs = sheepPrefabsWeight
                        .Where(config => config != null && config.Prefab != null)
                        .ToList();
                }

                if (validConfigs.Count == 0)
                {
                    Debug.LogError("[SheepSpawner] 所有羊预制体配置都是无效的！");
                    return null;
                }
            }

            // 计算总权重，权重为0时平均随机
            float totalWeight = validConfigs.Sum(config => config.weight);
            if (totalWeight <= 0)
            {
                Debug.LogWarning("[SheepSpawner] 权重之和为0，使用平均概率");
                int randomIndex = Random.Range(0, validConfigs.Count);
                return validConfigs[randomIndex];
            }

            // 累计权重法随机选择配置
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeightSum = 0f;
            foreach (var config in validConfigs)
            {
                currentWeightSum += config.weight;
                if (randomValue <= currentWeightSum) return config;
            }

            // 兜底：返回第一个有效配置
            return validConfigs[0];
        }



        // 检查是否可生成更多羊（满足：未超网格上限、未超关卡总数、有可用位置）
        private bool CanSpawnMoreSheep()
        {
            return activeSheep.Count < totalSheepForLevel && totalSpawnedSheep < totalSheepForLevel && availableGridPositions.Count > 0;
        }
        #endregion

        #region 网格位置管理
        // 初始化网格可用位置（按y上→下、x左→右顺序添加）
        private void InitializeGridPositions()
        {
            availableGridPositions.Clear();
            for (int y = 0; y < gridSize.y; y++)      // 先遍历y轴（上到下）
            {
                for (int x = 0; x < gridSize.x; x++)  // 再遍历x轴（左到右）
                {
                    availableGridPositions.Add(new Vector2Int(x, y));
                }
            }
            currentSpawnIndex = 0; // 重置生成索引
        }

        // 按顺序获取可用网格位置（无可用位置返回(-1,-1)）
        private Vector2Int GetNextOrderedPosition()
        {
            if (availableGridPositions.Count == 0) return Vector2Int.one * -1;
            return availableGridPositions[0]; // 取第一个可用位置（保证生成顺序）
        }

        // 将网格坐标转换为世界坐标（x对应世界X轴，y对应世界Z轴）
        private Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            float worldZ = gridStartPosition.z + (gridSize.y - 1 - gridPos.y) * gridSpacing.y; // y轴对应世界Z轴（上→下反向）
            float worldX = gridStartPosition.x + gridPos.x * gridSpacing.x; // x轴对应世界X轴（左→右正向）
            return new Vector3(worldX, gridStartPosition.y, worldZ);
        }
        #endregion

        #region 羊移除与清理
        // 羊移动完成（被移除）事件处理（销毁羊、释放位置、更新状态）
        private void OnSheepRemoved(SheepController sheep)
        {
            if (activeSheep.Contains(sheep)) // 防止重复处理
            {
                activeSheep.Remove(sheep); // 从活跃列表移除
                Vector2Int gridPos = sheep.GetGridPosition();
                int column = gridPos.x;

                // 从列队列中移除当前羊并重置变换状态
                if (columnSheepQueues.ContainsKey(column))
                {
                    var columnQueue = columnSheepQueues[column];
                    var newQueue = new Queue<SheepController>();
                    foreach (var s in columnQueue) { if (s != sheep) newQueue.Enqueue(s); } // 排除当前羊
                    columnSheepQueues[column] = newQueue;
                   // blackSheepTransformed[column] = false; // 重置变换状态
                    UpdateColumnClickable(column, newQueue); // 更新可点击状态
                }

                // 销毁羊对象
                if (sheep != null) Destroy(sheep.gameObject);
                // 释放网格位置（防止重复添加）
                if (!availableGridPositions.Contains(gridPos)) availableGridPositions.Add(gridPos);

                OnSheepCountChanged?.Invoke(activeSheep.Count, totalSheepForLevel); // 触发数量变化事件
            }
        }

        // 清理所有羊（销毁对象、重置列表和计数，用于关卡切换/重置）
        public void ClearAllSheep()
        {
            StopAllCoroutines(); // 停止所有协程（含保护计时器）

            // 销毁所有活跃羊
            foreach (var sheep in activeSheep) { if (sheep != null) Destroy(sheep.gameObject); }
            activeSheep.Clear(); // 清空活跃列表
            InitializeColumnQueues(); // 重置列队列
            InitializeGridPositions(); // 重置网格位置
            _quantityConfigSpawnedCount.Clear(); // 重置数量配置计数
            totalSpawnedSheep = 0; // 重置总生成数
            PassStartProtected = false;
        }

        // 公共方法：获取关卡内剩余的羊数（对外提供统一调用接口）
        public int GetRemainingSheepCount()
        {
            // 计算已被处理的羊数（已生成 - 当前活跃）
            int processedSheep = totalSpawnedSheep - activeSheep.Count;
            // 剩余羊数 = 总羊数 - 已处理数，确保结果非负
            //int remainingSheep = Mathf.Max(0, totalSheepForLevel - processedSheep);
            int remainingSheep = activeSheep.Count;
            return remainingSheep;
        }
        #endregion

        #region 编辑器工具（仅Unity编辑器生效）
        // 编辑器Gizmos：选中对象时绘制网格线框（辅助场景编辑）
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = UnityEngine.Color.yellow; // Gizmos颜色设为黄色
            // 遍历所有网格坐标，绘制线框立方体（大小0.8f避免重叠）
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector3 pos = GridToWorldPosition(new Vector2Int(x, y));
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);
                }
            }
        }
        #endregion
    }
}