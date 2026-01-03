using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// V2版本关卡生成器 - 主控制器
    /// 协调所有步骤,生成完整的关卡
    ///
    /// 核心思路(主动设计,从内向外):
    /// 1. 组合方块
    /// 2. 从中心向外设计路线
    /// 3. 在端点旁边放置动力轮/目标轮
    /// 4. 标记齿轮+移除未覆盖方块
    /// 5. 确定地图ReceivingBin+装饰
    /// 6. 最终验证
    /// 7. 写入LevelData
    /// </summary>
    public static class LevelGeneratorV2
    {
        /// <summary>
        /// 生成单个关卡
        /// </summary>
        /// <param name="levelIndex">关卡索引</param>
        /// <returns>生成的LevelData,失败返回null</returns>
        public static LevelData GenerateLevel(int levelIndex)
        {
            return GenerateLevel(levelIndex, -1);
        }

        /// <summary>
        /// 生成单个关卡（支持指定随机种子）
        /// </summary>
        /// <param name="levelIndex">关卡索引</param>
        /// <param name="randomSeed">随机种子，-1表示使用随机种子</param>
        /// <returns>生成的LevelData,失败返回null</returns>
        public static LevelData GenerateLevel(int levelIndex, int randomSeed)
        {
            // 获取难度配置
            var config = DifficultyConfigV2.GetConfigForLevel(levelIndex);

            // 设置随机种子
            config.randomSeed = randomSeed;

            // 验证配置
            if (!config.ValidateConfig(out string configError))
            {
                Debug.LogError($"[V2生成器] 配置无效: {configError}");
                return null;
            }

            string seedInfo = randomSeed != -1 ? $" (种子: {randomSeed})" : "";
            Debug.Log($"[V2生成器] ========== 开始生成关卡 {levelIndex}{seedInfo} ==========");

            // 尝试生成
            for (int attempt = 0; attempt < config.maxGenerationAttempts; attempt++)
            {
                var levelData = TryGenerateLevel(config, attempt);

                if (levelData != null)
                {
                    Debug.Log($"[V2生成器] ✓ 关卡 {levelIndex} 生成成功! (第 {attempt + 1} 次尝试){seedInfo}");
                    return levelData;
                }

                if ((attempt + 1) % 10 == 0)
                {
                    Debug.Log($"[V2生成器] 进度: {attempt + 1}/{config.maxGenerationAttempts} 次尝试...");
                }
            }

            Debug.LogError($"[V2生成器] ✗ 关卡 {levelIndex} 生成失败 (达到最大尝试次数 {config.maxGenerationAttempts})");
            return null;
        }

        /// <summary>
        /// 尝试生成关卡(单次尝试)
        /// </summary>
        private static LevelData TryGenerateLevel(DifficultyConfigV2 config, int attemptIndex)
        {
            // 创建随机数生成器
            // 如果指定了随机种子，使用固定种子；否则使用时间戳+关卡索引+尝试次数
            int seed = config.randomSeed != -1
                ? config.randomSeed
                : (System.DateTime.Now.Ticks.GetHashCode() ^ (config.levelIndex * 10000 + attemptIndex));
            var random = new System.Random(seed);

            // 步骤1: 组合方块
            var blockPlacements = BlockComposer.ComposeBlocks(config, random);
            if (blockPlacements == null)
            {
                return null;
            }

            var allOccupiedCells = BlockComposer.GetAllOccupiedCells(blockPlacements);

            // 步骤2: 设计路线
            var pathData = PathDesigner.DesignPath(allOccupiedCells, blockPlacements, config, random);
            if (pathData == null)
            {
                return null;
            }

            var pathCells = new HashSet<Vector2Int>(pathData.pathCells);

            // 步骤3: 放置动力轮/目标轮
            var gearData = PowerTargetPlacer.PlaceGearsAtEndpoints(pathData.startPoint, pathData.endpoints, allOccupiedCells, random);
            if (gearData == null)
            {
                return null;
            }

            // 验证齿轮放置
            if (!PowerTargetPlacer.ValidateGearPlacement(gearData, pathData.startPoint, pathData.endpoints, pathCells))
            {
                return null;
            }

            // 步骤4: 标记齿轮+移除未覆盖方块
            var finalBlocks = GearMarker.MarkGearsAndFilterBlocks(blockPlacements, pathCells, config, random);
            if (finalBlocks == null || finalBlocks.Count == 0)
            {
                return null;
            }

            // 步骤5: 生成地图
            var mapData = MapGeneratorV2.GenerateMap(finalBlocks, gearData, config, random);
            if (mapData == null)
            {
                return null;
            }

            // 步骤6: 最终验证
            if (!ValidatorV2.ValidateLevel(finalBlocks, mapData, out string validationError))
            {
                Debug.LogWarning($"[V2生成器] 验证失败: {validationError}");
                return null;
            }

            // 步骤7: 创建LevelData
            var levelData = ScriptableObject.CreateInstance<LevelData>();
            DataSerializerV2.SerializeToLevelData(levelData, finalBlocks, mapData, config);

            return levelData;
        }

        /// <summary>
        /// 批量生成关卡
        /// </summary>
        /// <param name="startLevel">起始关卡</param>
        /// <param name="endLevel">结束关卡(包含)</param>
        /// <returns>生成的LevelData数组</returns>
        public static LevelData[] GenerateBatch(int startLevel, int endLevel)
        {
            var results = new List<LevelData>();

            Debug.Log($"[V2生成器] ========== 开始批量生成关卡 {startLevel}-{endLevel} ==========");

            for (int levelIndex = startLevel; levelIndex <= endLevel; levelIndex++)
            {
                var levelData = GenerateLevel(levelIndex);

                if (levelData != null)
                {
                    results.Add(levelData);
                }
                else
                {
                    Debug.LogWarning($"[V2生成器] 关卡 {levelIndex} 生成失败,跳过");
                }
            }

            Debug.Log($"[V2生成器] ========== 批量生成完成 ==========");
            Debug.Log($"[V2生成器] 成功: {results.Count}/{endLevel - startLevel + 1}");

            return results.ToArray();
        }
    }
}
