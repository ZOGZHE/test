using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static ConnectMaster.LevelData;
using static UnityEngine.Rendering.DebugUI.Table;

namespace ConnectMaster
{
    public class ItemPairing : MonoBehaviour
    {
        public static ItemPairing Instance;

        [HideInInspector] public List<GridCellControl> allGridCells;

        #region 动画相关
        #region 备用飞行特效数据
        // 特效飞行动画配置
        //[Header("特效飞行动画配置")]
        //[Tooltip("特效从UI飞到3D模型的总时长（秒）")]
        //public float flyEffectDuration = 0.8f;
        //[Tooltip("特效飞行的缓动曲线")]
        //public AnimationCurve flyEffectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        //[Tooltip("特效飞行抛物线的高度（世界坐标单位，越高抛物线越明显）")]
        //public float flyEffectArcHeight = 1.5f;
        //[Tooltip("螺线的旋转圈数（正数为顺时针，负数为逆时针）")]
        //public float flyEffectSpiralTurns = 1f; // 螺线旋转圈数
        //[Tooltip("螺线的初始半径（世界坐标单位，越大螺线越宽）")]
        //public float flyEffectSpiralStartRadius = 1f; // 螺线初始半径
        //[Tooltip("螺线的结束半径（世界坐标单位，建议0以收敛到目标点）")]
        //public float flyEffectSpiralEndRadius = 0f; // 螺线结束半径
        #endregion
        [Header("特效飞行动画配置")]
        [Tooltip("特效从UI飞到3D模型的总时长（秒）")]
        public float flyEffectDuration = 0.8f;
        [Tooltip("特效飞行的缓动曲线")]
        public AnimationCurve flyEffectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("特效飞行抛物线的高度（世界坐标单位，越高抛物线越明显）")]
        public float flyEffectArcHeight = 1.5f;
        [Tooltip("螺线的旋转圈数（正数为顺时针，负数为逆时针）")]
        public float flyEffectSpiralTurns = 1f;
        [Tooltip("螺线半径曲线（X=动画进度0~1，Y=半径绝对值（世界坐标），起始Y=0避免偏移）")]
        // 默认曲线：0→1→0（Y轴1=1单位半径，可直接拖拽调整绝对值）
        public AnimationCurve flyEffectSpiralRadiusCurve = new AnimationCurve(
            new Keyframe(0, 0),    // 起始：半径0（无偏移）
            new Keyframe(0.5f, 1), // 中点：半径1（最大幅度）
            new Keyframe(1, 0)     // 结束：半径0（收敛到目标）
        );


