using UnityEngine;

namespace WoolyPath.Data
{
    // 允许在Unity编辑器中创建该ScriptableObject的实例
    // fileName: 创建时的默认文件名；menuName: 在Assets菜单中的路径
    [CreateAssetMenu(fileName = "GameSettings", menuName = "WoolyPath/游戏设置")]
    public class GameSettings : ScriptableObject
    {
        [Header("游戏基础设置")]
        [SerializeField] private string gameTitle = "WoolyPath"; // 游戏标题
        [SerializeField] private string version = "1.0.0"; // 游戏版本号
        [SerializeField] private int targetFrameRate = 60; // 目标帧率

        [Header("关卡设置")]
        [SerializeField] private int totalLevels = 10; // 总关卡数量
        [SerializeField] private float levelStartDelay = 1f; // 关卡开始前的延迟时间（秒）
        [SerializeField] private float levelCompleteDelay = 2f; // 关卡完成后的延迟时间（秒）

        [Header("音频设置")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f; // 主音量（0-1范围）
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f; // 音乐音量（0-1范围）
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f; // 音效音量（0-1范围）

        [Header("输入设置")]
        [SerializeField] private float touchSensitivity = 1f; // 触摸灵敏度
        [SerializeField] private float dragThreshold = 10f; // 拖拽识别的阈值（像素）
        [SerializeField] private LayerMask interactableLayerMask = -1; // 可交互物体的图层遮罩

        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = true; // 是否启用调试日志
        [SerializeField] private bool enableDebugGizmos = false; // 是否启用调试Gizmos
        [SerializeField] private bool showFPS = false; // 是否显示帧率

        // 公共属性（只读），供其他脚本访问设置
        public string GameTitle => gameTitle;
        public string Version => version;
        public int TargetFrameRate => targetFrameRate;

        public int TotalLevels => totalLevels;
        public float LevelStartDelay => levelStartDelay;
        public float LevelCompleteDelay => levelCompleteDelay;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        public float TouchSensitivity => touchSensitivity;
        public float DragThreshold => dragThreshold;
        public LayerMask InteractableLayerMask => interactableLayerMask;

        public bool EnableDebugLog => enableDebugLog;
        public bool EnableDebugGizmos => enableDebugGizmos;
        public bool ShowFPS => showFPS;

        // 运行时更新主音量（限制在0-1范围）
        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
        }

        // 运行时更新音乐音量（限制在0-1范围）
        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
        }

        // 运行时更新音效音量（限制在0-1范围）
        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        // 运行时更新触摸灵敏度（最小0.1）
        public void SetTouchSensitivity(float value)
        {
            touchSensitivity = Mathf.Max(0.1f, value);
        }

        // 验证设置（在编辑器中修改值时自动调用，确保数值合法）
        private void OnValidate()
        {
            targetFrameRate = Mathf.Max(30, targetFrameRate); // 帧率不低于30
            totalLevels = Mathf.Max(1, totalLevels); // 关卡数不低于1
            levelStartDelay = Mathf.Max(0f, levelStartDelay); // 延迟时间不小于0
            levelCompleteDelay = Mathf.Max(0f, levelCompleteDelay);
            touchSensitivity = Mathf.Max(0.1f, touchSensitivity); // 灵敏度不低于0.1
            dragThreshold = Mathf.Max(1f, dragThreshold); // 拖拽阈值不低于1
        }
    }
}