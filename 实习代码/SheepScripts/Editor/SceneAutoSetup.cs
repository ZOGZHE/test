using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 命名空间：WoolyPath项目，避免类名冲突
namespace WoolyPath
{
    /// <summary>
    /// 场景自动配置工具 - 一键配置游戏场景所需的所有组件
    /// 继承EditorWindow：成为Unity编辑器窗口（非运行时脚本）
    /// </summary>
    public class SceneAutoSetup : EditorWindow
    {
        // ---------------------------------------------------------------------
        // 1. 编辑器菜单入口：让工具在Unity菜单中显示
        // ---------------------------------------------------------------------
        /// <summary>
        /// 给Unity编辑器添加菜单选项（路径：WoolyPath/场景自动配置）
        /// MenuItem特性：必须静态方法，用于注册编辑器菜单
        /// </summary>
        [MenuItem("WoolyPath/场景自动配置")]
        public static void ShowWindow()
        {
            // 打开当前工具窗口，标题为"场景自动配置"
            GetWindow<SceneAutoSetup>("场景自动配置");
        }

        // ---------------------------------------------------------------------
        // 2. 编辑器窗口GUI绘制：定义窗口内的按钮、文本等交互元素
        // ---------------------------------------------------------------------
        /// <summary>
        /// Unity编辑器窗口的GUI渲染方法（每帧调用，类似MonoBehaviour的OnGUI）
        /// </summary>
        private void OnGUI()
        {
            // 标题文本（加粗样式）
            GUILayout.Label("羊毛路径游戏场景配置", EditorStyles.boldLabel);
            GUILayout.Space(10); // 空出10像素间距

            // 【一键配置所有组件】按钮（高度40像素）
            if (GUILayout.Button("🚀 一键配置所有组件", GUILayout.Height(40)))
            {
                // 点击后执行完整配置流程
                SetupCompleteScene();
            }

            GUILayout.Space(10);
            GUILayout.Label("单项配置:"); // 分类标题

            // 单项配置按钮组：分别执行单个功能
            if (GUILayout.Button("添加 EventSystemSetup 组件"))
            {
                AddEventSystemSetup();
            }
            if (GUILayout.Button("创建 SheepSpawner"))
            {
                CreateSheepSpawner();
            }
            if (GUILayout.Button("创建 PathConfiguration"))
            {
                CreatePathConfiguration();
            }
            if (GUILayout.Button("创建 EffectsManager"))
            {
                CreateEffectsManager();
            }
            if (GUILayout.Button("创建 UI Canvas 结构"))
            {
                CreateUICanvasStructure();
            }

            GUILayout.Space(10);

            // 【自动设置所有引用】按钮：单独处理组件间的引用绑定
            if (GUILayout.Button("🔗 自动设置所有引用"))
            {
                SetupAllReferences();
            }

            GUILayout.Space(10);
            GUILayout.Label("问题修复:"); // 分类标题

            // 问题修复按钮组
            if (GUILayout.Button("🔧 修复预制体引用问题", GUILayout.Height(30)))
            {
                FixPrefabReferences();
            }
            if (GUILayout.Button("📋 显示当前配置状态"))
            {
                ShowCurrentConfiguration();
            }
            if (GUILayout.Button("🔍 快速预制体检查"))
            {
                QuickPrefabCheck();
            }
        }

        // ---------------------------------------------------------------------
        // 3. 核心功能：一键配置完整场景（主流程）
        // ---------------------------------------------------------------------
        /// <summary>
        /// 一键配置完整场景：按顺序执行所有子功能
        /// </summary>
        private void SetupCompleteScene()
        {
            Debug.Log("🚀 [SceneAutoSetup] 开始一键配置场景...");

            // 步骤1：添加EventSystemSetup组件到GameSceneSetup
            AddEventSystemSetup();

            // 步骤2：创建核心管理器（羊群生成、路径配置、特效管理）
            CreateSheepSpawner();
            CreatePathConfiguration();
            CreateEffectsManager();

            // 步骤3：创建UI基础结构（Canvas + EventSystem）
            CreateUICanvasStructure();

            // 步骤4：自动绑定所有组件的引用（预制体、其他管理器）
            SetupAllReferences();

            Debug.Log("✅ [SceneAutoSetup] 场景配置完成！");
            // 弹出对话框提示用户配置完成
            EditorUtility.DisplayDialog("配置完成", "场景自动配置已完成！\n请检查Console日志了解详细信息。", "确定");
        }

