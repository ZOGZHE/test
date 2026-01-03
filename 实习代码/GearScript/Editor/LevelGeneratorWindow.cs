using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SuperGear
{
    /// <summary>
    /// Unity编辑器窗口：关卡生成器界面
    /// 使用方法：Unity菜单栏 → Tools → Level Generator
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        // 算法版本
        private enum AlgorithmVersion
        {
            V2,     // V2版本(主动设计,从内向外)
        }

        // 生成模式
        private enum GenerationMode
        {
            Single,     // 单个生成
            Batch       // 批量生成
        }

        private AlgorithmVersion algorithmVersion = AlgorithmVersion.V2;
        private GenerationMode mode = GenerationMode.Single;

        // 单个生成参数
        private int singleLevelIndex = 1;
        private int randomSeed = -1;

        // 批量生成参数
        private int batchStartLevel = 8;
        private int batchEndLevel = 57;
        private int batchRandomSeed = -1;  // 批量生成的基础随机种子 (-1为随机)

        // 通用参数
        private bool overwriteExisting = false;
        private bool autoSelectCreated = true;

        // 高级选项
        private bool showAdvancedOptions = false;
        private bool useCustomDifficulty = false;
        private LevelDifficultyConfig customConfig;  // V1使用
        private LevelGeneration.V2.DifficultyConfigV2 customConfigV2;  // V2使用

        // 难度取消选项
        private bool useDifficultyProgression = true;

        // UI状态
        private Vector2 scrollPosition;
        private bool isGenerating = false;
        private string statusMessage = "";

        [MenuItem("Tools/Level Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelGeneratorWindow>("关卡生成器");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // 初始化
            customConfig = new LevelDifficultyConfig();
            customConfigV2 = new LevelGeneration.V2.DifficultyConfigV2();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawModeSelection();
            EditorGUILayout.Space(10);

            DrawGenerationParameters();
            EditorGUILayout.Space(10);

            DrawCommonOptions();
            EditorGUILayout.Space(10);

            DrawAdvancedOptions();
            EditorGUILayout.Space(10);

            DrawGenerateButtons();
            EditorGUILayout.Space(10);

            DrawStatusSection();
            EditorGUILayout.Space(10);

            DrawExistingLevelsSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("IQ Gears - 关卡生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "使用此工具批量生成puzzle关卡。支持单个生成和批量生成。\n" +
                "关卡会自动保存到 Assets/Data/LevelData/ 目录。",
                MessageType.Info);
        }

        private void DrawModeSelection()
        {
            EditorGUILayout.LabelField("算法版本", EditorStyles.boldLabel);
            algorithmVersion = (AlgorithmVersion)EditorGUILayout.EnumPopup("算法", algorithmVersion);

            EditorGUILayout.HelpBox(
                "V2: 主动设计算法,从中心向外扩展路线,成功率高,速度快",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("生成模式", EditorStyles.boldLabel);
            mode = (GenerationMode)EditorGUILayout.EnumPopup("模式", mode);
        }

        private void DrawGenerationParameters()
        {
            EditorGUILayout.LabelField("生成参数", EditorStyles.boldLabel);

            if (mode == GenerationMode.Single)
            {
                // 单个生成
                singleLevelIndex = EditorGUILayout.IntField("关卡序号", singleLevelIndex);
                singleLevelIndex = Mathf.Max(1, singleLevelIndex);

                randomSeed = EditorGUILayout.IntField("随机种子 (-1为随机)", randomSeed);

                // 显示难度预览
                if (singleLevelIndex > 0 && algorithmVersion == AlgorithmVersion.V2)
                {
                    var configV2 = LevelGeneration.V2.DifficultyConfigV2.GetConfigForLevel(singleLevelIndex);
                    EditorGUILayout.HelpBox(
                        $"网格: {configV2.minGridSize}-{configV2.maxGridSize}\n" +
                        $"方块: {configV2.minBlockCount}-{configV2.maxBlockCount}\n" +
                        $"路线长度: {configV2.minPathLength}-{configV2.maxPathLength}\n" +
                        $"端点: {configV2.minEndpoints}-{configV2.maxEndpoints}\n" +
                        $"倒计时: {configV2.countdownDuration}秒",
                        MessageType.None);
                }

                // 检查是否已存在
                if (LevelAssetCreator.LevelAssetExists(singleLevelIndex))
                {
                    EditorGUILayout.HelpBox(
                        $"⚠️ LevelData_{singleLevelIndex}.asset 已存在！",
                        MessageType.Warning);
                }
            }
            else
            {
                // 批量生成
                EditorGUILayout.BeginHorizontal();
                batchStartLevel = EditorGUILayout.IntField("起始关卡", batchStartLevel);
                batchEndLevel = EditorGUILayout.IntField("结束关卡", batchEndLevel);
                EditorGUILayout.EndHorizontal();

                batchStartLevel = Mathf.Max(1, batchStartLevel);
                batchEndLevel = Mathf.Max(batchStartLevel, batchEndLevel);

                batchRandomSeed = EditorGUILayout.IntField("随机种子 (-1为随机)", batchRandomSeed);

                if (batchRandomSeed != -1)
                {
                    EditorGUILayout.HelpBox(
                        $"使用固定种子 {batchRandomSeed}，每次生成同样的关卡组\n" +
                        "种子相同 = 关卡内容一致，适合调试和小幅修改代码后对比",
                        MessageType.Info);
                }

                int totalLevels = batchEndLevel - batchStartLevel + 1;
                EditorGUILayout.HelpBox($"将生成 {totalLevels} 个关卡", MessageType.Info);

                // 检查已存在的关卡
                int existingCount = 0;
                for (int i = batchStartLevel; i <= batchEndLevel; i++)
                {
                    if (LevelAssetCreator.LevelAssetExists(i))
                        existingCount++;
                }

                if (existingCount > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"⚠️ 有 {existingCount} 个关卡已存在！",
                        MessageType.Warning);
                }
            }
        }

        private void DrawCommonOptions()
        {
            EditorGUILayout.LabelField("通用选项", EditorStyles.boldLabel);

            overwriteExisting = EditorGUILayout.Toggle("覆盖已存在的关卡", overwriteExisting);
            autoSelectCreated = EditorGUILayout.Toggle("自动选中创建的关卡", autoSelectCreated);

            if (mode == GenerationMode.Batch)
            {
                useDifficultyProgression = EditorGUILayout.Toggle("使用难度递增", useDifficultyProgression);
                if (!useDifficultyProgression)
                {
                    EditorGUILayout.HelpBox(
                        "关闭难度递增后,所有关卡将使用第一个关卡的难度配置",
                        MessageType.Warning);
                }
            }
        }

        private void DrawAdvancedOptions()
        {
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "高级选项");

            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;

                useCustomDifficulty = EditorGUILayout.Toggle("使用自定义难度", useCustomDifficulty);

                if (useCustomDifficulty && customConfig != null)
                {
                    EditorGUILayout.HelpBox(
                        "自定义难度配置（仅对单个生成有效）",
                        MessageType.Info);

                    customConfig.gridXSize = EditorGUILayout.IntField("网格X尺寸", customConfig.gridXSize);
                    customConfig.gridZSize = EditorGUILayout.IntField("网格Z尺寸", customConfig.gridZSize);
                    customConfig.powerGearCount = EditorGUILayout.IntField("动力齿轮数量", customConfig.powerGearCount);
                    customConfig.targetGearCount = EditorGUILayout.IntField("目标齿轮数量", customConfig.targetGearCount);
                    customConfig.minBlockCount = EditorGUILayout.IntField("最少方块数", customConfig.minBlockCount);
                    customConfig.maxBlockCount = EditorGUILayout.IntField("最多方块数", customConfig.maxBlockCount);
                    customConfig.allowedBlockComplexity = EditorGUILayout.IntSlider("方块复杂度", customConfig.allowedBlockComplexity, 1, 5);
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawGenerateButtons()
        {
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

            GUI.enabled = !isGenerating;

            if (mode == GenerationMode.Single)
            {
                if (GUILayout.Button("生成单个关卡", GUILayout.Height(40)))
                {
                    GenerateSingleLevel();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("预览难度配置"))
                {
                    PreviewDifficultyConfig(singleLevelIndex);
                }
                if (GUILayout.Button("删除此关卡"))
                {
                    DeleteSingleLevel();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("批量生成关卡", GUILayout.Height(40)))
                {
                    GenerateBatchLevels();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("预览难度曲线"))
                {
                    PreviewDifficultyCurve();
                }
                if (GUILayout.Button("删除范围内关卡"))
                {
                    DeleteBatchLevels();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = true;
        }

        private void DrawStatusSection()
        {
            EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);

            if (isGenerating)
            {
                EditorGUILayout.HelpBox("⏳ 正在生成关卡，请稍候...", MessageType.Info);
            }
            else if (!string.IsNullOrEmpty(statusMessage))
            {
                MessageType messageType = statusMessage.Contains("成功") ? MessageType.Info :
                                         statusMessage.Contains("失败") ? MessageType.Error :
                                         MessageType.Warning;
                EditorGUILayout.HelpBox(statusMessage, messageType);
            }
        }

        private void DrawExistingLevelsSection()
        {
            EditorGUILayout.LabelField("已有关卡", EditorStyles.boldLabel);

            var existingLevels = LevelAssetCreator.GetAllExistingLevelAssets();

            if (existingLevels.Length == 0)
            {
                EditorGUILayout.HelpBox("还没有创建任何关卡", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"共有 {existingLevels.Length} 个关卡", MessageType.Info);

                // 显示前10个和后10个
                int displayCount = Mathf.Min(10, existingLevels.Length);

                for (int i = 0; i < displayCount; i++)
                {
                    var level = existingLevels[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Level {level.LevelIndex}", GUILayout.Width(80));

                    if (GUILayout.Button("选中", GUILayout.Width(50)))
                    {
                        LevelAssetCreator.SelectLevelAsset(level.LevelIndex);
                    }

                    if (GUILayout.Button("信息", GUILayout.Width(50)))
                    {
                        LevelAssetCreator.PrintLevelAssetInfo(level);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (existingLevels.Length > displayCount)
                {
                    EditorGUILayout.LabelField($"... 还有 {existingLevels.Length - displayCount} 个关卡");
                }

                if (GUILayout.Button("在Project窗口查看全部"))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>("Assets/Data/LevelData");
                    EditorGUIUtility.PingObject(Selection.activeObject);
                }
            }
        }

        // === 生成逻辑 ===

        private void GenerateSingleLevel()
        {
            isGenerating = true;
            statusMessage = "";

            try
            {
                LevelData levelData = null;

                // 使用V2算法
                if (algorithmVersion == AlgorithmVersion.V2)
                {
                    // 生成关卡（传递随机种子）
                    levelData = LevelGeneration.V2.LevelGeneratorV2.GenerateLevel(singleLevelIndex, randomSeed);

                    if (levelData == null)
                    {
                        statusMessage = $"❌ 关卡 {singleLevelIndex} 生成失败！";
                        return;
                    }

                    // 创建asset
                    bool success = LevelAssetCreator.CreateLevelAsset(levelData, overwriteExisting);
                    if (!success)
                    {
                        statusMessage = $"⚠️ 关卡 {singleLevelIndex} 生成成功，但asset创建失败";
                        return;
                    }
                }

                statusMessage = $"✅ 关卡 {singleLevelIndex} 生成成功！";

                if (autoSelectCreated)
                {
                    LevelAssetCreator.SelectLevelAsset(singleLevelIndex);
                }
            }
            catch (System.Exception e)
            {
                statusMessage = $"❌ 发生错误: {e.Message}";
                Debug.LogError(e);
            }
            finally
            {
                isGenerating = false;
            }
        }

        private void GenerateBatchLevels()
        {
            isGenerating = true;
            statusMessage = "";

            try
            {
                int totalLevels = batchEndLevel - batchStartLevel + 1;
                int successCount = 0;

                // 使用V2算法批量生成
                if (algorithmVersion == AlgorithmVersion.V2)
                {
                    for (int i = batchStartLevel; i <= batchEndLevel; i++)
                    {
                        // 显示进度
                        float progress = (float)(i - batchStartLevel) / totalLevels;
                        EditorUtility.DisplayProgressBar(
                            "批量生成关卡 (V2)",
                            $"正在生成关卡 {i}... ({i - batchStartLevel + 1}/{totalLevels})",
                            progress);

                        // 计算该关卡的随机种子
                        // 如果指定了基础种子，使用 baseSeed + levelIndex 确保可重复性
                        // 如果未指定（-1），则使用默认随机种子
                        int levelSeed = batchRandomSeed != -1 ? (batchRandomSeed + i) : -1;

                        // 生成关卡
                        var levelData = LevelGeneration.V2.LevelGeneratorV2.GenerateLevel(i, levelSeed);

                        if (levelData != null)
                        {
                            // 创建asset
                            bool success = LevelAssetCreator.CreateLevelAsset(levelData, overwriteExisting);
                            if (success)
                            {
                                successCount++;
                            }
                        }
                        else
                        {
                            Debug.LogError($"关卡 {i} 生成失败！");
                        }
                    }
                }

                EditorUtility.ClearProgressBar();

                int failedCount = totalLevels - successCount;

                if (failedCount > 0)
                {
                    statusMessage = $"⚠️ 批量生成完成！成功: {successCount}/{totalLevels}, 失败: {failedCount}";
                }
                else
                {
                    statusMessage = $"✅ 批量生成完成！成功: {successCount}/{totalLevels}";
                }
            }
            catch (System.Exception e)
            {
                statusMessage = $"❌ 发生错误: {e.Message}";
                Debug.LogError(e);
                EditorUtility.ClearProgressBar();
            }
            finally
            {
                isGenerating = false;
            }
        }

        private void DeleteSingleLevel()
        {
            if (EditorUtility.DisplayDialog(
                "确认删除",
                $"确定要删除 LevelData_{singleLevelIndex}.asset 吗？",
                "删除", "取消"))
            {
                bool success = LevelAssetCreator.DeleteLevelAsset(singleLevelIndex);
                statusMessage = success
                    ? $"✅ 关卡 {singleLevelIndex} 已删除"
                    : $"❌ 关卡 {singleLevelIndex} 删除失败";
            }
        }

        private void DeleteBatchLevels()
        {
            int count = batchEndLevel - batchStartLevel + 1;

            if (EditorUtility.DisplayDialog(
                "确认批量删除",
                $"确定要删除 {batchStartLevel} 到 {batchEndLevel} 的 {count} 个关卡吗？",
                "删除", "取消"))
            {
                int deletedCount = LevelAssetCreator.DeleteLevelAssetsBatch(batchStartLevel, batchEndLevel);
                statusMessage = $"✅ 已删除 {deletedCount} 个关卡";
            }
        }

        private void PreviewDifficultyConfig(int levelIndex)
        {
            var config = DifficultyProgression.GetConfigForLevel(levelIndex);
            DifficultyProgression.DebugPrintConfig(config);
        }

        private void PreviewDifficultyCurve()
        {
            Debug.Log("=== 难度曲线预览 ===");
            var configs = DifficultyProgression.GetConfigsForRange(batchStartLevel, batchEndLevel);
            foreach (var config in configs)
            {
                DifficultyProgression.DebugPrintConfig(config);
            }
        }
    }
}
