using UnityEngine;

namespace ConnectMaster
{
    /// <summary>
    /// 手机震动管理类（支持iOS/Android，无需额外插件）
    /// </summary>
    public static class VibrationManager
    {
        // 全局震动开关（可后续加UI控制是否开启震动）
        public static bool IsVibrationEnabled = true;


        #region 预设震动模式（直接调用）
        /// <summary>
        /// 短震（按钮点击反馈，最常用）
        /// </summary>
        public static void VibrateShort()
        {
            if (!IsVibrationEnabled) return;

#if UNITY_ANDROID
            // Android 自定义短震（50毫秒，轻强度）
            AndroidVibrate(50);
#elif UNITY_IOS
            // iOS 短震（系统预设轻震）
            Handheld.Vibrate();
#endif
        }

        /// <summary>
        /// 中震（重要操作反馈，如广告激励成功、提示触发）
        /// </summary>
        public static void VibrateMedium()
        {
            if (!IsVibrationEnabled) return;

#if UNITY_ANDROID
            AndroidVibrate(150);
#elif UNITY_IOS
            // iOS 连续短震2次（模拟中震）
            Handheld.Vibrate();
            InvokeOnMainThread(() => Handheld.Vibrate(), 0.1f);
#endif
        }

        /// <summary>
        /// 长震（警告/失败反馈，如倒计时结束、关卡失败）
        /// </summary>
        public static void VibrateLong()
        {
            if (!IsVibrationEnabled) return;

#if UNITY_ANDROID
            AndroidVibrate(300);
#elif UNITY_IOS
            // iOS 连续短震3次（模拟长震）
            Handheld.Vibrate();
            InvokeOnMainThread(() => Handheld.Vibrate(), 0.1f);
            InvokeOnMainThread(() => Handheld.Vibrate(), 0.2f);
#endif
        }
        #endregion

        #region 平台适配底层方法（无需手动调用）
        /// <summary>
        /// Android 自定义震动时长（毫秒）
        /// </summary>
        private static void AndroidVibrate(long durationMs)
        {
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

                // Android API 26+ 需用 vibrate(VibrationEffect)，低版本用 vibrate(long)
                int apiLevel = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                if (apiLevel >= 26)
                {
                    AndroidJavaClass vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                    AndroidJavaObject effect = vibrationEffect.CallStatic<AndroidJavaObject>(
                        "createOneShot", durationMs, 100); // 100=震动强度（0-255）
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", durationMs);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Android 震动调用失败：" + e.Message);
            }
        }

        /// <summary>
        /// 主线程调用（避免iOS震动时序问题）
        /// </summary>
        private static void InvokeOnMainThread(System.Action action, float delay = 0)
        {
            if (action == null) return;
            MonoBehaviour.print("延迟执行震动");
            // 用Unity的主线程调度器
            UnityMainThreadDispatcher.Instance?.Enqueue(() =>
            {
                if (delay > 0)
                    MonoBehaviour.print("延迟执行震动");
                new WaitForSeconds(delay);
                action.Invoke();
            });
        }
        #endregion
    }

    // 辅助类：Unity主线程调度器（解决iOS震动时序问题）
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        public static UnityMainThreadDispatcher Instance { get; private set; }
        private readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> _actions = new();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            while (_actions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        public void Enqueue(System.Action action)
        {
            if (action != null)
                _actions.Enqueue(action);
        }
    }
}
