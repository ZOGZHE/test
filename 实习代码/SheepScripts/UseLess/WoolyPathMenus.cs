//using System.IO;
//using UnityEditor;
//using UnityEditor.SceneManagement;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//// 注：以下引用未在代码中使用，属于潜在冗余引用，可考虑删除
//using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

//namespace WoolyPath.Editor
//{
//    /// <summary>
//    /// WoolyPath编辑器工具主类：提供场景设置、目录创建、管理器添加等功能
//    /// 所有方法通过MenuItem标签添加到Unity菜单栏「WoolyPath」下
//    /// </summary>
//    public static class WoolyPathMenus
//    {
//        /// <summary>
//        /// 菜单栏选项：WoolyPath/设置主场景（优先级1，用于排序）
//        /// 功能：一键搭建GamePlay场景的基础结构（管理器、相机、光照、场景组织）
//        /// </summary>
//        [MenuItem("WoolyPath/设置主场景", false, 1)]
//        public static void SetupMainScene()
//        {
//            Debug.Log("[WoolyPath] 开始设置主场景...");

//            try
//            {
//                // 1. 定义主场景路径（固定为Assets/Scenes/GamePlay.unity）
//                string scenePath = "Assets/Scenes/GamePlay.unity";
//                // 检查场景文件是否存在，不存在则提示错误
//                if (!File.Exists(scenePath))
//                {
//                    Debug.LogError($"[WoolyPath] 场景文件不存在: {scenePath}");
//                    EditorUtility.DisplayDialog("错误", "GamePlay.unity 场景文件不存在！\n请先创建场景文件。", "确定");
//                    return;
//                }

//                // 2. 打开场景（编辑器模式下必须使用EditorSceneManager，而非SceneManager）
//                Scene scene = EditorSceneManager.OpenScene(scenePath);
//                // 检查场景是否成功加载
//                if (!scene.isLoaded)
//                {
//                    Debug.LogError("[WoolyPath] 无法加载场景");
//                    return;
//                }

//                // 3. 核心步骤：创建管理器系统（GameManager、LevelManager等）
//                CreateManagerSystem();
//                // 4. 创建场景组织结构（SYSTEMS、GAMEPLAY、ENVIRONMENT容器）
//                CreateSceneStructure();
//                // 5. 配置主相机（不存在则创建，已存在则跳过）
//                SetupCamera();
//                // 6. 配置主光源（不存在则创建，已存在则调整属性）
//                SetupLighting();

//                // 7. 保存场景修改（MarkSceneDirty标记场景为"已修改"，否则保存时会提示）
//                EditorSceneManager.MarkSceneDirty(scene);
//                EditorSceneManager.SaveScene(scene);

//                // 8. 操作完成：日志提示+弹窗告知
//                Debug.Log("[WoolyPath] 主场景设置完成！");
//                EditorUtility.DisplayDialog(
//                    "完成",
//                    "GamePlay主场景设置完成！\n\n已创建:\n- 管理器系统\n- 场景结构\n- 基础相机和光照",
//                    "确定"
//                );
//            }
//            catch (System.Exception e)
//            {
//                // 捕获异常：打印错误日志+弹窗提示具体错误信息
//                Debug.LogError($"[WoolyPath] 设置主场景时出错: {e.Message}");
//                EditorUtility.DisplayDialog("错误", $"设置主场景时出错:\n{e.Message}", "确定");
//            }
//        }

//        /// <summary>
//        /// 菜单栏选项：WoolyPath/整理场景（优先级2）
//        /// 功能：将场景中现有对象按类型归类到对应容器（SYSTEMS/GAMEPLAY/ENVIRONMENT）
//        /// </summary>
//        [MenuItem("WoolyPath/整理场景", false, 2)]
//        public static void OrganizeScene()
//        {
//            Debug.Log("[WoolyPath] 开始整理场景...");

//            try
//            {
//                // 1. 确保场景组织结构存在（不存在则创建）
//                CreateSceneStructure();
//                // 2. 按规则整理现有对象到对应容器
//                OrganizeExistingObjects();

//                // 3. 标记场景为已修改（避免用户忘记保存）
//                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

