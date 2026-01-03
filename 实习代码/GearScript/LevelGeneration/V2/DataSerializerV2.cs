using System.Collections.Generic;
using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// 步骤7: 数据序列化器
    /// 负责将生成的关卡数据写入LevelData
    /// 核心原则: 内部使用0-based坐标,输出到LevelData时转为1-based
    /// </summary>
    public static class DataSerializerV2
    {
        /// <summary>
        /// 序列化为LevelData
        /// </summary>
        public static void SerializeToLevelData(
            LevelData levelData,
            List<BlockPlacementV2> finalBlocks,
            MapData mapData,
            DifficultyConfigV2 config)
        {
            // 1. 基本信息
            levelData.LevelIndex = config.levelIndex;
            levelData.countdownDuration = config.countdownDuration;

            // 2. 方块生成数据
            levelData._blockGenerateData = CreateBlockGenerateData(finalBlocks);

            // 3. 接收柱生成数据
            levelData._receivingBinGenerateData = CreateReceivingBinGenerateData(mapData);

            // 4. 演示方块(提示系统)
            levelData.demonstrationBlocks = CreateDemonstrationBlocks(finalBlocks, mapData);

            // 5. 地形(随机选择)
            levelData._terrain = Random.Range(0, 3); // 假设有3种地形

            Debug.Log($"[数据序列化] 成功序列化关卡{config.levelIndex}");
        }

        /// <summary>
        /// 创建方块生成数据
        /// </summary>
        private static BlockGenerateData CreateBlockGenerateData(List<BlockPlacementV2> blocks)
        {
            var generateData = new BlockGenerateData();
            generateData.generateItems = new List<BlockGenerateItem>();

            foreach (var block in blocks)
            {
                var item = new BlockGenerateItem
                {
                    blockType = block.blockType,
                    gear1 = block.gearEnabled.Length > 0 ? block.gearEnabled[0] : false,
                    gear2 = block.gearEnabled.Length > 1 ? block.gearEnabled[1] : false,
                    gear3 = block.gearEnabled.Length > 2 ? block.gearEnabled[2] : false,
                    gear4 = block.gearEnabled.Length > 3 ? block.gearEnabled[3] : false
                };

                generateData.generateItems.Add(item);
            }

            return generateData;
        }

        /// <summary>
        /// 创建接收柱生成数据
        /// 注意: LevelData使用1-based坐标,需要转换
        /// </summary>
        private static ReceivingBinGenerateData CreateReceivingBinGenerateData(MapData mapData)
        {
            var binData = new ReceivingBinGenerateData
            {
                centerPosition = Vector3.zero, // 默认中心为原点
                xRowCount = mapData.gridXSize,
                zColumnCount = mapData.gridZSize
            };

            // 坐标转换: 0-based世界坐标 → 1-based配置坐标
            // 公式: configPos = worldPos - offset + 1

            // 动力轮位置
            binData.powerGearPositions = ConvertTo1Based(mapData.powerGearPositions, mapData.offsetX, mapData.offsetY);

            // 目标轮位置
            binData.targetGearPositions = ConvertTo1Based(mapData.targetGearPositions, mapData.offsetX, mapData.offsetY);

            // 障碍物位置
            binData.obstaclePositions = ConvertTo1Based(mapData.obstaclePositions, mapData.offsetX, mapData.offsetY);

            // 缺失点位
            binData.missingBinPositions = ConvertTo1Based(mapData.missingBinPositions, mapData.offsetX, mapData.offsetY);

            return binData;
        }

        /// <summary>
        /// 转换为1-based坐标数组
        /// </summary>
        private static Vector2Int[] ConvertTo1Based(List<Vector2Int> worldPositions, int offsetX, int offsetY)
        {
            var result = new Vector2Int[worldPositions.Count];

            for (int i = 0; i < worldPositions.Count; i++)
            {
                // 1-based坐标 = (世界坐标 - offset + 1)
                int x = worldPositions[i].x - offsetX + 1;
                int y = worldPositions[i].y - offsetY + 1;
                result[i] = new Vector2Int(x, y);
            }

            return result;
        }

        /// <summary>
        /// 创建演示方块(提示系统)
        /// </summary>
        private static List<DemonstrationBlock> CreateDemonstrationBlocks(List<BlockPlacementV2> blocks, MapData mapData)
        {
            var demonstrations = new List<DemonstrationBlock>();

            // 网格间距(硬编码,与实际游戏一致)
            const float gridSpacing = 1.1f;

            // 计算网格中心偏移(与LevelData.CorrectDemonstrationBlockPositions保持一致)
            // 公式: offset = (gridSize - 1) * 0.5 * gridSpacing
            float xOffset = (mapData.gridXSize - 1) * 0.5f * gridSpacing;
            float zOffset = (mapData.gridZSize - 1) * 0.5f * gridSpacing;

            foreach (var block in blocks)
            {
                var demo = new DemonstrationBlock
                {
                    blockType = block.blockType,
                    gear1 = block.gearEnabled.Length > 0 ? block.gearEnabled[0] : false,
                    gear2 = block.gearEnabled.Length > 1 ? block.gearEnabled[1] : false,
                    gear3 = block.gearEnabled.Length > 2 ? block.gearEnabled[2] : false,
                    gear4 = block.gearEnabled.Length > 3 ? block.gearEnabled[3] : false,

                    // 世界坐标转换:
                    // 1. block.centerPosition 是生成器内部的0-based网格坐标
                    // 2. 转换为最终地图的网格坐标(减去mapData的offset)
                    // 3. 转换为Unity世界坐标(乘以gridSpacing并减去中心偏移)
                    worldPosition = new Vector3(
                        (block.centerPosition.x - mapData.offsetX) * gridSpacing - xOffset,
                        0f,
                        (block.centerPosition.y - mapData.offsetY) * gridSpacing - zOffset
                    ),

                    // 旋转角度(直接使用生成器的角度,无需转换)
                    worldRotation = new Vector3(0f, block.rotationAngle, 0f)
                };

                demonstrations.Add(demo);
            }

            return demonstrations;
        }
    }
}
