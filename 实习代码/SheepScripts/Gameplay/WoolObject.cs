#region 引用
using System;
using System.Collections;
using System.Drawing;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
#endregion

namespace WoolyPath
{
    public class WoolObject : MonoBehaviour
    {
        #region 字段定义
        [Header("羊毛设置")]
        [SerializeField] private WoolColor woolColor = WoolColor.Green; // 羊毛颜色
        [SerializeField] private Renderer woolRenderer; // 羊毛渲染器

        [Header("物理设置")]
        [SerializeField] private float baseLaunchForce = 10f; // 发射力
        [SerializeField] private AnimationCurve flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 飞行曲线（横轴：时间，纵轴：数值）

        [Header("视觉效果")]
        [SerializeField] private ParticleSystem trailEffect; // 轨迹粒子效果
        [SerializeField] private GameObject collectEffect; // 收集效果预制体

        [Header("音频")]
        [SerializeField] private string launchSoundName = "wool_launch"; // 发射音效名称
        [SerializeField] private string collectSoundName = "wool_collect"; // 收集音效名称

        [Header("动态发射配置")]
        [SerializeField] private float minLaunchAngle = 15f;   // 最小发射角度（避免轨迹过缓）
        [SerializeField] private float maxLaunchAngle = 70f;   // 最大发射角度（避免轨迹过陡）
        [SerializeField] private float gravityFactor = 9.8f;   // 重力系数（匹配Unity物理引擎）
        [SerializeField] private float forceAdjustRatio = 0.2f;// 力度调整系数（调试用，控制发射距离）
        [SerializeField] private float PerfectGoldenSpiral = 3;// 力度调整系数（调试用，控制发射距离）

        private Rigidbody woolRigidbody; // 羊毛刚体组件
        //private Collider woolCollider; // 羊毛碰撞器组件
        private bool isFlying = false; // 是否正在飞行中
        private bool isOnBelt = false; // 是否在传送带上
        public bool isCollected = false; // 是否已被收集
       // public int CollectionStage = 0;
        #endregion

        #region 生命周期方法
        private void Awake()
        {
            // 如果未指定渲染器，尝试从子物体获取
            if (woolRenderer == null)
            {
                Debug.LogError("woolRenderer == null");
                woolRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void Start()
        {
            Initialize(woolColor);
            // 如果有轨迹效果则播放
            if (trailEffect != null)
            {
                trailEffect.Play();
            }
        }
        #endregion

        #region 公共方法
        // 标记羊毛为"已收集"（仅在收集成功时调用）
        public void MarkAsCollected()
        {
            isCollected = true;
        }

        // 初始化羊毛颜色
        public void Initialize(WoolColor color)
        {
            // 触发羊毛创建事件
            GameEvents.TriggerWoolCreated(this);
        }
        // 请求销毁羊毛对象（供外部调用，如收集器）
        public void RequestDestruction()
        {
            // 直接触发销毁逻辑，无需再播放收集动画
            PlayCollectEffects(transform.position); // 在当前位置播放效果
            DestroyWool();
        }

        #endregion

        #region 发射羊毛

        // 发射羊毛
        ////public void LaunchToBelt(Vector3 targetPosition, float ForceMultiple, float targetAngle)
        //public void Launch(WoolObject wool, Vector3 start, Vector3 end)
        //{
        //    if (isFlying) return;
        //    isFlying = true;

        //    var (dynamicForce, dynamicAngle) = GetDynamicLaunchParam(wool, start, end); // 获取动态发射参数
        //    StartCoroutine(LaunchWithAnimation(end, dynamicForce, dynamicAngle)); // 协程发射
        //}
        public void OutLaunch(WoolObject wool, Vector3 start, Vector3 end)
        {
            StartCoroutine(Launch( wool,  start,  end));
        }
        public IEnumerator Launch(WoolObject wool, Vector3 start, Vector3 end)
        {
            if (isFlying) yield break; // 如果正在飞行，直接退出
            isFlying = true;

            var (dynamicForce, dynamicAngle) = GetDynamicLaunchParam(wool, start, end);
            // 返回飞行动画协程，让外部可以等待它完成
            yield return LaunchWithAnimation(end, dynamicForce, dynamicAngle);
        }
        // 计算动态力度+角度
        private (float force, float angle) GetDynamicLaunchParam(WoolObject wool, Vector3 start, Vector3 end)
        {
            float horizontalDist = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z)); // 水平距离
            float heightDiff = end.y - start.y; // 高度差
            float targetAngle = GetDynamicLaunchAngle(start,end); // 动态角度
            float angleRad = Mathf.Deg2Rad * targetAngle; // 角度转弧度

            // 核心力度公式（基于抛物线物理：初速度 = √(射程×g / sin(2θ))）
            float baseForce = Mathf.Sqrt((horizontalDist + Mathf.Abs(heightDiff) * 0.5f) * gravityFactor / Mathf.Sin(2 * angleRad));
            float finalForce = baseForce * forceAdjustRatio ; // 最终力度（结合配置系数）

            //// 检查是否有有效收集器，无则用默认参数
            //CollectorPlate[] collectorPlates = FindObjectsOfType<CollectorPlate>();
            //bool hasValidCollector = Array.Exists(collectorPlates, c => c != null && c.WoolCanBeCollect(wool));
            //if (!hasValidCollector) return (1f, 45f); // 默认力度1，角度45°

            return (finalForce, targetAngle);
        }

