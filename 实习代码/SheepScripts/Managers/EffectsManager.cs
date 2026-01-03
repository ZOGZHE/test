using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    public class EffectsManager : MonoBehaviour
    {
        public static EffectsManager Instance { get; private set; }

        [Header("点击特效")]
        [SerializeField] private GameObject clickEffectPrefab; // 点击特效的预制体资源
        [SerializeField] private float clickEffectDuration = 1f; // 点击特效的显示时长（秒）
        [SerializeField] private float clickEffectScale = 1f; // 点击特效的缩放比例

        [Header("剃毛特效")]
        [SerializeField] private GameObject shearingEffectPrefab; // 剃毛时的特效预制体
        [SerializeField] private float shearingEffectDuration = 1.5f; // 剃毛特效的显示时长（秒）

        [Header("收集特效")]
        [SerializeField] private GameObject collectionEffectPrefab; // 收集羊毛时的特效预制体
        [SerializeField] private float collectionEffectDuration = 1f; // 收集特效的显示时长（秒）

        [Header("颜色配置")]
        [SerializeField]
        private Color[] colorEffects = new Color[]
        {
            new Color(0.4f, 0.8f, 0.4f), // 绿色（对应WoolColor.Green）
            new Color(1f, 0.9f, 0.3f),   // 黄色（对应WoolColor.Yellow）
            new Color(1f, 0.6f, 0.8f),   // 粉色（对应WoolColor.Pink）
            new Color(1f, 0.6f, 0.2f),   // 橙色（对应WoolColor.Orange）
            new Color(0.3f, 0.7f, 1f),   // 蓝色（对应WoolColor.Blue）
            new Color(0.7f, 0.4f, 0.9f)  // 紫色（对应WoolColor.Purple）
        };

        [Header("层级管理")]
        [SerializeField] private Transform effectsParent; // 所有特效的父节点（用于层级管理）

        // 活动特效列表：记录当前正在显示的特效，用于统一管理和清理
        private List<GameObject> activeEffects = new List<GameObject>();


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this; // 若实例不存在，设置为当前对象
                InitializeEffectsManager(); // 初始化特效管理器
            }
            else
            {
                Destroy(gameObject); // 若实例已存在，销毁当前对象（保证单例唯一性）
            }
        }

        /// <summary>初始化特效管理器核心逻辑</summary>
        private void InitializeEffectsManager()
        {
            // 若未指定特效父节点，自动创建一个（避免场景层级混乱）
            if (effectsParent == null)
            {
                GameObject parentGO = new GameObject("Effects"); // 创建名为"Effects"的空物体
                effectsParent = parentGO.transform; // 设为父节点
            }
        }

        #region 点击特效相关方法

        public void PlayClickEffect(Vector3 position)
        {
            // 直接创建新特效对象
            GameObject effect = CreateNewEffect(clickEffectPrefab);
            if (effect == null) return; // 若创建失败，直接返回

            // 设置特效的位置、颜色和缩放
            SetupEffect(effect, position, clickEffectScale);
            // 延迟指定时间后销毁特效
            StartCoroutine(DestroyAfterDelay(effect, clickEffectDuration));
        }


        public void PlayEnhancedClickEffect(Vector3 position)
        {
            // 先播放基础点击特效
            PlayClickEffect(position);
            //暂不使用
            // 附加屏幕震动效果
            //StartCoroutine(CameraShakeEffect(0f, 1f));

        
        }

        #endregion


        #region 剃毛特效相关方法

        public void PlayShearingEffect(Vector3 position)
        {
            // 直接创建新特效对象
            GameObject effect = CreateNewEffect(shearingEffectPrefab);
            if (effect == null) return;

            // 设置特效属性
           SetupEffect(effect, position, 0.6f);
            // 延迟销毁
            StartCoroutine(DestroyAfterDelay(effect, shearingEffectDuration));

         
        }

        #endregion

        #region 收集特效相关方法
        public void PlayCollectionEffect(Vector3 position)
        {
            // 直接创建新特效对象
            GameObject effect = CreateNewEffect(collectionEffectPrefab);
            if (effect == null) return;

            // 设置特效属性
            SetupEffect(effect, position,  0.4f);
            // 延迟销毁
            StartCoroutine(DestroyAfterDelay(effect, collectionEffectDuration));

  
        }

        #endregion

        #region 通用工具方法

        private GameObject CreateNewEffect(GameObject prefab)
        {
            if (prefab == null) return null;

            // 实例化新特效
            GameObject effect = Instantiate(prefab, effectsParent);
            effect.SetActive(true);
            activeEffects.Add(effect); // 加入活动列表
            return effect;
        }


        private void SetupEffect(GameObject effect, Vector3 position, float scale)
        {
            effect.transform.position = position; // 设置位置
            effect.transform.localScale = Vector3.one * scale; // 设置缩放

            // 为粒子系统设置颜色（若有）
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main; // 获取粒子系统主模块
                
            }
        }

        //private Color GetColorForWool(WoolColor woolColor)
        //{
        //    int index = (int)woolColor; // 将枚举转换为索引
        //    // 检查索引是否在有效范围内
        //    if (index >= 0 && index < colorEffects.Length)
        //    {
        //        return colorEffects[index];
        //    }
        //    return Color.white; // 索引无效时返回白色
        //}

        private IEnumerator DestroyAfterDelay(GameObject effect, float delay)
        {
            yield return new WaitForSeconds(delay); // 等待指定时长

            if (effect != null)
            {
                activeEffects.Remove(effect); // 从活动列表移除
                Destroy(effect); // 销毁特效对象
            }
        }

        #endregion

        #region 屏幕震动效果

        /// <summary>实现相机震动效果（协程）</summary>
        /// <param name="duration">震动持续时间（秒）</param>
        /// <param name="magnitude">震动幅度（数值越大震动越强烈）</param>
        private IEnumerator CameraShakeEffect(float duration, float magnitude)
        {
            Camera mainCamera = Camera.main; // 获取主相机
            if (mainCamera == null) yield break; // 若相机不存在，终止协程

            Vector3 originalPosition = mainCamera.transform.position; // 记录初始位置
            float elapsed = 0f; // 已震动时间

            while (elapsed < duration)
            {
                // 生成随机偏移量（基于震动幅度）
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                // 应用偏移（保持z轴不变，避免相机前后移动）
                mainCamera.transform.position = originalPosition + new Vector3(x, y, 0);

                elapsed += Time.deltaTime; // 累加时间
                yield return null; // 等待一帧
            }

            // 震动结束后恢复初始位置
            mainCamera.transform.position = originalPosition;
        }

        #endregion

        #region 特效管理与清理

        /// <summary>强制清理所有正在显示的特效</summary>
        public void ClearAllActiveEffects()
        {
            foreach (var effect in activeEffects)
            {
                if (effect != null)
                {
                    Destroy(effect); // 销毁特效
                }
            }
            activeEffects.Clear(); // 清空活动列表

            Debug.Log("[EffectsManager] 清理了所有活动特效");
        }

        /// <summary>获取当前正在显示的特效数量（用于性能监控）</summary>
        /// <returns>活动特效数量</returns>
        public int GetActiveEffectsCount()
        {
            return activeEffects.Count;
        }

        #endregion
    }
}