        //配对行动画配置
        [Header("配对行收束扩张动画配置")]
        [Tooltip("动画总时长（秒）")]
        public float shrinkAnimDuration = 0.5f;
        [Tooltip("水平间距收缩比例（0=完全贴紧，1=原间距；建议0.3~0.7）")]
        [Range(0f, 1f)]
        public float shrinkSpacingRatio = 0.5f; // 最终间距是原间距的50%
        [Tooltip("动画结束后的目标缩放（0=完全消失，1=原大小）")]
        [Range(0f, 1f)]
        public float shrinkTargetScale = 1f; // 可选：收束时是否缩放（默认保持原大小）
        [Tooltip("动画缓动曲线")]
        public AnimationCurve shrinkEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("是否启用水平间距收束")]
        public bool enableSpacingShrink = true;
        [Tooltip("是否启用缩放动画")]
        public bool enableScaleAnim = false; // 若仅要间距收束，可关闭缩放

        // 归纳框生成动画配置
        [Header("归纳框生成动画配置")]
        [Tooltip("动画时长（秒）")]
        public float summaryScaleAnimDuration = 0.3f;
        [Tooltip("动画缓动曲线")]
        public AnimationCurve summaryScaleEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("初始缩放比例（0=完全隐藏，1=原大小）")]
        public Vector3 summaryInitialScale = Vector3.zero;
        [Tooltip("目标缩放比例（1=原大小）")]
        public Vector3 summaryTargetScale = Vector3.one;
        [Tooltip("物品缩放比例")]
        public Vector3 ItemTargetScale = Vector3.one;

        // 归纳框消失动画配置
        [Header("收纳框消失动画配置")]
        [Tooltip("收纳框消失动画时长")]
        public float summaryDisappearDuration = 0.4f;
        [Tooltip("收纳框消失动画时长")]
        public float DelysummaryDisappearDuration = 1f;
        [Tooltip("收纳框消失动画曲线")]
        public AnimationCurve summaryDisappearEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        //补充生成动画配置
        [Header("补充生成格子缩小动画动画配置")]
        [Tooltip("格子缩小动画时长")]
        public float shrinkBeforeGenerateDuration = 0.3f;
        [Tooltip("格子缩放动画曲线")]
        public AnimationCurve scaleEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("格子缩小比例")]
        [Range(0f, 1f)]
        public float shrinkScale = 0.3f;

        [Header("格子移动及其复原动画配置")]
        [Tooltip("格子聚合（移动到同一位置）动画时长（秒）")]
        public float gridMoveTogetherDuration = 0.5f;
        [Tooltip("格子聚合动画缓动曲线")]
        public AnimationCurve gridMoveTogetherCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("格子复原（回到初始位置）动画时长（秒）")]
        public float gridMoveBackDuration = 0.5f;
        [Tooltip("格子复原动画缓动曲线")]
        public AnimationCurve gridMoveBackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Tooltip("移动动画是否启用位置插值（关闭则瞬间移动）")]
        public bool enableGridMoveSmooth = true;
        [Tooltip("移动过程中是否保持格子缩放不变")]
        public bool keepScaleDuringMove = true;
        [Tooltip("聚合时目标格子索引（1=第一列，2=第二列...）")]
        [Range(1, 4)]
        public int gridMoveTargetIndex = 4; // 默认移动到第4列位置
        [Tooltip("移动动画是否忽略Z轴（UI专用，避免深度偏移）")]
        public bool ignoreZAxisInMove = true;


        // 存储单个格子初始状态的结构体（位置+缩放）
        private struct GridCellInitialState
        {
            public Vector2 anchoredPosition;
            public Vector3 localScale;

            public GridCellInitialState(Vector2 pos, Vector3 scale)
            {
                anchoredPosition = pos;
                localScale = scale;
            }
        }
        // 全局存储每行收束前的初始状态（key=行索引，value=该行每个格子的初始状态）
        private Dictionary<int, List<GridCellInitialState>> _rowInitialStates = new Dictionary<int, List<GridCellInitialState>>();
        // 存储格子移动前的初始位置（复用已有结构体，扩展存储逻辑）
        private Dictionary<string, GridCellInitialState> _gridMoveInitialStates = new Dictionary<string, GridCellInitialState>();
        #endregion

        #region 归纳框数据配置
        [Header("归纳框数据配置")]
        [Tooltip("归纳框")]
        public GameObject[] SummaryBox;
        [Tooltip("归纳框的UI父节点")]
        public RectTransform summaryBoxParent;
        [Tooltip("归纳框偏移")]
        public Vector2 summaryOffest;
        [Tooltip("归纳后格子颜色")]
        public Color[] SummaryColor;
        // 当前使用的归纳框索引计数器（用于顺序循环）
        private int _currentSummaryIndex = 0;
        //有效预制体索引列表（仅存储非空预制体的索引，避免重复过滤）
        private List<int> _validSummaryIndices = new List<int>();
        // 当前使用的格子颜色索引计数器（与归纳框同步顺序）
        private int _currentColorIndex = 0;
        #endregion

        #region 配对相关
        // 避免同时触发出错缓存待处理的配对行（队列保证处理顺序）
        private Queue<List<GridCellControl>> _pairedRowQueue = new Queue<List<GridCellControl>>();
        // 标记是否正在处理配对（避免并行）
        private bool _isProcessingPair = false;

        public Action<ItemCategory> ParingRow;
        #endregion

        #region 补充相关
        //判断是否还需要补充 避免重复动画
        internal int SupplementNum = 0;
        #endregion

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                allGridCells = new List<GridCellControl>();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            Initialize();
            // 初始化归纳框索引列表（过滤空预制体）
            InitValidSummaryIndices();
        }
        #endregion

        #region 初始化
        private void Initialize()
        {

        }

        // 初始化有效预制体索引（只保留非空预制体的索引）
        private void InitValidSummaryIndices()
        {
            _validSummaryIndices.Clear();
            _currentSummaryIndex = 0; // 重置归纳框计数器
            _currentColorIndex = 0;   // 重置颜色计数器（关卡重置后从第一个颜色开始）

            if (SummaryBox == null || SummaryBox.Length == 0)
            {
                Debug.LogWarning("归纳框预制体数组为空，无法初始化有效索引");
                return;
            }

            // 只添加非空预制体的索引到有效列表
            for (int i = 0; i < SummaryBox.Length; i++)
            {
                if (SummaryBox[i] != null)
                {
                    _validSummaryIndices.Add(i);
                }
                else
                {
                    Debug.LogWarning($"归纳框数组索引{i}对应的预制体为空，已过滤");
                }
            }
        }

        #endregion

        #region 检测是否有配对的物品
        // 检查所有行：该行所有物品的 Category 是否完全一致
        public bool CheckHavePairing()
        {
            UpdateAllCell();
            // 安全校验：格子列表为空则直接返回
            if (allGridCells == null || allGridCells.Count == 0)
            {
                Debug.LogWarning("格子列表为空，无法检查配对");
                return false;
            }

            // 1. 按行分组：key = 行号（rowIndex），value = 该行所有格子
            Dictionary<int, List<GridCellControl>> rowToCells = new Dictionary<int, List<GridCellControl>>();
            foreach (var cell in allGridCells)
            {
                int row = cell.rowIndex;
                // 若字典中没有当前行的key，创建新列表
                if (!rowToCells.ContainsKey(row))
                {
                    rowToCells[row] = new List<GridCellControl>();
                }
                // 将当前格子加入对应行的列表
                rowToCells[row].Add(cell);
            }

            bool hasPairedRow = false;

            // 2. 遍历每行，检查物品分类一致性
            foreach (var rowKvp in rowToCells)
            {
                int currentRow = rowKvp.Key;
                List<GridCellControl> rowCells = rowKvp.Value;

                // 边界校验1：该行格子数量是否等于列数（避免漏格子）
                if (rowCells.Count != GridCellGenerate.Instance.colCount)
                {
                    Debug.LogWarning($"第{currentRow}行格子数量不完整（应有{GridCellGenerate.Instance.colCount}个，实际{rowCells.Count}个）");
                    continue;
                }

                // 边界校验2：该行是否有格子没有物品（空物品无法配对）
                bool hasEmptyItem = rowCells.Any(cell => cell._currentItem == null);
                if (hasEmptyItem)
                {
                    Debug.Log($"第{currentRow}行存在空物品，跳过校验");
                    continue;
                }
                // 重复校验：该行是否已经配对过
                bool hadPairing = rowCells.All(cell => cell.isPairing); // 整行都已配对才判定为“已处理”
                if (hadPairing)
                {
                    //Debug.Log($"第{currentRow}行已配对，跳过重复校验");
                    continue;
                }
                // 3. 校验该行所有物品的 Category 是否一致
                ItemCategory targetCategory = rowCells[0]._currentItem.category; // 以第一个物品为基准
                bool isRowPaired = true;

                foreach (var cell in rowCells)
                {
                    // 若当前物品分类与基准不一致，标记该行未配对
                    if (cell._currentItem.category != targetCategory)
                    {
                        isRowPaired = false;
                        break;
                    }
                }

                // 4. 处理配对成功的行
                if (isRowPaired)
                {
                    hasPairedRow = true;
                    Debug.Log($"✅ 第{currentRow}行配对成功！物品分类：{targetCategory}");
                    //不可拖动交换
                    foreach (var cell in rowCells)
                    {
                        ItemControl targetItemContorl = cell.GetComponentInChildren<ItemControl>();
                        targetItemContorl.canDrag = false;
                        targetItemContorl.canExchange = false;
                    }
                    //配对成功后的逻辑
                    _pairedRowQueue.Enqueue(rowCells);

                }
            }
            // 触发队列处理（如果当前没有正在处理的配对）
            if (hasPairedRow && !_isProcessingPair)
            {
                ProcessNextPairedRow();
            }

            return hasPairedRow;
        }
        //从队列中取出下一行处理
        private void ProcessNextPairedRow()
        {
            // 队列空 → 重置为未处理，返回
            if (_pairedRowQueue.Count == 0)
            {
                _isProcessingPair = false;
                return;
            }
            // 标记为正在处理，避免并行
            _isProcessingPair = true;
            // 取出队列首行
            List<GridCellControl> nextRow = _pairedRowQueue.Dequeue();
            UpdateAllCell();
            ItemCategory category = nextRow[0]._currentItem.category;

            //处理配对行
            HandlePairedRow(nextRow, category);
            //成功配对了一行，配对行++
            LevelManager.Instance.AddHasPairRows();

            //检查是否胜利
            LevelManager.Instance.CheckHasVictory();
        }
        #endregion

        #region 处理配对的物品
        private void HandlePairedRow(List<GridCellControl> pairedCells, ItemCategory category)
        {
            // 获取所有未配对行 + 统计数量
            List<List<GridCellControl>> allUnpairedRows = GetAllUnpairedRows();
            int unpairedCount = allUnpairedRows.Count;

            List<GridCellControl> targetRowCells;
            if (unpairedCount == 2)
            {
                //Debug.Log($"只剩{unpairedCount}行未配对");
                targetRowCells = pairedCells; // 目标行=自身，跳过动画
            }
            else
            {
                targetRowCells = FindHighestUnpairedRow();
                if (targetRowCells == null || targetRowCells.Count == 0)
                {
                    Debug.LogWarning("❌ 未找到目标行，设为自身");
                    targetRowCells = pairedCells;
                }
                //Debug.Log($"最终targetRowCells：行{targetRowCells[0].rowIndex}");
            }
            foreach (var cell in pairedCells)
            {
                cell.isPairing = true;
            }

            StartCoroutine(SwapEntireRowsCoroutine(pairedCells, targetRowCells, ProcessNextPairedRow));

        }
        // 寻找「行数最小（相对最高）且未配对」的行（目标行）
        private List<GridCellControl> FindHighestUnpairedRow()
        {
            UpdateAllCell();
            Dictionary<int, List<GridCellControl>> rowToCells = new Dictionary<int, List<GridCellControl>>();
            foreach (var cell in allGridCells)
            {
                int row = cell.rowIndex;
                if (!rowToCells.ContainsKey(row))
                {
                    rowToCells[row] = new List<GridCellControl>();
                }
                rowToCells[row].Add(cell);
            }

            List<List<GridCellControl>> unpairedRows = new List<List<GridCellControl>>();
            foreach (var rowKvp in rowToCells)
            {
                List<GridCellControl> rowCells = rowKvp.Value;
                bool isComplete = rowCells.Count == GridCellGenerate.Instance.colCount;
                bool noEmpty = !rowCells.Any(cell => cell._currentItem == null);
                bool isUnpaired = !rowCells.All(cell => cell.isPairing);

                if (isComplete && noEmpty && isUnpaired)
                {
                    unpairedRows.Add(rowCells);
                }
            }

            // 打印排序后的候选行号
            var sortedRows = unpairedRows.OrderBy(row => row[0].rowIndex).ToList();
            //Debug.Log($"候选未配对行（排序后）：{string.Join(",", sortedRows.Select(r => r[0].rowIndex))}");

            // 最终结果
            var result = sortedRows.FirstOrDefault()?.OrderBy(c => c.colIndex).ToList();
            //Debug.Log(result != null ? $"选中最高行：{result[0].rowIndex}" : "无符合条件的行");
            return result;
        }
        #endregion

        #region 核心方法： 整行交换

        //整行交换协程：复用SwapAnimationCoroutine，所有列物品同时交换
        private IEnumerator SwapEntireRowsCoroutine(List<GridCellControl> pairedRow, List<GridCellControl> targetRow, Action ProcessNextPairedRow)
        {
            ItemGenerate.Instance.LockAnimation();
            // 按列号排序（确保第1列和第1列交换，第2列和第2列交换）
            List<GridCellControl> sortedPairedRow = pairedRow.OrderBy(cell => cell.colIndex).ToList();
            List<GridCellControl> sortedTargetRow = targetRow.OrderBy(cell => cell.colIndex).ToList();

            int completedSwapCount = 0; // 记录已完成的交换数量
            int totalValidSwaps = 0;    // 记录有效启动的交换数量

            // 遍历所有列，同时启动所有交换协程（不等待单个完成）
            for (int col = 0; col < sortedPairedRow.Count; col++)
            {
                GridCellControl pairedCell = sortedPairedRow[col];
                GridCellControl targetCell = sortedTargetRow[col];

                // 获取两个格子中的物品（安全校验）
                ItemControl pairedItem = pairedCell.GetComponentInChildren<ItemControl>();
                ItemControl targetItem = targetCell.GetComponentInChildren<ItemControl>();

                if (pairedItem == null || targetItem == null)
                {
                    Debug.LogError($"❌ 第{col + 1}列交换失败：物品为空（配对行物品：{pairedItem != null}，目标行物品：{targetItem != null}）");
                    continue;
                }

                totalValidSwaps++; // 统计有效交换数
                int currentCol = col; // 闭包捕获临时变量

                // 启动交换协程（不等待，实现同时交换）
                StartCoroutine(pairedItem.SwapAnimationCoroutine(pairedItem, targetItem, () =>
                {
                    completedSwapCount++;
                    //Debug.Log($"🔄 第{currentCol + 1}列交换完成（配对行{pairedRow[0].rowIndex}→目标行{targetRow[0].rowIndex}）");
                }));
            }

            // 等待所有有效交换协程完成（直到完成数等于有效启动数）
            while (completedSwapCount < totalValidSwaps)
            {
                yield return null;
            }

            // 所有列交换完成后，更新配对状态
            SetPairedRowState(pairedRow, false);
            SetPairedRowState(targetRow, true);

            //触发提示匹配行
            ParingRow?.Invoke(targetRow[0]._currentItem.category);


            //-------------------------------------------------
            // 交换完成后，顺序执行动画
            int animationCompletedCount = 0; // 记录已完成跳动动画的格子数量
            int totalCellCount = targetRow.Count; // 目标行总格子数

            // 第一步：启动所有格子的跳动动画，并用回调统计完成状态
            foreach (var cell in targetRow)
            {
                if (cell == null) continue;

                // 启动跳动动画，并传入“动画完成回调”
                StartCoroutine(cell.ExchangeDoneAnimation(() =>
                {
                    animationCompletedCount++; // 某个格子动画完成，计数器+1
                    //Debug.Log($"格子[{cell.rowIndex},{cell.colIndex}] 跳动动画完成，已完成{animationCompletedCount}/{totalCellCount}");
                }));
            }

            // 等待所有格子的跳动动画全部完成（关键：直到计数器等于总格子数）
            while (animationCompletedCount < totalCellCount)
            {
                yield return null; // 每帧检查一次，不阻塞主线程
            }
            //Debug.Log("✅ 所有格子跳动动画执行完成！");

            // 第二步：所有跳动动画完成后，生成归纳框（同步执行，执行完再往下走）
            SummaryBoxGenerate(targetRow);
            //Debug.Log("✅ 归纳框生成完成！");

            // 第三步：执行收束+上色动画，并等待其完成
            yield return StartCoroutine(ShrinkRowToCenterAndColorCoroutine(targetRow));
            //Debug.Log("✅ 收束动画+格子上色完成！");
            //-------------------------------------------------

            // 交换完成后重新更新所有格子的提示颜色放置bug
            HintManager.Instance.UpdateColorsAfterSwap();
            StartCoroutine(DelayedProcessNextPairedRow(0.2f));//间隔0.2f触发

            ItemGenerate.Instance.UnlockAnimation();
        }
        // 延迟执行ProcessNextPairedRow，并保证解锁动画
        private IEnumerator DelayedProcessNextPairedRow(float delay)
        {
            ItemGenerate.Instance.LockAnimation();

            // 等待指定延迟（这0.2秒期间Lock生效，IsAnimating=true）
            yield return new WaitForSeconds(delay);

            try
            {
                // 执行队列处理逻辑
                ProcessNextPairedRow();
            }
            catch (Exception e)
            {
                Debug.LogError($"延迟执行ProcessNextPairedRow出错：{e.Message}");
            }
            finally
            {
                // 无论是否报错，都解锁动画（避免计数异常）
                ItemGenerate.Instance.UnlockAnimation();

            }
        }

        #endregion

        #region 归纳框生成
        // 归纳框生成方法（按预制体数组顺序0→1→2...循环，无状态列表）
        private void SummaryBoxGenerate(List<GridCellControl> pairingRow)
        {
            #region  1. 安全校验
            // 1. 安全校验
            if (SummaryBox == null || SummaryBox.Length == 0)
            {
                Debug.LogError("❌ 归纳框预制体数组SummaryBox为空，请先赋值！");
                return;
            }
            if (summaryBoxParent == null)
            {
                Debug.LogError("❌ 归纳框父节点summaryBoxParent未赋值！");
                return;
            }
            if (pairingRow == null || pairingRow.Count == 0)
            {
                Debug.LogError("❌ 配对行数据无效，无法生成归纳框！");
                return;
            }
            GridCellControl firstPairCell = pairingRow[0];
            if (firstPairCell._rectTransform == null)
            {
                Debug.LogError("❌ 配对行的格子缺少RectTransform组件，无法获取位置！");
                return;
            }
            // 校验是否有有效预制体
            if (_validSummaryIndices.Count == 0)
            {
                Debug.LogError("❌ 无有效归纳框预制体，无法生成！");
                return;
            }
            #endregion

            // 2. 固定顺序获取目标索引
            int targetIndex = _validSummaryIndices[_currentSummaryIndex];

            // 3. 更新计数器（循环逻辑：到末尾后重置为0）
            _currentSummaryIndex = (_currentSummaryIndex + 1) % _validSummaryIndices.Count;

            // 4. 实例化归纳框
            GameObject targetSummaryPrefab = SummaryBox[targetIndex];
            GameObject newSummaryBox = Instantiate(
                targetSummaryPrefab,
                summaryBoxParent,
                false
            );
            SummaryBoxControl newSummaryBoxControl = newSummaryBox.GetComponent<SummaryBoxControl>();
            newSummaryBoxControl.SetTargetRow(firstPairCell.rowIndex);

            newSummaryBox.transform.localScale = Vector3.one;
            SetSummaryBoxText(newSummaryBox, firstPairCell._currentItem.category);

            // 5. 设置位置
            RectTransform summaryRect = newSummaryBox.GetComponent<RectTransform>();

            summaryRect.anchoredPosition = new Vector2(summaryOffest.x, summaryOffest.y + firstPairCell._rectTransform.anchoredPosition.y);


            //------3d场景房屋显现------

            ShowHousePartModel(firstPairCell, summaryRect);
            //------3d场景房屋显现------

            //------振动------
            VibrationManager.VibrateLong();
            //------振动------

            // 启动缩放动画
            StartCoroutine(SummaryBoxScaleAnimCoroutine(newSummaryBox.GetComponent<RectTransform>()));

        }

        //归纳框缩放动画协程
        private IEnumerator SummaryBoxScaleAnimCoroutine(RectTransform summaryRect)
        {
            if (summaryRect == null) yield break;

            Vector3 initialScale = summaryInitialScale;
            Vector3 targetScale = summaryTargetScale;
            float elapsedTime = 0f;

            summaryRect.localScale = initialScale;

            while (elapsedTime < summaryScaleAnimDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / summaryScaleAnimDuration);
                float smoothProgress = summaryScaleEaseCurve.Evaluate(progress);
                summaryRect.localScale = Vector3.Lerp(initialScale, targetScale, smoothProgress);
                yield return null;
            }

            summaryRect.localScale = targetScale;

        }
        #endregion

        #region 收束动画效果与格子变色
        private IEnumerator ShrinkRowToCenterAndColorCoroutine(List<GridCellControl> pairedRow)
        {
            StartCoroutine(ShrinkRowToCenterCoroutine(pairedRow));
            // ========== 动画结束后，给整行格子统一上色 ==========
            ApplyPresetColorToRow(pairedRow);

            //补充生成
            SupplementGenerateItems(pairedRow);
            yield return null;

        }
        private IEnumerator ShrinkRowToCenterCoroutine(List<GridCellControl> pairedRow)
        {
            // 锁定动画状态
            ItemGenerate.Instance.LockAnimation();
            // 过滤无效GridCell
            List<RectTransform> cellRects = pairedRow
                .Select(cell => cell._rectTransform)
                .Where(rect => rect != null)
                .ToList();

            if (cellRects.Count == 0)
            {
                Debug.LogWarning("⚠️ 配对行无有效GridCell，无法执行收束动画");
                ItemGenerate.Instance.UnBuglockAnimation();
                yield break;
            }

            // ========== 1. 记录初始状态到全局字典（关键修改） ==========
            int rowIndex = pairedRow[0].rowIndex;
            List<GridCellInitialState> initialStates = new List<GridCellInitialState>();
            foreach (var rect in cellRects)
            {
                initialStates.Add(new GridCellInitialState(rect.anchoredPosition, rect.localScale));
            }
            // 存储到全局（若已有该行列状态，覆盖更新）
            if (_rowInitialStates.ContainsKey(rowIndex))
                _rowInitialStates[rowIndex] = initialStates;
            else
                _rowInitialStates.Add(rowIndex, initialStates);

            // ========== 2. 基于初始状态执行收束逻辑（原逻辑保留，仅读取全局存储的初始状态） ==========
            List<GridCellInitialState> targetInitialStates = _rowInitialStates[rowIndex];
            // 行的水平中心x坐标（取初始位置的最左/最右x的中点）
            float minInitialX = targetInitialStates.Min(state => state.anchoredPosition.x);
            float maxInitialX = targetInitialStates.Max(state => state.anchoredPosition.x);
            float rowCenterX = (minInitialX + maxInitialX) / 2f;
            // 每个格子初始位置相对于“行中心x”的偏移量
            List<float> initialXOffsets = targetInitialStates.Select(state => state.anchoredPosition.x - rowCenterX).ToList();

            float elapsedTime = 0f;
            while (elapsedTime < shrinkAnimDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / shrinkAnimDuration);
                float smoothProgress = shrinkEaseCurve.Evaluate(progress);

                // 当前间距收缩系数：从“1（原间距）”过渡到“shrinkSpacingRatio（目标间距）”
                float currentSpacingRatio = Mathf.Lerp(1f, shrinkSpacingRatio, smoothProgress);

                // 逐格子更新位置
                for (int i = 0; i < cellRects.Count; i++)
                {
                    RectTransform cellRect = cellRects[i];
                    GridCellInitialState initialState = targetInitialStates[i];

                    // 水平方向：向行中心收缩（偏移量 × 当前间距系数）
                    float newX = rowCenterX + initialXOffsets[i] * currentSpacingRatio;
                    // 垂直方向：保持初始y坐标不变
                    float newY = initialState.anchoredPosition.y;

                    // 应用新位置
                    if (enableSpacingShrink)
                    {
                        cellRect.anchoredPosition = new Vector2(newX, newY);
                    }

                    // 可选：同时执行缩放（若开启）
                    if (enableScaleAnim)
                    {
                        float currentScale = Mathf.Lerp(initialState.localScale.x, shrinkTargetScale, smoothProgress);
                        cellRect.localScale = Vector3.one * currentScale;
                    }
                }
                yield return null;
            }

            // ========== 3. 强制校正最终状态（避免插值误差） ==========
            float finalSpacingRatio = shrinkSpacingRatio;
            for (int i = 0; i < cellRects.Count; i++)
            {
                RectTransform cellRect = cellRects[i];
                GridCellInitialState initialState = targetInitialStates[i];

                float finalX = rowCenterX + initialXOffsets[i] * finalSpacingRatio;
                if (enableSpacingShrink)
                {
                    cellRect.anchoredPosition = new Vector2(finalX, initialState.anchoredPosition.y);
                }

                if (enableScaleAnim)
                {
                    cellRect.localScale = Vector3.one * shrinkTargetScale;
                }
            }

            // 解锁动画状态
            ItemGenerate.Instance.UnlockAnimation();

        }

        #endregion

        #region 补充生成
        private void SupplementGenerateItems(List<GridCellControl> pairedRow)
        {
            if (SupplementNum <= 0)
            {
                return;
            }
            SupplementNum--;
            StartCoroutine(SupplementGenerateWithAnimationCoroutine(pairedRow));
        }
        //
        private IEnumerator SupplementGenerateWithAnimationCoroutine(List<GridCellControl> pairedRow)
        {
            // 开头锁定（覆盖整个补充生成流程）
            ItemGenerate.Instance.LockAnimation();
            int rowIndex = pairedRow[0].rowIndex;
            // 等收纳框显示一会儿再开始消失（保持原有等待逻辑）
            yield return new WaitForSeconds(DelysummaryDisappearDuration);

            // 第一步：收纳框消失
            yield return StartCoroutine(PlaySummaryBoxDisappearAnimation(rowIndex));

            // 第二步：播放格子扩张动画
            yield return StartCoroutine(ExpandRowFromCenterCoroutine(pairedRow));

            // 第三步：四个格子聚合
            yield return StartCoroutine(GridMoveTogetherAnimation(pairedRow, 4));

            // 第四步：切换图片并更改颜色
            yield return StartCoroutine(CellToggleImage(pairedRow));

            // 第五步：清除旧物品并生成新物品
            ClearOldItems(pairedRow);
            yield return ItemGenerate.Instance.SupplementGenerateItems(LevelManager.Instance.HasPairRows, rowIndex);
            yield return StartCoroutine(pairedRow[3].ItemDropBounceAnimationSelf());
            yield return StartCoroutine(pairedRow[2].ItemDropBounceAnimationSelf());
            yield return StartCoroutine(pairedRow[1].ItemDropBounceAnimationSelf());
            yield return StartCoroutine(pairedRow[0].ItemDropBounceAnimationSelf());
            pairedRow[3].PlayEffect2();
            pairedRow[3].PlayEffect2();
            pairedRow[3].PlayEffect2();
            pairedRow[3].PlayEffect2();
            yield return new WaitForSeconds(0.3f);

            // 第六步：四个格子复合复原动画
            yield return StartCoroutine(GridMoveBackAnimation(pairedRow));

            // 第七步：动画完成后检查胜利条件
            if (!LevelManager.Instance.isLevelCompleted)
            {
                LevelManager.Instance.CheckHasVictory();
            }

            //第八步：格子Q弹效果
            yield return StartCoroutine(GridQ(pairedRow));

            //第九步：格子特效
            GridEffect(pairedRow);

            ItemGenerate.Instance.UnlockAnimation();
        }
        //private IEnumerator SupplementGenerateWithAnimationCoroutine(List<GridCellControl> pairedRow)
        //{
        //    // 开头锁定（覆盖整个补充生成流程）
        //    ItemGenerate.Instance.LockAnimation();
        //    int rowIndex = pairedRow[0].rowIndex;
        //    // 等收纳框显示一会儿再开始消失（保持原有等待逻辑）
        //    yield return new WaitForSeconds(DelysummaryDisappearDuration);

        //    // 第一步：同时启动收纳框消失与格子移动到一起动画
        //    Coroutine summaryDisappearCoroutine = StartCoroutine(PlaySummaryBoxDisappearAnimation(rowIndex));//收纳框消失
        //    Coroutine gridCollectionCoroutine = StartCoroutine(GridMoveTogetherAnimation(pairedRow, 4));//四个格子位置平滑移动到同一位置
        //    yield return summaryDisappearCoroutine;
        //    yield return gridCollectionCoroutine; // 等待两个协程都执行完成（确保动画同步结束）


        //    //第二步：格子缩小动画
        //    Coroutine gridShrinkCoroutine = StartCoroutine(PlayGridShrinkAnimation(pairedRow));//格子缩小
        //    yield return gridShrinkCoroutine;


        //    // 第三步：清除旧物品并生成新物品
        //    ClearOldItems(pairedRow);
        //    ItemGenerate.Instance.SupplementGenerateItems(LevelManager.Instance.HasPairRows, rowIndex);

        //    // 第四步：四个格子位置复原动画
        //    yield return StartCoroutine(GridMoveBackAnimation(pairedRow));

        //    // 第五步：播放格子扩张动画
        //    yield return StartCoroutine(ExpandRowFromCenterCoroutine(pairedRow));

        //    // 动画完成后检查胜利条件
        //    if (!LevelManager.Instance.isLevelCompleted)
        //    {
        //        LevelManager.Instance.CheckHasVictory();
        //    }
        //    ItemGenerate.Instance.UnlockAnimation();

        //    UpdateAllCell();
        //    foreach (GridCellControl cell in pairedRow)
        //    {
        //        cell.PlayEffect();
        //        cell.PlayEffect();
        //        cell.PlayEffect();
        //        cell.PlayEffect();

        //        yield return new WaitForSeconds(0.1f);
        //    }
        //}
        #endregion

        #region 补充生成动画效果方法

        // 第一步：收纳框消失/或缓慢消失
        private IEnumerator PlaySummaryBoxDisappearAnimation(int rowIndex)
        {
            // 查找对应行的收纳框
            RectTransform targetSummaryBox = FindSummaryBoxByRow(rowIndex);
            if (targetSummaryBox == null)
            {
                Debug.LogWarning($"未找到第{rowIndex}行的收纳框，跳过消失动画");
                yield break;
            }

            //float elapsedTime = 0f;
            //// 1. 保存所有初始状态（缩放 + 位置）
            //Vector3 initialScale = targetSummaryBox.localScale;
            //Vector2 initialAnchoredPos = targetSummaryBox.anchoredPosition; // 初始锚点位置

            //// 2. 定义所有目标状态（缩放 + 偏移位置）
            //Vector3 targetScale = Vector3.zero; // 缩放目标：缩为0
            //Vector2 targetAnchoredPos = new Vector2(
            //    initialAnchoredPos.x, // x轴保持不变
            //    initialAnchoredPos.y - summaryOffest.y // y轴向下偏移 summaryOffest.y
            //);

            //// 3. 动画循环：同时更新缩放和位置（同步插值）
            //while (elapsedTime < summaryDisappearDuration)
            //{
            //    elapsedTime += Time.deltaTime;
            //    float progress = Mathf.Clamp01(elapsedTime / summaryDisappearDuration); // 0~1 进度
            //    float smoothProgress = summaryDisappearEaseCurve.Evaluate(progress); // 平滑进度

            //    targetSummaryBox.localScale = Vector3.Lerp(initialScale, targetScale, smoothProgress);
            //    targetSummaryBox.anchoredPosition = Vector2.Lerp(initialAnchoredPos, targetAnchoredPos, smoothProgress);

            //    yield return null; // 等待下一帧，保证动画流畅
            //}

            //// 4. 强制校正最终状态（避免动画误差）
            //targetSummaryBox.localScale = targetScale;
            //targetSummaryBox.anchoredPosition = targetAnchoredPos;

            // 销毁收纳框
            Destroy(targetSummaryBox.gameObject);
        }

        // 第二步：播放格子扩张动画
        private IEnumerator ExpandRowFromCenterCoroutine(List<GridCellControl> pairedRow)
        {
            // 锁定动画状态
            ItemGenerate.Instance.LockAnimation();
            // 过滤无效GridCell
            List<RectTransform> cellRects = pairedRow
                .Select(cell => cell._rectTransform)
                .Where(rect => rect != null)
                .ToList();

            if (cellRects.Count == 0)
            {
                Debug.LogWarning("⚠️ 配对行无有效GridCell，无法执行扩张动画");
                ItemGenerate.Instance.UnBuglockAnimation();
                yield break;
            }

            // 读取全局存储的收束前初始状态
            int rowIndex = pairedRow[0].rowIndex;
            if (!_rowInitialStates.ContainsKey(rowIndex))
            {
                Debug.LogError($"❌ 未找到第{rowIndex}行的初始状态，无法执行扩张动画");
                ItemGenerate.Instance.UnBuglockAnimation();
                yield break;
            }
            List<GridCellInitialState> initialStates = _rowInitialStates[rowIndex];
            if (initialStates.Count != cellRects.Count)
            {
                Debug.LogError($"❌ 第{rowIndex}行初始状态数量与格子数量不匹配");
                ItemGenerate.Instance.UnBuglockAnimation();
                yield break;
            }

            // 记录当前收束状态（作为扩张起点）
            List<Vector2> currentPositions = cellRects.Select(rect => rect.anchoredPosition).ToList();
            List<Vector3> currentScales = cellRects.Select(rect => rect.localScale).ToList();

            // ========== 核心：复用收束动画配置，无需单独配置扩张参数 ==========
            float animDuration = shrinkAnimDuration; // 复用收束动画时长
            AnimationCurve easeCurve = shrinkEaseCurve; // 复用收束动画缓动曲线
            bool enableSpacing = enableSpacingShrink; // 复用是否启用间距动画

            // 执行扩张动画（从收束状态→初始状态）
            float elapsedTime = 0f;
            while (elapsedTime < animDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / animDuration);
                float smoothProgress = easeCurve.Evaluate(progress);

                // 逐格子更新位置和缩放（强制恢复初始状态）
                for (int i = 0; i < cellRects.Count; i++)
                {
                    RectTransform cellRect = cellRects[i];
                    GridCellInitialState targetState = initialStates[i];
                    Vector2 startPos = currentPositions[i];
                    Vector3 startScale = currentScales[i];

                    // 位置：恢复到收束前的初始位置
                    if (enableSpacing)
                    {
                        cellRect.anchoredPosition = Vector2.Lerp(startPos, targetState.anchoredPosition, smoothProgress);
                    }

                    // 缩放：强制恢复到收束前的初始大小（解决大小不一致问题）
                    cellRect.localScale = Vector3.Lerp(startScale, targetState.localScale, smoothProgress);
                }
                yield return null;
            }

            // ========== 强制校正最终状态（确保完全恢复初始状态） ==========
            for (int i = 0; i < cellRects.Count; i++)
            {
                RectTransform cellRect = cellRects[i];
                GridCellInitialState targetState = initialStates[i];

                if (enableSpacing)
                {
                    cellRect.anchoredPosition = targetState.anchoredPosition;
                }
                // 最终强制恢复初始缩放
                cellRect.localScale = targetState.localScale;
            }

            // 清理该行列的初始状态缓存
            _rowInitialStates.Remove(rowIndex);

            // 解锁动画状态
            ItemGenerate.Instance.UnlockAnimation();
        }

        // 第三步：四个格子聚合
        private IEnumerator GridMoveTogetherAnimation(List<GridCellControl> cells, int targetCellNum)
        {
            ItemGenerate.Instance.LockAnimation();

            // 1. 安全校验
            if (cells == null || cells.Count == 0)
            {
                Debug.LogWarning("⚠️ 格子列表为空，无法执行移动动画");
                ItemGenerate.Instance.UnlockAnimation();
                yield break;
            }

            // 优先使用配置的目标索引，兼容原有传参逻辑（传参为0时使用配置值）
            int useTargetIndex = targetCellNum > 0 ? targetCellNum : gridMoveTargetIndex;
            // 转换用户输入的1-4数字为列表索引（0-3）
            int targetIndex = useTargetIndex - 1;
            if (targetIndex < 0 || targetIndex >= cells.Count)
            {
                Debug.LogError($"❌ 目标索引无效：输入{useTargetIndex}，格子总数{cells.Count}，自动使用最后一列");
                targetIndex = cells.Count - 1; // 兜底：使用最后一列
            }

            // 2. 按列索引排序（确保1/2/3/4列顺序一致）
            List<GridCellControl> sortedCells = cells.OrderBy(cell => cell.colIndex).ToList();

            // 3. 获取目标格子的目标位置（锚点位置，UI移动核心）
            GridCellControl targetCell = sortedCells[targetIndex];
            RectTransform targetRect = targetCell._rectTransform;
            if (targetRect == null)
            {
                Debug.LogError($"❌ 目标格子{useTargetIndex}的RectTransform为空");
                ItemGenerate.Instance.UnlockAnimation();
                yield break;
            }
            Vector2 targetAnchoredPos = targetRect.anchoredPosition;

            // 4. 存储所有格子的初始位置（用于后续复原）
            _gridMoveInitialStates.Clear();
            foreach (var cell in sortedCells)
            {
                if (cell._rectTransform == null) continue;
                string cellKey = $"{cell.rowIndex}_{cell.colIndex}"; // 行+列作为唯一标识
                _gridMoveInitialStates[cellKey] = new GridCellInitialState(
                    cell._rectTransform.anchoredPosition,
                    cell._rectTransform.localScale
                );
            }

            // 5. 平滑移动动画逻辑（使用可配置参数）
            float animDuration = enableGridMoveSmooth ? gridMoveTogetherDuration : 0.01f; // 关闭平滑则极速完成
            AnimationCurve easeCurve = gridMoveTogetherCurve;
            float elapsedTime = 0f;

            while (elapsedTime < animDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / animDuration);
                float smoothProgress = easeCurve.Evaluate(progress);

                // 逐格子插值移动到目标位置
                foreach (var cell in sortedCells)
                {
                    if (cell._rectTransform == null) continue;

                    Vector2 currentPos = cell._rectTransform.anchoredPosition;
                    Vector2 lerpPos = Vector2.Lerp(currentPos, targetAnchoredPos, smoothProgress);

                    // 忽略Z轴（UI专用，避免深度偏移）
                    if (ignoreZAxisInMove)
                    {
                        cell._rectTransform.anchoredPosition = new Vector2(lerpPos.x, lerpPos.y);
                    }
                    else
                    {
                        cell._rectTransform.anchoredPosition = lerpPos;
                    }

                    // 保持缩放不变（可选配置）
                    if (keepScaleDuringMove)
                    {
                        string cellKey = $"{cell.rowIndex}_{cell.colIndex}";
                        if (_gridMoveInitialStates.ContainsKey(cellKey))
                        {
                            cell._rectTransform.localScale = _gridMoveInitialStates[cellKey].localScale;
                        }
                    }
                }

                yield return null; // 等待下一帧，保证动画流畅
            }

            // 6. 强制校正最终位置（避免插值误差）
            foreach (var cell in sortedCells)
            {
                if (cell._rectTransform == null) continue;

                if (ignoreZAxisInMove)
                {
                    cell._rectTransform.anchoredPosition = new Vector2(targetAnchoredPos.x, targetAnchoredPos.y);
                }
                else
                {
                    cell._rectTransform.anchoredPosition = targetAnchoredPos;
                }

                // 强制恢复初始缩放（若开启保持缩放）
                if (keepScaleDuringMove)
                {
                    string cellKey = $"{cell.rowIndex}_{cell.colIndex}";
                    if (_gridMoveInitialStates.ContainsKey(cellKey))
                    {
                        cell._rectTransform.localScale = _gridMoveInitialStates[cellKey].localScale;
                    }
                }
            }

            ItemGenerate.Instance.UnlockAnimation();
        }

        // 第四步：切换图片并更改颜色
        private IEnumerator CellToggleImage(List<GridCellControl> cells)
        {
            foreach (var cell in cells)
            {
                if (cell == null || cell.targetImage == null)
                {
                    Debug.LogWarning($"无效格子[{cell?.rowIndex},{cell?.colIndex}]：无法执行ToggleImage", cell?.gameObject);
                    continue;
                }
                cell.ToggleImage();
               
                cell.cellBackground.color = Color.white;
            }
            yield return null;
        }

        // 第五步：清除旧物品
        private void ClearOldItems(List<GridCellControl> pairedRow)
        {
            foreach (var cell in pairedRow)
            {
                // 清除格子上的物品
                if (cell._currentItem != null)
                {
                    ItemControl item = cell.GetComponentInChildren<ItemControl>();
                    if (item != null)
                    {
                        DestroyImmediate(item.gameObject);
                    }
                    cell._currentItem = null;
                    cell.isPairing = false;
                }
            }
        }

        // 第六步：四个格子复合复原动画
        private IEnumerator GridMoveBackAnimation(List<GridCellControl> cells)
        {
            ItemGenerate.Instance.LockAnimation();

            // 1. 安全校验
            if (cells == null || cells.Count == 0 || _gridMoveInitialStates.Count == 0)
            {
                Debug.LogWarning("⚠️ 无初始位置数据，无法执行复原动画");
                ItemGenerate.Instance.UnlockAnimation();
                yield break;
            }

            // 2. 按列索引排序（和移动时顺序一致）
            List<GridCellControl> sortedCells = cells.OrderBy(cell => cell.colIndex).ToList();

            // 3. 平滑复原动画逻辑（使用可配置参数）
            float animDuration = enableGridMoveSmooth ? gridMoveBackDuration : 0.01f;
            AnimationCurve easeCurve = gridMoveBackCurve;
            float elapsedTime = 0f;

            while (elapsedTime < animDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / animDuration);
                float smoothProgress = easeCurve.Evaluate(progress);

                // 逐格子插值回到初始位置
                foreach (var cell in sortedCells)
                {
                    if (cell._rectTransform == null) continue;

                    string cellKey = $"{cell.rowIndex}_{cell.colIndex}";
                    if (!_gridMoveInitialStates.ContainsKey(cellKey)) continue;

                    GridCellInitialState initialState = _gridMoveInitialStates[cellKey];
                    Vector2 currentPos = cell._rectTransform.anchoredPosition;
                    Vector2 lerpPos = Vector2.Lerp(currentPos, initialState.anchoredPosition, smoothProgress);

                    // 忽略Z轴（UI专用）
                    if (ignoreZAxisInMove)
                    {
                        cell._rectTransform.anchoredPosition = new Vector2(lerpPos.x, lerpPos.y);
                    }
                    else
                    {
                        cell._rectTransform.anchoredPosition = lerpPos;
                    }

                    // 保持缩放不变（可选配置）
                    if (keepScaleDuringMove)
                    {
                        cell._rectTransform.localScale = initialState.localScale;
                    }
                }

                yield return null;
            }

            // 4. 强制校正最终位置（确保完全复原）
            foreach (var cell in sortedCells)
            {
                if (cell._rectTransform == null) continue;

                string cellKey = $"{cell.rowIndex}_{cell.colIndex}";
                if (_gridMoveInitialStates.ContainsKey(cellKey))
                {
                    GridCellInitialState initialState = _gridMoveInitialStates[cellKey];

                    if (ignoreZAxisInMove)
                    {
                        cell._rectTransform.anchoredPosition = new Vector2(
                            initialState.anchoredPosition.x,
                            initialState.anchoredPosition.y
                        );
                    }
                    else
                    {
                        cell._rectTransform.anchoredPosition = initialState.anchoredPosition;
                    }

                    cell._rectTransform.localScale = initialState.localScale;
                }
            }

            // 5. 清理初始位置缓存（避免复用错误）
            _gridMoveInitialStates.Clear();

            ItemGenerate.Instance.UnlockAnimation();
        }

        //第八步：格子Q弹效果
        private IEnumerator GridQ(List<GridCellControl> targetRow)
        {
            // 交换完成后，顺序执行动画
            int animationCompletedCount = 0; // 记录已完成跳动动画的格子数量
            int totalCellCount = targetRow.Count; // 目标行总格子数

            // 第一步：启动所有格子的跳动动画，并用回调统计完成状态
            foreach (var cell in targetRow)
            {
                if (cell == null) continue;

                // 启动跳动动画，并传入“动画完成回调”
                StartCoroutine(cell.ExchangeDoneAnimation2(() =>
                {
                    animationCompletedCount++; // 某个格子动画完成，计数器+1
                    //Debug.Log($"格子[{cell.rowIndex},{cell.colIndex}] 跳动动画完成，已完成{animationCompletedCount}/{totalCellCount}");
                }));
            }

            // 等待所有格子的跳动动画全部完成（关键：直到计数器等于总格子数）
            while (animationCompletedCount < totalCellCount)
            {
                yield return null; // 每帧检查一次，不阻塞主线程
            }
        }

        //第九步：格子特效
        private void GridEffect(List<GridCellControl> pairedRow)
        {
            UpdateAllCell();
            foreach (GridCellControl cell in pairedRow)
            {
                if (cell.colIndex == 4) continue;
                cell.PlayEffect();
                cell.PlayEffect();
                cell.PlayEffect();
                cell.PlayEffect();
            }
        }

        // 根据行索引查找对应的收纳框
        private RectTransform FindSummaryBoxByRow(int rowIndex)
        {
            if (summaryBoxParent == null) return null;

            foreach (Transform child in summaryBoxParent)
            {
                SummaryBoxControl summaryControl = child.GetComponent<SummaryBoxControl>();
                if (summaryControl != null && summaryControl.targetRowIndex == rowIndex)
                {
                    return child.GetComponent<RectTransform>();
                }
            }
            return null;
        }

        //格子先缩小并回复原颜色
        private IEnumerator PlayGridShrinkAnimation(List<GridCellControl> pairedRow)
        {
            yield return StartCoroutine(PlayGridScaleAnimation(pairedRow, Vector3.one, Vector3.one * shrinkScale, shrinkBeforeGenerateDuration));
            // 给整行所有格子统一设置颜色
            foreach (var cell in pairedRow)
            {
                if (cell.cellBackground != null)
                {
                    cell.cellBackground.color = Color.white; // 给格子背景上色
                }
                else
                {
                    Debug.LogWarning($"⚠️ 格子{cell.name}未绑定cellBackground组件，请在Inspector中赋值");
                }
            }
        }
        //格子缩小
        private IEnumerator PlayGridScaleAnimation(List<GridCellControl> cells, Vector3 fromScale, Vector3 toScale, float duration)
        {
            List<RectTransform> cellRects = cells
                .Select(cell => cell._rectTransform)
                .Where(rect => rect != null)
                .ToList();

            if (cellRects.Count == 0) yield break;

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                float smoothProgress = scaleEaseCurve.Evaluate(progress);

                foreach (var rect in cellRects)
                {
                    rect.localScale = Vector3.Lerp(fromScale, toScale, smoothProgress);
                }
                yield return null;
            }

            // 强制校正最终状态
            foreach (var rect in cellRects)
            {
                rect.localScale = toScale;
            }
            foreach (var cell in cells)
            {
                cell.ToggleImage();
            }

        }

        #endregion

        #region 房屋物品显示
        public void ShowHousePartModel(GridCellControl cellControl)
        {
            int ModelIndex = CategoryToInt(cellControl._currentItem.category);
            LevelManager.Instance._houseControl.SetPartModelActive(ModelIndex, true);
        }
        public void ShowHousePartModel(GridCellControl cellControl, RectTransform summaryRect)
        {
            //Debug.Log($"【归纳框UI坐标】anchoredPosition：{summaryRect.anchoredPosition} | position：{summaryRect.position}");

            // 1. 计算3D模型索引
            int modelIndex = CategoryToInt(cellControl._currentItem.category);

            // 2. 获取收纳框的世界位置（调用修改后的GetSummaryBoxWorldPosition）
            Vector3 uiWorldPos = GetSummaryBoxWorldPosition(summaryRect);

            //Debug.Log($"【转换后世界坐标】uiWorldPos：{uiWorldPos}");

            if (uiWorldPos == Vector3.negativeInfinity)
            {
                Debug.LogWarning("收纳框世界位置获取失败，直接激活3D模型");
                LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
                return;
            }

            // 3. 获取3D目标模型的位置（含特效偏移）
            HouseControl houseControl = LevelManager.Instance._houseControl;
            if (!houseControl.HousePartModelDictionary.ContainsKey(modelIndex))
            {
                Debug.LogWarning($"索引{modelIndex}的3D模型不存在，直接激活");
                houseControl.SetPartModelActive(modelIndex, true);
                return;
            }
            GameObject targetModel = houseControl.HousePartModelDictionary[modelIndex];
            Vector3 targetWorldPos = targetModel.transform.position + HouseGeneration.Instance.EffectrOffect; // 与原有特效位置偏移一致

            // 4. 启动特效飞行协程
            StartCoroutine(FlyEffectToTargetCoroutine(uiWorldPos, targetWorldPos, modelIndex));
        }
        private IEnumerator FlyEffectToTargetCoroutine(Vector3 startPos, Vector3 targetPos, int modelIndex)
        {
            //Debug.Log($"【特效起始位置】startPos：{startPos} | 目标位置：{targetPos}");
            GameObject flyEffect = EffectManager.Instance.CreateEffect(
                effectKey: "FlyTo3D",
                position: startPos,
                rotation: Quaternion.identity,
                parent: null
            );
            if (flyEffect == null)
            {
                Debug.LogWarning("飞行特效创建失败，直接激活3D模型");
                LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
                yield break;
            }

            // 缓存参数
            float moveDuration = flyEffectDuration;
            AnimationCurve moveCurve = flyEffectCurve;
            float arcHeight = flyEffectArcHeight;
            float spiralTurns = flyEffectSpiralTurns;
            AnimationCurve spiralRadiusCurve = flyEffectSpiralRadiusCurve; // 仅用该曲线控制半径

            float elapsedTime = 0f;
            Transform effectTrans = flyEffect.transform;

            while (elapsedTime < moveDuration)
            {
                if (effectTrans == null || !flyEffect.activeInHierarchy)
                {
                    Debug.LogWarning("飞行特效已被销毁/禁用，终止协程");
                    yield break;
                }

                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / moveDuration);
                float smoothProgress = moveCurve.Evaluate(progress);

                // 抛物线基础位置（保留原有逻辑）
                Vector3 midPos = (startPos + targetPos) / 2 + Vector3.up * arcHeight;
                Vector3 lerp1 = Vector3.Lerp(startPos, midPos, smoothProgress);
                Vector3 lerp2 = Vector3.Lerp(midPos, targetPos, smoothProgress);
                Vector3 basePos = Vector3.Lerp(lerp1, lerp2, smoothProgress);

                // 螺旋偏移（核心：仅用Curve计算绝对半径）
                Vector3 spiralOffset = Vector3.zero;
                Vector3 dir = targetPos - startPos;
                float dirMagnitude = dir.magnitude;
                if (dirMagnitude > 0.01f)
                {
                    Vector3 forward = dir.normalized;
                    Vector3 upRef = Vector3.up;
                    if (Mathf.Abs(Vector3.Dot(forward, upRef)) > 0.99f)
                    {
                        upRef = Vector3.right;
                    }
                    Vector3 right = Vector3.Cross(forward, upRef).normalized;
                    Vector3 up = Vector3.Cross(right, forward).normalized;

                    // 螺旋角度
                    float spiralAngle = smoothProgress * spiralTurns * 2 * Mathf.PI;

                    // ========== 核心修改：仅用Curve获取绝对半径 ==========
                    // Y轴直接对应世界坐标的半径值（比如曲线Y=1 → 半径1单位）
                    float spiralRadius = spiralRadiusCurve.Evaluate(smoothProgress);
                    // 安全兜底：半径不能为负
                    spiralRadius = Mathf.Max(0, spiralRadius);

                    // 计算偏移
                    spiralOffset = Mathf.Cos(spiralAngle) * right + Mathf.Sin(spiralAngle) * up;
                    spiralOffset *= spiralRadius;
                }

                // 最终位置（起始时radius=0，无偏移）
                effectTrans.position = basePos + spiralOffset;

                yield return null;
            }

            // 动画结束校正
            if (effectTrans != null)
            {
                effectTrans.position = targetPos;
                Destroy(flyEffect);
            }

            LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
        }


        #region 备用普通抛物线 与 原螺旋抛物线
        //private IEnumerator FlyEffectToTargetCoroutine(Vector3 startPos, Vector3 targetPos, int modelIndex)
        //{
        //    Debug.Log($"【特效起始位置】startPos：{startPos} | 目标位置：{targetPos}");
        //    // 1. 创建特效并做安全校验
        //    GameObject flyEffect = EffectManager.Instance.CreateEffect(
        //        effectKey: "FlyTo3D",
        //        position: startPos,
        //        rotation: Quaternion.identity,
        //        parent: null
        //    );
        //    if (flyEffect == null)
        //    {
        //        Debug.LogWarning("飞行特效创建失败，直接激活3D模型");
        //        LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
        //        yield break;
        //    }

        //    // 2. 缓存动画参数，避免重复访问
        //    float moveDuration = flyEffectDuration;
        //    AnimationCurve moveCurve = flyEffectCurve;
        //    float arcHeight = flyEffectArcHeight;
        //    float spiralTurns = flyEffectSpiralTurns;
        //    float spiralStartRadius = flyEffectSpiralStartRadius;
        //    float spiralEndRadius = flyEffectSpiralEndRadius;

        //    float elapsedTime = 0f;
        //    Transform effectTrans = flyEffect.transform; // 缓存Transform，减少GetComponent开销

        //    while (elapsedTime < moveDuration)
        //    {
        //        // 3. 检测特效是否被销毁/禁用（更健壮的判断）
        //        if (effectTrans == null || !flyEffect.activeInHierarchy)
        //        {
        //            Debug.LogWarning("飞行特效已被销毁/禁用，终止飞行动画协程");
        //            yield break;
        //        }

        //        // 4. 计算进度（带缓动曲线）
        //        elapsedTime += Time.deltaTime;
        //        float progress = Mathf.Clamp01(elapsedTime / moveDuration);
        //        float smoothProgress = moveCurve.Evaluate(progress);

        //        // 5. 计算原抛物线的基础位置（保持和原代码完全一致的抛物线轨迹）
        //        Vector3 midPos = (startPos + targetPos) / 2 + Vector3.up * arcHeight;
        //        Vector3 lerp1 = Vector3.Lerp(startPos, midPos, smoothProgress);
        //        Vector3 lerp2 = Vector3.Lerp(midPos, targetPos, smoothProgress);
        //        Vector3 basePos = Vector3.Lerp(lerp1, lerp2, smoothProgress);

        //        // 6. 计算螺旋偏移（核心修改：围绕飞行方向的垂直平面做螺旋）
        //        Vector3 spiralOffset = Vector3.zero;
        //        Vector3 dir = targetPos - startPos;
        //        float dirMagnitude = dir.magnitude;
        //        if (dirMagnitude > 0.01f) // 避免起点和目标点重合导致的计算异常
        //        {
        //            // 6.1 计算飞行方向（归一化）
        //            Vector3 forward = dir.normalized;

        //            // 6.2 构建垂直于飞行方向的正交坐标系（解决方向重合问题）
        //            Vector3 upRef = Vector3.up;
        //            if (Mathf.Abs(Vector3.Dot(forward, upRef)) > 0.99f)
        //            {
        //                upRef = Vector3.right; // 若飞行方向接近垂直，改用右方向作为参考
        //            }
        //            Vector3 right = Vector3.Cross(forward, upRef).normalized;
        //            Vector3 up = Vector3.Cross(right, forward).normalized;

        //            // 6.3 计算螺旋角度（总圈数×2π×进度）
        //            float spiralAngle = smoothProgress * spiralTurns * 2 * Mathf.PI;

        //            // 6.4 计算螺旋半径（从初始半径插值到结束半径）
        //            float spiralRadius = Mathf.Lerp(spiralStartRadius, spiralEndRadius, smoothProgress);
        //            // 可选：让半径随飞行距离缩放（适配不同长度的抛物线）
        //            spiralRadius *= Mathf.Lerp(1f, 0.1f, progress); // 越靠近目标，半径缩小得更快（可调整）

        //            // 6.5 计算平面内的螺旋偏移向量
        //            spiralOffset = Mathf.Cos(spiralAngle) * right + Mathf.Sin(spiralAngle) * up;
        //            spiralOffset *= spiralRadius;
        //        }

        //        // 7. 最终位置 = 抛物线基础位置 + 螺旋偏移
        //        effectTrans.position = basePos + spiralOffset;

        //        yield return null;
        //    }

        //    // 8. 动画结束后校正位置并销毁特效
        //    if (effectTrans != null)
        //    {
        //        effectTrans.position = targetPos;
        //        Destroy(flyEffect);
        //    }

        //    // 9. 激活3D模型
        //    LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
        //}
        //private IEnumerator FlyEffectToTargetCoroutine(Vector3 startPos, Vector3 targetPos, int modelIndex)
        //{
        //    GameObject flyEffect = EffectManager.Instance.CreateEffect(
        //        effectKey: "FlyTo3D",
        //        position: startPos,
        //        rotation: Quaternion.identity,
        //        parent: null
        //    );
        //    if (flyEffect == null)
        //    {
        //        Debug.LogWarning("飞行特效创建失败，直接激活3D模型");
        //        LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
        //        yield break;
        //    }

        //    float moveDuration = flyEffectDuration;
        //    AnimationCurve moveCurve = flyEffectCurve;
        //    float elapsedTime = 0f;

        //    while (elapsedTime < moveDuration)
        //    {
        //        // ========== 核心：检测特效对象是否已被销毁 ==========
        //        if (flyEffect == null)
        //        {
        //            Debug.LogWarning("飞行特效已被销毁，终止飞行动画协程");
        //            yield break; // 终止协程，避免后续错误
        //        }

        //        elapsedTime += Time.deltaTime;
        //        float progress = Mathf.Clamp01(elapsedTime / moveDuration);
        //        float smoothProgress = moveCurve.Evaluate(progress);

        //        Vector3 midPos = (startPos + targetPos) / 2 + Vector3.up * flyEffectArcHeight;
        //        flyEffect.transform.position = Vector3.Lerp(
        //            Vector3.Lerp(startPos, midPos, smoothProgress),
        //            Vector3.Lerp(midPos, targetPos, smoothProgress),
        //            smoothProgress
        //        );

        //        yield return null;
        //    }

        //    // ========== 末尾也要检测对象是否存活 ==========
        //    if (flyEffect != null)
        //    {
        //        flyEffect.transform.position = targetPos;
        //        Destroy(flyEffect);
        //    }

        //    LevelManager.Instance._houseControl.SetPartModelActive(modelIndex, true);
        //}
        #endregion

        // 转换UI位置到世界坐标
        private Vector3 GetSummaryBoxWorldPosition(RectTransform summaryRect)
        {
            if (summaryRect == null) return Vector3.negativeInfinity;

            Canvas canvas = summaryRect.GetComponentInParent<Canvas>();
            if (canvas == null) return summaryRect.position;

            // （原方法内的renderMode判断逻辑保持不变）
            switch (canvas.renderMode)
            {
                case RenderMode.ScreenSpaceCamera:
                    Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, summaryRect.position);
                    return canvas.worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, canvas.planeDistance));
                case RenderMode.ScreenSpaceOverlay:
                    Vector3 overlayPos = summaryRect.position;
                    overlayPos.z = 1f;
                    return overlayPos;
                case RenderMode.WorldSpace:
                    return summaryRect.position;
                default:
                    return summaryRect.position;
            }
        }

        //获取物品类型转换索引
        public int CategoryToInt(ItemCategory category)
        {
            var _categoryToModelMapping = LevelManager.Instance.currentLevelData._categoryToModelMapping;
            // 处理数组为空/无配置的情况
            if (_categoryToModelMapping == null || _categoryToModelMapping.Count == 0)
            {
                Debug.LogWarning($"类别-模型映射数组未配置（_categoryToModelMapping 为空或长度为0）");
                return 1;
            }

            // 遍历映射数组匹配类别
            foreach (var mapping in _categoryToModelMapping)
            {
                if (mapping != null && mapping.targetCategory == category)
                {
                    return mapping.modelIndex;
                }
            }

            // 未找到匹配类别的警告与默认返回
            Debug.LogWarning($"类别 {category} 未在映射数组中配置，返回默认索引1");
            return 1;
        }

        #endregion

        #region 配对成功后视觉辅助
        // 设置归纳框文字的辅助方法
        private void SetSummaryBoxText(GameObject summaryBox, ItemCategory itemCategory)
        {
            if (summaryBox == null)
            {
                Debug.LogWarning("⚠️ 归纳框为空，无法设置文字！");
                return;
            }

            // 获取子节点中的 TextMeshPro 组件（支持 TextMeshPro - Text 和 TextMeshPro - Text UI）
            TextMeshProUGUI tmproText = summaryBox.GetComponentInChildren<TextMeshProUGUI>(true); // true = 包含禁用的子节点
            TextMeshPro tmproWorldText = summaryBox.GetComponentInChildren<TextMeshPro>(true);

            // 优先使用 UI 版本的 TextMeshPro，没有则尝试世界空间版本
            if (tmproText != null)
            {
                // 将枚举转换为文字（可自定义格式，如大写、添加前缀等）
                tmproText.text = itemCategory.ToString().ToUpper();
                // 可选：设置文字颜色、字体大小等
                // tmproText.color = Color.white;
                //tmproText.fontSize = 24;
            }
            else if (tmproWorldText != null)
            {
                tmproWorldText.text = itemCategory.ToString();
            }
            else
            {
                Debug.LogWarning($"⚠️ 归纳框{summaryBox.name}的子节点中未找到 TextMeshPro 组件！");
            }
        }
        // 按预设顺序给整行格子统一上色
        private void ApplyPresetColorToRow(List<GridCellControl> row)
        {
            // 安全校验1：颜色数组未配置 → 跳过
            if (SummaryColor == null || SummaryColor.Length == 0)
            {
                Debug.LogWarning("⚠️ 请先在Inspector中给SummaryColor数组配置颜色");
                return;
            }
            // 安全校验2：行数据无效 → 跳过
            if (row == null || row.Count == 0)
            {
                Debug.LogWarning("⚠️ 待上色的行数据无效");
                return;
            }

            // 按预设顺序取颜色（循环使用数组）
            int targetColorIndex = _currentColorIndex % SummaryColor.Length;
            Color targetColor = SummaryColor[targetColorIndex];
            //Debug.Log($"✅ 给当前行应用颜色（索引{targetColorIndex}：{targetColor}）");

            // 给整行所有格子统一设置颜色
            foreach (var cell in row)
            {
                if (cell.cellBackground != null)
                {
                    cell.ToggleImage();
                    cell.cellBackground.color = targetColor; // 给格子背景上色
                    ItemControl item = cell.GetComponentInChildren<ItemControl>();
                    item.Stopanimation();//停止呼吸动画
                    item.transform.localScale = ItemTargetScale;//物品缩放
                }
                else
                {
                    Debug.LogWarning($"⚠️ 格子{cell.name}未绑定cellBackground组件，请在Inspector中赋值");
                }
            }

            // 颜色计数器递增（下一行用下一个颜色）
            _currentColorIndex++;
        }
        //清理上一关的收纳框
        public void ClearAllSummaryBox()
        {
            if (summaryBoxParent == null) return;
            for (int i = summaryBoxParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(summaryBoxParent.GetChild(i).gameObject);
            }
        }
        #endregion

        #region 逻辑辅助方法

        //改变行的isPairing状态
        private void SetPairedRowState(List<GridCellControl> pairedCells, bool state)
        {
            foreach (var cell in pairedCells)
            {
                cell.isPairing = state;
            }
        }
        //：筛选所有「完整、无空物品、未配对」的行
        private List<List<GridCellControl>> GetAllUnpairedRows()
        {
            Dictionary<int, List<GridCellControl>> rowToCells = new Dictionary<int, List<GridCellControl>>();
            foreach (var cell in allGridCells)
            {
                int row = cell.rowIndex;
                if (!rowToCells.ContainsKey(row))
                {
                    rowToCells[row] = new List<GridCellControl>();
                }
                rowToCells[row].Add(cell);
            }

            List<List<GridCellControl>> unpairedRows = new List<List<GridCellControl>>();
            foreach (var rowKvp in rowToCells)
            {
                List<GridCellControl> rowCells = rowKvp.Value;
                // 筛选条件：完整行（格子数=列数）+ 无空物品 + 未配对（整行不是全部已配对）
                bool isComplete = rowCells.Count == GridCellGenerate.Instance.colCount;
                bool noEmpty = !rowCells.Any(cell => cell._currentItem == null);
                bool isUnpaired = !rowCells.All(cell => cell.isPairing);

                if (isComplete && noEmpty && isUnpaired)
                {
                    unpairedRows.Add(rowCells);
                }
            }
            return unpairedRows;
        }

        // 获取当前的所有格子
        public void UpdateAllCell()
        {
            allGridCells.Clear();
            // 遍历父节点下所有子物体，筛选带 GridCellControl 的格子
            foreach (Transform child in GridCellGenerate.Instance.gridParent)
            {
                GridCellControl cell = child.GetComponent<GridCellControl>();
                if (cell != null)
                {
                    allGridCells.Add(cell);
                }
            }
        }
        #endregion
    }
}
