using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace WoolyPath.Editor
{
    /// <summary>
    /// LevelManager 编辑器扩展，提供关卡管理工具
    /// </summary>
    public class LevelManagerEditorWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private LevelData[] allLevels;
        private bool autoRefresh = true;
        private string searchFilter = "";

        [MenuItem("WoolyPath/关卡管理器")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelManagerEditorWindow>("关卡管理器");
            window.minSize = new Vector2(400, 300);
            window.LoadLevels();
        }

        private void OnEnable()
        {
            LoadLevels();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 标题
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("关卡管理器", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                autoRefresh = GUILayout.Toggle(autoRefresh, "自动刷新", GUILayout.Width(80));

                if (GUILayout.Button("刷新", GUILayout.Width(60)))
                {
                    LoadLevels();
                }
            }

            EditorGUILayout.Space(5);

            // 搜索栏
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("搜索:", GUILayout.Width(40));
                searchFilter = EditorGUILayout.TextField(searchFilter);

                if (GUILayout.Button("清除", GUILayout.Width(50)))
                {
                    searchFilter = "";
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.Separator();

            // 关卡信息统计
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"总关卡数: {allLevels?.Length ?? 0}", EditorStyles.helpBox);

                if (allLevels != null && allLevels.Length > 0)
                {
                    int totalSheep = allLevels.Sum(l => l.totalSheepCount);
                    int totalCollectors = allLevels.Sum(l => l.collectors?.Length ?? 0);
                    EditorGUILayout.LabelField($"总羊数: {totalSheep}", EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"总收集器: {totalCollectors}", EditorStyles.helpBox);
                }
            }

            EditorGUILayout.Space(5);

            // 工具按钮
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("加载到LevelManager", GUILayout.Height(30)))
                {
                    LoadLevelsToManager();
                }

                if (GUILayout.Button("验证所有关卡", GUILayout.Height(30)))
                {
                    ValidateAllLevels();
                }

                if (GUILayout.Button("修复关卡索引", GUILayout.Height(30)))
                {
                    FixLevelIndexes();
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.Separator();

            // 关卡列表
            if (allLevels == null || allLevels.Length == 0)
            {
                EditorGUILayout.HelpBox("未找到关卡文件！请检查 Assets/Data/Levels 文件夹。", MessageType.Warning);
                return;
            }

            // 显示关卡列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var filteredLevels = string.IsNullOrEmpty(searchFilter)
                ? allLevels
                : allLevels.Where(l => l.name.ToLower().Contains(searchFilter.ToLower())).ToArray();

            foreach (var level in filteredLevels)
            {
                DrawLevelInfo(level);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLevelInfo(LevelData level)
        {
            if (level == null) return;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 关卡标题行
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 关卡名称和索引
                    EditorGUILayout.LabelField($"[{level.levelIndex}] {level.name}", EditorStyles.boldLabel);

                    // 选择按钮
                    if (GUILayout.Button("选择", GUILayout.Width(50)))
                    {
                        Selection.activeObject = level;
                        EditorGUIUtility.PingObject(level);
                    }

                    // 加载按钮（仅在运行时可用）
                    GUI.enabled = Application.isPlaying && LevelManager.Instance != null;
                    if (GUILayout.Button("加载", GUILayout.Width(50)))
                    {
                        LevelManager.Instance.LoadLevel(level.levelIndex);
                        Debug.Log($"[LevelManagerEditor] 加载关卡: {level.name}");
                    }
                    GUI.enabled = true;
                }

                // 关卡详细信息
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"网格: {level.GridSize.x}x{level.GridSize.y}", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"羊数: {level.totalSheepCount}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"收集器: {level.collectors?.Length ?? 0}", GUILayout.Width(80));

                    // 显示羊的颜色分布
                    if (level.levelSheepPrefabsWeight != null && level.levelSheepPrefabsWeight.Length > 0)
                    {
                        string colors = string.Join(", ", level.levelSheepPrefabsWeight
                            .Where(w => w.Quantity > 0)
                            .Select(w => $"{w.color}({w.Quantity})"));
                        EditorGUILayout.LabelField($"颜色: {colors}", GUILayout.MinWidth(100));
                    }
                }

                // 显示警告
                if (level.collectors == null || level.collectors.Length == 0)
                {
                    EditorGUILayout.HelpBox("警告: 此关卡没有配置收集器!", MessageType.Warning);
                }

                if (level.totalSheepCount == 0)
                {
                    EditorGUILayout.HelpBox("警告: 此关卡羊数为0!", MessageType.Warning);
                }
            }
        }

        private void LoadLevels()
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Data/Levels" });
            List<LevelData> levelsList = new List<LevelData>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (level != null)
                {
                    levelsList.Add(level);
                }
            }

            // 按名称排序
            allLevels = levelsList.OrderBy(level =>
            {
                string levelName = level.name;
                string numberStr = System.Text.RegularExpressions.Regex.Match(levelName, @"\d+").Value;
                if (int.TryParse(numberStr, out int levelNumber))
                {
                    return levelNumber;
                }
                return 999;
            }).ToArray();

            Debug.Log($"[LevelManagerEditor] 加载了 {allLevels.Length} 个关卡");
        }

        private void LoadLevelsToManager()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("提示", "请先运行游戏才能加载关卡到 LevelManager!", "确定");
                return;
            }

            if (LevelManager.Instance == null)
            {
                EditorUtility.DisplayDialog("错误", "LevelManager 实例不存在!", "确定");
                return;
            }

            // 修复索引
            for (int i = 0; i < allLevels.Length; i++)
            {
                allLevels[i].levelIndex = i;
                allLevels[i].levelName = $"Level {i + 1}";
            }

            LevelManager.Instance.SetLevels(allLevels);
            Debug.Log($"[LevelManagerEditor] 成功将 {allLevels.Length} 个关卡加载到 LevelManager");

            EditorUtility.DisplayDialog("成功", $"已将 {allLevels.Length} 个关卡加载到 LevelManager!", "确定");
        }

        private void ValidateAllLevels()
        {
            int warningCount = 0;
            int errorCount = 0;

            foreach (var level in allLevels)
            {
                if (level == null)
                {
                    errorCount++;
                    Debug.LogError("[LevelManagerEditor] 发现空关卡引用!");
                    continue;
                }

                // 检查收集器
                if (level.collectors == null || level.collectors.Length == 0)
                {
                    warningCount++;
                    Debug.LogWarning($"[LevelManagerEditor] 关卡 {level.name} 没有配置收集器!");
                }

                // 检查羊数
                if (level.totalSheepCount == 0)
                {
                    warningCount++;
                    Debug.LogWarning($"[LevelManagerEditor] 关卡 {level.name} 的羊数为0!");
                }

                // 检查网格大小
                if (level.GridSize.x <= 0 || level.GridSize.y <= 0)
                {
                    errorCount++;
                    Debug.LogError($"[LevelManagerEditor] 关卡 {level.name} 的网格大小无效!");
                }

                // 检查羊权重配置
                if (level.levelSheepPrefabsWeight == null || level.levelSheepPrefabsWeight.Length == 0)
                {
                    warningCount++;
                    Debug.LogWarning($"[LevelManagerEditor] 关卡 {level.name} 没有配置羊的颜色权重!");
                }
            }

            string message = $"验证完成!\n错误: {errorCount}\n警告: {warningCount}";

            if (errorCount > 0)
            {
                EditorUtility.DisplayDialog("验证结果", message, "确定");
            }
            else if (warningCount > 0)
            {
                EditorUtility.DisplayDialog("验证结果", message, "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("验证成功", "所有关卡验证通过!", "确定");
            }
        }

        private void FixLevelIndexes()
        {
            if (allLevels == null || allLevels.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有找到关卡文件!", "确定");
                return;
            }

            for (int i = 0; i < allLevels.Length; i++)
            {
                if (allLevels[i] != null)
                {
                    allLevels[i].levelIndex = i;
                    allLevels[i].levelName = $"Level {i + 1}";
                    EditorUtility.SetDirty(allLevels[i]);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"已修复 {allLevels.Length} 个关卡的索引!", "确定");
            Debug.Log($"[LevelManagerEditor] 修复了 {allLevels.Length} 个关卡的索引");
        }

        private void OnInspectorUpdate()
        {
            // 自动刷新
            if (autoRefresh)
            {
                Repaint();
            }
        }
    }

    /// <summary>
    /// GameSceneSetup 自定义编辑器
    /// </summary>
    [CustomEditor(typeof(GameSceneSetup))]
    public class GameSceneSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("关卡管理工具", EditorStyles.boldLabel);

            if (GUILayout.Button("打开关卡管理器", GUILayout.Height(30)))
            {
                LevelManagerEditorWindow.ShowWindow();
            }

            if (GUILayout.Button("在编辑器中加载关卡", GUILayout.Height(25)))
            {
                GameSceneSetup setup = (GameSceneSetup)target;
                var loadMethod = setup.GetType().GetMethod("LoadLevelsFromAssets",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (loadMethod != null)
                {
                    loadMethod.Invoke(setup, null);
                    Debug.Log("[GameSceneSetupEditor] 在编辑器中加载关卡完成");
                }
            }
        }
    }
}