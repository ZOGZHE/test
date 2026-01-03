using System.Collections.Generic;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤6: 验证器
    /// 负责最终验证关卡的合法性:
    /// 1. 方块不重叠
    /// 2. 动力链连通性检查(PathValidator BFS验证)
    /// </summary>
    public static class ValidatorV2
    {
        /// <summary>
        /// 最终验证
        /// </summary>
        public static bool ValidateLevel(
            List<BlockPlacementV2> finalBlocks,
            MapData mapData,
            out string errorMessage)
        {
            // 验证1: 方块不重叠
            if (!ValidateNoBlockOverlap(finalBlocks, out errorMessage))
            {
                return false;
            }

            // 验证2: 填充方块齿轮到网格
            FillBlockGearsToGrid(finalBlocks, mapData);

            // 验证3: 动力链连通性检查 (BFS验证,已足够)
            if (!PathValidator.ValidatePowerChain(
                mapData.grid,
                mapData.powerGearGridPositions,
                mapData.targetGearGridPositions))
            {
                errorMessage = "动力链不连通: 无法从动力轮到达所有目标轮";
                return false;
            }

            errorMessage = "";
            return true;
        }

        /// <summary>
        /// 验证方块不重叠
        /// </summary>
        private static bool ValidateNoBlockOverlap(List<BlockPlacementV2> blocks, out string errorMessage)
        {
            var occupiedCells = new HashSet<Vector2Int>();

            foreach (var block in blocks)
            {
                foreach (var cell in block.coveredPositions)
                {
                    if (occupiedCells.Contains(cell))
                    {
                        errorMessage = $"方块重叠: 位置{cell}被多个方块占用";
                        return false;
                    }
                    occupiedCells.Add(cell);
                }
            }

            errorMessage = "";
            return true;
        }

        /// <summary>
        /// 填充方块齿轮到网格(用于连通性验证)
        /// </summary>
        private static void FillBlockGearsToGrid(List<BlockPlacementV2> blocks, MapData mapData)
        {
            foreach (var block in blocks)
            {
                for (int i = 0; i < block.coveredPositions.Count && i < block.gearEnabled.Length; i++)
                {
                    if (block.gearEnabled[i])
                    {
                        Vector2Int worldPos = block.coveredPositions[i];
                        Vector2Int gridPos = mapData.WorldToGrid(worldPos);

                        if (mapData.grid.IsValidPosition(gridPos.x, gridPos.y))
                        {
                            // 只有Normal类型的接收柱才填充为PlacedGear
                            // PowerGear和TargetGear保持不变
                            if (mapData.grid.cells[gridPos.x, gridPos.y] == PathValidator.CellType.Normal)
                            {
                                mapData.grid.cells[gridPos.x, gridPos.y] = PathValidator.CellType.PlacedGear;
                            }
                        }
                    }
                }
            }
        }

    }
}
