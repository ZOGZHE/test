using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤1: 方块组合器
    /// 负责将多个方块拼接到一起,形成初步的方块布局
    /// 核心原则:
    /// 1. 前期方块尽量相连
    /// 2. 后期可以有分散/奇葩形状
    /// 3. 绝对不能重叠
    /// </summary>
    public static class BlockComposer
    {
        // 方块选择权重 (避免使用单格方块Purple06)
        private static readonly Dictionary<BlockType, float> BlockWeights = new Dictionary<BlockType, float>
        {
            { BlockType.BlockBlue01, 20f },    // 横I型 - 常用
            { BlockType.BlockPink02, 20f },    // T字型 - 常用
            { BlockType.BlockYollow03, 25f },  // 小L型 - 最常用
            { BlockType.BlockOrange04, 20f },  // 大L型 - 常用
            { BlockType.BlockGreen05, 15f },   // Z型 - 较少用
            // BlockType.BlockPurple06 - 单格完全不使用
        };

        /// <summary>
        /// 组合方块
        /// </summary>
        /// <param name="config">难度配置</param>
        /// <param name="random">随机数生成器</param>
        /// <returns>方块布局列表,如果失败返回null</returns>
        public static List<BlockPlacementV2> ComposeBlocks(DifficultyConfigV2 config, System.Random random)
        {
            // 确定方块数量
            int blockCount = random.Next(config.minBlockCount, config.maxBlockCount + 1);

            // 选择方块类型
            var blockTypes = SelectBlockTypes(blockCount, config.levelIndex, random);

            // 尝试放置方块
            Debug.Log($"[方块组合] 选择方块类型: {string.Join(", ", blockTypes)}");

            for (int attempt = 0; attempt < 20; attempt++)
            {
                var placements = TryComposeBlocks(blockTypes, config, random);
                if (placements != null)
                {
                    Debug.Log($"[方块组合] 成功组合 {placements.Count} 个方块 (第{attempt + 1}次尝试)");
                    // 输出每个方块的详细信息
                    for (int i = 0; i < placements.Count; i++)
                    {
                        var p = placements[i];
                        Debug.Log($"[方块组合]   方块{i}: {p.blockType} @{p.centerPosition} 旋转{p.rotationAngle}° 占用格子:{string.Join(",", p.coveredPositions)}");
                    }
                    return placements;
                }
            }

            Debug.LogWarning("[方块组合] 失败: 无法找到有效的方块组合");
            return null;
        }

        /// <summary>
        /// 选择方块类型
        /// </summary>
        private static List<BlockType> SelectBlockTypes(int count, int levelIndex, System.Random random)
        {
            var types = new List<BlockType>();

            // 根据关卡难度确定可用方块类型
            var availableTypes = GetAvailableBlockTypes(levelIndex);

            // 加权随机选择
            for (int i = 0; i < count; i++)
            {
                var selectedType = WeightedRandomSelect(availableTypes, random);
                types.Add(selectedType);
            }

            return types;
        }

        /// <summary>
        /// 根据难度获取可用方块类型
        /// </summary>
        private static List<BlockType> GetAvailableBlockTypes(int levelIndex)
        {
            var types = new List<BlockType>();

            // 教程关卡(1-10): 只用简单方块
            if (levelIndex <= 10)
            {
                types.Add(BlockType.BlockBlue01);
                types.Add(BlockType.BlockYollow03);
            }
            // 简单关卡(11-20): 加入T字型
            else if (levelIndex <= 20)
            {
                types.Add(BlockType.BlockBlue01);
                types.Add(BlockType.BlockPink02);
                types.Add(BlockType.BlockYollow03);
            }
            // 中等关卡(21-30): 加入大L型
            else if (levelIndex <= 30)
            {
                types.Add(BlockType.BlockBlue01);
                types.Add(BlockType.BlockPink02);
                types.Add(BlockType.BlockYollow03);
                types.Add(BlockType.BlockOrange04);
            }
            // 困难及以上(31+): 全部方块
            else
            {
                types.Add(BlockType.BlockBlue01);
                types.Add(BlockType.BlockPink02);
                types.Add(BlockType.BlockYollow03);
                types.Add(BlockType.BlockOrange04);
                types.Add(BlockType.BlockGreen05);
            }

            return types;
        }

        /// <summary>
        /// 加权随机选择
        /// </summary>
        private static BlockType WeightedRandomSelect(List<BlockType> availableTypes, System.Random random)
        {
            float totalWeight = 0f;
            foreach (var type in availableTypes)
            {
                if (BlockWeights.ContainsKey(type))
                {
                    totalWeight += BlockWeights[type];
                }
            }

            float randomValue = (float)random.NextDouble() * totalWeight;
            float cumulativeWeight = 0f;

            foreach (var type in availableTypes)
            {
                if (BlockWeights.ContainsKey(type))
                {
                    cumulativeWeight += BlockWeights[type];
                    if (randomValue <= cumulativeWeight)
                    {
                        return type;
                    }
                }
            }

            return availableTypes[availableTypes.Count - 1];
        }

        /// <summary>
        /// 尝试组合方块
        /// </summary>
        private static List<BlockPlacementV2> TryComposeBlocks(List<BlockType> blockTypes, DifficultyConfigV2 config, System.Random random)
        {
            var placements = new List<BlockPlacementV2>();
            var occupiedCells = new HashSet<Vector2Int>();

            // 第一个方块放在原点
            var firstBlock = CreateRandomBlock(blockTypes[0], Vector2Int.zero, random);
            placements.Add(firstBlock);
            foreach (var cell in firstBlock.coveredPositions)
            {
                occupiedCells.Add(cell);
            }

            // 放置后续方块
            for (int i = 1; i < blockTypes.Count; i++)
            {
                BlockPlacementV2 newBlock = null;

                // 根据难度决定放置策略
                if (config.allowDisconnectedBlocks && random.NextDouble() < 0.3)
                {
                    // 后期难度: 30%概率分散放置
                    newBlock = PlaceBlockDisconnected(blockTypes[i], occupiedCells, random);
                }
                else
                {
                    // 默认: 相邻放置
                    newBlock = PlaceBlockAdjacent(blockTypes[i], placements, occupiedCells, random);
                }

                if (newBlock == null)
                {
                    // 放置失败
                    return null;
                }

                placements.Add(newBlock);
                foreach (var cell in newBlock.coveredPositions)
                {
                    occupiedCells.Add(cell);
                }
            }

            // 验证连通性(前期关卡必须连通)
            if (!config.allowDisconnectedBlocks)
            {
                if (!AreAllBlocksConnected(placements))
                {
                    return null;
                }
            }

            return placements;
        }

        /// <summary>
        /// 放置方块(相邻策略)
        /// </summary>
        private static BlockPlacementV2 PlaceBlockAdjacent(BlockType blockType, List<BlockPlacementV2> existingBlocks, HashSet<Vector2Int> occupiedCells, System.Random random)
        {
            // 收集所有可能的相邻位置
            var candidates = new List<Vector2Int>();
            foreach (var block in existingBlocks)
            {
                foreach (var cell in block.coveredPositions)
                {
                    // 4方向相邻
                    candidates.Add(cell + Vector2Int.up);
                    candidates.Add(cell + Vector2Int.down);
                    candidates.Add(cell + Vector2Int.left);
                    candidates.Add(cell + Vector2Int.right);
                }
            }

            // 去重并打乱顺序
            candidates = candidates.Distinct().OrderBy(x => random.Next()).ToList();

            // 尝试在每个候选位置放置方块
            foreach (var candidateCenter in candidates)
            {
                // 尝试不同旋转角度
                var rotations = new[] { 0, 90, 180, 270 }.OrderBy(x => random.Next()).ToArray();
                foreach (var rotation in rotations)
                {
                    var testBlock = new BlockPlacementV2(blockType, candidateCenter, rotation);

                    // 检查是否重叠
                    bool overlaps = false;
                    foreach (var cell in testBlock.coveredPositions)
                    {
                        if (occupiedCells.Contains(cell))
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        return testBlock;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 放置方块(分散策略)
        /// </summary>
        private static BlockPlacementV2 PlaceBlockDisconnected(BlockType blockType, HashSet<Vector2Int> occupiedCells, System.Random random)
        {
            // 在已占用区域附近但不相邻的位置放置
            int minX = occupiedCells.Min(c => c.x) - 3;
            int maxX = occupiedCells.Max(c => c.x) + 3;
            int minY = occupiedCells.Min(c => c.y) - 3;
            int maxY = occupiedCells.Max(c => c.y) + 3;

            // 尝试随机位置
            for (int attempt = 0; attempt < 50; attempt++)
            {
                int x = random.Next(minX, maxX + 1);
                int y = random.Next(minY, maxY + 1);
                var candidateCenter = new Vector2Int(x, y);

                // 随机旋转
                int rotation = new[] { 0, 90, 180, 270 }[random.Next(4)];
                var testBlock = new BlockPlacementV2(blockType, candidateCenter, rotation);

                // 检查是否重叠
                bool overlaps = false;
                foreach (var cell in testBlock.coveredPositions)
                {
                    if (occupiedCells.Contains(cell))
                    {
                        overlaps = true;
                        break;
                    }
                }

                // 检查是否太近(至少间隔1格)
                bool tooClose = false;
                foreach (var cell in testBlock.coveredPositions)
                {
                    foreach (var occupiedCell in occupiedCells)
                    {
                        int distance = Mathf.Abs(cell.x - occupiedCell.x) + Mathf.Abs(cell.y - occupiedCell.y);
                        if (distance <= 1)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (tooClose) break;
                }

                if (!overlaps && !tooClose)
                {
                    return testBlock;
                }
            }

            return null;
        }

        /// <summary>
        /// 创建随机方块
        /// </summary>
        private static BlockPlacementV2 CreateRandomBlock(BlockType blockType, Vector2Int center, System.Random random)
        {
            int rotation = new[] { 0, 90, 180, 270 }[random.Next(4)];
            return new BlockPlacementV2(blockType, center, rotation);
        }

        /// <summary>
        /// 检查所有方块是否连通(BFS)
        /// </summary>
        private static bool AreAllBlocksConnected(List<BlockPlacementV2> placements)
        {
            if (placements.Count <= 1)
                return true;

            var visited = new HashSet<BlockPlacementV2>();
            var queue = new Queue<BlockPlacementV2>();

            // 从第一个方块开始BFS
            queue.Enqueue(placements[0]);
            visited.Add(placements[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // 检查所有未访问的方块
                foreach (var other in placements)
                {
                    if (!visited.Contains(other) && current.IsAdjacentTo(other))
                    {
                        visited.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }

            return visited.Count == placements.Count;
        }

        /// <summary>
        /// 获取所有方块占用的格子
        /// </summary>
        public static HashSet<Vector2Int> GetAllOccupiedCells(List<BlockPlacementV2> placements)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var block in placements)
            {
                foreach (var cell in block.coveredPositions)
                {
                    cells.Add(cell);
                }
            }
            return cells;
        }
    }
}
