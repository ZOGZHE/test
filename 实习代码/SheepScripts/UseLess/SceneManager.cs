//using System.Collections;
//using UnityEngine;
//using UnityEngine.SceneManagement;

//// 命名空间：WoolyPath（可根据项目实际情况修改）
//namespace WoolyPath
//{
//    // 场景管理器类，负责所有场景加载相关操作
//    public class WoolySceneManager : MonoBehaviour
//    {
//        // 单例实例，提供全局访问点
//        public static WoolySceneManager Instance { get; private set; }

//        [Header("场景配置")] // 编辑器中显示的标题
//        [SerializeField] private string gamePlaySceneName = "GamePlay"; // 游戏主场景名称
//        [SerializeField] private string[] levelSceneNames; // 所有关卡场景名称数组
//        [SerializeField] private float sceneTransitionDelay = 0.5f; // 场景切换延迟时间（秒）

//        [Header("加载设置")] // 编辑器中显示的标题
//        [SerializeField] private bool useAsyncLoading = true; // 是否使用异步加载
//        [SerializeField] private bool showLoadingScreen = true; // 是否显示加载屏幕

//        private bool isLoading = false; // 标记是否正在加载场景
//        private int currentLevelIndex = -1; // 当前关卡索引（-1表示未在关卡中）

//        // 场景加载事件（供外部订阅，监听加载状态）
//        public System.Action<string> OnSceneLoadStarted; // 加载开始时触发（参数：场景名称）
//        public System.Action<string, float> OnSceneLoadProgress; // 加载进度更新时触发（参数：场景名称、进度0-1）
//        public System.Action<string> OnSceneLoadCompleted; // 加载完成时触发（参数：场景名称）

//        // 初始化单例
//        private void Awake()
//        {
//            // 单例模式实现：如果实例不存在，则创建并保留；否则销毁重复对象
//            if (Instance == null)
//            {
//                Instance = this;
//                if (transform.parent != null)
//                {
//                    transform.SetParent(null);
//                }
//                DontDestroyOnLoad(gameObject); // 切换场景时不销毁该对象
//                InitializeSceneManager(); // 初始化场景管理器
//            }
//            else
//            {
//                Destroy(gameObject); // 销毁重复实例
//            }
//        }

//        // 初始化场景管理器
//        private void InitializeSceneManager()
//        {
//            // 如果关卡场景数组未设置，则初始化默认关卡名称
//            if (levelSceneNames == null || levelSceneNames.Length == 0)
//            {
//                levelSceneNames = new string[] { "Level01", "Level02", "Level03" };
//            }

//            //Debug.Log("[SceneManager] 场景管理器初始化完成");
//        }

//        // 加载游戏主场景
//        public void LoadGamePlayScene()
//        {
//            LoadScene(gamePlaySceneName);
//        }

//        // 加载指定索引的关卡
//        public void LoadLevel(int levelIndex)
//        {
//            // 校验关卡索引是否有效
//            if (levelIndex < 0 || levelIndex >= levelSceneNames.Length)
//            {
//                Debug.LogError($"[SceneManager] 无效的关卡索引: {levelIndex}");
//                return;
//            }

//            currentLevelIndex = levelIndex; // 更新当前关卡索引
//            string sceneName = levelSceneNames[levelIndex]; // 获取关卡场景名称
//            LoadScene(sceneName); // 加载场景
//        }

//        // 加载下一关
//        public void LoadNextLevel()
//        {
//            int nextLevel = currentLevelIndex + 1; // 计算下一关索引
//            if (nextLevel < levelSceneNames.Length)
//            {
//                LoadLevel(nextLevel); // 加载下一关
//            }
//            else
//            {
//                Debug.Log("[SceneManager] 已到达最后一关");
//                // 到达最后一关后返回主场景（可根据需求修改为显示通关界面等）
//                LoadGamePlayScene();
//            }
//        }

