using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear
{
    [System.Serializable]
    public class BlockGenerate : MonoBehaviour
    {
      
        [System.Serializable]
        public class BlockTypeMapping
        {
            public BlockType blockType;
            public GameObject blockPrefab;
        }
        public static BlockGenerate Instance;

        #region 序列化配置

        #region  积木生成参数与缩放配置
        public List<GameObject> blockInstances = new List<GameObject>();
        [Header("积木排列与切换配置")]
        [Tooltip("积木之间的间隔（世界单位）")]
        [SerializeField] public float blockSpacing = 2f; // 可自由调节的积木间隔
        [Header("积木缩放配置")]
        [Tooltip("生成时的缩放系数（小于1为缩小，大于1为放大）")]
        [SerializeField] private float spawnScaleFactor = 0.7f;
        [Tooltip("缩放动画时长（秒）")]
        [SerializeField] private float scaleAnimDuration = 0.3f;
        [Tooltip("缩放动画缓动曲线")]
        [SerializeField] private AnimationCurve scaleEaseCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private int blockCount; // 记录积木总数，用于动态计算布局
        public float _minMiddleX=0; // （左边界）
        public float _maxMiddleX=0; //（右边界）
        private float minMiddleX; // （左边界）
        private float maxMiddleX; //（右边界）
        [Header("类型与预制体映射")]
        [SerializeField] public List<BlockTypeMapping> typeMappings = new List<BlockTypeMapping>();
        public Dictionary<BlockType, GameObject> typeToPrefabDict = new Dictionary<BlockType, GameObject>();
        #endregion

        #region 滑动切换
        [Header("滑动区域配置")]
        [SerializeField] private Camera mainCamera;
        private bool isSliding = false;
        private Vector2 lastTouchPosition;
        [SerializeField] private float slideSensitivity = 0.02f;
        [SerializeField] private float slideAreaRate = 0.8f;
        public bool isSwitchingAnimationPlaying = false;

        [Header("拖拽平滑配置")]
        [SerializeField] private float dragSmoothTime = 0.05f;
        private float _smoothDampVelocity = 0f;
        private Coroutine _moveCoroutine;

        [Header("惯性滑动配置")]
        [Tooltip("惯性系数（越大惯性越强）")]
        [SerializeField] private float inertiaFactor = 3f;
        [Tooltip("惯性减速系数（越大减速越快）")]
        [SerializeField] private float inertiaDeceleration = 5f;
        [Tooltip("最小惯性速度（低于此值不触发惯性f）")]
        [SerializeField] private float minInertiaSpeed = 10f;

        [Header("边界弹性配置")]
        [Tooltip("边界弹性系数（0-1，越大弹性越强，默认0.2f）")]
        [SerializeField] private float boundaryElasticity = 0.2f;

        private float _dragVelocity;
        private float _currentInertiaSpeed;
        private bool _isInBoundaryElasticity = false; // 标记是否正在边界弹性状态
        #endregion



        #region 缩放偏移
        [Header("缩放偏移配置")]
        [SerializeField] public Transform leftBlockParent;   // 左积木父对象
        [SerializeField] public Transform middleBlockParent; // 中积木父对象（原blockParent）
        [SerializeField] public Transform rightBlockParent;  // 右积木父对象
        [SerializeField] public Transform blockPlaceParent;  //积木放置时的父对象
        //积木父对象的Z轴参数
        [Header("Z坐标与摄像机Size关联")]
        [Tooltip("Z坐标随摄像机size变化的系数（控制敏感度）")]
        [SerializeField] private float zScaleFactor = 1f; // 负号用于反向调整（可根据需求修改）
        [Tooltip("Z坐标基础偏移（在size计算基础上的固定偏移）")]
        [SerializeField] private float baseZOffset = 0f;
        [Tooltip("父节点Z轴平滑移动速度（值越大越快）")]
        [SerializeField] private float smoothMoveSpeed = 10f; // 初始值10，可根据需求调整
        private float _leftOriginalZ;    // 左生成点原位置Z值
        private float _middleOriginalZ;  // 中生成点原位置Z值
        private float _rightOriginalZ;   // 右生成点原位置Z值
        private bool _hasInitializedOriginalZ = false; // 标记是否已初始化初始Z值
        // 存储当前目标Z值（用于平滑插值）
        private float _targetLeftZ;
        private float _targetMiddleZ;
        private float _targetRightZ;
        #endregion

        #region Gizmo 边界可视化配置
        [Header("边界 Gizmo 配置")]
        [Tooltip("左边界 Gizmo 颜色")]
        [SerializeField] private Color leftBoundColor = Color.red; // 左边界默认红色
        [Tooltip("右边界 Gizmo 颜色")]
        [SerializeField] private Color rightBoundColor = Color.blue; // 右边界默认蓝色
        [Tooltip("Gizmo 线条长度（Y轴延伸距离）")]
        [SerializeField] private float gizmoLineLength = 5f; // 线条默认长度5单位
        [Tooltip("Gizmo 线条宽度（仅Scene视图生效）")]
        [SerializeField] private float gizmoLineWidth = 2f; // 线条默认宽度2像素
        [Tooltip("是否显示边界文字标签")]
        [SerializeField] private bool showBoundLabels = true; // 默认显示标签
        [Header("MiddleParent Gizmo 配置")]
        [Tooltip("实心球颜色")]
        [SerializeField] private Color middleParentColor = Color.green; // 区分边界的绿色
        [Tooltip("实心球大小（世界单位）")]
        [SerializeField] private float ballMarkerSize = 0.3f; // 默认0.3，可调整
        [Tooltip("相对于父节点的偏移量（世界单位）")]
        [SerializeField] private Vector3 middleParentOffset = new Vector3(0f, 0.5f, 0f); // 默认Y轴上移0.5（避免遮挡）
        #endregion

        #endregion

        #region 生命周期方法
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
            InitializeTypeMappingDict();
            InitializeOriginalZPositions();
        }
        private void Update()
        {
            ThePostionOfPrarentsMove();//缩放位移
            CheckScreenBottomAndMove();
        }
        #endregion

        #region 初始化与生成(新)
        //初始化积木映射
        private void InitializeTypeMappingDict()
        {
            typeToPrefabDict.Clear();
            foreach (var mapping in typeMappings)
            {
                if (mapping.blockPrefab != null && !typeToPrefabDict.ContainsKey(mapping.blockType))
                {
                    typeToPrefabDict.Add(mapping.blockType, mapping.blockPrefab);

                    if (mapping.blockPrefab.GetComponent<BlockControl>() == null)
                    {
                        Debug.LogError($"预制体 {mapping.blockPrefab.name} 缺少 BlockControl 组件！");
                    }
                }
            }
        }

        public void InitializeBlockGenerateData(LevelData levelData)
        {
            DestroyOldBlocks();
            var generateData = levelData._blockGenerateData;
            if (generateData == null || generateData.generateItems.Count == 0)
            {
                Debug.LogWarning("当前关卡没有配置积木生成数据！");
                return;
            }
            // 1. 记录积木总数
            blockCount = generateData.generateItems.Count;
            // 2. 直接计算布局
            CalculateBlockLayout();
            // 3. 生成并排列积木
            foreach (int i in Enumerable.Range(0, blockCount))
            {
                var item = generateData.generateItems[i];
                GenerateBlockWithLayout(item, i);
            }
        }
        // 计算单个积木相对于middleBlockParent的本地位置（核心：左右对称排列）
        private Vector3 CalculateBlockLocalPos(int index)
        {
            if (index >= blockCount) return Vector3.zero;

            // 1. 计算中心索引（支持奇数/偶数积木数量，如3个为1、4个为1.5）
            float centerIndex = (blockCount - 1) / 2f;

            // 2. 计算X偏移：（当前索引 - 中心索引）× 间隔 → 实现左右对称
            // 例：3个积木（索引0/1/2）→ 偏移分别为-2、0、2（blockSpacing=2时）
            float localX = (index - centerIndex) * blockSpacing;

            // Y、Z设为0（继承middleBlockParent的Y/Z位置，避免额外偏移）
            return new Vector3(localX, 0f, 0f);
        }

        // 正式生成积木：按计算好的布局排列，记录本地坐标
        private void GenerateBlockWithLayout(BlockGenerateItem config, int index)
        {
            if (!typeToPrefabDict.TryGetValue(config.blockType, out GameObject prefab))
            {
                Debug.LogError($"未找到 {config.blockType} 对应的预制体！");
                return;
            }

            // 1. 计算本地位置（左右对称）
            Vector3 spawnLocalPos = CalculateBlockLocalPos(index);
            // 2. 生成积木到middleBlockParent下，赋值本地位置
            GameObject blockInstance = Instantiate(prefab, middleBlockParent);
            blockInstance.transform.localPosition = spawnLocalPos; // 关键修改：用localPosition

            Vector3 prefabOriginalScale = prefab.transform.localScale;
            Vector3 spawnScale = prefabOriginalScale * spawnScaleFactor;
            blockInstance.transform.localScale = spawnScale;

            blockInstance.transform.rotation = Quaternion.identity;
            blockInstance.SetActive(true);
            BlockControl blockControl = blockInstance.GetComponent<BlockControl>();
            if (blockControl != null)
            {
                // 3. 齿轮数据配置
                blockControl._blockData.gear1 = config.gear1;
                blockControl._blockData.gear2 = config.gear2;
                blockControl._blockData.gear3 = config.gear3;
                blockControl._blockData.gear4 = config.gear4;
                UpdateGearActivation(blockControl._blockData);

                // 4. 记录积木的本地坐标
                blockControl.LocalSpawnPosition = blockInstance.transform.localPosition;

                // 5. 索引与事件绑定
                blockInstances.Add(blockInstance);
                int instanceIndex = blockInstances.Count - 1;
                blockControl._blockData._blockType = config.blockType;
                blockControl.PrefabIndex = instanceIndex;
                blockControl.OnPlaced += HandleBlockPlaced;
                blockControl.OnRemoved += HandleBlockRemoved;

                //6. 传递缩放参数给BlockControl
                blockControl.PrefabOriginalScale = prefabOriginalScale;
                blockControl.SpawnScaleFactor = spawnScaleFactor;
                blockControl.ScaleAnimDuration = scaleAnimDuration;
                blockControl.ScaleEaseCurve = scaleEaseCurve;

            }
            else
            {
                Debug.LogError($"预制体 {blockInstance.name} 缺少 BlockControl 组件！");
                Destroy(blockInstance);
            }
        }

        // 计算布局关键参数（核心：修正滑动边界）
        private void CalculateBlockLayout()
        {
            if (blockCount == 0) return;

            float centerIndex = (blockCount - 1) / 2f;
            float firstBlockLocalX = (0 - centerIndex) * blockSpacing* slideAreaRate;
            float lastBlockLocalX = (blockCount - 1 - centerIndex) * blockSpacing* slideAreaRate;

            // 输出所有计算参数
            //Debug.Log($"[滑动边界计算] 积木总数：{blockCount} | 中心索引：{centerIndex} | 左积木本地X：{firstBlockLocalX} | 右积木本地X：{lastBlockLocalX}");
            //Debug.Log($"[滑动边界计算] 原始边界 _minMiddleX：{_minMiddleX} | _maxMiddleX：{_maxMiddleX}");

            
            minMiddleX = firstBlockLocalX;
            maxMiddleX =  lastBlockLocalX;

            // 关键日志：确认最终滑动范围（必须满足 minMiddleX < maxMiddleX，否则只有单边生效）
            //Debug.Log($"[滑动边界计算] 最终滑动范围：minMiddleX = {minMiddleX} | maxMiddleX = {maxMiddleX}");

            // 如果边界反向，强制修正并报警告
            if (minMiddleX >= maxMiddleX)
            {
                float temp = minMiddleX;
                minMiddleX = maxMiddleX - 1f; // 强制留出1单位滑动空间
                maxMiddleX = minMiddleX+2f;
                //Debug.LogError($"[滑动边界错误] minMiddleX >= maxMiddleX，已强制修正！修正后：{minMiddleX} ~ {maxMiddleX}");
               // Debug.LogError($"请检查：1. _minMiddleX（{_minMiddleX}）是否小于 _maxMiddleX（{_maxMiddleX}）；2. blockSpacing（{blockSpacing}）是否过大");
            }
        }
        // 齿轮设置
        private void UpdateGearActivation(BlockData blockData)
        {
            if (blockData._gearobject == null) return;

            for (int i = 0; i < blockData._gearobject.Length; i++)
            {
                if (blockData._gearobject[i]?.GearObject == null) continue;

                bool isActive = i switch
                {
                    0 => blockData.gear1,
                    1 => blockData.gear2,
                    2 => blockData.gear3,
                    3 => blockData.gear4,
                    _ => false
                };

                blockData._gearobject[i].IsActive = isActive;
                blockData._gearobject[i].GearObject.SetActive(isActive);

            }
        }
        #endregion

        #region  滑动切换逻辑(新)
        private void CheckScreenBottomAndMove()
        {
            // 关键新增：如果InputManager存在且正在拖拽积木 → 直接禁用滑动
            if (InputManager.Instance != null && InputManager.Instance.IsDragging)
                return;

            // 基础跳过条件
            //if (middleBlockParent == null || isSwitchingAnimationPlaying || Time.timeScale == 0)
            //    return;
            if (middleBlockParent == null|| Time.timeScale == 0)
                return;

            // 处理触屏输入
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector2 touchPos = touch.position;

                // 核心判断：是否在屏幕下半部分
                bool isInBottomHalf = touchPos.y < Screen.height * 0.5f;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        if (isInBottomHalf) HandleSlideDown(touchPos);
                        break;

                    case TouchPhase.Moved:
                        if (isSliding) HandleSlideDrag(touchPos);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        HandleSlideUp();
                        break;
                }
            }

            // 处理鼠标输入
            if (Input.mousePresent)
            {
                Vector2 mousePos = Input.mousePosition;

                // 核心判断：是否在屏幕下半部分
                bool isInBottomHalf = mousePos.y < Screen.height * 0.5f;

                if (Input.GetMouseButtonDown(0))
                {
                    if (isInBottomHalf) HandleSlideDown(mousePos);
                }
                else if (Input.GetMouseButton(0) && isSliding)
                {
                    HandleSlideDrag(mousePos);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    HandleSlideUp();
                }
            }
        }

        // 处理滑动区域的按下事件
        private void HandleSlideDown(Vector2 inputPos)
        {
            HandleSlideUp(); // 重置之前状态

            // 停止可能的移动协程
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            isSwitchingAnimationPlaying = false;

            // 激活滑动状态
            isSliding = true;
            lastTouchPosition = inputPos;
            _smoothDampVelocity = 0f; // 重置平滑速度
        }

        private void HandleSlideDrag(Vector2 currentPos)
        {
            float deltaX = currentPos.x - lastTouchPosition.x;
            float deltaY = currentPos.y - lastTouchPosition.y; // Y轴正方向=向上滑动
            float deadZone = Screen.width * 0.005f;

            // 原有向上滑动优先级判断保留
            if (deltaY > deadZone && Mathf.Abs(deltaY) > Mathf.Abs(deltaX))
            {
                lastTouchPosition = currentPos;
                _dragVelocity = 0f; // 重置速度，避免误触发惯性
                return;
            }

            if (Mathf.Abs(deltaX) < deadZone)
            {
                lastTouchPosition = currentPos;
                _dragVelocity = 0f; // 重置速度
                return;
            }

            // 记录拖拽速度（像素/秒，用于后续惯性计算）
            _dragVelocity = deltaX / Time.deltaTime;

            // 原有滑动逻辑完全保留（包括SmoothDamp跟手、边界Clamp）
            float targetX = middleBlockParent.position.x + deltaX * slideSensitivity;
            targetX = Mathf.Clamp(targetX, minMiddleX, maxMiddleX); 
            //targetX = ApplyBoundaryElasticity(targetX);

            float smoothX = Mathf.SmoothDamp(
                middleBlockParent.position.x,
                targetX,
                ref _smoothDampVelocity,
                dragSmoothTime
            );

            middleBlockParent.position = new Vector3(
                smoothX,
                middleBlockParent.position.y,
                middleBlockParent.position.z
            );

            lastTouchPosition = currentPos;
        }

        private void HandleSlideUp()
        {
            isSliding = false;
            lastTouchPosition = Vector2.zero;

            // 拖拽速度达标（超过最小惯性速度），启动惯性滑动
            if (Mathf.Abs(_dragVelocity) > minInertiaSpeed)
            {
                if (_moveCoroutine != null)
                {
                    StopCoroutine(_moveCoroutine);
                    _moveCoroutine = null;
                }

                // 关键：将屏幕像素速度转换为世界单位速度（适配不同屏幕/布局）
                float screenToWorldRatio = (maxMiddleX - minMiddleX) / Screen.width; // 屏幕宽度与世界边界的比例
                _currentInertiaSpeed = _dragVelocity * inertiaFactor * screenToWorldRatio;

                _moveCoroutine = StartCoroutine(InertiaSlideCoroutine());
            }
            else
            {
                // 速度不达标，直接重置状态
                _smoothDampVelocity = 0f;
                _dragVelocity = 0f;
            }
        }
        //// 纯惯性滑动协程：拖拽结束后按惯性继续滑动，平滑减速，不超出边界
        //private IEnumerator InertiaSlideCoroutine()
        //{
        //    isSwitchingAnimationPlaying = true;
        //    _smoothDampVelocity = 0f; // 重置跟手平滑速度，避免冲突

        //    // 惯性滑动直到速度趋近于0（0.1为阈值，避免无限循环）
        //    while (Mathf.Abs(_currentInertiaSpeed) > 0.1f)
        //    {
        //        // 1. 惯性平滑减速（线性插值衰减）
        //        _currentInertiaSpeed = Mathf.Lerp(_currentInertiaSpeed, 0f, inertiaDeceleration * Time.deltaTime);

        //        // 2. 计算新位置，保留原有边界限制（不超出min/maxMiddleX）
        //        float newX = middleBlockParent.position.x + _currentInertiaSpeed * Time.deltaTime;
        //        //newX = Mathf.Clamp(newX, minMiddleX, maxMiddleX);
        //        newX = ApplyBoundaryElasticity(newX); // 启用弹性边界

        //        // 3. 平滑移动（保持惯性滑动的流畅度）
        //        middleBlockParent.position = Vector3.Lerp(
        //            middleBlockParent.position,
        //            new Vector3(newX, middleBlockParent.position.y, middleBlockParent.position.z),
        //            15f * Time.deltaTime // 惯性滑动平滑速度（可根据需求调整）
        //        );

        //        yield return null;
        //    }

        //    // 惯性结束，重置所有状态
        //    _currentInertiaSpeed = 0f;
        //    _dragVelocity = 0f;
        //    isSwitchingAnimationPlaying = false;
        //}

        //// 边界弹性处理：超过边界时产生回弹效果，不生硬截断
        //private float ApplyBoundaryElasticity(float targetX)
        //{
        //    if (targetX < minMiddleX)
        //    {
        //        // 左边界回弹：超出距离越多，回弹力度越柔和
        //        float overshoot = minMiddleX - targetX;
        //        targetX = minMiddleX + overshoot * boundaryElasticity;
        //    }
        //    else if (targetX > maxMiddleX)
        //    {
        //        // 右边界回弹：超出距离越多，回弹力度越柔和
        //        float overshoot = targetX - maxMiddleX;
        //        targetX = maxMiddleX - overshoot * boundaryElasticity;
        //    }
        //    return targetX;
        //}
        private bool ApplyBoundaryElasticity(ref float targetX, ref float currentSpeed)
        {
            bool isTriggeredElasticity = false;
            // 左边界反弹
            if (targetX < minMiddleX)
            {
                // 1. 修正位置到左边界（避免残留偏移）
                float overshoot = minMiddleX - targetX;
                targetX = minMiddleX; // 直接归位到边界，反弹效果由速度控制

                // 2. 反转速度（向左→向右）+ 速度衰减（乘以弹性系数）
                currentSpeed = -currentSpeed * boundaryElasticity;
                isTriggeredElasticity = true;
            }
            // 右边界反弹
            else if (targetX > maxMiddleX)
            {
                // 1. 修正位置到右边界
                float overshoot = targetX - maxMiddleX;
                targetX = maxMiddleX;

                // 2. 反转速度（向右→向左）+ 速度衰减
                currentSpeed = -currentSpeed * boundaryElasticity;
                isTriggeredElasticity = true;
            }
            return isTriggeredElasticity;
        }

        // 纯惯性滑动协程：触边后反弹（反向+减速），直到速度趋近于0
        private IEnumerator InertiaSlideCoroutine()
        {
            isSwitchingAnimationPlaying = true;
            _smoothDampVelocity = 0f;

            // 惯性滑动直到速度趋近于0（0.1为阈值，避免无限循环）
            while (Mathf.Abs(_currentInertiaSpeed) > 0.1f)
            {
                // 1. 惯性自然减速（基础减速逻辑保留）
                _currentInertiaSpeed = Mathf.Lerp(_currentInertiaSpeed, 0f, inertiaDeceleration * Time.deltaTime);

                // 2. 计算当前惯性位移后的目标位置
                float newX = middleBlockParent.position.x + _currentInertiaSpeed * Time.deltaTime;

                // 3. 应用边界弹性：触边则反弹（反转速度+衰减）
                ApplyBoundaryElasticity(ref newX, ref _currentInertiaSpeed);

                // 4. 平滑移动到目标位置（保持流畅）
                middleBlockParent.position = Vector3.Lerp(
                    middleBlockParent.position,
                    new Vector3(newX, middleBlockParent.position.y, middleBlockParent.position.z),
                    15f * Time.deltaTime
                );

                yield return null;
            }

            // 惯性结束，重置状态
            _currentInertiaSpeed = 0f;
            _dragVelocity = 0f;
            isSwitchingAnimationPlaying = false;
        }
        #endregion

        #region 更新放置取下后积木的布局与滑动边界
        public void UpdateBlockTransform()
        {
            // 1. 基础校验：确保middleBlockParent存在
            if (middleBlockParent == null)
            {
                Debug.LogError("UpdateBlockTransfrom：middleBlockParent未赋值！");
                return;
            }

            // 2. 获取middleBlockParent下所有有效积木（带BlockControl且激活的对象）
            List<BlockControl> validBlocks = new List<BlockControl>();
            // 遍历子物体获取BlockControl，排除未激活/空组件的对象
            foreach (Transform child in middleBlockParent)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    BlockControl blockCtrl = child.GetComponent<BlockControl>();
                    if (blockCtrl != null) // 确保组件存在
                    {
                        validBlocks.Add(blockCtrl);
                    }
                }
            }

            // 3. 无有效积木时直接返回（避免后续计算错误）
            if (validBlocks.Count == 0)
            {
                blockCount = 0;
                CalculateBlockLayout(); // 同步更新边界（空布局）
                Debug.Log("UpdateBlockTransfrom：middleBlockParent下无有效积木");
                return;
            }

            // 4. 更新积木总数，重新计算滑动边界
            blockCount = validBlocks.Count;
            CalculateBlockLayout(); // 基于新数量更新minMiddleX/maxMiddleX

            // 5. 按索引重新计算每个积木的本地坐标并更新
            for (int i = 0; i < validBlocks.Count; i++)
            {
                BlockControl block = validBlocks[i];
                if (block == null) continue;

                // 复用原有对称布局逻辑，计算新的本地坐标
                float centerIndex = (blockCount - 1) / 2f;
                float newLocalX = (i - centerIndex) * blockSpacing;
                Vector3 newLocalPos = new Vector3(newLocalX, 0f, 0f);

                // 更新积木的本地位置（父节点为middleBlockParent）
               // block.transform.localPosition = newLocalPos;
                // 同步更新LocalSpawnPosition（确保复位时使用最新坐标）
                block.LocalSpawnPosition = newLocalPos;

                // 可选：调试日志，查看坐标更新情况
                //Debug.Log($"积木[{i}] {block.gameObject.name} 新本地坐标：{newLocalPos}");
            }

        }
        //实际更新位置
        public void UpdateBlockPosition()
        {
            // 获取middleBlockParent下所有有效积木（带BlockControl且激活的对象）
            List<BlockControl> validBlocks = new List<BlockControl>();
            foreach (Transform child in middleBlockParent)
            {
                if (child.gameObject.activeInHierarchy)
                {
                    BlockControl blockCtrl = child.GetComponent<BlockControl>();
                    if (blockCtrl != null) // 确保组件存在
                    {
                        validBlocks.Add(blockCtrl);
                    }
                }
            }
            //按索引重新计
            //// 按PrefabIndex排序，确保顺序一致（如果需要）
            validBlocks.Sort((a, b) => a.PrefabIndex.CompareTo(b.PrefabIndex));
            for (int i = 0; i < validBlocks.Count; i++)
            {
                BlockControl block = validBlocks[i];
                if (block == null) continue;

                // 复用原有对称布局逻辑，计算新的本地坐标
                float centerIndex = (blockCount - 1) / 2f;
                float newLocalX = (i - centerIndex) * blockSpacing;
                Vector3 newLocalPos = new Vector3(newLocalX, 0f, 0f);

                // 更新积木的本地位置（父节点为middleBlockParent）
                block.transform.localPosition = newLocalPos;
                // 同步更新LocalSpawnPosition（确保复位时使用最新坐标）
                block.LocalSpawnPosition = newLocalPos;

                // 可选：调试日志，查看坐标更新情况
               // Debug.Log($"积木[{i}] {block.gameObject.name} 新本地坐标：{newLocalPos}");
            }
        }



        #endregion

        #region 事件处理（新）
        private void HandleBlockPlaced(BlockControl block, int prefabIndex)
        {
            block.transform.SetParent(blockPlaceParent); 
            UIManager.Instance?.ClearOrdinaryHints();
        }
        private void HandleBlockRemoved(BlockControl block, int prefabIndex)
        {
            block.transform.SetParent(middleBlockParent);
        }
        #endregion

        #region  辅助方法

        #region 缩放导致生成位置父节点平移
        private void InitializeOriginalZPositions()
        {
            if (_hasInitializedOriginalZ) return;
            // 初始化其他父节点原始Z值
            _leftOriginalZ = leftBlockParent?.position.z ?? 0f;
            _middleOriginalZ = middleBlockParent?.position.z ?? 0f;
            _rightOriginalZ = rightBlockParent?.position.z ?? 0f;

            // 初始化目标Z值（与原始值一致）
            _targetLeftZ = _leftOriginalZ;
            _targetMiddleZ = _middleOriginalZ;
            _targetRightZ = _rightOriginalZ;
            _hasInitializedOriginalZ = true;
        }

        public void ThePostionOfPrarentsMove()
        {
            if (!_hasInitializedOriginalZ)
            {
                InitializeOriginalZPositions();
                return;
            }

            // 获取当前摄像机Size
            float cameraSize = UIManager.Instance?.GetCurrentCameraSize() ?? 10f;

            // 1. 计算其他父节点的动态Z偏移（使用通用参数）
            float commonDynamicZOffset = (cameraSize - 10f) * zScaleFactor + baseZOffset;
           // Debug.Log($"commonDynamicZOffset：{commonDynamicZOffset} | cameraSize：{cameraSize} | zScaleFactor：{zScaleFactor}");
            _targetLeftZ = _leftOriginalZ + commonDynamicZOffset;
            _targetMiddleZ = _middleOriginalZ + commonDynamicZOffset;
            _targetRightZ = _rightOriginalZ + commonDynamicZOffset;

            // 2. 平滑更新所有父节点位置（共用平滑速度，但偏移计算独立）
            // 左父节点
            if (leftBlockParent != null)
            {
                Vector3 pos = leftBlockParent.position;
                leftBlockParent.position = Vector3.Lerp(pos, new Vector3(pos.x, pos.y, _targetLeftZ), smoothMoveSpeed * Time.deltaTime);
            }

            // 中父节点
            if (middleBlockParent != null)
            {
                Vector3 pos = middleBlockParent.position;
                middleBlockParent.position = Vector3.Lerp(pos, new Vector3(pos.x, pos.y, _targetMiddleZ), smoothMoveSpeed * Time.deltaTime);
            }

            // 右父节点
            if (rightBlockParent != null)
            {
                Vector3 pos = rightBlockParent.position;
                rightBlockParent.position = Vector3.Lerp(pos, new Vector3(pos.x, pos.y, _targetRightZ), smoothMoveSpeed * Time.deltaTime);
            }
        }
        #endregion

        #region 销毁旧积木
        public void DestroyOldBlocks()
        {
            UIManager.Instance?.ClearOrdinaryHints();
            if (blockInstances == null) return;
            // 停止所有切换动画协程，避免协程继续访问已销毁对象
            StopAllCoroutines(); // 若有其他重要协程，可改为保存切换协程引用后单独停止
            isSwitchingAnimationPlaying = false; // 重置动画状态

            // 新增：停止惯性滑动协程（避免销毁后协程继续执行）
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            // 新增：重置惯性和速度相关变量
            _dragVelocity = 0f;
            _currentInertiaSpeed = 0f;
            _smoothDampVelocity = 0f;

            for (int i = 0; i < blockInstances.Count; i++)
            {
                if (blockInstances[i] != null)
                {
                    var blockControl = blockInstances[i].GetComponent<BlockControl>();
                    if (blockControl != null)
                    {
                        blockControl.OnPlaced -= HandleBlockPlaced;
                        blockControl.OnRemoved -= HandleBlockRemoved;
                    }
                    Destroy(blockInstances[i]);
                }
            }

            // 保留原始区域重置逻辑
            blockInstances = new List<GameObject>();
        }
        #endregion

        #region  Gizmo
        private void OnDrawGizmos()
        {
            // 基础校验
            if (blockCount == 0)
            {
                Debug.LogWarning("Gizmo 未绘制：积木数量为0（未初始化）");
                return;
            }
            if (leftBlockParent == null || rightBlockParent == null || middleBlockParent == null)
            {
                Debug.LogWarning("Gizmo 未绘制：left/right/middleBlockParent 未赋值！");
                return;
            }
            if (minMiddleX == 0 && maxMiddleX == 0)
            {
                Debug.LogWarning("Gizmo 未绘制：边界未计算（请先调用 CalculateBlockLayout）");
                return;
            }

            // 1. 原有边界 Gizmo
            DrawBoundGizmo(minMiddleX, leftBoundColor, "Left Bound");
            DrawBoundGizmo(maxMiddleX, rightBoundColor, "Right Bound");

            // 2. 实心球 Gizmo
            DrawMiddleParentSolidBallGizmo();
        }

        // 原有边界绘制方法
        private void DrawBoundGizmo(float boundX, Color color, string label)
        {
            float targetY = middleBlockParent != null ? middleBlockParent.position.y : 0f;
            float targetZ = middleBlockParent != null ? middleBlockParent.position.z : 0f;

            Vector3 lineStart = new Vector3(boundX, targetY - gizmoLineLength / 2, targetZ);
            Vector3 lineEnd = new Vector3(boundX, targetY + gizmoLineLength / 2, targetZ);

            Gizmos.color = color;
            Gizmos.DrawLine(lineStart, lineEnd);

            if (showBoundLabels)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.color = color;
                Vector3 labelPos = new Vector3(boundX + 0.2f, targetY, targetZ);
                UnityEditor.Handles.Label(labelPos, $"{label}\nX: {boundX:F2}");
#endif
            }
        }

        // 最终版：实心球 + 可配置偏移
        private void DrawMiddleParentSolidBallGizmo()
        {
            if (middleBlockParent == null) return;

            // 计算最终小球位置 = 父节点位置 + 配置的偏移量
            Vector3 finalBallPos = middleBlockParent.position + middleParentOffset;

            // 绘制实心球（核心修改：DrawSphere 是实心，DrawWireSphere 是空心）
            Gizmos.color = middleParentColor;
            Gizmos.DrawSphere(finalBallPos, ballMarkerSize);

            // 可选：极简标签（显示父节点名称，可删除）
#if UNITY_EDITOR
            UnityEditor.Handles.color = middleParentColor;
            Vector3 labelPos = finalBallPos + new Vector3(0f, ballMarkerSize + 0.2f, 0f); // 标签在球上方
            UnityEditor.Handles.Label(labelPos, "MiddleParent");
#endif
        }
        #endregion

        #endregion

    }
}
