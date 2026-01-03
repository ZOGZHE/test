using SuperGear;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using EPOOutline;
using static UnityEngine.Rendering.DebugUI;

namespace SuperGear
{
    [System.Serializable]
    public class BlockControl : MonoBehaviour, InputManager.IInteractable
    {
        #region 积木基础设置
        [Header("积木设置")]
        [SerializeField] private bool lockOnPlace = true;
        [SerializeField] private float dragHeight = 0.5f;
        // 向前偏移量（解决手指遮挡，正数为向前，负数为向后）
        [SerializeField] public float forwardOffset = 0.3f;
        [SerializeField] private float slotPointsAdsorption = 1f;//最小吸附距离
        [SerializeField] private float CloseGroundValue = 1f;//离地距离
        [Tooltip("复位动画时长（秒）")]
        [SerializeField] private float resetAnimDuration = 0.3f;
        [Tooltip("复位动画缓动曲线")]
        [SerializeField] private AnimationCurve resetEaseCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private bool isRotating = false;
        private float rotateTime = 0.15f; // 旋转总时长（可调整）
        #endregion

        #region 轮廓
        [Header("轮廓")]
        private Outlinable outlinable; // 存储Outlinable组件实例
        [SerializeField] private Color canplacedOutlineColor = Color.green; // 放置后轮廓色
        [SerializeField] private Color defaultOutlineColor ; // 默认轮廓色
        #endregion

        #region 积木数据与状态
        [Header("积木数据")]
        [SerializeField] public BlockData _blockData;
        private Vector3 _targetPlacementPos; // 积木最终放置的目标位置
        public bool isPlaced = false; // 是否已放置到插槽
        public bool isResetting = false; // 防止复位动画重复触发
        [HideInInspector] public Vector3 LocalSpawnPosition; // 存储生成时相对父节点的本地坐标
        List<ReceivingBinControl> matchedBins = new List<ReceivingBinControl>();
        private Vector3 dragOffset;
        private Vector3 _slotLocalCenter;          // 积木插槽本地中心点（局部坐标）
        private Vector3 _slotLocalCenterWorld;     // 积木插槽中心点（世界坐标，用于绘制）
        private Vector3 _binWorldCenter;           // 接收柱群世界中心点（世界坐标）
        [Header("齿轮检测配置（解决传导延迟）")]
        [SerializeField] private float checkInterval = 0.2f;   // 检测间隔（多次检测的间隔时间）
        [SerializeField] private int maxCheckCount = 5;       // 最大检测次数（防止无限检测）
        #endregion

        #region  缩放相关变量
        [HideInInspector] public Vector3 PrefabOriginalScale;
        [HideInInspector] public float SpawnScaleFactor;
        [HideInInspector] public float ScaleAnimDuration;
        [HideInInspector] public AnimationCurve ScaleEaseCurve;
        private Coroutine scaleCoroutine;
        #endregion

        #region 匹配接收柱可视化配置
        [Header("匹配接收柱可视化配置")]
        [SerializeField] private Color matchedBinGizmoColor = Color.green; // 匹配接收柱标记颜色
        [SerializeField] private float matchedBinGizmoSize = 0.15f;        // 匹配接收柱标记大小
        [SerializeField] private bool showMatchedBinLabels = true;         // 是否显示接收柱标签
        [SerializeField] private float binLabelOffsetY = 0.3f;             // 接收柱标签Y轴偏移（避免遮挡）
        #endregion

        #region 中心点可视化配置
        [Header("中心点可视化配置")]   
        [SerializeField] private bool enableAllVisualizations = true; // 整体可视化总开关（控制所有Gizmos的显示/隐藏）
        [SerializeField] private Color slotCenterColor = Color.red;       // 插槽中心点颜色
        [SerializeField] private Color binCenterColor = Color.blue;        // 接收柱群中心点颜色
        [SerializeField] private float centerGizmoSize = 0.1f;             // 中心点球体大小
        [SerializeField] private bool showCenterLabels = true;             // 是否显示文字标签
        #endregion