        // ---------------------------------------------------------------------
        // 4. 子功能1：添加EventSystemSetup组件
        // ---------------------------------------------------------------------
        /// <summary>
        /// 给场景中的"GameSceneSetup"对象添加EventSystemSetup组件（若不存在）
        /// </summary>
        private void AddEventSystemSetup()
        {
            // 1. 在场景中查找名为"GameSceneSetup"的对象
            GameObject gameSceneSetup = FindGameObjectInScene("GameSceneSetup");
            if (gameSceneSetup == null)
            {
                Debug.LogError("❌ 未找到GameSceneSetup对象！");
                return; // 找不到目标对象，直接返回
            }

            // 2. 检查该对象是否已有EventSystemSetup组件
            if (gameSceneSetup.GetComponent<EventSystemSetup>() == null)
            {
                // 没有则添加组件
                gameSceneSetup.AddComponent<EventSystemSetup>();
                Debug.Log("✅ 已添加EventSystemSetup组件到GameSceneSetup");
            }
            else
            {
                Debug.Log("ℹ️ EventSystemSetup组件已存在");
            }

            // 3. 标记对象为"脏"（告诉Unity：该对象已修改，需要保存）
            EditorUtility.SetDirty(gameSceneSetup);
        }

        // ---------------------------------------------------------------------
        // 5. 子功能2：创建SheepSpawner（羊群生成管理器）
        // ---------------------------------------------------------------------
        /// <summary>
        /// 创建SheepSpawner对象（若已存在则只配置引用）
        /// </summary>
        private void CreateSheepSpawner()
        {
            // 1. 先检查场景中是否已有SheepSpawner
            GameObject existingSheepSpawner = FindGameObjectInScene("SheepSpawner");
            SheepSpawner spawnerComponent; // 存储SheepSpawner组件引用

            if (existingSheepSpawner != null)
            {
                Debug.Log("ℹ️ SheepSpawner已存在，正在配置引用...");
                spawnerComponent = existingSheepSpawner.GetComponent<SheepSpawner>();
            }
            else
            {
                // 2. 查找"=== SYSTEMS ==="父节点（用于归类管理器对象）
                GameObject systemsParent = FindGameObjectInScene("=== SYSTEMS ===");
                if (systemsParent == null)
                {
                    Debug.LogWarning("⚠️ 未找到SYSTEMS父节点，将在根节点创建");
                }

                // 3. 创建新的SheepSpawner对象
                GameObject sheepSpawner = new GameObject("SheepSpawner");
                // 若有父节点，则设置父对象（保持场景层级整洁）
                if (systemsParent != null)
                {
                    sheepSpawner.transform.parent = systemsParent.transform;
                }

                // 4. 给新对象添加SheepSpawner组件
                spawnerComponent = sheepSpawner.AddComponent<SheepSpawner>();
                Debug.Log("✅ 已创建SheepSpawner");
                EditorUtility.SetDirty(sheepSpawner); // 标记为脏，等待保存
            }

            // 5. 配置SheepSpawner的所有引用（预制体、父节点等）
            ConfigureSheepSpawnerReferences(spawnerComponent);
        }

        /// <summary>
        /// 配置SheepSpawner组件的私有字段引用（羊群父节点、羊预制体等）
        /// </summary>
        /// <param name="spawnerComponent">要配置的SheepSpawner组件</param>
        private void ConfigureSheepSpawnerReferences(SheepSpawner spawnerComponent)
        {
            if (spawnerComponent == null) return; // 组件为空则返回

            // 1. 设置sheepParent（羊群生成后的父节点，用于归类羊群对象）
            GameObject sheepParent = FindGameObjectInScene("SheepSpawnParent");
            if (sheepParent != null)
            {
                // 用反射设置私有字段（因为sheepParent是private）
                SetPrivateField(spawnerComponent, "sheepParent", sheepParent.transform);
                Debug.Log("✅ 已设置SheepSpawner的sheepParent引用");
            }

            // 2. 设置sheepPrefab（生成羊群的预制体）
            GameObject sheepPrefab = LoadPrefabFromFolder("Sheep");
            if (sheepPrefab != null)
            {
                SetPrivateField(spawnerComponent, "sheepPrefab", sheepPrefab);
                Debug.Log("✅ 已设置SheepSpawner的sheepPrefab引用");
            }
            else
            {
                Debug.LogError("❌ 无法找到Sheep预制体！请检查Prefabs文件夹中是否有Sheep预制体");
            }

            // 3. 设置网格起始位置（基于SheepGrid对象的位置计算）
            GameObject sheepGrid = FindGameObjectInScene("SheepGrid");
            if (sheepGrid != null)
            {
                Vector3 gridPos = sheepGrid.transform.position;
                // 偏移3单位，避免羊群生成在网格原点
                SetPrivateField(spawnerComponent, "gridStartPosition", gridPos + new Vector3(-3f, 0f, -3f));
                Debug.Log($"✅ 已设置SheepSpawner的gridStartPosition: {gridPos}");
            }

            EditorUtility.SetDirty(spawnerComponent.gameObject);
        }

