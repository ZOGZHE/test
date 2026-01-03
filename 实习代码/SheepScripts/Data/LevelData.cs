using System.Collections.Generic;
using UnityEngine;
using WoolyPath;

// 游戏命名空间
namespace WoolyPath
{

    [System.Serializable]
    public class SheepData
    {
        // 绵羊的羊毛颜色（默认绿色）
        [Tooltip("绵羊的羊毛颜色，默认值为绿色")]
        public WoolColor color = WoolColor.Green;

       // 绵羊在网格中的位置（整数坐标）
       [Tooltip("绵羊在游戏网格中的整数坐标（X=列，Y=行），范围受关卡网格尺寸限制")]
        public Vector2Int gridPosition = Vector2Int.zero;

        // 绵羊是否激活（是否在场景中显示/参与游戏）
        [Tooltip("控制绵羊是否在场景中显示并参与游戏逻辑，未激活则隐藏且不生效")]
        public bool isActive = true;
        // 黑羊遮罩状态（true=带有黑羊遮罩，外观显示黑；false=无遮罩，显示原颜色）
        public bool isblackMasked=false;

        public SheepData(WoolColor color, Vector2Int gridPosition, bool isActive = true, bool isblackMasked = false)
        {
            this.color = color;
            this.gridPosition = gridPosition;
            this.isActive = isActive;
            this.isblackMasked = isblackMasked;
        }

        public SheepData(WoolColor color, int x, int y, bool isActive = true)
        {
            this.color = color;
            this.gridPosition = new Vector2Int(x, y);
            this.isActive = isActive;
        }
        public SheepData() { }
        public SheepData Clone()
        {
            return new SheepData
            {
                color = this.color,
                gridPosition = this.gridPosition,
                isActive = this.isActive,
                isblackMasked = this.isblackMasked
                // 复制其他需要的字段
            };
        }

    }

    [System.Serializable]
    public class CollectorPrefabCollection
    {
        [Tooltip("按颜色分组的预制体数组")]
        public CollectorCapacityPrefabs[] _colorPrefabs;
        
    }
    [System.Serializable]
    public class CollectorCapacityPrefabs
    {
        public WoolColor color;
        [Tooltip("该颜色下不同容量的预制体数组（容量1对应索引0）")]
        public GameObject[] _capacityPrefabs;
    }

    [System.Serializable]
    public class CollectorData
    {
        // 收集器的目标羊毛颜色（默认绿色）
        [Tooltip("收集器仅能收集的羊毛颜色，默认值为绿色")]
        public WoolColor targetColor = WoolColor.Pink;

        // 收集器的容量（可收集的羊毛数量）
        [Tooltip("收集器最多能容纳的羊毛数量，达到容量则视为装满")]
        public int capacity = 1;

        // 收集器在3D世界中的位置
        [Tooltip("收集器在场景中的3D世界坐标（Y轴通常为高度）")]
        public Vector3 position = Vector3.zero;

        // 引用预制体集合
        [Tooltip("收集器预制体集合，包含不同容量对应的预制体")]
        public CollectorPrefabCollection prefabCollection;

        [Tooltip("收集器层级")]
        public int Tier=1;


        // 获取对应容量的预制体
        public GameObject Prefab
        {
            get
            {
                if (prefabCollection == null || prefabCollection._colorPrefabs == null)
                    return null;

                // 查找对应颜色的预制体数组
                foreach (var colorPrefab in prefabCollection._colorPrefabs)
                {
                    if (colorPrefab.color == targetColor)
                    {
                        // 找到颜色后，按容量查找
                        int index = capacity - 1;
                        if (index >= 0 && index < colorPrefab._capacityPrefabs.Length)
                            return colorPrefab._capacityPrefabs[index];
                        else
                        {
                            Debug.LogWarning($"容量 {capacity} 超出范围，使用最大可用容量");
                            return colorPrefab._capacityPrefabs[colorPrefab._capacityPrefabs.Length - 1];
                        }
                    }
                }

                Debug.LogWarning($"未找到颜色为 {targetColor} 的收集器");
                return null;
            }
        }

        public CollectorData()
        {
            // 空构造函数，用于序列化
        }

        public CollectorData(WoolColor targetColor, int capacity, Vector3 position)
        {
            this.targetColor = targetColor;
            this.capacity = capacity;
            this.position = position;
            
        }
        public CollectorData(WoolColor targetColor, int capacity, Vector3 position, CollectorPrefabCollection prefabs)
        {
            this.targetColor = targetColor;
            this.capacity = capacity;
            this.position = position;
            this.prefabCollection = prefabs;
        }
    }
    [System.Serializable] 
    public class SheepPrefabsWeight
    {
        [Tooltip("羊的颜色（与WoolColor枚举对应）")]
        public WoolColor color;

        [Tooltip("该颜色对应的羊预制体")]
        public GameObject Prefab;

        [Tooltip("生成权重（值越大，生成概率越高）")]
        public float weight = 1f;
        [Tooltip("生成数量（值越大，生成数量越多）")]
        public int Quantity = 1;
        // 收集器颜色匹配开关
        [Tooltip("打开后，遍历收集器各个颜色的总容量赋值给对应SheepPrefabsWeight的Quantity")]
        public bool useCollectorColorMatch = false;
        public WoolColor GetSheepPrefabsColors()
        {
            return color;
        }
    }
    [System.Serializable] 
    public class BlackSheepMask 
    {
        [Tooltip("黑羊遮罩权重（值越大，生成数量越多）")]
        public float blackSheepMaskweight = 1;
        [Tooltip("黑羊遮罩数量（值越大，生成数量越多）")]
        public int blackSheepMaskQuantity = 1;
    }
    [System.Serializable]
    public class ConveyorPathData
    {
        [Tooltip("传送带路径点数组（世界坐标）")]
        public Vector3[] pathPoints = new Vector3[0];
        public Vector3 entry;
        public Vector3 collect;
    }

