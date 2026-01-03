using System.Collections.Generic;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// V2版本方块放置数据
    /// 描述一个方块的类型、位置、旋转和齿轮启用状态
    /// </summary>
    [System.Serializable]
    public class BlockPlacementV2
    {
        [Header("方块基础信息")]
        public BlockType blockType;

        [Tooltip("方块中心的网格坐标(0-based)")]
        public Vector2Int centerPosition;

        [Tooltip("旋转角度 (0, 90, 180, 270)")]
        public int rotationAngle;

        [Header("方块占用的格子")]
        [Tooltip("方块占用的所有网格位置(0-based,用于碰撞检测)")]
        public List<Vector2Int> coveredPositions = new List<Vector2Int>();

        [Header("齿轮配置")]
        [Tooltip("每个槽位的齿轮是否启用(最多4个槽位)")]
        public bool[] gearEnabled = new bool[4] { false, false, false, false };

        /// <summary>
        /// 构造函数
        /// </summary>
        public BlockPlacementV2(BlockType type, Vector2Int center, int rotation)
        {
            blockType = type;
            centerPosition = center;
            rotationAngle = rotation;

            // 计算方块占用的格子
            CalculateCoveredPositions();

            // 默认禁用所有齿轮
            gearEnabled = new bool[4] { false, false, false, false };
        }

        /// <summary>
        /// 计算方块占用的所有网格位置
        /// </summary>
        public void CalculateCoveredPositions()
        {
            coveredPositions.Clear();

            // 获取旋转后的槽位坐标
            var slotPositions = BlockShapeDefinition.GetRotatedSlotPositions(blockType, rotationAngle);

            // 转换为世界坐标(网格坐标)
            foreach (var slot in slotPositions)
            {
                Vector2Int worldPos = centerPosition + slot;
                coveredPositions.Add(worldPos);
            }
        }

        /// <summary>
        /// 检查是否与其他方块重叠
        /// </summary>
        public bool OverlapsWith(BlockPlacementV2 other)
        {
            foreach (var pos in coveredPositions)
            {
                if (other.coveredPositions.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查是否与指定位置重叠
        /// </summary>
        public bool OverlapsWith(Vector2Int position)
        {
            return coveredPositions.Contains(position);
        }

        /// <summary>
        /// 检查是否与位置集合重叠
        /// </summary>
        public bool OverlapsWith(HashSet<Vector2Int> positions)
        {
            foreach (var pos in coveredPositions)
            {
                if (positions.Contains(pos))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查方块是否与已有方块相邻(用于连通性检查)
        /// </summary>
        public bool IsAdjacentTo(BlockPlacementV2 other)
        {
            foreach (var myPos in coveredPositions)
            {
                foreach (var otherPos in other.coveredPositions)
                {
                    // 检查4方向相邻
                    int dx = Mathf.Abs(myPos.x - otherPos.x);
                    int dy = Mathf.Abs(myPos.y - otherPos.y);

                    if ((dx == 1 && dy == 0) || (dx == 0 && dy == 1))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取启用齿轮的数量
        /// </summary>
        public int GetEnabledGearCount()
        {
            int count = 0;
            for (int i = 0; i < gearEnabled.Length && i < coveredPositions.Count; i++)
            {
                if (gearEnabled[i])
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 获取启用齿轮的位置列表
        /// </summary>
        public List<Vector2Int> GetEnabledGearPositions()
        {
            var positions = new List<Vector2Int>();
            for (int i = 0; i < gearEnabled.Length && i < coveredPositions.Count; i++)
            {
                if (gearEnabled[i])
                {
                    positions.Add(coveredPositions[i]);
                }
            }
            return positions;
        }

        /// <summary>
        /// 启用指定位置的齿轮
        /// </summary>
        public bool EnableGearAt(Vector2Int position)
        {
            for (int i = 0; i < coveredPositions.Count; i++)
            {
                if (coveredPositions[i] == position)
                {
                    if (i < gearEnabled.Length)
                    {
                        gearEnabled[i] = true;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 获取方块的边界矩形
        /// </summary>
        public void GetBounds(out int minX, out int maxX, out int minY, out int maxY)
        {
            if (coveredPositions.Count == 0)
            {
                minX = maxX = centerPosition.x;
                minY = maxY = centerPosition.y;
                return;
            }

            minX = maxX = coveredPositions[0].x;
            minY = maxY = coveredPositions[0].y;

            foreach (var pos in coveredPositions)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }
        }

        /// <summary>
        /// 克隆方块
        /// </summary>
        public BlockPlacementV2 Clone()
        {
            var clone = new BlockPlacementV2(blockType, centerPosition, rotationAngle);
            clone.coveredPositions = new List<Vector2Int>(coveredPositions);
            clone.gearEnabled = (bool[])gearEnabled.Clone();
            return clone;
        }

        public override string ToString()
        {
            int enabledGears = GetEnabledGearCount();
            return $"{blockType} @ {centerPosition}, 旋转{rotationAngle}°, {coveredPositions.Count}格, {enabledGears}齿轮";
        }
    }
}
