using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤4: 齿轮标记器
    /// 负责:
    /// 1. 标记方块上的齿轮启用状态(根据路线覆盖情况)
    /// 2. 移除未被路线覆盖的整个方块
    /// 3. 后期难度: 添加干扰方块(未被路线覆盖但有1个齿轮)
    /// </summary>
    public static class GearMarker
    {
        /// <summary>
        /// 标记齿轮并过滤方块
        /// </summary>
        /// <param name="blockPlacements">所有方块</param>
        /// <param name="pathCells">路线覆盖的格子</param>
        /// <param name="config">难度配置</param>
        /// <param name="random">随机数生成器</param>
        /// <returns>最终方块列表(带齿轮状态)</returns>
        public static List<BlockPlacementV2> MarkGearsAndFilterBlocks(
            List<BlockPlacementV2> blockPlacements,
            HashSet<Vector2Int> pathCells,
            DifficultyConfigV2 config,
            System.Random random)
        {
            var coveredBlocks = new List<BlockPlacementV2>();
            var uncoveredBlocks = new List<BlockPlacementV2>();

            // 1. 标记所有方块的齿轮状态,并分类
            foreach (var block in blockPlacements)
            {
                bool isAnyCovered = MarkGearsForBlock(block, pathCells);

                if (isAnyCovered)
                {
                    coveredBlocks.Add(block);
                }
                else
                {
                    uncoveredBlocks.Add(block);
                }
            }

            Debug.Log($"[齿轮标记] 路线覆盖{coveredBlocks.Count}个方块, 未覆盖{uncoveredBlocks.Count}个方块");

            // 2. 添加干扰方块(后期难度)
            if (config.addDistractorBlock && uncoveredBlocks.Count > 0)
            {
                var distractorBlock = uncoveredBlocks[random.Next(uncoveredBlocks.Count)];
                AddDistractorGear(distractorBlock, random);
                coveredBlocks.Add(distractorBlock);
                Debug.Log($"[齿轮标记] 添加干扰方块: {distractorBlock}");
            }

            // 3. 验证每个方块至少有1个齿轮
            foreach (var block in coveredBlocks)
            {
                if (block.GetEnabledGearCount() == 0)
                {
                    Debug.LogWarning($"[齿轮标记] 警告: 方块{block}没有启用的齿轮");
                    return null;
                }
            }

            return coveredBlocks;
        }

        /// <summary>
        /// 为单个方块标记齿轮
        /// </summary>
        /// <returns>是否有任何槽位被覆盖</returns>
        private static bool MarkGearsForBlock(BlockPlacementV2 block, HashSet<Vector2Int> pathCells)
        {
            bool anyCovered = false;
            var enabledSlots = new List<int>();

            for (int i = 0; i < block.coveredPositions.Count && i < block.gearEnabled.Length; i++)
            {
                Vector2Int slotPos = block.coveredPositions[i];

                if (pathCells.Contains(slotPos))
                {
                    block.gearEnabled[i] = true;
                    anyCovered = true;
                    enabledSlots.Add(i);
                }
                else
                {
                    block.gearEnabled[i] = false;
                }
            }

            // 详细日志: 输出每个方块的齿轮启用情况
            if (anyCovered)
            {
                var enabledPositions = enabledSlots.Select(i => block.coveredPositions[i]).ToList();
                Debug.Log($"[齿轮标记] {block.blockType} @{block.centerPosition} 旋转{block.rotationAngle}° - 启用槽位{string.Join(",", enabledSlots)} 位置{string.Join(",", enabledPositions)}");
            }

            return anyCovered;
        }

        /// <summary>
        /// 为干扰方块添加1个随机齿轮
        /// </summary>
        private static void AddDistractorGear(BlockPlacementV2 block, System.Random random)
        {
            // 随机选择一个槽位启用齿轮
            int slotCount = Mathf.Min(block.coveredPositions.Count, block.gearEnabled.Length);
            if (slotCount > 0)
            {
                int randomSlot = random.Next(slotCount);
                block.gearEnabled[randomSlot] = true;
            }
        }

        /// <summary>
        /// 获取所有启用齿轮的位置
        /// </summary>
        public static HashSet<Vector2Int> GetAllEnabledGearPositions(List<BlockPlacementV2> blocks)
        {
            var positions = new HashSet<Vector2Int>();

            foreach (var block in blocks)
            {
                for (int i = 0; i < block.coveredPositions.Count && i < block.gearEnabled.Length; i++)
                {
                    if (block.gearEnabled[i])
                    {
                        positions.Add(block.coveredPositions[i]);
                    }
                }
            }

            return positions;
        }
    }
}
