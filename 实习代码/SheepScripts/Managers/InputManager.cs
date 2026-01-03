using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems; //引入UI事件系统命名空间

// 命名空间：WoolyPath（羊毛路径），用于区分游戏中的其他代码
namespace WoolyPath
{
    // 输入管理器：负责处理所有玩家输入（鼠标/触摸），检测与羊的交互并触发相应事件
    public class InputManager : MonoBehaviour
    {
      
        public static InputManager Instance { get; private set; }
        // 缓存EventSystem实例（UI输入的核心组件）
        private EventSystem _eventSystem;

        [Header("输入设置")]
        [SerializeField] private LayerMask sheepLayerMask = -1; // 用于检测羊的图层掩码（-1表示检测所有图层）
        [SerializeField] private float raycastDistance = 100f; // 射线检测的最大距离
        [SerializeField] private bool showDebugRays = false;   // 是否显示调试射线


        [Header("点击反馈")]
        [SerializeField] private GameObject clickEffectPrefab; // 点击效果预制体
        [SerializeField] private float clickEffectDuration = 0.5f; // 点击效果持续时间

        private Camera mainCamera; // 主相机引用（用于射线检测）
        private bool inputEnabled = true; // 输入是否启用的开关

        // 唤醒时初始化单例
        private void Awake()
        {
            // 单例模式：如果实例不存在则创建，否则销毁重复对象
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
            // 1. 初始化相机（原有逻辑）
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("[InputManager] 未找到相机！");
            }

            // 2.初始化EventSystem（获取场景中的UI事件系统）
            _eventSystem = FindObjectOfType<EventSystem>();
            if (_eventSystem == null)
            {
                Debug.LogError("[InputManager] 场景中未找到EventSystem！UI输入无法正常工作，请在场景中添加EventSystem（右键→UI→Event System）");
            }
        }

        // 每帧更新时处理输入
        private void Update()
        {
            // 如果输入被禁用或游戏管理器不存在，则不处理输入
            if (!inputEnabled || GameManager.Instance == null) return;

            // 只在游戏处于"游玩中"状态时处理输入
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            // 处理鼠标输入
            HandleMouseInput();
            // 处理触摸输入（移动设备）
            HandleTouchInput();
        }

        // 处理鼠标输入
        private void HandleMouseInput()
        {
            // 检测左键点击（0表示左键）
            if (Input.GetMouseButtonDown(0))
            {

                // 获取鼠标在屏幕上的位置
                Vector3 mousePosition = Input.mousePosition;
                // 处理该屏幕位置的点击
                HandleClickAtScreenPosition(mousePosition);
            }
        }

        // 处理触摸输入（移动设备）
        private void HandleTouchInput()
        {
            // 检测是否有触摸
            if (Input.touchCount > 0)
            {
                // 获取第一个触摸点（支持单点触摸）
                Touch touch = Input.GetTouch(0);

                // 检测触摸开始（类似鼠标按下）
                if (touch.phase == TouchPhase.Began)
                {
                    // 获取触摸在屏幕上的位置
                    Vector3 touchPosition = touch.position;
                    // 处理该屏幕位置的点击
                    HandleClickAtScreenPosition(touchPosition);
                }
            }
        }

        // 处理屏幕位置的点击（统一处理鼠标和触摸输入）
        private void HandleClickAtScreenPosition(Vector3 screenPosition)
        {
            // 如果相机不存在，直接返回
            if (mainCamera == null) return;

            // 从相机向屏幕位置发射射线
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);

            // 如果启用调试射线，在场景中绘制射线并输出日志
            if (showDebugRays)
            {
                //Debug.DrawRay(ray.origin, ray.direction * raycastDistance, UnityEngine.Color.red, 1f);
               // Debug.Log($"[InputManager] 射线检测: {ray.origin} -> {ray.direction}");
            }