//                // 4. 操作完成提示
//                Debug.Log("[WoolyPath] 场景整理完成！");
//                EditorUtility.DisplayDialog("完成", "场景整理完成！\n所有对象已按类型归类。", "确定");
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"[WoolyPath] 整理场景时出错: {e.Message}");
//                EditorUtility.DisplayDialog("错误", $"整理场景时出错:\n{e.Message}", "确定");
//            }
//        }

//        /// <summary>
//        /// 菜单栏选项：WoolyPath/创建目录（优先级3）
//        /// 功能：创建项目标准目录结构（脚本、场景、预制体、音频等文件夹）
//        /// 并在空目录中创建.gitkeep（确保空目录能被Git追踪）
//        /// </summary>
//        [MenuItem("WoolyPath/创建目录", false, 3)]
//        public static void CreateDirectories()
//        {
//            Debug.Log("[WoolyPath] 开始创建目录结构...");

//            try
//            {
//                // 定义需要创建的所有目录路径（按功能分类）
//                string[] directories = {
//                    "Assets/Scripts/Managers",    // 管理器脚本目录
//                    "Assets/Scripts/Utils",       // 工具类脚本目录
//                    "Assets/Scripts/Gameplay",    // 游戏逻辑脚本目录
//                    "Assets/Scripts/Data",        // 数据处理脚本目录
//                    "Assets/Scripts/Editor",      // 编辑器脚本目录
//                    "Assets/Scenes",              // 场景文件目录
//                    "Assets/Prefabs/UI",          // UI预制体目录
//                    "Assets/Prefabs/Gameplay",    // 游戏对象预制体目录
//                    "Assets/Prefabs/Effects",     // 特效预制体目录
//                    "Assets/Data/Config",         // 配置文件目录
//                    "Assets/Audio/Music",         // 背景音乐目录
//                    "Assets/Audio/SFX",           // 音效目录
//                    "Assets/Materials",           // 材质目录
//                    "Assets/Textures",            // 纹理目录
//                    "Assets/Animation"            // 动画目录
//                };

//                int createdCount = 0; // 统计新创建的目录数量
//                foreach (string dir in directories)
//                {
//                    // 目录不存在则创建
//                    if (!Directory.Exists(dir))
//                    {
//                        Directory.CreateDirectory(dir);
//                        // 在空目录中创建.gitkeep文件（Git默认忽略空目录，此文件用于占位）
//                        File.WriteAllText(Path.Combine(dir, ".gitkeep"), "");
//                        createdCount++;
//                        Debug.Log($"[WoolyPath] 创建目录: {dir}");
//                    }
//                }

//                // 刷新Unity资源库（确保新创建的目录能在Project窗口中显示）
//                AssetDatabase.Refresh();

//                // 拼接提示信息（根据是否创建新目录区分）
//                string message = createdCount > 0
//                    ? $"目录创建完成！\n新创建了 {createdCount} 个目录。"
//                    : "所有必需的目录都已存在。";

//                Debug.Log($"[WoolyPath] {message}");
//                EditorUtility.DisplayDialog("完成", message, "确定");
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"[WoolyPath] 创建目录时出错: {e.Message}");
//                EditorUtility.DisplayDialog("错误", $"创建目录时出错:\n{e.Message}", "确定");
//            }
//        }

//        /// <summary>
//        /// 菜单栏选项：WoolyPath/项目状态（优先级11）
//        /// 功能：检测并展示项目关键状态（当前场景、管理器存在性、场景结构完整性等）
//        /// </summary>
//        [MenuItem("WoolyPath/项目状态", false, 11)]
//        public static void ShowProjectStatus()
//        {
//            // 获取项目状态字符串
//            var status = GetProjectStatus();
//            // 打印日志+弹窗展示
//            Debug.Log($"[WoolyPath] 项目状态:\n{status}");
//            EditorUtility.DisplayDialog("项目状态", status, "确定");
//        }

//        /// <summary>
//        /// 菜单栏选项：WoolyPath/创建GameSettings配置（优先级12）
//        /// 功能：创建ScriptableObject类型的GameSettings配置文件（存储游戏全局配置）
//        /// </summary>
//        [MenuItem("WoolyPath/创建GameSettings配置", false, 12)]
//        public static void CreateGameSettings()
//        {
//            try
//            {
//                // 配置文件的存储路径
//                string assetPath = "Assets/Data/Config/GameSettings.asset";

