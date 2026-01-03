using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WoolyPath
{
    // 游戏场景启动核心调度脚本，协调管理器初始化、对象创建、预制体绑定，确保启动流程有序
    public class GameSceneSetup : MonoBehaviour
    {
        #region 序列化字段 - 核心管理器引用
        [Header("核心管理器引用（场景运行必需）")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private InputManager inputManager;
        #endregion

        #region 关卡配置
        [Header("关卡配置")]
        [SerializeField] private LevelData[] levelDataArray; // 手动配置的关卡数组（可选）
        [SerializeField] private bool autoLoadLevels = true; // 是否自动加载Data/Levels文件夹中的关卡
        #endregion

        #region 序列化字段 - 场景层级管理（父节点）
        [Header("场景对象父节点（用于Hierarchy整洁）")]
        [SerializeField] private Transform sheepParent;
        [SerializeField] private Transform collectorsParent;
        [SerializeField] private Transform effectsParent;
        #endregion

        #region 序列化字段 - 系统组件与预制体
        [Header("传送带系统（羊毛运输核心）")]
        [SerializeField] private ConveyorBelt conveyorBelt;

        [Header("动态生成预制体（必需配置）")]
        [SerializeField] private GameObject sheepPrefab;
        [SerializeField] private GameObject collectorPrefab;
        [SerializeField] private GameObject woolPrefab;
        #endregion

        #region 生命周期方法 - 初始化流程
        private void Awake()
        {
            InitializeScene(); // 执行场景基础初始化
        }

        private void Start()
        {
            SetupGame(); // 执行游戏启动配置
        }
        #endregion

        #region 场景初始化核心逻辑
        // 场景初始化入口（Awake执行）：创建父节点+验证核心组件，避免空引用
        private void InitializeScene()
        {
            CreateParentObjects(); // 确保父节点存在
            ValidateRequiredComponents(); // 验证核心组件
            LoadLevelData(); // 加载关卡数据
        }

        // 创建必需父节点（仅不存在时创建）：统一管理同类对象，避免Hierarchy杂乱
        private void CreateParentObjects()
        {
            // 创建羊的父节点
            if (sheepParent == null)
            {
                GameObject sheepParentGO = new GameObject("Sheep"); // 节点命名便于识别
                sheepParent = sheepParentGO.transform;
            }

            // 创建收集器的父节点
            if (collectorsParent == null)
            {
                GameObject collectorsParentGO = new GameObject("Collectors");
                collectorsParent = collectorsParentGO.transform;
            }

            // 创建特效的父节点
            if (effectsParent == null)
            {
                GameObject effectsParentGO = new GameObject("Effects");
                effectsParent = effectsParentGO.transform;
            }
        }

        // 验证核心组件（避免空引用）：空则查找场景实例，失败按重要性触发Error/Warning
        private void ValidateRequiredComponents()
        {
            // 验证GameManager（必需，缺失无法启动）
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager == null)
                {
                    Debug.LogError("[GameSceneSetup] 未找到GameManager！请在场景中添加实例。");
                }
            }

            // 验证LevelManager（必需，缺失无法加载关卡）
            if (levelManager == null)
            {
                levelManager = FindObjectOfType<LevelManager>();
                if (levelManager == null)
                {
                    Debug.LogError("[GameSceneSetup] 未找到LevelManager！请在场景中添加实例。");
                }
            }

            // 验证ConveyorBelt（核心，缺失影响玩法）
            if (conveyorBelt == null)
            {
                conveyorBelt = FindObjectOfType<ConveyorBelt>();
                if (conveyorBelt == null)
                {
                    Debug.LogWarning("[GameSceneSetup] 未找到ConveyorBelt！可启用CreateDefaultConveyorBelt创建。");
                    //CreateDefaultConveyorBelt(); // 如需自动创建，取消注释
                }
            }
        }      


        #endregion

        #region 关卡数据加载
        // 加载关卡数据（支持自动加载和手动配置）
        private void LoadLevelData()
        {
            if (autoLoadLevels)
            {
                LoadLevelsFromAssets();
            }
            else if (levelDataArray != null && levelDataArray.Length > 0)
            {
                // 使用手动配置的关卡数组
                SetLevelsToManager(levelDataArray);
            }
            else
            {
                Debug.LogWarning("[GameSceneSetup] 未配置关卡数据！请启用自动加载或手动配置关卡数组。");
            }
        }

        // 从Assets/Data/Levels文件夹自动加载所有关卡
        private void LoadLevelsFromAssets()
        {
#if UNITY_EDITOR
            // 编辑器模式下直接从Assets文件夹加载
            List<LevelData> levelsList = new List<LevelData>();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Data/Levels" });

            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    levelsList.Add(level);
                    Debug.Log($"[GameSceneSetup] 加载关卡: {level.name} from {path}");
                }
            }

            // 按名称排序（Level01, Level02, ...）
            var sortedLevels = levelsList.OrderBy(level =>
            {
                string levelName = level.name;
                string numberStr = System.Text.RegularExpressions.Regex.Match(levelName, @"\d+").Value;
                if (int.TryParse(numberStr, out int levelNumber))
                {
                    return levelNumber;
                }
                return 999;
            }).ToArray();

            // 更新索引
            for (int i = 0; i < sortedLevels.Length; i++)
            {
                sortedLevels[i].levelIndex = i;
                sortedLevels[i].levelName = $"Level {i + 1}";
            }

            levelDataArray = sortedLevels;
            Debug.Log($"[GameSceneSetup] 自动加载 {sortedLevels.Length} 个关卡（编辑器模式）");
            SetLevelsToManager(sortedLevels);
