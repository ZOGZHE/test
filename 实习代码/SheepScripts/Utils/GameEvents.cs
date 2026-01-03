using System;
using UnityEngine;

namespace WoolyPath
{
    /// <summary>
    /// 游戏事件管理类（静态类）
    /// 用于定义游戏中所有事件、提供事件触发方法和清理方法
    /// 实现不同系统/组件间的解耦通信
    /// </summary>
    public static class GameEvents
    {
        #region 事件定义（按类别分组）

        // 游戏状态事件
        /// <summary>当游戏状态发生变化时触发（如从运行到暂停）</summary>
        /// <param name="GameState">新的游戏状态</param>
        public static event Action<GameState> OnGameStateChanged;

        // 绵羊相关事件
        /// <summary>当绵羊被点击时触发</summary>
        /// <param name="SheepController">被点击的绵羊控制器</param>
        /// <param name="Vector3">点击的世界坐标位置</param>
        public static event Action<SheepController, Vector3> OnSheepClicked;

        /// <summary>当绵羊开始移动时触发</summary>
        /// <param name="SheepController">开始移动的绵羊控制器</param>
        public static event Action<SheepController> OnSheepMoveStarted;

        /// <summary>当绵羊完成移动时触发</summary>
        /// <param name="SheepController">完成移动的绵羊控制器</param>
        public static event Action<SheepController> OnSheepMoveCompleted;

        /// <summary>当绵羊被停用（如移除/销毁）时触发</summary>
        /// <param name="SheepController">被停用的绵羊控制器</param>
        public static event Action<SheepController> OnSheepDeactivated;

        // 羊毛相关事件
        /// <summary>当羊毛对象被创建时触发</summary>
        /// <param name="WoolObject">被创建的羊毛对象</param>
        public static event Action<WoolObject> OnWoolCreated;

        /// <summary>当羊毛被添加到传送带上时触发</summary>
        /// <param name="WoolObject">被添加的羊毛对象</param>
        public static event Action<WoolObject> OnWoolAddedToBelt;

        /// <summary>当羊毛被收集时触发</summary>
        /// <param name="WoolObject">被收集的羊毛对象</param>
        public static event Action<WoolObject> OnWoolCollected;

        /// <summary>当羊毛被销毁时触发</summary>
        /// <param name="WoolObject">被销毁的羊毛对象</param>
        public static event Action<WoolObject> OnWoolDestroyed;

        // 收集器相关事件
        /// <summary>当收集板收集到羊毛时触发</summary>
        /// <param name="CollectorPlate">收集羊毛的收集板</param>
        /// <param name="WoolColor">收集到的羊毛颜色</param>
        public static event Action<CollectorPlate, WoolColor> OnWoolCollectedByPlate;

        /// <summary>当收集板完成收集目标（如收集足够数量/颜色的羊毛）时触发</summary>
        /// <param name="CollectorPlate">完成目标的收集板</param>
        public static event Action<CollectorPlate> OnCollectorPlateCompleted;

        // 关卡相关事件
        /// <summary>当关卡开始时触发</summary>
        /// <param name="int">关卡索引（如第1关、第2关）</param>
        public static event Action<int> OnLevelStarted;

        /// <summary>当关卡完成时触发</summary>
        /// <param name="int">关卡索引</param>
        public static event Action<int> OnLevelCompleted;

        /// <summary>当关卡失败时触发</summary>
        /// <param name="int">关卡索引</param>
        public static event Action<int> OnLevelFailed;

        /// <summary>当关卡数据加载完成时触发</summary>
        /// <param name="LevelData">加载完成的关卡数据</param>
        public static event Action<LevelData> OnLevelDataLoaded;

        // UI相关事件
        /// <summary>当暂停菜单打开时触发</summary>
        public static event Action OnPauseMenuOpened;

        /// <summary>当暂停菜单关闭时触发</summary>
        public static event Action OnPauseMenuClosed;

        /// <summary>当设置菜单打开时触发</summary>
        public static event Action OnSettingsMenuOpened;

