using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace ConnectMaster
{
    // 类别-模型索引映射类
    [System.Serializable]
    public class CategoryToModelMapping
    {
        [Tooltip("配对成功的物品类别")]
        public ItemCategory targetCategory;
        [Tooltip("对应房屋模型索引（与HouseControl字典键一致，从1开始）")]
        public int modelIndex;
    }
    [CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data", order = 2)]
    public class LevelData : ScriptableObject
    {
        #region 筛选组配置类（每组1个，含cols个不同物品）
        [System.Serializable]
        public class LevelRequiredItem
        {
            [Tooltip("本组统一筛选类别（只显示该类物品）")]
            public ItemCategory filterCategory; // 每组1个筛选条件
            public List<Item> selectedItems = new List<Item>(); // 本组选中的cols个不同物品
            [HideInInspector] public bool IsMatch = false;
            
        }
        #endregion
        

        [Header("关卡基础信息")]
        public int level; // 关卡编号
        public string levelName; // 关卡名称

        [Header("关卡时间")]
        public int countdownDuration; // 关卡时间

        [Header("房屋相关")]
        public HouseType _houseType; // 房屋类型
        public int houseModelProgress; // 房屋进度


        [Header("生成物品类型行(最多为9)")]
        public int rows = 4; // 筛选组数量=rows（有多少行就有多少组）
        [HideInInspector]public int cols = 4;

        [Header("格子布局配置(最多为6)")]
         public int Cellrows = 4; // 筛选组数量=rows（有多少行就有多少组）
        [HideInInspector] public int Cellcols = 4; 
        public float cellWidth = 200f;
        public float cellHeight = 200f;
        public float spacingX = 20f;
        public float spacingY = 20f;
        public Vector2 pos;

        [Header("消除规则")]
        public int categoryEliminateCount = 4; // 横排4个同类别消除

        [Header("关联依赖")]
        public ItemDatabase itemDatabase; // 关联物品数据库

        [Header("筛选组配置（每行1个筛选组）")]
        [HideInInspector] public List<LevelRequiredItem> filterGroups = new List<LevelRequiredItem>(); // 筛选组：数量=rows，每组选cols个不同物品

        [Header("筛选结果（自动同步所有选中物品）")]
        public List<Item> requiredItems = new List<Item>(); // 最终所有选中的物品集合（去重）

        [Header("类型对应模型")]
        public List<CategoryToModelMapping> _categoryToModelMapping = new List<CategoryToModelMapping>();

        //[Header("上一关最后一个类型为")]
        [HideInInspector] public ItemCategory LastLevelLastCategory;

        #region 生命周期函数
        private void OnEnable()
        {
          
        }
        #endregion

        #region 自动同步逻辑（核心修正：rows=组数，cols=每组物品数）
        private void OnValidate()
        {
            if (itemDatabase == null || itemDatabase.allItems == null || rows <= 0 || cols <= 0) return;

            //SyncFilterGroupsCount(); // 同步筛选组数量=rows（有多少行就有多少组）
            //SyncEachGroupItemsCount(); // 同步每组物品数=cols（有多少列每组就有多少物品）
            //SyncRequiredItems(); // 同步所有选中物品到requiredItems（去重）
            SyncCellrows();//同步配置格子数量
        }

        // 同步筛选组数量=行数（rows）：有多少行就有多少组
        private void SyncFilterGroupsCount()
        {
            // 组数不足：新增筛选组（默认无类别+空物品列表）
            while (filterGroups.Count < rows)
            {
                filterGroups.Add(new LevelRequiredItem
                {
                    filterCategory = ItemCategory.None,
                    selectedItems = new List<Item>()
                });
            }
            // 组数过多：删除多余组（保留前面配置）
            while (filterGroups.Count > rows)
            {
                filterGroups.RemoveAt(filterGroups.Count - 1);
            }
        }

        // 同步每组物品数=列数（cols）：有多少列每组就有多少物品
        private void SyncEachGroupItemsCount()
        {
            foreach (var group in filterGroups)
            {
                if (group.selectedItems == null) group.selectedItems = new List<Item>();
                // 物品数不足：补充null占位
                while (group.selectedItems.Count < cols) group.selectedItems.Add(null);
                // 物品数过多：删除多余项
                while (group.selectedItems.Count > cols) group.selectedItems.RemoveAt(group.selectedItems.Count - 1);
            }
        }

        // 同步所有选中物品到requiredItems（去重）
        private void SyncRequiredItems()
        {
            requiredItems.Clear();
            foreach (var group in filterGroups)
            {
                foreach (var item in group.selectedItems)
                {
                    if (item != null && !requiredItems.Contains(item))
                    {
                        requiredItems.Add(item);
                    }
                }
            }
        }

        // 同步配置格子行数（Cellrows）：跟随 rows 同步，且限制 1~6 之间
        private void SyncCellrows()
        {
            // 边界值处理：rows≤0 时，默认设为 1（格子行数至少为1，否则布局无效）
            if (rows <= 0)
            {
                Cellrows = 1;
            }
            // rows≤6 时，Cellrows 跟随 rows 同步
            else if (rows <= 6)
            {
                Cellrows = rows;
            }
            // rows>6 时，Cellrows 上限为6
            else
            {
                Cellrows = 6;
            }
        }
        #endregion

        #region 外部调用
    
        //同步requiredItems的物品类别去重依次填入targetCategory
        public void SyncCategoryToModelMapping()
        {
            // 1. 初始化映射列表（防止空引用）
            if (_categoryToModelMapping == null)
                _categoryToModelMapping = new List<CategoryToModelMapping>();
            _categoryToModelMapping.Clear();

            // 2. 提取 requiredItems 中的有效类别（去重、排除 None/Null）
            var validCategories = requiredItems
                .Where(item => item != null && item.category != ItemCategory.None) // 过滤无效数据
                .Select(item => item.category) // 提取类别
                .Distinct() // 去重：确保有效类别集合无重复
                .OrderBy(category => category) // 按枚举顺序排序（方便编辑）
                .ToList();
            // 3. 新增有效类别对应的映射项
            foreach (var category in validCategories)
            {
                // 新建映射项，自动填入类别，modelIndex 按顺序分配（从1开始）
                _categoryToModelMapping.Add(new CategoryToModelMapping
                {
                    targetCategory = category, // 自动填入有效类别
                 //modelIndex = _categoryToModelMapping.Count + 1 // 保证索引连续递增
                });
            }
        }
        #endregion
    }
}








#region 旧版itempairing
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using TMPro;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.UI;
//using static ConnectMaster.LevelData;
//using static UnityEngine.Rendering.DebugUI.Table;

//namespace ConnectMaster
//{
//    public class ItemPairing : MonoBehaviour
//    {
//        public static ItemPairing Instance;

//        [HideInInspector]public List<GridCellControl> allGridCells;

//        #region 动画相关
//        // 特效飞行动画配置
//        [Header("特效飞行动画配置")]
//        [Tooltip("特效从UI飞到3D模型的总时长（秒）")]
//        public float flyEffectDuration = 1.8f;
//        [Tooltip("特效飞行的缓动曲线（建议末端平缓实现蝴蝶降落效果）")]
//        public AnimationCurve flyEffectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//        [Tooltip("特效飞行抛物线的高度（世界坐标单位，越高抛物线越明显）")]
//        public float flyEffectArcHeight = 1.5f;
//        [Tooltip("终点位置额外偏移（用于调整飞行终点，正Y值=更高）")]
//        public Vector3 targetPositionOffset = Vector3.zero;
//        [Tooltip("末端减速强度（0=无减速，1=强减速，像蝴蝶降落）")]
//        [Range(0f, 1f)]
//        public float landingSlowdown = 0.6f;
//        [Tooltip("开始减速的进度点（0.7=最后30%开始减速）")]
//        [Range(0.5f, 0.95f)]
//        public float landingSlowdownStart = 0.7f;

//        [Header("流星摆动轨迹配置")]
//        [Tooltip("主摆动幅度（横向偏移的最大距离）")]
//        public float swingAmplitude = 0.5f;
//        [Tooltip("主摆动频率（飞行过程中摆动的次数）")]
//        public float swingFrequency = 3f;
//        [Tooltip("次摆动幅度（叠加的小幅高频抖动）")]
//        public float secondaryAmplitude = 0.15f;
//        [Tooltip("次摆动频率（高频抖动的次数，建议为主频率的2-3倍）")]
//        public float secondaryFrequency = 7f;
//        [Tooltip("垂直摆动幅度（上下方向的波动）")]
//        public float verticalAmplitude = 0.2f;
//        [Tooltip("垂直摆动频率")]
//        public float verticalFrequency = 4f;
//        [Tooltip("摆动幅度曲线（控制摆动从起点到终点的强度变化）")]
//        public AnimationCurve swingIntensityCurve = AnimationCurve.EaseInOut(0, 0.3f, 0.5f, 1f);
//        [Tooltip("是否让特效朝向飞行方向（拖尾更自然）")]
//        public bool orientToVelocity = true;

//        [Header("星星旋转配置")]
//        [Tooltip("Z轴自旋速度（度/秒，正值顺时针，负值逆时针）")]
//        public float spinSpeed = 360f;
//        [Tooltip("旋转速度随时间变化曲线（可实现加速/减速旋转）")]
//        public AnimationCurve spinSpeedCurve = AnimationCurve.Linear(0, 1, 1, 1);
//        [Tooltip("轻微摇摆幅度（X/Y轴的微小晃动，0=纯Z轴旋转）")]
//        [Range(0f, 15f)]
//        public float wobbleAmount = 0f;
//        [Tooltip("摇摆频率")]
//        public float wobbleFrequency = 2f;
//        //配对行动画配置
//        [Header("配对行收束扩张动画配置")]
//        [Tooltip("动画总时长（秒）")]
//        public float shrinkAnimDuration = 0.5f;
//        [Tooltip("水平间距收缩比例（0=完全贴紧，1=原间距；建议0.3~0.7）")]
//        [Range(0f, 1f)]
//        public float shrinkSpacingRatio = 0.5f; // 最终间距是原间距的50%
//        [Tooltip("动画结束后的目标缩放（0=完全消失，1=原大小）")]
//        [Range(0f, 1f)]
//        public float shrinkTargetScale = 1f; // 可选：收束时是否缩放（默认保持原大小）
//        [Tooltip("动画缓动曲线")]
//        public AnimationCurve shrinkEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//        [Tooltip("是否启用水平间距收束")]
//        public bool enableSpacingShrink = true;
//        [Tooltip("是否启用缩放动画")]
//        public bool enableScaleAnim = false; // 若仅要间距收束，可关闭缩放

//        // 归纳框生成动画配置
//        [Header("归纳框生成动画配置")]
//        [Tooltip("动画时长（秒）")]
//        public float summaryScaleAnimDuration = 0.3f;
//        [Tooltip("动画缓动曲线")]
//        public AnimationCurve summaryScaleEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//        [Tooltip("初始缩放比例（0=完全隐藏，1=原大小）")]
//        public Vector3 summaryInitialScale = Vector3.zero;
//        [Tooltip("目标缩放比例（1=原大小）")]
//        public Vector3 summaryTargetScale = Vector3.one;

//        // 归纳框消失动画配置
//        [Header("收纳框消失动画配置")]
//        [Tooltip("收纳框消失动画时长")]
//        public float summaryDisappearDuration = 0.4f;
//        [Tooltip("收纳框消失动画时长")]
//        public float DelysummaryDisappearDuration = 1f;
//        [Tooltip("收纳框消失动画曲线")]
//        public AnimationCurve summaryDisappearEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

//        //补充生成动画配置
//        [Header("补充生成格子缩小动画动画配置")]
//        [Tooltip("格子缩小动画时长")]
//        public float shrinkBeforeGenerateDuration = 0.3f;
//        [Tooltip("格子缩放动画曲线")]
//        public AnimationCurve scaleEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
//        [Tooltip("格子缩小比例")]
//        [Range(0f, 1f)]
//        public float shrinkScale = 0.3f;

//        // 存储单个格子初始状态的结构体（位置+缩放）
//        private struct GridCellInitialState
//        {
//            public Vector2 anchoredPosition;
//            public Vector3 localScale;

//            public GridCellInitialState(Vector2 pos, Vector3 scale)
//            {
//                anchoredPosition = pos;
//                localScale = scale;
//            }
//        }
//        // 全局存储每行收束前的初始状态（key=行索引，value=该行每个格子的初始状态）
//        private Dictionary<int, List<GridCellInitialState>> _rowInitialStates = new Dictionary<int, List<GridCellInitialState>>();
//        #endregion

//        #region 归纳框数据配置
//        [Header("归纳框数据配置")]
//        [Tooltip("归纳框")]
//        public GameObject[] SummaryBox;
//        [Tooltip("归纳框的UI父节点")]
//        public RectTransform summaryBoxParent;
//        [Tooltip("归纳框偏移")]
//        public Vector2 summaryOffest;
//        [Tooltip("归纳后格子颜色")]
//        public Color[] SummaryColor;
//        // 当前使用的归纳框索引计数器（用于顺序循环）
//        private int _currentSummaryIndex = 0;
//        //有效预制体索引列表（仅存储非空预制体的索引，避免重复过滤）
//        private List<int> _validSummaryIndices = new List<int>();
//        // 当前使用的格子颜色索引计数器（与归纳框同步顺序）
//        private int _currentColorIndex = 0;
//        #endregion

//        #region 配对相关
//        // 避免同时触发出错缓存待处理的配对行（队列保证处理顺序）
//        private Queue<List<GridCellControl>> _pairedRowQueue = new Queue<List<GridCellControl>>();
//        // 标记是否正在处理配对（避免并行）
//        private bool _isProcessingPair = false;

//        public Action<ItemCategory> ParingRow;
//        #endregion

//        #region 补充相关
//        //判断是否还需要补充 避免重复动画
//        internal int SupplementNum = 0;
//        #endregion

//        #region 生命周期函数
//        private void Awake()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//                DontDestroyOnLoad(gameObject);
//                allGridCells = new List<GridCellControl>();
//            }
//            else
//            {
//                Destroy(gameObject);
//            }
//        }
//        private void Start()
//        {
//            Initialize();
//            // 初始化归纳框索引列表（过滤空预制体）
//            InitValidSummaryIndices();
//        }
//        #endregion

//        #region 初始化
//        private void Initialize()
//        {

//        }

//        // 初始化有效预制体索引（只保留非空预制体的索引）
//        private void InitValidSummaryIndices()
//        {
//            _validSummaryIndices.Clear();
//            _currentSummaryIndex = 0; // 重置归纳框计数器
//            _currentColorIndex = 0;   // 重置颜色计数器（关卡重置后从第一个颜色开始）

//            if (SummaryBox == null || SummaryBox.Length == 0)
//            {
//                Debug.LogWarning("归纳框预制体数组为空，无法初始化有效索引");
//                return;
//            }

//            // 只添加非空预制体的索引到有效列表
//            for (int i = 0; i < SummaryBox.Length; i++)
//            {
//                if (SummaryBox[i] != null)
//                {
//                    _validSummaryIndices.Add(i);
//                }
//                else
//                {
//                    Debug.LogWarning($"归纳框数组索引{i}对应的预制体为空，已过滤");
//                }
//            }
//        }

//        #endregion

//        #region 检测是否有配对的物品
//        // 检查所有行：该行所有物品的 Category 是否完全一致
//        public bool CheckHavePairing()
//        {
//            UpdateAllCell();
//            // 安全校验：格子列表为空则直接返回
//            if (allGridCells == null || allGridCells.Count == 0)
//            {
//                Debug.LogWarning("格子列表为空，无法检查配对");
//                return false;
//            }

//            // 1. 按行分组：key = 行号（rowIndex），value = 该行所有格子
//            Dictionary<int, List<GridCellControl>> rowToCells = new Dictionary<int, List<GridCellControl>>();
//            foreach (var cell in allGridCells)
//            {
//                int row = cell.rowIndex;
//                // 若字典中没有当前行的key，创建新列表
//                if (!rowToCells.ContainsKey(row))
//                {
//                    rowToCells[row] = new List<GridCellControl>();
//                }
//                // 将当前格子加入对应行的列表
//                rowToCells[row].Add(cell);
//            }

//            bool hasPairedRow = false;

//            // 2. 遍历每行，检查物品分类一致性
//            foreach (var rowKvp in rowToCells)
//            {
//                int currentRow = rowKvp.Key;
//                List<GridCellControl> rowCells = rowKvp.Value;

//                // 边界校验1：该行格子数量是否等于列数（避免漏格子）
//                if (rowCells.Count != GridCellGenerate.Instance.colCount)
//                {
//                    Debug.LogWarning($"第{currentRow}行格子数量不完整（应有{GridCellGenerate.Instance.colCount}个，实际{rowCells.Count}个）");
//                    continue;
//                }

//                // 边界校验2：该行是否有格子没有物品（空物品无法配对）
//                bool hasEmptyItem = rowCells.Any(cell => cell._currentItem == null);
//                if (hasEmptyItem)
//                {
//                    Debug.Log($"第{currentRow}行存在空物品，跳过校验");
//                    continue;
//                }
//                // 重复校验：该行是否已经配对过
//                bool hadPairing = rowCells.All(cell => cell.isPairing); // 整行都已配对才判定为“已处理”
//                if (hadPairing)
//                {
//                    //Debug.Log($"第{currentRow}行已配对，跳过重复校验");
//                    continue;
//                }
//                // 3. 校验该行所有物品的 Category 是否一致
//                ItemCategory targetCategory = rowCells[0]._currentItem.category; // 以第一个物品为基准
//                bool isRowPaired = true;

//                foreach (var cell in rowCells)
//                {
//                    // 若当前物品分类与基准不一致，标记该行未配对
//                    if (cell._currentItem.category != targetCategory)
//                    {
//                        isRowPaired = false;
//                        break;
//                    }
//                }

//                // 4. 处理配对成功的行
//                if (isRowPaired)
//                {
//                    hasPairedRow = true;
//                    Debug.Log($"✅ 第{currentRow}行配对成功！物品分类：{targetCategory}");
//                    //不可拖动交换
//                    foreach(var cell in rowCells)
//                    {
//                        ItemControl targetItemContorl = cell.GetComponentInChildren<ItemControl>();
//                        targetItemContorl.canDrag=false;
//                        targetItemContorl.canExchange=false;
//                    }
//                    //配对成功后的逻辑
//                    _pairedRowQueue.Enqueue(rowCells);

//                } 
//            }
//            // 触发队列处理（如果当前没有正在处理的配对）
//            if (hasPairedRow && !_isProcessingPair)
//            {
//                ProcessNextPairedRow();
//            }

//            return hasPairedRow;
//        }
//        //从队列中取出下一行处理
//        private void ProcessNextPairedRow()
//        {
//            // 队列空 → 重置为未处理，返回
//            if (_pairedRowQueue.Count == 0)
//            {
//                _isProcessingPair = false;
//                return;
//            }
//            // 标记为正在处理，避免并行
//            _isProcessingPair = true;
//            // 取出队列首行
//            List<GridCellControl> nextRow = _pairedRowQueue.Dequeue();
//            UpdateAllCell();
//            ItemCategory category = nextRow[0]._currentItem.category;

//            //处理配对行
//            HandlePairedRow(nextRow, category);
//            //成功配对了一行，配对行++
//            LevelManager.Instance.AddHasPairRows();

//            //检查是否胜利
//            LevelManager.Instance.CheckHasVictory();
//        }
//        #endregion

//        #region 处理配对的物品
//        private void HandlePairedRow(List<GridCellControl> pairedCells, ItemCategory category)
//        {
//            // 获取所有未配对行 + 统计数量
//            List<List<GridCellControl>> allUnpairedRows = GetAllUnpairedRows();
//            int unpairedCount = allUnpairedRows.Count;

//            List<GridCellControl> targetRowCells;
//            if (unpairedCount ==2)
//            {
//                //Debug.Log($"只剩{unpairedCount}行未配对");
//                targetRowCells = pairedCells; // 目标行=自身，跳过动画
//            }
//            else
//            {
//                targetRowCells = FindHighestUnpairedRow();
//                if (targetRowCells == null || targetRowCells.Count == 0)
//                {
//                    Debug.LogWarning("❌ 未找到目标行，设为自身");
//                    targetRowCells = pairedCells;
//                }
//                //Debug.Log($"最终targetRowCells：行{targetRowCells[0].rowIndex}");
//            }
//            foreach (var cell in pairedCells)
//            {
//                cell.isPairing = true;
//            }

//            StartCoroutine(SwapEntireRowsCoroutine(pairedCells, targetRowCells, ProcessNextPairedRow));

//        }
//        // 寻找「行数最小（相对最高）且未配对」的行（目标行）
//        private List<GridCellControl> FindHighestUnpairedRow()
//        {
//            UpdateAllCell();
//            Dictionary<int, List<GridCellControl>> rowToCells = new Dictionary<int, List<GridCellControl>>();
//            foreach (var cell in allGridCells)
//            {
//                int row = cell.rowIndex;
//                if (!rowToCells.ContainsKey(row))
//                {
//                    rowToCells[row] = new List<GridCellControl>();
//                }
//                rowToCells[row].Add(cell);
//            }

//            List<List<GridCellControl>> unpairedRows = new List<List<GridCellControl>>();
//            foreach (var rowKvp in rowToCells)
//            {
//                List<GridCellControl> rowCells = rowKvp.Value;
//                bool isComplete = rowCells.Count == GridCellGenerate.Instance.colCount;
//                bool noEmpty = !rowCells.Any(cell => cell._currentItem == null);
//                bool isUnpaired = !rowCells.All(cell => cell.isPairing);

//                if (isComplete && noEmpty && isUnpaired)
//                {
//                    unpairedRows.Add(rowCells);
//                }
//            }

//            // 打印排序后的候选行号
//            var sortedRows = unpairedRows.OrderBy(row => row[0].rowIndex).ToList();
//            //Debug.Log($"候选未配对行（排序后）：{string.Join(",", sortedRows.Select(r => r[0].rowIndex))}");

//            // 最终结果
//            var result = sortedRows.FirstOrDefault()?.OrderBy(c => c.colIndex).ToList();
//            //Debug.Log(result != null ? $"选中最高行：{result[0].rowIndex}" : "无符合条件的行");
//            return result;
//        }
//        #endregion

//        #region 核心方法： 整行交换

//        //整行交换协程：复用SwapAnimationCoroutine，所有列物品同时交换
//        private IEnumerator SwapEntireRowsCoroutine(List<GridCellControl> pairedRow, List<GridCellControl> targetRow,Action ProcessNextPairedRow)
//        {
//            ItemGenerate.Instance.LockAnimation();
//            // 按列号排序（确保第1列和第1列交换，第2列和第2列交换）
//            List<GridCellControl> sortedPairedRow = pairedRow.OrderBy(cell => cell.colIndex).ToList();
//            List<GridCellControl> sortedTargetRow = targetRow.OrderBy(cell => cell.colIndex).ToList();

//            int completedSwapCount = 0; // 记录已完成的交换数量
//            int totalValidSwaps = 0;    // 记录有效启动的交换数量

//            // 遍历所有列，同时启动所有交换协程（不等待单个完成）
//            for (int col = 0; col < sortedPairedRow.Count; col++)
//            {
//                GridCellControl pairedCell = sortedPairedRow[col];
//                GridCellControl targetCell = sortedTargetRow[col];

//                // 获取两个格子中的物品（安全校验）
//                ItemControl pairedItem = pairedCell.GetComponentInChildren<ItemControl>();
//                ItemControl targetItem = targetCell.GetComponentInChildren<ItemControl>();

//                if (pairedItem == null || targetItem == null)
//                {
//                    Debug.LogError($"❌ 第{col + 1}列交换失败：物品为空（配对行物品：{pairedItem != null}，目标行物品：{targetItem != null}）");
//                    continue;
//                }

//                totalValidSwaps++; // 统计有效交换数
//                int currentCol = col; // 闭包捕获临时变量

//                // 启动交换协程（不等待，实现同时交换）
//                StartCoroutine(pairedItem.SwapAnimationCoroutine(pairedItem, targetItem, () =>
//                {
//                    completedSwapCount++;
//                    //Debug.Log($"🔄 第{currentCol + 1}列交换完成（配对行{pairedRow[0].rowIndex}→目标行{targetRow[0].rowIndex}）");
//                }));
//            }

//            // 等待所有有效交换协程完成（直到完成数等于有效启动数）
//            while (completedSwapCount < totalValidSwaps)
//            {
//                yield return null;
//            }

//            // 所有列交换完成后，更新配对状态
//            SetPairedRowState(pairedRow, false);
//            SetPairedRowState(targetRow, true);

//            //触发提示匹配行
//            ParingRow?.Invoke(targetRow[0]._currentItem.category);


//            //-------------------------------------------------
//            // 交换完成后，顺序执行动画
//            int animationCompletedCount = 0; // 记录已完成跳动动画的格子数量
//            int totalCellCount = targetRow.Count; // 目标行总格子数

//            // 第一步：启动所有格子的跳动动画，并用回调统计完成状态
//            foreach (var cell in targetRow)
//            {
//                if (cell == null) continue;

//                // 启动跳动动画，并传入“动画完成回调”
//                StartCoroutine(cell.ExchangeDoneAnimation(() =>
//                {
//                    animationCompletedCount++; // 某个格子动画完成，计数器+1
//                    //Debug.Log($"格子[{cell.rowIndex},{cell.colIndex}] 跳动动画完成，已完成{animationCompletedCount}/{totalCellCount}");
//                }));
//            }

//            // 等待所有格子的跳动动画全部完成（关键：直到计数器等于总格子数）
//            while (animationCompletedCount < totalCellCount)
//            {
//                yield return null; // 每帧检查一次，不阻塞主线程
//            }
//            //Debug.Log("✅ 所有格子跳动动画执行完成！");

//            // 第二步：所有跳动动画完成后，生成归纳框（同步执行，执行完再往下走）
//            SummaryBoxGenerate(targetRow);
//            //Debug.Log("✅ 归纳框生成完成！");

//            // 第三步：执行收束+上色动画，并等待其完成
//            yield return StartCoroutine(ShrinkRowToCenterAndColorCoroutine(targetRow));
//            //Debug.Log("✅ 收束动画+格子上色完成！");
//            //-------------------------------------------------

//            // 交换完成后重新更新所有格子的提示颜色放置bug
//            HintManager.Instance.UpdateColorsAfterSwap();   
//            StartCoroutine(DelayedProcessNextPairedRow(0.2f));//间隔0.2f触发

//            ItemGenerate.Instance.UnlockAnimation();
//        }
//        // 延迟执行ProcessNextPairedRow，并保证解锁动画
//        private IEnumerator DelayedProcessNextPairedRow(float delay)
//        {
//            ItemGenerate.Instance.LockAnimation();

//            // 等待指定延迟（这0.2秒期间Lock生效，IsAnimating=true）
//            yield return new WaitForSeconds(delay);

//            try
//            {
//                // 执行队列处理逻辑
//                ProcessNextPairedRow();
//            }
//            catch (Exception e)
//            {
//                Debug.LogError($"延迟执行ProcessNextPairedRow出错：{e.Message}");
//            }
//            finally
//            {
//                // 无论是否报错，都解锁动画（避免计数异常）
//                 ItemGenerate.Instance.UnlockAnimation();

//            }
//        }

//        #endregion


//        #region 归纳框生成
//        // 归纳框生成方法（按预制体数组顺序0→1→2...循环，无状态列表）
//        private void SummaryBoxGenerate(List<GridCellControl> pairingRow)
//        {
//            #region  1. 安全校验
//            // 1. 安全校验
//            if (SummaryBox == null || SummaryBox.Length == 0)
//            {
//                Debug.LogError("❌ 归纳框预制体数组SummaryBox为空，请先赋值！");
//                return;
//            }
//            if (summaryBoxParent == null)
//            {
//                Debug.LogError("❌ 归纳框父节点summaryBoxParent未赋值！");
//                return;
//            }
//            if (pairingRow == null || pairingRow.Count == 0)
//            {
//                Debug.LogError("❌ 配对行数据无效，无法生成归纳框！");
//                return;
//            }
//            GridCellControl firstPairCell = pairingRow[0];
//            if (firstPairCell._rectTransform == null)
//            {
//                Debug.LogError("❌ 配对行的格子缺少RectTransform组件，无法获取位置！");
//                return;
//            }
//            // 校验是否有有效预制体
//            if (_validSummaryIndices.Count == 0)
//            {
//                Debug.LogError("❌ 无有效归纳框预制体，无法生成！");
//                return;
//            }
//            #endregion

//            // 2. 固定顺序获取目标索引
//            int targetIndex = _validSummaryIndices[_currentSummaryIndex];

//            // 3. 更新计数器（循环逻辑：到末尾后重置为0）
//            _currentSummaryIndex = (_currentSummaryIndex + 1) % _validSummaryIndices.Count;

//            // 4. 实例化归纳框
//            GameObject targetSummaryPrefab = SummaryBox[targetIndex];
//            GameObject newSummaryBox = Instantiate(
//                targetSummaryPrefab,
//                summaryBoxParent,
//                false
//            );
//            SummaryBoxControl newSummaryBoxControl = newSummaryBox.GetComponent<SummaryBoxControl>();
//            newSummaryBoxControl.SetTargetRow(firstPairCell.rowIndex);

//            newSummaryBox.transform.localScale = Vector3.one;
//            SetSummaryBoxText(newSummaryBox, firstPairCell._currentItem.category);

//            // 5. 设置位置
//            RectTransform summaryRect = newSummaryBox.GetComponent<RectTransform>();

//           summaryRect.anchoredPosition = new Vector2(summaryOffest.x, summaryOffest.y + firstPairCell._rectTransform.anchoredPosition.y);


//            //------3d场景房屋显现------
//            //ShowHousePartModel(firstPairCell);
//            //  ShowHousePartModel(firstPairCell, newSummaryBox);
//            ShowHousePartModel(firstPairCell, summaryRect);
//            //------3d场景房屋显现------


//            // 启动缩放动画
//            StartCoroutine(SummaryBoxScaleAnimCoroutine(newSummaryBox.GetComponent<RectTransform>()));
//        }

//        //归纳框缩放动画协程
//        private IEnumerator SummaryBoxScaleAnimCoroutine(RectTransform summaryRect)
//        {
//            if (summaryRect == null) yield break;

//            Vector3 initialScale = summaryInitialScale;
//            Vector3 targetScale = summaryTargetScale;
//            float elapsedTime = 0f;

//            summaryRect.localScale = initialScale;

//            while (elapsedTime < summaryScaleAnimDuration)
//            {
//                elapsedTime += Time.deltaTime;
//                float progress = Mathf.Clamp01(elapsedTime / summaryScaleAnimDuration);
//                float smoothProgress = summaryScaleEaseCurve.Evaluate(progress);
//                summaryRect.localScale = Vector3.Lerp(initialScale, targetScale, smoothProgress);
//                yield return null;
//            }

//            summaryRect.localScale = targetScale;
//            VibrationManager.VibrateShort();
//        }
//        #endregion

//        #region 收束动画效果与格子变色
//        private IEnumerator ShrinkRowToCenterAndColorCoroutine(List<GridCellControl> pairedRow)
//        {
//            StartCoroutine(ShrinkRowToCenterCoroutine(pairedRow));
//            // ========== 动画结束后，给整行格子统一上色 ==========
//            ApplyPresetColorToRow(pairedRow);

//            //补充生成
//            SupplementGenerateItems(pairedRow);
//            yield return null;
//        }
//        private IEnumerator ShrinkRowToCenterCoroutine(List<GridCellControl> pairedRow)
//        {
//            // 锁定动画状态
//            ItemGenerate.Instance.LockAnimation();
//            // 过滤无效GridCell
//            List<RectTransform> cellRects = pairedRow
//                .Select(cell => cell._rectTransform)
//                .Where(rect => rect != null)
//                .ToList();

//            if (cellRects.Count == 0)
//            {
//                Debug.LogWarning("⚠️ 配对行无有效GridCell，无法执行收束动画");
//                ItemGenerate.Instance.UnBuglockAnimation();
//                yield break;
//            }

//            // ========== 1. 记录初始状态到全局字典（关键修改） ==========
//            int rowIndex = pairedRow[0].rowIndex;
//            List<GridCellInitialState> initialStates = new List<GridCellInitialState>();
//            foreach (var rect in cellRects)
//            {
//                initialStates.Add(new GridCellInitialState(rect.anchoredPosition, rect.localScale));
//            }
//            // 存储到全局（若已有该行列状态，覆盖更新）
//            if (_rowInitialStates.ContainsKey(rowIndex))
//                _rowInitialStates[rowIndex] = initialStates;
//            else
//                _rowInitialStates.Add(rowIndex, initialStates);

//            // ========== 2. 基于初始状态执行收束逻辑（原逻辑保留，仅读取全局存储的初始状态） ==========
//            List<GridCellInitialState> targetInitialStates = _rowInitialStates[rowIndex];
//            // 行的水平中心x坐标（取初始位置的最左/最右x的中点）
//            float minInitialX = targetInitialStates.Min(state => state.anchoredPosition.x);
//            float maxInitialX = targetInitialStates.Max(state => state.anchoredPosition.x);
//            float rowCenterX = (minInitialX + maxInitialX) / 2f;
//            // 每个格子初始位置相对于“行中心x”的偏移量
//            List<float> initialXOffsets = targetInitialStates.Select(state => state.anchoredPosition.x - rowCenterX).ToList();

//            float elapsedTime = 0f;
//            while (elapsedTime < shrinkAnimDuration)
//            {
//                elapsedTime += Time.deltaTime;
//                float progress = Mathf.Clamp01(elapsedTime / shrinkAnimDuration);
//                float smoothProgress = shrinkEaseCurve.Evaluate(progress);

//                // 当前间距收缩系数：从“1（原间距）”过渡到“shrinkSpacingRatio（目标间距）”
//                float currentSpacingRatio = Mathf.Lerp(1f, shrinkSpacingRatio, smoothProgress);

//                // 逐格子更新位置
//                for (int i = 0; i < cellRects.Count; i++)
//                {
//                    RectTransform cellRect = cellRects[i];
//                    GridCellInitialState initialState = targetInitialStates[i];

//                    // 水平方向：向行中心收缩（偏移量 × 当前间距系数）
//                    float newX = rowCenterX + initialXOffsets[i] * currentSpacingRatio;
//                    // 垂直方向：保持初始y坐标不变
//                    float newY = initialState.anchoredPosition.y;

//                    // 应用新位置
//                    if (enableSpacingShrink)
//                    {
//                        cellRect.anchoredPosition = new Vector2(newX, newY);
//                    }

//                    // 可选：同时执行缩放（若开启）
//                    if (enableScaleAnim)
//                    {
//                        float currentScale = Mathf.Lerp(initialState.localScale.x, shrinkTargetScale, smoothProgress);
//                        cellRect.localScale = Vector3.one * currentScale;
//                    }
//                }
//                yield return null;
//            }

//            // ========== 3. 强制校正最终状态（避免插值误差） ==========
//            float finalSpacingRatio = shrinkSpacingRatio;
//            for (int i = 0; i < cellRects.Count; i++)
//            {
//                RectTransform cellRect = cellRects[i];
//                GridCellInitialState initialState = targetInitialStates[i];

//                float finalX = rowCenterX + initialXOffsets[i] * finalSpacingRatio;
//                if (enableSpacingShrink)
//                {
//                    cellRect.anchoredPosition = new Vector2(finalX, initialState.anchoredPosition.y);
//                }

//                if (enableScaleAnim)
//                {
//                    cellRect.localScale = Vector3.one * shrinkTargetScale;
//                }
//            }

//            // 解锁动画状态
//            ItemGenerate.Instance.UnlockAnimation();

//        }

//        #endregion

//        #region 补充生成
//        private void SupplementGenerateItems(List<GridCellControl> pairedRow)
//        {
//            if(SupplementNum<=0)
//            {
//                return;
//            }
//            SupplementNum--;
//            StartCoroutine(SupplementGenerateWithAnimationCoroutine(pairedRow));
//        }
//        private IEnumerator SupplementGenerateWithAnimationCoroutine(List<GridCellControl> pairedRow)
//        {
//            // 开头锁定（覆盖整个补充生成流程）
//            ItemGenerate.Instance.LockAnimation();
//            int rowIndex = pairedRow[0].rowIndex;
//            // 等收纳框显示一会儿再开始消失（保持原有等待逻辑）
//            yield return new WaitForSeconds(DelysummaryDisappearDuration);

//            // ========== 并行执行两个动画 ==========
//            // 1. 同时启动两个动画协程，不等待单个完成
//            Coroutine summaryDisappearCoroutine = StartCoroutine(PlaySummaryBoxDisappearAnimation(rowIndex));//收纳框消失
//            Coroutine gridShrinkCoroutine = StartCoroutine(PlayGridShrinkAnimation(pairedRow));//格子缩小

//            // 2. 等待两个协程都执行完成（确保动画同步结束）
//            yield return summaryDisappearCoroutine;
//            yield return gridShrinkCoroutine;

//            // ========== 两个动画都完成后，再执行后续逻辑 ==========
//            // 第三步：清除旧物品并生成新物品
//            ClearOldItems(pairedRow);
//            ItemGenerate.Instance.SupplementGenerateItems(LevelManager.Instance.HasPairRows, rowIndex);

//            // 第四步：播放格子扩张动画
//            yield return StartCoroutine(ExpandRowFromCenterCoroutine(pairedRow));

//            // 动画完成后检查胜利条件
//            if (!LevelManager.Instance.isLevelCompleted)
//            {
//                LevelManager.Instance.CheckHasVictory();
//            }
//            ItemGenerate.Instance.UnlockAnimation();
//        }
//        #endregion

//        #region 补充生成动画效果方法

//        // 方法1：收纳框逐渐变小消失动画
//        private IEnumerator PlaySummaryBoxDisappearAnimation(int rowIndex)
//        {
//            // 查找对应行的收纳框
//            RectTransform targetSummaryBox = FindSummaryBoxByRow(rowIndex);
//            if (targetSummaryBox == null)
//            {
//                Debug.LogWarning($"未找到第{rowIndex}行的收纳框，跳过消失动画");
//                yield break;
//            }

//            float elapsedTime = 0f;
//            // 1. 保存所有初始状态（缩放 + 位置）
//            Vector3 initialScale = targetSummaryBox.localScale;
//            Vector2 initialAnchoredPos = targetSummaryBox.anchoredPosition; // 初始锚点位置

//            // 2. 定义所有目标状态（缩放 + 偏移位置）
//            Vector3 targetScale = Vector3.zero; // 缩放目标：缩为0
//            Vector2 targetAnchoredPos = new Vector2(
//                initialAnchoredPos.x, // x轴保持不变
//                initialAnchoredPos.y - summaryOffest.y // y轴向下偏移 summaryOffest.y
//            );

//            // 3. 动画循环：同时更新缩放和位置（同步插值）
//            while (elapsedTime < summaryDisappearDuration)
//            {
//                elapsedTime += Time.deltaTime;
//                float progress = Mathf.Clamp01(elapsedTime / summaryDisappearDuration); // 0~1 进度
//                float smoothProgress = summaryDisappearEaseCurve.Evaluate(progress); // 平滑进度

//                targetSummaryBox.localScale = Vector3.Lerp(initialScale, targetScale, smoothProgress);
//                targetSummaryBox.anchoredPosition = Vector2.Lerp(initialAnchoredPos, targetAnchoredPos, smoothProgress);

//                yield return null; // 等待下一帧，保证动画流畅
//            }

//            // 4. 强制校正最终状态（避免动画误差）
//            targetSummaryBox.localScale = targetScale;
//            targetSummaryBox.anchoredPosition = targetAnchoredPos;

//            // 销毁收纳框
//            Destroy(targetSummaryBox.gameObject);
//        }

//        // 方法2：格子先缩小 → 格子继续收束 → 生成物品→扩张动画协程

//        //格子先缩小并回复原颜色
//        private IEnumerator PlayGridShrinkAnimation(List<GridCellControl> pairedRow)
//        {
//            yield return StartCoroutine(PlayGridScaleAnimation(pairedRow, Vector3.one, Vector3.one * shrinkScale, shrinkBeforeGenerateDuration));
//            // 给整行所有格子统一设置颜色
//            foreach (var cell in pairedRow)
//            {
//                if (cell.cellBackground != null)
//                {
//                    cell.cellBackground.color = Color.white; // 给格子背景上色
//                }
//                else
//                {
//                    Debug.LogWarning($"⚠️ 格子{cell.name}未绑定cellBackground组件，请在Inspector中赋值");
//                }
//            }
//        }

//        private IEnumerator PlayGridScaleAnimation(List<GridCellControl> cells, Vector3 fromScale, Vector3 toScale, float duration)
//        {
//            List<RectTransform> cellRects = cells
//                .Select(cell => cell._rectTransform)
//                .Where(rect => rect != null)
//                .ToList();

//            if (cellRects.Count == 0) yield break;

//            float elapsedTime = 0f;

//            while (elapsedTime < duration)
//            {
//                elapsedTime += Time.deltaTime;
//                float progress = Mathf.Clamp01(elapsedTime / duration);
//                float smoothProgress = scaleEaseCurve.Evaluate(progress);

//                foreach (var rect in cellRects)
//                {
//                    rect.localScale = Vector3.Lerp(fromScale, toScale, smoothProgress);
//                }
//                yield return null;
//            }

//            // 强制校正最终状态
//            foreach (var rect in cellRects)
//            {
//                rect.localScale = toScale;
//            }

//        }

//        //扩张动画协程
//        private IEnumerator ExpandRowFromCenterCoroutine(List<GridCellControl> pairedRow)
//        {
//            // 锁定动画状态
//            ItemGenerate.Instance.LockAnimation();
//            // 过滤无效GridCell
//            List<RectTransform> cellRects = pairedRow
//                .Select(cell => cell._rectTransform)
//                .Where(rect => rect != null)
//                .ToList();

//            if (cellRects.Count == 0)
//            {
//                Debug.LogWarning("⚠️ 配对行无有效GridCell，无法执行扩张动画");
//                ItemGenerate.Instance.UnBuglockAnimation();
//                yield break;
//            }

//            // 读取全局存储的收束前初始状态
//            int rowIndex = pairedRow[0].rowIndex;
//            if (!_rowInitialStates.ContainsKey(rowIndex))
//            {
//                Debug.LogError($"❌ 未找到第{rowIndex}行的初始状态，无法执行扩张动画");
//                ItemGenerate.Instance.UnBuglockAnimation();
//                yield break;
//            }
//            List<GridCellInitialState> initialStates = _rowInitialStates[rowIndex];
//            if (initialStates.Count != cellRects.Count)
//            {
//                Debug.LogError($"❌ 第{rowIndex}行初始状态数量与格子数量不匹配");
//                ItemGenerate.Instance.UnBuglockAnimation();
//                yield break;
//            }

//            // 记录当前收束状态（作为扩张起点）
//            List<Vector2> currentPositions = cellRects.Select(rect => rect.anchoredPosition).ToList();
//            List<Vector3> currentScales = cellRects.Select(rect => rect.localScale).ToList();

//            // ========== 核心：复用收束动画配置，无需单独配置扩张参数 ==========
//            float animDuration = shrinkAnimDuration; // 复用收束动画时长
//            AnimationCurve easeCurve = shrinkEaseCurve; // 复用收束动画缓动曲线
//            bool enableSpacing = enableSpacingShrink; // 复用是否启用间距动画

//            // 执行扩张动画（从收束状态→初始状态）
//            float elapsedTime = 0f;
//            while (elapsedTime < animDuration)
//            {
//                elapsedTime += Time.deltaTime;
//                float progress = Mathf.Clamp01(elapsedTime / animDuration);
//                float smoothProgress = easeCurve.Evaluate(progress);

//                // 逐格子更新位置和缩放（强制恢复初始状态）
//                for (int i = 0; i < cellRects.Count; i++)
//                {
//                    RectTransform cellRect = cellRects[i];
//                    GridCellInitialState targetState = initialStates[i];
//                    Vector2 startPos = currentPositions[i];
//                    Vector3 startScale = currentScales[i];

//                    // 位置：恢复到收束前的初始位置
//                    if (enableSpacing)
//                    {
//                        cellRect.anchoredPosition = Vector2.Lerp(startPos, targetState.anchoredPosition, smoothProgress);
//                    }

//                    // 缩放：强制恢复到收束前的初始大小（解决大小不一致问题）
//                    cellRect.localScale = Vector3.Lerp(startScale, targetState.localScale, smoothProgress);
//                }
//                yield return null;
//            }

//            // ========== 强制校正最终状态（确保完全恢复初始状态） ==========
//            for (int i = 0; i < cellRects.Count; i++)
//            {
//                RectTransform cellRect = cellRects[i];
//                GridCellInitialState targetState = initialStates[i];

//                if (enableSpacing)
//                {
//                    cellRect.anchoredPosition = targetState.anchoredPosition;
//                }
//                // 最终强制恢复初始缩放
//                cellRect.localScale = targetState.localScale;
//            }

//            // 清理该行列的初始状态缓存
//            _rowInitialStates.Remove(rowIndex);

//            // 解锁动画状态
//            ItemGenerate.Instance.UnlockAnimation();
//        }


//        // 根据行索引查找对应的收纳框
//        private RectTransform FindSummaryBoxByRow(int rowIndex)
//        {
//            if (summaryBoxParent == null) return null;

//            foreach (Transform child in summaryBoxParent)
//            {
//                SummaryBoxControl summaryControl = child.GetComponent<SummaryBoxControl>();
//                if (summaryControl != null && summaryControl.targetRowIndex == rowIndex)
//                {
//                    return child.GetComponent<RectTransform>();
//                }
//            }
//            return null;
//        }

//        // 清除旧物品
//        private void ClearOldItems(List<GridCellControl> pairedRow)
//        {
//            foreach (var cell in pairedRow)
//            {
//                // 清除格子上的物品
//                if (cell._currentItem != null)
//                {
//                    ItemControl item = cell.GetComponentInChildren<ItemControl>();
//                    if (item != null)
//                    {
//                        DestroyImmediate(item.gameObject);
//                    }
//                    cell._currentItem = null;
//                    cell.isPairing=false;
//                }
//            }
//        }

//        #endregion

//        #region 房屋物品显示
//        public void ShowHousePartModel(GridCellControl cellControl)
//        {
//            int ModelIndex = CategoryToInt(cellControl._currentItem.category);
//            LevelManager.Instance._houseControl.SetPartModelActive(ModelIndex, true);
//        }


//        public void ShowHousePartModel(GridCellControl cellControl, RectTransform summaryRect)
//        {
//            // 1. 计算3D模型索引
//            int modelIndex = CategoryToInt(cellControl._currentItem.category);

//            // 2. 获取收纳框的世界位置（调用修改后的GetSummaryBoxWorldPosition）
//            Vector3 uiWorldPos = GetSummaryBoxWorldPosition(summaryRect);
//            if (uiWorldPos == Vector3.negativeInfinity)
//            {
//                Debug.LogWarning("收纳框世界位置获取失败，直接激活3D模型");
//                LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
//                return;
//            }

//            // 3. 获取3D目标模型的位置（含特效偏移）
//            HouseControl houseControl = LevelManager.Instance._houseControl;
//            if (!houseControl.HousePartModelDictionary.ContainsKey(modelIndex))
//            {
//                Debug.LogWarning($"索引{modelIndex}的3D模型不存在，直接激活");
//                houseControl.SetPartModelActive(modelIndex, true);
//                return;
//            }
//            GameObject targetModel = houseControl.HousePartModelDictionary[modelIndex];
//            Vector3 targetWorldPos = targetModel.transform.position + HouseGeneration.Instance.EffectrOffect; // 与原有特效位置偏移一致

//            // 4. 启动特效飞行协程
//            StartCoroutine(FlyEffectToTargetCoroutine(uiWorldPos, targetWorldPos, modelIndex));
//        }
//        private IEnumerator FlyEffectToTargetCoroutine(Vector3 startPos, Vector3 targetPos, int modelIndex)
//        {
//            GameObject flyEffect = EffectManager.Instance.CreateEffect(
//                effectKey: "FlyTo3D",
//                position: startPos,
//                rotation: Quaternion.identity,
//                parent: null
//            );
//            if (flyEffect == null)
//            {
//                Debug.LogWarning("飞行特效创建失败，直接激活3D模型");
//                LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
//                yield break;
//            }

//            // ========== 禁用自动销毁，由协程手动控制销毁时机 ==========
//            EffectAutoDestroy autoDestroy = flyEffect.GetComponent<EffectAutoDestroy>();
//            if (autoDestroy != null)
//            {
//                autoDestroy.DisableAutoDestroy();
//            }

//            float moveDuration = flyEffectDuration;
//            AnimationCurve moveCurve = flyEffectCurve;
//            float elapsedTime = 0f;

//            // ========== 应用终点偏移 ==========
//            Vector3 finalTargetPos = targetPos + targetPositionOffset;

//            // ========== 预计算摆动所需的坐标系 ==========
//            Vector3 flyDirection = (finalTargetPos - startPos).normalized;
//            // 计算垂直于飞行方向的横向轴（用于左右摆动）
//            Vector3 swingAxisRight = Vector3.Cross(flyDirection, Vector3.up).normalized;
//            if (swingAxisRight.sqrMagnitude < 0.01f)
//            {
//                swingAxisRight = Vector3.Cross(flyDirection, Vector3.forward).normalized;
//            }
//            // 计算垂直于飞行方向的纵向轴（用于上下波动）
//            Vector3 swingAxisUp = Vector3.Cross(swingAxisRight, flyDirection).normalized;

//            // 随机相位偏移（让每次飞行轨迹略有不同）
//            float phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

//            // 累计Z轴旋转角度
//            float accumulatedSpin = 0f;
//            // 随机摇摆相位
//            float wobblePhaseX = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
//            float wobblePhaseY = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

//            Vector3 previousPosition = startPos;

//            while (elapsedTime < moveDuration)
//            {
//                // ========== 核心：检测特效对象是否已被销毁 ==========
//                if (flyEffect == null)
//                {
//                    Debug.LogWarning("飞行特效已被销毁，终止飞行动画协程");
//                    yield break;
//                }

//                elapsedTime += Time.deltaTime;
//                float progress = Mathf.Clamp01(elapsedTime / moveDuration);

//                // ========== 蝴蝶降落效果：末端减速 ==========
//                float adjustedProgress = progress;
//                if (progress > landingSlowdownStart && landingSlowdown > 0f)
//                {
//                    // 计算减速区间内的局部进度 (0~1)
//                    float landingProgress = (progress - landingSlowdownStart) / (1f - landingSlowdownStart);
//                    // 使用平滑的减速曲线（越接近终点越慢）
//                    float slowdownFactor = 1f - landingSlowdown * Mathf.Pow(landingProgress, 0.5f);
//                    // 重新映射进度：前段正常，后段减速
//                    adjustedProgress = landingSlowdownStart + (progress - landingSlowdownStart) * slowdownFactor;
//                }

//                float smoothProgress = moveCurve.Evaluate(adjustedProgress);

//                // ========== 基础抛物线轨迹（二次贝塞尔曲线） ==========
//                Vector3 midPos = (startPos + finalTargetPos) / 2 + Vector3.up * flyEffectArcHeight;
//                Vector3 basePosition = Vector3.Lerp(
//                    Vector3.Lerp(startPos, midPos, smoothProgress),
//                    Vector3.Lerp(midPos, finalTargetPos, smoothProgress),
//                    smoothProgress
//                );

//                // ========== 流星摆动效果（多频率叠加） ==========
//                // 摆动强度：中间最强，两端收敛（像流星飘逸的感觉）
//                float intensity = swingIntensityCurve.Evaluate(progress);
//                // 末端额外衰减，确保精确到达目标
//                float endDamping = 1f - Mathf.Pow(progress, 3f);
//                float finalIntensity = intensity * endDamping;

//                // 主摆动（大幅度蛇形）
//                float mainSwing = Mathf.Sin(progress * swingFrequency * Mathf.PI * 2f + phaseOffset) * swingAmplitude;
//                // 次摆动（小幅度高频抖动，增加华丽感）
//                float secondarySwing = Mathf.Sin(progress * secondaryFrequency * Mathf.PI * 2f + phaseOffset * 1.5f) * secondaryAmplitude;
//                // 垂直波动（上下飘动）
//                float verticalSwing = Mathf.Sin(progress * verticalFrequency * Mathf.PI * 2f + phaseOffset * 0.7f) * verticalAmplitude;

//                // 组合所有摆动偏移
//                Vector3 swingOffset = swingAxisRight * (mainSwing + secondarySwing) * finalIntensity
//                                    + swingAxisUp * verticalSwing * finalIntensity;

//                Vector3 finalPosition = basePosition + swingOffset;
//                flyEffect.transform.position = finalPosition;

//                // ========== 计算旋转：Z轴自旋 + 轻微摇摆（适合2D图片特效） ==========

//                // 1. Z轴自旋（主旋转）
//                float currentSpinSpeed = spinSpeed * spinSpeedCurve.Evaluate(progress);
//                accumulatedSpin += currentSpinSpeed * Time.deltaTime;

//                // 2. 轻微摇摆（X/Y轴微小晃动，让星星更灵动）
//                float wobbleX = Mathf.Sin(progress * wobbleFrequency * Mathf.PI * 2f + wobblePhaseX) * wobbleAmount;
//                float wobbleY = Mathf.Sin(progress * wobbleFrequency * Mathf.PI * 2f + wobblePhaseY + Mathf.PI * 0.5f) * wobbleAmount;

//                // 3. 组合旋转：先摇摆，再Z轴自旋
//                Quaternion wobbleRotation = Quaternion.Euler(wobbleX, wobbleY, 0f);
//                Quaternion spinRotation = Quaternion.Euler(0f, 0f, accumulatedSpin);

//                // 如果启用朝向飞行方向，先应用朝向
//                if (orientToVelocity && elapsedTime > 0.01f)
//                {
//                    Vector3 velocity = finalPosition - previousPosition;
//                    if (velocity.sqrMagnitude > 0.0001f)
//                    {
//                        Quaternion lookRotation = Quaternion.LookRotation(velocity.normalized);
//                        flyEffect.transform.rotation = lookRotation * spinRotation * wobbleRotation;
//                    }
//                    else
//                    {
//                        flyEffect.transform.rotation = spinRotation * wobbleRotation;
//                    }
//                }
//                else
//                {
//                    // 纯Z轴旋转 + 摇摆（2D图片推荐）
//                    flyEffect.transform.rotation = spinRotation * wobbleRotation;
//                }

//                previousPosition = finalPosition;

//                yield return null;
//            }

//            // ========== 末尾也要检测对象是否存活 ==========
//            if (flyEffect != null)
//            {
//                flyEffect.transform.position = finalTargetPos;
//                //EffectManager.Instance.CreateEffect(
//                //    effectKey: "3DModelAppear",
//                //    position: targetPos,
//                //    rotation: flyEffect.transform.rotation,
//                //    parent: LevelManager.Instance._houseControl.transform
//                //);
//                // 若粒子系统未自动销毁，手动销毁（避免内存泄漏）
//                Destroy(flyEffect);
//            }

//            LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
//        }

//        // 转换UI位置到世界坐标
//        private Vector3 GetSummaryBoxWorldPosition(RectTransform summaryRect)
//        {
//            if (summaryRect == null) return Vector3.negativeInfinity;

//            Canvas canvas = summaryRect.GetComponentInParent<Canvas>();
//            if (canvas == null) return summaryRect.position;

//            // （原方法内的renderMode判断逻辑保持不变）
//            switch (canvas.renderMode)
//            {
//                case RenderMode.ScreenSpaceCamera:
//                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, summaryRect.position);
//                    return canvas.worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, canvas.planeDistance));
//                case RenderMode.ScreenSpaceOverlay:
//                    Vector3 overlayPos = summaryRect.position;
//                    overlayPos.z = 1f;
//                    return overlayPos;
//                case RenderMode.WorldSpace:
//                    return summaryRect.position;
//                default:
//                    return summaryRect.position;
//            }
//        }

//        //获取物品类型转换索引
//        public int CategoryToInt(ItemCategory category)
//        {
//            var _categoryToModelMapping = LevelManager.Instance.currentLevelData._categoryToModelMapping;
//            // 处理数组为空/无配置的情况
//            if (_categoryToModelMapping == null || _categoryToModelMapping.Count == 0)
//            {
//                Debug.LogWarning($"类别-模型映射数组未配置（_categoryToModelMapping 为空或长度为0）");
//                return 1;
//            }

//            // 遍历映射数组匹配类别
//            foreach (var mapping in _categoryToModelMapping)
//            {
//                if (mapping != null && mapping.targetCategory == category)
//                {
//                    return mapping.modelIndex;
//                }
//            }

//            // 未找到匹配类别的警告与默认返回
//            Debug.LogWarning($"类别 {category} 未在映射数组中配置，返回默认索引1");
//            return 1;
//        }

//        #endregion

//        #region 配对成功后视觉辅助
//        // 设置归纳框文字的辅助方法
//        private void SetSummaryBoxText(GameObject summaryBox, ItemCategory itemCategory)
//        {
//            if (summaryBox == null)
//            {
//                Debug.LogWarning("⚠️ 归纳框为空，无法设置文字！");
//                return;
//            }

//            // 获取子节点中的 TextMeshPro 组件（支持 TextMeshPro - Text 和 TextMeshPro - Text UI）
//            TextMeshProUGUI tmproText = summaryBox.GetComponentInChildren<TextMeshProUGUI>(true); // true = 包含禁用的子节点
//            TextMeshPro tmproWorldText = summaryBox.GetComponentInChildren<TextMeshPro>(true);

//            // 优先使用 UI 版本的 TextMeshPro，没有则尝试世界空间版本
//            if (tmproText != null)
//            {
//                // 将枚举转换为文字（可自定义格式，如大写、添加前缀等）
//                tmproText.text = itemCategory.ToString().ToUpper();
//                // 可选：设置文字颜色、字体大小等
//               // tmproText.color = Color.white;
//                //tmproText.fontSize = 24;
//            }
//            else if (tmproWorldText != null)
//            {
//                tmproWorldText.text = itemCategory.ToString();
//            }
//            else
//            {
//                Debug.LogWarning($"⚠️ 归纳框{summaryBox.name}的子节点中未找到 TextMeshPro 组件！");
//            }
//        }
//        // 按预设顺序给整行格子统一上色
//        private void ApplyPresetColorToRow(List<GridCellControl> row)
//        {
//            // 安全校验1：颜色数组未配置 → 跳过
//            if (SummaryColor == null || SummaryColor.Length == 0)
//            {
//                Debug.LogWarning("⚠️ 请先在Inspector中给SummaryColor数组配置颜色");
//                return;
//            }
//            // 安全校验2：行数据无效 → 跳过
//            if (row == null || row.Count == 0)
//            {
//                Debug.LogWarning("⚠️ 待上色的行数据无效");
//                return;
//            }

//            // 按预设顺序取颜色（循环使用数组）
//            int targetColorIndex = _currentColorIndex % SummaryColor.Length;
//            Color targetColor = SummaryColor[targetColorIndex];
//            //Debug.Log($"✅ 给当前行应用颜色（索引{targetColorIndex}：{targetColor}）");

//            // 给整行所有格子统一设置颜色
//            foreach (var cell in row)
//            {
//                if (cell.cellBackground != null)
//                {

//                    cell.cellBackground.color = targetColor; // 给格子背景上色
//                }
//                else
//                {
//                    Debug.LogWarning($"⚠️ 格子{cell.name}未绑定cellBackground组件，请在Inspector中赋值");
//                }
//            }

//            // 颜色计数器递增（下一行用下一个颜色）
//            _currentColorIndex++;
//        }
//        //清理上一关的收纳框
//        public void ClearAllSummaryBox()
//        {
//            if (summaryBoxParent == null) return;
//            for (int i = summaryBoxParent.childCount - 1; i >= 0; i--)
//            {
//                DestroyImmediate(summaryBoxParent.GetChild(i).gameObject);
//            }
//        }
//        #endregion

//        #region 逻辑辅助方法

//        //改变行的isPairing状态
//        private void SetPairedRowState(List<GridCellControl> pairedCells,bool state)
//        {
//            foreach (var cell in pairedCells)
//            {
//                cell.isPairing = state;
//            }
//        }
//        //：筛选所有「完整、无空物品、未配对」的行
//        private List<List<GridCellControl>> GetAllUnpairedRows()
//        {
//            Dictionary<int, List<GridCellControl>> rowToCells = new Dictionary<int, List<GridCellControl>>();
//            foreach (var cell in allGridCells)
//            {
//                int row = cell.rowIndex;
//                if (!rowToCells.ContainsKey(row))
//                {
//                    rowToCells[row] = new List<GridCellControl>();
//                }
//                rowToCells[row].Add(cell);
//            }

//            List<List<GridCellControl>> unpairedRows = new List<List<GridCellControl>>();
//            foreach (var rowKvp in rowToCells)
//            {
//                List<GridCellControl> rowCells = rowKvp.Value;
//                // 筛选条件：完整行（格子数=列数）+ 无空物品 + 未配对（整行不是全部已配对）
//                bool isComplete = rowCells.Count == GridCellGenerate.Instance.colCount;
//                bool noEmpty = !rowCells.Any(cell => cell._currentItem == null);
//                bool isUnpaired = !rowCells.All(cell => cell.isPairing);

//                if (isComplete && noEmpty && isUnpaired)
//                {
//                    unpairedRows.Add(rowCells);
//                }
//            }
//            return unpairedRows;
//        }

//        // 获取当前的所有格子
//        public void UpdateAllCell()
//        {
//            allGridCells.Clear();
//            // 遍历父节点下所有子物体，筛选带 GridCellControl 的格子
//            foreach (Transform child in GridCellGenerate.Instance.gridParent)
//            {
//                GridCellControl cell = child.GetComponent<GridCellControl>();
//                if (cell != null)
//                {
//                    allGridCells.Add(cell);
//                }
//            }
//        }
//        #endregion
//    }
//}
#endregion 