        // ---------------------------------------------------------------------
        // 6. 子功能3：创建PathConfiguration（路径配置管理器）
        // ---------------------------------------------------------------------
        /// <summary>
        /// 创建PathConfiguration对象（管理游戏中的路径数据，如传送带入口）
        /// </summary>
        private void CreatePathConfiguration()
        {
            // 1. 检查是否已存在
            GameObject existingPathConfig = FindGameObjectInScene("PathConfiguration");
            if (existingPathConfig != null)
            {
                Debug.Log("ℹ️ PathConfiguration已存在");
                return;
            }

            // 2. 查找SYSTEMS父节点（归类管理器）
            GameObject systemsParent = FindGameObjectInScene("=== SYSTEMS ===");
            if (systemsParent == null)
            {
                Debug.LogWarning("⚠️ 未找到SYSTEMS父节点，将在根节点创建");
            }

            // 3. 创建PathConfiguration对象并设置父节点
            GameObject pathConfig = new GameObject("PathConfiguration");
            if (systemsParent != null)
            {
                pathConfig.transform.parent = systemsParent.transform;
            }

            // 4. 添加PathConfiguration组件
            PathConfiguration pathComponent = pathConfig.AddComponent<PathConfiguration>();

            // 5. 自动绑定传送带入口引用
            GameObject conveyorBelt = FindGameObjectInScene("ConveyorBelt");
            if (conveyorBelt != null)
            {
                SetPrivateField(pathComponent, "conveyorBeltEntry", conveyorBelt.transform);
            }

            Debug.Log("✅ 已创建PathConfiguration");
            EditorUtility.SetDirty(pathConfig);
        }

        // ---------------------------------------------------------------------
        // 7. 子功能4：创建EffectsManager（特效管理器）
        // ---------------------------------------------------------------------
        /// <summary>
        /// 创建EffectsManager对象（管理游戏中的所有特效，如点击特效、羊毛发射特效）
        /// </summary>
        private void CreateEffectsManager()
        {
            // 1. 检查是否已存在
            GameObject existingEffectsManager = FindGameObjectInScene("EffectsManager");
            if (existingEffectsManager != null)
            {
                Debug.Log("ℹ️ EffectsManager已存在");
                return;
            }

            // 2. 查找SYSTEMS父节点
            GameObject systemsParent = FindGameObjectInScene("=== SYSTEMS ===");
            if (systemsParent == null)
            {
                Debug.LogWarning("⚠️ 未找到SYSTEMS父节点，将在根节点创建");
            }

            // 3. 创建对象并设置父节点
            GameObject effectsManager = new GameObject("EffectsManager");
            if (systemsParent != null)
            {
                effectsManager.transform.parent = systemsParent.transform;
            }

            // 4. 添加EffectsManager组件
            EffectsManager effectsComponent = effectsManager.AddComponent<EffectsManager>();

            Debug.Log("✅ 已创建EffectsManager");
            EditorUtility.SetDirty(effectsManager);
        }

        // ---------------------------------------------------------------------
        // 8. 子功能5：创建UI Canvas结构
        // ---------------------------------------------------------------------
        /// <summary>
        /// 创建UI基础结构：EventSystem（输入响应） + Canvas（UI渲染容器）
        /// </summary>
        private void CreateUICanvasStructure()
        {
            // 1. 检查并创建EventSystem（UI交互必须，处理点击、触摸等输入）
            EventSystem existingEventSystem = FindObjectOfType<EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();
                // 添加StandaloneInputModule：处理PC端输入（如鼠标）
                eventSystemGO.AddComponent<StandaloneInputModule>();
                Debug.Log("✅ 已创建EventSystem");
                EditorUtility.SetDirty(eventSystemGO);
            }

