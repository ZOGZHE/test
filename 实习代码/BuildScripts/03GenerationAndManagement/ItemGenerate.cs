using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using static UnityEditor.Progress;

namespace ConnectMaster
{
    public class ItemGenerate : MonoBehaviour
    {
        public static ItemGenerate Instance;
        //预制体与生成父节点
        public ItemControl ItemPrefab;
        public RectTransform TopHierarchyPoint;//最高层级防遮挡
        public RectTransform gridParent;
        [HideInInspector] private List<GridCellControl> allGridCells;//所有格子
        [HideInInspector] private static readonly System.Random _random = new System.Random();


        #region 核心：4个物品列表（提前分配，对应不同生成阶段）
        // 初次生成列表（最多6行，每行4个物品）
        private List<Item> _initialItemList = new List<Item>();
        // 补充生成列表（各1行=4个物品，对应3次补充）
        private List<Item> _supplement1ItemList = new List<Item>(); // 第一次补充（row≥7时有效）
        private List<Item> _supplement2ItemList = new List<Item>(); // 第二次补充（row≥8时有效）
        private List<Item> _supplement3ItemList = new List<Item>(); // 第三次补充（row≥9时有效）

        // 全局状态（保存关卡配置+总物品池）
        private LevelData _currentLevelData;
        internal List<Item> _totalItemPool = new List<Item>(); // 所有物品（去重后，确保全用到）
        private const int MAX_RETRY = 50; // 最大重试次数（避免死循环）
        private const int COL_COUNT = 4; // 固定4列
        #endregion

        #region 生命周期
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                allGridCells = new List<GridCellControl>();//初始化allGridCells放置空引用
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            ItemPairing.Instance.ParingRow += NoviceState2;
        }
        private void OnDestroy()
        {
            ClearAllItems();
            allGridCells?.Clear();
            ResetAllItemLists();
            ItemPairing.Instance.ParingRow -= NoviceState2;
            
        }
        #endregion

        #region 动画状态
        [HideInInspector] public int _animationCount = 0;
        // 只读属性，禁止外部直接修改
        public bool IsAnimating => _animationCount > 0;

        // 锁定方法（引用计数+1）
        public void LockAnimation()
        {
            _animationCount++;
            //Debug.Log($"动画锁定，当前计数：{_animationCount}");
        }

        // 解锁方法（引用计数-1，防止负数）
        public void UnlockAnimation()
        {
            _animationCount = Mathf.Max(0, _animationCount - 1); // 避免负数
            //Debug.Log($"动画解锁，当前计数：{_animationCount}");
        }
        // bug解锁方法（引用计数-1，防止负数）
        public void UnBuglockAnimation()
        {
            _animationCount = Mathf.Max(0, _animationCount - 1); // 避免负数
            //Debug.Log($"bug动画解锁，当前计数：{_animationCount}");
        }

        // 强制重置（用于关卡切换/异常恢复）
        public void ResetAnimationState()
        {
            _animationCount = 0;
            //Debug.Log("动画状态强制重置");
        }
        #endregion

        #region 核心0：初始化物品列表（LevelManager传入数据后调用）
        // 初始化所有生成阶段的物品列表（LevelManager传入数据后必须调用）
        public bool InitAllItemLists(LevelData levelData, List<Item> requiredItems)
        {
            // 重置之前的状态
            ResetAllItemLists();
            _currentLevelData = levelData;
            _totalItemPool = new List<Item>(requiredItems.Distinct()); // 确保无重复

            // 基础校验：物品总数必须=row×4（每行4列，全用到无重复）
            int totalNeedItemCount = levelData.rows * COL_COUNT;
            if (_totalItemPool.Count != totalNeedItemCount)
            {
                Debug.LogError($"❌ 物品总数不匹配：需{totalNeedItemCount}个（{levelData.rows}行×4列），实际{_totalItemPool.Count}个");
                return false;
            }

            // 重试机制：直到所有阶段都满足规则
            for (int retry = 0; retry < MAX_RETRY; retry++)
            {
                // 1. 打乱总物品池（保证随机性）
                ShuffleList(_totalItemPool);

                // 2. 拆分到4个阶段列表
                if (!SplitIntoStageLists(levelData.rows))
                {
                    Debug.LogWarning($"⚠️ 第{retry + 1}次拆分失败，重新尝试");
                    continue;
                }

                // 3. 校验所有阶段列表的规则（每行至少2类+生成后有4个同类别）
                if (ValidateAllStageLists(levelData.rows))
                {
                    Debug.Log($"✅ 物品列表初始化成功（重试{retry + 1}次）");
                    return true;
                }

                Debug.LogWarning($"⚠️ 第{retry + 1}次规则校验失败，重新尝试");
            }

            Debug.LogError($"❌ 初始化失败：重试{MAX_RETRY}次仍未满足所有规则");
            return false;
        }