        /// <summary>当设置菜单关闭时触发</summary>
        public static event Action OnSettingsMenuClosed;

        // 音频相关事件
        /// <summary>当需要播放背景音乐时触发</summary>
        /// <param name="string">音乐名称（用于查找对应的音频文件）</param>
        public static event Action<string> OnMusicRequested;

        /// <summary>当需要播放音效时触发</summary>
        /// <param name="string">音效名称（用于查找对应的音频文件）</param>
        public static event Action<string> OnSFXRequested;

        // 传送带相关事件
        /// <summary>当传送带满载时触发</summary>
        public static event Action OnConveyorBeltFull;

        /// <summary>当传送带容量发生变化时触发</summary>
        /// <param name="int">新的容量值</param>
        public static event Action<int> OnConveyorCapacityChanged;

        public static event Action OnSwapModeStarted;
        public static event Action OnSwapModeEnded;

        #endregion


        #region 事件触发方法（用于主动发送事件通知）

        // 游戏状态事件触发
        /// <summary>触发游戏状态变化事件</summary>
        /// <param name="newState">新的游戏状态</param>
        public static void TriggerSwapModeStarted()
        {
            OnSwapModeStarted?.Invoke();
        }

        public static void TriggerSwapModeEnded()
        {
            OnSwapModeEnded?.Invoke();
        }
        public static void TriggerGameStateChanged(GameState newState)
        {
            // 使用?.Invoke()安全触发事件（避免事件未被订阅时的空引用异常）
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"[GameEvents] 游戏状态已变更为: {newState}");
        }

        // 绵羊事件触发
        /// <summary>触发绵羊被点击事件</summary>
        public static void TriggerSheepClicked(SheepController sheep, Vector3 clickPosition)
        {
            OnSheepClicked?.Invoke(sheep, clickPosition);
        }

        /// <summary>触发绵羊开始移动事件</summary>
        public static void TriggerSheepMoveStarted(SheepController sheep)
        {
            OnSheepMoveStarted?.Invoke(sheep);
        }

        /// <summary>触发绵羊完成移动事件</summary>
        public static void TriggerSheepMoveCompleted(SheepController sheep)
        {
            OnSheepMoveCompleted?.Invoke(sheep);
        }

        /// <summary>触发绵羊被停用事件</summary>
        public static void TriggerSheepDeactivated(SheepController sheep)
        {
            OnSheepDeactivated?.Invoke(sheep);
        }

        // 羊毛事件触发
        /// <summary>触发羊毛创建事件</summary>
        public static void TriggerWoolCreated(WoolObject wool)
        {
            OnWoolCreated?.Invoke(wool);
        }

        /// <summary>触发羊毛添加到传送带事件</summary>
        public static void TriggerWoolAddedToBelt(WoolObject wool)
        {
            OnWoolAddedToBelt?.Invoke(wool);
        }

        /// <summary>触发羊毛被收集事件</summary>
        public static void TriggerWoolCollected(WoolObject wool)
        {
            OnWoolCollected?.Invoke(wool);
        }

        /// <summary>触发羊毛被销毁事件</summary>
        public static void TriggerWoolDestroyed(WoolObject wool)
        {
            OnWoolDestroyed?.Invoke(wool);
        }

        // 收集器事件触发
        /// <summary>触发收集板收集羊毛事件</summary>
        public static void TriggerWoolCollectedByPlate(CollectorPlate plate, WoolColor color)
        {
            OnWoolCollectedByPlate?.Invoke(plate, color);
        }

        /// <summary>触发收集板完成目标事件</summary>

    
        public static void TriggerCollectorPlateCompleted(CollectorPlate collector)
        {
            OnCollectorPlateCompleted?.Invoke(collector);
        }

        // 新增：层级解锁事件（参数为新解锁的层级）
        public static event Action<int> OnTierUnlocked;
        public static void TriggerTierUnlocked(int newTier)
        {
            OnTierUnlocked?.Invoke(newTier);
        }

