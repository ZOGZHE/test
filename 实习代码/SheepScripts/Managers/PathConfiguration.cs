using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    /// <summary>
    /// 路径配置管理器，单例模式，管理羊的移动路径关键点和退出区域
    /// </summary>
    public class PathConfiguration : MonoBehaviour
    {
        /// <summary>
        /// 单例实例，全局唯一访问点
        /// </summary>
        public static PathConfiguration Instance { get; private set; }

        [Header("羊毛分离点配置")]
        [Tooltip("羊毛分离的位置点（羊在此处脱毛）")]
        [SerializeField] private Transform woolSeparationPoint;


        [Header("退出点配置")]
        [Tooltip("左侧出口点数组（羊从左侧离开场景的位置）")]
        [SerializeField] private Transform[] leftExitPoints;
        [Tooltip("右侧出口点数组（羊从右侧离开场景的位置）")]
        [SerializeField] private Transform[] rightExitPoints;


        [Header("视觉化配置")]
        [Tooltip("运行时是否显示Gizmos辅助线")]
        [SerializeField] private bool showGizmosInGame = false;
        [Tooltip("分离点的Gizmos颜色")]
        [SerializeField] private Color separationPointColor = Color.yellow;
        [Tooltip("左侧出口点的Gizmos颜色")]
        [SerializeField] private Color leftExitColor = Color.red;
        [Tooltip("右侧出口点的Gizmos颜色")]
        [SerializeField] private Color rightExitColor = Color.blue;
        [Tooltip("传送带入口的Gizmos颜色")]
        [SerializeField] private Color conveyorEntryColor = Color.green;

        // 缓存位置数据，减少Transform访问开销
        private Vector3 cachedSeparationPoint;
        private List<Vector3> cachedLeftExits = new List<Vector3>();
        private List<Vector3> cachedRightExits = new List<Vector3>();
        private Vector3 cachedConveyorEntry;

        /// <summary>
        /// 初始化单例和路径配置
        /// </summary>
        private void Awake()
        {
            // 单例实现：确保全局唯一，重复实例直接销毁
            if (Instance == null)
            {
                Instance = this;
                InitializePathConfiguration();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化路径配置：创建缺失路径点，缓存路径数据
        /// </summary>
        private void InitializePathConfiguration()
        {
            CreateMissingPathPoints();
            UpdateCachedPaths();
        }

        /// <summary>
        /// 创建缺失的路径点，避免空引用错误
        /// </summary>
        private void CreateMissingPathPoints()
        {
            // 羊毛分离点不存在时，自动创建并设为子对象
            if (woolSeparationPoint == null)
            {
                GameObject separationGO = new GameObject("WoolSeparationPoint");
                separationGO.transform.parent = transform;
                woolSeparationPoint = separationGO.transform;
                Debug.Log("[PathConfiguration] 创建了羊毛分离点");
            }
        }

        /// <summary>
        /// 更新缓存的路径数据，减少Transform频繁访问的性能消耗
        /// </summary>
        private void UpdateCachedPaths()
        {
            // 缓存羊毛分离点位置
            if (woolSeparationPoint != null)
            {
                cachedSeparationPoint = woolSeparationPoint.position;
            }

            // 缓存左侧退出点列表
            cachedLeftExits.Clear();
            if (leftExitPoints != null)
            {
                foreach (var exit in leftExitPoints)
                {
                    if (exit != null)
                    {
                        cachedLeftExits.Add(exit.position);
                    }
                }
            }

            // 缓存右侧退出点列表
            cachedRightExits.Clear();
            if (rightExitPoints != null)
            {
                foreach (var exit in rightExitPoints)
                {
                    if (exit != null)
                    {
                        cachedRightExits.Add(exit.position);
                    }
                }
            }
        }

        #region 公共接口

        /// <summary>
        /// 获取羊毛分离点的世界坐标
        /// </summary>
        /// <returns>羊毛分离点位置</returns>
        public Vector3 GetWoolSeparationPoint()
        {
            return cachedSeparationPoint;
        }

        /// <summary>
        /// 获取随机的左侧出口点世界坐标
        /// </summary>
        /// <returns>随机左侧退出点位置</returns>
        public Vector3 GetRandomLeftExitPoint()
        {
            if (cachedLeftExits.Count == 0)
            {
                return Vector3.left * 5f; // 无配置时返回默认左侧位置
            }

            int randomIndex = Random.Range(0, cachedLeftExits.Count);
            return cachedLeftExits[randomIndex];
        }

        /// <summary>
        /// 获取随机的右侧出口点世界坐标
        /// </summary>
        /// <returns>随机右侧退出点位置</returns>
        public Vector3 GetRandomRightExitPoint()
        {
            if (cachedRightExits.Count == 0)
            {
                return Vector3.right * 5f; // 无配置时返回默认右侧位置
            }

            int randomIndex = Random.Range(0, cachedRightExits.Count);
            return cachedRightExits[randomIndex];
        }

        /// <summary>
        /// 根据方向获取对应随机退出点的世界坐标
        /// </summary>
        /// <param name="isLeft">是否为左侧出口</param>
        /// <returns>对应方向的随机退出点位置</returns>
        public Vector3 GetExitPoint(bool isLeft)
        {
            return isLeft ? GetRandomLeftExitPoint() : GetRandomRightExitPoint();
        }

        /// <summary>
        /// 获取传送带入口的世界坐标
        /// </summary>
        /// <returns>传送带入口位置</returns>
        public Vector3 GetConveyorBeltEntryPoint()
        {
            return cachedConveyorEntry;
        }

        /// <summary>
        /// 获取离参考位置最近的退出点世界坐标
        /// </summary>
        /// <param name="fromPosition">参考位置</param>
        /// <returns>最近的退出点位置</returns>
        public Vector3 GetNearestExitPoint(Vector3 fromPosition)
        {
            Vector3 leftExit = GetRandomLeftExitPoint();
            Vector3 rightExit = GetRandomRightExitPoint();

            float leftDistance = Vector3.Distance(fromPosition, leftExit);
            float rightDistance = Vector3.Distance(fromPosition, rightExit);

            return leftDistance < rightDistance ? leftExit : rightExit;
        }

        #endregion

        #region 编辑器工具和调试

        /// <summary>
        /// 编辑器右键菜单：刷新路径配置（创建缺失点+更新缓存）
        /// </summary>
        [ContextMenu("刷新路径配置")]
        public void RefreshPathConfiguration()
        {
            CreateMissingPathPoints();
            UpdateCachedPaths();
            Debug.Log("[PathConfiguration] 路径配置已刷新");
        }

        /// <summary>
        /// 编辑器右键菜单：重新生成退出点（清理旧点+重置引用+更新缓存）
        /// </summary>
        [ContextMenu("重新生成退出点")]
        public void RegenerateExitPoints()
        {
            // 清理现有左侧退出点（编辑器模式下立即销毁）
            if (leftExitPoints != null)
            {
                foreach (var exit in leftExitPoints)
                {
                    if (exit != null && Application.isEditor)
                    {
                        DestroyImmediate(exit.gameObject);
                    }
                }
            }

            // 清理现有右侧退出点（编辑器模式下立即销毁）
            if (rightExitPoints != null)
            {
                foreach (var exit in rightExitPoints)
                {
                    if (exit != null && Application.isEditor)
                    {
                        DestroyImmediate(exit.gameObject);
                    }
                }
            }

            // 重置退出点数组引用
            leftExitPoints = null;
            rightExitPoints = null;

            // 更新缓存数据
            UpdateCachedPaths();
        }

        /// <summary>
        /// 编辑器字段验证：Inspector修改时触发，运行时不执行
        /// </summary>
        private void OnValidate()
        {
            if (Application.isPlaying) return;
        }

        /// <summary>
        /// 绘制Gizmos辅助线：可视化路径点，支持运行时开关
        /// </summary>
        private void OnDrawGizmos()
        {
            // 运行时是否显示，由配置决定
            if (!showGizmosInGame && Application.isPlaying) return;

            // 绘制羊毛分离点（线框球+向上指示线）
            if (woolSeparationPoint != null)
            {
                Gizmos.color = separationPointColor;
                Gizmos.DrawWireSphere(woolSeparationPoint.position, 0.5f);
                Gizmos.DrawRay(woolSeparationPoint.position, Vector3.up);
            }

            // 绘制左侧退出点（线框立方体）
            if (leftExitPoints != null)
            {
                Gizmos.color = leftExitColor;
                foreach (var exit in leftExitPoints)
                {
                    if (exit != null)
                    {
                        Gizmos.DrawWireCube(exit.position, Vector3.one * 0.8f);
                    }
                }
            }

            // 绘制右侧退出点（线框立方体）
            if (rightExitPoints != null)
            {
                Gizmos.color = rightExitColor;
                foreach (var exit in rightExitPoints)
                {
                    if (exit != null)
                    {
                        Gizmos.DrawWireCube(exit.position, Vector3.one * 0.8f);
                    }
                }
            }
        }

        #endregion
    }
}