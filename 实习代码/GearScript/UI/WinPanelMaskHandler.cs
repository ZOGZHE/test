using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperGear
{
    /// <summary>
    /// WinPanel黑色遮罩层处理器
    /// 按住黑色遮罩层时,临时将WinPanel设置为透明以预览过关成果
    /// </summary>
    public class WinPanelMaskHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("WinPanel引用")]
        [Tooltip("需要临时透明化的WinPanel主内容")]
        [SerializeField] private CanvasGroup winPanelContent;

        [Header("透明度设置")]
        [Tooltip("按住时的透明度 (0 = 完全透明, 1 = 完全不透明)")]
        [SerializeField] private float pressedAlpha = 0f;

        [Tooltip("松开时的透明度 (正常状态)")]
        [SerializeField] private float normalAlpha = 1f;

        [Header("平滑过渡设置")]
        [Tooltip("是否启用平滑过渡")]
        [SerializeField] private bool enableSmoothTransition = true;

        [Tooltip("透明度过渡速度")]
        [SerializeField] private float transitionSpeed = 10f;

        [Header("提示文字处理")]
        [Tooltip("点击提示文字时是否也能触发透明(如果为false,提示文字需要关闭Raycast Target)")]
        [SerializeField] private bool allowClickOnHintText = true;

        // 目标透明度
        private float targetAlpha;

        private void Awake()
        {
            // 如果未手动赋值,尝试自动查找WinPanel的CanvasGroup
            if (winPanelContent == null)
            {
                // 假设WinPanel的主内容是遮罩层的兄弟节点或父节点的其他子节点
                Transform parent = transform.parent;
                if (parent != null)
                {
                    winPanelContent = parent.GetComponentInChildren<CanvasGroup>();
                }
            }

            // 初始化为正常透明度
            targetAlpha = normalAlpha;
        }

        private void Update()
        {
            // 如果启用平滑过渡,逐渐调整透明度到目标值
            if (enableSmoothTransition && winPanelContent != null)
            {
                winPanelContent.alpha = Mathf.Lerp(
                    winPanelContent.alpha,
                    targetAlpha,
                    Time.unscaledDeltaTime * transitionSpeed
                );
            }
        }

        /// <summary>
        /// 按下时触发 - 设置为透明
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (winPanelContent == null)
            {
                Debug.LogWarning("WinPanelMaskHandler: winPanelContent未设置!");
                return;
            }

            Debug.Log("按住黑色遮罩层 - 临时透明化WinPanel");
            targetAlpha = pressedAlpha;

            // 如果不启用平滑过渡,立即设置透明度
            if (!enableSmoothTransition)
            {
                winPanelContent.alpha = pressedAlpha;
            }
        }

        /// <summary>
        /// 松开时触发 - 恢复不透明
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (winPanelContent == null)
            {
                Debug.LogWarning("WinPanelMaskHandler: winPanelContent未设置!");
                return;
            }

            Debug.Log("松开黑色遮罩层 - 恢复WinPanel不透明");
            targetAlpha = normalAlpha;

            // 如果不启用平滑过渡,立即设置透明度
            if (!enableSmoothTransition)
            {
                winPanelContent.alpha = normalAlpha;
            }
        }

        /// <summary>
        /// 手动设置透明度 (外部调用接口)
        /// </summary>
        public void SetTransparency(float alpha)
        {
            targetAlpha = Mathf.Clamp01(alpha);
            if (!enableSmoothTransition && winPanelContent != null)
            {
                winPanelContent.alpha = targetAlpha;
            }
        }

        /// <summary>
        /// 重置为正常状态
        /// </summary>
        public void ResetToNormal()
        {
            targetAlpha = normalAlpha;
            if (winPanelContent != null)
            {
                winPanelContent.alpha = normalAlpha;
            }
        }
    }
}
