using ConnectMaster;

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConnectMaster
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;

        #region  倒计时相关
        // 时间缩放相关
        private float _previousTimeScale = 1f; // 记录暂停前的时间缩放值
        public bool IsGamePaused => Time.timeScale == 0; // 当前是否处于暂停状态
        // 倒计时相关
        private float _remainingTime; // 剩余时间
        private float _totalTime; // 总时长（用于UI显示比例）
        public event Action<float> OnCountdownUpdated; // 传递(剩余时间, 总时长)
        public event Action OnCountdownEnd; // 倒计时结束事件
        public bool _isCountingDown = false; // 倒计时状态标记                             
        public bool _hasResumedCountdown = false;//标记是否已恢复过倒计时（确保只触发一次）
        #endregion


        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateCountdown();
        }
        #endregion

        #region 倒计时控制
        // 初始化并开始倒计时（从当前关卡配置获取时长，每关调用时会重置总时长）
        public void StartCountdown()
        {
            var currentLevelData = LevelManager.Instance.AlllevelData[(LevelManager.Instance.currentLevelIndex) % (LevelManager.Instance.AlllevelData.Count)];
            _totalTime = currentLevelData.countdownDuration; // 从当前关卡数据重置总时长
            _remainingTime = _totalTime;
            _isCountingDown = true; // 开始倒计时
            OnCountdownUpdated?.Invoke(_remainingTime); // 初始通知
        }
        public void RestartCountdown(float time)
        {
            _totalTime = time;
            _remainingTime = _totalTime;
            _isCountingDown = true; // 开始倒计时
            OnCountdownUpdated?.Invoke(_remainingTime); // 初始通知
        }

        // 倒计时更新逻辑
        private void UpdateCountdown()
        {
            if (!_isCountingDown) return;

            _remainingTime -= Time.deltaTime;

            // 确保时间不会小于0
            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
                _isCountingDown = false;
                OnCountdownEnd?.Invoke();
            }

            OnCountdownUpdated?.Invoke(_remainingTime);
        }
        #endregion

        #region 外部便捷调用时间方法

        public void StopCountdown()
        {
            _isCountingDown = false;
        }

        public void ResumeCountdown()
        {
            if (_remainingTime > 0 && !_isCountingDown)
            {
                _isCountingDown = true;
            }
        }

        public void PauseGame()
        {
            // 保存当前时间缩放值（支持从慢动作等状态暂停）
            _previousTimeScale = Time.timeScale;
            // 冻结时间
            Time.timeScale = 0f;
            //Debug.Log("暂停时间");
        }

        public void ResumeGame()
        {
            // 还原到暂停前的时间缩放值
            Time.timeScale = 1;
            //Debug.Log("恢复时间");
        }
        #endregion

    }
}