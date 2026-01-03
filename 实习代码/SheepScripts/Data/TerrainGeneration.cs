using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    public class TerrainGeneration : MonoBehaviour
    {
        public static TerrainGeneration Instance { get; private set; }
        [Header("关卡数据管理")]
        [SerializeField] private LevelData[] levels; // 保留用于兼容性，但优先使用LevelManager的数据
        [SerializeField] public LevelData currentLevelData;
        [HideInInspector] public GameObject _mapInstance;
        public GameObject[] ConveyorbeltModel;

        [Header("地形变换配置")]
        [Tooltip("地形的位置")]
        [SerializeField] public Vector3 terrainPosition;
        [Tooltip("地形的旋转角度（欧拉角，单位角度）")]
        [SerializeField] public Vector3 terrainRotation;
        [Tooltip("地形的缩放比例")]
        [SerializeField] public Vector3 terrainScale;  

        private Dictionary<int, GameObject> TerrainMapping = new Dictionary<int, GameObject>();
        private int index;
        

        private void Awake()
        {
            // 单例模式确保全局唯一实例
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 如场景切换需要保持，否则可能导致切换关卡后实例丢失
            }
            else
            {
                Destroy(gameObject);
            }
            Initializethedictionary();
        }

        void Start()
        {
            // 直接调用刷新方法，统一初始化逻辑
            RefreshTerrain();
        }



        private void Initializethedictionary()
        {
            // 清空字典以避免重复添加
            TerrainMapping.Clear();

            // 安全添加地形映射，检查数组边界
            if (ConveyorbeltModel != null && ConveyorbeltModel.Length >= 4)
            {
                TerrainMapping.Add(1, ConveyorbeltModel[0]);
                TerrainMapping.Add(2, ConveyorbeltModel[1]);
                TerrainMapping.Add(3, ConveyorbeltModel[2]);
                TerrainMapping.Add(4, ConveyorbeltModel[3]);
            }
            else
            {
                Debug.LogWarning($"[TerrainGeneration] ConveyorbeltModel 数组未正确配置，需要至少 4 个元素，当前: {ConveyorbeltModel?.Length ?? 0}");
            }
        }

        private void InitializedLevelData()
        {
            // 优先从 LevelManager 获取当前关卡数据
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelData != null)
            {
                currentLevelData = LevelManager.Instance.CurrentLevelData;
            }
            // 如果 LevelManager 没有数据，尝试使用本地配置的 levels 数组（兼容旧版本）
            else if (levels != null && levels.Length > 0 && GameManager.Instance != null)
            {
                int levelIndex = GameManager.Instance.CurrentLevel;
                if (levelIndex >= 0 && levelIndex < levels.Length)
                {
                    currentLevelData = levels[levelIndex];
                }
                else
                {
                    Debug.LogError($"[TerrainGeneration] 关卡索引 {levelIndex} 超出范围 (0-{levels.Length - 1})");
                    return;
                }
            }
            else
            {
                Debug.LogError("[TerrainGeneration] 无法获取关卡数据！请确保 LevelManager 或本地 levels 数组已配置。");
                return;
            }

            // 检查地形预设是否存在
            if (currentLevelData._terrainPreset == null)
            {
                Debug.LogWarning($"[TerrainGeneration] 关卡 {currentLevelData.name} 没有配置地形预设，使用默认值");
                // 使用默认值
                index = 1;
                terrainPosition = Vector3.zero;
                terrainRotation = Vector3.zero;
                terrainScale = Vector3.one;
            }
            else
            {
                index = currentLevelData._terrainPreset.mapType;
                terrainPosition = currentLevelData._terrainPreset.GenerationLocation;
                terrainRotation = currentLevelData._terrainPreset.GenerationRotation;
                terrainScale = currentLevelData._terrainPreset.GenerationScale;
            }

            // 确保缩放值有效
            if (terrainScale == Vector3.zero)
            {
                terrainScale = Vector3.one;
            }
        }

        private void GenerationTerrain(int index, Vector3 CreatePosition)
        {
            // 生成前检查：预制体是否存在，避免意外错误
            if (!TerrainMapping.ContainsKey(index))
            {
                Debug.LogError($"[TerrainGeneration] 地形类型索引 {index} 未在字典中配置！");
                return;
            }

            var mapPrefab = TerrainMapping[index];
            if (mapPrefab == null)
            {
                Debug.LogError($"[TerrainGeneration] 地形类型 {index} 的预制体为空！");
                return;
            }

            _mapInstance = Instantiate(
                mapPrefab,
                CreatePosition,
                Quaternion.Euler(terrainRotation), // 应用旋转角度
                transform
            );

            // 应用缩放比例
            _mapInstance.transform.localScale = terrainScale;
        }

        // 销毁上一关卡的旧地形
        private void DestroyOldTerrain()
        {
            if (_mapInstance != null)
            {
                Destroy(_mapInstance); // 运行时用Destroy（而非DestroyImmediate，潜在风险）
                _mapInstance = null; // 置空引用，防止重复销毁
            }
        }

        // 核心的地形刷新方法（由LevelManager调用，主要接口）
        public void RefreshTerrain()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[TerrainGeneration] GameManager实例不存在，无法刷新地形！");
                return;
            }

            DestroyOldTerrain(); // 第一步：销毁旧地形
            InitializedLevelData(); // 第二步：重新初始化当前关卡的地形数据

            // 第三步：生成新地形（只有当有有效数据时）
            if (currentLevelData != null)
            {
                GenerationTerrain(index, terrainPosition);
            }
        }

        // 编辑器工具：更新地形变换的方法（修改参数后手动调用此方法刷新）
        [ContextMenu("刷新地形变换")]
        public void UpdateTerrainTransform()
        {
            if (_mapInstance != null)
            {
                _mapInstance.transform.rotation = Quaternion.Euler(terrainRotation);
                _mapInstance.transform.localScale = terrainScale;
            }
        }
    }
}
