using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace ConnectMaster
{
    public class HintManager : MonoBehaviour
    {
       public static HintManager Instance;
        [Header("UI配置")]
       public RectTransform HintPlane;
       public TMP_Text HintText;
        private CanvasGroup hintCanvasGroup; // 控制Alpha的核心组件
        [Header("初始位置与显示位置")]
        public Vector2 InitializePosition;
       public Vector2 TargetPosition;
        [Header("动画时长")]
        public float AnimationTime=0.5f;
        [Tooltip("消失动画曲线")]
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #region 关键词提示相关
        //是否在关键词动画中
       // private bool isAnimation=false; 
        //是否正在显示关键词
        private bool isShowHintKeyWord=false;
        //当前关键词
        private ItemCategory Keywords = ItemCategory.None;
        //上一个关键词是……
        private ItemCategory lastKeyword = ItemCategory.None;
        //存储配对的类型
        public Dictionary<ItemCategory, int> CategoryPairing = new Dictionary<ItemCategory, int>();
        #endregion

        #region 物品提示相关
        public ItemCategory ItemCategoryWord = ItemCategory.None;
        // 自定义提示数量（1-4）
        private int _customHintCount = 4;
        private List<ItemControl> _hintTargetItems = new List<ItemControl>();
        #endregion



        #region 生命周期函数

        private void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            InitializeHint();
        }
        private void Start()
        {
            ItemPairing.Instance.ParingRow += EndHintKeywords;
            ItemPairing.Instance.ParingRow += EndHintItem;
        }
        private void OnDestroy()
        {
            ItemPairing.Instance.ParingRow -= EndHintKeywords;
            ItemPairing.Instance.ParingRow -= EndHintItem;
        }
        #endregion

        #region 初始化
        public void InitializeHint()
        {
            // 自动获取/添加CanvasGroup
            hintCanvasGroup = HintPlane.GetComponent<CanvasGroup>() ?? HintPlane.gameObject.AddComponent<CanvasGroup>();
            hintCanvasGroup.alpha = 0;
            HintPlane.anchoredPosition = InitializePosition;
            //isAnimation = false;
            isShowHintKeyWord = false;
            lastKeyword = ItemCategory.None;
            //HintPlane.gameObject.SetActive(false);
        }

        #endregion

        #region 找到可匹配类型
        //找到可匹配类型
        public ItemCategory FindCanParingItemCategory()
        {
            ItemPairing.Instance.UpdateAllCell();
            CategoryPairing.Clear();
            foreach (var cell in ItemPairing.Instance.allGridCells)
            {
                //空值检查
                if (cell == null || cell._currentItem == null || cell.isPairing)
                    continue;

                var category = cell._currentItem.category;

                if (CategoryPairing.TryGetValue(category, out int count))
                {
                    CategoryPairing[category] = count + 1; // 存在则自增
                }
                else
                {
                    CategoryPairing.Add(category, 1); // 不存在则新增
                }
            }

            List<ItemCategory> targetCategories = new();
            foreach (var kvp in CategoryPairing)
            {
                if (kvp.Value == 4)
                {
                    targetCategories.Add(kvp.Key);
                }
            }
            return targetCategories.Count > 0 ? targetCategories[0] : ItemCategory.None;
        }
        public List<ItemCategory> FindCanParingItemCategoryList()
        {
            ItemPairing.Instance.UpdateAllCell();
            CategoryPairing.Clear();
            foreach (var cell in ItemPairing.Instance.allGridCells)
            {
                //空值检查
                if (cell == null || cell._currentItem == null || cell.isPairing)
                    continue;

                var category = cell._currentItem.category;

                if (CategoryPairing.TryGetValue(category, out int count))
                {
                    CategoryPairing[category] = count + 1; // 存在则自增
                }
                else
                {
                    CategoryPairing.Add(category, 1); // 不存在则新增
                }
            }

            List<ItemCategory> targetCategories = new();
            foreach (var kvp in CategoryPairing)
            {
                if (kvp.Value == 4)
                {
                    targetCategories.Add(kvp.Key);
                }
            }
            return targetCategories;
        }
        #endregion

        #region 提示关键词
        public void HintKeywords()
        {
            List<ItemCategory> targetItems = FindCanParingItemCategoryList();
            if (targetItems == null || targetItems.Count == 0)
            {
                return;
            }

            if (targetItems.Count == 1)
            {
                Keywords = targetItems[0];
            }
            else
            {
                // 排除上次显示的类型
                var availableTypes = targetItems.FindAll(category => category != lastKeyword);

                // 如果没有可用的类型（比如只有两种类型且上次显示过其中一个），就用所有类型
                if (availableTypes.Count == 0)
                {
                    availableTypes = targetItems;
                }

                // 随机选择
                Keywords = availableTypes[Random.Range(0, availableTypes.Count)];
                lastKeyword = Keywords;
            }

            HintText.text = Keywords.ToString();
            isShowHintKeyWord = true;
            StartCoroutine(HintPlaneUpCoroutine());
        }

        public void EndHintKeywords(ItemCategory Category)
        {
            if (!isShowHintKeyWord)
            {
                return;//没有显示框前无反应
            }
            if(Keywords== Category)
            {
                isShowHintKeyWord = false;
                //向下隐藏
                StartCoroutine(HintPlaneDownCoroutine());
            }
        }
        #endregion

        #region 提示物品
        //public void HintItem()
        //{
        //    ItemCategory targetItem = FindCanParingItemCategory();
        //    if (targetItem == ItemCategory.None)
        //    {
        //        return;
        //    }
        //    ItemCategoryWord = targetItem;

        //    UpdateColorsAfterSwap();
        //    //Debug.Log("HintItem:"+Instance.ItemCategoryWord);
        //}

        //// 交换后更新颜色的专用方法
        //public void UpdateColorsAfterSwap()
        //{
        //    ItemPairing.Instance.UpdateAllCell();

        //    foreach (var cell in ItemPairing.Instance.allGridCells)
        //    {
        //        if (cell == null || cell._currentItem == null)
        //            continue;

        //        cell.UpdateColor();
        //    }
        //}
        ////当物品提示结束
        //public void EndHintItem(ItemCategory Category)
        //{

        //    if (ItemCategoryWord == Category)
        //    {

        //        ItemCategoryWord =ItemCategory.None;
        //        ItemPairing.Instance.UpdateAllCell();
        //        foreach (var cell in ItemPairing.Instance.allGridCells)
        //        {
        //            if (cell == null || cell._currentItem == null || cell.isPairing)
        //                continue;
        //            cell.SetHighlight(false);
        //        }
        //    }
        //}

        // 1. 修改自定义提示方法：缓存ItemControl
        public void HintCustomItems(int count)
        {
            _customHintCount = Mathf.Clamp(count, 1, 4);
            ItemCategory targetItem = FindCanParingItemCategory();
            if (targetItem == ItemCategory.None)
            {
                _hintTargetItems.Clear(); // 无目标时清空缓存
                return;
            }

            ItemCategoryWord = targetItem;
            _hintTargetItems.Clear(); // 重置缓存

            ItemPairing.Instance.UpdateAllCell();
            // 筛选符合条件的ItemControl并缓存（而非格子）
            var validItems = ItemPairing.Instance.allGridCells
                .Where(cell => cell != null && cell._currentItem != null && !cell.isPairing
                        && cell._currentItem.category == targetItem)
                .Select(cell => cell.GetComponentInChildren<ItemControl>()) // 从格子取物品
                .Where(item => item != null) // 过滤空物品
                .Take(_customHintCount)
                .ToList();

            _hintTargetItems = validItems;
            UpdateCustomHintColors(); // 更新高亮
        }

        // 2. 修改高亮更新方法：基于缓存的ItemControl找当前格子
        private void UpdateCustomHintColors()
        {
            if (ItemCategoryWord == ItemCategory.None || _hintTargetItems.Count == 0) return;

            ItemPairing.Instance.UpdateAllCell();
            foreach (var cell in ItemPairing.Instance.allGridCells)
            {
                if (cell == null || cell._currentItem == null || cell.isPairing)
                {
                    //cell.SetHighlight(false);
                    continue;
                }

                // 关键：判断当前格子的物品是否在缓存的提示列表中
                ItemControl cellItem = cell.GetComponentInChildren<ItemControl>();
                bool isHighlight = _hintTargetItems.Contains(cellItem);
                cell.SetHighlight(isHighlight);
            }
        }

        // 3. 修改结束提示逻辑：清空物品缓存
        public void EndHintItem(ItemCategory Category)
        {
            if (ItemCategoryWord == Category)
            {
                ItemCategoryWord = ItemCategory.None;
                _hintTargetItems.Clear(); // 清空物品缓存

                // 清除所有高亮
                ItemPairing.Instance.UpdateAllCell();
                foreach (var cell in ItemPairing.Instance.allGridCells)
                {
                    if (cell == null || cell._currentItem == null || cell.isPairing) continue;
                    cell.SetHighlight(false);
                }
            }
        }

        // 兼容交换后的颜色更新
        public void UpdateColorsAfterSwap()
        {
            if (ItemCategoryWord != ItemCategory.None)
            {
                UpdateCustomHintColors();
            }
        }
        #endregion

        #region 移动动画效果
        //向上显示
        private IEnumerator HintPlaneUpCoroutine()
        {
            float elapsedTime = 0f;
            while (elapsedTime < AnimationTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / AnimationTime);
                float smoothProgress = Curve.Evaluate(progress);
                // Alpha平滑过渡（0→1）
                hintCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothProgress);
                HintPlane.anchoredPosition = Vector2.Lerp(InitializePosition, TargetPosition, smoothProgress);
                yield return null;
            }
            HintPlane.anchoredPosition = TargetPosition;
        }
        //向下隐藏
        private IEnumerator HintPlaneDownCoroutine()
        {
            float elapsedTime = 0f;
            while (elapsedTime < AnimationTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / AnimationTime);
                float smoothProgress = Curve.Evaluate(progress);
                // Alpha平滑过渡（1→0）
                hintCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothProgress);
                HintPlane.anchoredPosition = Vector2.Lerp(TargetPosition, InitializePosition, smoothProgress);
                yield return null;
            }
            HintPlane.anchoredPosition = InitializePosition;
        }
        #endregion
    }

}
