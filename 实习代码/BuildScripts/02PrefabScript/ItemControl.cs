using System;
using System.Collections;
using System.Collections.Generic;
using UIAnimationSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UIAnimationSystem;

namespace ConnectMaster
{
    public class ItemControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // 全局静态变量：标记当前正在拖拽的物品实例（null表示无拖拽）
        private static ItemControl _currentDraggingItem = null;

        [Header("物品核心数据")]
        public Item item; // 关联的物品核心数据
        public Image _itemImage;//外观
        public RectTransform _rectTransform; // 物品的UI位置
        private CanvasGroup _canvasGroup; // 控制UI透明度和射线检测

        [Header("拖拽配置")]
        private Canvas _parentCanvas; // 父物体所在的Canvas（确保层级正确）
        private RectTransform _originalParent; // 拖拽前的原始父节点（起点格子的RectTransform）
        public Vector2 _originalAnchoredPos; // 仅存储「原格子内的本地坐标」（不被覆盖）
        private GridCellControl _startCell; // 缓存起点格子控制组件（优化性能）

        public bool canDrag = true;
        public bool canExchange = true;

        [Header("格子配置")]
        private List<GridCellControl> _allGridCells; // 所有的可放置区域
        private GridCellControl _originalCell; // 记录物品原格子


        [Header("交换动画配置")]
        [Tooltip("交换动画时长（秒），后续可直接调整或替换动画逻辑")]
        public float swapAnimationDuration = 0.3f;
        [Tooltip("动画平滑度（0-1，1为最平滑）")]
        public float animationSmoothness = 0.8f;
        [Tooltip("拖拽时物品透明度")]
        public float dragAlpha = 0.8f;
        [Tooltip("交换时物品透明度")]
        public float swapAlpha = 0.9f;

        [Header("交换完成效果动画参数")]
        public float initialScale = 1f;
        public float peakScale = 1.1f;
        public float finalScale = 1f;
        public float scaleUpDuration = 0.3f;
        public float scaleDownDuration = 0.2f;
        //呼吸动画
        public UILoopAnimation animation;


