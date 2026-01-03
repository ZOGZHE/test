using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    // 羊的模型管理器，处理有毛 / 无毛状态切换，负责管理羊毛的消失动画和模型切换
    public class SheepModel : MonoBehaviour
    {
        [Header("SheepController引用")]
        [SerializeField] private SheepController _sheepController;
        [Header("模型引用")]
        [HideInInspector] public GameObject woollyModel; // 有毛的羊模型
        [HideInInspector] public GameObject shornModel; // 剃毛后的羊模型
        [HideInInspector] public GameObject woolModel; // 羊毛模型
        // public GameObject BlackSheep; // 羊毛模型
        [SerializeField]
        private Transform sheepModelParent;
        
        [Header("羊毛消失效果")]
        [SerializeField] private float woolFadeTime = 0.1f; // 羊毛消失动画持续时间
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // 消失动画曲线

        [Header("羊模型预制体")]
        [SerializeField] public GameObject[] _SheepModelPrefab;
        public Dictionary<WoolColor, GameObject> SheepModelPrefab = new Dictionary<WoolColor, GameObject>();
        [Header("无毛羊模型预制体")]
        [SerializeField] public GameObject[] _NoWoolSheepModelPrefab;
        public Dictionary<WoolColor, GameObject> NoWoolSheepModelPrefab = new Dictionary<WoolColor, GameObject>();
        [Header("羊毛模型预制体")]
        [SerializeField] public GameObject[] _WoolModelPrefab;
        public Dictionary<WoolColor, GameObject> WoolModelPrefab = new Dictionary<WoolColor, GameObject>();

        public Dictionary<int, WoolColor> ColorPrefabIndex = new Dictionary<int, WoolColor>();
     
        // 当前是否为有毛状态
        public bool IsWoolly { get; private set; } = true;
        // 是否正在切换状态
        public bool IsSwitching = false;

        // Awake 在对象初始化时调用
        private void Awake()
        {
            SetWoollyState(true); // 初始设置为有毛状态
            AddMySheepModelPrefabDictionary();
            AddMyWoolModelPrefabDictionary();
            AddMyNoWoolSheepModelPrefabDictionary();
        }
        // Start 在第一次帧更新之前调用
        private void Start()
        {
        }

        private void AddMySheepModelPrefabDictionary()
        {
            SheepModelPrefab.Add(WoolColor.Pink, _SheepModelPrefab[0]);
            SheepModelPrefab.Add(WoolColor.Yellow, _SheepModelPrefab[1]);
            SheepModelPrefab.Add(WoolColor.Blue, _SheepModelPrefab[2]);
            SheepModelPrefab.Add(WoolColor.Purple, _SheepModelPrefab[3]);
            SheepModelPrefab.Add(WoolColor.Green, _SheepModelPrefab[4]);
            SheepModelPrefab.Add(WoolColor.Orange, _SheepModelPrefab[5]);
            SheepModelPrefab.Add(WoolColor.Black, _SheepModelPrefab[6]);

            ColorPrefabIndex.Add(0, WoolColor.Pink);
            ColorPrefabIndex.Add(1, WoolColor.Yellow);
            ColorPrefabIndex.Add(2, WoolColor.Blue);
            ColorPrefabIndex.Add(3, WoolColor.Purple);
            ColorPrefabIndex.Add(4, WoolColor.Green);
            ColorPrefabIndex.Add(5, WoolColor.Orange);
            ColorPrefabIndex.Add(6, WoolColor.Black);
        }
        private void AddMyWoolModelPrefabDictionary()
        {
            WoolModelPrefab.Add(WoolColor.Pink, _WoolModelPrefab[0]);
            WoolModelPrefab.Add(WoolColor.Yellow, _WoolModelPrefab[1]);
            WoolModelPrefab.Add(WoolColor.Blue, _WoolModelPrefab[2]);
            WoolModelPrefab.Add(WoolColor.Purple, _WoolModelPrefab[3]);
            WoolModelPrefab.Add(WoolColor.Green, _WoolModelPrefab[4]);
            WoolModelPrefab.Add(WoolColor.Orange, _WoolModelPrefab[5]);
            WoolModelPrefab.Add(WoolColor.Black, _WoolModelPrefab[6]);
        }
        private void AddMyNoWoolSheepModelPrefabDictionary()
        {
            NoWoolSheepModelPrefab.Add(WoolColor.Pink, _NoWoolSheepModelPrefab[0]);
            NoWoolSheepModelPrefab.Add(WoolColor.Yellow, _NoWoolSheepModelPrefab[1]);
            NoWoolSheepModelPrefab.Add(WoolColor.Blue, _NoWoolSheepModelPrefab[2]);
            NoWoolSheepModelPrefab.Add(WoolColor.Purple, _NoWoolSheepModelPrefab[3]);
            NoWoolSheepModelPrefab.Add(WoolColor.Green, _NoWoolSheepModelPrefab[4]);
            NoWoolSheepModelPrefab.Add(WoolColor.Orange, _NoWoolSheepModelPrefab[5]);
            NoWoolSheepModelPrefab.Add(WoolColor.Black, _NoWoolSheepModelPrefab[6]);

        }

        // 实例化羊模型（根据羊毛颜色）

        public GameObject InstantiateSheepModel(WoolColor color, Transform parent = null)
        {

            // 处理其他颜色（从字典获取预制体）
            if (SheepModelPrefab.TryGetValue(color, out GameObject prefab))
            {
                Transform finalParent = sheepModelParent != null ? sheepModelParent : (parent != null ? parent : transform);
                GameObject sheepInstance = Instantiate(prefab, finalParent.position, finalParent.rotation, finalParent);
                sheepInstance.name = $"{color}Sheep_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                sheepInstance.SetActive(true);

                // 确保新模型处于有毛状态
                ResetToWoollyState();

                return sheepInstance;
            }
            else
            {
                string availableColors = string.Join(", ", SheepModelPrefab.Keys);
                Debug.LogError($"[InstantiateSheepModel1] 未找到 {color} 预制体！当前字典包含: {availableColors}");
                return null;
            }
        }
        // 重置为有毛状态（用于颜色切换后）
        public void ResetToWoollyState()
        {
            IsWoolly = true;
            IsSwitching = false;

            // 确保模型状态正确
            if (woollyModel != null)
                woollyModel.SetActive(true);
            if (shornModel != null)
                shornModel.SetActive(false);

            //Debug.Log($"[SheepModel] {name}: 重置为有毛状态");
        }

        // 销毁羊模型实例
        public void DestroySheepModel(GameObject sheepInstance)
        {
            if (sheepInstance != null)
            {
                //Debug.Log($"销毁羊模型: {sheepInstance.name}");
                Destroy(sheepInstance);
            }
            else
            {
                Debug.LogWarning("尝试销毁空的羊模型实例");
            }
        }


        //woolly: true 表示有毛，false 表示无毛
        public void SetWoollyState(bool woolly)
        {
            // 如果状态相同或正在切换中，则直接返回
            if (IsWoolly == woolly || IsSwitching) return;

            IsWoolly = woolly;

            // 激活 / 禁用相应的模型
            if (woollyModel != null)
                woollyModel.SetActive(woolly);

            if (shornModel != null)
                shornModel.SetActive(!woolly);

            Debug.Log($"[SheepModel] {name}: 切换到 {(woolly ? " 有毛 " : " 剃毛 ")} 状态");
        }

        // 开始剃毛动画（羊毛逐渐消失）
        //onComplete: 动画完成后的回调函数
        public void StartShearingAnimation(System.Action onComplete = null)
        {
            // 如果已经无毛或正在切换，直接调用完成回调
            if (!IsWoolly || IsSwitching)
            {
                onComplete?.Invoke();
                return;
            }
           StartCoroutine(ShearingAnimationCoroutine(onComplete));
        }

        // 剃毛动画协程（处理羊毛消失过程）
        private IEnumerator ShearingAnimationCoroutine(System.Action onComplete)
        {
            IsSwitching = true; // 标记为正在切换状态
            float elapsedTime = 0f; // 已过去的时间
            bool hasModelSwitched = false; // 是否已切换模型

            // 动画循环（持续到设定的动画时间）
            while (elapsedTime < woolFadeTime)
            {
                elapsedTime += Time.deltaTime; // 增加时间（基于帧间隔）
                float normalizedTime = elapsedTime / woolFadeTime; // 规范化时间 (0-1)
                float curveValue = fadeCurve.Evaluate(normalizedTime); // 根据曲线获取当前效果值

                if (!hasModelSwitched )
                {
                     SwitchToShornModel();
                   
                    hasModelSwitched = true;
                }

                yield return null; // 等待下一帧
            }

            // 确保动画完成后的最终状态
            CompleteShearingAnimation();

            IsSwitching = false; // 标记切换完成
            onComplete?.Invoke(); // 调用完成回调
        }


        // 切换到剃毛模型（激活剃毛模型）
        private void SwitchToShornModel()
        {
            if (shornModel != null)
            {
                shornModel.SetActive(true);
                //Debug.Log($"[SheepModel] {name}: 显示剃毛模型");
            }

        }
        // 完成剃毛动画，清理最终状态
        private void CompleteShearingAnimation()
        {
            // 销毁羊模型 + 置null
            if (_sheepController != null && _sheepController._sheepModelInstance != null)
            {
                DestroySheepModel(_sheepController._sheepModelInstance);
                _sheepController._sheepModelInstance = null;
            }
            else
            {
                Debug.LogWarning($"[SheepController] 羊模型实例为空，无法销毁");
            }

            IsWoolly = false; // 更新状态为无毛
        }
    }
}