        #region 事件与公共属性
        // 事件：通知积木放置/移除状态（传递自身和预制体索引）
        public event System.Action<BlockControl, int> OnPlaced;  // 放置时触发
        public event System.Action<BlockControl, int> OnRemoved; // 移除时触发
        public int PrefabIndex { get; set; } // 记录自身在预制体数组中的索引
        #endregion

        #region 生命周期函数
        private void Awake()
        {
            //initialPosition = transform.position;
            InitializeBlock();
            InitializeOutlinable();
            OnRemoved += OnBlockRemovedStopGear;
            //初始化中心点为无效值（避免初始绘制错误）
            _slotLocalCenterWorld = Vector3.negativeInfinity;
            _binWorldCenter = Vector3.negativeInfinity;
        }
        #endregion

        #region 初始化齿轮
        private void InitializeBlock()
        {
            if (_blockData?._gearobject == null) return;

            for (int i = 0; i < _blockData._gearobject.Length; i++)
            {
                var gearData = _blockData._gearobject[i];
                if (gearData?.GearObject == null) continue;

                // 激活/禁用齿轮
                bool isEnable = (i == 0 && _blockData.gear1) || (i == 1 && _blockData.gear2) || (i == 2 && _blockData.gear3) || (i == 3 && _blockData.gear4);
                gearData.GearObject.SetActive(isEnable);

                // 初始化齿轮控制脚本（关联BlockData的旋转参数）
                if (isEnable)
                {
                    GearControl gearCtrl = gearData.GearObject.GetComponent<GearControl>();
                    if (gearCtrl != null)
                    {
                        gearData.CurrentDirection = gearCtrl.initialDirection;
                        gearData.RotationSpeed = gearCtrl.rotationSpeed;
                        gearData.IsActive = true;
                    }
                }
            }
        }
        // 初始化轮廓组件
        private void InitializeOutlinable()
        {
            // 获取自身游戏对象上的Outlinable组件（EPOOutline插件的组件）
            outlinable = this.GetComponent<Outlinable>();

            // 空值检查：如果没有挂载组件，提示用户（避免后续使用报错）
            if (outlinable == null)
            {
                //Debug.LogError($"【BlockControl】积木 {gameObject.name} 未挂载 Outlinable 组件！请在Inspector面板添加EPOOutline的Outlinable组件。", this);
                return;
            }
            defaultOutlineColor = outlinable.OutlineParameters.Color;

            // 初始化轮廓状态（默认隐藏，拖拽时再显示）
            outlinable.enabled = false;
  
        }
        private void SetOutlineColor(Color targetColor)
        {
            if (outlinable == null) return;
            outlinable.OutlineParameters.Color = targetColor;
        }
        public void SetOutlinable(bool OutlinableBool)
        {
            // 仅在关闭轮廓时重置颜色，开启时保留当前颜色（避免覆盖拖拽中的实时颜色）
            if (!OutlinableBool)
            {
                SetOutlineColor(defaultOutlineColor);
            }
            outlinable.enabled = OutlinableBool;
        }

        #endregion

        #region IInteractable 接口实现
        public bool CanDrag() => true;

        public void OnClick()
        {
            if(isPlaced)
            {
                return;
            }
            if(LevelManager.Instance.CurrentLevelIndex==0&&!LevelManager.Instance.IsSecondState)
            {
                LevelManager.Instance.IsSecondState = true;
                LevelManager.Instance.AfterRotation();
                
            }
            RotateCurrentBlock();
            VibrationManager.VibrateShort();
            Debug.Log($"点击了积木：{gameObject.name}");
        }

