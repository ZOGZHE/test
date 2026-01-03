using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SuperGear;

namespace SuperGear
{
    /// <summary>
    /// LevelManager 自动加载工具
    /// 使用方法:Unity菜单栏 → Tools → Auto Load Level Data
    /// </summary>
    public class LevelManagerAutoLoader : EditorWindow
    {
        private LevelManager levelManager;
        private List<LevelData> foundLevelData = new List<LevelData>();
        private Vector2 scrollPosition;
        private string statusMessage = "";
        private bool isSuccess = false;

        [MenuItem("Tools/Auto Load Level Data")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelManagerAutoLoader>("关卡数据自动加载");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("关卡数据自动加载工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // LevelManager 对象字段
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("LevelManager:", GUILayout.Width(120));
            levelManager = (LevelManager)EditorGUILayout.ObjectField(levelManager, typeof(LevelManager), true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 说明文字
            EditorGUILayout.HelpBox(
                "此工具将自动从 Assets/Data/LevelData 目录中查找所有 LevelData_*.asset 文件,\n" +
                "并按数字顺序自动填充到 LevelManager 的 allLevelDatas 列表中。",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // 搜索按钮
            if (GUILayout.Button("搜索关卡数据", GUILayout.Height(30)))
            {
                SearchLevelData();
            }

            EditorGUILayout.Space(10);

            // 显示找到的关卡数据
            if (foundLevelData.Count > 0)
            {
                EditorGUILayout.LabelField($"找到 {foundLevelData.Count} 个关卡数据:", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));
                for (int i = 0; i < foundLevelData.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(40));
                    EditorGUILayout.ObjectField(foundLevelData[i], typeof(LevelData), false);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(10);

                // 应用按钮
                GUI.enabled = levelManager != null;
                if (GUILayout.Button("应用到 LevelManager", GUILayout.Height(40)))
                {
                    ApplyToLevelManager();
                }
                GUI.enabled = true;

                if (levelManager == null)
                {
                    EditorGUILayout.HelpBox("请先指定 LevelManager 对象!", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(10);

            // 状态消息
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, isSuccess ? MessageType.Info : MessageType.Warning);
            }
        }

        /// <summary>
        /// 搜索关卡数据文件
        /// </summary>
        private void SearchLevelData()
        {
            foundLevelData.Clear();
            statusMessage = "";

            // 查找所有 LevelData 资源
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Data/LevelData" });

            if (guids.Length == 0)
            {
                statusMessage = "未找到任何关卡数据文件!";
                isSuccess = false;
                return;
            }

            // 加载所有 LevelData 并提取文件名中的数字
            List<(int index, LevelData data)> levelDataWithIndex = new List<(int, LevelData)>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (data != null)
                {
                    // 从文件名提取数字 (例如: "LevelData_42.asset" -> 42)
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    Match match = Regex.Match(fileName, @"LevelData_(\d+)");

                    if (match.Success)
                    {
                        int index = int.Parse(match.Groups[1].Value);
                        levelDataWithIndex.Add((index, data));
                    }
                    else
                    {
                        Debug.LogWarning($"文件名格式不符: {fileName},已跳过");
                    }
                }
            }

            // 按数字排序
            levelDataWithIndex.Sort((a, b) => a.index.CompareTo(b.index));

            // 提取排序后的 LevelData
            foundLevelData = levelDataWithIndex.Select(x => x.data).ToList();

            statusMessage = $"成功找到 {foundLevelData.Count} 个关卡数据文件!";
            isSuccess = true;

            Debug.Log($"[LevelManagerAutoLoader] 找到 {foundLevelData.Count} 个关卡:");
            for (int i = 0; i < foundLevelData.Count; i++)
            {
                Debug.Log($"  [{i}] {foundLevelData[i].name}");
            }
        }

        /// <summary>
        /// 应用到 LevelManager
        /// </summary>
        private void ApplyToLevelManager()
        {
            if (levelManager == null)
            {
                statusMessage = "错误: LevelManager 未指定!";
                isSuccess = false;
                return;
            }

            if (foundLevelData.Count == 0)
            {
                statusMessage = "错误: 没有可应用的关卡数据!";
                isSuccess = false;
                return;
            }

            // 记录撤销操作
            Undo.RecordObject(levelManager, "Auto Load Level Data");

            // 应用关卡数据
            levelManager.allLevelDatas = new List<LevelData>(foundLevelData);

            // 标记为已修改
            EditorUtility.SetDirty(levelManager);

            statusMessage = $"成功应用 {foundLevelData.Count} 个关卡数据到 LevelManager!";
            isSuccess = true;

            Debug.Log($"[LevelManagerAutoLoader] 成功应用 {foundLevelData.Count} 个关卡数据到 {levelManager.name}");
        }

        /// <summary>
        /// 快捷方式:直接在场景中查找 LevelManager 并自动加载
        /// </summary>
        [MenuItem("Tools/Quick Auto Load Levels")]
        public static void QuickAutoLoad()
        {
            // 查找场景中的 LevelManager
            LevelManager manager = Object.FindObjectOfType<LevelManager>();

            if (manager == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中未找到 LevelManager!", "确定");
                return;
            }

            // 查找所有 LevelData
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/Data/LevelData" });

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "未找到任何关卡数据文件!", "确定");
                return;
            }

            // 加载并排序
            List<(int index, LevelData data)> levelDataWithIndex = new List<(int, LevelData)>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);

                if (data != null)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    Match match = Regex.Match(fileName, @"LevelData_(\d+)");

                    if (match.Success)
                    {
                        int index = int.Parse(match.Groups[1].Value);
                        levelDataWithIndex.Add((index, data));
                    }
                }
            }

            levelDataWithIndex.Sort((a, b) => a.index.CompareTo(b.index));
            List<LevelData> sortedData = levelDataWithIndex.Select(x => x.data).ToList();

            // 应用
            Undo.RecordObject(manager, "Quick Auto Load Level Data");
            manager.allLevelDatas = new List<LevelData>(sortedData);
            EditorUtility.SetDirty(manager);

            EditorUtility.DisplayDialog(
                "成功",
                $"已自动加载 {sortedData.Count} 个关卡数据到 {manager.name}!",
                "确定"
            );

            Debug.Log($"[Quick Auto Load] 成功加载 {sortedData.Count} 个关卡数据");
        }
    }
}