//        // 重启当前关卡
//        public void RestartCurrentLevel()
//        {
//            if (currentLevelIndex >= 0)
//            {
//                LoadLevel(currentLevelIndex); // 重新加载当前关卡
//            }
//            else
//            {
//                Debug.LogWarning("[SceneManager] 没有当前关卡可以重新开始");
//            }
//        }

//        // 加载指定名称的场景
//        public void LoadScene(string sceneName)
//        {
//            // 如果正在加载中，则忽略新的加载请求
//            if (isLoading)
//            {
//                Debug.LogWarning("[SceneManager] 正在加载场景中，请稍等...");
//                return;
//            }

//            // 启动协程处理场景加载（协程可实现异步等待逻辑）
//            StartCoroutine(LoadSceneCoroutine(sceneName));
//        }

//        // 场景加载协程（核心加载逻辑）
//        private IEnumerator LoadSceneCoroutine(string sceneName)
//        {
//            isLoading = true; // 标记为正在加载

//            // 触发加载开始事件
//            OnSceneLoadStarted?.Invoke(sceneName);
//            Debug.Log($"[SceneManager] 开始加载场景: {sceneName}");

//            // 如果需要显示加载屏幕，且UIManager存在，则显示加载界面
//            if (showLoadingScreen && UIManager.Instance != null)
//            {
//                UIManager.Instance.ShowLoadingScreen($"加载场景: {sceneName}");
//            }

//            // 等待场景切换延迟（可用于播放过渡动画）
//            yield return new WaitForSeconds(sceneTransitionDelay);

//            if (useAsyncLoading)
//            {
//                // 异步加载场景（不会阻塞主线程，避免卡顿）
//                AsyncOperation asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
//                asyncLoad.allowSceneActivation = false; // 暂时不激活场景（可用于等待加载完成后手动激活）

//                // 监控加载进度
//                while (!asyncLoad.isDone)
//                {
//                    // 异步加载进度范围是0-0.9，这里转换为0-1的进度值
//                    float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

//                    // 触发进度更新事件
//                    OnSceneLoadProgress?.Invoke(sceneName, progress);
//                    // 更新UI加载进度（如果UIManager存在）
//                    if (UIManager.Instance != null)
//                    {
//                        UIManager.Instance.UpdateLoadingProgress(progress);
//                    }

//                    // 当加载到90%时（即asyncLoad.progress >= 0.9）
//                    if (asyncLoad.progress >= 0.9f)
//                    {
//                        // 可以在这里添加额外的准备工作（如资源预加载）
//                        yield return new WaitForSeconds(0.5f);
//                        asyncLoad.allowSceneActivation = true; // 激活场景
//                    }

//                    yield return null; // 等待一帧，避免死循环阻塞
//                }
//            }
//            else
//            {
//                // 同步加载场景（会阻塞主线程，可能导致卡顿）
//                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
//            }

//            // 隐藏加载屏幕
//            if (showLoadingScreen && UIManager.Instance != null)
//            {
//                UIManager.Instance.HideLoadingScreen();
//            }

//            // 触发加载完成事件
//            OnSceneLoadCompleted?.Invoke(sceneName);
//            Debug.Log($"[SceneManager] 场景加载完成: {sceneName}");

//            isLoading = false; // 标记为加载完成
//        }

//        // 获取当前是否正在加载场景
//        public bool IsLoading()
//        {
//            return isLoading;
//        }

//        // 获取当前关卡索引
//        public int GetCurrentLevelIndex()
//        {
//            return currentLevelIndex;
//        }

//        // 获取当前激活的场景名称
//        public string GetCurrentSceneName()
//        {
//            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
//        }

//        // 判断指定场景是否为关卡场景
//        public bool IsLevelScene(string sceneName)
//        {
//            foreach (string levelScene in levelSceneNames)
//            {
//                if (levelScene == sceneName)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        // 获取总关卡数量
//        public int GetTotalLevels()
//        {
//            return levelSceneNames.Length;
//        }

//    }
//}