            // 执行射线检测：检测指定距离内的指定图层（羊的图层）
            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, sheepLayerMask))
            {
                // 处理射线击中的结果
                HandleRaycastHit(hit, screenPosition);
            }
            // 如果未击中且启用调试，输出日志
            else if (showDebugRays)
            {
                Debug.Log($"[InputManager] 射线未击中任何物体，图层掩码: {sheepLayerMask.value}");
            }
        }

        // 处理射线击中的物体
        private void HandleRaycastHit(RaycastHit hit, Vector3 screenPosition)
        {
            // 调试日志：输出击中的物体名称和位置
            if (showDebugRays)
            {
                Debug.Log($"[InputManager] 射线击中: {hit.collider.name} 在 {hit.point}");
            }

            // 显示点击效果
            ShowClickEffect(hit.point);

            // 检查是否击中了羊（获取碰撞体上的SheepController组件）
            SheepController sheep = hit.collider.GetComponent<SheepController>();
            if (sheep != null)
            {
                // 处理羊被点击的逻辑
                OnSheepClicked(sheep, hit.point);
                return;
            }

            // 如果当前物体没有羊组件，尝试从父物体中查找
            sheep = hit.collider.GetComponentInParent<SheepController>();
            if (sheep != null)
            {
                OnSheepClicked(sheep, hit.point);
                return;
            }

            // 其他可交互物体的处理（预留扩展：如UI元素、特殊物体等）
            Debug.Log($"[InputManager] 点击了非羊物体: {hit.collider.name}");
        }

        // 显示点击效果
        private void ShowClickEffect(Vector3 worldPosition)
        {
            // 优先使用效果管理器播放点击效果（推荐方式，便于集中管理效果）
            if (EffectsManager.Instance != null)
            {
                EffectsManager.Instance.PlayClickEffect(worldPosition);
            }
            // 如果效果管理器不存在，使用传统方式实例化预制体
            else if (clickEffectPrefab != null)
            {
                // 在点击位置实例化效果预制体
                GameObject effect = Instantiate(clickEffectPrefab, worldPosition, Quaternion.identity);

                // 如果设置了持续时间，自动销毁效果物体
                if (clickEffectDuration > 0)
                {
                    Destroy(effect, clickEffectDuration);
                }
            }
        }

        // 羊被点击时的处理逻辑
        // 在InputManager的OnSheepClicked方法中，在调用其他逻辑之前添加：
        private void OnSheepClicked(SheepController sheep, Vector3 hitPoint)
        {
            // 安全检查：如果羊组件为空，直接返回
            if (sheep == null) return;

            // 如果在交换模式下，优先处理交换逻辑
            if (SwapManager.Instance != null && SwapManager.Instance.IsInSwapMode())
            {
                SwapManager.Instance.SelectSheep(sheep);
                return; // 在交换模式下，不执行其他点击逻辑
            }

            
            if (showDebugRays)
            {
                Debug.Log($"[InputManager] 羊被点击: {sheep.name} 在位置 {hitPoint}");
            }

            // 播放增强的点击效果（带有羊的颜色）
            if (EffectsManager.Instance != null)
            {
                EffectsManager.Instance.PlayEnhancedClickEffect(hitPoint);
            }

            // 先通知关卡管理器（关卡逻辑优先）
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.OnSheepClicked(sheep);
            }

            // 触发羊被点击的全局事件（供其他系统监听）
            GameEvents.TriggerSheepClicked(sheep, hitPoint);

            
        }

        // 启用输入
        public void EnableInput()
        {
            // 1. 恢复“羊的输入”（原有逻辑）
            inputEnabled = true;
            Debug.Log("[InputManager] 游戏输入（羊）已启用");

            // 2. 恢复“UI输入”（启用EventSystem）
            if (_eventSystem != null && !_eventSystem.enabled)
            {
                _eventSystem.enabled = true;
                Debug.Log("[InputManager] UI输入（EventSystem）已启用");
            }
        }

        // 禁用输入（如暂停时）
        public void DisableInput()
        {
            // 1. 屏蔽“羊的输入”（原有逻辑）
            inputEnabled = false;
            Debug.Log("[InputManager] 游戏输入（羊）已禁用");

            // 2. 屏蔽“UI输入”（禁用EventSystem）
            if (_eventSystem != null && _eventSystem.enabled)
            {
                _eventSystem.enabled = false;
                Debug.Log("[InputManager] UI输入（EventSystem）已禁用");
            }
        }

        // 检查输入是否启用
        public bool IsInputEnabled()
        {
            return inputEnabled;
        }

        // 在编辑器中绘制Gizmos（便于调试射线）
        private void OnDrawGizmosSelected()
        {
            // 如果相机存在，在编辑器中可视化射线
            if (mainCamera != null)
            {
                Gizmos.color = UnityEngine.Color.red;
                Vector3 mousePos = Input.mousePosition;
                Ray ray = mainCamera.ScreenPointToRay(mousePos);
                Gizmos.DrawRay(ray.origin, ray.direction * raycastDistance);
            }
        }
    }
}