using UnityEngine;

namespace SuperGear.LevelGeneration.V2
{
    /// <summary>
    /// V2版本关卡难度配置
    /// 核心理念: 路线长度 + 覆盖方块数 = 难度
    /// </summary>
    [System.Serializable]
    public class DifficultyConfigV2
    {
        [Header("基础信息")]
        public int levelIndex;

        [Header("网格配置")]
        public int minGridSize = 5;
        public int maxGridSize = 5;

        [Header("方块配置")]
        [Tooltip("需要组合的方块数量")]
        public int minBlockCount = 2;
        public int maxBlockCount = 3;

        [Tooltip("是否允许方块不连通(后期难度)")]
        public bool allowDisconnectedBlocks = false;

        [Header("路线配置")]
        [Tooltip("路线覆盖的格子数量(核心难度指标)")]
        public int minPathLength = 3;
        public int maxPathLength = 5;

        [Tooltip("路线需要覆盖的最少方块数")]
        public int minCoveredBlocks = 2;

        [Tooltip("是否允许分叉路线(后期难度)")]
        public bool allowBranchedPath = false;

        [Header("动力轮/目标轮配置")]
        [Tooltip("路线端点会自动生成动力轮或目标轮")]
        public int minEndpoints = 2; // 至少2个端点(1动力+1目标)
        public int maxEndpoints = 2;

        [Header("干扰方块(后期难度)")]
        [Tooltip("是否添加干扰方块(未被路线覆盖但有1个齿轮的方块)")]
        public bool addDistractorBlock = false;

        [Header("地图装饰")]
        [Tooltip("额外添加的迷惑接收柱比例(0-1)")]
        [Range(0f, 0.3f)]
        public float extraReceivingBinRatio = 0.05f;

        [Tooltip("缺失点位比例(0-1)")]
        [Range(0f, 0.3f)]
        public float missingBinRatio = 0.1f;

        [Tooltip("障碍物比例(0-1)")]
        [Range(0f, 0.2f)]
        public float obstacleRatio = 0.05f;

        [Tooltip("外圈接收柱密度(0=全空, 0.5=半满)")]
        [Range(0f, 0.5f)]
        public float outerRimDensity = 0.2f;

        [Header("时间限制")]
        public float countdownDuration = 180f;

        [Header("生成控制")]
        public int maxGenerationAttempts = 50; // V2成功率更高,减少尝试次数
        public int randomSeed = -1; // -1表示使用随机种子