//                // 1. 检查配置文件是否已存在
//                if (File.Exists(assetPath))
//                {
//                    EditorUtility.DisplayDialog("提示", "GameSettings配置已存在！", "确定");
//                    // 高亮显示已存在的配置文件（方便用户定位）
//                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
//                    return;
//                }

//                // 2. 确保配置文件所在目录存在（不存在则创建）
//                string directory = Path.GetDirectoryName(assetPath);
//                if (!Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }

//                // 3. 创建ScriptableObject实例（GameSettings需继承自ScriptableObject）
//                var gameSettings = ScriptableObject.CreateInstance<Data.GameSettings>();
//                // 将实例保存为Unity资源文件
//                AssetDatabase.CreateAsset(gameSettings, assetPath);
//                AssetDatabase.SaveAssets(); // 保存资源库修改
//                AssetDatabase.Refresh();    // 刷新资源库

//                // 4. 高亮并选中新创建的配置文件（提升用户体验）
//                EditorGUIUtility.PingObject(gameSettings);
//                Selection.activeObject = gameSettings;

//                Debug.Log($"[WoolyPath] GameSettings配置创建完成: {assetPath}");
//                EditorUtility.DisplayDialog(
//                    "完成",
//                    "GameSettings配置创建成功！\n已在Project窗口中选中。",
//                    "确定"
//                );
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"[WoolyPath] 创建GameSettings时出错: {e.Message}");
//                EditorUtility.DisplayDialog("错误", $"创建GameSettings时出错:\n{e.Message}", "确定");
//            }
//        }

//        /// <summary>
//        /// 菜单栏选项：WoolyPath/添加UIManager到场景（优先级13）
//        /// 功能：在场景中创建UIManager（UI管理器），避免重复创建
//        /// </summary>
//        [MenuItem("WoolyPath/添加UIManager到场景", false, 13)]
//        public static void AddUIManagerToScene()
//        {
//            try
//            {
//                // 1. 检查场景中是否已存在UIManager（避免重复创建）
//                var existing = Object.FindObjectOfType<UIManager>();
//                if (existing != null)
//                {
//                    EditorUtility.DisplayDialog("提示", "场景中已存在UIManager！", "确定");
//                    // 选中已存在的UIManager（方便用户定位）
//                    Selection.activeGameObject = existing.gameObject;
//                    return;
//                }

//                // 2. 创建UIManager游戏对象并添加组件
//                GameObject uiManagerObj = new GameObject("UIManager");
//                var uiManager = uiManagerObj.AddComponent<UIManager>();

//                // 3. 找到或创建SYSTEMS容器（将UIManager归入管理器容器，保持场景整洁）
//                GameObject systems = GameObject.Find("=== SYSTEMS ===");
//                if (systems != null)
//                {
//                    uiManagerObj.transform.SetParent(systems.transform);
//                }

//                // 4. 选中新创建的UIManager（方便用户立即配置）
//                Selection.activeGameObject = uiManagerObj;

//                // 5. 标记场景为已修改
//                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

//                Debug.Log("[WoolyPath] UIManager添加到场景完成");
//                EditorUtility.DisplayDialog(
//                    "完成",
//                    "UIManager已添加到场景！\n请在Inspector中配置UI引用。",
//                    "确定"
//                );
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"[WoolyPath] 添加UIManager时出错: {e.Message}");
//                EditorUtility.DisplayDialog("错误", $"添加UIManager时出错:\n{e.Message}", "确定");
//            }
//        }

//        /// <summary>
//        /// 菜单栏选项：WoolyPath/添加SceneManager到场景（优先级14）
//        /// 功能：在场景中创建WoolySceneManager（场景切换管理器），避免重复创建
//        /// </summary>
//        [MenuItem("WoolyPath/添加SceneManager到场景", false, 14)]
//        public static void AddSceneManagerToScene()
//        {
//            try
//            {
//                // 1. 检查场景中是否已存在SceneManager
//                var existing = Object.FindObjectOfType<WoolySceneManager>();
//                if (existing != null)
//                {
//                    EditorUtility.DisplayDialog("提示", "场景中已存在SceneManager！", "确定");
//                    Selection.activeGameObject = existing.gameObject;
//                    return;
//                }