        #endregion

        #region 核心1：拆分打乱物品分成4个阶段列表

        // Fisher-Yates洗牌 打乱物品排序
        private void ShuffleList<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        // 按关卡row数拆分总物品池到4个阶段列表
        private bool SplitIntoStageLists(int totalItemRow)
        {
            int index = 0;
            int initialRow = Mathf.Min(totalItemRow, 6); // 初次生成行数

            // 1. 初次生成列表（前initialRow行=initialRow×4个物品）
            _initialItemList = _totalItemPool.Skip(index).Take(initialRow * COL_COUNT).ToList();
            index += initialRow * COL_COUNT;

            // 2. 补充列表（按需拆分，各1行=4个物品）
            if (totalItemRow >= 7)
            {
                _supplement1ItemList = _totalItemPool.Skip(index).Take(COL_COUNT).ToList();
                index += COL_COUNT;
            }
            if (totalItemRow >= 8)
            {
                _supplement2ItemList = _totalItemPool.Skip(index).Take(COL_COUNT).ToList();
                index += COL_COUNT;
            }
            if (totalItemRow >= 9)
            {
                _supplement3ItemList = _totalItemPool.Skip(index).Take(COL_COUNT).ToList();
                index += COL_COUNT;
            }

            // 校验：拆分后总数一致
            int totalSplitCount = _initialItemList.Count + _supplement1ItemList.Count + _supplement2ItemList.Count + _supplement3ItemList.Count;
            return totalSplitCount == _totalItemPool.Count;
        }
        #endregion

        #region 核心2：校验所有阶段列表的规则
        //校验：1.每行至少2类 2.每个阶段生成后满足「有效类别数量」要求
        private bool ValidateAllStageLists(int totalItemRow)
        {
            // 基础消除数量（从LevelData读取，默认4个/组）
            int singleGroupCount = _currentLevelData.categoryEliminateCount;

            // 临时集合：存储各阶段生成后的完整物品集合
            List<Item> tempInitialAll = new List<Item>(_initialItemList);
            List<Item> tempSupplement1All = new List<Item>(tempInitialAll);
            tempSupplement1All.AddRange(_supplement1ItemList);
            List<Item> tempSupplement2All = new List<Item>(tempSupplement1All);
            tempSupplement2All.AddRange(_supplement2ItemList);
            List<Item> tempSupplement3All = new List<Item>(tempSupplement2All);
            tempSupplement3All.AddRange(_supplement3ItemList);

            // 2. 校验初次生成（1个有效类别）
            if (!ValidateSingleStageList(_initialItemList, "初次生成") ||
                !ValidateValidCategoryCount(tempInitialAll, "初次生成后", requiredValidCategoryCount: 1, singleGroupCount))
            {
                return false;
            }

            // 3. 按需校验补充阶段（按要求递增有效类别数量）
            if (totalItemRow >= 7)
            {
                if (!ValidateSingleStageList(_supplement1ItemList, "第一次补充") ||
                    !ValidateValidCategoryCount(tempSupplement1All, "第一次补充后", requiredValidCategoryCount: 2, singleGroupCount))
                {
                    return false;
                }
            }
            if (totalItemRow >= 8)
            {
                if (!ValidateSingleStageList(_supplement2ItemList, "第二次补充") ||
                    !ValidateValidCategoryCount(tempSupplement2All, "第二次补充后", requiredValidCategoryCount: 3, singleGroupCount))
                {
                    return false;
                }
            }
            if (totalItemRow >= 9)
            {
                if (!ValidateSingleStageList(_supplement3ItemList, "第三次补充") ||
                    !ValidateValidCategoryCount(tempSupplement3All, "第三次补充后", requiredValidCategoryCount: 4, singleGroupCount))
                {
                    return false;
                }
            }

            return true;
        }