            // 2. 检查并创建Canvas（UI元素的父容器）
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas == null)
            {
                GameObject canvasGO = new GameObject("UI Canvas");
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                // 设置渲染模式：屏幕空间覆盖（UI在最上层，不与3D物体穿插）
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10; // 渲染层级（值越大越靠上）

                // 添加CanvasScaler：处理不同屏幕分辨率的UI适配
                CanvasScaler canvasScaler = canvasGO.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 按屏幕尺寸缩放
                canvasScaler.referenceResolution = new Vector2(1920, 1080); // 参考分辨率（1080P）
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 宽高适配
                canvasScaler.matchWidthOrHeight = 0.5f; // 宽高各适配50%

                // 添加GraphicRaycaster：让UI能接收射线检测（如点击）
                canvasGO.AddComponent<GraphicRaycaster>();

                Debug.Log("✅ 已创建UI Canvas");
                EditorUtility.SetDirty(canvasGO);
            }
            else
            {
                Debug.Log("ℹ️ UI Canvas已存在");
            }
        }

        // ---------------------------------------------------------------------
        // 9. 子功能6：自动设置所有组件引用
        // ---------------------------------------------------------------------
        /// <summary>
        /// 批量绑定所有管理器的引用（跨组件协作的关键）
        /// </summary>
        private void SetupAllReferences()
        {
            Debug.Log("🔗 [SceneAutoSetup] 开始设置组件引用...");

            // 1. 获取所有需要配置的管理器组件
            GameSceneSetup gameSceneSetup = FindObjectOfType<GameSceneSetup>();
            LevelManager levelManager = FindObjectOfType<LevelManager>();
            SheepSpawner sheepSpawner = FindObjectOfType<SheepSpawner>();
            PathConfiguration pathConfiguration = FindObjectOfType<PathConfiguration>();
            EffectsManager effectsManager = FindObjectOfType<EffectsManager>();

            // 2. 配置GameSceneSetup的预制体引用
            if (gameSceneSetup != null)
            {
                SetupGameSceneSetupPrefabReferences(gameSceneSetup);
                EditorUtility.SetDirty(gameSceneSetup.gameObject);
            }

            // 3. 配置LevelManager的SheepSpawner引用
            if (levelManager != null && sheepSpawner != null)
            {
                SetPrivateField(levelManager, "sheepSpawner", sheepSpawner);
                Debug.Log("✅ 已设置LevelManager的SheepSpawner引用");
                EditorUtility.SetDirty(levelManager.gameObject);
            }

            // 4. 配置SheepSpawner的羊预制体数组（支持多类型羊）
            if (sheepSpawner != null)
            {
                SetupSheepSpawnerPrefabReferences(sheepSpawner);
                EditorUtility.SetDirty(sheepSpawner.gameObject);
            }

            Debug.Log("✅ [SceneAutoSetup] 组件引用设置完成");
        }

        /// <summary>
        /// 给SheepSpawner配置多只羊的预制体数组（支持生成不同类型的羊）
        /// </summary>
        private void SetupSheepSpawnerPrefabReferences(SheepSpawner sheepSpawner)
        {
            // 加载Sheep文件夹下的所有预制体
            GameObject[] sheepPrefabs = LoadAllPrefabsFromFolder("Sheep");

            if (sheepPrefabs != null && sheepPrefabs.Length > 0)
            {
                SetPrivateField(sheepSpawner, "sheepPrefabs", sheepPrefabs);
                Debug.Log($"✅ 已设置SheepSpawner的羊预制体数组: 找到 {sheepPrefabs.Length} 个预制体");

                // 打印所有找到的预制体名称（调试用）
                foreach (var prefab in sheepPrefabs)
                {
                    Debug.Log($"   - {prefab.name}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ 未在Sheep文件夹中找到任何羊预制体");
            }
        }

        /// <summary>
        /// 给GameSceneSetup配置核心预制体引用（羊、收集器、羊毛）
        /// </summary>
        private void SetupGameSceneSetupPrefabReferences(GameSceneSetup gameSceneSetup)
        {
            // 加载各预制体（从Prefabs文件夹搜索）
            GameObject sheepPrefab = LoadPrefabFromFolder("Sheep");
            GameObject collectorPrefab = LoadPrefabFromFolder("Collect");  // 收集器预制体（需项目中存在）
            GameObject woolPrefab = LoadPrefabFromFolder("Wool");

            // 配置羊预制体
            if (sheepPrefab != null)
            {
                SetPrivateField(gameSceneSetup, "sheepPrefab", sheepPrefab);
                Debug.Log($"✅ 已设置羊预制体引用: {sheepPrefab.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到Sheep预制体，请检查Prefabs文件夹中是否有名为'Sheep'的预制体");
            }

            // 配置收集器预制体
            if (collectorPrefab != null)
            {
                SetPrivateField(gameSceneSetup, "collectorPrefab", collectorPrefab);
                Debug.Log($"✅ 已设置收集器预制体引用: {collectorPrefab.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到Collect预制体，请检查Prefabs文件夹中是否有名为'Collect'的预制体");
            }

            // 配置羊毛预制体
            if (woolPrefab != null)
            {
                SetPrivateField(gameSceneSetup, "woolPrefab", woolPrefab);
                Debug.Log($"✅ 已设置羊毛预制体引用: {woolPrefab.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到Wool预制体，请检查Prefabs文件夹中是否有名为'Wool'的预制体");
            }
        }

        // ---------------------------------------------------------------------
        // 10. 工具方法1：智能加载预制体（支持文件夹搜索+组件匹配）
        // ---------------------------------------------------------------------
        /// <summary>
        /// 从指定文件夹加载预制体（智能搜索：先按文件夹，再按组件匹配）
        /// </summary>
        /// <param name="folderName">目标文件夹名（如Sheep、Wool）</param>
        /// <returns>找到的最佳匹配预制体</returns>
        private GameObject LoadPrefabFromFolder(string folderName)
        {
            Debug.Log($"🔍 在文件夹 '{folderName}' 中搜索预制体...");

            // 步骤1：搜索所有预制体的GUID（GUID是Unity中资源的唯一标识）
            string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            System.Collections.Generic.List<GameObject> candidatePrefabs = new System.Collections.Generic.List<GameObject>();

            // 遍历所有预制体，筛选出目标文件夹下的预制体
            foreach (string guid in allPrefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); // GUID转资源路径

                // 兼容不同系统的路径分隔符（/ 和 \）
                if (path.ToLower().Contains($"/{folderName.ToLower()}/") ||
                    path.ToLower().Contains($"/{folderName.ToLower()}\\") ||
                    path.ToLower().Contains($"\\{folderName.ToLower()}\\") ||
                    path.ToLower().Contains($"\\{folderName.ToLower()}/"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        candidatePrefabs.Add(prefab);
                        Debug.Log($"🔍 在 {folderName} 文件夹中找到预制体: {prefab.name}");
                    }
                }
            }

            // 步骤2：从候选预制体中选择最佳匹配
            if (candidatePrefabs.Count > 0)
            {
                GameObject bestMatch = SelectBestPrefab(candidatePrefabs, folderName);
                if (bestMatch != null)
                {
                    Debug.Log($"✅ 选择最佳匹配预制体: {bestMatch.name} (来自 {folderName} 文件夹)");
                    return bestMatch;
                }

                // 若无最佳匹配，返回第一个
                Debug.Log($"✅ 使用第一个找到的预制体: {candidatePrefabs[0].name} (来自 {folderName} 文件夹)");
                return candidatePrefabs[0];
            }

            // 步骤3：文件夹搜索失败，尝试全局按组件搜索（容错机制）
            Debug.Log($"🔍 文件夹搜索失败，尝试全局组件搜索...");

            foreach (string guid in allPrefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // 按组件判断是否为目标预制体（如Sheep预制体需有SheepController）
                if (prefab != null && HasRequiredComponent(prefab, folderName))
                {
                    Debug.Log($"✅ 根据组件找到预制体: {prefab.name} at {path}");
                    return prefab;
                }
            }

            // 搜索失败
            Debug.LogError($"❌ 未在 '{folderName}' 文件夹中找到任何预制体");
            Debug.LogError($"请确保Prefabs/{folderName}文件夹中有相应的预制体");
            return null;
        }

        /// <summary>
        /// 从候选预制体中选择最佳匹配（优先选带目标组件的预制体）
        /// </summary>
        private GameObject SelectBestPrefab(System.Collections.Generic.List<GameObject> candidates, string folderName)
        {
            // 优先选择包含目标组件的预制体（如Sheep预制体必须有SheepController）
            foreach (GameObject candidate in candidates)
            {
                if (HasRequiredComponent(candidate, folderName))
                {
                    return candidate;
                }
            }

            // 若无带组件的预制体，选择名称最短的（默认是基础版本）
            GameObject simplest = null;
            int shortestNameLength = int.MaxValue;

            foreach (GameObject candidate in candidates)
            {
                if (candidate.name.Length < shortestNameLength)
                {
                    shortestNameLength = candidate.name.Length;
                    simplest = candidate;
                }
            }

            return simplest;
        }

        /// <summary>
        /// 检查预制体是否包含目标组件（按文件夹名判断所需组件）
        /// </summary>
        private bool HasRequiredComponent(GameObject prefab, string folderName)
        {
            switch (folderName.ToLower())
            {
                case "sheep": // Sheep文件夹的预制体需有SheepController
                    return prefab.GetComponent<SheepController>() != null;
                case "collect": // Collect文件夹需有CollectorPlate
                case "collector":
                    return prefab.GetComponent<CollectorPlate>() != null;
                case "wool": // Wool文件夹需有WoolObject
                    return prefab.GetComponent<WoolObject>() != null;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 从指定文件夹加载所有预制体（用于批量配置，如多类型羊）
        /// </summary>
        private GameObject[] LoadAllPrefabsFromFolder(string folderName)
        {
            Debug.Log($"🔍 在文件夹 '{folderName}' 中搜索所有预制体...");

            var allPrefabs = new System.Collections.Generic.List<GameObject>();

            // 步骤1：先搜索指定文件夹（Assets/Prefabs/[folderName]）
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { $"Assets/Prefabs/{folderName}" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    Debug.Log($"   找到预制体: {prefab.name} (Path: {path})");
                    allPrefabs.Add(prefab);
                }
            }

            // 步骤2：若未找到，扩大搜索范围到Assets/Prefabs
            if (allPrefabs.Count == 0)
            {
                Debug.Log($"   在指定文件夹中未找到，在Assets/Prefabs中搜索...");
                guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    // 按组件筛选
                    if (prefab != null && HasRequiredComponent(prefab, folderName))
                    {
                        Debug.Log($"   找到适合的预制体: {prefab.name} (Path: {path})");
                        allPrefabs.Add(prefab);
                    }
                }
            }

            // 返回结果
            if (allPrefabs.Count > 0)
            {
                Debug.Log($"✅ 在 {folderName} 文件夹中找到 {allPrefabs.Count} 个预制体");
                return allPrefabs.ToArray();
            }
            else
            {
                Debug.LogWarning($"⚠️ 在 {folderName} 文件夹中未找到任何适合的预制体");
                return null;
            }
        }

        // ---------------------------------------------------------------------
        // 11. 工具方法2：场景对象查找
        // ---------------------------------------------------------------------
        /// <summary>
        /// 在场景中查找指定名称的GameObject（遍历所有对象，精确匹配名称）
        /// </summary>
        /// <param name="name">要查找的对象名称</param>
        /// <returns>找到的对象（null表示未找到）</returns>
        private GameObject FindGameObjectInScene(string name)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>(); // 获取场景中所有激活的GameObject
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == name) // 精确匹配名称
                {
                    return obj;
                }
            }
            return null;
        }

        // ---------------------------------------------------------------------
        // 12. 工具方法3：反射设置私有字段
        // ---------------------------------------------------------------------
        /// <summary>
        /// 用反射设置对象的私有字段（因私有字段无法直接访问，需反射突破访问限制）
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="fieldName">私有字段名</param>
        /// <param name="value">要设置的值</param>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            // 获取字段：NonPublic（私有） + Instance（实例字段，非静态）
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value); // 设置字段值
            }
            else
            {
                // 字段不存在时提示（调试用）
                Debug.LogWarning($"⚠️ 未找到字段: {fieldName} in {obj.GetType().Name}");
            }
        }

        // ---------------------------------------------------------------------
        // 13. 问题修复：预制体引用修复
        // ---------------------------------------------------------------------
        /// <summary>
        /// 修复预制体引用问题（如预制体丢失、路径错误导致的引用失效）
        /// </summary>
        private void FixPrefabReferences()
        {
            Debug.Log("🔧 [SceneAutoSetup] 开始修复预制体引用问题...");

            // 修复SheepSpawner的引用
            SheepSpawner sheepSpawner = FindObjectOfType<SheepSpawner>();
            if (sheepSpawner != null)
            {
                ConfigureSheepSpawnerReferences(sheepSpawner);
            }

            // 修复GameSceneSetup的引用
            GameSceneSetup gameSceneSetup = FindObjectOfType<GameSceneSetup>();
            if (gameSceneSetup != null)
            {
                SetupGameSceneSetupPrefabReferences(gameSceneSetup);
            }

            // 修复EffectsManager的特效预制体引用
            EffectsManager effectsManager = FindObjectOfType<EffectsManager>();
            if (effectsManager != null)
            {
                ConfigureEffectsManagerReferences(effectsManager);
            }

            Debug.Log("✅ [SceneAutoSetup] 预制体引用修复完成");
            EditorUtility.DisplayDialog("修复完成", "预制体引用问题已修复！\n请查看Console了解详细信息。", "确定");
        }

        /// <summary>
        /// 配置EffectsManager的特效预制体引用（默认用第一个找到的粒子预制体）
        /// </summary>
        private void ConfigureEffectsManagerReferences(EffectsManager effectsManager)
        {
            if (effectsManager == null) return;

            Debug.Log("🔧 配置EffectsManager特效预制体引用...");

            // 找到所有带ParticleSystem的预制体（特效通常是粒子系统）
            GameObject[] effectPrefabs = FindEffectPrefabs();

            if (effectPrefabs.Length > 0)
            {
                // 用第一个特效预制体作为默认值（可根据项目需求扩展）
                GameObject defaultEffect = effectPrefabs[0];

                // 设置所有特效字段的引用
                SetPrivateField(effectsManager, "clickEffectPrefab", defaultEffect);
                SetPrivateField(effectsManager, "woolLaunchEffectPrefab", defaultEffect);
                SetPrivateField(effectsManager, "shearingEffectPrefab", defaultEffect);
                SetPrivateField(effectsManager, "collectionEffectPrefab", defaultEffect);

                Debug.Log($"✅ 已设置EffectsManager特效预制体: {defaultEffect.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ 未找到特效预制体，EffectsManager将使用代码创建默认特效");
            }

            EditorUtility.SetDirty(effectsManager.gameObject);
        }

        /// <summary>
        /// 查找所有带ParticleSystem的预制体（特效预制体）
        /// </summary>
        private GameObject[] FindEffectPrefabs()
        {
            System.Collections.Generic.List<GameObject> effects = new System.Collections.Generic.List<GameObject>();

            // 遍历所有预制体，筛选带ParticleSystem的
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null && prefab.GetComponent<ParticleSystem>() != null)
                {
                    effects.Add(prefab);
                }
            }

            return effects.ToArray();
        }

        // ---------------------------------------------------------------------
        // 14. 状态检测：显示当前配置状态
        // ---------------------------------------------------------------------
        /// <summary>
        /// 检测并显示当前场景的配置状态（管理器是否存在、引用是否有效）
        /// </summary>
        private void ShowCurrentConfiguration()
        {
            // 用StringBuilder拼接状态信息（比字符串拼接高效）
            System.Text.StringBuilder status = new System.Text.StringBuilder();
            status.AppendLine("📋 当前场景配置状态:\n");

            // 1. 检查核心管理器是否存在
            status.AppendLine("=== 管理器组件 ===");
            status.AppendLine($"GameSceneSetup: {(FindObjectOfType<GameSceneSetup>() != null ? "✅" : "❌")}");
            status.AppendLine($"SheepSpawner: {(FindObjectOfType<SheepSpawner>() != null ? "✅" : "❌")}");
            status.AppendLine($"PathConfiguration: {(FindObjectOfType<PathConfiguration>() != null ? "✅" : "❌")}");
            status.AppendLine($"EffectsManager: {(FindObjectOfType<EffectsManager>() != null ? "✅" : "❌")}");
            status.AppendLine($"EventSystem: {(FindObjectOfType<EventSystem>() != null ? "✅" : "❌")}");

            // 2. 检查SheepSpawner的关键引用
            SheepSpawner sheepSpawner = FindObjectOfType<SheepSpawner>();
            if (sheepSpawner != null)
            {
                status.AppendLine("\n=== SheepSpawner 引用 ===");

                // 反射获取私有字段sheepPrefab
                var sheepPrefabField = sheepSpawner.GetType().GetField("sheepPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                GameObject sheepPrefab = sheepPrefabField?.GetValue(sheepSpawner) as GameObject;
                status.AppendLine($"sheepPrefab: {(sheepPrefab != null ? $"✅ ({sheepPrefab.name})" : "❌")}");

                // 反射获取私有字段sheepParent
                var sheepParentField = sheepSpawner.GetType().GetField("sheepParent",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Transform sheepParent = sheepParentField?.GetValue(sheepSpawner) as Transform;
                status.AppendLine($"sheepParent: {(sheepParent != null ? $"✅ ({sheepParent.name})" : "❌")}");
            }

            // 3. 检查核心预制体是否存在
            status.AppendLine("\n=== 预制体检查 ===");
            GameObject sheepPrefabCheck = LoadPrefabFromFolder("Sheep");
            GameObject collectorPrefabCheck = LoadPrefabFromFolder("Collect");  // 收集器预制体
            GameObject woolPrefabCheck = LoadPrefabFromFolder("Wool");

            status.AppendLine($"Sheep预制体: {(sheepPrefabCheck != null ? $"✅ ({sheepPrefabCheck.name})" : "❌ 未找到")}");
            status.AppendLine($"Collect预制体: {(collectorPrefabCheck != null ? $"✅ ({collectorPrefabCheck.name})" : "❌ 未找到")}");
            status.AppendLine($"Wool预制体: {(woolPrefabCheck != null ? $"✅ ({woolPrefabCheck.name})" : "❌ 未找到")}");

            // 打印日志并弹出对话框
            Debug.Log(status.ToString());
            EditorUtility.DisplayDialog("配置状态", status.ToString(), "确定");
        }

        // ---------------------------------------------------------------------
        // 15. 状态检测：快速预制体检查
        // ---------------------------------------------------------------------
        /// <summary>
        /// 快速检查预制体文件夹状态（数量、组件完整性、推荐使用的预制体）
        /// </summary>
        private void QuickPrefabCheck()
        {
            Debug.Log("🔍 [快速预制体检查] 开始检查您的预制体文件夹...");

            System.Text.StringBuilder result = new System.Text.StringBuilder();
            result.AppendLine("🔍 快速预制体检查结果:\n");

            // 要检查的预制体文件夹列表
            string[] folderNames = { "Sheep", "Collect", "Wool" };
            string[] descriptions = { "羊预制体文件夹", "收集器预制体文件夹", "羊毛预制体文件夹" };

            // 遍历每个文件夹检查
            for (int i = 0; i < folderNames.Length; i++)
            {
                result.AppendLine($"📁 {descriptions[i]} ({folderNames[i]}):");

                // 获取文件夹中的所有预制体
                System.Collections.Generic.List<GameObject> folderPrefabs = GetPrefabsInFolder(folderNames[i]);

                if (folderPrefabs.Count > 0)
                {
                    result.AppendLine($"   找到 {folderPrefabs.Count} 个预制体:");

                    // 检查每个预制体的组件完整性
                    foreach (GameObject prefab in folderPrefabs)
                    {
                        result.Append($"   - {prefab.name}");
                        bool hasComponent = HasRequiredComponent(prefab, folderNames[i]);
                        result.AppendLine($" {(hasComponent ? "✅" : "⚠️")}");

                        // 提示缺少的组件
                        if (!hasComponent)
                        {
                            string requiredComponent = GetRequiredComponentName(folderNames[i]);
                            result.AppendLine($"     (缺少 {requiredComponent} 组件)");
                        }
                    }

                    // 推荐最佳预制体
                    GameObject recommended = SelectBestPrefab(folderPrefabs, folderNames[i]);
                    if (recommended != null)
                    {
                        result.AppendLine($"   🎯 推荐使用: {recommended.name}");
                    }
                }
                else
                {
                    result.AppendLine($"   ❌ 文件夹为空或不存在");
                    result.AppendLine($"   请检查Prefabs/{folderNames[i]}文件夹");
                }

                result.AppendLine();
            }

            // 检查特效预制体
            GameObject[] effectPrefabs = FindEffectPrefabs();
            result.AppendLine($"✨ 特效预制体: 找到 {effectPrefabs.Length} 个");
            if (effectPrefabs.Length > 0)
            {
                result.AppendLine($"   建议使用: {effectPrefabs[0].name}");
            }

            // 输出结果
            Debug.Log(result.ToString());
            EditorUtility.DisplayDialog("预制体检查", result.ToString(), "确定");
        }

        /// <summary>
        /// 获取指定文件夹中的所有预制体（仅按路径筛选，不按组件）
        /// </summary>
        private System.Collections.Generic.List<GameObject> GetPrefabsInFolder(string folderName)
        {
            System.Collections.Generic.List<GameObject> prefabs = new System.Collections.Generic.List<GameObject>();

            // 遍历所有预制体，按路径筛选
            string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

            foreach (string guid in allPrefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // 兼容不同系统路径分隔符
                if (path.ToLower().Contains($"/{folderName.ToLower()}/") ||
                    path.ToLower().Contains($"/{folderName.ToLower()}\\") ||
                    path.ToLower().Contains($"\\{folderName.ToLower()}\\") ||
                    path.ToLower().Contains($"\\{folderName.ToLower()}/"))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        prefabs.Add(prefab);
                    }
                }
            }

            return prefabs;
        }

        /// <summary>
        /// 根据文件夹名获取所需组件的名称（用于提示用户）
        /// </summary>
        private string GetRequiredComponentName(string folderName)
        {
            switch (folderName.ToLower())
            {
                case "sheep":
                    return "SheepController";
                case "collect":
                case "collector":
                    return "CollectorPlate";
                case "wool":
                    return "WoolObject";
                default:
                    return "未知组件";
            }
        }
    }
}