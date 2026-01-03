using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    [System.Serializable]
    public class AudioClipInfo
    {
        public string name;           // 音频名称
        public AudioClip clip;        // 音频片段
        [Range(0f, 1f)]
        public float volume = 1f;     // 音量
        [Range(0.1f, 3f)]
        public float pitch = 1f;      // 音调
        public bool loop = false;     // 是否循环
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }  // 单例实例

        [Header("音频片段")]
        [SerializeField] private AudioClipInfo[] musicClips;  // 音乐片段数组

        private AudioSource musicSource;  // 音乐播放器组件

        private void Awake()
        {
            // 单例模式实现
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);  // 加载新场景时不销毁

                // 确保有AudioSource组件
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null)
                {
                    musicSource = gameObject.AddComponent<AudioSource>();
                }
            }
            else
            {
                Destroy(gameObject);  // 如果已有实例，销毁当前对象
            }
        }

        /// <summary>
        /// 播放指定名称的音乐
        /// </summary>
        /// <param name="musicName">音乐名称</param>
        public void PlayMusic(string musicName)
        {
            // 查找对应的音乐片段
            AudioClipInfo clipInfo = FindMusicClip(musicName);
            if (clipInfo != null && clipInfo.clip != null)
            {
                // 设置音频源属性
                musicSource.clip = clipInfo.clip;
                musicSource.volume = clipInfo.volume;
                musicSource.pitch = clipInfo.pitch;
                musicSource.loop = clipInfo.loop;

                // 播放音乐
                musicSource.Play();
            }
            else
            {
                Debug.LogWarning($"找不到名为 {musicName} 的音乐片段");
            }
        }

        /// <summary>
        /// 停止当前播放的音乐
        /// </summary>
        public void StopMusic()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// 暂停当前播放的音乐
        /// </summary>
        public void PauseMusic()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }

        /// <summary>
        /// 继续播放暂停的音乐
        /// </summary>
        public void ResumeMusic()
        {
            if (!musicSource.isPlaying && musicSource.clip != null)
            {
                musicSource.UnPause();
            }
        }

        /// <summary>
        /// 检查是否正在播放音乐
        /// </summary>
        /// <returns>是否正在播放</returns>
        public bool IsMusicPlaying()
        {
            return musicSource.isPlaying;
        }

        /// <summary>
        /// 查找指定名称的音乐片段
        /// </summary>
        /// <param name="musicName">音乐名称</param>
        /// <returns>找到的音乐片段信息，找不到返回null</returns>
        private AudioClipInfo FindMusicClip(string musicName)
        {
            foreach (var clipInfo in musicClips)
            {
                if (clipInfo.name == musicName)
                {
                    return clipInfo;
                }
            }
            return null;
        }
    }
}
