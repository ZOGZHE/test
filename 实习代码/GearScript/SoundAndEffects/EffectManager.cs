using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    #region 特效数据类
    // 特效数据类：用于编辑器配置（关联Key和预制体）
    [Serializable]
    public class EffectData
    {
        [Tooltip("特效唯一标识（代码中通过此Key调用）")]
        public string effectKey; // 例如："Explosion"、"Hit"、"Heal"

        [Tooltip("对应的特效预制体")]
        public GameObject effectPrefab; // 手动拖入预制体
    }
    #endregion

    #region 特效管理器
  
    public class EffectManager : MonoBehaviour
    {
        #region 单例与配置
        public static EffectManager Instance { get; private set; }

        [Tooltip("在此处配置所有特效（Key+预制体）")]
        public List<EffectData> allEffects = new List<EffectData>();

        // 内部缓存：Key到预制体的映射（优化查找速度）
        private Dictionary<string, GameObject> _effectDict = new Dictionary<string, GameObject>();
        #endregion

        #region 生命周期
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitEffectDict(); // 初始化映射表
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion


        #region 初始化与缓存
        // 初始化Key到预制体的映射
        private void InitEffectDict()
        {
            _effectDict.Clear();
            foreach (var effect in allEffects)
            {
                // 检查重复Key或空预制体
                if (string.IsNullOrEmpty(effect.effectKey))
                {
                    Debug.LogError("存在空的特效Key，请检查配置！");
                    continue;
                }
                if (effect.effectPrefab == null)
                {
                    Debug.LogError($"特效Key [{effect.effectKey}] 未配置预制体，请检查！");
                    continue;
                }
                if (_effectDict.ContainsKey(effect.effectKey))
                {
                    Debug.LogError($"特效Key [{effect.effectKey}] 重复，请检查！");
                    continue;
                }
                _effectDict.Add(effect.effectKey, effect.effectPrefab);
            }
        }
        #endregion


        #region 特效创建
        public GameObject CreateEffect(string effectKey, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (string.IsNullOrEmpty(effectKey))
            {
                Debug.LogError("特效Key不能为空！");
                return null;
            }

            // 查找对应的预制体
            if (!_effectDict.TryGetValue(effectKey, out GameObject effectPrefab))
            {
                Debug.LogError($"未找到Key为 [{effectKey}] 的特效，请检查配置！");
                return null;
            }

            // 实例化特效
            GameObject effectInstance = Instantiate(effectPrefab, position, rotation, parent);
            effectInstance.name = effectPrefab.name; // 保持名称一致

            // 添加自动销毁组件
            effectInstance.AddComponent<EffectAutoDestroy>();

            return effectInstance;
        }
        #endregion
    }
    #endregion


    #region 特效自动销毁组件
    // 特效自动销毁组件（处理粒子/动画特效的自动回收）
    public class EffectAutoDestroy : MonoBehaviour
    {
        #region 组件与参数
        private ParticleSystem _particleSystem;
        private Animator _animator; // 支持动画特效
        private float _maxLifeTime = 2f; // 最大生命周期（防止无限循环特效）
        private float _lifeTimer;
        #endregion


        #region 组件初始化
        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _animator = GetComponent<Animator>();
        }
        #endregion


        #region 自动销毁逻辑
        private void Update()
        {
            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= _maxLifeTime)
            {
                Destroy(gameObject);
                return;
            }

            // 粒子特效结束判断
            if (_particleSystem != null && !_particleSystem.isPlaying)
            {
                Destroy(gameObject);
            }

            // 动画特效结束判断（假设动画状态名为"Effect"）
            if (_animator != null)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsTag("Effect") && stateInfo.normalizedTime >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }
        #endregion
    }
    #endregion
}