//                // 2. 创建SceneManager游戏对象并添加组件
//                GameObject sceneManagerObj = new GameObject("SceneManager");
//                var sceneManager = sceneManagerObj.AddComponent<WoolySceneManager>();

//                // 3. 将SceneManager归入SYSTEMS容器（保持场景结构整洁）
//                GameObject systems = GameObject.Find("=== SYSTEMS ===");
//                if (systems != null)
//                {
//                    sceneManagerObj.transform.SetParent(systems.transform);
//                }

//                // 4. 选中新创建的SceneManager
//                Selection.activeGameObject = sceneManagerObj;

//                // 5. 标记场景为已修改
//                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

//                Debug.Log("[WoolyPath] SceneManager添加到场景完成");
//                EditorUtility.DisplayDialog(
//                    "完成",
//                    "SceneManager已添加到场景！\n请在Inspector中配置场景设置。",
//                    "确定"
//                );
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"[WoolyPath] 添加SceneManager时出错: {e.Message}");
//                EditorUtility.DisplayDialog("错误", $"添加SceneManager时出错:\n{e.Message}", "确定");
//            }
//        }

//        /// <summary>
//        /// 私有辅助方法：创建核心管理器系统（GameManager、LevelManager等）
//        /// 内部调用CreateManager泛型方法实现具体管理器的创建
//        /// </summary>
//        private static void CreateManagerSystem()
//        {
//            Debug.Log("[WoolyPath] 创建管理器系统...");

//            // 1. 创建管理器容器（=== SYSTEMS ===），用于统一管理所有管理器
//            GameObject systems = FindOrCreateGameObject("=== SYSTEMS ===");

//            // 2. 创建各核心管理器（泛型方法：自动添加对应MonoBehaviour组件）
//            CreateManager<GameManager>("GameManager", systems.transform);    // 游戏主管理器
//            CreateManager<LevelManager>("LevelManager", systems.transform);  // 关卡管理器
//            CreateManager<AudioManager>("AudioManager", systems.transform);  // 音频管理器
//            CreateManager<InputManager>("InputManager", systems.transform);  // 输入管理器

//            Debug.Log("[WoolyPath] 管理器系统创建完成");
//        }

//        /// <summary>
//        /// 私有泛型辅助方法：创建指定类型的管理器（避免重复代码）
//        /// </summary>
//        /// <typeparam name="T">管理器组件类型（必须继承自MonoBehaviour）</typeparam>
//        /// <param name="name">管理器游戏对象名称</param>
//        /// <param name="parent">父节点（用于归入SYSTEMS容器）</param>
//        private static void CreateManager<T>(string name, Transform parent) where T : MonoBehaviour
//        {
//            // 1. 检查场景中是否已存在该类型的管理器（单例思想，避免重复）
//            T existing = Object.FindObjectOfType<T>();
//            if (existing != null)
//            {
//                // 如果管理器存在但父节点不正确，移动到指定父节点（保持结构整洁）
//                if (existing.transform.parent != parent)
//                {
//                    existing.transform.SetParent(parent);
//                    Debug.Log($"[WoolyPath] 移动现有管理器: {name}");
//                }
//                return; // 已存在则直接返回，不重复创建
//            }

//            // 2. 不存在则创建新管理器：新建游戏对象→设置父节点→添加组件
//            GameObject managerObj = new GameObject(name);
//            managerObj.transform.SetParent(parent);
//            managerObj.AddComponent<T>();

//            Debug.Log($"[WoolyPath] 创建管理器: {name}");
//        }

//        /// <summary>
//        /// 私有辅助方法：创建场景组织结构（三大核心容器）
//        /// 用于分类管理场景中的不同类型对象，保持场景层级整洁
//        /// </summary>
//        private static void CreateSceneStructure()
//        {
//            Debug.Log("[WoolyPath] 创建场景结构...");

