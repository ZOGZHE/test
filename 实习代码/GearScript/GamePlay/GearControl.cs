using UnityEngine;
using SuperGear;
using System.Collections;

namespace SuperGear
{
    #region   齿轮类型旋转方向枚举
    // 齿轮类型枚举
    public enum GearType { Power, Normal, Target }
    // 旋转方向枚举
    public enum RotationDirection { Clockwise, CounterClockwise }
    #endregion
    [RequireComponent(typeof(BoxCollider))]
    public class GearControl : MonoBehaviour
    {
        #region 序列化配置
        [Header("齿轮基础配置")]
        public GearType gearType=GearType.Normal;
        public bool IsRotatingLastMoment = false;
        public float rotationSpeed = 30f;
        public RotationDirection initialDirection = RotationDirection.Clockwise;
        public bool IsShowForHint = false;  //是否是展示用 展示用的齿轮无法旋转

        [Header("传导检测配置")]
        [Tooltip("检测相邻齿轮的范围（建议与齿轮半径一致）")]
        [SerializeField] private float detectRange = 0.5f;
        [Tooltip("忽略Y轴高度差")]
        [SerializeField] private float yTolerance = 0.1f;
        [Tooltip("重复检测的间隔时间（秒），越小越灵敏但性能消耗略高")]
       private float detectInterval = 0.02f;

        [Header("目标齿轮回调")]
        public UnityEngine.Events.UnityEvent OnTargetRotated;
        #endregion

        #region 状态变量
        // 动力状态
        public bool HasOriginalPower;// { get; private set; }
        public bool HasTempPower ;//{ get; private set; }
        public bool IsInPowerChain ;//{ get; private set; }//动力链状态（是否与原始动力齿轮相连）

        private float _secondTimer; // 1秒周期计时器
        private bool _hasRotatedInCurrentSecond; // 当前1秒内是否有旋转
        private bool _lastSecondRotated; // 暂存上一秒旋转状态
        // 旋转状态
        public bool IsRotating { get; private set; }
        public RotationDirection CurrentDirection { get; private set; }
        public bool EffectBool=false;

        private BoxCollider _gearCollider;
        #endregion

        #region 生命周期函数
        private void Awake()
        {
            _gearCollider = GetComponent<BoxCollider>();
            if (_gearCollider == null)
            {
                Debug.LogError($"[GearControl] {gameObject.name} - 未找到BoxCollider组件！");
            }
            else
            {
                _gearCollider.isTrigger = true;
                _gearCollider.size = new Vector3(detectRange * 2, 0.1f, detectRange * 2);
            }

            CurrentDirection = initialDirection;
            IsRotating = false;
            IsInPowerChain = false;  // 初始不在动力链中

            // 初始化动力状态
            HasOriginalPower = gearType == GearType.Power;
            HasTempPower = false;

            _secondTimer = 0f;
            _hasRotatedInCurrentSecond = false;
            _lastSecondRotated = false;
            IsRotatingLastMoment = _lastSecondRotated;


        }

        private void Start()
        {
            if (gearType == GearType.Power)
            {
                //Debug.Log($"[GearControl] {gameObject.name} 是动力齿轮，启动初始旋转");
                IsInPowerChain = true;  // 动力齿轮自身在动力链中
                StartRotation(initialDirection);
            }
        }

        private void Update()
        {
            // 核心监测逻辑
            _secondTimer += Time.deltaTime;
            if (IsRotating) _hasRotatedInCurrentSecond = true; // 标记当前秒是否旋转过

            // 满1秒时更新“上一秒状态”
            if (_secondTimer >= 0.3f)
            {
                IsRotatingLastMoment = _lastSecondRotated; // 赋值最终结果
                _lastSecondRotated = _hasRotatedInCurrentSecond; // 暂存当前秒状态为“下一秒的上一秒”
                _secondTimer = 0f; // 重置计时器
                _hasRotatedInCurrentSecond = false; // 重置当前秒标记
            }
            if (IsRotating)
            {
                RotateGear();
            }
        }
        private void TargetGearEffect()
        {
            LevelManager.Instance.TargetGearEffect();
        }//触发特效

        /// <summary>
        /// 播放目标齿轮特效（音效、震动、粒子效果）
        /// 仅在首次开始旋转时触发（通过IsRotatingLastMoment判断）
        /// </summary>
        private void PlayTargetGearEffects()
        {
            // 只在首次开始旋转时播放特效（避免重复触发）
            if (!IsRotatingLastMoment && gearType == GearType.Target)
            {
                // 播放音效
                SoundManager.Instance?.PlaySound("5");
                // 播放震动
                VibrationManager.VibrateShort();
                // 播放粒子特效（位置在齿轮上方1.5f）
                Vector3 effectPosition = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);
                EffectManager.Instance?.CreateEffect("1", effectPosition, Quaternion.identity);
            }
        }
        #endregion

