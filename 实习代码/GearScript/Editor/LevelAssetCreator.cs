using UnityEngine;
using UnityEditor;
using System.IO;

namespace SuperGear
{
    /// <summary>
    /// Unity编辑器工具：创建LevelData asset文件
    /// </summary>
    public static class LevelAssetCreator
    {
        private const string LEVEL_DATA_FOLDER = "Assets/Data/LevelData";

        /// <summary>
        /// 获取关卡资源路径
        /// </summary>
        public static string GetLevelAssetPath(int levelIndex)
        {
            string fileName = $"LevelData_{levelIndex}.asset";
            return Path.Combine(LEVEL_DATA_FOLDER, fileName);
        }

        /// <summary>
        /// 创建单个LevelData asset文件
        /// </summary>
        public static bool CreateLevelAsset(LevelData levelData, bool overwrite = false)
        {
            if (levelData == null)
            {
                Debug.LogError("LevelData为空，无法创建asset");
                return false;
            }

            // 确保目录存在
            if (!Directory.Exists(LEVEL_DATA_FOLDER))
            {
                Directory.CreateDirectory(LEVEL_DATA_FOLDER);
                Debug.Log($"创建目录: {LEVEL_DATA_FOLDER}");
            }

            // 生成文件路径
            string fileName = $"LevelData_{levelData.LevelIndex}.asset";
            string assetPath = Path.Combine(LEVEL_DATA_FOLDER, fileName);

            // 检查文件是否已存在
            if (File.Exists(assetPath))
            {
                if (!overwrite)
                {
                    Debug.LogWarning($"文件已存在: {assetPath}，跳过创建（设置overwrite=true可覆盖）");
                    return false;
                }
                else
                {
                    Debug.Log($"覆盖现有文件: {assetPath}");
                }
            }

            try
            {
                // 创建或更新asset
                if (File.Exists(assetPath))
                {
                    // 更新现有asset
                    var existingAsset = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                    if (existingAsset != null)
                    {
                        EditorUtility.CopySerialized(levelData, existingAsset);
                        EditorUtility.SetDirty(existingAsset);
                        Debug.Log($"更新LevelData asset: {assetPath}");
                    }
                    else
                    {
                        // 如果无法加载，删除后重新创建
                        AssetDatabase.DeleteAsset(assetPath);
                        AssetDatabase.CreateAsset(levelData, assetPath);
                        Debug.Log($"重新创建LevelData asset: {assetPath}");
                    }
                }
                else
                {
                    // 创建新asset
                    AssetDatabase.CreateAsset(levelData, assetPath);
                    Debug.Log($"创建LevelData asset: {assetPath}");
                }

                // 保存并刷新
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 选中创建的asset
                var createdAsset = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
                if (createdAsset != null)
                {
                    EditorGUIUtility.PingObject(createdAsset);
                    Selection.activeObject = createdAsset;
                }

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"创建asset时发生错误: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 批量创建LevelData assets
        /// </summary>
        public static int CreateLevelAssetsBatch(LevelData[] levelDatas, bool overwrite = false)
        {
            if (levelDatas == null || levelDatas.Length == 0)
            {
                Debug.LogError("没有要创建的LevelData");
                return 0;
            }

            int successCount = 0;
            int totalCount = levelDatas.Length;

            for (int i = 0; i < totalCount; i++)
            {
                var levelData = levelDatas[i];

                if (levelData == null)
                {
                    Debug.LogWarning($"第{i + 1}个LevelData为空，跳过");
                    continue;
                }

                // 显示进度条
                float progress = (float)i / totalCount;
                string progressText = $"创建关卡 {levelData.LevelIndex} ({i + 1}/{totalCount})";
                EditorUtility.DisplayProgressBar("批量创建关卡", progressText, progress);

                // 创建asset
                bool success = CreateLevelAsset(levelData, overwrite);
                if (success)
                {
                    successCount++;
                }
            }

            // 清除进度条
            EditorUtility.ClearProgressBar();

            Debug.Log($"批量创建完成：成功 {successCount}/{totalCount}");
            return successCount;
        }

        /// <summary>
        /// 删除指定的LevelData asset
        /// </summary>
        public static bool DeleteLevelAsset(int levelIndex)
        {
            string fileName = $"LevelData_{levelIndex}.asset";
            string assetPath = Path.Combine(LEVEL_DATA_FOLDER, fileName);

            if (!File.Exists(assetPath))
            {
                Debug.LogWarning($"文件不存在: {assetPath}");
                return false;
            }

            try
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.Refresh();
                Debug.Log($"删除LevelData asset: {assetPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"删除asset时发生错误: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 批量删除LevelData assets
        /// </summary>
        public static int DeleteLevelAssetsBatch(int startIndex, int endIndex)
        {
            int deletedCount = 0;

            for (int i = startIndex; i <= endIndex; i++)
            {
                if (DeleteLevelAsset(i))
                {
                    deletedCount++;
                }
            }

            Debug.Log($"批量删除完成：删除了 {deletedCount} 个关卡");
            return deletedCount;
        }

        /// <summary>
        /// 检查LevelData asset是否存在
        /// </summary>
        public static bool LevelAssetExists(int levelIndex)
        {
            string fileName = $"LevelData_{levelIndex}.asset";
            string assetPath = Path.Combine(LEVEL_DATA_FOLDER, fileName);
            return File.Exists(assetPath);
        }

        /// <summary>
        /// 获取现有的所有LevelData assets
        /// </summary>
        public static LevelData[] GetAllExistingLevelAssets()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { LEVEL_DATA_FOLDER });
            var levelDatas = new LevelData[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                levelDatas[i] = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            }

            // 按关卡序号排序
            System.Array.Sort(levelDatas, (a, b) => a.LevelIndex.CompareTo(b.LevelIndex));

            return levelDatas;
        }

        /// <summary>
        /// 获取下一个可用的关卡序号
        /// </summary>
        public static int GetNextAvailableLevelIndex()
        {
            var existingLevels = GetAllExistingLevelAssets();

            if (existingLevels.Length == 0)
                return 1;

            // 返回最大序号+1
            int maxIndex = existingLevels[existingLevels.Length - 1].LevelIndex;
            return maxIndex + 1;
        }

        /// <summary>
        /// 验证asset的完整性
        /// </summary>
        public static bool ValidateLevelAsset(LevelData levelData)
        {
            if (levelData == null)
                return false;

            // 检查基本配置
            if (levelData.LevelIndex <= 0)
            {
                Debug.LogError($"关卡序号无效: {levelData.LevelIndex}");
                return false;
            }

            if (levelData._receivingBinGenerateData == null)
            {
                Debug.LogError($"关卡 {levelData.LevelIndex}: 缺少接收槽配置");
                return false;
            }

            if (levelData._blockGenerateData == null || levelData._blockGenerateData.generateItems.Count == 0)
            {
                Debug.LogError($"关卡 {levelData.LevelIndex}: 缺少方块配置");
                return false;
            }

            // 检查齿轮数量
            var binData = levelData._receivingBinGenerateData;
            if (binData.powerGearPositions == null || binData.powerGearPositions.Length == 0)
            {
                Debug.LogError($"关卡 {levelData.LevelIndex}: 缺少动力齿轮");
                return false;
            }

            if (binData.targetGearPositions == null || binData.targetGearPositions.Length == 0)
            {
                Debug.LogError($"关卡 {levelData.LevelIndex}: 缺少目标齿轮");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 打印asset信息（调试用）
        /// </summary>
        public static void PrintLevelAssetInfo(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.Log("LevelData为空");
                return;
            }

            Debug.Log($"=== LevelData_{levelData.LevelIndex} ===\n" +
                      $"网格大小: {levelData._receivingBinGenerateData.xRowCount}x{levelData._receivingBinGenerateData.zColumnCount}\n" +
                      $"动力齿轮: {levelData._receivingBinGenerateData.powerGearPositions.Length}\n" +
                      $"目标齿轮: {levelData._receivingBinGenerateData.targetGearPositions.Length}\n" +
                      $"障碍物: {levelData._receivingBinGenerateData.obstaclePositions.Length}\n" +
                      $"可用方块: {levelData._blockGenerateData.generateItems.Count}\n" +
                      $"倒计时: {levelData.countdownDuration}秒\n" +
                      $"地形: {levelData._terrain}");
        }

        /// <summary>
        /// 在Project窗口中选中指定关卡
        /// </summary>
        public static void SelectLevelAsset(int levelIndex)
        {
            string fileName = $"LevelData_{levelIndex}.asset";
            string assetPath = Path.Combine(LEVEL_DATA_FOLDER, fileName);

            var asset = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            else
            {
                Debug.LogWarning($"找不到关卡: {assetPath}");
            }
        }
    }
}
