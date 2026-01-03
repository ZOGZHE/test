using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    /// <summary>
    /// 方块形状定义：定义6种俄罗斯方块形状的槽位坐标
    /// 坐标系：以方块中心为原点，X轴向右，Z轴向前
    /// </summary>
    public static class BlockShapeDefinition
    {
        /// <summary>
        /// 获取指定方块类型的基础槽位坐标（旋转角度为0度时）
        /// 返回的坐标是相对于方块中心的本地坐标
        /// </summary>
        public static List<Vector2Int> GetBaseSlotPositions(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.BlockBlue01:
                    // 竖向3格直线: 
                    return new List<Vector2Int>
                    {
                        new Vector2Int(0, 1),
                        new Vector2Int(0, 0),
                        new Vector2Int(0, -1)
                    };

                case BlockType.BlockPink02:
                    // T字型（4格）:    ●   (槽位1: 上)
                    //              ● ● ●  (槽位0,2,3: 左中右)
                    return new List<Vector2Int>
                    {
                        new Vector2Int(-1, 0),  // 槽位0: 01左
                        new Vector2Int(0, 1),   // 槽位1: 02上
                        new Vector2Int(0, 0),   // 槽位2: 03中心
                        new Vector2Int(1, 0)    // 槽位3: 04右
                    };

                case BlockType.BlockYollow03:
                    // 小L型（3格）: 
                    // 注意：槽位0是拐弯的中心点
                    return new List<Vector2Int>
                    {
                        new Vector2Int(0, 0),   // 槽位0: 中心（拐弯点）
                        new Vector2Int(0, 1),   // 槽位1: 正上
                        new Vector2Int(1, 0)   // 槽位2: 右方
                    };

                case BlockType.BlockOrange04:
                    // 大L型（4格）:    ●    (槽位2: 上上)
                    //                 ●    (槽位1: 上)
                    //                 ●●   (槽位0,3: 拐弯点+右)
                    return new List<Vector2Int>
                    {
                        new Vector2Int(0, -1),   // 槽位0: 拐弯点(L型的拐角)
                        new Vector2Int(0, 0),    // 槽位1: 在槽位0正上方
                        new Vector2Int(0, 1),    // 槽位2: 在槽位1正上方
                        new Vector2Int(1, -1)    // 槽位3: 在槽位0右边
                    };

                case BlockType.BlockGreen05:
                    // Z型（4格）:  ●     (槽位0: 中上)
                    //             ●●    (槽位1,2: 中心+右侧)
                    //              ●    (槽位3: 右侧下)
                    return new List<Vector2Int>
                    {
                        new Vector2Int(1, -1),  // 槽位0: 右下
                        new Vector2Int(1, 0),  // 槽位1: 右
                        new Vector2Int(0, 0),   // 槽位2: 中心
                        new Vector2Int(0, 1)   // 槽位3: 中心上
                    };

                case BlockType.BlockPurple06:
                    // 单格: ●
                    return new List<Vector2Int>
                    {
                        new Vector2Int(0, 0)
                    };

                default:
                    Debug.LogError($"未定义的方块类型: {blockType}");
                    return new List<Vector2Int>();
            }
        }

        /// <summary>
        /// 获取旋转后的槽位坐标
        /// </summary>
        /// <param name="blockType">方块类型</param>
        /// <param name="rotationAngle">旋转角度（0, 90, 180, 270）</param>
        /// <returns>旋转后的槽位坐标列表</returns>
        public static List<Vector2Int> GetRotatedSlotPositions(BlockType blockType, int rotationAngle)
        {
            var basePositions = GetBaseSlotPositions(blockType);

            // 标准化旋转角度到 0, 90, 180, 270
            rotationAngle = ((rotationAngle % 360) + 360) % 360;

            // 0度时直接返回基础坐标
            if (rotationAngle == 0)
                return basePositions;

            // 旋转坐标
            var rotatedPositions = new List<Vector2Int>();
            foreach (var pos in basePositions)
            {
                rotatedPositions.Add(RotatePoint(pos, rotationAngle));
            }

            return rotatedPositions;
        }

        /// <summary>
        /// 旋转一个2D点（顺时针）
        /// </summary>
        private static Vector2Int RotatePoint(Vector2Int point, int angle)
        {
            switch (angle)
            {
                case 90:
                    // 顺时针90度: (x, z) -> (z, -x)
                    return new Vector2Int(point.y, -point.x);
                case 180:
                    // 180度: (x, z) -> (-x, -z)
                    return new Vector2Int(-point.x, -point.y);
                case 270:
                    // 顺时针270度: (x, z) -> (-z, x)
                    return new Vector2Int(-point.y, point.x);
                default:
                    return point;
            }
        }

        /// <summary>
        /// 获取方块所有可能的旋转状态（去重）
        /// 例如：单格方块只有1个状态，直线有2个状态，L形有4个状态
        /// </summary>
        public static List<int> GetUniqueRotations(BlockType blockType)
        {
            var uniqueRotations = new List<int>();
            var seenConfigurations = new HashSet<string>();

            foreach (int angle in new[] { 0, 90, 180, 270 })
            {
                var positions = GetRotatedSlotPositions(blockType, angle);
                var signature = GetPositionSignature(positions);

                if (!seenConfigurations.Contains(signature))
                {
                    seenConfigurations.Add(signature);
                    uniqueRotations.Add(angle);
                }
            }

            return uniqueRotations;
        }

        /// <summary>
        /// 生成位置配置的唯一签名（用于去重）
        /// </summary>
        private static string GetPositionSignature(List<Vector2Int> positions)
        {
            // 排序后生成字符串签名
            var sorted = new List<Vector2Int>(positions);
            sorted.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            return string.Join("|", sorted);
        }

        /// <summary>
        /// 将槽位坐标转换为世界坐标（考虑网格间距）
        /// </summary>
        /// <param name="slotPositions">槽位坐标列表</param>
        /// <param name="centerWorldPos">方块中心的世界坐标</param>
        /// <param name="gridSpacing">网格间距（默认1.1f）</param>
        /// <returns>世界坐标列表</returns>
        public static List<Vector3> ToWorldPositions(List<Vector2Int> slotPositions, Vector3 centerWorldPos, float gridSpacing = 1.1f)
        {
            var worldPositions = new List<Vector3>();

            foreach (var slot in slotPositions)
            {
                float worldX = centerWorldPos.x + slot.x * gridSpacing;
                float worldZ = centerWorldPos.z + slot.y * gridSpacing;
                worldPositions.Add(new Vector3(worldX, centerWorldPos.y, worldZ));
            }

            return worldPositions;
        }

        /// <summary>
        /// 获取方块的复杂度评分（用于难度调整）
        /// </summary>
        public static int GetComplexityScore(BlockType blockType)
        {
            switch (blockType)
            {
                case BlockType.BlockPurple06:
                    return 1; // 单格最简单
                case BlockType.BlockBlue01:
                case BlockType.BlockPink02:
                    return 2; // 直线简单
                case BlockType.BlockYollow03:
                    return 3; // L形中等
                case BlockType.BlockOrange04:
                    return 4; // T形较复杂
                case BlockType.BlockGreen05:
                    return 5; // 十字形最复杂
                default:
                    return 0;
            }
        }

        /// <summary>
        /// 获取所有可用的方块类型
        /// </summary>
        public static BlockType[] GetAllBlockTypes()
        {
            return new[]
            {
                BlockType.BlockBlue01,
                BlockType.BlockPink02,
                BlockType.BlockYollow03,
                BlockType.BlockOrange04,
                BlockType.BlockGreen05,
                BlockType.BlockPurple06
            };
        }

        /// <summary>
        /// 根据难度筛选方块类型（简单关卡只用简单方块）
        /// 注意：单格方块(Purple06)权重很低，仅作为备用
        /// </summary>
        public static List<BlockType> GetBlockTypesForDifficulty(int difficultyLevel)
        {
            var availableBlocks = new List<BlockType>();

            if (difficultyLevel >= 1)
            {
                // 最简单：直线方块为主
                availableBlocks.Add(BlockType.BlockBlue01);
                availableBlocks.Add(BlockType.BlockPink02);
            }

            if (difficultyLevel >= 2)
            {
                // 中等：加入L形
                availableBlocks.Add(BlockType.BlockYollow03);
            }

            if (difficultyLevel >= 3)
            {
                // 较难：加入T形
                availableBlocks.Add(BlockType.BlockOrange04);
            }

            if (difficultyLevel >= 4)
            {
                // 最难：加入十字形
                availableBlocks.Add(BlockType.BlockGreen05);
            }

            // 单格方块(Purple06)完全不使用

            return availableBlocks;
        }
    }
}