        #region  目标齿轮上下左右是否有旋转齿轮
        private bool IsNearbyGearRotation()
        {
            // 1. 定义要检测的四个方向（上下左右）
            Vector3[] checkDirections = new Vector3[]
            {
        Vector3.forward,  // 前（上）
        Vector3.back,     // 后（下）
        Vector3.right,    // 右
        Vector3.left      // 左
            };

            // 2. 遍历每个方向检测
            foreach (var dir in checkDirections)
            {
                // 计算检测位置：偏移避免检测自身
                Vector3 detectPos = transform.position + dir * detectRange + dir * 0.05f;

                // 检测指定范围的齿轮（仅“Gear”层）
                Collider[] hitColliders = Physics.OverlapBox(
                    detectPos,
                    new Vector3(detectRange * 0.5f, yTolerance, detectRange * 0.5f), // 检测盒尺寸
                    Quaternion.identity,
                    LayerMask.GetMask("Gear") // 只检测齿轮层
                );

                // 3. 判断检测到的齿轮是否在旋转
                foreach (var hitCol in hitColliders)
                {
                    // 跳过自身
                    if (hitCol.gameObject == gameObject) continue;

                    // 获取齿轮控制组件，判断是否旋转
                    GearControl adjacentGear = hitCol.GetComponent<GearControl>();
                    if (adjacentGear != null && adjacentGear.IsRotating)
                    {
                        return true; // 有旋转的相邻齿轮，返回true
                    }
                }
            }

            // 4. 所有方向都无旋转齿轮，返回false
            return false;
        }

        #endregion

        #region 旋转控制
        public void StartRotation(RotationDirection direction)
        {
            if(IsShowForHint)
            {
                return;
            }
            // 只有拥有动力的齿轮才能开始旋转
            if (!HasOriginalPower && !HasTempPower)
            {
               // Debug.LogWarning($"[GearControl] {gameObject.name} - 没有动力，无法旋转");
                return;
            }

            if (IsRotating)
            {
                // 如果已经旋转但方向不同，更新方向
                if (CurrentDirection != direction)
                {
                    CurrentDirection = direction;
                }
                return;
            }

            IsRotating = true;
            CurrentDirection = direction;

            // 如果是被带动的齿轮，加入动力链
            if (!HasOriginalPower)
            {
                IsInPowerChain = true;
            }

            if (gearType == GearType.Target)
            {
                OnTargetRotated?.Invoke();
                // 直接播放音效和震动，避免通过事件遍历所有齿轮
                PlayTargetGearEffects();
            }

            // 开始周期性检测相邻齿轮
            InvokeRepeating(nameof(TriggerAdjacentGears), 0.02f, detectInterval);
        }
        public void StopRotation()
        {
            //if (!IsRotating)
            //{
            //    return;
            //}

            IsRotating = false;
            IsInPowerChain = false;  // 停止旋转时退出动力链
            CancelInvoke(nameof(TriggerAdjacentGears));

            // 清除临时动力，但保留原始动力
            if (!HasOriginalPower)
            {
                HasTempPower = false;
            }
            // 核心重置逻辑：目标齿轮停止时，重置特效状态（允许下次触发）
            if (gearType == GearType.Target) // 仅目标齿轮需要重置
            {
                //EffectBool = false; // 重置为false，下次旋转时可再次触发特效
            }
            // 通知相邻齿轮检查是否还能保持在动力链中
            NotifyAdjacentGearsToCheckPower();
        }