//            // 创建三大容器：
//            // 1. === SYSTEMS ===：存储所有管理器（如GameManager、UIManager）
//            // 2. === GAMEPLAY ===：存储游戏逻辑对象（如玩家、敌人、关卡区域）
//            // 3. === ENVIRONMENT ===：存储环境对象（如地面、栅栏、场景装饰）
//            FindOrCreateGameObject("=== SYSTEMS ===");
//            FindOrCreateGameObject("=== GAMEPLAY ===");
//            FindOrCreateGameObject("=== ENVIRONMENT ===");

//            Debug.Log("[WoolyPath] 场景结构创建完成");
//        }

//        /// <summary>
//        /// 私有辅助方法：整理场景中已存在的对象到对应容器
//        /// 配合OrganizeScene方法使用，实现对象自动归类
//        /// </summary>
//        private static void OrganizeExistingObjects()
//        {
//            Debug.Log("[WoolyPath] 整理现有对象...");

//            // 1. 获取三大容器（如果不存在则提示警告）
//            GameObject systems = GameObject.Find("=== SYSTEMS ===");
//            GameObject gameplay = GameObject.Find("=== GAMEPLAY ===");
//            GameObject environment = GameObject.Find("=== ENVIRONMENT ===");

//            if (systems == null || gameplay == null || environment == null)
//            {
//                Debug.LogWarning("[WoolyPath] 场景结构不完整，请先运行'创建场景结构'");
//                return;
//            }

//            // 2. 整理管理器：将指定名称的管理器归入SYSTEMS容器
//            string[] managerNames = { "GameManager", "LevelManager", "AudioManager", "InputManager" };
//            foreach (string managerName in managerNames)
//            {
//                GameObject manager = GameObject.Find(managerName);
//                if (manager != null && manager.transform.parent != systems.transform)
//                {
//                    manager.transform.SetParent(systems.transform);
//                    Debug.Log($"[WoolyPath] 整理管理器: {managerName}");
//                }
//            }

//            // 3. 整理游戏对象：将GameArea归入GAMEPLAY容器
//            GameObject gameArea = GameObject.Find("GameArea");
//            if (gameArea != null && gameArea.transform.parent != gameplay.transform)
//            {
//                gameArea.transform.SetParent(gameplay.transform);
//                Debug.Log("[WoolyPath] 整理GameArea到GAMEPLAY");
//            }

//            // 4. 整理环境对象：将指定名称的环境对象归入ENVIRONMENT容器
//            string[] envNames = { "Environment", "地面", "栅栏", "栅栏 (1)" };
//            foreach (string envName in envNames)
//            {
//                GameObject envObj = GameObject.Find(envName);
//                if (envObj != null && envObj.transform.parent != environment.transform)
//                {
//                    envObj.transform.SetParent(environment.transform);
//                    Debug.Log($"[WoolyPath] 整理环境对象: {envName}");
//                }
//            }
//        }

//        /// <summary>
//        /// 私有辅助方法：查找指定名称的游戏对象，不存在则创建
//        /// 用于统一处理"查找/创建"逻辑，避免重复代码
//        /// </summary>
//        /// <param name="name">游戏对象名称</param>
//        /// <returns>找到或新创建的游戏对象</returns>
//        private static GameObject FindOrCreateGameObject(string name)
//        {
//            // 1. 先尝试查找场景中是否存在该对象
//            GameObject existing = GameObject.Find(name);
//            if (existing != null) return existing;

//            // 2. 不存在则创建新对象
//            GameObject newObj = new GameObject(name);
//            Debug.Log($"[WoolyPath] 创建组织对象: {name}");
//            return newObj;
//        }

//        /// <summary>
//        /// 私有辅助方法：配置主相机（不存在则创建，已存在则跳过）
//        /// 为相机设置默认位置、旋转、标签和AudioListener组件
//        /// </summary>
//        private static void SetupCamera()
//        {
//            // 1. 获取主相机（通过"MainCamera"标签查找）
//            Camera mainCamera = Camera.main;
//            if (mainCamera == null)
//            {
//                // 2. 不存在则创建：新建对象→设置标签→添加相机和音频监听器组件
//                GameObject cameraObj = new GameObject("Main Camera");
//                cameraObj.tag = "MainCamera"; // 必须设置此标签，否则Camera.main无法找到
//                mainCamera = cameraObj.AddComponent<Camera>();
//                cameraObj.AddComponent<AudioListener>(); // 音频监听器：用于接收场景中的音频

