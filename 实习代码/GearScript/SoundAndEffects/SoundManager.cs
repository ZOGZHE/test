using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SuperGear
{
    #region 声音数据配置类
    [Serializable]
    public class SoundData
    {
        // 声音唯一标识（代码中通过此Key调用）
        public string soundKey;
        // 对应的音频文件
        public AudioClip audioClip;
        // 是否为背景音乐（true=BGM，false=音效）
        public bool isBgm;
        // 基础循环模式（仅对BGM有效：true=普通循环，false=单次播放）
        public bool isLoop;
        // 【BGM专用】是否启用“播放→休息→重复”模式（启用后忽略isLoop）
        public bool useRestLoop;
        // 【BGM专用】休息间隔范围（秒，仅useRestLoop=true时有效）
        public Vector2 restRange = new Vector2(10, 15);
        // 音量（0-1）
        [Range(0, 1)] public float volume = 1f;
    }
    #endregion

    #region 声音管理器核心类（简化为音效+BGM两类）
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }
        // 本地开关（默认true开启）
        public bool IsBgmEnabled = true;
        public bool IsSfxEnabled = true;
        // 所有声音配置（音效+BGM，在编辑器中配置）
        public List<SoundData> allSounds = new List<SoundData>();
        // 全局音效音量（0-1）
        [Range(0, 1)] public float sfxVolume = 1f;
        // 全局BGM音量（0-1）
        [Range(0, 1)] public float bgmVolume = 1f;
        // 启动时自动播放的默认BGM（填写soundKey，可选）
        public string defaultBgmKey;

        [Header("BGM轮流播放配置")]
        [Tooltip("启用BGM轮流播放模式（多首BGM依次播放）")]
        public bool usePlaylist = false;
        [Tooltip("BGM播放列表（按顺序填写soundKey，如bgm1、bgm2）")]
        public List<string> bgmPlaylist = new List<string>();
        [Tooltip("BGM播放完成后的休息时间（秒）")]
        public float bgmRestInterval = 15f;

        // 私有变量
        private Dictionary<string, SoundData> _soundDict = new Dictionary<string, SoundData>(); // 快速查找字典
        private Transform _sfxParent; // 音效父节点
        private AudioSource _currentBgmSource; // 当前播放的BGM
        private Coroutine _bgmRestLoopCoroutine; // BGM休息循环的协程引用
        private int _currentPlaylistIndex = 0; // 当前播放列表索引

        private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        [Header("音效池配置")]
        public int poolInitialSize = 10; // 初始池大小
        public int poolMaxSize = 30;     // 池最大容量（避免过多音频通道占用）

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
                InitSoundDict();
                InitSfxParent();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 自动播放BGM
            if (usePlaylist && bgmPlaylist != null && bgmPlaylist.Count > 0)
            {
                // 播放列表模式：播放第一首BGM
                StartPlaylist();
            }
            else if (!string.IsNullOrEmpty(defaultBgmKey))
            {
                // 单曲模式：播放默认BGM
                PlayBGM(defaultBgmKey);
            }
        }
        #endregion

        #region 初始化
        // 初始化音效池
        private void InitSfxPool()
        {
            for (int i = 0; i < poolInitialSize; i++)
            {
                CreatePooledAudioSource();
            }
        }
        // 创建池化AudioSource
        private AudioSource CreatePooledAudioSource()
        {
            GameObject soundObj = new GameObject("Pooled_Sound");
            soundObj.transform.parent = _sfxParent;
            soundObj.SetActive(false); // 初始隐藏（未使用）

            AudioSource audioSource = soundObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D音效（3D音效需调整为1f）
            audioSource.bypassReverbZones = true; // 绕过混响，减少计算

            _sfxPool.Enqueue(audioSource);
            return audioSource;
        }
        // 初始化声音字典
        private void InitSoundDict()
        {
            _soundDict.Clear();
            foreach (var sound in allSounds)
            {
                if (string.IsNullOrEmpty(sound.soundKey))
                {
                    Debug.LogError("存在空的声音Key，请检查配置！");
                    continue;
                }
                if (sound.audioClip == null)
                {
                    Debug.LogError($"声音Key [{sound.soundKey}] 未关联音频文件！");
                    continue;
                }
                if (_soundDict.ContainsKey(sound.soundKey))
                {
                    Debug.LogError($"声音Key [{sound.soundKey}] 重复！");
                    continue;
                }
                _soundDict.Add(sound.soundKey, sound);
            }
        }

        // 初始化音效父节点
        private void InitSfxParent()
        {
            GameObject parent = new GameObject("SFX_Container");
            parent.transform.parent = transform;
            _sfxParent = parent.transform;
        }
        #endregion

        #region 音效播放（仅处理非BGM的声音）
        public AudioSource PlaySound(string soundKey, Vector3 position = default, Transform parent = null)
        {
            if (!IsSfxEnabled) return null;
            // 1. 查找声音配置（原有校验逻辑保留）
            if (!_soundDict.TryGetValue(soundKey, out SoundData soundData))
            {
                Debug.LogError($"未找到Key为 [{soundKey}] 的声音！");
                return null;
            }

            // 校验：音效不能标记为BGM（原有逻辑保留）
            if (soundData.isBgm)
            {
                Debug.LogError($"Key [{soundKey}] 是BGM，应使用PlayBGM方法播放！");
                return null;
            }

            // 2. 从池获取AudioSource（无则创建，不超过最大容量）
            AudioSource audioSource = null;
            if (_sfxPool.Count > 0)
            {
                audioSource = _sfxPool.Dequeue(); // 从池取出可用对象
            }
            else if (_sfxParent.childCount < poolMaxSize)
            {
                audioSource = CreatePooledAudioSource(); // 池满前创建新对象
            }
            else
            {
                Debug.LogWarning($"音效池已满（最大{poolMaxSize}个），无法播放[{soundKey}]！");
                return null;
            }

            // 3. 配置音效对象（位置、父节点、激活状态）
            GameObject soundObj = audioSource.gameObject;
            soundObj.SetActive(true); // 激活对象（池化对象默认隐藏）
            soundObj.transform.position = position;
            soundObj.transform.parent = parent ?? _sfxParent;
            soundObj.name = $"Sound_{soundKey}"; // 命名方便调试

            // 4. 配置音频属性并播放（原有ConfigureAudioSource逻辑保留）
            ConfigureAudioSource(audioSource, soundData);
            audioSource.Play();

            // 5. 非循环音效：播放完成后回收至池（替换原有Destroy逻辑）
            if (!soundData.isLoop)
            {
                StartCoroutine(RecycleSfxCoroutine(audioSource, soundData.audioClip.length));
            }

            return audioSource;
        }
        #endregion

        #region BGM播放（统一控制，支持两种模式）
        //同步BGM状态（开关切换时调用）
        public void SyncBgmState()
        {
            if (IsBgmEnabled)
            {
                if (_currentBgmSource == null)
                {
                    // 根据模式启动对应的播放
                    if (usePlaylist && bgmPlaylist != null && bgmPlaylist.Count > 0)
                    {
                        StartPlaylist();
                    }
                    else if (!string.IsNullOrEmpty(defaultBgmKey))
                    {
                        PlayBGM(defaultBgmKey);
                    }
                }
            }
            else if (!IsBgmEnabled && _currentBgmSource != null)
            {
                StopCurrentBGM();
            }
        }

        /// <summary>开始播放BGM列表</summary>
        public void StartPlaylist()
        {
            if (!IsBgmEnabled) return;
            if (bgmPlaylist == null || bgmPlaylist.Count == 0)
            {
                Debug.LogWarning("BGM播放列表为空！");
                return;
            }

            _currentPlaylistIndex = 0;
            StopCurrentBGM();
            _bgmRestLoopCoroutine = StartCoroutine(PlaylistCoroutine());
        }

        /// <summary>BGM播放列表协程（依次播放列表中的BGM）</summary>
        private IEnumerator PlaylistCoroutine()
        {
            while (true)
            {
                // 获取当前要播放的BGM Key
                string currentBgmKey = bgmPlaylist[_currentPlaylistIndex];

                // 校验BGM配置
                if (!_soundDict.TryGetValue(currentBgmKey, out SoundData bgmData))
                {
                    Debug.LogError($"播放列表中的BGM [{currentBgmKey}] 未找到！跳过...");
                    // 跳到下一首
                    _currentPlaylistIndex = (_currentPlaylistIndex + 1) % bgmPlaylist.Count;
                    continue;
                }

                if (!bgmData.isBgm)
                {
                    Debug.LogError($"播放列表中的 [{currentBgmKey}] 不是BGM！跳过...");
                    _currentPlaylistIndex = (_currentPlaylistIndex + 1) % bgmPlaylist.Count;
                    continue;
                }

                // 创建并配置AudioSource
                GameObject bgmObj = new GameObject($"BGM_{currentBgmKey}");
                bgmObj.transform.parent = transform;
                _currentBgmSource = bgmObj.AddComponent<AudioSource>();
                ConfigureAudioSource(_currentBgmSource, bgmData);
                _currentBgmSource.loop = false; // 播放列表模式不循环单曲

                // 播放BGM
                _currentBgmSource.Play();
                Debug.Log($"[BGM播放列表] 正在播放: {currentBgmKey} ({_currentPlaylistIndex + 1}/{bgmPlaylist.Count})");

                // 等待BGM播放完成
                yield return new WaitForSeconds(bgmData.audioClip.length);

                // 销毁当前BGM对象
                if (_currentBgmSource != null)
                {
                    Destroy(_currentBgmSource.gameObject);
                    _currentBgmSource = null;
                }

                // BGM播放完成后休息15秒
                Debug.Log($"[BGM播放列表] BGM播放完成,休息 {bgmRestInterval} 秒...");
                yield return new WaitForSeconds(bgmRestInterval);

                // 切换到下一首（循环播放列表）
                _currentPlaylistIndex = (_currentPlaylistIndex + 1) % bgmPlaylist.Count;
            }
        }
        /// <summary>播放BGM（自动根据配置选择“普通循环”或“播放→休息→重复”）</summary>
        public void PlayBGM(string bgmKey)
        {
            if (!IsBgmEnabled)
            {
                StopCurrentBGM();
                return;
            }
            // 停止当前BGM及协程
            StopCurrentBGM();

            // 校验配置
            if (!_soundDict.TryGetValue(bgmKey, out SoundData bgmData))
            {
                Debug.LogError($"未找到Key为 [{bgmKey}] 的BGM！");
                return;
            }
            if (!bgmData.isBgm)
            {
                Debug.LogError($"Key [{bgmKey}] 不是BGM（未勾选isBgm）！");
                return;
            }
            if (bgmData.useRestLoop && bgmData.restRange.x > bgmData.restRange.y)
            {
                Debug.LogError($"BGM [{bgmKey}] 的休息范围无效（x应小于y）！");
                return;
            }

            // 创建BGM的AudioSource
            GameObject bgmObj = new GameObject($"BGM_{bgmKey}");
            bgmObj.transform.parent = transform;
            _currentBgmSource = bgmObj.AddComponent<AudioSource>();
            ConfigureAudioSource(_currentBgmSource, bgmData);

            // 根据配置选择播放模式
            if (bgmData.useRestLoop)
            {
                // 模式1：播放→休息→重复（协程控制）
                _bgmRestLoopCoroutine = StartCoroutine(RestLoopCoroutine(bgmData));
            }
            else
            {
                // 模式2：普通循环（原生loop属性）
                _currentBgmSource.loop = bgmData.isLoop;
                _currentBgmSource.Play();
            }
        }

        /// <summary>停止当前播放的BGM</summary>
        public void StopCurrentBGM()
        {
            // 停止休息循环协程
            if (_bgmRestLoopCoroutine != null)
            {
                StopCoroutine(_bgmRestLoopCoroutine);
                _bgmRestLoopCoroutine = null;
            }
            // 销毁当前BGM的AudioSource
            if (_currentBgmSource != null)
            {
                Destroy(_currentBgmSource.gameObject);
                _currentBgmSource = null;
            }
        }

        /// <summary>暂停/恢复当前BGM</summary>
        public void ToggleBGM(bool isPause)
        {
            if (_currentBgmSource == null) return;

            if (isPause && _currentBgmSource.isPlaying)
                _currentBgmSource.Pause();
            else if (!isPause && !_currentBgmSource.isPlaying)
                _currentBgmSource.UnPause();
        }

        /// <summary>BGM休息循环协程（播放→等待完成→随机休息→重复）</summary>
        private IEnumerator RestLoopCoroutine(SoundData bgmData)
        {
            while (true)
            {
                // 播放BGM
                _currentBgmSource.Play();
                // 等待BGM播放完成
                yield return new WaitForSeconds(bgmData.audioClip.length);
                // 随机休息一段时间
                float restTime = UnityEngine.Random.Range(bgmData.restRange.x, bgmData.restRange.y);
                yield return new WaitForSeconds(restTime);
            }
        }
        #endregion

        #region 工具方法（音量配置与同步）
        /// <summary>音效播放完成后回收至池（复用核心逻辑）</summary>
        private IEnumerator RecycleSfxCoroutine(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay); // 等待音效播放完成

            // 安全校验：避免对象已被手动销毁
            if (audioSource == null || audioSource.gameObject == null) yield break;

            // 重置音频状态（避免影响下次复用）
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.gameObject.SetActive(false); // 隐藏对象
            audioSource.transform.parent = _sfxParent; // 回归默认父节点

            // 回收至池（供下次播放使用）
            _sfxPool.Enqueue(audioSource);
        }
        /// <summary>配置AudioSource属性</summary>
        private void ConfigureAudioSource(AudioSource source, SoundData data)
        {
            source.clip = data.audioClip;
            source.loop = false; // 禁用原生loop（BGM的循环由代码控制）
            source.volume = data.volume * (data.isBgm ? bgmVolume : sfxVolume); // 关联全局音量
            source.playOnAwake = false;
        }

        /// <summary>更新全局音效音量（实时生效）</summary>
        public void UpdateSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            foreach (Transform child in _sfxParent)
            {
                AudioSource source = child.GetComponent<AudioSource>();
                if (source != null)
                {
                    SoundData data = _soundDict.Values.FirstOrDefault(d => d.audioClip == source.clip);
                    if (data != null && !data.isBgm) // 只更新音效
                        source.volume = data.volume * sfxVolume;
                }
            }
        }

        /// <summary>更新全局BGM音量（实时生效）</summary>
        public void UpdateBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (_currentBgmSource != null)
            {
                SoundData data = _soundDict.Values.FirstOrDefault(d => d.audioClip == _currentBgmSource.clip);
                if (data != null)
                    _currentBgmSource.volume = data.volume * bgmVolume;
            }
        }
        #endregion
    }
    #endregion
}