        private void RotateGear()
        {
            float angle = rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up, CurrentDirection == RotationDirection.Clockwise ? angle : -angle);
        }
        #endregion

        #region 动力传导（带动力链检测）
        private void TriggerAdjacentGears()
        {
            // 只有在动力链中且正在旋转的齿轮才能传递动力
            if (!IsRotating || !IsInPowerChain)
            {
                CancelInvoke(nameof(TriggerAdjacentGears));
                return;
            }

            Vector3[] detectDirections = new Vector3[]
            {
                Vector3.forward, Vector3.back, Vector3.right, Vector3.left
            };

            foreach (var dir in detectDirections)
            {
                DetectAndTriggerGear(dir);
            }
        }

        private void DetectAndTriggerGear(Vector3 direction)
        {
            Vector3 detectPos = transform.position + direction * detectRange;
            detectPos += direction * 0.05f; // 向外偏移避免检测自身

            Collider[] hitColliders = Physics.OverlapBox(
                detectPos,
                new Vector3(detectRange * 0.5f, yTolerance, detectRange * 0.5f),
                Quaternion.identity,
                LayerMask.GetMask("Gear")
            );

            foreach (var hitCol in hitColliders)
            {
                if (hitCol.gameObject == gameObject)
                    continue;

                GearControl adjacentGear = hitCol.GetComponent<GearControl>();
                if (adjacentGear == null)
                {
                    //Debug.LogWarning($"[GearControl] {gameObject.name} - 检测到物体 {hitCol.gameObject.name} 但无GearControl组件");
                    continue;
                }

                // 只有当前齿轮在动力链中，才能给相邻齿轮提供动力
                if (IsInPowerChain)
                {
                    adjacentGear.HasTempPower = true;

                    // 如果相邻齿轮没有在旋转，则启动旋转
                    if (!adjacentGear.IsRotating)
                    {
                        RotationDirection oppositeDir = CurrentDirection == RotationDirection.Clockwise
                            ? RotationDirection.CounterClockwise
                            : RotationDirection.Clockwise;

                        adjacentGear.StartRotation(oppositeDir);
                        //Debug.Log($"[GearControl] {gameObject.name} - 带动齿轮 {adjacentGear.gameObject.name} | 方向: {oppositeDir}");
                    }
                    // 如果方向不同，更新旋转方向
                    else if (adjacentGear.CurrentDirection == CurrentDirection)
                    {
                        RotationDirection oppositeDir = CurrentDirection == RotationDirection.Clockwise
                            ? RotationDirection.CounterClockwise
                            : RotationDirection.Clockwise;

                        adjacentGear.CurrentDirection = oppositeDir;
                    }
                }
            }
        }

        // 通知相邻齿轮检查是否还能保持动力
        private void NotifyAdjacentGearsToCheckPower()
        {
            Vector3[] detectDirections = new Vector3[]
            {
                Vector3.forward, Vector3.back, Vector3.right, Vector3.left
            };

            foreach (var dir in detectDirections)
            {
                Vector3 detectPos = transform.position + dir * detectRange;
                detectPos += dir * 0.05f;

                Collider[] hitColliders = Physics.OverlapBox(
                    detectPos,
                    new Vector3(detectRange * 0.5f, yTolerance, detectRange * 0.5f),
                    Quaternion.identity,
                    LayerMask.GetMask("Gear")
                );

                foreach (var hitCol in hitColliders)
                {
                    if (hitCol.gameObject == gameObject)
                        continue;

                    GearControl adjacentGear = hitCol.GetComponent<GearControl>();
                    if (adjacentGear != null && adjacentGear.IsRotating)
                    {
                        adjacentGear.CheckIfStillInPowerChain();
                    }
                }
            }
        }

        // 检查是否仍然在动力链中（是否有其他动力源）
        private void CheckIfStillInPowerChain()
        {
            // 原始动力齿轮始终在动力链中
            if (HasOriginalPower)
            {
                IsInPowerChain = true;
                return;
            }

            // 检查是否有其他在动力链中的相邻齿轮提供动力
            bool hasValidPowerSource = false;
            Vector3[] detectDirections = new Vector3[]
            {
                Vector3.forward, Vector3.back, Vector3.right, Vector3.left
            };

            foreach (var dir in detectDirections)
            {
                Vector3 detectPos = transform.position + dir * detectRange;
                detectPos += dir * 0.05f;

                Collider[] hitColliders = Physics.OverlapBox(
                    detectPos,
                    new Vector3(detectRange * 0.5f, yTolerance, detectRange * 0.5f),
                    Quaternion.identity,
                    LayerMask.GetMask("Gear")
                );

                foreach (var hitCol in hitColliders)
                {
                    if (hitCol.gameObject == gameObject)
                        continue;

                    GearControl adjacentGear = hitCol.GetComponent<GearControl>();
                    if (adjacentGear != null && adjacentGear.IsRotating && adjacentGear.IsInPowerChain)
                    {
                        hasValidPowerSource = true;
                        break;
                    }
                }
                if (hasValidPowerSource) break;
            }

            // 如果没有有效动力源，停止旋转
            if (!hasValidPowerSource)
            {
                StopRotation();
            }
        }
        #endregion

        #region Gizmos可视化
        private void OnDrawGizmos()
        {
            // 绘制自身碰撞体（绿色）
            BoxCollider selfCollider = GetComponent<BoxCollider>();
            if (selfCollider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position + selfCollider.center, selfCollider.size);
            }

            // 绘制检测盒（红色）
            Gizmos.color = Color.red;
            Vector3[] detectDirections = new Vector3[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            foreach (var dir in detectDirections)
            {
                Vector3 detectPos = transform.position + dir * detectRange;
                detectPos += dir * 0.05f; // 与自身碰撞体错开
                Gizmos.DrawWireCube(detectPos, new Vector3(detectRange, yTolerance * 2, detectRange));
            }

            // 绘制动力状态指示（黄色：原始动力，蓝色：临时动力，红色：不在动力链）
            if (HasOriginalPower)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position + Vector3.up * 0.3f, 0.1f);
            }
            else if (HasTempPower && IsInPowerChain)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(transform.position + Vector3.up * 0.3f, 0.1f);
            }
            else if (!IsInPowerChain)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position + Vector3.up * 0.3f, 0.1f);
            }
        }
        #endregion
    }
}