        // 根据收集器位置计算动态发射角度（返回角度：度）
        private float GetDynamicLaunchAngle(Vector3 start,Vector3 end)
        {

            // 计算水平距离（忽略Y轴）
            float horizontalDist = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));
            float heightDiff = end.y - start.y; // 高度差（收集器-羊毛生成点）

            if (horizontalDist < 0.1f) return 45f; // 近距离默认45°（避免除零错误）

            // 基础角度计算（反正切+高度补偿）
            float angleRad = Mathf.Atan2(heightDiff + horizontalDist * 0.2f, horizontalDist);
            float angleDeg = Mathf.Rad2Deg * angleRad;
            return Mathf.Clamp(angleDeg, minLaunchAngle, maxLaunchAngle); // 限制角度范围
        }

        // 发射羊毛
        private IEnumerator LaunchWithAnimation(Vector3 targetPosition, float forceMultiple, float targetAngle)
        {
            Vector3 startPosition = transform.position;
            // 水平距离
            float horizontalDist = Vector3.Distance(
                new Vector3(startPosition.x, 0, startPosition.z),
                new Vector3(targetPosition.x, 0, targetPosition.z)
            );
            float angleRad = Mathf.Deg2Rad * targetAngle; // 动态角度转弧度
            float finalForce = baseLaunchForce * forceMultiple; // 最终力度

            // 计算飞行时间（基于水平距离和力度）
            float flightTime = horizontalDist / (finalForce * Mathf.Cos(angleRad));
            float timer = 0f;

            while (timer < flightTime)
            {
                timer += Time.deltaTime;
                float t = timer / flightTime;
                float curveValue = flightCurve.Evaluate(t);

                // 基础水平位置插值
                Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, curveValue);

                // 用动态角度计算Y轴高度（抛物线公式：y = y0 + v0*sinθ*t - 0.5*g*t²）
                float verticalVelocity = finalForce * Mathf.Sin(angleRad);
                float gravity = 0.5f * Physics.gravity.magnitude * timer * timer;
                currentPos.y = startPosition.y + verticalVelocity * timer - gravity;

                transform.position = currentPos;
                transform.Rotate(Vector3.right, PerfectGoldenSpiral * 360f * Time.deltaTime); // 保留旋转动画
                yield return null;
            }

            transform.position = targetPosition;

                OnReachedBelt();
        }


        #endregion

        #region 私有方法
        public void SetOnBelt(bool onBelt)
        {
            isOnBelt = onBelt;
        }

        ///到达传送带时的处理
        private void OnReachedBelt()
        {
            if (isOnBelt) return;

            isFlying = false;
            isOnBelt = true;

            // 停止物理运动
            if (woolRigidbody != null)
            {
                woolRigidbody.isKinematic = true;
            }

            // 添加到传送带
            if (ConveyorBelt.Instance != null)
            {
                bool added = ConveyorBelt.Instance.TryAddWool(this);
                if (added)
                {
                    GameEvents.TriggerWoolAddedToBelt(this);
                }
                else
                {
                    // 传送带已满 - 游戏结束相关处理
                    Debug.LogWarning("[WoolObject] 传送带已满!");
                    DestroyWool();
                }
            }
        }
        // 销毁羊毛对象
        private void DestroyWool()
        {
            GameEvents.TriggerWoolDestroyed(this);

            // 停止轨迹效果
            if (trailEffect != null)
            {
                trailEffect.Stop();
            }

            Destroy(gameObject);
        }

        #endregion

        #region 特效
        // 播放收集效果（音效和粒子）
        private void PlayCollectEffects(Vector3 position)
        {
          
            // 播放收集粒子效果
            if (collectEffect != null)
            {
                GameObject effect = Instantiate(collectEffect, position, Quaternion.identity);
                Destroy(effect, 2f); // 2秒后销毁效果对象
            }
        }
        #endregion

        #region 公共访问器
        public WoolColor Color => woolColor;
        public bool IsFlying => isFlying;
        public bool IsOnBelt => isOnBelt;
        public bool IsCollected => isCollected;
        #endregion

        #region 编辑器方法
        // 编辑器中绘制Gizmos
        private void OnDrawGizmosSelected()
        {
            // 绘制收集半径指示器
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
        #endregion
    }
}
