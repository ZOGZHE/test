using EPOOutline;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace WoolyPath
{
    // 羊的控制器类，负责处理羊的点击交互、移动逻辑、羊毛分离、视觉表现及状态管理
    public class SheepController : MonoBehaviour
    {
        [Header("羊的设置")]
        private bool useLeftExit;
        // 羊的数据配置（存储颜色、网格位置、激活状态等）
        [SerializeField] public SheepData sheepData;
        // 羊毛预制体，用于生成羊的羊毛
        private GameObject woolPrefab;
        // 羊的模型组件（用于控制剪毛动画、羊毛颜色）
        [SerializeField] private SheepModel sheepModel;
        // 存储羊身体（代替直接操作预制体）
        private GameObject _SheepBodyInstance;
        // 存储黑羊遮罩羊
        private GameObject _blackMaskInstance;
        // 羊的视觉根节点（用于点击缩放动画）
        public Transform visualRoot { get; private set; }
        // 存储实例化的羊模型
        [HideInInspector] public GameObject _sheepModelInstance;
        private int randomIndex;


        [Header("移动设置")]
        // 羊的移动速度
        [SerializeField] private float moveSpeed = 5f;

        // 中间位置的Y轴坐标（PathConfiguration为空时的默认值）
        [SerializeField] private float middlePositionY = 2f;
        // 移动动画曲线（控制移动平滑度，如缓入缓出）
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("退出设置")]
        // 侧向退出的距离（PathConfiguration为空时的默认值）
        [SerializeField] private float sidewardExitDistance = 5f;
        // 羊退出阶段的移动速度
        [SerializeField] private float exitSpeed = 3f;
        // 走路动画的播放速度（控制退出时的颠簸频率）
        [SerializeField] private float walkAnimationSpeed = 1f;

        [Header("轮廓设置")]
        // 存储_sheepModelInstance上的Outlinable组件
        public Outlinable _modelOutlinable;
        public Outlinable _MaskmodelOutlinable;

        [Header("视觉设置")]
        // 点击羊时播放的粒子效果
        [SerializeField] private ParticleSystem clickEffect;

        [Header("音频")]
        // 点击羊时播放的音效名称
        [SerializeField] private string clickSoundName = "sheep_click";

        // 羊的初始位置（Awake时记录）
        private Vector3 originalPosition;
        // 中间位置（羊毛分离的关键节点）
        private Vector3 middlePosition;
        // 最终退出位置（羊移动的终点）
        private Vector3 exitPosition;
        // 标记羊是否正在移动中
        public bool _isMoving = false;
        // 标记羊是否已被点击过
        [SerializeField] private bool hasBeenClicked = false;
        // 标记羊是否已完成剃毛
        private bool hasSheared = false;
        public bool IsClickableByLogic = false;
        public bool IsClickablebyMode = false;
        public bool ClickableAtALL = false;

        // 组件唤醒时调用（初始化基础数据和路径位置）
        private void Awake()
        {
            randomIndex = UnityEngine.Random.Range(0, 5); // 随机索引初始化
            originalPosition = transform.position; // 记录羊的初始位置
            UpdatePathPositions(); // 更新中间位置和退出位置（优先从配置获取，无则用默认）
        }

        private void Start()
        {
            InitializeModel(); // 初始化羊模型
        }

        #region 初始化

        private void InitializeModel()//初始化普通羊
        {
            _sheepModelInstance = sheepModel.InstantiateSheepModel(sheepData.color); // 实例化有毛羊模型
            woolPrefab = sheepModel.WoolModelPrefab[sheepData.color]; // 获取对应颜色的羊毛预制体

            if (sheepModel.NoWoolSheepModelPrefab.TryGetValue(sheepData.color, out GameObject noWoolPrefab))
            {
                // 字典中存在对应预制体，正常实例化
                _SheepBodyInstance= Instantiate(noWoolPrefab, transform.position, transform.rotation, transform);//实例化无毛羊模型
            }
            _blackMaskInstance = sheepModel.InstantiateSheepModel(WoolColor.Black); // 实例化新模型
            _blackMaskInstance.SetActive(false);
            UpdateSheepModelData(); // 更新羊模型数据
            if (_sheepModelInstance != null)
            {
                _modelOutlinable = _sheepModelInstance.GetComponent<Outlinable>(); // 获取轮廓组件
                if (_modelOutlinable == null) _modelOutlinable = _sheepModelInstance.AddComponent<Outlinable>(); // 无则添加
                _modelOutlinable.enabled = false;
                IsClickablebyMode = false;
            }
            if (_blackMaskInstance != null)
            {
                _MaskmodelOutlinable = _blackMaskInstance.GetComponent<Outlinable>(); // 获取轮廓组件
                if (_MaskmodelOutlinable == null) _MaskmodelOutlinable = _blackMaskInstance.AddComponent<Outlinable>(); // 无则添加
                _MaskmodelOutlinable.enabled = false;
                IsClickablebyMode = false;
            }
            else Debug.LogWarning($"[SheepController] {name}: 羊模型实例生成失败，无法添加Outlinable！");
        }
        //外部调用初始化
        public void Initialize(SheepData data)
        {
            sheepData = data; // 赋值羊数据
            if (!sheepData.isActive) { DeactivateSheep(); Debug.LogError(" DeactivateSheep();"); } // 未激活则直接停用
        }
        #endregion

        #region 点击条件
        // 更新视觉状态（含开局保护判断，控制轮廓显示）
        public void UpdateVisualStateWithProtection()
        {
            UpdateClickableAtALL();
            bool actuallyClickable = ClickableAtALL;
            if (_modelOutlinable != null)
            {
                _modelOutlinable.enabled = actuallyClickable; 
            }
            if (_MaskmodelOutlinable != null)
            {
                _MaskmodelOutlinable.enabled = actuallyClickable;
            }
        }


        // 设置羊的可点击状态 
        //条件上来说可点击
        public void SetClickableByLogic(bool clickable)
        {
            IsClickableByLogic = clickable;
        }
        //模式上来说可点击
        public void SetClickablebyMode(bool clickable)
        {
            IsClickablebyMode = clickable;
        }

        //更新总的是否可以点击
        public void UpdateClickableAtALL()
        {
            ClickableAtALL = CanBeClicked();
        }

        // 检查羊是否满足被点击的条件（内部调用，过滤无效点击）
        public bool CanBeClicked()
        {
            return sheepData != null
                && sheepData.isActive
                && !_isMoving 
                && !hasBeenClicked
                && IsClickableByLogic
                && GameManager.Instance != null
                 || IsClickablebyMode;
        }

        // 羊被点击时的回调方法
        public void OnClicked()
        {
            if (!CanBeClicked()) return; // 不满足条件则返回
            hasBeenClicked = true; // 标记已点击
            GameEvents.TriggerSheepClicked(this, transform.position); // 触发点击事件
            PlayClickEffect(); // 播放点击视觉效果
            PlayClickSound(); // 播放点击音效
            StartCoroutine(MoveSequence()); // 启动完整移动序列

        }
        public void OutClicked()
        {
           
            StartCoroutine(MoveSequence()); // 启动完整移动序列
            
        }

        #endregion

        #region 转换羊的颜色
        // 添加黑羊遮罩
        public void ApplyBlackMask()
        {
                sheepData.isblackMasked = true;
            if (_sheepModelInstance != null)
            {
                _sheepModelInstance.SetActive(false);
            }
              
                _blackMaskInstance.SetActive(sheepData.isblackMasked);
        }
        // 移除黑羊遮罩
        public void RemoveBlackMask()
        {
                sheepData.isblackMasked = false;
            if (_sheepModelInstance != null)
            {
                _sheepModelInstance.SetActive(true);
            }
            _blackMaskInstance.SetActive(sheepData.isblackMasked);  
        }
        // 展示黑羊真正颜色
        public void RemoveBlackMaskToOtherSheep()
        {
            RemoveBlackMask();
        }

        // 切换羊的颜色
        public void SwitchToOtherSheep(SheepData TargetSheepData)
        {
            sheepData.color = TargetSheepData.color; // 更新羊数据颜色

            // 销毁旧有毛模型 + 置null
            if (_sheepModelInstance != null)
            {
                sheepModel.DestroySheepModel(_sheepModelInstance);
                _sheepModelInstance = null;
            }

            // 销毁旧无毛模型 + 置null
            if (_SheepBodyInstance != null)
            {
                sheepModel.DestroySheepModel(_SheepBodyInstance);
                _SheepBodyInstance = null;
            }

            woolPrefab = sheepModel.WoolModelPrefab[sheepData.color]; // 更新羊毛预制体
            _sheepModelInstance = sheepModel.InstantiateSheepModel(sheepData.color); // 实例化新有毛模型  

            // 实例化新无毛模型（确保赋值给 _SheepBodyInstance）
            if (sheepModel.NoWoolSheepModelPrefab.TryGetValue(TargetSheepData.color, out GameObject noWoolPrefab))
            {
                _SheepBodyInstance = Instantiate(noWoolPrefab, transform.position, transform.rotation, transform);
            }
            else
            {
                Debug.LogWarning($"[SheepController] 未找到颜色{sheepData.color}的无毛羊预制体！");
                _SheepBodyInstance = null;
            }

            UpdateSheepModelData(); // 更新模型数据

            // 处理黑羊遮罩
            if (TargetSheepData.isblackMasked)
            {
                ApplyBlackMask();
            }
            else
            {
                RemoveBlackMask();
            }

            // 处理轮廓组件
            if (_sheepModelInstance != null)
            {
                _modelOutlinable = _sheepModelInstance.GetComponent<Outlinable>();
                if (_modelOutlinable == null)
                {
                    _modelOutlinable = _sheepModelInstance.AddComponent<Outlinable>();
                }
                _modelOutlinable.enabled = CanBeClicked();
            }
            else
            {
                Debug.LogWarning($"[SheepController] 切换颜色时，羊模型实例生成失败！");
                _modelOutlinable = null;
            }

            hasSheared = false; // 重置剪毛标志
            if (sheepModel != null)
            {
                sheepModel.SetWoollyState(true); // 设置新模型为有毛状态
            }
            SheepSpawner.instance.UpdateAllColumnsClickable();
        }
        // 更新羊模型数据（同步模型引用至SheepModel）
        public void UpdateSheepModelData()
        {
            sheepModel.woollyModel = _sheepModelInstance; // 同步羊模型引用
            sheepModel.woolModel = woolPrefab; // 同步羊毛模型引用
            sheepModel.shornModel = _SheepBodyInstance;//同步无毛羊
        }
        #endregion

        #region 特效音乐
        private void PlayClickEffect()
        {
            if (clickEffect != null) clickEffect.Play(); // 播放粒子效果
            if (visualRoot != null) StartCoroutine(ClickScaleAnimation()); // 播放缩放动画
        }

        // 播放点击羊时的音效（通过音频管理器）
        private void PlayClickSound()
        {
            if(AudioManager.Instance!=null)
            {
 // 创建包含两个音频名称的数组
            string[] soundNames = { "1", "2" };

            // 随机生成0或1的索引（两个选项）
            int randomIndex = UnityEngine.Random.Range(0, soundNames.Length);

            // 只播放随机选中的那一个音频
            AudioManager.Instance.PlayMusic(soundNames[randomIndex]);
            }
           
        }

        // 点击时的缩放动画协程（先放大再缩小，模拟点击反馈）
        private IEnumerator ClickScaleAnimation()
        {
            Vector3 originalScale = visualRoot.localScale; // 记录原始缩放
            Vector3 clickScale = originalScale * 1.5f; // 点击后放大比例
            float duration = 0.1f; // 单阶段动画时长
            float timer = 0f;

            // 第一阶段：放大
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                visualRoot.localScale = Vector3.Lerp(originalScale, clickScale, t);
                yield return null;
            }

            // 第二阶段：缩小回原尺寸
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                visualRoot.localScale = Vector3.Lerp(clickScale, originalScale, t);
                yield return null;
            }

            visualRoot.localScale = originalScale; // 确保最终缩放正确
        }
        #endregion

        #region 三个阶段
        // 羊的完整移动序列协程（分三阶段：移动到中间点→羊毛分离→侧向退出）
        private IEnumerator MoveSequence()
        {
            _isMoving = true; // 标记开始移动
            GameEvents.TriggerSheepMoveStarted(this); // 触发移动开始事件

            //Debug.Log($"[SheepController] {name}: 开始三阶段移动序列");

            yield return StartCoroutine(MoveToPosition(middlePosition)); // 阶段1：移动到中间点
            yield return StartCoroutine(WoolSeparationPhase()); // 阶段2：羊毛分离
            yield return StartCoroutine(SidewardExitPhase()); // 阶段3：侧向退出

            GameEvents.TriggerSheepMoveCompleted(this); // 触发移动完成事件
            DeactivateSheep(); // 移动完成后停用羊
        }

        // 移动到指定目标位置的协程（带动画曲线控制的平滑移动）
        private IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position; // 记录起始位置
            float journey = 0f; // 行程计时器
            float journeyLength = Vector3.Distance(startPosition, targetPosition); // 移动距离
            float journeyTime = journeyLength / moveSpeed; // 预估移动时间

            // 按动画曲线平滑移动
            while (journey <= journeyTime)
            {
                journey += Time.deltaTime;
                float fractionOfJourney = journey / journeyTime;
                float curveValue = moveCurve.Evaluate(fractionOfJourney);
                transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                yield return null;
            }

            transform.position = targetPosition; // 确保最终位置准确
        }

        // 羊毛分离阶段协程（生成羊毛、播放剪毛效果和动画）
        private IEnumerator WoolSeparationPhase()
        {
            // 播放剪毛视觉效果（位置在羊模型上方0.5单位）
            Vector3 shearingEffectPosition = transform.position + Vector3.up * 0.5f;
            if (EffectsManager.Instance != null)
            {
                EffectsManager.Instance.PlayShearingEffect(shearingEffectPosition);
            }
            // 生成并发射羊毛资源
            CreateAndLaunchWool();

            // 处理剪毛动画逻辑
            bool isShearingAnimationComplete = false;
            if (sheepModel != null)
            {
                // 启动剪毛动画，设置动画完成回调（标记状态）
                sheepModel.StartShearingAnimation(() =>
                {
                    isShearingAnimationComplete = true;
                    hasSheared = true;
                });

                // 等待剪毛动画播放完成
                while (!isShearingAnimationComplete)
                {
                    yield return null;
                }
            }
            // 若没有模型，直接标记剪毛完成
            else
            {
                hasSheared = true;
            }
        }

        // 侧向退出阶段协程（带行走颠簸效果的移动）
        private IEnumerator SidewardExitPhase()
        {
            Vector3 startPosition = transform.position; // 起始位置（中间点）
            Quaternion StartRotation = transform.rotation; // 起始旋转
            Quaternion FinalRotation = Quaternion.Euler(0, useLeftExit ? -90 : 90, 0); // 目标旋转（根据退出方向）
            float journey = 0f; // 行程计时器
            float journeyLength = Vector3.Distance(startPosition, exitPosition); // 退出距离
            float journeyTime = journeyLength / exitSpeed; // 预估退出时间
            float walkCycleTime = 0f; // 走路周期计时器（控制颠簸频率）

            // 带颠簸效果的侧向移动
            while (journey <= journeyTime)
            {
                journey += Time.deltaTime;
                walkCycleTime += Time.deltaTime * walkAnimationSpeed; // 更新走路周期

                float fractionOfJourney = journey / journeyTime;
                Vector3 currentPos = Vector3.Lerp(startPosition, exitPosition, fractionOfJourney); // 基础侧向移动
                Quaternion currentRot = Quaternion.Lerp(StartRotation, FinalRotation, 3 * fractionOfJourney); // 平滑旋转

                float walkBob = Mathf.Sin(walkCycleTime * 6f) * 0.1f; // 颠簸Y轴偏移（正弦函数模拟）
                currentPos.y += walkBob; // 应用颠簸效果

                transform.position = currentPos;
                transform.rotation = currentRot;
                yield return null;
            }

            transform.position = exitPosition; // 确保最终位置准确
            //Debug.Log($"[SheepController] {name}: 侧向退出完成");
        }
        #endregion

        #region 发射羊毛与停用逻辑与路径更新
        // 创建羊毛预制体并发射到传送带（内部调用，羊毛分离阶段执行）
        private void CreateAndLaunchWool()
        {
            Vector3 woolSpawnPosition = GetWoolSpawnPosition(); // 获取羊毛生成位置
            GameObject woolObject = Instantiate(woolPrefab, woolSpawnPosition, Quaternion.identity); // 实例化羊毛
            WoolObject wool = woolObject.GetComponent<WoolObject>(); // 获取羊毛组件

            if (wool != null)
            {
                //Vector3 Target = GetBeltEntryPoint(wool);
                Vector3 Target = ConveyorBelt.Instance.GetEntryPoint();//发射到传送带上
                // 播放羊毛发射效果

                wool.OutLaunch(wool, woolSpawnPosition, Target);


            }
        }

        // 获取传送带入口点（优先从收集器获取，无则从传送带实例获取，兜底返回原点）
        private Vector3 GetBeltEntryPoint(WoolObject wool)
        {
            CollectorPlate[] collectorPlates = FindObjectsOfType<CollectorPlate>();
            // 遍历收集器，找到可收集当前羊毛的目标
            foreach (var collector in collectorPlates)
            {
                if (collector != null && collector.WoolCanBeCollect(wool)) return collector.GetCollectionPoint();
            }

            if (ConveyorBelt.Instance != null) return ConveyorBelt.Instance.GetEntryPoint(); // 从传送带获取入口点
            return Vector3.zero; // 兜底返回原点
        }

        // 获取羊毛生成位置（羊位置向上偏移0.5单位，避免与模型重叠）
        private Vector3 GetWoolSpawnPosition()
        {
            return transform.position + Vector3.up * 0.5f;
        }

        // 停用羊的逻辑（设置状态、触发事件、禁用物体）
        private void DeactivateSheep()
        {
            sheepData.isActive = false; // 标记羊为未激活
            GameEvents.TriggerSheepDeactivated(this); // 触发羊停用事件

            // 销毁有毛模型 + 置null
            if (_sheepModelInstance != null)
            {
                sheepModel?.DestroySheepModel(_sheepModelInstance);
                _sheepModelInstance = null;
            }

            // 销毁无毛模型 + 置null
            if (_SheepBodyInstance != null)
            {
                sheepModel?.DestroySheepModel(_SheepBodyInstance);
                _SheepBodyInstance = null;
            }

            // 销毁黑羊遮罩 + 置null
            if (_blackMaskInstance != null)
            {
                sheepModel?.DestroySheepModel(_blackMaskInstance);
                _blackMaskInstance = null;
            }

            gameObject.SetActive(false); // 禁用羊对象
        }
        // 更新羊的移动路径关键位置（中间点、退出点）
        private void UpdatePathPositions()
        {
            if (PathConfiguration.Instance != null)
            {
                middlePosition = PathConfiguration.Instance.GetWoolSeparationPoint(); // 从配置获取羊毛分离点
                useLeftExit = UnityEngine.Random.value > 0.5f; // 随机决定退出方向
                exitPosition = PathConfiguration.Instance.GetExitPoint(useLeftExit); // 从配置获取退出点
            }
            else
            {
                middlePosition = new Vector3(originalPosition.x, middlePositionY, originalPosition.z); // 默认中间点
                float exitDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f; // 随机退出方向
                exitPosition = new Vector3(originalPosition.x + exitDirection * sidewardExitDistance, originalPosition.y, originalPosition.z); // 默认退出点
                Debug.Log($"[SheepController] {name}: 使用默认计算 - 中间点:{middlePosition}, 退出点:{exitPosition}");
            }
        }
        #endregion

        #region Public Accessors（公共访问器：外部类获取/设置羊的状态和数据）
        // 获取羊的羊毛颜色（为空时默认返回绿色）
        public WoolColor GetColor() => sheepData?.color ?? WoolColor.Green;

        // 获取羊的网格位置（为空时默认返回原点）
        public Vector2Int GetGridPosition() => sheepData?.gridPosition ?? Vector2Int.zero;

        // 检查羊是否处于激活状态
        public bool IsActive() => sheepData?.isActive ?? false;

        // 检查羊是否已被点击过
        public bool HasBeenClicked() => hasBeenClicked;


        // 设置羊的网格位置
        public void SetGridPosition(Vector2Int position)
        {
            if (sheepData != null) sheepData.gridPosition = position;
        }
        #endregion

        #region Gizmos
        // Unity编辑器下的Gizmos绘制（辅助开发，显示路径关键位置）
        private void OnDrawGizmosSelected()
        {
            Vector3 currentPos = transform.position; // 当前羊位置

            // 绘制当前位置→中间位置（绿色线段+球体）
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawLine(currentPos, middlePosition);
            Gizmos.DrawWireSphere(middlePosition, 0.3f);

            // 绘制中间位置→退出位置（黄色线段）
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawLine(middlePosition, exitPosition);

            // 绘制退出位置（红色球体）
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawWireSphere(exitPosition, 0.2f);


            // 编辑器中添加文字标签（方便识别）
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(middlePosition + Vector3.up * 0.5f, "羊毛分离点");
            UnityEditor.Handles.Label(exitPosition + Vector3.up * 0.5f, "退出点");
            #endif

        }
        #endregion

    }
}