        // 关卡事件触发
        /// <summary>触发关卡开始事件</summary>
        public static void TriggerLevelStarted(int levelIndex)
        {
            OnLevelStarted?.Invoke(levelIndex);
            Debug.Log($"[GameEvents] 关卡 {levelIndex} 已开始");
        }

        /// <summary>触发关卡完成事件</summary>
        public static void TriggerLevelCompleted(int levelIndex)
        {
            OnLevelCompleted?.Invoke(levelIndex);
            Debug.Log($"[GameEvents] 关卡 {levelIndex} 已完成");
        }

        /// <summary>触发关卡失败事件</summary>
        public static void TriggerLevelFailed(int levelIndex)
        {
            OnLevelFailed?.Invoke(levelIndex);
            Debug.Log($"[GameEvents] 关卡 {levelIndex} 已失败");
        }

        /// <summary>触发关卡数据加载完成事件</summary>
        public static void TriggerLevelDataLoaded(LevelData levelData)
        {
            OnLevelDataLoaded?.Invoke(levelData);
        }

        // UI事件触发
        /// <summary>触发暂停菜单打开事件</summary>
        public static void TriggerPauseMenuOpened()
        {
            OnPauseMenuOpened?.Invoke();
        }

        /// <summary>触发暂停菜单关闭事件</summary>
        public static void TriggerPauseMenuClosed()
        {
            OnPauseMenuClosed?.Invoke();
        }

        /// <summary>触发设置菜单打开事件</summary>
        public static void TriggerSettingsMenuOpened()
        {
            OnSettingsMenuOpened?.Invoke();
        }

        /// <summary>触发设置菜单关闭事件</summary>
        public static void TriggerSettingsMenuClosed()
        {
            OnSettingsMenuClosed?.Invoke();
        }

        // 音频事件触发
        /// <summary>触发播放背景音乐请求</summary>
        public static void TriggerMusicRequested(string musicName)
        {
            OnMusicRequested?.Invoke(musicName);
        }

        /// <summary>触发播放音效请求</summary>
        public static void TriggerSFXRequested(string sfxName)
        {
            OnSFXRequested?.Invoke(sfxName);
        }

        // 传送带事件触发
        /// <summary>触发传送带满载事件</summary>
        public static void TriggerConveyorBeltFull()
        {
            OnConveyorBeltFull?.Invoke();
            Debug.Log("[GameEvents] 传送带已满载!");
        }

        /// <summary>触发传送带容量变化事件</summary>
        public static void TriggerConveyorCapacityChanged(int newCapacity)
        {
            OnConveyorCapacityChanged?.Invoke(newCapacity);
        }

        #endregion


        #region 事件清理方法

        /// <summary>
        /// 清除所有事件的订阅（在场景切换时特别有用）
        /// 防止旧场景的对象继续订阅事件导致空引用或逻辑错误
        /// </summary>
        public static void ClearAllEvents()
        {
            // 将所有事件设为null，清除所有订阅
            OnGameStateChanged = null;
            OnSheepClicked = null;
            OnSheepMoveStarted = null;
            OnSheepMoveCompleted = null;
            OnSheepDeactivated = null;
            OnWoolCreated = null;
            OnWoolAddedToBelt = null;
            OnWoolCollected = null;
            OnWoolDestroyed = null;
            OnWoolCollectedByPlate = null;
            OnCollectorPlateCompleted = null;
            OnLevelStarted = null;
            OnLevelCompleted = null;
            OnLevelFailed = null;
            OnLevelDataLoaded = null;
            OnPauseMenuOpened = null;
            OnPauseMenuClosed = null;
            OnSettingsMenuOpened = null;
            OnSettingsMenuClosed = null;
            OnMusicRequested = null;
            OnSFXRequested = null;
            OnConveyorBeltFull = null;
            OnConveyorCapacityChanged = null;

            Debug.Log("[GameEvents] 所有事件已清除");
        }

        #endregion
    }
}