        public void OnDragStart(Vector3 hitPoint)
        {
            RemoveBlock();// 已放置的积木移除
            dragOffset = transform.position - hitPoint;
            //Z轴 += forwardOffset，实现向前偏移
            transform.position = new Vector3(transform.position.x, dragHeight, transform.position.z + forwardOffset);
            // 播放拿起积木音效
            SoundManager.Instance?.PlaySound("1");
            VibrationManager.VibrateShort();
            SetOutlinable(true);

            GameStateManager.Instance.ResumeCountdown();

            scaleCoroutine = StartCoroutine(SmoothScale(PrefabOriginalScale));
        }
        // 平滑缩放协程（基于预制体原缩放）
        private IEnumerator SmoothScale(Vector3 targetScale)
        {
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < ScaleAnimDuration && gameObject.activeInHierarchy)
            {
                float t = ScaleEaseCurve.Evaluate(elapsed / ScaleAnimDuration);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localScale = targetScale;
            scaleCoroutine = null;
        }
        public void OnDragged(Vector2 screenPosition, Vector3 delta)
        {
            if (ScreenToGroundPosition(screenPosition, out Vector3 groundPosition))
            {
                Vector3 newPosition = groundPosition + dragOffset;
                newPosition.y = dragHeight;
                transform.position = newPosition;

                // 实时预检测与颜色反馈 
                // 1. 实时判断当前位置是否合法（复用已有IsValidPlacement逻辑，无需重写）
                bool isCurrentPositionValid = IsValidPlacement();
                // 2. 根据合法性切换轮廓颜色
                if (isCurrentPositionValid)
                {
                    SetOutlineColor(canplacedOutlineColor); // 合法：绿色
                }
                else
                {
                    SetOutlineColor(defaultOutlineColor); // 不合法：红色
                }
            }
        }

        public void OnDragEnd()
        {
            VibrationManager.VibrateShort();
            if (IsValidPlacement())
            {
                PlaceBlock();
            }
            else
            {
                // 确保复位前父节点正确
                if (!isPlaced)
                {
                    transform.SetParent(BlockGenerate.Instance?.middleBlockParent);
                }
                SafeResetPosition();
                scaleCoroutine = StartCoroutine(SmoothScale(PrefabOriginalScale * SpawnScaleFactor));
                // 放置失败时重置中心点（避免残留无效可视化）
                _slotLocalCenterWorld = Vector3.negativeInfinity;
                _binWorldCenter = Vector3.negativeInfinity;
            }
        }
        #endregion