        //核心校验：统计满足「≥singleGroupCount个」的类别数量，是否达到要求
        private bool ValidateValidCategoryCount(List<Item> itemList, string stageDesc, int requiredValidCategoryCount, int singleGroupCount)
        {
            // 基础校验：物品总数至少满足「有效类别×阈值」（避免无意义计算）
            int minTotalItem = requiredValidCategoryCount * singleGroupCount;
            if (itemList.Count < minTotalItem)
            {
                Debug.LogError($"❌ {stageDesc} - 物品总数不足（需≥{minTotalItem}个，实际{itemList.Count}个），无法满足{requiredValidCategoryCount}个有效类别要求");
                return false;
            }

            // 统计每个类别的物品数量
            Dictionary<ItemCategory, int> categoryCountDict = new Dictionary<ItemCategory, int>();
            foreach (var item in itemList)
            {
                if (item == null) continue;
                categoryCountDict[item.category] = categoryCountDict.TryGetValue(item.category, out int count) ? count + 1 : 1;
            }

            // 统计「满足≥singleGroupCount个」的有效类别数量
            int validCategoryCount = categoryCountDict.Count(kv => kv.Value >= singleGroupCount);

            // 校验是否达标
            if (validCategoryCount < requiredValidCategoryCount)
            {
                // 日志显示详细信息（方便调试）
                string categoryDetail = string.Join(", ", categoryCountDict.Select(kv => $"{kv.Key}:{kv.Value}"));
                Debug.LogWarning($"❌ {stageDesc} - 有效类别数量不达标（需≥{requiredValidCategoryCount}个，实际{validCategoryCount}个），单个类别达标阈值：{singleGroupCount}，当前各类别数量：{categoryDetail}");
                return false;
            }

           // Debug.Log($"✅ {stageDesc} - 有效类别数量达标（需≥{requiredValidCategoryCount}个，实际{validCategoryCount}个）");
            return true;
        }