//                // 3. 设置相机默认位置和旋转（适合顶视角/第三人称视角的初始参数）
//                cameraObj.transform.position = new Vector3(0, 15.8f, -14.9f);
//                cameraObj.transform.rotation = Quaternion.Euler(48, 0, 0);

//                Debug.Log("[WoolyPath] 创建主相机");
//            }
//            else
//            {
//                Debug.Log("[WoolyPath] 主相机已存在");
//            }
//        }

//        /// <summary>
//        /// 私有辅助方法：配置主光源（方向光）
//        /// 不存在则创建，已存在则调整光照属性（强度、阴影类型）
//        /// </summary>
//        private static void SetupLighting()
//        {
//            // 1. 查找场景中已有的方向光（主光源通常为方向光）
//            Light[] lights = Object.FindObjectsOfType<Light>();
//            Light directionalLight = null;

//            foreach (Light light in lights)
//            {
//                if (light.type == LightType.Directional)
//                {
//                    directionalLight = light;
//                    break;
//                }
//            }

//            // 2. 不存在方向光则创建
//            if (directionalLight == null)
//            {
//                GameObject lightObj = new GameObject("Directional Light");
//                directionalLight = lightObj.AddComponent<Light>();
//                directionalLight.type = LightType.Directional; // 设置为方向光
//                // 设置光源默认旋转（模拟太阳角度，避免场景过暗）
//                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

//                Debug.Log("[WoolyPath] 创建主光源");
//            }
//            else
//            {
//                Debug.Log("[WoolyPath] 主光源已存在");
//            }

//            // 3. 统一设置光源属性（确保光照效果一致）
//            directionalLight.intensity = 1f; // 光照强度
//            directionalLight.shadows = LightShadows.Soft; // 软阴影（提升画面质量）
//        }

//        /// <summary>
//        /// 私有辅助方法：生成项目状态字符串
//        /// 用于ShowProjectStatus方法展示关键信息
//        /// </summary>
//        /// <returns>格式化的项目状态文本</returns>
//        private static string GetProjectStatus()
//        {
//            var status = "=== WoolyPath 项目状态 ===\n\n";

//            // 1. 当前场景信息
//            Scene activeScene = SceneManager.GetActiveScene();
//            status += $"当前场景: {activeScene.name}\n";
//            status += $"场景路径: {activeScene.path}\n\n";

//            // 2. 核心管理器存在性检测（✓表示存在，✗表示不存在）
//            status += "管理器状态:\n";
//            status += $"- GameManager: {(Object.FindObjectOfType<GameManager>() != null ? "✓" : "✗")}\n";
//            status += $"- LevelManager: {(Object.FindObjectOfType<LevelManager>() != null ? "✓" : "✗")}\n";
//            status += $"- AudioManager: {(Object.FindObjectOfType<AudioManager>() != null ? "✓" : "✗")}\n";
//            status += $"- InputManager: {(Object.FindObjectOfType<InputManager>() != null ? "✓" : "✗")}\n\n";

//            // 3. 场景结构完整性检测
//            status += "场景结构:\n";
//            status += $"- === SYSTEMS ===: {(GameObject.Find("=== SYSTEMS ===") != null ? "✓" : "✗")}\n";
//            status += $"- === GAMEPLAY ===: {(GameObject.Find("=== GAMEPLAY ===") != null ? "✓" : "✗")}\n";
//            status += $"- === ENVIRONMENT ===: {(GameObject.Find("=== ENVIRONMENT ===") != null ? "✓" : "✗")}\n\n";

//            // 4. 基础组件检测（相机、光源）
//            status += "基础组件:\n";
//            status += $"- 主相机: {(Camera.main != null ? "✓" : "✗")}\n";
//            status += $"- 主光源: {(Object.FindObjectOfType<Light>() != null ? "✓" : "✗")}\n";

//            return status;
//        }
//    }
//}