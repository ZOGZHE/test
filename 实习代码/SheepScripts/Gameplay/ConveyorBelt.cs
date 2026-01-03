using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace WoolyPath
{
    // 传送带核心类：管理羊毛在传送带上的移动、容量控制及与收集器的交互

    public class ConveyorBelt : MonoBehaviour
    {
        // 单例实例，全局访问传送带
        public static ConveyorBelt Instance { get; private set; }

        [Header("关卡数据关联")]
        [SerializeField] private LevelData[] levels;
        [SerializeField] public LevelData currentLevelData;
        private List<Transform> dynamicPathPoints = new List<Transform>();

        [Header("箭头设置")]
        [SerializeField] private GameObject arrowPrefab; // 箭头预制体
        [SerializeField] private int arrowsCount = 10; // 箭头数量
        [SerializeField] private float arrowSpacing = 0.5f; // 箭头间距
        [SerializeField] private float RotationSpeed = 1f; // 速度
        private List<Transform> arrows = new List<Transform>(); // 存储所有箭头
        private List<float> arrowProgress = new List<float>(); // 每个箭头的进度

        [Header("传送带设置")]
        [SerializeField] public Transform[] beltPath; // 传送带的路径点数组
        [SerializeField] private float beltSpeed = 2f; // 传送带移动速度mutiple
        [SerializeField] public int maxCapacity = 10; // 传送带最大容纳羊毛数

        [Header("视觉设置")]
        [SerializeField] private LineRenderer beltRenderer; // 绘制传送带的线渲染器
        [SerializeField] private float beltWidth = 0.5f; // 传送带宽度

        [Header("入口点")]
        public Transform entryPoint; // 羊毛进入传送带的入口点
        public Transform collectPoint; // 羊毛进入传送带的入口点

        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true; // 是否显示调试 gizmos
        [SerializeField] private UnityEngine.Color debugColor = UnityEngine.Color.cyan; // 调试绘制颜色

        private Queue<WoolObject> woolQueue = new Queue<WoolObject>(); // 羊毛队列（备用顺序管理）
        public List<WoolObject> woolsOnBelt = new List<WoolObject>(); // 当前在传送带上的羊毛列表
        public Dictionary<WoolObject, float> woolProgress = new Dictionary<WoolObject, float>(); // 羊毛在传送带上的进度（0-1）

        private void Awake()
        {
            // 单例模式：确保全局唯一实例
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); //跨场景保留实例，避免关卡切换后丢失
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            var currentData = GetCurrentLevelData();
            if (currentData != null)
            {
                InitializeFromLevelData(currentData.conveyorPath);
            }
            InitializeBelt();
           

        }
        private void Update()
        {
      
            CheckForCollections();
            UpdateWoolMovement();
            // 实时更新传送带容量（每帧检查，确保与关卡数据同步）
            UpdateConveyorSettings();
            UpdateArrowsMovement(); // 更新箭头移动
        }

        private void InitializeArrows()
        {
            // 清理现有的箭头
            foreach (Transform arrow in arrows)
            {
                if (arrow != null)
                    Destroy(arrow.gameObject);
            }
            arrows.Clear();
            arrowProgress.Clear();

            // 新增：路径点不足的日志
            if (beltPath == null || beltPath.Length < 2)
            {
                Debug.LogError($"[传送带] 箭头生成失败：beltPath为空或路径点数量不足（当前数量：{beltPath?.Length ?? 0}）");
                return;
            }

            // 计算总长度和箭头间距
            float totalLength = GetTotalBeltLength();
            int actualArrowCount = Mathf.Min(arrowsCount, Mathf.FloorToInt(totalLength / arrowSpacing));

            // 新增：箭头数量不足的日志
            if (actualArrowCount <= 0)
            {
                Debug.LogError($"[传送带] 箭头生成失败：路径总长度不足（总长度：{totalLength}，间距：{arrowSpacing}，计算后箭头数：{actualArrowCount}）");
                return;
            }

            // 创建箭头
            for (int i = 0; i < actualArrowCount; i++)
            {
                //Debug.LogError("创建箭头");
                GameObject arrow = Instantiate(arrowPrefab, transform);
                arrow.name = $"ConveyorArrow_{i}";

                // 计算初始进度（均匀分布）
                float progress = (float)i / actualArrowCount;

                // 设置箭头位置和旋转
                arrow.transform.position = GetPositionOnBelt(progress);
                Vector3 direction = GetDirectionOnBelt(progress);
                if (direction != Vector3.zero)
                {
                    arrow.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                }

                arrows.Add(arrow.transform);
                arrowProgress.Add(progress);
            }

            Debug.Log($"[传送带] 初始化了 {arrows.Count} 个箭头");
        }
        // 更新箭头移动
        private void UpdateArrowsMovement()
        {
            if (arrows.Count == 0 || beltPath == null || beltPath.Length < 2) return;

            float progressStep = (beltSpeed / GetTotalBeltLength()) * Time.deltaTime * RotationSpeed;

            for (int i = 0; i < arrows.Count; i++)
            {
                if (arrows[i] == null) continue;

                // 更新进度
                arrowProgress[i] += progressStep;

                // 循环处理：当进度超过1时回到0
                if (arrowProgress[i] >= 1f)
                {
                    arrowProgress[i] -= 1f;
                }

                // 设置位置和旋转
                arrows[i].position = GetPositionOnBelt(arrowProgress[i]);
                Vector3 direction = GetDirectionOnBelt(arrowProgress[i]);
                if (direction != Vector3.zero)
                {
                    arrows[i].rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                }
            }
        }
        // 刷新箭头（当路径改变时调用）
        public void RefreshArrows()
        {
            InitializeArrows();
        }

        // 添加获取当前关卡数据的方法
        public LevelData GetCurrentLevelData()
        {
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevelData != null)
            {
                return LevelManager.Instance.CurrentLevelData;
            }
            return null;
        }
        //实时更新传送带设置
        private void UpdateConveyorSettings()
        {
            var currentData = GetCurrentLevelData();
            if (currentData != null)
            {
                // 实时更新容量
                if (maxCapacity != currentData.conveyorCapacity)
                {
                    maxCapacity = currentData.conveyorCapacity;
                    // Debug.Log($"[传送带] 容量实时更新: {maxCapacity}");
                }
                if(beltSpeed!=currentData.conveyorSpeed)
                {
                    beltSpeed = currentData.conveyorSpeed;
                }
            }
        }
        public void RefreshFromLevelData()
        {
            var currentData = GetCurrentLevelData();
            if (currentData != null)
            {
                currentLevelData = currentData;
                InitializeFromLevelData(currentData.conveyorPath);
                Debug.Log($"[传送带] 已刷新配置 - 容量: {currentData.conveyorCapacity}");
            }
        }
        // 添加一个公共方法来处理关卡切换
        public void OnLevelChanged()
        {
            var currentData = GetCurrentLevelData();
            if (currentData != null)
            {
                InitializeFromLevelData(currentData.conveyorPath);
                Debug.Log($"[传送带] 关卡切换，重新初始化箭头和路径");
            }
        }

        // 从ConveyorPathData初始化传送带
        private void InitializeFromLevelData(ConveyorPathData pathData)
        {    
                // 1. 清理旧路径点
             ClearDynamicPoints();
          
            // 2. 初始化路径点
            if (pathData.pathPoints != null && pathData.pathPoints.Length >= 2)
            {
                beltPath = new Transform[pathData.pathPoints.Length];
                for (int i = 0; i < pathData.pathPoints.Length; i++)
                {
                    // 创建空物体作为路径点载体
                    Transform point = new GameObject($"Path_{i}").transform;
                    point.position = pathData.pathPoints[i];
                    point.parent = transform; // 父级设为传送带，方便管理
                    beltPath[i] = point;    
                    dynamicPathPoints.Add(point);
                }
            }

            // 3. 初始化入口点
            if (entryPoint == null)
            {
                entryPoint = new GameObject("EntryPoint").transform;
                entryPoint.parent = transform;
                dynamicPathPoints.Add(entryPoint);
            }
            entryPoint.position = pathData.entry;

            // 4. 初始化收集点
            if (collectPoint == null)
            {
                collectPoint = new GameObject("CollectPoint").transform;
                collectPoint.parent = transform;
                dynamicPathPoints.Add(collectPoint);
            }
            collectPoint.position = pathData.collect;
            // 刷新箭头
            RefreshArrows();
            Debug.Log($"从LevelData加载传送带配置：{pathData.pathPoints.Length}个路径点");
        }

        // 清理动态创建的路径点
        private void ClearDynamicPoints()
        {
            foreach (var point in dynamicPathPoints)
            {
                if (point != null)
                    Destroy(point.gameObject);
            }
            dynamicPathPoints.Clear();
            beltPath = null;
            // 同时清理箭头
            foreach (Transform arrow in arrows)
            {
                if (arrow != null)
                    Destroy(arrow.gameObject);
            }
            arrows.Clear();
            arrowProgress.Clear();
        }


        //初始化轨道
        private void InitializeBelt()
        {
            // 验证传送带路径有效性（至少2个点）
            if (beltPath == null || beltPath.Length < 2)
            {
                Debug.LogError("[传送带] 传送带路径必须至少有2个点！");
                return;
            }

            // 自动设置入口点为路径起点（若未指定）
            if (entryPoint == null && beltPath.Length > 0)
            {
                entryPoint = beltPath[0];
            }
        }

        #region 收集逻辑
        // 使用实时容量检查
        public bool TryAddWool(WoolObject wool)
        {
            if (wool == null) return false;

            // 实时获取当前容量设置
            var currentData = GetCurrentLevelData();
            int currentMaxCapacity = maxCapacity;
            if (currentData != null)
            {
                currentMaxCapacity = currentData.conveyorCapacity;
            }

            CollectorPlate[] collectorPlates = FindObjectsOfType<CollectorPlate>();
            bool hasValidCollector = Array.Exists(collectorPlates, c => c != null && c.WoolCanBeCollect(wool));
            wool.SetOnBelt(true);

            // 使用实时容量进行检查
            if (woolsOnBelt.Count >= currentMaxCapacity)
            {
                if (hasValidCollector)
                {
                    Debug.LogWarning($"[传送带] 已达最大容量({currentMaxCapacity})，但{wool.Color}羊毛可消除，允许加入（当前数量：{woolsOnBelt.Count + 1}）");
                }
                else
                {
                    Debug.LogWarning($"[传送带] 已达最大容量({currentMaxCapacity})，{wool.Color}羊毛不可消除，添加失败");
                    GameEvents.TriggerConveyorBeltFull();
                    return false;
                }
            }

            woolsOnBelt.Add(wool);
            woolProgress[wool] = 0f;
            if (entryPoint != null) wool.transform.position = entryPoint.position;

            Debug.Log($"[传送带] 已添加{wool.Color}羊毛到传送带上（当前数量/最大容量：{woolsOnBelt.Count}/{currentMaxCapacity}）");
            return true;
        }

        // 检查传送带上的羊毛是否可被收集器收集
        //private void CheckForCollections()
        //{
        //    CollectorPlate[] collectors = FindObjectsOfType<CollectorPlate>();

        //    foreach (WoolObject wool in woolsOnBelt.ToList())
        //    {
        //        if (wool == null || wool.IsCollected) continue;

        //        foreach (CollectorPlate collector in collectors)
        //        {
        //            if (collector == null || collector.IsComplete()) continue;

        //            if (collector.TryCollectWool(wool))
        //            {
        //                break; // 收集成功则停止检查其他收集器
        //            }
        //        }
        //    }
        //}
        private void CheckForCollections()
        {
            CollectorPlate[] collectors = FindObjectsOfType<CollectorPlate>();

            foreach (WoolObject wool in woolsOnBelt.ToList())
            {
                if (wool == null || wool.IsCollected) continue;

                foreach (CollectorPlate collector in collectors)
                {
                    if (collector == null || collector.IsComplete()) continue;

                    // 检查羊毛是否可被收集
                    if (collector.WoolCanBeCollect(wool))
                    {
                        // 从传送带移除羊毛
                        RemoveWool(wool);
                        collector.WhenAddWool();
                        // 启动飞行到收集器的协程
                        StartCoroutine(FlyWoolToCollector(wool, collector));
                        
                      
                        break; // 收集成功则停止检查其他收集器
                    }
                }
            }
        }

        private IEnumerator FlyWoolToCollector(WoolObject wool, CollectorPlate collector)
        {
            if (wool == null || collector == null) yield break;

            // 等待Launch内部的飞行动画完全执行完毕
            yield return StartCoroutine(wool.Launch(wool, wool.transform.position, collector.transform.position));

            //// 飞行完成后，再执行收集逻辑
            //collector.DirectCollectWool(wool);
            // 飞行结束后，再次确认状态（双重保险）
            if (collector.WoolCanBeCollect(wool))
            {
                collector.DirectCollectWool(wool);
                if(AudioManager.Instance!=null)
                {
                    AudioManager.Instance.PlayMusic("4");
                }
                
            }
            else
            {
                // 放回传送带
                wool.SetOnBelt(true);
                ConveyorBelt.Instance.woolsOnBelt.Add(wool);
                ConveyorBelt.Instance.woolProgress[wool] = UnityEngine.Random.Range(0.01f, 0.99f);
            }
        }
        //private IEnumerator FlyWoolToCollector(WoolObject wool, CollectorPlate collector)
        //{
        //    if (wool == null || collector == null) yield break;

        //    Vector3 startPosition = wool.transform.position;
        //    Vector3 targetPosition = collector.GetCollectionPoint();
        //    float flightDuration = 0.5f;
        //    float timer = 0f;

        //    Rigidbody woolRb = wool.GetComponent<Rigidbody>();
        //    if (woolRb != null) woolRb.isKinematic = true;

        //    // 新增：飞行过程中每帧检查收集器状态
        //    while (timer < flightDuration)
        //    {
        //        timer += Time.deltaTime;
        //        float t = Mathf.SmoothStep(0f, 1f, timer / flightDuration);

        //        // 关键：飞行中实时检查收集器是否仍可收集
        //        if (!collector.WoolCanBeCollect(wool))
        //        {
        //            Debug.LogWarning($"[飞行中断] {wool.Color}羊毛飞向{collector.GetTargetColor()}收集器，收集器已不可用");

        //            // 中断飞行，将羊毛放回传送带
        //            wool.SetOnBelt(true);
        //            if (!ConveyorBelt.Instance.woolsOnBelt.Contains(wool))
        //            {
        //                ConveyorBelt.Instance.woolsOnBelt.Add(wool);
        //                ConveyorBelt.Instance.woolProgress[wool] = 0f; // 重置进度（或根据需求调整）
        //            }
        //            if (woolRb != null) woolRb.isKinematic = false;
        //            yield break; // 终止协程
        //        }

        //        // 原有飞行逻辑
        //        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
        //        currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.5f;
        //        wool.transform.position = currentPos;
        //        wool.transform.Rotate(Vector3.up, 360f * Time.deltaTime);

        //        yield return null;
        //    }

        //    // 飞行结束后，再次确认状态（双重保险）
        //    if (collector.WoolCanBeCollect(wool))
        //    {
        //        collector.DirectCollectWool(wool);
        //    }
        //    else
        //    {
        //        // 放回传送带
        //        wool.SetOnBelt(true);
        //        ConveyorBelt.Instance.woolsOnBelt.Add(wool);
        //        ConveyorBelt.Instance.woolProgress[wool] = UnityEngine.Random.Range(0.01f, 0.99f);
        //    }
        //}









        // 从传送带移除羊毛（清理数据）
        public void RemoveWool(WoolObject wool)
        {
            if (wool == null) return;

            woolsOnBelt.Remove(wool);
            if (woolProgress.ContainsKey(wool))
            {
                woolProgress.Remove(wool);
            }
        }
        #endregion

        #region 传送带逻辑
        // 逐帧更新所有羊毛在传送带上的移动逻辑
        private void UpdateWoolMovement()
        {
            if (beltPath == null || beltPath.Length < 2) return;

            foreach (WoolObject wool in woolsOnBelt.ToList())
            {
                if (wool == null)
                {
                    woolsOnBelt.Remove(wool);
                    if (woolProgress.ContainsKey(wool)) woolProgress.Remove(wool);
                    continue;
                }

                float currentProgress = woolProgress[wool];
                currentProgress += (beltSpeed / GetTotalBeltLength()) * Time.deltaTime;

                if (currentProgress >= 1f)
                {
                    currentProgress = 0f; // 直接重置进度为0，而不是标记移除

                }

                woolProgress[wool] = currentProgress;
                wool.transform.position = GetPositionOnBelt(currentProgress);
                Vector3 direction = GetDirectionOnBelt(currentProgress);
                if (direction != Vector3.zero) wool.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
        // 根据进度计算羊毛在传送带上的位置（沿路径插值）
        private Vector3 GetPositionOnBelt(float progress)
        {
            if (beltPath == null || beltPath.Length < 2) return Vector3.zero;

            progress = Mathf.Clamp01(progress);
            float totalLength = GetTotalBeltLength();
            float targetDistance = progress * totalLength;
            float currentDistance = 0f;

            // 遍历路径段，找到进度对应的位置
            for (int i = 0; i < beltPath.Length; i++)
            {
                int nextIndex = (i + 1) % beltPath.Length;
                float segmentLength = Vector3.Distance(beltPath[i].position, beltPath[nextIndex].position);

                if (currentDistance + segmentLength >= targetDistance)
                {
                    float segmentProgress = (targetDistance - currentDistance) / segmentLength;
                    return Vector3.Lerp(beltPath[i].position, beltPath[nextIndex].position, segmentProgress);
                }
                currentDistance += segmentLength;
            }

            return beltPath[beltPath.Length - 1].position;
        }

        // 根据进度计算羊毛在传送带上的移动方向
        private Vector3 GetDirectionOnBelt(float progress)
        {
            if (beltPath == null || beltPath.Length < 2) return Vector3.forward;

            progress = Mathf.Clamp01(progress);
            float totalLength = GetTotalBeltLength();
            float targetDistance = progress * totalLength;
            float currentDistance = 0f;

            // 遍历路径段，找到进度对应的方向
            for (int i = 0; i < beltPath.Length; i++)
            {
                int nextIndex = (i + 1) % beltPath.Length;
                float segmentLength = Vector3.Distance(beltPath[i].position, beltPath[nextIndex].position);

                if (currentDistance + segmentLength >= targetDistance)
                {
                    return (beltPath[nextIndex].position - beltPath[i].position).normalized;
                }
                currentDistance += segmentLength;
            }

            return Vector3.forward;
        }

        // 计算传送带的总长度（所有路径段的和）
        private float GetTotalBeltLength()
        {
            if (beltPath == null || beltPath.Length < 2) return 1f;

            float totalLength = 0f;
            for (int i = 0; i < beltPath.Length; i++)
            {
                int nextIndex = (i + 1) % beltPath.Length;
                totalLength += Vector3.Distance(beltPath[i].position, beltPath[nextIndex].position);
            }
            return totalLength;
        }
        #endregion


        #region Public Accessors
        /// <summary>
        /// 清除传送带上所有羊毛（数据+对象，可选）
        /// </summary>
        public void ClearAllWools()
        {
            // 1. 边界检查：无羊毛时直接返回
            if (woolsOnBelt.Count == 0 && woolProgress.Count == 0)
            {
                //Debug.Log("[传送带] 传送带上当前没有羊毛可清除！");
                return;
            }

            // 2. 记录清除数量（用于日志）
            int clearedCount = woolsOnBelt.Count;

            // 3. 遍历羊毛列表的副本（避免遍历中修改集合导致异常）
            foreach (WoolObject wool in woolsOnBelt.ToList())
            {
                if (wool == null) continue;

                // 3.1 标记羊毛为“已收集”（避免与收集器逻辑冲突，根据业务需求调整）
                if (!wool.isCollected)
                {
                    wool.isCollected = true;
                }

                // 3.2 调用现有方法移除羊毛数据（列表+进度字典）
                RemoveWool(wool);

                // 3.3 （可选）销毁羊毛游戏对象（若不需要复用羊毛，建议销毁避免内存残留）
                if (wool.gameObject != null)
                {
                    Destroy(wool.gameObject);
                }
            }

            // 4. 双重保险：确保进度字典清空（防止极端情况下数据残留）
            woolProgress.Clear();

        }
        // 获取传送带入口点位置
        public Vector3 GetEntryPoint() => entryPoint != null ? entryPoint.position : Vector3.zero;
        public Vector3 GetCollectPoint() => collectPoint != null ? collectPoint.position : Vector3.zero;

        // 获取当前容纳的羊毛数
        public int GetCurrentCapacity() => woolsOnBelt.Count;
        // 获取最大容量
        public int GetMaxCapacity() => maxCapacity;
        // 检查是否已满
        public bool IsFull() => woolsOnBelt.Count >= maxCapacity;
        // 获取填充百分比
        public float GetFillPercentage() => (float)woolsOnBelt.Count / maxCapacity;



        #endregion
        #region Gizmo
// 场景视图中绘制传送带调试信息（未选中时）
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || beltPath == null) return;

            // 绘制路径线与路径点
            Gizmos.color = debugColor;
            for (int i = 0; i < beltPath.Length; i++)
            {
                if (beltPath[i] == null) continue;
                int nextIndex = (i + 1) % beltPath.Length;
                if (beltPath[nextIndex] != null) Gizmos.DrawLine(beltPath[i].position, beltPath[nextIndex].position);
                Gizmos.DrawWireSphere(beltPath[i].position, 0.2f);
            }

            // 绘制入口点
            if (entryPoint != null)
            {
                Gizmos.color = UnityEngine.Color.green;
                Gizmos.DrawWireSphere(entryPoint.position, 0.3f);
            }
        }

        // 选中传送带时绘制更详细的调试信息（宽度等）
        private void OnDrawGizmosSelected()
        {
            if (beltPath == null) return;

            Gizmos.color = UnityEngine.Color.yellow;
            // 绘制传送带宽度的辅助线
            for (int i = 0; i < beltPath.Length; i++)
            {
                if (beltPath[i] == null) continue;
                int nextIndex = (i + 1) % beltPath.Length;
                if (beltPath[nextIndex] == null) continue;

                Vector3 direction = (beltPath[nextIndex].position - beltPath[i].position).normalized;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up) * beltWidth * 0.5f;

                Gizmos.DrawLine(beltPath[i].position + perpendicular, beltPath[nextIndex].position + perpendicular);
                Gizmos.DrawLine(beltPath[i].position - perpendicular, beltPath[nextIndex].position - perpendicular);
            }
        }
        #endregion
        
    }
}