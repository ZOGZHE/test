using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoolyPath
{
    /// <summary>
    /// 游戏状态枚举：定义游戏所有可能的运行状态
    /// </summary>
    public enum GameState
    {
        MainMenu,       // 主菜单状态（初始状态，未进入游戏）
        Playing,        // 游戏进行中状态（玩家操作关卡）
        Paused,         // 游戏暂停状态（时间暂停，界面显示暂停面板）
        GameOver,       // 游戏失败状态（关卡挑战失败，显示失败面板）
        Victory,        // 胜利状态（关卡挑战成功，显示胜利面板）
        Lost,           // 失败状态（与GameOver功能类似，可根据需求细分场景）
        Loading,        // 加载状态（场景/关卡切换时的过渡状态）
        Buying          // 购买状态（如商店购买道具时的状态）
    }

    public class GameManager : MonoBehaviour
    {
        // 单例实例：确保全局唯一，外部通过 GameManager.Instance 访问
        public static GameManager Instance { get; private set; }

        [Header("游戏基础设置")]
        [SerializeField] private float gameStartDelay = 1f;  // 游戏启动时的延迟时间（用于过渡动画/准备资源）

        [Header("调试设置")]
        [SerializeField] private bool enableDebugLog = true;  // 是否启用调试日志（发布版本建议关闭）

        public GameState CurrentState { get; private set; }

        public int CurrentLevel { get; private set; }

        private void Awake()
        {
            // 单例模式实现：防止重复创建实例
            if (Instance == null)
            {
                Instance = this;
                // 若当前对象有父物体，剥离父物体（避免被父物体销毁影响）
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);  // 标记为跨场景保留（切换关卡/菜单不销毁）
                InitializeGame();  // 初始化游戏核心配置
            }
            else
            {
                Destroy(gameObject);  // 若已存在实例，销毁当前重复对象
            }
        }


        private void Start()
        {
            // 启动游戏开始序列（协程处理延迟逻辑）
            StartCoroutine(GameStartSequence());
        }
        private void InitializeGame()
        {
            Application.targetFrameRate = 60;  // 设置游戏目标帧率（避免性能波动）
            ChangeGameState(GameState.MainMenu);  // 初始状态设为主菜单
        }

        private IEnumerator GameStartSequence()
        {
            // 等待设置的延迟时间
            yield return new WaitForSeconds(gameStartDelay);
            DebugLog("游戏准备就绪，当前状态：主菜单");
        }

        public void StartLevel(int levelIndex)
        {
            CurrentLevel = levelIndex;  // 更新当前关卡索引
            ChangeGameState(GameState.Playing);  // 切换状态为"游戏进行中"

            // 通过LevelManager加载关卡（职责分离：GameManager管流程，LevelManager管具体加载）
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadLevel(levelIndex);
            }


            // 埋点：记录关卡开始事件（Lion SDK 数据分析）
            if (LionSDKManager.Instance != null)
            {
                LionSDKManager.Instance.LogMissionStarted(levelIndex);
            }
        }

        public void LoadNextLevel()
        {
            CurrentLevel++;  // 当前关卡索引自增（进入下一关）
            ChangeGameState(GameState.Playing);  // 切换状态为"游戏进行中"

            // 通过LevelManager加载下一关
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadNextLevel();
            }

            DebugLog($"启动下一关：索引{CurrentLevel}（显示为Level {CurrentLevel + 1}）");
        }


        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeGameState(GameState.Paused);  // 切换状态为"暂停"
                Time.timeScale = 0f;  // 冻结游戏时间（物理、动画等均暂停）
                DebugLog("游戏已暂停");
            }
        }

        public void RetryGame()
        {
            // 埋点：记录关卡放弃事件（重试属于放弃当前尝试）
            if (LionSDKManager.Instance != null)
            {
                LionSDKManager.Instance.LogMissionAbandoned(CurrentLevel);
            }

            ChangeGameState(GameState.Playing);  // 切换状态为"游戏进行中"

            // 通过LevelManager重新加载当前关卡
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.LoadLevel(CurrentLevel);
            }

            // 埋点：记录重试后的关卡开始事件
            if (LionSDKManager.Instance != null)
            {
                LionSDKManager.Instance.LogMissionStarted(CurrentLevel);
            }

            DebugLog("游戏重试：重新加载当前关卡");
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                ChangeGameState(GameState.Playing);  // 切换状态为"游戏进行中"
                Time.timeScale = 1f;  // 恢复游戏时间（正常运行）
                DebugLog("游戏已继续");
            }
        }


        public void RestartLevel()
        {
            // 埋点：记录关卡放弃事件（重启属于放弃当前尝试）
            if (LionSDKManager.Instance != null)
            {
                LionSDKManager.Instance.LogMissionAbandoned(CurrentLevel);
            }

            Time.timeScale = 1f;  // 确保时间恢复正常（防止从暂停状态重启时时间冻结）
            StartLevel(CurrentLevel);  // 重新启动当前关卡
            DebugLog("关卡重启：重新加载当前关卡");
        }



        public void CompleteLevel()
        {
            // 埋点：记录关卡完成事件
            if (LionSDKManager.Instance != null)
            {
                LionSDKManager.Instance.LogMissionComplete(CurrentLevel);
            }

            ChangeGameState(GameState.Victory);  // 切换状态为"胜利"
            DebugLog($"关卡完成：索引{CurrentLevel}（显示为Level {CurrentLevel + 1}）");
        }


        public void GameOver()
        {
            // 埋点：记录关卡失败事件（原因标记为"game_over"）
            if (LionSDKManager.Instance != null)
            {
                LionSDKManager.Instance.LogMissionFailed(CurrentLevel, "game_over");
            }

            ChangeGameState(GameState.GameOver);  // 切换状态为"游戏结束"
            DebugLog("游戏结束：关卡挑战失败");
        }


        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;  // 确保时间恢复正常（防止带暂停状态返回菜单）
            ChangeGameState(GameState.MainMenu);  // 切换状态为主菜单

            DebugLog("返回主菜单");
        }

        public void ChangeGameState(GameState newState)
        {
            CurrentState = newState;  // 更新当前状态

            // 触发全局游戏状态变更事件（其他脚本可监听此事件做对应处理，如UI刷新）
            GameEvents.TriggerGameStateChanged(newState);
        }


        public void CheckVictoryCondition()
        {
            // 通过LevelManager判断当前关卡是否满足完成条件（职责分离：LevelManager管关卡逻辑）
            if (LevelManager.Instance != null && LevelManager.Instance.IsLevelComplete())
            {
                CompleteLevel();  // 满足条件则执行"关卡完成"逻辑
            }
        }


        private void DebugLog(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }

    }
}