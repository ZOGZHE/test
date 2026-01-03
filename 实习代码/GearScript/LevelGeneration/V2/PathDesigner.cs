using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤2: 路线设计器 (核心!)
    /// 负责在已有方块上设计齿轮连接路线
    /// 核心策略: "子弹式"路径生成
    /// 1. 从随机边缘格子出发(模拟动力轮位置)
    /// 2. 向前推进,永不回头,可分裂成多个分支
    /// 3. 到达边缘记录端点(用于放置动力轮/目标轮)
    /// 4. 路线长度+覆盖方块数=难度
    /// 5. 重试机制满足minCoveredBlocks要求
    /// </summary>
    public static class PathDesigner
    {
        /// <summary>
        /// 设计路线
        /// </summary>
        /// <param name="allOccupiedCells">所有方块占用的格子</param>
        /// <param name="blockPlacements">方块列表(用于统计覆盖方块数)</param>
        /// <param name="config">难度配置</param>
        /// <param name="random">随机数生成器</param>
        /// <returns>路线数据,包含路径格子和端点</returns>
        public static PathData DesignPath(HashSet<Vector2Int> allOccupiedCells, List<BlockPlacementV2> blockPlacements, DifficultyConfigV2 config, System.Random random)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var pathData = TryDesignPath(allOccupiedCells, blockPlacements, config, random);
                if (pathData != null && ValidatePath(pathData, config, blockPlacements))
                {
                    Debug.Log($"[路线设计] 成功设计路线: 长度{pathData.pathCells.Count}, 端点{pathData.endpoints.Count}, 覆盖{pathData.coveredBlockCount}个方块 (第{attempt + 1}次尝试)");
                    return pathData;
                }
            }

            Debug.LogWarning("[路线设计] 失败: 无法找到符合要求的路线");
            return null;
        }

        /// <summary>
        /// 尝试设计路线 - "子弹式"算法
        /// 从随机边缘出发,向前推进,永不回头,可分裂
        /// </summary>
        private static PathData TryDesignPath(HashSet<Vector2Int> allOccupiedCells, List<BlockPlacementV2> blockPlacements, DifficultyConfigV2 config, System.Random random)
        {
            if (allOccupiedCells.Count == 0)
                return null;

            // 1. 选择随机边缘格子作为起点
            Vector2Int startCell = SelectRandomEdgeCell(allOccupiedCells, random);
            if (startCell == Vector2Int.zero && !allOccupiedCells.Contains(Vector2Int.zero))
            {
                // 如果没有边缘格子(不应该发生),回退到任意格子
                startCell = allOccupiedCells.First();
            }

            // 2. 初始化路径
            var pathCells = new List<Vector2Int> { startCell };
            var pathSet = new HashSet<Vector2Int> { startCell };
            var endpoints = new List<Vector2Int>();

            // 3. 初始化"子弹头"列表
            var activeBullets = new List<BulletHead> { new BulletHead(startCell) };

            // 4. 目标路径长度
            int targetPathLength = random.Next(config.minPathLength, config.maxPathLength + 1);

            // 5. 子弹式扩展
            int maxIterations = 100; // 防止死循环
            int iteration = 0;
            bool reachedTargetLength = false;

            while (activeBullets.Count > 0 && iteration < maxIterations)
            {
                iteration++;
                var bulletsToRemove = new List<BulletHead>();

                foreach (var bullet in activeBullets.ToList()) // ToList防止迭代中修改
                {
                    // 如果已经达到目标长度，不再扩展，只检测终点
                    if (reachedTargetLength)
                    {
                        // 标记为终点并移除
                        if (!endpoints.Contains(bullet.currentPos))
                        {
                            endpoints.Add(bullet.currentPos);
                        }
                        bulletsToRemove.Add(bullet);
                        continue;
                    }

                    // 选择前进方向
                    Vector2Int? nextCell = ChooseForwardDirection(bullet, pathSet, allOccupiedCells, random);

                    if (nextCell.HasValue)
                    {
                        // 成功前进
                        pathCells.Add(nextCell.Value);
                        pathSet.Add(nextCell.Value);
                        bullet.currentPos = nextCell.Value;

                        // 检查是否达到目标长度
                        if (pathCells.Count >= targetPathLength)
                        {
                            reachedTargetLength = true;
                        }

                        // 分支逻辑(暂时完全禁用以调试)
                        // if (config.allowBranchedPath && pathCells.Count >= 8 && random.NextDouble() < 0.2) // 20%概率分支
                        // {
                        //     Vector2Int? branchCell = ChooseBranchDirection(bullet, pathSet, allOccupiedCells, random);
                        //     if (branchCell.HasValue)
                        //     {
                        //         activeBullets.Add(new BulletHead(branchCell.Value));
                        //         pathCells.Add(branchCell.Value);
                        //         pathSet.Add(branchCell.Value);
                        //     }
                        // }
                    }
                    else
                    {
                        // 到达终点(无路可走)
                        if (!endpoints.Contains(bullet.currentPos))
                        {
                            endpoints.Add(bullet.currentPos);
                        }
                        bulletsToRemove.Add(bullet);
                    }
                }

                // 移除已到达终点的子弹头
                foreach (var bullet in bulletsToRemove)
                {
                    activeBullets.Remove(bullet);
                }
            }

            // 6. 确保有足够的终点
            // 重要: 起点绝不能是端点! 起点用于放置动力轮,终点用于放置目标轮
            // minEndpoints表示总端点数(起点+终点), 所以目标轮数 = minEndpoints - 1
            int minTargetEndpoints = Mathf.Max(1, config.minEndpoints - 1);
            int maxTargetEndpoints = Mathf.Max(1, config.maxEndpoints - 1);

            // 强制排除起点
            endpoints = endpoints.Where(e => e != startCell).ToList();

            if (endpoints.Count < minTargetEndpoints)
            {
                // 补充终点: 选择路径中的边缘格子,但排除起点
                var additionalEndpoints = pathCells
                    .Where(c => c != startCell && IsEdgeCell(c, allOccupiedCells) && !endpoints.Contains(c))
                    .ToList();

                foreach (var ep in additionalEndpoints.OrderBy(x => random.Next()).Take(minTargetEndpoints - endpoints.Count))
                {
                    endpoints.Add(ep);
                }
            }

            // 限制终点数量
            if (endpoints.Count > maxTargetEndpoints)
            {
                endpoints = endpoints.OrderBy(x => random.Next()).Take(maxTargetEndpoints).ToList();
            }

            // 7. 统计覆盖的方块数
            int coveredBlockCount = CountCoveredBlocks(pathSet, blockPlacements);

            // 调试日志
            Debug.Log($"[子弹式路径] 起点:{startCell}, 路径长度:{pathCells.Count}, 终点数:{endpoints.Count}, 覆盖方块数:{coveredBlockCount}");
            Debug.Log($"[子弹式路径] 完整路径: {string.Join(" → ", pathCells)}");
            Debug.Log($"[子弹式路径] 终点位置: {string.Join(", ", endpoints)}");

            return new PathData
            {
                pathCells = pathCells,
                startPoint = startCell,
                endpoints = endpoints,
                coveredBlockCount = coveredBlockCount
            };
        }

        /// <summary>
        /// 选择随机边缘格子作为起点
        /// </summary>
        private static Vector2Int SelectRandomEdgeCell(HashSet<Vector2Int> allOccupiedCells, System.Random random)
        {
            // 筛选边缘格子(至少有一个方向是空的)
            var edgeCells = allOccupiedCells.Where(c => IsEdgeCell(c, allOccupiedCells)).ToList();

            if (edgeCells.Count == 0)
            {
                // 如果没有边缘格子(理论上不应该发生),返回任意格子
                return allOccupiedCells.First();
            }

            // 随机选择一个边缘格子
            return edgeCells[random.Next(edgeCells.Count)];
        }

        /// <summary>
        /// 选择前进方向(优先离起点更远的方向)
        /// </summary>
        private static Vector2Int? ChooseForwardDirection(BulletHead bullet, HashSet<Vector2Int> pathSet, HashSet<Vector2Int> allOccupiedCells, System.Random random)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var candidates = new List<(Vector2Int pos, int distanceFromStart)>();

            foreach (var dir in directions)
            {
                Vector2Int neighbor = bullet.currentPos + dir;

                // 必须满足:
                // 1. 在方块占用范围内
                // 2. 未被访问过
                if (allOccupiedCells.Contains(neighbor) && !pathSet.Contains(neighbor))
                {
                    int distance = ManhattanDistance(neighbor, bullet.startPos);
                    candidates.Add((neighbor, distance));
                }
            }

            if (candidates.Count == 0)
                return null; // 到达终点

            // 70%概率选择离起点最远的,30%随机选择(增加多样性)
            if (random.NextDouble() < 0.7)
            {
                return candidates.OrderByDescending(c => c.distanceFromStart).First().pos;
            }
            else
            {
                return candidates[random.Next(candidates.Count)].pos;
            }
        }

        /// <summary>
        /// 选择分支方向(从当前位置的其他可用方向)
        /// </summary>
        private static Vector2Int? ChooseBranchDirection(BulletHead bullet, HashSet<Vector2Int> pathSet, HashSet<Vector2Int> allOccupiedCells, System.Random random)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var candidates = new List<Vector2Int>();

            foreach (var dir in directions)
            {
                Vector2Int neighbor = bullet.currentPos + dir;

                if (allOccupiedCells.Contains(neighbor) && !pathSet.Contains(neighbor))
                {
                    candidates.Add(neighbor);
                }
            }

            if (candidates.Count <= 1)
                return null; // 只有1个或0个方向,不分支

            // 随机选择一个方向作为分支
            return candidates[random.Next(candidates.Count)];
        }


        /// <summary>
        /// 检查是否为边缘格子(至少一个方向是空的)
        /// </summary>
        private static bool IsEdgeCell(Vector2Int cell, HashSet<Vector2Int> allOccupiedCells)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                if (!allOccupiedCells.Contains(cell + dir))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 统计路线覆盖的方块数
        /// </summary>
        private static int CountCoveredBlocks(HashSet<Vector2Int> pathSet, List<BlockPlacementV2> blockPlacements)
        {
            int count = 0;
            foreach (var block in blockPlacements)
            {
                bool covered = false;
                foreach (var cell in block.coveredPositions)
                {
                    if (pathSet.Contains(cell))
                    {
                        covered = true;
                        break;
                    }
                }
                if (covered)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 计算曼哈顿距离
        /// </summary>
        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }


        /// <summary>
        /// 验证路径是否符合要求
        /// </summary>
        private static bool ValidatePath(PathData pathData, DifficultyConfigV2 config, List<BlockPlacementV2> blockPlacements)
        {
            // 检查路线长度
            if (pathData.pathCells.Count < config.minPathLength)
            {
                return false;
            }

            // 检查终点数量(endpoints现在只包含终点,不包括起点)
            // minEndpoints表示总端点数(起点+终点), 所以目标轮数 = minEndpoints - 1
            int minTargetEndpoints = Mathf.Max(1, config.minEndpoints - 1);
            if (pathData.endpoints.Count < minTargetEndpoints)
            {
                return false;
            }

            // 检查覆盖方块数
            if (pathData.coveredBlockCount < config.minCoveredBlocks)
            {
                return false;
            }

            // 检查路线连通性
            if (!IsPathConnected(pathData.pathCells))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查路径是否连通(BFS)
        /// </summary>
        private static bool IsPathConnected(List<Vector2Int> pathCells)
        {
            if (pathCells.Count == 0)
                return false;

            var pathSet = new HashSet<Vector2Int>(pathCells);
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            queue.Enqueue(pathCells[0]);
            visited.Add(pathCells[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (var dir in directions)
                {
                    var neighbor = current + dir;
                    if (pathSet.Contains(neighbor) && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited.Count == pathCells.Count;
        }
    }

    /// <summary>
    /// 路径数据
    /// </summary>
    public class PathData
    {
        /// <summary>路线覆盖的所有格子</summary>
        public List<Vector2Int> pathCells = new List<Vector2Int>();

        /// <summary>路线起点(用于放置动力轮)</summary>
        public Vector2Int startPoint;

        /// <summary>路线端点(用于放置目标轮)</summary>
        public List<Vector2Int> endpoints = new List<Vector2Int>();

        /// <summary>路线覆盖的方块数量</summary>
        public int coveredBlockCount = 0;
    }

    /// <summary>
    /// 子弹头 - 用于"子弹式"路径扩展
    /// </summary>
    internal class BulletHead
    {
        /// <summary>当前位置</summary>
        public Vector2Int currentPos;

        /// <summary>起始位置(用于计算前进方向)</summary>
        public Vector2Int startPos;

        public BulletHead(Vector2Int start)
        {
            currentPos = start;
            startPos = start;
        }
    }
}
