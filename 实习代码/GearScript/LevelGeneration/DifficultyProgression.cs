using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    /// <summary>
    /// 难度配置类：定义单个关卡的所有生成参数
    /// </summary>
    [System.Serializable]
    public class LevelDifficultyConfig
    {
        [Header("基本信息")]
        public int levelIndex;
        public string difficultyName;
        public float countdownDuration = 60f;

        [Header("网格配置")]
        public int gridXSize = 3;
        public int gridZSize = 3;
        public float missingBinProbability = 0f;
        public int maxMissingBins = 0;

        [Header("齿轮配置")]
        public int powerGearCount = 1;
        public int targetGearCount = 1;
        public int minDistanceBetweenGears = 2;

        [Header("障碍物配置")]
        public int obstacleCount = 0;

        [Header("方块配置")]
        public int minBlockCount = 2;
        public int maxBlockCount = 3;
        public int allowedBlockComplexity = 1;

        [Header("地形配置")]
        public int terrainIndex = 0;

        [Header("生成约束")]
        public int maxGenerationAttempts = 200; // 增加默认尝试次数
    }

    /// <summary>
    /// 难度递增系统（全新版本）：针对新生成器优化的难度曲线
    /// </summary>
    public static class DifficultyProgression
    {
        /// <summary>
        /// 获取指定关卡序号的难度配置
        /// </summary>
        public static LevelDifficultyConfig GetConfigForLevel(int levelIndex)
        {
            var config = new LevelDifficultyConfig
            {
                levelIndex = levelIndex
            };

            // 根据关卡序号确定难度阶段
            if (levelIndex >= 1 && levelIndex <= 10)
            {
                ApplyTutorialSettings(config, levelIndex);
            }
            else if (levelIndex >= 11 && levelIndex <= 20)
            {
                ApplyEasySettings(config, levelIndex);
            }
            else if (levelIndex >= 21 && levelIndex <= 30)
            {
                ApplyMediumSettings(config, levelIndex);
            }
            else if (levelIndex >= 31 && levelIndex <= 40)
            {
                ApplyHardSettings(config, levelIndex);
            }
            else if (levelIndex >= 41 && levelIndex <= 50)
            {
                ApplyExpertSettings(config, levelIndex);
            }
            else
            {
                // 超过50关，使用专家+设置
                ApplyMasterSettings(config, levelIndex);
            }

            return config;
        }

        /// <summary>
        /// 关卡 1-10：教程难度
        /// 目标：让玩家理解基本机制
        /// 优化：更大地图、更多方块、禁用单格方块
        /// </summary>
        private static void ApplyTutorialSettings(LevelDifficultyConfig config, int levelIndex)
        {
            config.difficultyName = "教程";
            config.countdownDuration = 180f; // 3分钟，充足时间

            // 网格：从5x5开始，逐步扩大避免重复感
            config.gridXSize = 5 + (levelIndex - 1) / 5; // 5-6
            config.gridZSize = 5 + (levelIndex - 1) / 5; // 5-6
            config.missingBinProbability = 0.03f; // 减少缺口，避免路径规划困难
            config.maxMissingBins = 0 + (levelIndex - 1) / 5; // 0-2个

            // 齿轮：1动力 + 1目标
            config.powerGearCount = 1;
            config.targetGearCount = 1;
            config.minDistanceBetweenGears = 2;

            // 无障碍物
            config.obstacleCount = 0;

            // 方块：2-6个，范围更宽松
            // 完全无限制所有类型（在SolveBlockPuzzle中已全部开放）
            config.minBlockCount = 2 + (levelIndex - 1) / 4; // 2-4
            config.maxBlockCount = 4 + (levelIndex - 1) / 2; // 4-8
            config.allowedBlockComplexity = 6; // 允许所有复杂度（实际已在代码中全部开放）

            config.terrainIndex = levelIndex % 5; // 地形变化
            config.maxGenerationAttempts = 200; // 增加尝试次数以提高成功率
        }

        /// <summary>
        /// 关卡 11-20：简单难度
        /// 目标：更大地图，更多目标轮
        /// </summary>
        private static void ApplyEasySettings(LevelDifficultyConfig config, int levelIndex)
        {
            config.difficultyName = "简单";
            config.countdownDuration = 150f;

            // 网格：6x6到7x7，继续扩大
            config.gridXSize = 6 + (levelIndex - 11) / 5; // 6-7
            config.gridZSize = 6 + (levelIndex - 11) / 5; // 6-7
            config.missingBinProbability = 0.08f; // 减少缺口概率
            config.maxMissingBins = 2 + (levelIndex - 11) / 3; // 2-5个缺口

            // 齿轮：1-2动力 + 2目标
            config.powerGearCount = 1 + (levelIndex - 11) / 8; // 逐渐增加到2个
            config.targetGearCount = 2 + (levelIndex - 11) / 5; // 2-3个
            config.minDistanceBetweenGears = 1; // 缩短距离，增加挑战

            // 少量障碍物
            config.obstacleCount = (levelIndex - 11) / 5; // 0-1个

            // 方块：3-8个，范围更宽松
            config.minBlockCount = 3 + (levelIndex - 11) / 5; // 3-5
            config.maxBlockCount = 6 + (levelIndex - 11) / 3; // 6-9
            config.allowedBlockComplexity = 6; // 全部开放

            config.terrainIndex = (levelIndex - 11) % 5;
            config.maxGenerationAttempts = 200; // 增加尝试次数以提高成功率
        }

        /// <summary>
        /// 关卡 21-30：中等难度
        /// 目标：更大的网格，T形方块，开始使用齿轮开关特性
        /// </summary>
        private static void ApplyMediumSettings(LevelDifficultyConfig config, int levelIndex)
        {
            config.difficultyName = "中等";
            config.countdownDuration = 120f;

            // 网格：7x7到8x8
            config.gridXSize = 7 + (levelIndex - 21) / 5; // 7-8
            config.gridZSize = 7 + (levelIndex - 21) / 5; // 7-8
            config.missingBinProbability = 0.15f;
            config.maxMissingBins = 5 + (levelIndex - 21) / 2; // 5-10个

            // 齿轮：2动力 + 2-3目标
            config.powerGearCount = 2;
            config.targetGearCount = 2 + (levelIndex - 21) / 4; // 2-4个
            config.minDistanceBetweenGears = 1;

            // 更多障碍物
            config.obstacleCount = 1 + (levelIndex - 21) / 3; // 1-4个

            // 方块：5-9个
            config.minBlockCount = 5 + (levelIndex - 21) / 4;
            config.maxBlockCount = 9 + (levelIndex - 21) / 3;
            config.allowedBlockComplexity = 6; // 全部开放

            config.terrainIndex = (levelIndex - 21) % 5;
            config.maxGenerationAttempts = 200; // 增加尝试次数以提高成功率
        }

        /// <summary>
        /// 关卡 31-40：困难难度
        /// 目标：复杂路径，十字形方块，充分利用齿轮配置
        /// </summary>
        private static void ApplyHardSettings(LevelDifficultyConfig config, int levelIndex)
        {
            config.difficultyName = "困难";
            config.countdownDuration = 90f;

            // 网格：8x8-9x9，复杂形状
            config.gridXSize = 8 + (levelIndex - 31) / 5; // 8-9
            config.gridZSize = 8 + (levelIndex - 31) / 5; // 8-9
            config.missingBinProbability = 0.2f;
            config.maxMissingBins = 8 + (levelIndex - 31) / 2; // 8-13个

            // 齿轮：2-3动力 + 3-5目标
            config.powerGearCount = 2 + (levelIndex - 31) / 6; // 逐渐增加到3个
            config.targetGearCount = 3 + (levelIndex - 31) / 3; // 3-6个
            config.minDistanceBetweenGears = 1;

            // 更多障碍物
            config.obstacleCount = 2 + (levelIndex - 31) / 3; // 2-5个

            // 方块：6-10个
            config.minBlockCount = 6 + (levelIndex - 31) / 4;
            config.maxBlockCount = 10 + (levelIndex - 31) / 3;
            config.allowedBlockComplexity = 6; // 全部开放

            config.terrainIndex = (levelIndex - 31) % 5;
            config.maxGenerationAttempts = 200; // 增加尝试次数以提高成功率
        }

        /// <summary>
        /// 关卡 41-50：专家难度
        /// 目标：大网格，复杂齿轮配置，需要深思熟虑
        /// </summary>
        private static void ApplyExpertSettings(LevelDifficultyConfig config, int levelIndex)
        {
            config.difficultyName = "专家";
            config.countdownDuration = 75f;

            // 网格：9x9-10x10
            config.gridXSize = 9 + (levelIndex - 41) / 5; // 9-10
            config.gridZSize = 9 + (levelIndex - 41) / 5; // 9-10
            config.missingBinProbability = 0.25f;
            config.maxMissingBins = 12 + (levelIndex - 41); // 12-21个

            // 齿轮：3动力 + 4-6目标
            config.powerGearCount = 3;
            config.targetGearCount = 4 + (levelIndex - 41) / 3; // 4-7个
            config.minDistanceBetweenGears = 1;

            // 障碍物
            config.obstacleCount = 3 + (levelIndex - 41) / 3; // 3-6个

            // 方块：7-12个
            config.minBlockCount = 7 + (levelIndex - 41) / 4;
            config.maxBlockCount = 12 + (levelIndex - 41) / 3;
            config.allowedBlockComplexity = 6; // 全部开放

            config.terrainIndex = (levelIndex - 41) % 5;
            config.maxGenerationAttempts = 150;
        }

        /// <summary>
        /// 关卡 51+：大师难度
        /// 目标：极限挑战
        /// </summary>
        private static void ApplyMasterSettings(LevelDifficultyConfig config, int levelIndex)
        {
            config.difficultyName = "大师";
            config.countdownDuration = 60f;

            // 网格：10x10+
            int extra = (levelIndex - 51) / 10;
            config.gridXSize = 10 + Mathf.Min(extra, 2); // 10-12
            config.gridZSize = 10 + Mathf.Min(extra, 2); // 10-12
            config.missingBinProbability = 0.3f;
            config.maxMissingBins = 20 + (levelIndex - 51) / 2; // 20+

            // 齿轮：3-4动力 + 6+目标
            config.powerGearCount = 3 + (levelIndex - 51) / 15; // 3-4
            config.targetGearCount = 6 + (levelIndex - 51) / 5; // 6+
            config.minDistanceBetweenGears = 1;

            // 障碍物
            config.obstacleCount = 4 + (levelIndex - 51) / 5; // 4+

            // 方块：8-15个
            config.minBlockCount = 8 + (levelIndex - 51) / 8;
            config.maxBlockCount = 15 + (levelIndex - 51) / 6;
            config.allowedBlockComplexity = 6; // 全部开放

            config.terrainIndex = (levelIndex - 51) % 5;
            config.maxGenerationAttempts = 200;
        }

        /// <summary>
        /// 批量获取多个关卡的配置
        /// </summary>
        public static List<LevelDifficultyConfig> GetConfigsForRange(int startLevel, int endLevel)
        {
            var configs = new List<LevelDifficultyConfig>();

            for (int i = startLevel; i <= endLevel; i++)
            {
                configs.Add(GetConfigForLevel(i));
            }

            return configs;
        }

        /// <summary>
        /// 打印难度配置（调试用）
        /// </summary>
        public static void DebugPrintConfig(LevelDifficultyConfig config)
        {
            Debug.Log($"=== 关卡 {config.levelIndex} ({config.difficultyName}) ===\n" +
                      $"网格: {config.gridXSize}x{config.gridZSize}\n" +
                      $"动力齿轮: {config.powerGearCount}, 目标齿轮: {config.targetGearCount}\n" +
                      $"方块: {config.minBlockCount}-{config.maxBlockCount} (复杂度: {config.allowedBlockComplexity})\n" +
                      $"障碍物: {config.obstacleCount}, 最大缺口: {config.maxMissingBins}\n" +
                      $"倒计时: {config.countdownDuration}秒\n" +
                      $"地形: {config.terrainIndex}");
        }

        /// <summary>
        /// 验证难度配置的合理性
        /// </summary>
        public static bool ValidateConfig(LevelDifficultyConfig config)
        {
            if (config.gridXSize < 3 || config.gridZSize < 3)
            {
                Debug.LogError($"关卡 {config.levelIndex}: 网格尺寸过小");
                return false;
            }

            if (config.powerGearCount < 1 || config.targetGearCount < 1)
            {
                Debug.LogError($"关卡 {config.levelIndex}: 齿轮数量不足");
                return false;
            }

            if (config.minBlockCount < 1 || config.maxBlockCount < config.minBlockCount)
            {
                Debug.LogError($"关卡 {config.levelIndex}: 方块数量配置错误");
                return false;
            }

            int totalCells = config.gridXSize * config.gridZSize;
            int requiredCells = config.powerGearCount + config.targetGearCount + config.obstacleCount;
            if (requiredCells > totalCells / 2)
            {
                Debug.LogError($"关卡 {config.levelIndex}: 网格容量可能不足");
                return false;
            }

            return true;
        }
    }
}