#else
            // 运行时模式下从Resources文件夹加载
            // 注意：需要将关卡文件放到Resources文件夹中才能在运行时加载
            LevelData[] loadedLevels = Resources.LoadAll<LevelData>("Levels");

            if (loadedLevels == null || loadedLevels.Length == 0)
            {
                Debug.LogWarning("[GameSceneSetup] 未找到关卡数据！请将关卡文件放到 Resources/Levels 文件夹中。");
                return;
            }

            // 按名称排序
            var sortedLevels = loadedLevels.OrderBy(level =>
            {
                string levelName = level.name;
                string numberStr = System.Text.RegularExpressions.Regex.Match(levelName, @"\d+").Value;
                if (int.TryParse(numberStr, out int levelNumber))
                {
                    return levelNumber;
                }
                return 999;
            }).ToArray();

            // 更新索引
            for (int i = 0; i < sortedLevels.Length; i++)
            {
                sortedLevels[i].levelIndex = i;
                sortedLevels[i].levelName = $"Level {i + 1}";
            }

            levelDataArray = sortedLevels;
            Debug.Log($"[GameSceneSetup] 自动加载 {sortedLevels.Length} 个关卡（运行时模式）");
            SetLevelsToManager(sortedLevels);
#endif
        }

        // 将关卡数据设置到LevelManager
        private void SetLevelsToManager(LevelData[] levels)
        {
            if (levelManager != null && levels != null && levels.Length > 0)
            {
                levelManager.SetLevels(levels);
                Debug.Log($"[GameSceneSetup] 已设置 {levels.Length} 个关卡到 LevelManager");
            }
            else
            {
                Debug.LogWarning("[GameSceneSetup] 无法设置关卡数据到 LevelManager");
            }
        }
        #endregion

        #region 关卡管理器配置
      private void SetupGame()
        {
            SetupLevelManager(); // 配置LevelManager
            SetupInputSystem(); // 配置输入系统
        }
        private void SetupLevelManager()
        {
            if (levelManager == null) return; // 引用为空则退出，避免报错

            // 1. 绑定预制体引用（动态生成对象的模板）
            if (sheepPrefab != null && collectorPrefab != null)
            {
                levelManager.SetPrefabs(sheepPrefab, collectorPrefab);
            }
            else
            {
                Debug.LogWarning("[GameSceneSetup] 羊/收集器预制体未赋值！可能导致生成失败。");
            }

            // 2. 绑定父节点引用（控制生成对象的层级）
            if (sheepParent != null && collectorsParent != null)
            {
                levelManager.SetParentReferences(sheepParent, collectorsParent);
            }
        }//设置父节点
        // 初始化输入系统：检查引用，空则查找，确保输入能被正确处理
        private void SetupInputSystem()
        {
            if (inputManager == null)
            {
                inputManager = FindObjectOfType<InputManager>();
                if (inputManager == null)
                {
                    Debug.LogWarning("[GameSceneSetup] 未找到InputManager！玩家输入可能无法处理。");
                }
            }
        }
        #endregion

        #region 辅助功能

        // 自动创建默认传送带（场景无ConveyorBelt时）：生成矩形路径，反射设置私有字段
        private void CreateDefaultConveyorBelt()
        {
            GameObject conveyorGO = new GameObject("ConveyorBelt"); // 创建传送带根对象
            conveyorBelt = conveyorGO.AddComponent<ConveyorBelt>(); // 添加核心组件

            // 定义默认路径点位置（矩形路径，4个点）
            Transform[] defaultPath = new Transform[4];
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-2f, 0.1f, -2f),  // 路径点0：左下
                new Vector3(2f, 0.1f, -2f),   // 路径点1：右下
                new Vector3(2f, 0.1f, 2f),    // 路径点2：右上
                new Vector3(-2f, 0.1f, 2f)    // 路径点3：左上
            };

            // 生成路径点并设置父子关系
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{i}"); // 路径点命名：Waypoint_索引
                waypoint.transform.position = positions[i]; // 设置位置
                waypoint.transform.parent = conveyorGO.transform; // 作为传送带子节点
                defaultPath[i] = waypoint.transform; // 存入路径数组
            }

            // 反射访问ConveyorBelt私有字段beltPath并赋值
            System.Type conveyorType = typeof(ConveyorBelt);
            System.Reflection.FieldInfo pathField = conveyorType.GetField(
                "beltPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (pathField != null)
            {
                pathField.SetValue(conveyorBelt, defaultPath); // 为私有字段赋值
            }

            Debug.Log("[GameSceneSetup] 成功创建默认传送带（矩形路径），可调整路径点位置。");
        }


        #endregion

        #region 公共访问器（只读）
        // 羊父节点只读访问器：提供外部访问，防止直接修改
        public Transform SheepParent => sheepParent;
        // 收集器父节点只读访问器：提供外部访问，防止直接修改
        public Transform CollectorsParent => collectorsParent;
        // 特效父节点只读访问器：提供外部访问，防止直接修改
        public Transform EffectsParent => effectsParent;
        // 羊预制体只读访问器：提供外部访问，防止直接修改
        public GameObject SheepPrefab => sheepPrefab;
        // 收集器预制体只读访问器：提供外部访问，防止直接修改
        public GameObject CollectorPrefab => collectorPrefab;
        // 羊毛预制体只读访问器：提供外部访问，防止直接修改
        public GameObject WoolPrefab => woolPrefab;
        #endregion
    }
}