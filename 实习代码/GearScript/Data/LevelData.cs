using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    #region 积木
    public enum BlockType
    {
        BlockBlue01, BlockPink02, BlockYollow03, BlockOrange04, BlockGreen05, BlockPurple06
    }

    [System.Serializable]
    public class BlockData
    {
        public BlockType _blockType;
        [Header("齿轮配置")]
        [Tooltip("勾选那个gear就启用哪些齿轮")]
        public GearData[] _gearobject;
        public bool gear1;
        public bool gear2;
        public bool gear3;
        public bool gear4;
        [Header("插槽配置")]
        [Tooltip("插槽在积木「本地坐标系」中的位置（相对于积木中心）")]
        [SerializeField] private List<Vector3> slotPoints;
        public List<Vector3> SlotPoints => slotPoints ?? new List<Vector3>();
    }
    #endregion

    #region 齿轮
    [System.Serializable]
    public class GearData
    {
        public GameObject GearObject;               // 齿轮
        public RotationDirection CurrentDirection; // 当前旋转方向
        public float RotationSpeed = 30f;          // 旋转速度（度/秒）
        public bool IsRotating = false;            // 是否正在旋转
        public bool IsActive = true;               // 是否启用
        public bool IsShowForHint = false;  //是否是展示用 展示用的齿轮无法旋转
    }
    #endregion

    #region 积木生成预设
    [System.Serializable]
    public class BlockGenerateData
    {
        [Header("本关卡可用积木列表")]
        [Tooltip("配置本关卡可以生成的积木类型及齿轮设置")]
        public List<BlockGenerateItem> generateItems = new List<BlockGenerateItem>();
    }

    [System.Serializable]
    public class BlockGenerateItem
    {
        [Header("积木类型")]
        public BlockType blockType;  // 要生成的积木类型

        [Header("齿轮启用配置")]
        [Tooltip("是否启用第1个齿轮")]
        public bool gear1;
        [Tooltip("是否启用第2个齿轮")]
        public bool gear2;
        [Tooltip("是否启用第3个齿轮")]
        public bool gear3;
        [Tooltip("是否启用第4个齿轮")]
        public bool gear4;
    }
    #endregion

    #region 接收柱生成预设
    [System.Serializable]
    public class ReceivingBinGenerateData
    {
        [SerializeField] public Vector3 centerPosition = Vector3.zero; // 接收柱网格的中心坐标
        [SerializeField] public int xRowCount = 5; // x方向的接收柱数量（行）
        [SerializeField] public int zColumnCount = 5; // z方向的接收柱数量（列）
        [Header("障碍物设置")]
        [Tooltip("障碍物的相对坐标数组，例如(1,1)表示第二排第二个位置")]
        [SerializeField] public Vector2Int[] obstaclePositions; // 障碍物的相对坐标数组
        [Header("目标齿轮设置")]
        [Tooltip("目标齿轮的相对坐标数组")]
        [SerializeField] public Vector2Int[] targetGearPositions; // 目标齿轮的相对坐标数组
        [Header("动力齿轮设置")]
        [Tooltip("动力齿轮的相对坐标数组")]
        [SerializeField] public Vector2Int[] powerGearPositions; // 动力齿轮的相对坐标数组
        [Header("缺失接收柱设置")]
        [Tooltip("缺失接收柱的相对坐标数组，这些位置将不生成任何物体")]
        [SerializeField] public Vector2Int[] missingBinPositions; // 缺失接收柱的相对坐标数组
    }
    #endregion

    #region 积木提示预设
    [System.Serializable]
    public class DemonstrationBlock
    {
        [Header("积木基础信息")]
        public BlockType blockType; // 积木类型
        [Header("齿轮启用配置")]
        [Tooltip("是否启用第1个齿轮")]
        public bool gear1;
        [Tooltip("是否启用第2个齿轮")]
        public bool gear2;
        [Tooltip("是否启用第3个齿轮")]
        public bool gear3;
        [Tooltip("是否启用第4个齿轮")]
        public bool gear4;
        [Header("放置位置与旋转")]
        public Vector3 worldPosition; // 世界坐标位置
        public Vector3 worldRotation; // 旋转角度（欧拉角）
    }
    #endregion

    #region 地形生成预设
    [System.Serializable]
    public class Terrain
    {
    }
    #endregion

    [CreateAssetMenu(fileName = "LevelData_", menuName = "SuperGear/LevelData", order = 1)]
    public class LevelData : ScriptableObject
    {
        #region 关卡基本信息
        [Header("关卡基本信息")]
        public int LevelIndex; // 关卡索引（唯一）
        public string LevelName; // 关卡名字
        [Header("关卡难度")]
        public bool IsDifficultyLevel = false; // 关卡难度
        [Header("倒计时配置")]
        [Tooltip("当前关卡的倒计时时长（秒）")]
        public float countdownDuration = 60f; // 倒计时总时长
        [Tooltip("当前关卡的摄像机Size")]
        public float CameraSize = 10f; // 摄像机Size
        [Header("=== BlockGenerate 配置 ===")]
        [SerializeField] public BlockGenerateData _blockGenerateData;
        [Header("=== ReceivingBinGenerate 配置 ===")]
        [SerializeField] public ReceivingBinGenerateData _receivingBinGenerateData;
        [Header("=== 预设解法配置（提示用） ===")]
        [SerializeField] public List<DemonstrationBlock> demonstrationBlocks = new List<DemonstrationBlock>();
        [Header("=== TerrainGenerate 配置 ===")]
        [SerializeField] public int _terrain;
        #endregion

        #region  根据接收柱配置矫正示范积木的位置
#if UNITY_EDITOR
        [ContextMenu("矫正示范积木位置")]
        private void CorrectPositionsInEditor()
        {
            CorrectDemonstrationBlockPositions();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
        #endregion

        #region 从示范积木同步生成可用积木配置
#if UNITY_EDITOR
        [ContextMenu("从示范积木同步生成可用积木配置")]
        private void EditorHintToGenerateItem()
        {
            SyncBlockGenerateItemsFromDemonstration();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
        #endregion

        #region 一键配置
#if UNITY_EDITOR
        [ContextMenu("一键配置")]
        private void EditorAll()
        {
            CorrectDemonstrationBlockPositions();
            SyncBlockGenerateItemsFromDemonstration();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
        #endregion

        #region 编辑方法
        //同步可用积木配置
        private void SyncBlockGenerateItemsFromDemonstration()
        {
            if (demonstrationBlocks == null || demonstrationBlocks.Count == 0)
            {
                Debug.LogWarning("示范积木列表为空，无法同步生成可用积木配置");
                return;
            }

            if (_blockGenerateData == null)
            {
                _blockGenerateData = new BlockGenerateData();
                _blockGenerateData.generateItems = new List<BlockGenerateItem>();
            }

            // 清空现有配置（如需保留原有配置可改为去重逻辑，这里按覆盖处理）
            _blockGenerateData.generateItems.Clear();

            // 遍历示范积木，同步配置到BlockGenerateItem
            foreach (var demoBlock in demonstrationBlocks)
            {
                BlockGenerateItem generateItem = new BlockGenerateItem
                {
                    blockType = demoBlock.blockType,
                    gear1 = demoBlock.gear1,
                    gear2 = demoBlock.gear2,
                    gear3 = demoBlock.gear3,
                    gear4 = demoBlock.gear4
                };
                _blockGenerateData.generateItems.Add(generateItem);
            }

#if UNITY_EDITOR
            // 保存修改
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            Debug.Log($"已从示范积木同步生成 {_blockGenerateData.generateItems.Count} 项可用积木配置");
        }
        //矫正示范积木位置
        public void CorrectDemonstrationBlockPositions()
        {
            if (demonstrationBlocks == null || demonstrationBlocks.Count == 0)
                return;

            const float binSpacing = 1.1f;               // 接收柱固定间距
            const float precisionThreshold = 1e-5f;      // 精度阈值（小于该值视为0）
            var binData = _receivingBinGenerateData;
            Vector3 gridCenter = binData.centerPosition;

            // 计算网格原点（左下角）相对于中心的偏移（确保中心对齐）
            float xOffset = (binData.xRowCount - 1) * 0.5f * binSpacing;
            float zOffset = (binData.zColumnCount - 1) * 0.5f * binSpacing;

            foreach (var demoBlock in demonstrationBlocks)
            {
                // 计算X轴矫正位置（包含精度修正）
                float xDistance = demoBlock.worldPosition.x - (gridCenter.x - xOffset);
                float xMultiplier = Mathf.Round(xDistance / binSpacing);
                float correctedX = gridCenter.x - xOffset + xMultiplier * binSpacing;
                correctedX = Mathf.Abs(correctedX) < precisionThreshold ? 0 : correctedX;

                // 计算Z轴矫正位置（包含精度修正）
                float zDistance = demoBlock.worldPosition.z - (gridCenter.z - zOffset);
                float zMultiplier = Mathf.Round(zDistance / binSpacing);
                float correctedZ = gridCenter.z - zOffset + zMultiplier * binSpacing;
                correctedZ = Mathf.Abs(correctedZ) < precisionThreshold ? 0 : correctedZ;

                // 应用矫正后的位置（保持Y轴不变）
                demoBlock.worldPosition = new Vector3(
                    correctedX,
                    demoBlock.worldPosition.y,
                    correctedZ
                );
            }
        }
        #endregion

    }
}