        #region 生命周期函数
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _parentCanvas = GetComponentInParent<Canvas>();
           
        }

        private void Start()
        {
            // 初始化时设置默认透明度
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }
        private void OnDestroy()
        {
            // 新增：物品销毁时释放全局拖拽锁
            if (_currentDraggingItem == this)
            {
                _currentDraggingItem = null;
            }
        }
        #endregion

        #region 初始化
        public void Initialize()
        {
            if (item == null || _itemImage == null)
            {
                Debug.LogError("物品初始化失败：item或_itemImage为空", gameObject);
                return;
            }

            // 物品UI初始化
            _itemImage.sprite = item.itemIcon;

            // 先获取原格子，再获取原父节点
            _originalCell = transform.parent.GetComponent<GridCellControl>();
            if (_originalCell == null)
            {
                Debug.LogError("物品初始化失败：父物体不是格子（缺少GridCellControl）", gameObject);
                return;
            }

            // 缓存起点格子相关数据
            _originalParent = _originalCell.GetComponent<RectTransform>();
            _startCell = _originalCell;
            _originalCell._currentItem = this.item; // 绑定物品到格子

            // 仅此处赋值：记录「物品在原格子内的本地坐标」（不被后续修改）
            _originalAnchoredPos = _rectTransform.anchoredPosition;

            // 查找格子父容器，收集所有可放置格子（安全校验）
            if (GridCellGenerate.Instance == null || GridCellGenerate.Instance.gridParent == null)
            {
                Debug.LogError("物品初始化失败：GridCellGenerate实例或gridParent为空", gameObject);
                return;
            }

            GameObject gridContainer = GridCellGenerate.Instance.gridParent.gameObject;
            _allGridCells = new List<GridCellControl>(gridContainer.GetComponentsInChildren<GridCellControl>());
        }
        #endregion

        public void Stopanimation()
        {
            animation.StopAllAnimations();
        }

        #region 拖拽核心逻辑
        private void AbortDrag()
        {
            if (_currentDraggingItem == this)
            {
                _currentDraggingItem = null;
            }
        }

        // 拖拽开始
        public void OnBeginDrag(PointerEventData eventData)
        {
            // 检查是否有其他物品正在拖拽（全局锁机制）
            if (_currentDraggingItem != null && _currentDraggingItem != this)
            {
                // 已经有其他物品在拖拽，不允许开始新的拖拽
                eventData.delta = Vector2.zero;
                eventData.pressPosition = Vector2.zero;
                eventData.pointerDrag = null;
                eventData.pointerEnter = null;
                return;
            }

            // 检查其他条件（动画、配对状态、可拖拽性）
            if (ItemGenerate.Instance.IsAnimating || _originalCell.isPairing || !canDrag)
            {
                AbortDrag();
                eventData.delta = Vector2.zero;
                eventData.pressPosition = Vector2.zero;
                eventData.pointerDrag = null;
                eventData.pointerEnter = null;
                return;
            }

            // 占用全局拖拽锁（标记当前物品为拖拽中）
            _currentDraggingItem = this;

            SoundManager.Instance.PlaySound("1");
            VibrationManager.VibrateShort();


            // 清空起点格子的物品数据（避免重复关联）
            _originalCell._currentItem = null;

            // 1. 移至Canvas顶层，突破格子层级限制
            TopHierarchy();

            // 2. 坐标转换：调用封装的 LocalToCanvasPos 方法（替代原手动转换，逻辑完全一致）
            Vector2 canvasLocalPos = LocalToCanvasPos(_originalParent, _originalAnchoredPos);
            _rectTransform.anchoredPosition = canvasLocalPos;

            // 3. 拖拽状态配置（不阻挡射线，避免拖拽时无法检测目标格子）
            ChangeCblocksRaycasts(false);
        }

        // 拖拽过程：适配Canvas缩放，确保拖拽速度正常
        public void OnDrag(PointerEventData eventData)
        {
            // 检查当前拖拽的合法性
            if (_currentDraggingItem != this)
            {
                // 不是当前拖拽的物品，不允许拖拽
                eventData.delta = Vector2.zero;
                eventData.pressPosition = Vector2.zero;
                eventData.pointerDrag = null;
                eventData.pointerEnter = null;
                return;
            }

            // 检查其他条件
            if (ItemGenerate.Instance.IsAnimating || _originalCell.isPairing || !canDrag)
            {
                AbortDrag();
                eventData.delta = Vector2.zero;
                eventData.pressPosition = Vector2.zero;
                eventData.pointerDrag = null;
                eventData.pointerEnter = null;
                return;
            }

            TopHierarchy();
            CoordinateDraggingSpeed(eventData);
            //实时检测命中的格子，显示边框（其他格子隐藏）
            HideAllGridBorders(); // 先隐藏所有边框，避免多个边框同时显示
            GridCellControl hitCell = FindHitGridCell();
            if (hitCell != null && hitCell != _originalCell)
            {
                hitCell.SetBorderable(true); // 命中格子显示边框
            }
        }

        // 拖拽结束：执行物品互换或放回原格子
        public void OnEndDrag(PointerEventData eventData)
        {
            // 检查当前拖拽的合法性
            if (_currentDraggingItem != this)
            {
                // 不是当前拖拽的物品，不允许结束拖拽
                eventData.delta = Vector2.zero;
                eventData.pressPosition = Vector2.zero;
                eventData.pointerDrag = null;
                eventData.pointerEnter = null;
                return;
            }

            // 检查其他条件
            if (ItemGenerate.Instance.IsAnimating || _originalCell.isPairing || !canDrag)
            {
                AbortDrag();
                eventData.delta = Vector2.zero;
                eventData.pressPosition = Vector2.zero;
                eventData.pointerDrag = null;
                eventData.pointerEnter = null;
                return;
            }
            // 恢复射线检测
            ChangeCblocksRaycasts(true);
            HideAllGridBorders(); //拖拽结束后隐藏所有边框

            // 查找命中的目标格子
            GridCellControl targetCell = FindHitGridCell();
            if (targetCell != null && targetCell != _originalCell)
            {
                SwapItems(targetCell); // 目标格子有效，执行互换
                SoundManager.Instance.PlaySound("34");
                VibrationManager.VibrateShort();
            }
            else
            {
                SoundManager.Instance.PlaySound("2");
                VibrationManager.VibrateShort();
                // 直接启动放回动画协程
                StartCoroutine(MoveAnimationCoroutine(_originalCell)); // 未命中有效格子，放回起点
            }
            // 释放全局拖拽锁（仅当当前物品为标记的拖拽物品时）
            if (_currentDraggingItem == this)
            {
                _currentDraggingItem = null;
            }
        }
        #endregion

        #region 物品互换逻辑
        //核心互换方法
        private void SwapItems(GridCellControl targetCell)
        {
           ItemControl targetItem =FindTargetCellItem(targetCell);
            if(targetItem.canExchange)
            {
                StartCoroutine(SwapAnimationCoroutine(targetItem, this, OnSwapAnimationDone));
            }
            else
            {
                StartCoroutine(MoveAnimationCoroutine(_originalCell)); // 不能交换，放回起点
            }
        }
        private void OnSwapAnimationDone()
        {
            HintManager.Instance.UpdateColorsAfterSwap();//更新颜色
            ItemPairing.Instance.CheckHavePairing();//检查是否配对
        }

        #endregion

        #region 交换动画
        // 物品交换动画
        public IEnumerator SwapAnimationCoroutine(ItemControl targetItem, ItemControl currentItem, Action OnSwapAnimationDone)
        {
            // 1. 前置校验：空值直接返回，避免无效加锁
            if (targetItem == null || currentItem == null)
            {
                Debug.LogError($"❌ 交换动画失败：目标物品/当前物品为空（targetItem:{targetItem != null}, currentItem:{currentItem != null}）");
                yield break;
            }

            // 2. 动画准备
            ItemGenerate.Instance.LockAnimation();
            currentItem.TopHierarchy();
            targetItem.TopHierarchy();

            // 记录初始状态（Canvas坐标系下的位置）
            Vector2 currentStartPos = currentItem._rectTransform.anchoredPosition;
            Vector2 targetStartPos = targetItem._rectTransform.anchoredPosition;

            // 计算目标位置（交换后各自对应的格子位置，转换为Canvas坐标）
            Vector2 currentTargetPos = LocalToCanvasPos(
             targetItem._originalParent,
                targetItem._originalAnchoredPos 
                );
            Vector2 targetTargetPos = LocalToCanvasPos(
                currentItem._originalParent,
                currentItem._originalAnchoredPos 
            );
            // 动画参数初始化（Vector2类型，匹配SmoothDamp要求）
            Vector2 currentVelocity = Vector2.zero;
            Vector2 targetVelocity = Vector2.zero;
            float elapsedTime = 0f;
            float totalDuration = swapAnimationDuration;

            // 设置交换期间透明度
            //currentItem.Changealpha(swapAlpha);
            //targetItem.Changealpha(swapAlpha);

            // 3. 平滑交换动画（核心：全程基于Canvas坐标，父节点不变）
            while (elapsedTime < totalDuration)
            {
                // 防止动画过程中状态被篡改（双重锁定）
                if (!ItemGenerate.Instance.IsAnimating) break;

                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / totalDuration);
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress); // 缓入缓出，自然流畅

                // 关键：基于Canvas坐标更新位置（父节点未变，基准统一）
                currentItem._rectTransform.anchoredPosition = Vector2.Lerp(
                    currentStartPos,
                    currentTargetPos,
                    smoothProgress
                );
                targetItem._rectTransform.anchoredPosition = Vector2.Lerp(
                    targetStartPos,
                    targetTargetPos,
                    smoothProgress
                );

                yield return null; // 等待下一帧，确保动画连续
            }

            // 强制校正最终位置（避免动画累积误差）
            currentItem._rectTransform.anchoredPosition = currentTargetPos;
            targetItem._rectTransform.anchoredPosition = targetTargetPos;

            // 3. 数据交换（核心：更新父节点、格子绑定、物品配置）
            // 缓存当前物品的原始配置
            RectTransform currentOrigParent = currentItem._originalParent;
            Vector2 currentOrigPos = currentItem._originalAnchoredPos;
            GridCellControl currentOrigCell = currentItem._originalCell;

            // 缓存目标物品的原始配置
            RectTransform targetOrigParent = targetItem._originalParent;
            Vector2 targetOrigPos = targetItem._originalAnchoredPos;
            GridCellControl targetOrigCell = targetItem._originalCell;

            // 更新当前物品配置（迁移到目标格子）
            currentItem._originalParent = targetOrigParent;
            currentItem._originalAnchoredPos = targetOrigPos;
            currentItem._originalCell = targetOrigCell;
            currentItem._startCell = targetOrigCell;
            targetOrigCell._currentItem = currentItem.item; // 绑定物品到新格子

            // 更新目标物品配置（迁移到原格子）
            targetItem._originalParent = currentOrigParent;
            targetItem._originalAnchoredPos = currentOrigPos;
            targetItem._originalCell = currentOrigCell;
            targetItem._startCell = currentOrigCell;
            currentOrigCell._currentItem = targetItem.item; // 绑定物品到新格子

            //---交换完成动画---
            // 直接使用已有的targetItem
            StartCoroutine(this.ExchangeDoneAnimation()); // 当前物品执行动画
            StartCoroutine(targetItem.ExchangeDoneAnimation()); // 目标物品执行动画

            // 4. 动画结束清理
            //currentItem.Changealpha(1f);
            //targetItem.Changealpha(1f);
            currentItem.BackHierarchy();
            targetItem.BackHierarchy();
            ItemGenerate.Instance.UnlockAnimation();
            OnSwapAnimationDone?.Invoke();


        }
        #endregion

        #region 放回动画
        private IEnumerator MoveAnimationCoroutine(GridCellControl targetCell)
        {
            // 安全校验（避免空引用崩溃）
            if (targetCell == null || targetCell._rectTransform == null)
            {
                Debug.LogError("放回动画失败：目标格子或格子RectTransform为空");
                yield break;
            }

            // 替换直接赋值 → 引用计数+1
            ItemGenerate.Instance.LockAnimation();
            TopHierarchy();

            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector2 targetPos = LocalToCanvasPos(
                targetCell._rectTransform,
                _originalAnchoredPos
            );

            float elapsedTime = 0f;
            float totalDuration = swapAnimationDuration;

            // 改用 IsAnimating 只读属性判断
            while (elapsedTime < totalDuration && ItemGenerate.Instance.IsAnimating)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / totalDuration);
                float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

                _rectTransform.anchoredPosition = Vector2.Lerp(
                    startPos,
                    targetPos,
                    smoothProgress
                );

                yield return null;
            }

            _rectTransform.anchoredPosition = targetPos;
            targetCell._currentItem = this.item;
            //Changealpha(1f);
            BackHierarchy();

            // 替换直接赋值 → 引用计数-1
            ItemGenerate.Instance.UnlockAnimation();
        }
        #endregion

        #region 层级与父节点更改

        //移至Canvas顶层（突破格子层级限制，避免遮挡）
        public void TopHierarchy()
        {
            // 安全校验：Canvas和物品RectTransform不能为空
            if (_parentCanvas == null || _rectTransform == null)
            {
                Debug.LogError("TopHierarchy失败：Canvas或物品RectTransform为空");
                return;
            }

            RectTransform canvasRect = _parentCanvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Debug.LogError("TopHierarchy失败：Canvas缺少RectTransform组件");
                return;
            }

            // 移至Canvas下，设为顶层
            _rectTransform.SetParent(ItemGenerate.Instance.TopHierarchyPoint);
            _rectTransform.SetAsLastSibling(); // 置顶，避免被其他UI遮挡
        }

        //放回原格子层级（恢复到拖拽前的父节点和位置）
        public void BackHierarchy()
        {
            // 安全校验：原父节点和物品RectTransform不能为空
            if (_originalParent == null || _rectTransform == null)
            {
                Debug.LogError("BackHierarchy失败：原父节点或物品RectTransform为空");
                return;
            }

            // 放回原格子下，恢复原位置
            _rectTransform.SetParent(_originalParent);
            _rectTransform.anchoredPosition = _originalAnchoredPos;
            _rectTransform.SetAsLastSibling(); // 在格子内置顶，避免被遮挡
        }
        #endregion

        #region 坐标转换
        // 坐标转换通用方法（本地坐标 → Canvas本地坐标）
        public Vector2 LocalToCanvasPos(RectTransform targetRect, Vector2 localPos)
        {
            if (targetRect == null || _parentCanvas == null) return Vector2.zero;

            RectTransform canvasRect = _parentCanvas.GetComponent<RectTransform>();
            // 本地 → 世界 → 屏幕 → Canvas本地（严格遵循桥梁规则）
            Vector3 worldPos = targetRect.TransformPoint(localPos);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_parentCanvas.worldCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, _parentCanvas.worldCamera, out Vector2 canvasPos);
            return canvasPos;
        }

        //坐标转换通用方法（Canvas本地坐标 → 目标Rect本地坐标）
        public Vector2 CanvasToLocalPos(RectTransform targetRect, Vector2 canvasPos)
        {
            if (targetRect == null || _parentCanvas == null) return Vector2.zero;

            RectTransform canvasRect = _parentCanvas.GetComponent<RectTransform>();
            // Canvas本地 → 世界 → 屏幕 → 目标本地（严格遵循桥梁规则）
            Vector3 worldPos = canvasRect.TransformPoint(canvasPos);
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_parentCanvas.worldCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetRect, screenPos, _parentCanvas.worldCamera, out Vector2 localPos);
            return localPos;
        }
        #endregion 

        #region 辅助方法
        //查找目标格子中的原物品（ItemControl组件）
        private ItemControl FindTargetCellItem(GridCellControl gridCell)
        {
            if (gridCell == null)
            {
                Debug.LogError("FindTargetCellItem：目标格子为null！", gameObject);
                return null;
            }

            // 查找格子下的ItemControl（物品是直接子物体）
            ItemControl targetItem = gridCell.GetComponentInChildren<ItemControl>();
            return targetItem;
        }

        // 查找鼠标命中的格子
        private GridCellControl FindHitGridCell()
        {
            foreach (var cell in _allGridCells)
            {
                if (cell != null && cell.IsMouseOverCell())
                {
                    return cell;
                }
            }
            return null;
        }

        // 协调拖拽速度（适配Canvas缩放）
        private void CoordinateDraggingSpeed(PointerEventData eventData)
        {
            // 安全校验：避免Canvas或RectTransform为空
            if (_parentCanvas == null || _rectTransform == null)
            {
                Debug.LogError("拖拽速度协调失败：Canvas或物品RectTransform为空");
                return;
            }

            RectTransform canvasRect = _parentCanvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Debug.LogError("拖拽速度协调失败：Canvas缺少RectTransform");
                return;
            }

            // 关键：获取Canvas的「屏幕空间大小」和「本地坐标大小」，计算比例
            Vector2 canvasScreenSize = RectTransformUtility.PixelAdjustRect(canvasRect, _parentCanvas).size;
            Vector2 canvasLocalSize = canvasRect.sizeDelta;

            // 避免除零（极端情况Canvas大小为0时用默认比例）
            if (canvasScreenSize.x < 1 || canvasScreenSize.y < 1 || canvasLocalSize.x < 1 || canvasLocalSize.y < 1)
            {
                _rectTransform.anchoredPosition += eventData.delta / (_parentCanvas.scaleFactor > 0 ? _parentCanvas.scaleFactor : 1f);
                return;
            }

            // 计算「屏幕像素 → Canvas本地坐标」的比例（x/y轴分别计算，适配非等比缩放）
            float xRatio = canvasLocalSize.x / canvasScreenSize.x;
            float yRatio = canvasLocalSize.y / canvasScreenSize.y;
            Vector2 speedRatio = new Vector2(xRatio, yRatio);

            // 最终移动量 = 鼠标像素移动量 × 比例 ÷ UI缩放（确保缩放后速度仍同步）
            float scaleFactor = _parentCanvas.scaleFactor > 0 ? _parentCanvas.scaleFactor : 1f;
            Vector2 moveDelta = eventData.delta * speedRatio / scaleFactor;

            // 应用移动（Canvas本地坐标，与鼠标完全同步）
            _rectTransform.anchoredPosition += moveDelta;
        }

        // 控制CanvasGroup（透明度）
        public void Changealpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }
        // 控制CanvasGroup（射线检测）
        private void ChangeCblocksRaycasts(bool blocksRaycasts)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = blocksRaycasts;
            }
        }
        #endregion

        #region 边框相关
        private void HideAllGridBorders()
        {
            foreach (var cell in _allGridCells)
            {
                if (cell != null)
                    cell.SetBorderable(false);
            }
        }
        #endregion

        #region 交换完成动画相关
        public IEnumerator ExchangeDoneAnimation()
        {
            ItemGenerate.Instance.LockAnimation();
            // 空引用安全校验：确保当前物品的RectTransform存在
            if (_rectTransform == null)
            {
                Debug.LogError("ExchangeDoneAnimation错误：当前物品的RectTransform为空", gameObject);
                yield break;
            }

            // 保存当前物品原有的缩放比例
            Vector3 originalScale = _rectTransform.localScale;

            // 第一阶段：从初始缩放放大到峰值缩放
            float elapsedTime = 0f;
            Vector3 startScale = originalScale * initialScale;
            Vector3 peakScaleVector = originalScale * peakScale;

            _rectTransform.localScale = startScale;

            // 放大到峰值
            while (elapsedTime < scaleUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / scaleUpDuration;
                _rectTransform.localScale = Vector3.Lerp(startScale, peakScaleVector, progress);
                yield return null;
            }

            _rectTransform.localScale = peakScaleVector;

            // 第二阶段：从峰值缩放到最终缩放
            elapsedTime = 0f;
            Vector3 finalScaleVector = originalScale * finalScale;

            while (elapsedTime < scaleDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / scaleDownDuration;
                _rectTransform.localScale = Vector3.Lerp(peakScaleVector, finalScaleVector, progress);
                yield return null;
            }

            // 动画结束：强制恢复最终状态（避免插值误差）
            _rectTransform.localScale = finalScaleVector;
            ItemGenerate.Instance.UnlockAnimation();
        }
        

        #endregion


    }
}