        // 校验单个阶段列表：每行至少2类（4个物品为1行）
        private bool ValidateSingleStageList(List<Item> stageList, string stageName)
        {
            // 拆分列表为行（4个物品1行）
            List<List<Item>> stageRows = SplitListIntoRows(stageList);

            foreach (var row in stageRows)
            {
                HashSet<ItemCategory> categories = new HashSet<ItemCategory>(row.Select(item => item.category));
                if (categories.Count < 2)
                {
                    Debug.LogWarning($"❌ {stageName} - 某行类别单一（仅{categories.Count}类），不满足规则");
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region 核心3：初次生成方法（GenerateItems）与补充生成方法（SupplementGenerateItems）
        //初次生成：直接按照_initialItemList顺序依次生成到对应格子上
        public void GenerateItems()
        {
            
            CollectGeneratedCells();

            // 校验
            if (!CheckGenerateValid(_initialItemList.Count))
            {
                Debug.LogError("❌ 初次生成失败：校验不通过");
                return;
            }
            //Debug.Log($"🔄 开始初次生成 - 物品数量: {_initialItemList.Count}");
            // 直接按顺序将_initialItemList中的物品分配到对应格子
            for (int i = 0; i < _initialItemList.Count; i++)
            {
                // 获取当前物品和对应的格子
                Item targetItem = _initialItemList[i];
                GridCellControl cell = allGridCells[i];

                if (cell == null)
                {
                    Debug.LogError($"❌ 第{i}个格子为空，无法分配物品");
                    continue;
                }

                if (targetItem == null)
                {
                    Debug.LogError($"❌ 第{i}个物品数据为空");
                    continue;
                }
               //Debug.Log($"🎯 分配物品到格子 [{cell.rowIndex},{cell.colIndex}] - 物品: {targetItem.name}");
                // 实例化新物品
                try
                {
                    ItemControl itemInstance = Instantiate(ItemPrefab, cell.GetComponent<RectTransform>());
                    itemInstance.item = targetItem;
                    // UI适配
                    RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    if (itemRect != null && cellRect != null)
                    {
                        itemRect.pivot = new Vector2(0.5f, 0.5f);
                        itemRect.anchoredPosition = new Vector2(0f, 10f);
                        itemInstance.Initialize();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 实例化异常: {e.Message}");
                }
            }
            //Debug.Log($"✅ 初次生成完成：共{_initialItemList.Count}个物品");
        }

        // 补充生成（外部灵活调用）
        public bool SupplementGenerateItems(int generateState, int targetRowIndex)
        {
            // 静默校验：失败直接返回false，不报错
            if (_currentLevelData == null)
            {
                // 可选保留Warning（不影响运行），不需要可直接删除该行
                Debug.LogWarning("补充生成跳过：动画中或关卡数据未初始化");
                return false;
            }

            // 1. 根据状态获取对应补充列表
            List<Item> supplementList = generateState switch
            {
                1 => _supplement1ItemList,
                2 => _supplement2ItemList,
                3 => _supplement3ItemList,
                _ => null
            };

            // 2. 基础校验：列表为空直接返回
            if (supplementList == null || supplementList.Count == 0)
            {
                Debug.LogWarning($"补充生成跳过：状态{generateState}无对应的物品列表");
                return false;
            }

            // 3. 生成有效性校验：不通过直接返回
            if (!CheckGenerateValid(supplementList.Count, targetRowIndex))
            {
                Debug.LogWarning($"补充生成跳过：状态{generateState}，行索引{targetRowIndex}校验不通过");
                return false;
            }

            // 4. 重复生成校验：目标行已有物品直接返回
            if (IsRowHasItems(targetRowIndex))
            {
                Debug.LogWarning($"补充生成跳过：行索引{targetRowIndex}已存在物品");
                return false;
            }

            // 所有校验通过，才执行生成逻辑
            AssignRowItemsToGrid(supplementList, targetRowIndex);
            Debug.Log($"✅ 补充生成完成：状态{generateState}，行索引{targetRowIndex}，1行共{supplementList.Count}个物品");
            return true;
        }

        //将1行物品（4个）分配到指定行索引的格子
        private void AssignRowItemsToGrid(List<Item> rowItems, int targetRowIndex)
        {
            if (rowItems.Count != COL_COUNT)
            {
                Debug.LogError($"❌ 行物品数量错误：需{COL_COUNT}个，实际{rowItems.Count}个");
                return;
            }

            // 找到目标行的4个格子（按列索引排序）
            var targetCells = allGridCells
                .Where(cell => cell.rowIndex == targetRowIndex)
                .OrderBy(cell => cell.colIndex)
                .ToList();

            if (targetCells.Count != COL_COUNT)
            {
                Debug.LogError($"❌ 目标行{targetRowIndex}格子数量不足：需{COL_COUNT}个，实际{targetCells.Count}个");
                return;
            }

            // 分配物品到格子（复用原有UI逻辑）
            for (int col = 0; col < COL_COUNT; col++)
            {
                GridCellControl cell = targetCells[col];
                Item targetItem = rowItems[col];

                // 实例化新物品
                ItemControl itemInstance = Instantiate(ItemPrefab, cell.GetComponent<RectTransform>());
                itemInstance.item = targetItem;
              

                // UI适配（复用原有逻辑）
                RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
                RectTransform cellRect = cell.GetComponent<RectTransform>();
                if (itemRect != null && cellRect != null)
                {
                    itemRect.pivot = new Vector2(0.5f, 0.5f);
                    itemRect.anchoredPosition = new Vector2(0f, 10f);
                    itemInstance.Initialize();//最后再调用初始化
                }
            }
            //ItemControl Item= targetCells[3].GetComponentInChildren<ItemControl>();
            //Item.Changealpha(0);


        }
        #endregion

        #region 核心4：物品分配+列表处理+辅助方法

        // 将一维物品列表拆分为多行（每行4个）
        private List<List<Item>> SplitListIntoRows(List<Item> itemList)
        {
            List<List<Item>> rows = new List<List<Item>>();
            for (int i = 0; i < itemList.Count; i += COL_COUNT)
            {
                List<Item> row = itemList.Skip(i).Take(COL_COUNT).ToList();
                if (row.Count == COL_COUNT) rows.Add(row);
            }
            return rows;
        }

        // 检查目标行是否已存在物品
        private bool IsRowHasItems(int targetRowIndex)
        {
            var targetCells = allGridCells.Where(cell => cell.rowIndex == targetRowIndex).ToList();
            return targetCells.Any(cell => cell.GetComponentInChildren<ItemControl>() != null);
        }
        // 生成校验（适配初次/补充生成）
        private bool CheckGenerateValid(int needItemCount, int targetRowIndex = -1)
        {
            // 原有基础校验（物品预制体、格子、父节点等，保留原有逻辑）
            if (ItemPrefab == null) { Debug.LogError("❌ 物品预制体未赋值"); return false; }
            if (allGridCells == null || allGridCells.Count == 0) { Debug.LogError("❌ 未收集到格子"); return false; }
            if (gridParent == null || gridParent.GetComponentInParent<Canvas>() == null) { Debug.LogError("❌ UI父节点无效"); return false; }

            // 校验物品数量
            if (needItemCount <= 0) { Debug.LogError("❌ 需生成物品数量无效"); return false; }

            // 补充生成额外校验：目标行索引有效
            if (targetRowIndex >= 0)
            {
                int maxRowIndex = GridCellGenerate.Instance.rowCount - 1;
                if (targetRowIndex < 0 || targetRowIndex > maxRowIndex)
                {
                    Debug.LogError($"❌ 目标行索引{targetRowIndex}无效（最大{maxRowIndex}）");
                    return false;
                }
            }

            return true;
        }
        // 收集格子
        private void CollectGeneratedCells()
        {
            allGridCells.Clear();
            if (GridCellGenerate.Instance == null || GridCellGenerate.Instance.gridParent == null)
            {
                Debug.LogError("❌ 未找到GridCellGenerate实例或格子父节点");
                return;
            }
            foreach (Transform child in GridCellGenerate.Instance.gridParent)
            {
                GridCellControl cell = child.GetComponent<GridCellControl>();
                if (cell != null) allGridCells.Add(cell);
            }
        }
        #endregion

        #region 新手关生成
        [Header("新手关生成内容")] 
        public List<Item> NoviceGenerateltems;
        public void NoviceGenerate()
        {
            CollectGeneratedCells();
            for (int i = 0; i < NoviceGenerateltems.Count; i++)
            {
                // 获取当前物品和对应的格子
                Item targetItem = NoviceGenerateltems[i];
                GridCellControl cell = allGridCells[i];

                if (cell == null)
                {
                    Debug.LogError($"❌ 第{i}个格子为空，无法分配物品");
                    continue;
                }

                if (targetItem == null)
                {
                    Debug.LogError($"❌ 第{i}个物品数据为空");
                    continue;
                }
                // 实例化新物品
                try
                {
                    ItemControl itemInstance = Instantiate(ItemPrefab, cell.GetComponent<RectTransform>());
                    itemInstance.item = targetItem;
                    // UI适配
                    RectTransform itemRect = itemInstance.GetComponent<RectTransform>();
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    if (itemRect != null && cellRect != null)
                    {
                        itemRect.pivot = new Vector2(0.5f, 0.5f);
                        itemRect.anchoredPosition = new Vector2(0f, 10f);
                        itemInstance.Initialize();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ 实例化异常: {e.Message}");
                }
            }
            List<ItemControl> targetitem =new List<ItemControl>();
            List<ItemControl> items = new List<ItemControl>();
            foreach (var cell in allGridCells)
            {
                var itemc=cell.GetComponentInChildren <ItemControl>();
                itemc.canDrag=false;
                itemc.canExchange=false;
                items.Add(itemc);
            }
            //targetitem= items.Where(ItemControl => ItemControl.item.id==1114|| ItemControl.item.id == 1124|| ItemControl.item.id == 1133|| ItemControl.item.id == 1143).ToList();
            //foreach(var itemc in targetitem)
            //{
            //    itemc.canDrag = true;
            //}
            targetitem= items.Where(ItemControl => ItemControl.item.id==1114|| ItemControl.item.id == 1124|| ItemControl.item.id == 1133|| ItemControl.item.id == 1143).ToList();
            targetitem[1].canDrag=true;
            targetitem[0].canExchange=true;
            HintManager.Instance.HintCustomItems(4);
            NoviceHint.Instance.Move1();
            NoviceHint.Instance.NoviceHintImage.SetActive(true); 
        }
        //第二次提示
        public void NoviceState2(ItemCategory category)
        {
            if (category==ItemCategory.Floor&&LevelManager.Instance.currentLevelIndex==0)
            {
                NoviceHint.Instance.stopMove1();
                NoviceHint.Instance.Move2();
                //Debug.Log("NoviceState2");
                List<ItemControl> targetitem = new List<ItemControl>();
                List<ItemControl> items = new List<ItemControl>();
                foreach (var cell in allGridCells)
                {
                    var itemc = cell.GetComponentInChildren<ItemControl>();
                    itemc.canDrag = false;
                    itemc.canExchange = false;
                    items.Add(itemc);
                }

                targetitem = items.Where(ItemControl => ItemControl.item.id == 1114 || ItemControl.item.id == 1124 || ItemControl.item.id == 1133 || ItemControl.item.id == 1143).ToList();
                targetitem[3].canDrag = true;
                targetitem[2].canExchange = true;
                Invoke("DelyHintItem", 2f);
                ItemPairing.Instance.ParingRow -= NoviceState2;
            }
        }
        private void DelyHintItem()
        {
            HintManager.Instance.HintCustomItems(4);
        }


        #endregion

        #region 清除与重置
        // 清理所有物品
        public void ClearAllItems()
        {
            ResetAnimationState();
            int deleteCount = 0;
            foreach (ItemControl item in Object.FindObjectsOfType<ItemControl>(includeInactive: true))
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                    deleteCount++;
                }
            }
            //Debug.Log($"✅ 清理物品：共删除{deleteCount}个");
        }
        //重置所有物品列表（关卡切换时调用）
        public void ResetAllItemLists()
        {
            _initialItemList.Clear();
            _supplement1ItemList.Clear();
            _supplement2ItemList.Clear();
            _supplement3ItemList.Clear();
            _totalItemPool.Clear();
            _currentLevelData = null;
        }
        #endregion 
    }
}