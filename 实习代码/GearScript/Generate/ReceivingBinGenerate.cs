using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    public class ReceivingBinGenerate : MonoBehaviour
    {
        public static ReceivingBinGenerate Instance;
        #region 数据配置
        [SerializeField] private Vector3 centerPosition = Vector3.zero; // 接收柱网格的中心坐标
        [SerializeField] private int xRowCount = 5; // x方向的接收柱数量（行）
        [SerializeField] private int zColumnCount = 5; // z方向的接收柱数量（列）
        [SerializeField] private float spacing = 1.1f; // 相邻接收柱之间的间距(固定为1.1f,与方块系统对齐)
        [SerializeField] private Vector3 receivingBinScale = new Vector3(0.5f, 0.1f, 0.5f); // 单个接收柱的缩放（大小）
        [SerializeField] private GameObject receivingBinPrefab; // 接收柱的预制体（可选，若为null则生成Cube）

        [Header("障碍物设置")]
        [Tooltip("缺失接收柱的相对坐标数组，这些位置将不生成任何物体，例如(1,1)表示第1排第1个位置")]
        [SerializeField] private Vector2Int[] obstaclePositions; // 障碍物的相对坐标数组
        [SerializeField] private GameObject obstaclePrefab; // 障碍物预制体
        [SerializeField] private Vector3 obstacleRotation = new Vector3(0, 0, 0);

        [Header("目标齿轮设置")]
        [Tooltip("目标齿轮的相对坐标数组 例如(1,1)表示第1排第1个位置")]
        [SerializeField] private Vector2Int[] targetGearPositions; // 目标齿轮的相对坐标数组
        [SerializeField] private GameObject targetGearPrefab; // 目标齿轮预制体
        [SerializeField] private Vector3 targetGearRotation = new Vector3(0, 0, 0);
        [Tooltip("目标齿轮的位置微调偏移（用于模拟误差）")]
        [SerializeField] private Vector3 targetGearOffset = Vector3.zero; // 目标齿轮位置微调

        [Header("动力齿轮设置")]
        [Tooltip("动力齿轮的相对坐标数组 例如(1,1)表示第1排第1个位置")]
        [SerializeField] private Vector2Int[] powerGearPositions; // 动力齿轮的相对坐标数组
        [SerializeField] private GameObject powerGearPrefab; // 动力齿轮预制体
        [SerializeField] private Vector3 powerGearRotation = new Vector3(0, 0, 0);
        [Tooltip("动力齿轮的位置微调偏移（用于模拟误差）")]
        [SerializeField] private Vector3 powerGearOffset = Vector3.zero; // 动力齿轮位置微调

        [Header("缺失接收柱设置 例如(1,1)表示第1排第1个位置")]
        [Tooltip("缺失接收柱的相对坐标数组，这些位置将不生成任何物体")]
        [SerializeField] private Vector2Int[] missingBinPositions; // 缺失接收柱的相对坐标数组
         // 存储所有生成的「活跃接收柱」（排除障碍物、齿轮、缺失位置）
        public List<ReceivingBinControl> activeReceivingBins = new List<ReceivingBinControl>();
        #region Gizmos可视化
        //[Header("Gizmos 可视化")]
        //[SerializeField] private bool drawGizmos = true; // 控制是否绘制Gizmos
        //[SerializeField] private Color centerColor = Color.blue; // 中心点颜色
        //[SerializeField] private Color gridColor = Color.green; // 网格外框颜色
        //[SerializeField] private Color binGizmoColor = Color.yellow; // 单个接收柱预览颜色
        //[SerializeField] private Color obstacleGizmoColor = Color.red; // 障碍物预览颜色
        //[SerializeField] private Color missingBinGizmoColor = Color.gray; // 缺失接收柱预览颜色
        //[SerializeField] private Color targetGearGizmoColor = Color.cyan; // 目标齿轮预览颜色
        //[SerializeField] private Color powerGearGizmoColor = Color.magenta; // 动力齿轮预览颜色
        #endregion
        #endregion

        #region 生命周期函数

        private void Awake()
        {      
            if (Instance == null)
            {
                Instance = this;
              
                //DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
         
        }

        #endregion

        #region 初始化
        public void InitReceivingBinGenerateData(LevelData levelData)
        {
            // 1. 销毁上一关的旧物体（接收柱、齿轮、障碍物）
            DestroyOldBins();

            // 2. 从LevelData的接收柱配置类（_receivingBinGenerateData）中读取当前关卡参数，覆盖脚本自身的默认配置
            //「接收柱网格中心点坐标」，决定整个接收柱区域在场景中的位置
            centerPosition = levelData._receivingBinGenerateData.centerPosition;
            // 「X方向接收柱数量」（即网格的行数）
            xRowCount = levelData._receivingBinGenerateData.xRowCount;
            // 「Z方向接收柱数量」（即网格的列数）
            zColumnCount = levelData._receivingBinGenerateData.zColumnCount;
            // 「障碍物相对坐标数组」，标记网格中需要生成障碍物的位置
            obstaclePositions = levelData._receivingBinGenerateData.obstaclePositions;
            // 「目标齿轮相对坐标数组」，标记网格中需要生成目标齿轮的位置
            targetGearPositions = levelData._receivingBinGenerateData.targetGearPositions;
            // 「动力齿轮相对坐标数组」，标记网格中需要生成动力齿轮的位置
            powerGearPositions = levelData._receivingBinGenerateData.powerGearPositions;
            // 「缺失接收柱相对坐标数组」，标记网格中不生成任何物体的空位
            missingBinPositions = levelData._receivingBinGenerateData.missingBinPositions;

            // 3. 初始化活跃接收柱列表
            InitializeActiveBinList();

            // 4. 生成本关卡的接收柱、齿轮、障碍物
            GenerateReceivingBins();
        }
        private void InitializeActiveBinList()
        {
            // 若列表未实例化（为null），则新建一个空列表
            if (activeReceivingBins == null)
            {
                activeReceivingBins = new List<ReceivingBinControl>();
            }
            // 若列表已有数据，清空旧数据（避免关卡切换时残留上一关的接收柱）
            else
            {
                activeReceivingBins.Clear();
            }
        }
        #endregion

        #region  具体生成
        void GenerateReceivingBins()
        {
            // 计算x和z方向的起始偏移量，使整个网格以centerPosition为中心
            float xStartOffset = -(xRowCount - 1) * spacing / 2f;
            float zStartOffset = -(zColumnCount - 1) * spacing / 2f;

            // 双重循环生成x行z列的元素（优先级：缺失接收柱 > 目标齿轮 > 动力齿轮 > 障碍物 > 正常接收柱）
            for (int xIndex = 0; xIndex < xRowCount; xIndex++)
            {
                for (int zIndex = 0; zIndex < zColumnCount; zIndex++)
                {
                    if (IsMissingBinPosition(xIndex, zIndex))
                    {
                        continue; // 缺失接收柱：不生成任何物体
                    }
                    else if (IsTargetGearPosition(xIndex, zIndex))
                    {
                        GenerateTargetGear(xIndex, zIndex, xStartOffset, zStartOffset);
                    }
                    else if (IsPowerGearPosition(xIndex, zIndex))
                    {
                        GeneratePowerGear(xIndex, zIndex, xStartOffset, zStartOffset);
                    }
                    else if (IsObstaclePosition(xIndex, zIndex))
                    {
                        GenerateObstacle(xIndex, zIndex, xStartOffset, zStartOffset);
                    }
                    else
                    {
                        // 生成接收柱的 GameObject
                        GameObject newReceivingBinObj = GenerateReceivingBin(xIndex, zIndex, xStartOffset, zStartOffset);
                        if (newReceivingBinObj != null)
                        {
                            // 从 GameObject 上获取 ReceivingBinControl 组件
                            ReceivingBinControl binControl = newReceivingBinObj.GetComponent<ReceivingBinControl>();
                            if (binControl != null)
                            {
                                // 将组件添加到活跃列表（列表类型为 List<ReceivingBinControl>）
                                activeReceivingBins.Add(binControl);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>检查是否为缺失接收柱位置</summary>
        private bool IsMissingBinPosition(int xIndex, int zIndex)
        {
            if (missingBinPositions == null || missingBinPositions.Length == 0)
                return false;
            foreach (var pos in missingBinPositions)
                if (pos.x-1 == xIndex && pos.y-1 == zIndex) return true;
            return false;
        }

        /// <summary>检查是否为目标齿轮位置</summary>
        private bool IsTargetGearPosition(int xIndex, int zIndex)
        {
            if (targetGearPositions == null || targetGearPositions.Length == 0)
                return false;
            foreach (var pos in targetGearPositions)
                if (pos.x-1 == xIndex && pos.y -1== zIndex) return true;
            return false;
        }

        /// <summary>检查是否为动力齿轮位置</summary>
        private bool IsPowerGearPosition(int xIndex, int zIndex)
        {
            if (powerGearPositions == null || powerGearPositions.Length == 0)
                return false;
            foreach (var pos in powerGearPositions)
                if (pos.x -1== xIndex && pos.y-1== zIndex) return true;
            return false;
        }

        /// <summary>检查是否为障碍物位置</summary>
        private bool IsObstaclePosition(int xIndex, int zIndex)
        {
            if (obstaclePositions == null || obstaclePositions.Length == 0)
                return false;
            foreach (var pos in obstaclePositions)
                if (pos.x -1== xIndex && pos.y -1== zIndex) return true;
            return false;
        }

        /// <summary>生成单个接收柱</summary>
        private GameObject GenerateReceivingBin(int xIndex, int zIndex, float xStartOffset, float zStartOffset)
        {
            // 计算接收柱的世界坐标
            Vector3 binWorldPos = centerPosition + new Vector3(
                xStartOffset + xIndex * spacing,
                0f,
                zStartOffset + zIndex * spacing
            );

            // 生成接收柱（优先用预制体，无预制体则生成默认Cube）
            GameObject currentBin = receivingBinPrefab != null
                ? Instantiate(receivingBinPrefab, binWorldPos, Quaternion.identity, transform)
                : CreateDefaultCube(binWorldPos, $"ReceivingBin_X{xIndex+1}_Z{zIndex+1}");

            // 设置接收柱名称（便于调试识别）
            currentBin.name = $"ReceivingBin_X{xIndex+1}_Z{zIndex+1}";
            return currentBin;
        }

        /// <summary>生成障碍物</summary>
        private void GenerateObstacle(int xIndex, int zIndex, float xStartOffset, float zStartOffset)
        {
            Vector3 obstacleWorldPos = centerPosition + new Vector3(
                xStartOffset + xIndex * spacing,
                0f,
                zStartOffset + zIndex * spacing
            );
            Quaternion obstacleRot = Quaternion.Euler(obstacleRotation);

            GameObject currentObstacle = obstaclePrefab != null
                ? Instantiate(obstaclePrefab, obstacleWorldPos, obstacleRot, transform)
                : CreateDefaultCube(obstacleWorldPos, $"Obstacle_X{xIndex+1}_Z{zIndex+1}", Color.red, obstacleRot);

            currentObstacle.name = $"Obstacle_X{xIndex+1}_Z{zIndex+1}";
        }

        /// <summary>生成目标齿轮（含位置微调）</summary>
        private void GenerateTargetGear(int xIndex, int zIndex, float xStartOffset, float zStartOffset)
        {
            // 计算基础位置 + 微调偏移
            Vector3 targetGearWorldPos = centerPosition + new Vector3(
                xStartOffset + xIndex * spacing,
                0f,
                zStartOffset + zIndex * spacing
            ) + targetGearOffset;

            Quaternion targetGearRot = Quaternion.Euler(targetGearRotation);

            GameObject currentTargetGear = targetGearPrefab != null
                ? Instantiate(targetGearPrefab, targetGearWorldPos, targetGearRot, transform)
                : CreateDefaultSphere(targetGearWorldPos, $"TargetGear_X{xIndex+1}_Z{zIndex+1}", Color.cyan, targetGearRot);

            currentTargetGear.name = $"TargetGear_X{xIndex+1}_Z{zIndex+1}";
        }

        /// <summary>生成动力齿轮（含位置微调）</summary>
        private void GeneratePowerGear(int xIndex, int zIndex, float xStartOffset, float zStartOffset)
        {
            // 计算基础位置 + 微调偏移
            Vector3 powerGearWorldPos = centerPosition + new Vector3(
                xStartOffset + xIndex * spacing,
                0f,
                zStartOffset + zIndex * spacing
            ) + powerGearOffset;

            Quaternion powerGearRot = Quaternion.Euler(powerGearRotation);

            GameObject currentPowerGear = powerGearPrefab != null
                ? Instantiate(powerGearPrefab, powerGearWorldPos, powerGearRot, transform)
                : CreateDefaultSphere(powerGearWorldPos, $"PowerGear_X{xIndex+1}_Z{zIndex+1}", Color.magenta, powerGearRot);

            currentPowerGear.name = $"PowerGear_X{xIndex+1}_Z{zIndex+1}";
        }

        /// <summary>创建默认Cube（用于无预制体时的接收柱/障碍物）</summary>
        private GameObject CreateDefaultCube(Vector3 pos, string name, Color? color = null, Quaternion? rot = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
            cube.transform.rotation = rot ?? Quaternion.identity;
            cube.transform.localScale = receivingBinScale; // 使用接收柱的缩放配置
            cube.transform.parent = transform;
            if (color.HasValue) cube.GetComponent<Renderer>().material.color = color.Value;
            cube.name = name;
            return cube;
        }

        /// <summary>创建默认Sphere（用于无预制体时的齿轮）</summary>
        private GameObject CreateDefaultSphere(Vector3 pos, string name, Color? color = null, Quaternion? rot = null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = pos;
            sphere.transform.rotation = rot ?? Quaternion.identity;
            sphere.transform.localScale = receivingBinScale * 0.8f; // 齿轮比接收柱略小
            sphere.transform.parent = transform;
            if (color.HasValue) sphere.GetComponent<Renderer>().material.color = color.Value;
            sphere.name = name;
            return sphere;
        }

        #endregion

        #region 销毁旧的接收柱、齿轮、障碍物
        public void DestroyOldBins()
        {
            // 先停止所有可能访问接收柱的协程
            StopAllCoroutines();

            // 安全的接收柱状态重置
            if (activeReceivingBins != null)
            {
                foreach (var bin in activeReceivingBins.ToArray()) // 使用副本遍历
                {
                    if (bin != null && bin.gameObject != null)
                    {
                        bin.isOccupied = false;
                    }
                }
            }

            // 分阶段销毁子物体
            List<GameObject> childrenToDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child != null && child.gameObject != null)
                {
                    childrenToDestroy.Add(child.gameObject);
                }
            }

            // 立即销毁所有子物体
            foreach (var child in childrenToDestroy)
            {
                if (child != null)
                {
                    DestroyImmediate(child);
                }
            }

            // 彻底清空列表
            if (activeReceivingBins != null)
            {
                activeReceivingBins.Clear();
            }
            else
            {
                activeReceivingBins = new List<ReceivingBinControl>();
            }

            // 强制资源清理
            Resources.UnloadUnusedAssets();
        }

        #endregion

        #region Gizmos绘制
        //private void OnDrawGizmos()
        //{
        //    if (!drawGizmos) return;

        //    // 1. 绘制中心点
        //    Gizmos.color = centerColor;
        //    Gizmos.DrawSphere(centerPosition, 0.1f);

        //    // 2. 绘制整个网格的外框
        //    float totalXLength = (xRowCount - 1) * spacing;
        //    float totalZLength = (zColumnCount - 1) * spacing;
        //    Vector3 gridSize = new Vector3(totalXLength, 0.1f, totalZLength);
        //    Gizmos.color = gridColor;
        //    Gizmos.DrawWireCube(centerPosition, gridSize);

        //    // 3. 绘制每个元素的预览框
        //    float xStartOffset = -(xRowCount - 1) * spacing / 2f;
        //    float zStartOffset = -(zColumnCount - 1) * spacing / 2f;

        //    for (int xIndex = 0; xIndex < xRowCount; xIndex++)
        //    {
        //        for (int zIndex = 0; zIndex < zColumnCount; zIndex++)
        //        {
        //            Vector3 basePosition = centerPosition + new Vector3(
        //                xStartOffset + xIndex * spacing,
        //                0f,
        //                zStartOffset + zIndex * spacing
        //            );
        //            Vector3 finalPosition = basePosition;

        //            // 根据元素类型叠加偏移（Gizmos可视化匹配实际生成位置）
        //            if (IsTargetGearPosition(xIndex, zIndex))
        //                finalPosition += targetGearOffset;
        //            else if (IsPowerGearPosition(xIndex, zIndex))
        //                finalPosition += powerGearOffset;

        //            // 设置Gizmos颜色（匹配元素类型）
        //            if (IsMissingBinPosition(xIndex, zIndex))
        //                Gizmos.color = missingBinGizmoColor;
        //            else if (IsTargetGearPosition(xIndex, zIndex))
        //                Gizmos.color = targetGearGizmoColor;
        //            else if (IsPowerGearPosition(xIndex, zIndex))
        //                Gizmos.color = powerGearGizmoColor;
        //            else if (IsObstaclePosition(xIndex, zIndex))
        //                Gizmos.color = obstacleGizmoColor;
        //            else
        //                Gizmos.color = binGizmoColor;

        //            Gizmos.DrawWireCube(finalPosition, receivingBinScale);
        //        }
        //    }
        //}
        #endregion

    }
}