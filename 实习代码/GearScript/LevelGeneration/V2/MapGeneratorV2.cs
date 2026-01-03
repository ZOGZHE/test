using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤5: 地图生成器
    /// 负责:
    /// 1. 确定必需的ReceivingBin(方块占用的所有格子)
    /// 2. 计算地图大小
    /// 3. 装饰地图(添加迷惑接收柱、缺失、障碍)
    /// 4. 生成GridData供验证使用
    /// </summary>
    public static class MapGeneratorV2
    {
        /// <summary>
        /// 生成地图数据
        /// </summary>
        public static MapData GenerateMap(
            List<BlockPlacementV2> finalBlocks,
            GearPlacementData gearData,
            DifficultyConfigV2 config,
            System.Random random)
        {
            // 1. 收集所有必需的接收柱位置(方块的所有槽位)
            var requiredBinPositions = new HashSet<Vector2Int>();
            foreach (var block in finalBlocks)
            {
                foreach (var pos in block.coveredPositions)
                {
                    requiredBinPositions.Add(pos);
                }
            }

            // 2. 计算地图范围
            var allPositions = new HashSet<Vector2Int>(requiredBinPositions);
            allPositions.UnionWith(gearData.powerGearPositions);
            allPositions.UnionWith(gearData.targetGearPositions);

            if (allPositions.Count == 0)
            {
                Debug.LogWarning("[地图生成] 没有任何位置");
                return null;
            }

            int minX = allPositions.Min(p => p.x);
            int maxX = allPositions.Max(p => p.x);
            int minY = allPositions.Min(p => p.y);
            int maxY = allPositions.Max(p => p.y);

            // 添加1-2格padding
            int padding = 1;
            minX -= padding;
            maxX += padding;
            minY -= padding;
            maxY += padding;

            int gridXSize = maxX - minX + 1;
            int gridZSize = maxY - minY + 1;

            // 3. 创建网格
            var grid = new PathValidator.GridData(gridXSize, gridZSize);

            // 坐标转换函数: 世界坐标 -> 网格坐标
            Vector2Int WorldToGrid(Vector2Int worldPos)
            {
                return new Vector2Int(worldPos.x - minX, worldPos.y - minY);
            }

            // 4. 填充必需的接收柱
            foreach (var worldPos in requiredBinPositions)
            {
                var gridPos = WorldToGrid(worldPos);
                grid.cells[gridPos.x, gridPos.y] = PathValidator.CellType.Normal;
            }

            // 5. 放置动力轮
            var powerGearGridPositions = new List<Vector2Int>();
            foreach (var worldPos in gearData.powerGearPositions)
            {
                var gridPos = WorldToGrid(worldPos);
                grid.cells[gridPos.x, gridPos.y] = PathValidator.CellType.PowerGear;
                powerGearGridPositions.Add(gridPos);
            }

            // 6. 放置目标轮
            var targetGearGridPositions = new List<Vector2Int>();
            foreach (var worldPos in gearData.targetGearPositions)
            {
                var gridPos = WorldToGrid(worldPos);
                grid.cells[gridPos.x, gridPos.y] = PathValidator.CellType.TargetGear;
                targetGearGridPositions.Add(gridPos);
            }

            // 7. 装饰地图(在Empty区域添加迷惑、障碍等)
            DecorateMap(grid, config, random);

            // 8. 生成最终的地图数据
            var mapData = new MapData
            {
                gridXSize = gridXSize,
                gridZSize = gridZSize,
                offsetX = minX,
                offsetY = minY,
                grid = grid,
                powerGearPositions = gearData.powerGearPositions,
                targetGearPositions = gearData.targetGearPositions,
                powerGearGridPositions = powerGearGridPositions,
                targetGearGridPositions = targetGearGridPositions,
                requiredBinPositions = new List<Vector2Int>(requiredBinPositions),
                obstaclePositions = new List<Vector2Int>(),
                missingBinPositions = new List<Vector2Int>()
            };

            // 收集障碍和缺失位置(用于后续数据序列化)
            for (int x = 0; x < gridXSize; x++)
            {
                for (int z = 0; z < gridZSize; z++)
                {
                    Vector2Int worldPos = new Vector2Int(x + minX, z + minY);

                    if (grid.cells[x, z] == PathValidator.CellType.Obstacle)
                    {
                        mapData.obstaclePositions.Add(worldPos);
                    }
                    else if (grid.cells[x, z] == PathValidator.CellType.Empty)
                    {
                        // Empty表示"缺失的接收柱"(不生成任何物体)
                        // V2: 包括外圈的Empty也记录，实现稀疏外圈效果
                        mapData.missingBinPositions.Add(worldPos);
                    }
                }
            }

            Debug.Log($"[地图生成] 成功: 大小{gridXSize}x{gridZSize}, 必需接收柱{requiredBinPositions.Count}, 障碍{mapData.obstaclePositions.Count}, 缺失{mapData.missingBinPositions.Count}");

            return mapData;
        }

        /// <summary>
        /// 装饰地图(添加迷惑接收柱、缺失、障碍)
        /// V2: 分离内部区域和外圈处理
        /// </summary>
        private static void DecorateMap(PathValidator.GridData grid, DifficultyConfigV2 config, System.Random random)
        {
            // 收集内部Empty格子(排除最外圈)
            var innerEmptyPositions = new List<Vector2Int>();
            for (int x = 1; x < grid.xSize - 1; x++)
            {
                for (int z = 1; z < grid.zSize - 1; z++)
                {
                    if (grid.cells[x, z] == PathValidator.CellType.Empty)
                    {
                        innerEmptyPositions.Add(new Vector2Int(x, z));
                    }
                }
            }

            // 收集外圈Empty格子
            var outerRimPositions = CollectOuterRimPositions(grid);

            // 装饰内部区域
            DecorateInnerArea(grid, innerEmptyPositions, config, random);

            // 装饰外圈
            DecorateOuterRim(grid, outerRimPositions, config, random);
        }

        /// <summary>
        /// 收集最外圈的Empty格子
        /// </summary>
        private static List<Vector2Int> CollectOuterRimPositions(PathValidator.GridData grid)
        {
            var outerRim = new List<Vector2Int>();

            // 上下边
            for (int x = 0; x < grid.xSize; x++)
            {
                if (grid.cells[x, 0] == PathValidator.CellType.Empty)
                    outerRim.Add(new Vector2Int(x, 0));
                if (grid.cells[x, grid.zSize - 1] == PathValidator.CellType.Empty)
                    outerRim.Add(new Vector2Int(x, grid.zSize - 1));
            }

            // 左右边(排除角落避免重复)
            for (int z = 1; z < grid.zSize - 1; z++)
            {
                if (grid.cells[0, z] == PathValidator.CellType.Empty)
                    outerRim.Add(new Vector2Int(0, z));
                if (grid.cells[grid.xSize - 1, z] == PathValidator.CellType.Empty)
                    outerRim.Add(new Vector2Int(grid.xSize - 1, z));
            }

            return outerRim;
        }

        /// <summary>
        /// 装饰内部区域 - V2: 只选择与内容相邻的位置，形成连续延伸
        /// </summary>
        private static void DecorateInnerArea(PathValidator.GridData grid, List<Vector2Int> emptyPositions, DifficultyConfigV2 config, System.Random random)
        {
            if (emptyPositions.Count == 0)
                return;

            // 策略1: 只保留与现有内容相邻的Empty格子(去掉孤立和远离的)
            var adjacentToContent = emptyPositions.Where(p => IsAdjacentToContent(p, grid)).ToList();

            if (adjacentToContent.Count == 0)
            {
                Debug.LogWarning("[内部装饰] 没有与内容相邻的Empty格子");
                return;
            }

            // 策略2: 波次扩展选择装饰点，形成连续延伸的感觉
            int extraBinCount = Mathf.RoundToInt(adjacentToContent.Count * config.extraReceivingBinRatio);
            int obstacleCount = Mathf.RoundToInt(adjacentToContent.Count * config.obstacleRatio);

            var decorationPositions = SelectContinuousDecorations(adjacentToContent, extraBinCount + obstacleCount, grid, random);

            // 打乱选中的装饰位置
            decorationPositions = decorationPositions.OrderBy(p => random.Next()).ToList();

            // 前部分作为迷惑接收柱
            for (int i = 0; i < extraBinCount && i < decorationPositions.Count; i++)
            {
                var pos = decorationPositions[i];
                grid.cells[pos.x, pos.y] = PathValidator.CellType.Normal;
            }

            // 后部分作为障碍物
            for (int i = extraBinCount; i < decorationPositions.Count; i++)
            {
                var pos = decorationPositions[i];
                grid.cells[pos.x, pos.y] = PathValidator.CellType.Obstacle;
            }

            Debug.Log($"[内部装饰] 候选{adjacentToContent.Count}, 装饰{decorationPositions.Count}(接收柱{extraBinCount}+障碍{obstacleCount})");
        }

        /// <summary>
        /// 判断是否与内容区域相邻(Normal/PowerGear/TargetGear)
        /// </summary>
        private static bool IsAdjacentToContent(Vector2Int pos, PathValidator.GridData grid)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                if (neighbor.x >= 0 && neighbor.x < grid.xSize &&
                    neighbor.y >= 0 && neighbor.y < grid.zSize)
                {
                    var cellType = grid.cells[neighbor.x, neighbor.y];
                    if (cellType == PathValidator.CellType.Normal ||
                        cellType == PathValidator.CellType.PowerGear ||
                        cellType == PathValidator.CellType.TargetGear ||
                        cellType == PathValidator.CellType.Obstacle) // 障碍也算内容
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 选择连续的装饰位置 - 波次扩展，形成俄罗斯方块式的连续形状
        /// </summary>
        private static List<Vector2Int> SelectContinuousDecorations(List<Vector2Int> candidates, int targetCount, PathValidator.GridData grid, System.Random random)
        {
            if (candidates.Count == 0 || targetCount <= 0)
                return new List<Vector2Int>();

            var selected = new List<Vector2Int>();
            var remaining = new HashSet<Vector2Int>(candidates);

            // 策略: 从内容区域开始，波次向外扩展选择装饰点
            // 这样形成的装饰点会连续延伸，像俄罗斯方块一样

            while (selected.Count < targetCount && remaining.Count > 0)
            {
                Vector2Int nextPos;

                if (selected.Count == 0)
                {
                    // 第一个随机选择
                    var candidatesList = remaining.ToList();
                    nextPos = candidatesList[random.Next(candidatesList.Count)];
                }
                else
                {
                    // 后续选择与已选位置相邻的
                    var adjacentToSelected = remaining.Where(p => IsAdjacentToSelected(p, selected)).ToList();

                    if (adjacentToSelected.Count > 0)
                    {
                        // 优先选择与已选位置相邻的(形成连续形状)
                        nextPos = adjacentToSelected[random.Next(adjacentToSelected.Count)];
                    }
                    else
                    {
                        // 没有相邻的了，随机选一个(但这样会打断连续性，少发生)
                        var candidatesList = remaining.ToList();
                        nextPos = candidatesList[random.Next(candidatesList.Count)];
                    }
                }

                selected.Add(nextPos);
                remaining.Remove(nextPos);
            }

            return selected;
        }

        /// <summary>
        /// 判断是否与已选位置相邻
        /// </summary>
        private static bool IsAdjacentToSelected(Vector2Int pos, List<Vector2Int> selected)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                if (selected.Contains(neighbor))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 装饰外圈 - V2: 只保留靠近内容的位置，去掉孤立和远离的
        /// </summary>
        private static void DecorateOuterRim(PathValidator.GridData grid, List<Vector2Int> outerRimPositions, DifficultyConfigV2 config, System.Random random)
        {
            if (outerRimPositions.Count == 0) return;

            // 策略1: 只保留与内容相邻的外圈位置(去掉孤立和远离的)
            var adjacentToContent = outerRimPositions.Where(p => IsNearContent(p, grid)).ToList();

            if (adjacentToContent.Count == 0)
            {
                Debug.Log("[外圈装饰] 没有与内容相邻的外圈位置，跳过外圈装饰");
                return;
            }

            // 策略2: 根据outerRimDensity决定保留多少
            int keepCount = Mathf.RoundToInt(adjacentToContent.Count * config.outerRimDensity);
            keepCount = Mathf.Min(keepCount, adjacentToContent.Count);

            if (keepCount == 0)
            {
                Debug.Log("[外圈装饰] outerRimDensity=0，跳过外圈装饰");
                return;
            }

            // 策略3: 从相邻的位置中选择，优先角落
            var selectedPositions = SelectOuterRimPositions(adjacentToContent, keepCount, grid, random);

            // 决定是Normal还是Obstacle
            var shuffled = selectedPositions.OrderBy(x => random.Next()).ToList();

            // 少量外圈障碍物(10%的选中位置)
            int outerObstacleCount = Mathf.Max(0, Mathf.RoundToInt(selectedPositions.Count * 0.1f));
            for (int i = 0; i < outerObstacleCount && i < shuffled.Count; i++)
            {
                var pos = shuffled[i];
                grid.cells[pos.x, pos.y] = PathValidator.CellType.Obstacle;
            }

            // 其余选中位置转为Normal(生成普通接收柱)
            for (int i = outerObstacleCount; i < shuffled.Count; i++)
            {
                var pos = shuffled[i];
                grid.cells[pos.x, pos.y] = PathValidator.CellType.Normal;
            }

            Debug.Log($"[外圈装饰] 总外圈{outerRimPositions.Count}, 靠近内容{adjacentToContent.Count}, 保留{selectedPositions.Count}, 障碍{outerObstacleCount}");
        }

        /// <summary>
        /// 智能选择外圈位置 - V2: 传入的已经是靠近内容的，优先角落
        /// </summary>
        private static List<Vector2Int> SelectOuterRimPositions(List<Vector2Int> candidates, int keepCount, PathValidator.GridData grid, System.Random random)
        {
            if (keepCount >= candidates.Count) return new List<Vector2Int>(candidates);
            if (keepCount <= 0) return new List<Vector2Int>();

            var selected = new List<Vector2Int>();
            var remaining = new List<Vector2Int>(candidates);

            // 优先级1: 角落位置(视觉锚点)，如果有的话
            var corners = candidates.Where(p => IsCorner(p, grid)).ToList();
            int cornerKeep = Mathf.Min(corners.Count, Mathf.CeilToInt(keepCount * 0.4f)); // 提高角落优先级到40%
            for (int i = 0; i < cornerKeep && corners.Count > 0; i++)
            {
                var corner = corners[random.Next(corners.Count)];
                selected.Add(corner);
                remaining.Remove(corner);
                corners.Remove(corner);
            }

            // 优先级2: 随机选择剩余位置(都已经是靠近内容的了)
            int randomKeep = keepCount - selected.Count;
            var randomSelected = remaining.OrderBy(x => random.Next()).Take(randomKeep).ToList();
            selected.AddRange(randomSelected);

            return selected;
        }

        /// <summary>判断是否为角落</summary>
        private static bool IsCorner(Vector2Int pos, PathValidator.GridData grid)
        {
            return (pos.x == 0 || pos.x == grid.xSize - 1) &&
                   (pos.y == 0 || pos.y == grid.zSize - 1);
        }

        /// <summary>判断是否靠近内容区域</summary>
        private static bool IsNearContent(Vector2Int pos, PathValidator.GridData grid)
        {
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                if (neighbor.x >= 0 && neighbor.x < grid.xSize &&
                    neighbor.y >= 0 && neighbor.y < grid.zSize)
                {
                    var cellType = grid.cells[neighbor.x, neighbor.y];
                    if (cellType == PathValidator.CellType.Normal ||
                        cellType == PathValidator.CellType.PowerGear ||
                        cellType == PathValidator.CellType.TargetGear)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// 地图数据
    /// </summary>
    public class MapData
    {
        /// <summary>网格尺寸</summary>
        public int gridXSize;
        public int gridZSize;

        /// <summary>世界坐标偏移(用于坐标转换)</summary>
        public int offsetX;
        public int offsetY;

        /// <summary>网格数据(供验证使用)</summary>
        public PathValidator.GridData grid;

        /// <summary>动力轮位置(世界坐标)</summary>
        public List<Vector2Int> powerGearPositions = new List<Vector2Int>();

        /// <summary>目标轮位置(世界坐标)</summary>
        public List<Vector2Int> targetGearPositions = new List<Vector2Int>();

        /// <summary>动力轮位置(网格坐标)</summary>
        public List<Vector2Int> powerGearGridPositions = new List<Vector2Int>();

        /// <summary>目标轮位置(网格坐标)</summary>
        public List<Vector2Int> targetGearGridPositions = new List<Vector2Int>();

        /// <summary>必需的接收柱位置(世界坐标)</summary>
        public List<Vector2Int> requiredBinPositions = new List<Vector2Int>();

        /// <summary>障碍物位置(世界坐标)</summary>
        public List<Vector2Int> obstaclePositions = new List<Vector2Int>();

        /// <summary>缺失点位(世界坐标)</summary>
        public List<Vector2Int> missingBinPositions = new List<Vector2Int>();

        /// <summary>世界坐标转网格坐标</summary>
        public Vector2Int WorldToGrid(Vector2Int worldPos)
        {
            return new Vector2Int(worldPos.x - offsetX, worldPos.y - offsetY);
        }

        /// <summary>网格坐标转世界坐标</summary>
        public Vector2Int GridToWorld(Vector2Int gridPos)
        {
            return new Vector2Int(gridPos.x + offsetX, gridPos.y + offsetY);
        }
    }
}
