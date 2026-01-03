using System;
using System.Collections;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace ConnectMaster
{
    public class GridCellControl : MonoBehaviour
    {
        [Header("格子配置")]
        [Tooltip("相对行号（1-4），从1开始计数，左上角为第1行")]
        public int rowIndex; // 相对行号
        [Tooltip("相对列号（1-4），从1开始计数，左上角为第1列")]
        public int colIndex; // 相对列号
        [HideInInspector]public Item _currentItem;
        public RectTransform _rectTransform;
        public bool isPairing = false;

        [Header("配对完成效果动画参数")]
        public float initialScale = 1f;
        public float peakScale = 1.1f;
        public float finalScale = 1f;
        public float scaleUpDuration = 0.3f;
        public float scaleDownDuration = 0.2f;

        [Header("物品掉落动画配置")]
        [Tooltip("掉落初始高度（UI像素）")]
        public float dropHeight = 250f;
        [Tooltip("掉落时长（秒）")]
        public float dropDuration = 0.4f;
        [Tooltip("回弹幅度（UI像素）")]
        public float bounceAmplitude = 20f;
        [Tooltip("回弹时长（秒）")]
        public float bounceDuration = 0.15f;
        [Tooltip("格子间掉落延迟系数（行列越大越晚掉）")]
        public float dropDelayFactor = 0.05f;

        [Header("物品偏移")]
        public Vector2 ItemOffset = new Vector2(0f, 10f);

        [Tooltip("格子的背景Image组件（用于上色）")]
        public Image cellBackground;

        [Header("边框显示控制")]
        public GameObject border;
        private Canvas _parentCanvas; // 缓存格子所在的Canvas，用于获取UI相机

        [Header("图片切换")]
        public Sprite sprite1;
        public Sprite sprite2;
        // 引用预制体上的Image组件
        public Image targetImage;
        // 当前显示的是哪张图（用于切换逻辑）
        private bool isShowingSprite1 = true;


        #region 生命周期函数
        private void Awake()
        {
            Initialize();

            // 修正：优先找自身，没有则找子物体（适配UI结构）
            targetImage = GetComponent<Image>() ?? GetComponentInChildren<Image>();

            // 初始化显示第一张图（增加空值校验）
            if (targetImage != null && sprite1 != null)
            {
                targetImage.sprite = sprite1;
                isShowingSprite1 = true; // 确保初始状态和显示一致
            }
            else
            {
                Debug.LogError($"格子[{rowIndex},{colIndex}]：Image组件或Sprite1未赋值！", gameObject);
            }
        }
        private void Update()
        {

        }
        #endregion

        #region 初始化 
        private void Initialize()
        {
            _rectTransform = GetComponent<RectTransform>();
            // 获取父级Canvas（关键）
            _parentCanvas = GetComponentInParent<Canvas>();

            // 安全校验
            if (_parentCanvas == null)
            {
                Debug.LogError($"格子[{rowIndex},{colIndex}]未找到父级Canvas！", gameObject);
            }
        }
        #endregion

        #region  切换图片
        /// <summary>
        /// 切换图片（切换到另一张）
        /// </summary>
        public void ToggleImage()
        {
            if (targetImage == null) return;
            
            if (isShowingSprite1)
            {
                // 切换到第二张
                targetImage.sprite = sprite2;
            }
            else
            {
                // 切换到第一张
                targetImage.sprite = sprite1;
            }
            // 翻转状态
            isShowingSprite1 = !isShowingSprite1;
        }
        #endregion

        #region 边框相关
        // 方法1：直接禁用Image组件（推荐，完全隐藏边框）
        public void SetBorderable(bool able)
        {
            border.SetActive(able);
        }
        private void HideAllGridBorders()
        {
            foreach (var cell in ItemPairing.Instance.allGridCells)
            {
                if (cell != null)
                    cell.SetBorderable(false);
            }
        }
        #endregion

        #region 格子变色
        public void SetHighlight(bool able)
        {
            if (cellBackground != null)
            {
                // 用Color构造函数（参数为0~1的浮点数，由RGB值/255得到）
                cellBackground.color = able
                    ? new UnityEngine.Color(255f / 255f, 178f / 255f, 224f / 255f) // 对应#FFB2E0（alpha默认1，即不透明）
                    : UnityEngine.Color.white;
            }
        }


        public void UpdateColor()
        {
            if(isPairing)
            {
                return;
            }

            // 如果没有提示类型，取消高亮
            if (HintManager.Instance.ItemCategoryWord == ItemCategory.None)
            {
                SetHighlight(false);
                return;
            }

            // 颜色判断逻辑
            if (_currentItem.category == HintManager.Instance.ItemCategoryWord)
            {

                //Debug.Log(HintManager.Instance.ItemCategoryWord);
                SetHighlight(true);
                
            }
            else
            {
                SetHighlight(false);
            }
        }
        #endregion

        #region 辅助方法
        public bool IsMouseOverCell()
        {
            if (_rectTransform == null || _parentCanvas == null)
                return false;

            // 关键：根据Canvas渲染模式动态选择相机
            Camera uiCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceCamera
                ? _parentCanvas.worldCamera
                : null;

            // 校验Camera模式下的相机配置
            if (_parentCanvas.renderMode == RenderMode.ScreenSpaceCamera && uiCamera == null)
            {
                Debug.LogWarning($"格子[{rowIndex},{colIndex}]：Canvas需配置Render Camera！", gameObject);
                return false;
            }

            // 带相机参数的射线检测（适配两种模式）
            return RectTransformUtility.RectangleContainsScreenPoint(
                _rectTransform,
                Input.mousePosition,
                uiCamera // Camera模式传相机，Overlay模式传null
            );
        }
        #endregion

        #region 交换完成动画相关

        // 带完成回调的版本（供StartAnimation调用，核心）
        public IEnumerator ExchangeDoneAnimation(Action onComplete)
        {

            float delytime = (colIndex / 8f); // 修复：加f避免整数除法（原代码colIndex/8永远是0）
            yield return new WaitForSeconds(delytime);


            // 空引用安全校验：确保RectTransform存在
            if (_rectTransform == null)
            {
                Debug.LogError($"格子[{rowIndex},{colIndex}] RectTransform为空，无法执行跳动动画", gameObject);
                
                onComplete?.Invoke(); // 即使出错，也触发回调，避免外部卡死
                yield break;
            }

            Vector3 originalScale = _rectTransform.localScale;
            Vector3 startScale = originalScale * initialScale;
            Vector3 peakScaleVector = originalScale * peakScale;
            Vector3 finalScaleVector = originalScale * finalScale;

            // 第一阶段：放大
            float elapsedTime = 0f;
            while (elapsedTime < scaleUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / scaleUpDuration);
                _rectTransform.localScale = Vector3.Lerp(startScale, peakScaleVector, progress);
                yield return null;
            }

            // 第二阶段：缩小
            elapsedTime = 0f;
            while (elapsedTime < scaleDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / scaleDownDuration);
                _rectTransform.localScale = Vector3.Lerp(peakScaleVector, finalScaleVector, progress);
                yield return null;
            }

            // 强制恢复原始缩放（避免影响后续收束动画）
            _rectTransform.localScale = originalScale;
            SoundManager.Instance.PlaySound("34");

            // 动画完成，触发回调（通知外部该格子动画结束）
            onComplete?.Invoke();
        }
        private const float WaitTime_Col1 = 0.5f;    // colIndex=1 时的等待时间（首项）
        private const float WaitTime_Col4 = 0.125f;   // colIndex=4 时的等待时间（末项）
        private const int TotalColCount = 4;         // 总列数（1-4）
        public IEnumerator ExchangeDoneAnimation2(Action onComplete)
        {

            //float delytime = (1f/colIndex ); 
            //yield return new WaitForSeconds(delytime);
            // ========== 核心修改：等差递减计算等待时间 ==========
            float delytime = 0f;
            // 1. 计算等差公差（负数，保证colIndex越大，等待时间越少）
            float waitTimeStep = (WaitTime_Col4 - WaitTime_Col1) / (TotalColCount - 1);
            // 2. 等差公式计算当前列的等待时间
            delytime = WaitTime_Col1 + (colIndex - 1) * waitTimeStep;
            // 3. 容错：防止colIndex超出1-4范围导致异常
            if (colIndex < 1 || colIndex > TotalColCount)
            {
                delytime = WaitTime_Col1; // 超出范围时用首项兜底
                Debug.LogWarning($"格子[{rowIndex},{colIndex}] 列索引超出1-{TotalColCount}范围，使用默认等待时间", gameObject);
            }

            // 等待指定时间（等差递减的等待时间）
            yield return new WaitForSeconds(delytime);


            // 空引用安全校验：确保RectTransform存在
            if (_rectTransform == null)
            {
                Debug.LogError($"格子[{rowIndex},{colIndex}] RectTransform为空，无法执行跳动动画", gameObject);

                onComplete?.Invoke(); // 即使出错，也触发回调，避免外部卡死
                yield break;
            }

            Vector3 originalScale = _rectTransform.localScale;
            Vector3 startScale = originalScale * initialScale;
            Vector3 peakScaleVector = originalScale * peakScale;
            Vector3 finalScaleVector = originalScale * finalScale;

            // 第一阶段：放大
            float elapsedTime = 0f;
            while (elapsedTime < scaleUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / scaleUpDuration);
                _rectTransform.localScale = Vector3.Lerp(startScale, peakScaleVector, progress);
                yield return null;
            }

            // 第二阶段：缩小
            elapsedTime = 0f;
            while (elapsedTime < scaleDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / scaleDownDuration);
                _rectTransform.localScale = Vector3.Lerp(peakScaleVector, finalScaleVector, progress);
                yield return null;
            }

            // 强制恢复原始缩放（避免影响后续收束动画）
            _rectTransform.localScale = originalScale;
            SoundManager.Instance.PlaySound("34");

            // 动画完成，触发回调（通知外部该格子动画结束）
            onComplete?.Invoke();
        }
        #endregion

        #region 补充生成特效
        public void PlayEffect()
        {
            if (EffectManager.Instance == null)
            {
                return;
            }

            RectTransform uiRect = GetComponent<RectTransform>();
            EffectManager.Instance.CreateUIEffectForCameraSimple("EmojiStar", uiRect);
            
        }
        public void PlayEffect2()
        {
            if (EffectManager.Instance == null)
            {
                return;
            }

            RectTransform uiRect = GetComponent<RectTransform>();
            EffectManager.Instance.CreateUIEffectForCameraSimple("EmojiStar2", uiRect);

        }
        #endregion

        #region
        /// <summary>
        /// 物品掉落+回弹核心动画
        /// </summary>
        /// <param name="item">要动画的物品</param>
        /// <param name="onComplete">动画完成回调</param>
        public IEnumerator ItemDropBounceAnimation( Action onComplete = null)
        {
            
            ItemControl item = GetComponentInChildren<ItemControl>();
            item.Changealpha(0);
            // 安全校验
            if (item == null || item._rectTransform == null || _rectTransform == null)
            {
                onComplete?.Invoke();
                yield break;
                
            }
            // 加动画锁，避免和其他动画冲突
            ItemGenerate.Instance.LockAnimation();

            // 行列延迟：让掉落更有层次感（行列越大，掉落越晚）
            float delay = (rowIndex + colIndex) * dropDelayFactor;
            yield return new WaitForSeconds(delay);

            // 目标位置（格子内物品的最终位置）
            Vector2 targetPos = item._originalAnchoredPos + ItemOffset;
            // 初始位置（目标位置上方dropHeight处）
            Vector2 startPos = new Vector2(targetPos.x, targetPos.y + dropHeight);

            // ========== 阶段1：从上往下掉落（模拟重力） ==========
            item._rectTransform.anchoredPosition = startPos;
            float elapsed = 0f;
            while (elapsed < dropDuration && ItemGenerate.Instance.IsAnimating)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / dropDuration);
                // 缓出曲线：模拟重力加速掉落
                item.Changealpha(progress);
                float easeProgress = Mathf.SmoothStep(0f, 1f, progress);
                item._rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeProgress);
                yield return null;
            }
            // 强制校正到目标位置（避免动画误差）
            item._rectTransform.anchoredPosition = targetPos;

            // ========== 阶段2：轻微回弹（落地弹一下） ==========
            Vector2 bouncePos = new Vector2(targetPos.x, targetPos.y + bounceAmplitude);
            elapsed = 0f;
            while (elapsed < bounceDuration && ItemGenerate.Instance.IsAnimating)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / bounceDuration);
                // 正弦曲线：先弹起再归位
                float bounceProgress = Mathf.Sin(progress * Mathf.PI);
                item._rectTransform.anchoredPosition = Vector2.Lerp(targetPos, bouncePos, bounceProgress);
                yield return null;
            }
            // 最终归位
            item._rectTransform.anchoredPosition = targetPos;

            // 释放动画锁+回调
            ItemGenerate.Instance.UnlockAnimation();
            onComplete?.Invoke();
        }

        public IEnumerator ItemDropBounceAnimationSelf(Action onComplete = null)
        {
            // ========== 核心替换：操作自身而非子物体ItemControl ==========
            RectTransform selfRect = GetComponent<RectTransform>();
            CanvasGroup selfCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            //// 初始透明度置0（复用原逻辑）
            //selfCanvasGroup.alpha = 0;

            // 安全校验（复用原逻辑，对象换自身）
            if (selfRect == null || _rectTransform == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            // 加动画锁（复用原逻辑）
            ItemGenerate.Instance.LockAnimation();

            // 行列延迟（复用原参数和逻辑）
            float delay = (rowIndex + colIndex) * dropDelayFactor;
            yield return new WaitForSeconds(delay);

            // 位置计算（复用原参数和逻辑）
            Vector2 targetPos = selfRect.anchoredPosition + ItemOffset; // 原item._originalAnchoredPos替换为自身初始锚点位置
            Vector2 startPos = new Vector2(targetPos.x, targetPos.y + dropHeight);

            // ========== 阶段1：从上往下掉落（模拟重力）（完全复用原动画逻辑） ==========
            selfRect.anchoredPosition = startPos;
            float elapsed = 0f;
            while (elapsed < dropDuration && ItemGenerate.Instance.IsAnimating)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / dropDuration);

                //// 透明度渐变（复用原进度）
                //selfCanvasGroup.alpha = progress;

                // 缓出曲线模拟重力（复用原曲线）
                float easeProgress = Mathf.SmoothStep(0f, 1f, progress);
                selfRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeProgress);
                yield return null;
            }
            // 强制校正位置（复用原逻辑）
            selfRect.anchoredPosition = targetPos;

            // ========== 阶段2：轻微回弹（落地弹一下）（完全复用原动画逻辑） ==========
            Vector2 bouncePos = new Vector2(targetPos.x, targetPos.y + bounceAmplitude);
            elapsed = 0f;
            while (elapsed < bounceDuration && ItemGenerate.Instance.IsAnimating)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / bounceDuration);

                // 正弦曲线回弹（复用原曲线）
                float bounceProgress = Mathf.Sin(progress * Mathf.PI);
                selfRect.anchoredPosition = Vector2.Lerp(targetPos, bouncePos, bounceProgress);
                yield return null;
            }
            // 最终归位（复用原逻辑）
            selfRect.anchoredPosition = targetPos;

            // 释放动画锁+回调（复用原逻辑）
            ItemGenerate.Instance.UnlockAnimation();
            onComplete?.Invoke();
        }
        #endregion
    }
}