    [System.Serializable]
    public class TerrainPreset
    {
        [Tooltip("地形预设对应的类型索引（与TerrainMapping的key匹配）")]
        public int mapType;
        [Tooltip("地形生成的位置")]
        public Vector3 GenerationLocation;
        [Tooltip("地形生成的旋转角度（欧拉角）")]
        public Vector3 GenerationRotation; 
        [Tooltip("地形生成的缩放比例")]
        public Vector3 GenerationScale= new Vector3(1, 1, 1);
    }

    /// <summary>
    /// 关卡数据类，继承自ScriptableObject，可作为资源文件存储关卡信息
    /// 通过CreateAssetMenu属性，可在Unity菜单中创建该资源
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "WoolyPath/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("关卡信息")]
        [Tooltip("关卡的唯一索引，用于区分不同关卡（建议从0开始递增）")]
        public int levelIndex = 0;               // 关卡索引

        [Tooltip("关卡的显示名称，为空时会自动生成「Level X」格式（X=索引+1）")]
        public string levelName = "Level 1";     // 关卡名称

        [Tooltip("关卡的详细描述文本，支持多行输入，用于向玩家展示关卡目标或规则")]
        [TextArea(3, 5)]
        public string levelDescription = "";     // 关卡描述（多行文本框）

        [Header("绵羊生成位置设置")]
        [Tooltip("游戏网格的宽高尺寸（列数x行数），决定绵羊可活动的区域大小")]
        [SerializeField] public Vector2Int GridSize = new Vector2Int(7, 7); // 网格尺寸（x=行数，y=列数）
        [SerializeField] public Vector2 GridSpacing = new Vector2(1f, 1f); // 网格单元格间距（x轴、z轴）
        [SerializeField] public Vector3 GridStartPosition = new Vector3(-3f, 0f, -3f); // 网格起始位置（左下角基准点）

       
        public TerrainPreset _terrainPreset;
        
        [Header("传送带设置")]
        [Tooltip("传送带路径数据")]
        [SerializeField] public ConveyorPathData conveyorPath = new ConveyorPathData();

        [Tooltip("是否使用数量生成")]
        public bool UseQuantity = true;
        [Header("绵羊权重")]
        [Tooltip("不同颜色绵羊的生成概率权重（权重越高，生成概率越大，0则不生成）")]
        [SerializeField] public SheepPrefabsWeight[] levelSheepPrefabsWeight;
        [SerializeField] public BlackSheepMask blackSheepMask;


        [Header("收集器")]
        [Tooltip("关卡中的收集器数据数组，可通过右键菜单「Generate Default Collectors」生成默认布局")]
        [SerializeField] public CollectorData[] collectors;  // 收集器数组

        [Header("绵羊生成")]
        [Tooltip("是否启用绵羊动态生成系统（关闭则仅使用初始绵羊布局）")]
        public bool useDynamicSheepSpawn = true;             // 是否使用动态生成系统

       
        public int totalSheepCount = 100;                    // 关卡总羊数


        [Header("游戏规则")]
        [Tooltip("传送带最多能暂存的羊毛数量（超过则无法继续传输）")]
        public int conveyorCapacity = 10;        // 传送带容量
        public float conveyorSpeed = 10f;        // 传送带容量


        public CollectorData[] Collectors => collectors;


        /// <summary>
        /// 根据收集器颜色计算总容量，更新对应SheepPrefabsWeight的Quantity
        /// </summary>
        public void UpdateSheepQuantityByCollectorCapacity()
        {
            // 空值判断，避免报错
            if (collectors == null || collectors.Length == 0)
            {
                Debug.LogWarning($"关卡 {levelIndex} 未设置收集器，无法更新绵羊数量");
                return;
            }
            if (levelSheepPrefabsWeight == null || levelSheepPrefabsWeight.Length == 0)
            {
                Debug.LogWarning($"关卡 {levelIndex} 未设置绵羊权重数据，无法更新绵羊数量");
                return;
            }
            // 新增：计算所有收集器的总容量（不区分颜色）
            int totalAllCollectorCapacity = 0;
            foreach (var collector in collectors)
            {
                totalAllCollectorCapacity += collector.capacity;
            }
            totalSheepCount = totalAllCollectorCapacity;
            // 1. 遍历所有SheepPrefabsWeight，仅处理开启匹配开关的项
            foreach (var sheepWeight in levelSheepPrefabsWeight)
            {
                if (!sheepWeight.useCollectorColorMatch)
                    continue; // 未开启开关，跳过

                // 2. 计算当前绵羊颜色对应的收集器总容量
                int totalCollectorCapacity = 0;
                foreach (var collector in collectors)
                {
                    // 匹配收集器目标颜色与绵羊颜色
                    if (collector.targetColor == sheepWeight.color)
                    {
                        totalCollectorCapacity += collector.capacity;
                    }
                }

                // 3. 将总容量赋值给Quantity（若未找到对应收集器，Quantity设为0）
                sheepWeight.Quantity = totalCollectorCapacity;
                Debug.Log($"已更新颜色 {sheepWeight.color} 的绵羊数量：{sheepWeight.Quantity}（匹配收集器总容量）");
            }
        }

        /// <summary>
        /// 编辑模式右键菜单：手动触发「根据收集器容量更新绵羊数量」
        /// </summary>
        [ContextMenu("根据收集器容量更新绵羊数量")]
        private void UpdateSheepQuantityByCollectorCapacity_Editor()
        {
            UpdateSheepQuantityByCollectorCapacity();
            
        }
    }

}
