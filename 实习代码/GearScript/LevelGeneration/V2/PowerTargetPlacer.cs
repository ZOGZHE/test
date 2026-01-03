using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤3: 动力轮/目标轮放置器
    /// 负责严格按照路线端点的横竖相邻位置放置动力轮和目标轮
    /// 核心原则:
    /// 1. 严格在端点的4方向相邻位置放置
    /// 2. 不占用方块位置
    /// 3. 固定1个动力轮,其余为目标轮
    /// </summary>
    public static class PowerTargetPlacer
    {
        /// <summary>
        /// 放置动力轮和目标轮
        /// </summary>
        /// <param name="startPoint">路线起点(旁边放置动力轮)</param>
        /// <param name="endpoints">路线终点(旁边放置目标轮)</param>
        /// <param name="allOccupiedCells">所有方块占用的格子</param>
        /// <param name="random">随机数生成器</param>
        /// <returns>放置数据,如果失败返回null</returns>
        public static GearPlacementData PlaceGearsAtEndpoints(Vector2Int startPoint, List<Vector2Int> endpoints, HashSet<Vector2Int> allOccupiedCells, System.Random random)
        {
            var usedPositions = new HashSet<Vector2Int>(allOccupiedCells);

            // 1. 在起点旁边放置动力轮
            var powerGearPos = FindGearPositionNearEndpoint(startPoint, usedPositions, random);
            if (powerGearPos == null)
            {
                Debug.LogWarning($"[动力轮/目标轮放置] 失败: 起点{startPoint}附近没有合适的位置放置动力轮");
                return null;
            }
            usedPositions.Add(powerGearPos.Value);

            // 2. 在所有终点旁边放置目标轮
            var targetGearPositions = new List<Vector2Int>();
            foreach (var endpoint in endpoints)
            {
                var targetGearPos = FindGearPositionNearEndpoint(endpoint, usedPositions, random);
                if (targetGearPos == null)
                {
                    Debug.LogWarning($"[动力轮/目标轮放置] 失败: 终点{endpoint}附近没有合适的位置放置目标轮");
                    return null;
                }

                targetGearPositions.Add(targetGearPos.Value);
                usedPositions.Add(targetGearPos.Value);
            }

            Debug.Log($"[动力轮/目标轮放置] 成功: 1个动力轮在起点{startPoint}旁, {targetGearPositions.Count}个目标轮在终点旁");

            return new GearPlacementData
            {
                powerGearPositions = new List<Vector2Int> { powerGearPos.Value },
                targetGearPositions = targetGearPositions
            };
        }

        /// <summary>
        /// 找到端点附近的齿轮位置(4方向相邻)
        /// </summary>
        private static Vector2Int? FindGearPositionNearEndpoint(Vector2Int endpoint, HashSet<Vector2Int> usedPositions, System.Random random)
        {
            // 4方向候选位置
            var candidates = new List<Vector2Int>
            {
                endpoint + Vector2Int.up,
                endpoint + Vector2Int.down,
                endpoint + Vector2Int.left,
                endpoint + Vector2Int.right
            };

            // 打乱顺序
            candidates = candidates.OrderBy(x => random.Next()).ToList();

            // 选择第一个未占用的位置
            foreach (var candidate in candidates)
            {
                if (!usedPositions.Contains(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// 验证齿轮放置是否合理
        /// </summary>
        public static bool ValidateGearPlacement(GearPlacementData gearData, Vector2Int startPoint, List<Vector2Int> endpoints, HashSet<Vector2Int> pathCells)
        {
            var allGearPositions = new HashSet<Vector2Int>();
            allGearPositions.UnionWith(gearData.powerGearPositions);
            allGearPositions.UnionWith(gearData.targetGearPositions);

            // 检查1: 每个齿轮必须与路径相邻
            foreach (var gearPos in allGearPositions)
            {
                if (!IsAdjacentToPath(gearPos, pathCells))
                {
                    Debug.LogWarning($"[齿轮验证] 齿轮{gearPos}未与路径相邻");
                    return false;
                }
            }

            // 检查2: 齿轮不能与方块重叠
            // (已经在FindGearPositionNearEndpoint中保证)

            // 检查3: 至少1个动力轮和1个目标轮
            if (gearData.powerGearPositions.Count < 1)
            {
                Debug.LogWarning("[齿轮验证] 至少需要1个动力轮");
                return false;
            }

            if (gearData.targetGearPositions.Count < 1)
            {
                Debug.LogWarning("[齿轮验证] 至少需要1个目标轮");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查齿轮是否与路径相邻(4方向)
        /// </summary>
        private static bool IsAdjacentToPath(Vector2Int gearPos, HashSet<Vector2Int> pathCells)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                if (pathCells.Contains(gearPos + dir))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 齿轮放置数据
    /// </summary>
    public class GearPlacementData
    {
        /// <summary>动力轮位置列表</summary>
        public List<Vector2Int> powerGearPositions = new List<Vector2Int>();

        /// <summary>目标轮位置列表</summary>
        public List<Vector2Int> targetGearPositions = new List<Vector2Int>();

        /// <summary>获取所有齿轮位置</summary>
        public HashSet<Vector2Int> GetAllGearPositions()
        {
            var all = new HashSet<Vector2Int>();
            all.UnionWith(powerGearPositions);
            all.UnionWith(targetGearPositions);
            return all;
        }
    }
}
