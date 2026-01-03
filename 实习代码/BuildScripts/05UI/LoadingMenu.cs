using ConnectMaster;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ConnectMaster
{

    public class LoadingMenu : MonoBehaviour
    {
        public static LoadingMenu Instance { get; private set; }

        [SerializeField] private Image loadingFrame;    // 加载条底框（背景容器，用于限制加载条的最大宽度）
        [SerializeField] private Image loadingFill;     // 加载条填充物（实际显示进度的部分）
        [SerializeField] float loadDuration = 1f;         // 模拟加载的总时长
        private float _maxFillWidth; // 加载条填充物的最大宽度（等于底框的宽度，用于计算进度对应的宽度）

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeLoadingBar();
        }
        public void StartLoading()
        {
            StartCoroutine(SimulateLoadingProcess());
        }


        // 初始化加载条参数：设置初始状态和基础配置
        private void InitializeLoadingBar()
        {
            if (loadingFrame == null || loadingFill == null)
            {
                Debug.LogError("请在Inspector中设置加载条的底框和填充物组件！");
                return; // 缺少必要组件，终止初始化
            }
            // 记录底框的宽度作为填充物的最大宽度（确保加载条不会超过底框范围）
            _maxFillWidth = 941f;
            // 初始化填充物宽度为0（加载开始时进度为0）
            SetFillAmount(0);
        }
        // 参数progress：进度值（范围0-1，0表示未开始，1表示完成）
        private void SetFillAmount(float progress)
        {
            // 限制进度值在0-1之间（防止因计算错误导致进度超过范围）
            progress = Mathf.Clamp01(progress);

            // 计算当前进度对应的填充物宽度（进度 * 最大宽度）
            float fillWidth = progress * _maxFillWidth;

            // 更新填充物的宽度（只修改水平方向，保持垂直方向大小不变）
            loadingFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth);

        }

        // 模拟加载过程：用协程模拟一个耗时的加载流程（用于测试或没有真实加载数据时）
        public IEnumerator SimulateLoadingProcess()
        {
            // 确保开始时进度为0
            SetFillAmount(0);
            float currentProgress = 0;       // 当前进度（0-1）

            float elapsedTime = 0;           // 已流逝的时间

            // 当已流逝时间小于总时长时，持续更新进度
            while (elapsedTime < loadDuration)
            {
                // 累加每帧流逝的时间（Time.deltaTime是上一帧到当前帧的时间间隔）
                elapsedTime += Time.deltaTime;
                // 计算当前进度（已用时间 / 总时长 = 0-1之间的比例）
                currentProgress = elapsedTime / loadDuration;

                // 使用SmoothStep让进度变化更平滑（模拟真实加载中速度有快有慢的效果）
                // SmoothStep会在0到1之间创建一个平滑的过渡曲线，而非线性变化
                currentProgress = Mathf.SmoothStep(0, 1, currentProgress);

                // 更新加载条UI
                SetFillAmount(currentProgress);

                // 暂停协程，等待下一帧再继续执行（确保UI能实时更新）
                yield return null;
            }

            // 循环结束后，确保进度强制设为100%（避免因计算误差导致未达100%）
            SetFillAmount(1);

            // 加载完成后等待0.2秒
            yield return new WaitForSeconds(0.2f);
            // 增加空值判断
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetLoadPanel(false);
                UIManager.Instance.SetHudPanel(true);
            }
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.StartCountdown(); // 初始化倒计时（从关卡数据读取时长）
                
            }
        }
    }
}