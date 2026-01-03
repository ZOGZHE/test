using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    /// <summary>
    /// 路径验证器：使用BFS检查动力齿轮是否能连接到所有目标齿轮
    /// </summary>
    public class PathValidator
    {
        /// <summary>
        /// 网格状态枚举
        /// </summary>
        public enum CellType
        {
            Empty,          // 空格（无接收槽）
            Normal,         // 普通接收槽
            Obstacle,       // 障碍物
            PowerGear,      // 动力齿轮
            TargetGear,     // 目标齿轮
            PlacedGear      // 已放置的方块上的齿轮
        }

        /// <summary>
        /// 网格数据类
        /// </summary>
        public class GridData
        {
            public int xSize;
            public int zSize;
            public CellType[,] cells;

            public GridData(int xSize, int zSize)
            {
                this.xSize = xSize;
                this.zSize = zSize;
                this.cells = new CellType[xSize, zSize];

                // 默认全部为空
                for (int x = 0; x < xSize; x++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        cells[x, z] = CellType.Empty;
                    }
                }
            }

            public bool IsValidPosition(int x, int z)
            {
                return x >= 0 && x < xSize && z >= 0 && z < zSize;
            }

            public bool HasGear(int x, int z)
            {
                if (!IsValidPosition(x, z))
                    return false;

                var cellType = cells[x, z];
                return cellType == CellType.PowerGear ||
                       cellType == CellType.TargetGear ||
                       cellType == CellType.PlacedGear;
            }

            public bool IsWalkable(int x, int z)
            {
                if (!IsValidPosition(x, z))
                    return false;

                var cellType = cells[x, z];
                return cellType != CellType.Empty && cellType != CellType.Obstacle;
            }
        }

        /// <summary>
        /// 验证所有目标齿轮是否可以从动力齿轮到达
        /// </summary>
        public static bool ValidatePowerChain(GridData grid, List<Vector2Int> powerGearPositions, List<Vector2Int> targetGearPositions)
        {
            if (powerGearPositions == null || powerGearPositions.Count == 0)
            {
                Debug.LogWarning("没有动力齿轮，无法验证");
                return false;
            }

            if (targetGearPositions == null || targetGearPositions.Count == 0)
            {
                Debug.LogWarning("没有目标齿轮，无法验证");
                return false;
            }

            // 从所有动力齿轮开始BFS
            var reachablePositions = BFSFromPowerGears(grid, powerGearPositions);

            // 检查是否所有目标齿轮都可达
            foreach (var targetPos in targetGearPositions)
            {
                if (!reachablePositions.Contains(targetPos))
                {
                    Debug.Log($"目标齿轮 ({targetPos.x}, {targetPos.y}) 无法从动力齿轮到达");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 从所有动力齿轮开始BFS，找到所有可达的齿轮位置
        /// </summary>
        private static HashSet<Vector2Int> BFSFromPowerGears(GridData grid, List<Vector2Int> powerGearPositions)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            // 将所有动力齿轮作为起点
            foreach (var powerPos in powerGearPositions)
            {
                if (grid.HasGear(powerPos.x, powerPos.y))
                {
                    queue.Enqueue(powerPos);
                    visited.Add(powerPos);
                }
            }

            // 四个方向：上、下、左、右（不包括对角线）
            var directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // 上
                new Vector2Int(0, -1),  // 下
                new Vector2Int(-1, 0),  // 左
                new Vector2Int(1, 0)    // 右
            };

            // BFS遍历
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // 检查四个方向的相邻格子
                foreach (var dir in directions)
                {
                    var neighbor = new Vector2Int(current.x + dir.x, current.y + dir.y);

                    // 跳过已访问的格子
                    if (visited.Contains(neighbor))
                        continue;

                    // 跳过无效位置
                    if (!grid.IsValidPosition(neighbor.x, neighbor.y))
                        continue;

                    // 只有包含齿轮的格子才能传递动力
                    if (grid.HasGear(neighbor.x, neighbor.y))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited;
        }

        /// <summary>
        /// 计算从动力齿轮到目标齿轮的最短路径长度（用于启发式搜索）
        /// </summary>
        public static int GetShortestPathLength(GridData grid, Vector2Int start, Vector2Int target)
        {
            if (!grid.IsValidPosition(start.x, start.y) || !grid.IsValidPosition(target.x, target.y))
                return int.MaxValue;

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<(Vector2Int pos, int distance)>();

            queue.Enqueue((start, 0));
            visited.Add(start);

            var directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            while (queue.Count > 0)
            {
                var (current, distance) = queue.Dequeue();

                // 到达目标
                if (current == target)
                    return distance;

                foreach (var dir in directions)
                {
                    var neighbor = new Vector2Int(current.x + dir.x, current.y + dir.y);

                    if (visited.Contains(neighbor))
                        continue;

                    if (!grid.IsWalkable(neighbor.x, neighbor.y))
                        continue;

                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, distance + 1));
                }
            }

            return int.MaxValue; // 无法到达
        }

        /// <summary>
        /// 获取从某个位置可达的所有齿轮位置（用于调试）
        /// </summary>
        public static List<Vector2Int> GetReachableGears(GridData grid, Vector2Int startPos)
        {
            var reachable = BFSFromPowerGears(grid, new List<Vector2Int> { startPos });
            return new List<Vector2Int>(reachable);
        }

        /// <summary>
        /// 检查两个齿轮是否相邻（可以直接传递动力）
        /// </summary>
        public static bool AreGearsAdjacent(Vector2Int pos1, Vector2Int pos2)
        {
            int dx = Mathf.Abs(pos1.x - pos2.x);
            int dz = Mathf.Abs(pos1.y - pos2.y);

            // 只有横竖相邻才返回true（不包括对角线）
            return (dx == 1 && dz == 0) || (dx == 0 && dz == 1);
        }

        /// <summary>
        /// 计算未连接的目标齿轮数量（用于评估关卡完成度）
        /// </summary>
        public static int CountUnconnectedTargets(GridData grid, List<Vector2Int> powerGearPositions, List<Vector2Int> targetGearPositions)
        {
            var reachablePositions = BFSFromPowerGears(grid, powerGearPositions);

            int unconnectedCount = 0;
            foreach (var targetPos in targetGearPositions)
            {
                if (!reachablePositions.Contains(targetPos))
                {
                    unconnectedCount++;
                }
            }

            return unconnectedCount;
        }

        /// <summary>
        /// 可视化调试：打印网格状态
        /// </summary>
        public static void DebugPrintGrid(GridData grid)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Grid Size: {grid.xSize} x {grid.zSize}");
            sb.AppendLine("Grid Layout:");

            for (int z = grid.zSize - 1; z >= 0; z--)
            {
                for (int x = 0; x < grid.xSize; x++)
                {
                    char symbol = GetCellSymbol(grid.cells[x, z]);
                    sb.Append(symbol);
                    sb.Append(" ");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        private static char GetCellSymbol(CellType cellType)
        {
            switch (cellType)
            {
                case CellType.Empty: return '.';
                case CellType.Normal: return 'o';
                case CellType.Obstacle: return 'X';
                case CellType.PowerGear: return 'P';
                case CellType.TargetGear: return 'T';
                case CellType.PlacedGear: return '*';
                default: return '?';
            }
        }
    }
}
