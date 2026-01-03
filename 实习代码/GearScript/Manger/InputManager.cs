using UnityEngine;
using System;
using UnityEngine.EventSystems;

namespace SuperGear
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance;
        #region 引用与配置
        [Header("引用设置")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private EventSystem eventSystem; // UI事件系统（控制所有UI交互）

        [Header("射线检测设置")]
        [SerializeField] private LayerMask interactableLayers;
        [SerializeField] private float maxRayDistance = 100f;
        [SerializeField] private Color rayColor = Color.red; // 射线颜色

        [Header("射线平滑偏移设置")]
        [SerializeField] private float targetRayXOffset = 0f; // 目标X轴偏移量（世界空间）
        [SerializeField] private float targetRayYOffset = 0f; // 目标Y轴偏移量（世界空间）
        [SerializeField] private float targetRayZOffset = 0.5f; // 目标Z轴偏移量（世界空间）
        [SerializeField] private float smoothTime = 0.2f; // 平滑过渡时间（秒）

        [Header("拖拽平面设置")]
        [SerializeField] private Vector3 dragPlaneNormal = Vector3.up; // 拖拽平面法线（默认Y轴向上，即地面）
        [SerializeField] private float dragPlaneDistance = 0f; // 拖拽平面距离原点的距离（默认Y=0平面）

        private float currentRayXOffset = 0f; // 当前X轴偏移量（动态变化）
        private float currentRayYOffset = 0f; // 当前Y轴偏移量（动态变化）
        private float currentRayZOffset = 0f; // 当前Z轴偏移量（动态变化）

        private float rayXVelocity = 0f; // X轴平滑阻尼速度变量
        private float rayYVelocity = 0f; // Y轴平滑阻尼速度变量
        private float rayZVelocity = 0f; // Z轴平滑阻尼速度变量
        #endregion

        #region 状态与事件
        private GameObject currentInteractable;
        private IInteractable currentInteractableComponent;
        private bool isDragging = false;
        private Vector2 lastInputPosition;

        // 记录点击时的初始位置（用于判断是否发生移动）
        private Vector2 initialInputPosition;
        // 记录拖拽平面上的初始碰撞点（用于正确传递拖拽起点）
        private Vector3 initialDragWorldPos;
        // 标记是否是首次进入拖拽逻辑（避免重复激活）
        private bool isFirstDragFrame = false;

        private bool isInputEnabled = true;
        [Header("长按拖拽配置")]
        [SerializeField] private float longPressThreshold = 0.3f; // 长按判定阈值（秒）
        [SerializeField] private float DragThreshold = 5f; // 向上向下拖拽判定阈值（像素），可根据需求调整
        private float pressStartTime; // 按下开始的时间戳
        private bool isLongPressDetected = false; // 是否已判定为长按
        #endregion

        #region 生命周期
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
            if (mainCamera == null)
                mainCamera = Camera.main;

            // 自动查找EventSystem（未指定时）
            if (eventSystem == null)
                eventSystem = FindObjectOfType<EventSystem>();
            Application.targetFrameRate = 60;

            // 初始化射线偏移为0（确保拖拽开始无初始偏移）
            currentRayXOffset = 0f;
            currentRayYOffset = 0f;
            currentRayZOffset = 0f;
        }

        private void Update()
        {
            if (!isInputEnabled) return; // 输入整体禁用时，直接跳过

            // 处理射线偏移平滑过渡
            UpdateRayOffsetSmoothing();

            HandleMouseInput();
            HandleTouchInput();
            DrawRay(); // 始终绘制射线

        }

        /// <summary>
        /// 更新射线偏移的平滑过渡
        /// </summary>
        private void UpdateRayOffsetSmoothing()
        {
            // 根据拖拽状态设置目标偏移值
            float targetX = isDragging ? targetRayXOffset : 0f;
            float targetY = isDragging ? targetRayYOffset : 0f;
            float targetZ = isDragging ? targetRayZOffset : 0f;

            // 应用平滑阻尼过渡
            currentRayXOffset = Mathf.SmoothDamp(currentRayXOffset, targetX, ref rayXVelocity, smoothTime);
            currentRayYOffset = Mathf.SmoothDamp(currentRayYOffset, targetY, ref rayYVelocity, smoothTime);
            currentRayZOffset = Mathf.SmoothDamp(currentRayZOffset, targetZ, ref rayZVelocity, smoothTime);
        }
        #endregion

        #region 射线绘制
        private void DrawRay()
        {
            if (mainCamera != null)
            {
                // 适配鼠标/触摸：优先取当前输入位置，无输入时用上次位置
                Vector2 inputPosition = Input.mousePresent ? Input.mousePosition :
                                      (Input.touchCount > 0 ? Input.GetTouch(0).position : lastInputPosition);

                // 生成带偏移的射线
                Ray ray = GetOffsetRay(inputPosition);
                Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, rayColor);
            }
        }

        /// <summary>
        /// 获取带有偏移量的射线
        /// </summary>
        public Ray GetOffsetRay(Vector2 screenPosition)
        {
            Ray baseRay = mainCamera.ScreenPointToRay(screenPosition);
            // 应用当前平滑计算后的偏移量
            baseRay.origin += new Vector3(currentRayXOffset, currentRayYOffset, currentRayZOffset);
            return baseRay;
        }
        #endregion

        #region 输入处理（鼠标 + 触摸）
        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0)) HandleInputDown(Input.mousePosition);
            else if (Input.GetMouseButton(0)) HandleInputDrag(Input.mousePosition);
            else if (Input.GetMouseButtonUp(0)) HandleInputUp();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount <= 0) return;
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleInputDown(touch.position);
                    break;
                case TouchPhase.Moved:
                    HandleInputDrag(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleInputUp();
                    break;
            }
        }
        #endregion

        #region 输入核心逻辑
        private void HandleInputDown(Vector2 inputPosition)
        {
            //if (BlockGenerate.Instance != null && BlockGenerate.Instance.isSwitchingAnimationPlaying)
            //{
            //    return;
            //}

            Ray ray = GetOffsetRay(inputPosition);
            Debug.DrawRay(ray.origin, ray.direction * maxRayDistance, rayColor);

            // 1. 检测拖拽平面碰撞（获取真实拖拽起点）
            Plane dragPlane = new Plane(dragPlaneNormal, dragPlaneDistance);
            bool isHitDragPlane = dragPlane.Raycast(ray, out float dragPlaneEnterDistance);

            // 2. 检测积木碰撞（确保点击到可交互对象）
            if (Physics.Raycast(ray, out RaycastHit hitInfo, maxRayDistance, interactableLayers) && isHitDragPlane)
            {
                currentInteractable = hitInfo.collider.gameObject;
                currentInteractableComponent = currentInteractable.GetComponent<IInteractable>();
                initialInputPosition = inputPosition;
                lastInputPosition = inputPosition;
                // 记录拖拽平面上的初始世界坐标（关键修复：拖拽起点基于平面碰撞）
                initialDragWorldPos = ray.GetPoint(dragPlaneEnterDistance);
                isFirstDragFrame = true;
                isLongPressDetected = false; // 初始化长按状态
                pressStartTime = Time.time; // 记录按下时间戳

     
            }
        }
       
        private void HandleInputDrag(Vector2 inputPosition)
        {
            if (currentInteractable == null || currentInteractableComponent == null) return;

            // 首次进入拖拽时，判断是“垂直拖拽（上下）”还是“普通拖拽”
            if (isFirstDragFrame)
            {
                // 获取当前拖拽积木的BlockControl组件（用于获取isPlaced状态）
                BlockControl currentBlock = currentInteractable.GetComponent<BlockControl>();
                if (currentBlock == null)
                {
                    // 若未获取到组件，按未放置状态处理（或根据需求调整默认逻辑）
                    isFirstDragFrame = false;
                    return;
                }

                bool isVerticalDrag = true;
                // 仅当积木未放置时，判断垂直拖拽条件
                if (!currentBlock.isPlaced)
                {
                    isVerticalDrag = false;
                    // 计算X轴（左右）位移和Y轴（上下）位移
                    float dragXOffset = inputPosition.x - initialInputPosition.x;
                    float dragYOffset = inputPosition.y - initialInputPosition.y;
                    // 计算总移动距离（过滤微小抖动）
                    float moveDistance = Vector2.Distance(inputPosition, initialInputPosition);

                    // 1. 增强垂直主导判断：上下位移需大于左右
                    bool isVerticalDominant = Mathf.Abs(dragYOffset) > Mathf.Abs(dragXOffset);
                    // 2. 增加最小垂直位移阈值（过滤微小抖动，可调整）
                    bool isMinVerticalDisplacement = Mathf.Abs(dragYOffset) > DragThreshold * 1.5f;
                    // 3. 垂直拖拽条件：主导方向+最小位移+有效移动
                    isVerticalDrag = isVerticalDominant && isMinVerticalDisplacement && moveDistance > 0.3f;
                }else
                {
                    isVerticalDrag = true;
                }

                // 4. 普通拖拽条件：已放置的积木直接判断长按；未放置的积木需满足“非垂直拖拽+长按”
                bool isNormalLongPress = currentBlock.isPlaced
                    ? (Time.time - pressStartTime) >= longPressThreshold  // 已放置：仅判断长按
                    : !isVerticalDrag && (Time.time - pressStartTime) >= longPressThreshold;  // 未放置：非垂直+长按

                // 满足任一条件则开启拖拽
                if (isVerticalDrag)
                //if (isVerticalDrag || isNormalLongPress)
                {
                    isLongPressDetected = true;
                    isDragging = true;
                    isFirstDragFrame = false;
                    OnObjectDragStarted?.Invoke(currentInteractable, initialDragWorldPos);
                    currentInteractableComponent.OnDragStart(initialDragWorldPos);
                }
                return; // 未满足条件则直接返回，不执行拖拽逻辑
            }

            // 核心拖拽逻辑
            Plane dragPlane = new Plane(dragPlaneNormal, dragPlaneDistance);
            Ray currentRay = GetOffsetRay(inputPosition);
            if (!dragPlane.Raycast(currentRay, out float currentEnterDistance)) return;
            Vector3 currentWorldPos = currentRay.GetPoint(currentEnterDistance);

            Ray lastRay = GetOffsetRay(lastInputPosition);
            if (!dragPlane.Raycast(lastRay, out float lastEnterDistance)) return;
            Vector3 lastWorldPos = lastRay.GetPoint(lastEnterDistance);

            Vector3 delta = currentWorldPos - lastWorldPos;
            OnObjectDragged?.Invoke(currentInteractable, inputPosition, delta);
            currentInteractableComponent.OnDragged(inputPosition, delta);
            lastInputPosition = inputPosition;
        }

        private void HandleInputUp()
        {
            // 情况1：未触发拖拽（可能是短按）
            if (!isDragging)
            {
                // 判定条件：有交互对象 + 未判定为长按 + 按下时间<阈值
                if (currentInteractableComponent != null && !isLongPressDetected)
                {
                    float pressDuration = Time.time - pressStartTime;
                    if (pressDuration < longPressThreshold)
                    {
                        currentInteractableComponent.OnClick(); // 短按触发旋转（新增核心代码）
                        OnObjectClicked?.Invoke(currentInteractable); // 触发点击事件
                    }
                }

                // 重置状态
                currentInteractable = null;
                currentInteractableComponent = null;
                isFirstDragFrame = false;
                return;
            }

            // 情况2：正常结束拖拽（原有逻辑保留）
            if (currentInteractable != null && currentInteractableComponent != null)
            {
                OnObjectDragEnded?.Invoke(currentInteractable);
                currentInteractableComponent.OnDragEnd();
            }

            // 重置所有状态
            isDragging = false;
            currentInteractable = null;
            currentInteractableComponent = null;
            isFirstDragFrame = false;
            isLongPressDetected = false; // 重置长按状态
        }
        #endregion

        #region 输入控制方法
        public void SetInput(bool able)
        {
            isInputEnabled = able;
        }
        /// <summary>
        /// 禁用**所有输入**（游戏对象交互 + UI交互）
        /// </summary>
        public void DisableAllInput()
        {
            if (!isInputEnabled) return;

            if (isDragging) HandleInputUp(); // 先结束当前拖拽
            isInputEnabled = false;

            // 禁用UI交互：直接关闭EventSystem（所有UI元素将无法响应输入）
            if (eventSystem != null)
            {
                eventSystem.enabled = false;
                Debug.Log("[InputManager] UI输入已通过禁用EventSystem关闭");
            }
        }

        /// <summary>
        /// 启用**所有输入**（游戏对象交互 + UI交互）
        /// </summary>
        public void EnableAllInput()
        {
            if (isInputEnabled) return;

            isInputEnabled = true;

            // 启用UI交互：重新打开EventSystem
            if (eventSystem != null)
            {
                eventSystem.enabled = true;
                Debug.Log("[InputManager] UI输入已通过启用EventSystem恢复");
            }
        }

        public bool IsInputEnabled() => isInputEnabled;
        #endregion

        #region 交互接口
        public interface IInteractable
        {
            bool CanDrag();
            void OnClick();
            void OnDragStart(Vector3 hitPoint);
            void OnDragged(Vector2 screenPosition, Vector3 delta);
            void OnDragEnd();
        }
        #endregion

        #region 事件定义
        public event Action<GameObject> OnObjectClicked;
        public event Action<GameObject, Vector3> OnObjectDragStarted;
        public event Action<GameObject, Vector2, Vector3> OnObjectDragged;
        public event Action<GameObject> OnObjectDragEnded;
        #endregion
        // ：公开拖拽状态（供BlockGenerate判断，仅读不写）
        public bool IsDragging => isDragging;
    }
}