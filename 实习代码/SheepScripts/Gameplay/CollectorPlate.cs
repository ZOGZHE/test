using EPOOutline;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace WoolyPath
{

    /// <summary>
    /// 收集器核心类
    /// 负责收集特定颜色的羊毛，处理收集逻辑、视觉反馈及完成状态管理
    /// 与LevelManager交互实现关卡层级递进逻辑
    /// </summary>
    public class CollectorPlate : MonoBehaviour
    {
        #region 配置参数
        [Header("核心数据")]
        [Tooltip("收集器配置数据（目标颜色、容量、层级等）")]
        [SerializeField] private CollectorData collectorData;

        [Header("消失设置")]
        [Tooltip("收集完成后延迟销毁时间（秒）")]
        private float destroyDelay = 0.5f;

        [Header("视觉组件")]
        [Tooltip("收集器托盘渲染组件")]
        [SerializeField] private Renderer plateRenderer1;
        [SerializeField] private Renderer[] plateRenderer2;
        [Tooltip("羊毛卷视觉元素的父容器数组")]
        [SerializeField] private Transform[] woolContainer;
        [Tooltip("羊毛卷视觉表现预制体")]
        [SerializeField] private GameObject woolDisplayPrefab;
        // 在状态数据区域添加
        private Color originalPlateColor1; // 存储plateRenderer1的原始颜色
        private bool hasSavedOriginalColor = false; // 标记是否已保存原始颜色

        [Header("特效与动画")]
        [Tooltip("羊毛出现的缩放动画曲线")]
        [SerializeField] private AnimationCurve scaleCurve;

        [Header("音频设置")]
        [Tooltip("收集羊毛音效名称（需在AudioManager中注册）")]
        [SerializeField] private string collectSoundName;
        [Tooltip("收集完成音效名称（需在AudioManager中注册）")]
        [SerializeField] private string completeSoundName;

        [Header("收集条件")]
        [Tooltip("羊毛与传送带入口点的最大允许距离")]
        [SerializeField] private float maxEntryPointDistance = 0.5f;

        private Outlinable _outlinable;
        #endregion


        #region 状态数据
        private List<GameObject> collectedWoolVisuals = new List<GameObject>(); // 收集到的羊毛视觉元素
        private int currentWoolCount = 0; // 当前收集数量
        private bool isComplete = false; // 是否已完成收集

        private bool canCollect = true;  // false时不参与收集
        private int FutureWoolCount = 0;  

        // 锁定状态标记（true=已解锁，可交互；false=锁定，仅视觉可见）
        public bool isUnlocked = false;
        #endregion


        #region 初始化

        private void Awake()
        {
            //CheckAndFindRenderers();
            // 从自身游戏对象获取Outlinable组件
            _outlinable = GetComponent<Outlinable>();

            // 可选：添加空引用检查，避免后续调用出错
            if (_outlinable == null)
            {
                Debug.LogError("当前游戏对象上未找到Outlinable组件！", this);
            }
            _outlinable.enabled = false;
            //取消收集器轮廓设定
        }

      
        private void Start()
        {
            canCollect = true;
        }


        private void CheckAndFindRenderers()
        {
            if (plateRenderer1 == null || plateRenderer2 == null || plateRenderer2.Length == 0)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    plateRenderer1 = renderers[0];
                    if (renderers.Length > 1)
                    {
                        plateRenderer2 = new Renderer[renderers.Length - 1];
                        System.Array.Copy(renderers, 1, plateRenderer2, 0, renderers.Length - 1);
                    }
                    else
                    {
                        plateRenderer2 = new Renderer[] { plateRenderer1 };
                    }
                }
                else
                {
                    Debug.LogError("[CollectorPlate] 未找到任何渲染组件！");
                }
            }
        }


        public void Initialize(CollectorData data)
        {
            collectorData = data;
            transform.position = data.position;

            // 初始解锁逻辑（Tier1解锁，其他锁定）
            isUnlocked = collectorData.Tier == 1;
            
                UpdateVisuals(isUnlocked); // 更新视觉（锁定/解锁样式）
            
        }
        #endregion

        #region 核心收集逻辑

        //public bool TryCollectWool(WoolObject wool)
        //{
        //    // 检查是否可收集
        //    if (!WoolCanBeCollect(wool))
        //        return false;

        //    // 重要：不再直接收集，而是通知传送带启动飞行动画
        //    if (ConveyorBelt.Instance != null)
        //    {
        //        ConveyorBelt.Instance.StartWoolCollectionFlight(wool, this);
        //        return true;
        //    }

        //    return false;
        //}
        // 添加一个公共方法来处理直接收集（用于飞行动画完成后的收集）
        public void WhenAddWool()
        {
            FutureWoolCount++;
            if(FutureWoolCount>collectorData.capacity)
            {
                canCollect = false;  // 容量满时设置为不可收集
            }
        }
        public void DirectCollectWool(WoolObject wool)
        {
            // 再次确认状态（最后一道保险）
            if (!WoolCanBeCollect(wool))
            {
                // 若状态无效，放回传送带
                if (wool != null && !wool.IsOnBelt)
                {
                    wool.SetOnBelt(true);
                    ConveyorBelt.Instance.woolsOnBelt.Add(wool);
                    ConveyorBelt.Instance.woolProgress[wool] = 0f;
                }
                return;
            }

            // 重要：先从传送带移除
            if (wool.IsOnBelt)
            {
                ConveyorBelt.Instance?.RemoveWool(wool);
            }

            currentWoolCount++;
            wool.MarkAsCollected(); // 标记羊毛为已收集

            CreateWoolVisual(); // 创建羊毛视觉表现
            PlayCollectEffects(); // 播放收集反馈

            // 检查是否达到收集目标
            if (currentWoolCount >= collectorData.capacity)
            {
                
                CompleteCollector();
            }

            // 通知羊毛销毁自身
            wool?.RequestDestruction();

            Debug.Log($"[CollectorPlate] 收集 {wool.Color} 羊毛 ({currentWoolCount}/{collectorData.capacity})");
        }


        private IEnumerator SecondaryCollection(WoolObject wool, CollectorPlate collector)
        {
            wool.Launch(wool, ConveyorBelt.Instance.collectPoint.position, collector.transform.position);

            yield return null;
            collector.CollectWool(wool);

        }



        public bool WoolCanBeCollect(WoolObject wool)
        {
            //if (!canCollect)
            //    return false;
            // 1. 基础判空检查
            if (collectorData == null || wool == null)
                return false;

            // 2. 羊毛状态检查（已被收集）
            if (wool.IsCollected)
                return false;

            // 3. 收集器状态检查（已完成或未激活）
            if (isComplete || !gameObject.activeSelf)
                return false;

            // 4. 颜色匹配检查
            if (wool.Color != collectorData.targetColor)
                return false;

            // 5. 容量限制检查
            if (currentWoolCount >= collectorData.capacity)
                return false;

            // 新增：6. 解锁状态检查（未解锁则无法收集）
            if (!isUnlocked)
            {
               // Debug.LogWarning($"[CollectorPlate] 收集器未解锁（Tier{collectorData.Tier}），无法收集");
                return false;
            }

            // 原有：7. 层级权限检查（可保留，作为双重保险）
            if (collectorData.Tier != LevelManager.Instance.GetCurrentAllowedTier())
            {
                Debug.LogWarning($"[CollectorPlate] 层级不匹配：当前允许Tier{LevelManager.Instance.GetCurrentAllowedTier()}，该收集器为Tier{collectorData.Tier}");
                return false;
            }

            return true;
        }


        public void CollectWool(WoolObject wool)
        {
            // 重要：先从传送带移除
            if (wool.IsOnBelt)
            {
                ConveyorBelt.Instance?.RemoveWool(wool);
            }
            currentWoolCount++;
            wool.MarkAsCollected(); // 标记羊毛为已收集

            CreateWoolVisual(); // 创建羊毛视觉表现
            PlayCollectEffects(); // 播放收集反馈

            // 检查是否达到收集目标
            if (currentWoolCount >= collectorData.capacity)
            {
                CompleteCollector();
            }

            // 通知羊毛销毁自身
            wool?.RequestDestruction();

            Debug.Log($"[CollectorPlate] 收集 {wool.Color} 羊毛 ({currentWoolCount}/{collectorData.capacity})");
        }
        #endregion






        #region 视觉与动画
        /// <summary>
        /// 更新收集器的视觉状态
        /// 可扩展用于显示颜色、填充进度等
        /// </summary>
        public void UpdateVisuals(bool enable)
        {
            // 控制轮廓组件显示/隐藏
            //if (_outlinable != null)
            //    _outlinable.enabled = enable;

            // 控制羊毛容器数组显示/隐藏（与轮廓状态同步）
            if (woolContainer != null && woolContainer.Length > 0)
            {
                foreach (var container in woolContainer)
                {
                    if (container != null)
                        container.gameObject.SetActive(enable);
                }
            }
            // 处理plateRenderer1的颜色变化
            if (plateRenderer1 != null)
            {
                // 首次执行时保存原始颜色
                if (!hasSavedOriginalColor)
                {
                    originalPlateColor1 = plateRenderer1.material.color;
                    hasSavedOriginalColor = true;
                }

                // 创建材质副本（避免修改原始材质影响其他对象）
                Material targetMaterial = new Material(plateRenderer1.material);

                if (enable)
                {
                    // 已解锁 - 恢复原始颜色
                    targetMaterial.color = originalPlateColor1;
                }
                else
                {
                    // 未解锁 - 变深变灰（降低亮度和饱和度）
                    Color grayedColor = originalPlateColor1;
                    // 转为灰度（简单算法：取RGB平均值）
                    float grayValue = (grayedColor.r + grayedColor.g + grayedColor.b) / 3f;
                    // 降低亮度（0.3为缩放因子，可调整）
                    grayValue *= 0.4f;
                    grayedColor = new Color(grayValue, grayValue, grayValue, grayedColor.a);

                    targetMaterial.color = grayedColor;
                }

                plateRenderer1.material = targetMaterial;
            }
            if (plateRenderer2 != null && plateRenderer2.Length > 0)
            {
                foreach (var renderer in plateRenderer2)
                {
                    if (renderer != null)
                        renderer.gameObject.SetActive(enable);
                }
            }

        }

        /// <summary>
        /// 创建收集到的羊毛的视觉显示元素
        /// 实例化预制体并设置位置、旋转与颜色
        /// </summary>
        private void CreateWoolVisual()
        {
            if (woolDisplayPrefab == null || woolContainer == null || woolContainer.Length == 0)
                return;

            // 检查容器索引有效性
            if (currentWoolCount - 1 >= woolContainer.Length)
            {
                Debug.LogWarning("[CollectorPlate] 羊毛容器数量不足！");
                return;
            }

            // 获取容器位置与自定义旋转
            Transform targetContainer = woolContainer[currentWoolCount - 1];
            Vector3 woolPosition = targetContainer.position;
            Quaternion woolRotation = Quaternion.Euler(90f, 0f, 0f); // 固定旋转角度

            // 实例化毛线视觉元素
            GameObject woolVisual = Instantiate(woolDisplayPrefab, woolPosition, woolRotation, targetContainer);

            // 设置毛线颜色
           //SetWoolVisualColor(woolVisual);

            // 播放出现动画
           //StartCoroutine(AnimateWoolAppear(woolVisual));

            // 加入管理列表
            collectedWoolVisuals.Add(woolVisual);
        }

        /// <summary>
        /// 设置羊毛视觉元素的颜色
        /// </summary>
        /// <param name="woolVisual">羊毛视觉对象</param>
        private void SetWoolVisualColor(GameObject woolVisual)
        {
            Renderer woolRenderer = woolVisual.GetComponent<Renderer>();
            if (woolRenderer != null && collectorData != null)
            {
                Material woolMaterial = new Material(woolRenderer.material);
                woolMaterial.color = collectorData.targetColor.ToUnityColor();
                woolRenderer.material = woolMaterial;
            }
        }

        #endregion


        #region 特效与音效
        /// <summary>
        /// 播放收集羊毛时的特效与音效
        /// 包括粒子效果、音效和托盘缩放动画
        /// </summary>
        private void PlayCollectEffects()
        {
      

            // 播放收集音效
         

            // 播放托盘缩放动画
            StartCoroutine(CollectScaleAnimation());
        }

        /// <summary>
        /// 收集时的托盘缩放动画（轻微放大再缩小）
        /// 提供收集成功的视觉反馈
        /// </summary>
        private IEnumerator CollectScaleAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.1f;
            float duration = 0.2f;

            // 放大阶段
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / duration);
                yield return null;
            }

            // 缩小阶段
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / duration);
                yield return null;
            }

            transform.localScale = originalScale; // 确保最终大小正确
        }
        #endregion


        #region 完成逻辑
        /// <summary>
        /// 收集完成时的处理逻辑
        /// 标记状态、触发事件、清理资源并销毁自身
        /// </summary>
        private void CompleteCollector()
        {
            isComplete = true;

            // 触发完成事件
            PlayCompleteEffects();
            GameEvents.TriggerCollectorPlateCompleted(this);
            LevelManager.Instance?.OnCollectorCompleted(this);
            LevelManager.Instance?.RemoveCollector(this);
 
            StartCoroutine(FadeAndDestroy());

        }

        /// <summary>
        /// 简化版淡出消失：透明度渐变→销毁
        /// </summary>
        private IEnumerator FadeAndDestroy()
        {
                yield return new WaitForSeconds(destroyDelay); // 等待
            // 清理羊毛视觉元素
            foreach (var woolVisual in collectedWoolVisuals)
                if (woolVisual != null)
                    Destroy(woolVisual);
            collectedWoolVisuals.Clear();
            // 4. 完全透明后销毁
            Destroy(gameObject);
        }

        public void UnlockCollector()
        {
            if (isUnlocked) return; // 避免重复解锁

            isUnlocked = true;
            
            Debug.Log($"[CollectorPlate] {collectorData.targetColor} 收集器已解锁（Tier{collectorData.Tier}）");
        }


        public void OnTierChange()
        {

        }

        /// <summary>
        /// 播放收集完成时的特效与音效
        /// （当前逻辑中未使用，保留用于扩展）
        /// </summary>
        private void PlayCompleteEffects()
        {
            Vector3 effectPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
            if (EffectsManager.Instance != null)
            {
                EffectsManager.Instance.PlayCollectionEffect(effectPosition);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic("3");
            }
            StartCoroutine(CompleteAnimation());
        }

        /// <summary>
        /// 收集完成时的脉冲动画
        /// （当前逻辑中未使用，保留用于扩展）
        /// </summary>
        private IEnumerator CompleteAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 pulseScale = originalScale * 1.3f;
            float pulseDuration = 0.2f;

            // 单次脉冲（可扩展为多次）
            float timer = 0f;
            while (timer < pulseDuration)
            {
                timer += Time.deltaTime;
                transform.localScale = Vector3.Lerp(originalScale, pulseScale, timer / pulseDuration);
                yield return null;
            }

            timer = 0f;
            while (timer < pulseDuration)
            {
                timer += Time.deltaTime;
                transform.localScale = Vector3.Lerp(pulseScale, originalScale, timer / pulseDuration);
                yield return null;
            }

            transform.localScale = originalScale;
        }
        #endregion


        #region 公共接口
        /// <summary>
        /// 获取收集点位置
        /// </summary>
        /// <returns>收集器的世界位置</returns>
        public Vector3 GetCollectionPoint() => transform.position;

        /// <summary>
        /// 获取目标羊毛颜色
        /// </summary>
        /// <returns>目标颜色（默认绿色）</returns>
        public WoolColor GetTargetColor() => collectorData?.targetColor ?? WoolColor.Green;

        /// <summary>
        /// 获取收集容量
        /// </summary>
        /// <returns>最大可收集数量</returns>
        public int GetCapacity() => collectorData?.capacity ?? 1;

        /// <summary>
        /// 获取当前收集数量
        /// </summary>
        /// <returns>当前已收集数量</returns>
        public int GetCurrentCount() => currentWoolCount;

        /// <summary>
        /// 检查是否已完成收集
        /// </summary>
        /// <returns>已完成返回true</returns>
        public bool IsComplete() => isComplete;

        /// <summary>
        /// 获取填充百分比
        /// </summary>
        /// <returns>0-1之间的填充比例</returns>
        public float GetFillPercentage() => collectorData != null ?
            (float)currentWoolCount / collectorData.capacity : 0f;

        /// <summary>
        /// 获取收集器配置数据
        /// </summary>
        /// <returns>收集器数据对象</returns>
        public CollectorData GetData() => collectorData;
        #endregion


        #region 编辑器辅助

        /// <summary>
        /// 编辑器中绘制选中时的辅助线
        /// 可视化收集范围和容量
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制收集范围指示器
            Gizmos.color = collectorData != null ?
                collectorData.targetColor.ToUnityColor() : Color.white;
            Gizmos.DrawWireSphere(transform.position, 1f);

            // 绘制容量指示器
            if (collectorData != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < collectorData.capacity; i++)
                {
                    Vector3 pos = transform.position + Vector3.up * (i * 0.2f + 1f);
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
                }
            }
            // 新增：绘制入口点距离范围（如果传送带存在）
            ConveyorBelt conveyor = ConveyorBelt.Instance;
            if (conveyor != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 entryPoint = conveyor.GetEntryPoint();
                Gizmos.DrawWireSphere(entryPoint, maxEntryPointDistance);

                // 绘制从收集器到入口点的连线
                Gizmos.DrawLine(transform.position, entryPoint);
            }
        }
        #endregion
    }
}