        #region 验证对齐逻辑
        private bool IsValidPlacement()
        {
            matchedBins.Clear();
            // 1. 基础校验：确保必要组件和数据存在
            if (_blockData == null)
            {
                Debug.LogError($"积木 {gameObject.name} 的 _blockData 未赋值！");
                return false;
            }
            List<Vector3> blockSlotPoints = _blockData.SlotPoints;
            if (blockSlotPoints == null || blockSlotPoints.Count == 0)
            {
                Debug.LogError($"积木{gameObject.name}未配置slotPoints！");
                return false;
            }
            // 2. 检查接收柱生成器实例
            if (ReceivingBinGenerate.Instance == null)
            {
                Debug.LogError("ReceivingBinGenerate 实例未找到！");
                return false;
            }
            var activeBins = ReceivingBinGenerate.Instance.activeReceivingBins;
            if (activeBins == null || activeBins.Count < blockSlotPoints.Count)
            {
                Debug.LogWarning("活跃接收柱数量不足，无法匹配所有slotPoints！");
                return false;
            }


            // 2. 将积木插槽点转换为世界坐标（基于当前拖拽位置）
            List<Vector3> slotPointsWorld = new List<Vector3>();
            foreach (Vector3 localSlot in blockSlotPoints)
            {
                Vector3 worldSlot = transform.TransformPoint(localSlot);
                slotPointsWorld.Add(worldSlot);
            }

            // 3. 双重循环匹配：确保每个插槽点都有对应的接收柱
            foreach (Vector3 slotWorld in slotPointsWorld)
            {
                ReceivingBinControl bestMatchedBin = null;
                float minDistance = float.MaxValue;

                foreach (var bin in activeBins)
                {
                    if (bin == null || !bin.gameObject.activeInHierarchy || bin.isOccupied)
                        continue;

                    // 计算XZ平面距离（忽略Y轴高度）
                    float distance = Vector2.Distance(
                        new Vector2(slotWorld.x, slotWorld.z),
                        new Vector2(bin.transform.position.x, bin.transform.position.z)
                    );

                    if (distance <= slotPointsAdsorption && distance < minDistance)
                    {
                        minDistance = distance;
                        bestMatchedBin = bin;
                    }
                }

                matchedBins.Add(bestMatchedBin);
            }
            // 若不是所有插槽点都找到匹配的接收柱，直接判定为不合法
            if (matchedBins.Count != slotPointsWorld.Count)
            {
                Debug.Log("不完全匹配");
                return false;
            }
            // 额外检查：确保所有匹配的接收柱都不是null
            foreach (var bin in matchedBins)
            {
                if (bin == null)
                {
                    //Debug.Log("存在未匹配的插槽点");
                    return false;
                }
            }
            // 4. 计算中心点并确定最终位置
            // 4.1 计算积木所有插槽点的本地中心点
            _slotLocalCenter = Vector3.zero;
            foreach (Vector3 localSlot in blockSlotPoints)
            {
                _slotLocalCenter += localSlot;
            }
            _slotLocalCenter /= blockSlotPoints.Count;

            // 4.2 计算匹配的接收柱群的世界坐标中心点
            _binWorldCenter = Vector3.zero;
            foreach (ReceivingBinControl bin in matchedBins)
            {
                if (bin != null)
                    _binWorldCenter += bin.transform.position;
            }
            _binWorldCenter /= matchedBins.Count;

            // 4.3 核心公式：正确计算目标位置
            // 将本地中心点转换为世界坐标，然后计算偏移
            Vector3 slotWorldCenter = transform.TransformPoint(_slotLocalCenter);
            _slotLocalCenterWorld = slotWorldCenter; // 用于可视化

            // 计算当前插槽中心点与目标中心点的偏移
            Vector3 centerOffset = _binWorldCenter - slotWorldCenter;
            // 将偏移应用到当前积木位置
            _targetPlacementPos = transform.position + centerOffset;

            return true;
        }
        #endregion

        #region 放置逻辑
        private bool ScreenToGroundPosition(Vector2 screenPos, out Vector3 groundPos)
        {
            // 关键修改：不再用原始射线，而是调用InputManager的偏移射线
            if (InputManager.Instance == null)
            {
                Debug.LogError("InputManager.Instance 为空，无法获取偏移射线！");
                groundPos = Vector3.zero;
                return false;
            }
            Ray ray = InputManager.Instance.GetOffsetRay(screenPos); // 使用偏移射线
            //Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                groundPos = ray.GetPoint(distance);
                return true;
            }

            groundPos = Vector3.zero;
            return false;
        }
        
        private void PlaceBlock()
        {
            isPlaced = true;

            foreach (var bin in matchedBins)
            {
                if (bin != null)
                    bin.isOccupied = true;
            }
            // 移动到目标位置,    
            transform.position =new Vector3( _targetPlacementPos.x, _targetPlacementPos.y-CloseGroundValue, _targetPlacementPos.z);
            // Debug.Log($"积木 {gameObject.name} 已居中对齐！插槽中心点与接收柱群中心点重合");
            OnPlaced?.Invoke(this, PrefabIndex);
            BlockGenerate.Instance.UpdateBlockTransform();
            BlockGenerate.Instance.UpdateBlockPosition();
            // 播放放下积木音效（成功放置）
            SoundManager.Instance?.PlaySound("2");
            SetOutlinable(false);
            Invoke("TargetGearEffect", 0.3f);//延迟触发特效 
            Invoke("DelayUpdatelevelObjectiveGear", 0.5f);

        }

