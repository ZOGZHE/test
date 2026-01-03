using UnityEngine;
using UnityEngine.EventSystems;

namespace WoolyPath
{
    /// <summary>
    /// 确保场景中有EventSystem来处理输入事件（如UI交互）
    /// 自动创建、配置EventSystem，适配不同输入系统
    /// </summary>
    public class EventSystemSetup : MonoBehaviour
    {
        [Header("EventSystem 设置")]
        [SerializeField] private bool createIfMissing = true; // 如果场景中没有EventSystem，是否自动创建
        [SerializeField] private bool sendNavigationEvents = true; // 是否发送导航事件（如键盘方向键导航UI）
        [SerializeField] private int pixelDragThreshold = 10; // 拖拽判定阈值（像素），超过此值才视为拖拽

        /// <summary>
        /// 唤醒时确保EventSystem存在
        /// </summary>
        private void Awake()
        {
            EnsureEventSystemExists();
        }

        /// <summary>
        /// 检查并确保EventSystem存在于场景中
        /// 存在则配置，不存在且允许创建则新建
        /// </summary>
        private void EnsureEventSystemExists()
        {
            // 查找场景中已有的EventSystem
            EventSystem eventSystem = FindObjectOfType<EventSystem>();

            // 如果没有EventSystem且允许创建，则创建新的
            if (eventSystem == null && createIfMissing)
            {
                Debug.Log("[EventSystemSetup] EventSystem不存在，正在创建...");
                CreateEventSystem();
            }
            // 如果已有EventSystem，则进行配置
            else if (eventSystem != null)
            {
               // Debug.Log("[EventSystemSetup] EventSystem已存在");
                ConfigureEventSystem(eventSystem);
            }
        }

        /// <summary>
        /// 创建新的EventSystem游戏对象及必要组件
        /// 自动适配新输入系统或传统输入系统
        /// </summary>
        private void CreateEventSystem()
        {
            // 创建EventSystem游戏对象
            GameObject eventSystemGO = new GameObject("EventSystem");
            // 给游戏对象添加EventSystem核心组件
            EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();

            // 添加输入模块（根据项目使用的输入系统选择）
#if ENABLE_INPUT_SYSTEM
            // 如果使用新的Input System（需要在Player Settings中启用）
            if (eventSystemGO.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
#else
            // 如果使用传统的Input Manager
            StandaloneInputModule inputModule = eventSystemGO.AddComponent<StandaloneInputModule>();
            ConfigureStandaloneInputModule(inputModule); // 配置传统输入模块
#endif

            // 配置EventSystem的属性
            ConfigureEventSystem(eventSystem);

            Debug.Log("[EventSystemSetup] EventSystem创建成功");
        }

        /// <summary>
        /// 配置EventSystem的基础属性
        /// </summary>
        /// <param name="eventSystem">需要配置的EventSystem实例</param>
        private void ConfigureEventSystem(EventSystem eventSystem)
        {
            eventSystem.sendNavigationEvents = sendNavigationEvents; // 设置是否发送导航事件
            eventSystem.pixelDragThreshold = pixelDragThreshold; // 设置拖拽判定阈值
        }

        /// <summary>
        /// 配置传统输入模块（StandaloneInputModule）的输入轴和按钮映射
        /// </summary>
        /// <param name="inputModule">需要配置的StandaloneInputModule实例</param>
        private void ConfigureStandaloneInputModule(StandaloneInputModule inputModule)
        {
            inputModule.horizontalAxis = "Horizontal"; // 水平导航轴（对应Input Manager中的设置）
            inputModule.verticalAxis = "Vertical"; // 垂直导航轴
            inputModule.submitButton = "Submit"; // 提交按钮（如Enter键）
            inputModule.cancelButton = "Cancel"; // 取消按钮（如Esc键）
        }

        /// <summary>
        /// 在编辑器中验证设置（不运行时也会执行）
        /// 提示用户场景中是否缺少EventSystem
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying) return; // 运行时不执行

#if UNITY_EDITOR
            // 编辑器中检查EventSystem是否存在
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null && createIfMissing)
            {
                Debug.LogWarning("[EventSystemSetup] 场景中没有EventSystem，运行时将自动创建");
            }
#endif
        }

        /// <summary>
        /// 静态方法：获取已有的EventSystem，若不存在则创建新的
        /// 供外部脚本快速获取EventSystem实例
        /// </summary>
        /// <returns>场景中的EventSystem实例</returns>
        public static EventSystem GetOrCreateEventSystem()
        {
            // 查找已有的EventSystem
            EventSystem eventSystem = FindObjectOfType<EventSystem>();

            // 如果不存在则创建
            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
                // 添加新输入系统的UI输入模块
                go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                // 添加传统输入模块
                go.AddComponent<StandaloneInputModule>();
#endif

                Debug.Log("[EventSystemSetup] EventSystem动态创建成功");
            }

            return eventSystem;
        }
    }
}