        /// <summary>
        /// 获取50关的难度配置(预设)
        /// </summary>
        public static DifficultyConfigV2 GetConfigForLevel(int levelIndex)
        {
            var config = new DifficultyConfigV2 { levelIndex = levelIndex };

            // 教程关卡 (1-10): 简单
            if (levelIndex <= 10)
            {
                config.minGridSize = 5;
                config.maxGridSize = 6;
                config.minBlockCount = 2;
                config.maxBlockCount = 3;
                config.minPathLength = 3;
                config.maxPathLength = 5;
                config.minCoveredBlocks = 2;
                config.minEndpoints = 2;
                config.maxEndpoints = 2;
                config.allowDisconnectedBlocks = false;
                config.allowBranchedPath = false;
                config.addDistractorBlock = false;
                config.extraReceivingBinRatio = 0f;     // 教程关卡：无额外接收柱
                config.missingBinRatio = 0.05f;         // 少量缺失点位
                config.obstacleRatio = 0f;              // 暂无障碍物
                config.outerRimDensity = 0f;            // 关闭外圈
                config.countdownDuration = 180f;
            }
            // 简单关卡 (11-20)
            else if (levelIndex <= 20)
            {
                config.minGridSize = 6;
                config.maxGridSize = 7;
                config.minBlockCount = 3;
                config.maxBlockCount = 4;
                config.minPathLength = 5;
                config.maxPathLength = 8;
                config.minCoveredBlocks = 3;
                config.minEndpoints = 2;
                config.maxEndpoints = 3;
                config.allowDisconnectedBlocks = false;
                config.allowBranchedPath = false;
                config.addDistractorBlock = false;
                config.extraReceivingBinRatio = 0.05f; // 简单关卡：增加装饰
                config.missingBinRatio = 0.08f;
                config.obstacleRatio = 0.03f;           // 开始出现少量障碍物
                config.outerRimDensity = 0f;            // 关闭外圈
                config.countdownDuration = 150f;
            }
            // 中等关卡 (21-30)
            else if (levelIndex <= 30)
            {
                config.minGridSize = 7;
                config.maxGridSize = 8;
                config.minBlockCount = 4;
                config.maxBlockCount = 6;
                config.minPathLength = 8;
                config.maxPathLength = 12;
                config.minCoveredBlocks = 4;
                config.minEndpoints = 3;
                config.maxEndpoints = 4;
                config.allowDisconnectedBlocks = false;
                config.allowBranchedPath = false; // 30及以下不支持分叉
                config.addDistractorBlock = false;
                config.extraReceivingBinRatio = 0.08f; // 中等关卡：更多装饰
                config.missingBinRatio = 0.1f;
                config.obstacleRatio = 0.05f;           // 更多障碍物
                config.outerRimDensity = 0f;            // 关闭外圈
                config.countdownDuration = 120f;
            }
            // 困难关卡 (31-40)
            else if (levelIndex <= 40)
            {
                config.minGridSize = 8;
                config.maxGridSize = 9;
                config.minBlockCount = 5;
                config.maxBlockCount = 8;
                config.minPathLength = 12;
                config.maxPathLength = 18;
                config.minCoveredBlocks = 5;
                config.minEndpoints = 3;
                config.maxEndpoints = 5;
                config.allowDisconnectedBlocks = true; // 允许分散方块
                config.allowBranchedPath = true; // 31+开始支持分叉
                config.addDistractorBlock = false; // 暂时禁用干扰方块
                config.extraReceivingBinRatio = 0.12f; // 困难关卡：大量装饰
                config.missingBinRatio = 0.15f;
                config.obstacleRatio = 0.08f;           // 显著增加障碍物
                config.outerRimDensity = 0f;            // 关闭外圈
                config.countdownDuration = 90f;
            }
            // 专家关卡 (41-50)
            else if (levelIndex <= 50)
            {
                config.minGridSize = 9;
                config.maxGridSize = 10;
                config.minBlockCount = 7;
                config.maxBlockCount = 10;
                config.minPathLength = 18;
                config.maxPathLength = 25;
                config.minCoveredBlocks = 6;
                config.minEndpoints = 4;
                config.maxEndpoints = 6;
                config.allowDisconnectedBlocks = true;
                config.allowBranchedPath = true;
                config.addDistractorBlock = false; // 暂时禁用干扰方块
                config.extraReceivingBinRatio = 0.15f; // 专家关卡：高密度装饰
                config.missingBinRatio = 0.2f;
                config.obstacleRatio = 0.1f;            // 高密度障碍物
                config.outerRimDensity = 0f;            // 关闭外圈
                config.countdownDuration = 75f;
            }
            // 大师关卡 (51+)
            else
            {
                config.minGridSize = 10;
                config.maxGridSize = 12;
                config.minBlockCount = 8;
                config.maxBlockCount = 15;
                config.minPathLength = 25;
                config.maxPathLength = 35;
                config.minCoveredBlocks = 8;
                config.minEndpoints = 5;
                config.maxEndpoints = 8;
                config.allowDisconnectedBlocks = true;
                config.allowBranchedPath = true;
                config.addDistractorBlock = false; // 暂时禁用干扰方块
                config.extraReceivingBinRatio = 0.2f;  // 大师关卡：极限装饰
                config.missingBinRatio = 0.25f;
                config.obstacleRatio = 0.12f;           // 极限障碍物密度
                config.outerRimDensity = 0f;            // 关闭外圈
                config.countdownDuration = 60f;
            }

            return config;
        }

        /// <summary>
        /// 验证配置是否合理
        /// </summary>
        public bool ValidateConfig(out string errorMessage)
        {
            if (minBlockCount < 1)
            {
                errorMessage = "方块数量至少为1";
                return false;
            }

            if (maxBlockCount < minBlockCount)
            {
                errorMessage = "最大方块数不能小于最小方块数";
                return false;
            }

            if (minPathLength < 2)
            {
                errorMessage = "路线长度至少为2";
                return false;
            }

            if (maxPathLength < minPathLength)
            {
                errorMessage = "最大路线长度不能小于最小路线长度";
                return false;
            }

            if (minCoveredBlocks < 1)
            {
                errorMessage = "至少需要覆盖1个方块";
                return false;
            }

            if (minCoveredBlocks > maxBlockCount)
            {
                errorMessage = "覆盖方块数不能大于总方块数";
                return false;
            }

            if (minEndpoints < 2)
            {
                errorMessage = "至少需要2个端点(1动力+1目标)";
                return false;
            }

            if (maxEndpoints < minEndpoints)
            {
                errorMessage = "最大端点数不能小于最小端点数";
                return false;
            }

            errorMessage = "";
            return true;
        }
    }
}