        private void TargetGearEffect()
        {
            LevelManager.Instance.TargetGearEffect();
        }//触发特效

        #endregion

        #region 移除逻辑
        public void SafeResetPosition()
        {
            isResetting = false; // 强制重置状态，避免残留
            if (gameObject.activeInHierarchy)
            {
                BlockGenerate.Instance.isSwitchingAnimationPlaying = true;
                StartCoroutine(ResetPosition()); // 激活时走平滑复位
            }
            else
            {
                // 核心修改：未激活时，用本地坐标转换获取目标位置
                Transform targetParent = BlockGenerate.Instance?.middleBlockParent ?? transform.parent;
                if (targetParent != null)
                {
                    // 本地坐标转世界坐标，保留Y轴高度
                    Vector3 targetWorldPos = targetParent.TransformPoint(LocalSpawnPosition);
                    targetWorldPos.y = dragHeight;
                    transform.position = targetWorldPos;
                }
                isPlaced = false;
                SetOutlinable(false);
                // 重置中心点可视化
                _slotLocalCenterWorld = Vector3.negativeInfinity;
                _binWorldCenter = Vector3.negativeInfinity;
            }
        }
        public IEnumerator ResetPosition()
        {
            if (isResetting) yield break; // 避免重复触发动画
            isResetting = true;

            isPlaced = false;
            SoundManager.Instance?.PlaySound("2"); // 播放复位音效

            // 1. 确定目标父节点（和UpdateBlockPosition的父节点保持一致）
            Transform targetParent = BlockGenerate.Instance?.middleBlockParent ?? transform.parent;
            if (targetParent == null)
            {
                targetParent = transform.parent;
                isResetting = false;
                yield break; // 父节点为空，无法计算位置，直接退出
            }

            // 2. 同步最新布局数据（和UpdateBlockPosition依赖的数据源一致）
            BlockGenerate.Instance.UpdateBlockTransform(); // 更新blockCount、边界、LocalSpawnPosition

            // 3. 复刻UpdateBlockPosition的「有效积木筛选+排序逻辑」（关键：确保索引一致）
            List<BlockControl> validBlocks = new List<BlockControl>();
            foreach (Transform child in targetParent)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    BlockControl blockCtrl = child.GetComponent<BlockControl>();
                    if (blockCtrl != null)
                    {
                        validBlocks.Add(blockCtrl);
                    }
                }
            }
            // 按PrefabIndex排序（和UpdateBlockPosition完全一致，确保索引顺序相同）
            validBlocks.Sort((a, b) => a.PrefabIndex.CompareTo(b.PrefabIndex));

            // 4. 找到当前积木在排序后列表中的索引（确保计算的坐标是UpdateBlockPosition会用的坐标）
            int currentBlockIndex = validBlocks.FindIndex(block => block == this);
            if (currentBlockIndex == -1) // 异常情况：当前积木不在有效列表中
            {
                Debug.LogWarning($"积木 {gameObject.name} 不在有效列表中，复位失败");
                isResetting = false;
                yield break;
            }

            // 5. 复刻UpdateBlockPosition的「坐标计算逻辑」（公式完全一致）
            float blockCount = validBlocks.Count;
            float centerIndex = (blockCount - 1) / 2f;
            float blockSpacing = BlockGenerate.Instance.blockSpacing; // 直接用BlockGenerate的间隔（避免偏差）
            Vector3 finalLocalPos = new Vector3((currentBlockIndex - centerIndex) * blockSpacing, 0f, 0f);

            // 6. 同步LocalSpawnPosition（确保后续其他逻辑也用这个最终坐标）
            this.LocalSpawnPosition = finalLocalPos;

            // 7. 动画以「最终本地坐标」为目标（用本地坐标动画，不受父节点位置影响，更稳定）
            Vector3 startLocalPos = transform.localPosition; // 动画起始本地坐标
            float elapsed = 0f;

            while (elapsed < resetAnimDuration && gameObject.activeInHierarchy)
            {
                elapsed += Time.deltaTime;
                float t = resetEaseCurve.Evaluate(elapsed / resetAnimDuration);
                // 本地坐标平滑插值（直接瞄准最终坐标）
                transform.localPosition = Vector3.Lerp(startLocalPos, finalLocalPos, t);
                // 保持Y轴高度（避免落地）
                transform.localPosition = new Vector3(transform.localPosition.x, dragHeight, transform.localPosition.z);
                yield return null;
            }

            // 8. 强制赋值最终坐标（确保动画结束后位置100%准确，无插值偏差）
            transform.localPosition = finalLocalPos;
            transform.localPosition = new Vector3(transform.localPosition.x, dragHeight, transform.localPosition.z);

            // 9. 收尾工作
            SetOutlinable(false);
            isResetting = false;

            // 此时调用UpdateBlockPosition：因为当前积木已经在最终位置，其他积木也会按相同逻辑更新
            // 不会出现跳变，反而能同步所有积木的位置，确保整体布局整齐
            BlockGenerate.Instance.UpdateBlockPosition();
            BlockGenerate.Instance.isSwitchingAnimationPlaying = false;

            // 重置中心点可视化（避免残留）
            _slotLocalCenterWorld = Vector3.negativeInfinity;
            _binWorldCenter = Vector3.negativeInfinity;
        }

        public void RemoveBlock()
        {
            if (!isPlaced) return;

            // 更安全的接收柱清理
            foreach (var bin in matchedBins.ToArray()) // 使用副本遍历
            {
                if (bin != null && bin.gameObject != null)
                {
                    bin.isOccupied = false;
                }
            }
            matchedBins.Clear();
            isPlaced = false;


            //除时重置中心点（避免残留可视化）
            _slotLocalCenterWorld = Vector3.negativeInfinity;
            _binWorldCenter = Vector3.negativeInfinity;

            OnRemoved?.Invoke(this, PrefabIndex);
            Invoke("DelayUpdatelevelObjectiveGear", 0.3f);
            Invoke("GearEffectBool", 0.5f);
            //Debug.Log($"积木 {gameObject.name} 已移除");
        }
        #endregion

        #region UI更新
        private void DelayUpdatelevelObjectiveGear()
        {
            UIManager.Instance.UpdatelevelObjectiveGear();
        }
        #endregion

        #region 旋转逻辑
        public void RotateCurrentBlock()
        {
            // 避免重复旋转（可选但建议加）
            if (isRotating) return;

            isRotating = true;
            StartCoroutine(SmoothRotate(this.transform));
        }
        IEnumerator SmoothRotate(Transform block)
        {
            SoundManager.Instance.PlaySound("34");
            float t = 0;
            Quaternion startRot = block.rotation; // 起始旋转
            Quaternion targetRot = startRot * Quaternion.Euler(0, 90, 0); // 目标旋转（+90度Y轴）

            // 线性插值旋转
            while (t < 1)
            {
                t += Time.deltaTime / rotateTime; // 计算进度（0~1）
                block.rotation = Quaternion.Lerp(startRot, targetRot, t);
                yield return null; // 等下一帧
            }

            block.rotation = targetRot; // 确保最终角度准确
            isRotating = false;

        }
        #endregion

        #region  随时检测停止齿轮旋转

        public void OnBlockRemovedStopGear(BlockControl block, int prefabIndex)
        {
           
            GearControl[] allGears = Object.FindObjectsOfType<GearControl>();
            if (allGears == null || allGears.Length == 0)
            {
                //Debug.Log("场景中没有齿轮，无需停止旋转");
                return;
            }

            // 遍历所有齿轮
            foreach (GearControl gear in allGears)
            {
                // 跳过空引用或未激活的齿轮
                if (gear == null || !gear.gameObject.activeInHierarchy)
                    continue;

                // 仅停止非动力齿轮（保留动力齿轮旋转）
                //if (gear.gearType != GearType.Power && gear.IsRotating)
                if (gear.gearType == GearType.Normal && gear.IsRotating)
                {
                    gear.StopRotation();
                    //Debug.Log($"停止非动力齿轮：{gear.gameObject.name}");
                }
                 if (gear.gearType == GearType.Target&&!gear.IsRotating)
                {
                    gear.StopRotation();
                    //gear.EffectBool = false;
                    //Debug.Log($"保留动力齿轮旋转：{gear.gameObject.name}");
                }
            }
        }

        private void OnDestroy()
        {
            // 更完整的事件清理
            OnPlaced = null;
            OnRemoved = null;

            // 停止所有协程
            StopAllCoroutines();

            // 清理接收柱占用
            foreach (var bin in matchedBins.ToArray())
            {
                if (bin != null) bin.isOccupied = false;
            }
            matchedBins.Clear();
        }
        #endregion

        #region  Gizmos 绘制方法（在Scene视图中显示中心点 + 匹配接收柱）
        private void OnDrawGizmos()
        {
            // 新增：整体可视化总开关，为false时直接退出，不绘制任何Gizmos
            if (!enableAllVisualizations) return;

            // -------------------------- 绘制中心点 --------------------------
            // 1. 绘制积木插槽中心点（世界坐标）
            if (_slotLocalCenterWorld != Vector3.negativeInfinity) // 仅当中心点有效时绘制
            {
                Gizmos.color = slotCenterColor;
                Gizmos.DrawSphere(_slotLocalCenterWorld, centerGizmoSize);
                // 绘制文字标签（受原有子开关控制）
#if UNITY_EDITOR
                if (showCenterLabels)
                {
                    UnityEditor.Handles.Label(_slotLocalCenterWorld + Vector3.up * 0.2f,
                        $"{gameObject.name}\n插槽中心点");
                }
#endif
            }

            // 2. 绘制接收柱群中心点（世界坐标）
            if (_binWorldCenter != Vector3.negativeInfinity) // 仅当中心点有效时绘制
            {
                Gizmos.color = binCenterColor;
                Gizmos.DrawSphere(_binWorldCenter, centerGizmoSize);
                // 绘制文字标签（受原有子开关控制）
#if UNITY_EDITOR
                if (showCenterLabels)
                {
                    UnityEditor.Handles.Label(_binWorldCenter + Vector3.up * 0.2f,
                        "接收柱群中心点");
                }
#endif

                // 绘制插槽中心点与接收柱群中心点的连线（可选）
                if (_slotLocalCenterWorld != Vector3.negativeInfinity)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(_slotLocalCenterWorld, _binWorldCenter);
                }
            }

            // -------------------------- 绘制matchedBins接收柱 --------------------------
            // 遍历所有匹配的接收柱，绘制标记和标签（受原有子开关控制）
            if (matchedBins != null && matchedBins.Count > 0)
            {
                foreach (var matchedBin in matchedBins)
                {
                    // 跳过空值或未激活的接收柱（避免报错）
                    if (matchedBin == null || !matchedBin.gameObject.activeInHierarchy)
                        continue;

                    // 1. 绘制接收柱位置的球体标记
                    Gizmos.color = matchedBinGizmoColor;
                    Gizmos.DrawSphere(matchedBin.transform.position, matchedBinGizmoSize);

                    // 2. 绘制接收柱标签（显示名称，避免遮挡）
#if UNITY_EDITOR
                    if (showMatchedBinLabels)
                    {
                        // 标签位置：接收柱上方偏移（可通过Inspector调整offsetY）
                        Vector3 labelPos = matchedBin.transform.position + Vector3.up * binLabelOffsetY;
                        UnityEditor.Handles.Label(labelPos, $"匹配接收柱: {matchedBin.gameObject.name}");
                    }
#endif
                }
            }
        }
        #endregion
    }
}