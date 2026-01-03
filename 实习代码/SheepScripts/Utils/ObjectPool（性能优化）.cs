using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    /// <summary>
    /// 通用对象池，用于复用游戏对象
    /// 泛型版本，适用于带有特定组件的对象
    /// </summary>
    /// <typeparam name="T">要池化的组件类型（必须继承自MonoBehaviour）</typeparam>
    public class ObjectPool<T> where T : MonoBehaviour
    {
        // 存储闲置对象的队列（先进先出，保证复用顺序）
        private Queue<T> pool = new Queue<T>();
        // 池化对象的预制体（用于创建新对象）
        private T prefab;
        // 池化对象的父节点（用于场景层级管理）
        private Transform parent;
        // 池的最大容量（防止对象过多导致内存溢出）
        private int maxSize;
        // 当前池中对象的总数量（激活+闲置）
        private int currentSize = 0;

        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="prefab">要池化的对象预制体</param>
        /// <param name="parent">对象的父节点（可选）</param>
        /// <param name="initialSize">初始创建的对象数量</param>
        /// <param name="maxSize">池的最大容量</param>
        public ObjectPool(T prefab, Transform parent = null, int initialSize = 5, int maxSize = 20)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;

            // 预填充对象池（提前创建初始数量的对象）
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 从池中获取一个对象
        /// </summary>
        /// <returns>激活状态的对象组件</returns>
        public T Get()
        {
            // 如果池中有闲置对象，直接复用
            if (pool.Count > 0)
            {
                T obj = pool.Dequeue();
                obj.gameObject.SetActive(true); // 激活对象
                return obj;
            }

            // 如果池为空，创建新对象（会自动加入池并返回）
            return CreateNewObject();
        }

        /// <summary>
        /// 将对象归还到池中
        /// </summary>
        /// <param name="obj">要归还的对象组件</param>
        public void Return(T obj)
        {
            if (obj == null) return; // 防止空引用错误

            obj.gameObject.SetActive(false); // 设为非激活状态（闲置）

            // 如果未达到最大容量，放回池中；否则销毁
            if (pool.Count < maxSize)
            {
                pool.Enqueue(obj);
            }
            else
            {
                // 超过最大容量，销毁多余对象
                Object.Destroy(obj.gameObject);
                currentSize--;
            }
        }

        /// <summary>
        /// 创建新对象并加入池中（私有方法，内部调用）
        /// </summary>
        /// <returns>新创建的对象组件</returns>
        private T CreateNewObject()
        {
            // 从预制体实例化新对象，并设置父节点
            GameObject newObj = Object.Instantiate(prefab.gameObject, parent);
            newObj.SetActive(false); // 初始设为非激活状态
            currentSize++; // 总数量加1

            // 获取对象上的目标组件
            T component = newObj.GetComponent<T>();
            pool.Enqueue(component); // 加入闲置队列

            return component;
        }

        /// <summary>
        /// 清空对象池（销毁所有对象）
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                T obj = pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }

            currentSize = 0; // 重置总数量
        }

        /// <summary>
        /// 当前池中的闲置对象数量
        /// </summary>
        public int AvailableCount => pool.Count;

        /// <summary>
        /// 池中对象的总数量（激活+闲置）
        /// </summary>
        public int TotalCount => currentSize;
    }

    /// <summary>
    /// 简单游戏对象池，适用于非MonoBehaviour对象
    /// 直接操作GameObject，不依赖特定组件
    /// </summary>
    public class GameObjectPool
    {
        // 存储闲置游戏对象的队列
        private Queue<GameObject> pool = new Queue<GameObject>();
        // 池化对象的预制体
        private GameObject prefab;
        // 池化对象的父节点
        private Transform parent;
        // 池的最大容量
        private int maxSize;
        // 当前池中对象的总数量
        private int currentSize = 0;

        /// <summary>
        /// 初始化游戏对象池
        /// </summary>
        /// <param name="prefab">要池化的游戏对象预制体</param>
        /// <param name="parent">对象的父节点（可选）</param>
        /// <param name="initialSize">初始创建的对象数量</param>
        /// <param name="maxSize">池的最大容量</param>
        public GameObjectPool(GameObject prefab, Transform parent = null, int initialSize = 5, int maxSize = 20)
        {
            this.prefab = prefab;
            this.parent = parent;
            this.maxSize = maxSize;

            // 预填充对象池
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 从池中获取一个游戏对象
        /// </summary>
        /// <returns>激活状态的游戏对象</returns>
        public GameObject Get()
        {
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }

            return CreateNewObject();
        }

        /// <summary>
        /// 将游戏对象归还到池中
        /// </summary>
        /// <param name="obj">要归还的游戏对象</param>
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);

            if (pool.Count < maxSize)
            {
                pool.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj);
                currentSize--;
            }
        }

        /// <summary>
        /// 创建新游戏对象并加入池中
        /// </summary>
        /// <returns>新创建的游戏对象</returns>
        private GameObject CreateNewObject()
        {
            GameObject newObj = Object.Instantiate(prefab, parent);
            newObj.SetActive(false);
            currentSize++;

            pool.Enqueue(newObj);
            return newObj;
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj);
                }
            }

            currentSize = 0;
        }

        /// <summary>
        /// 当前闲置对象数量
        /// </summary>
        public int AvailableCount => pool.Count;

        /// <summary>
        /// 总对象数量
        /// </summary>
        public int TotalCount => currentSize;
    }

    /// <summary>
    /// 对象池管理器，用于统一管理游戏中的常用对象池
    /// 单例模式，提供全局访问点
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        // 单例实例（全局唯一）
        public static ObjectPoolManager Instance { get; private set; }

        [Header("池设置")]
        [SerializeField] private int initialPoolSize = 10; // 初始池大小
        [SerializeField] private int maxPoolSize = 50;     // 最大池大小

        [Header("预制体")]
        [SerializeField] private WoolObject woolPrefab;         // 羊毛对象预制体
        [SerializeField] private GameObject collectEffectPrefab; // 收集特效预制体
        [SerializeField] private GameObject clickEffectPrefab;   // 点击特效预制体

        // 各个对象池的实例
        private ObjectPool<WoolObject> woolPool;
        private GameObjectPool collectEffectPool;
        private GameObjectPool clickEffectPool;

        private void Awake()
        {
            // 单例模式实现：确保全局只有一个实例
            if (Instance == null)
            {
                Instance = this;
                InitializePools(); // 初始化所有对象池
                DontDestroyOnLoad(gameObject); // 跨场景保留
            }
            else
            {
                Destroy(gameObject); // 销毁重复实例
            }
        }

        /// <summary>
        /// 初始化所有对象池
        /// </summary>
        private void InitializePools()
        {
            // 创建羊毛对象池（使用泛型池，因为WoolObject是MonoBehaviour）
            if (woolPrefab != null)
            {
                woolPool = new ObjectPool<WoolObject>(woolPrefab, transform, initialPoolSize, maxPoolSize);
            }

            // 创建收集特效池（使用普通游戏对象池）
            if (collectEffectPrefab != null)
            {
                collectEffectPool = new GameObjectPool(collectEffectPrefab, transform, initialPoolSize / 2, maxPoolSize / 2);
            }

            // 创建点击特效池
            if (clickEffectPrefab != null)
            {
                clickEffectPool = new GameObjectPool(clickEffectPrefab, transform, initialPoolSize / 2, maxPoolSize / 2);
            }

            Debug.Log("[ObjectPoolManager] 对象池初始化完成");
        }

        #region 公共API（供外部调用）

        /// <summary>
        /// 获取羊毛对象
        /// </summary>
        /// <returns>羊毛对象组件</returns>
        public WoolObject GetWool()
        {
            return woolPool?.Get(); // ?. 运算符防止空引用
        }

        /// <summary>
        /// 归还羊毛对象到池
        /// </summary>
        /// <param name="wool">要归还的羊毛对象</param>
        public void ReturnWool(WoolObject wool)
        {
            woolPool?.Return(wool);
        }

        /// <summary>
        /// 获取收集特效
        /// </summary>
        /// <returns>收集特效游戏对象</returns>
        public GameObject GetCollectEffect()
        {
            return collectEffectPool?.Get();
        }

        /// <summary>
        /// 归还收集特效到池
        /// </summary>
        /// <param name="effect">要归还的特效对象</param>
        public void ReturnCollectEffect(GameObject effect)
        {
            collectEffectPool?.Return(effect);
        }

        /// <summary>
        /// 获取点击特效
        /// </summary>
        /// <returns>点击特效游戏对象</returns>
        public GameObject GetClickEffect()
        {
            return clickEffectPool?.Get();
        }

        /// <summary>
        /// 归还点击特效到池
        /// </summary>
        /// <param name="effect">要归还的特效对象</param>
        public void ReturnClickEffect(GameObject effect)
        {
            clickEffectPool?.Return(effect);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            woolPool?.Clear();
            collectEffectPool?.Clear();
            clickEffectPool?.Clear();

            Debug.Log("[ObjectPoolManager] 所有对象池已清空");
        }

        #endregion

        #region 调试信息

        /// <summary>
        /// 存储对象池信息的结构体（用于调试）
        /// </summary>
        [System.Serializable]
        public struct PoolInfo
        {
            public string poolName; // 池名称
            public int available;   // 闲置数量
            public int total;       // 总数量
        }

        /// <summary>
        /// 获取所有对象池的状态信息
        /// </summary>
        /// <returns>对象池信息数组</returns>
        public PoolInfo[] GetPoolInfo()
        {
            List<PoolInfo> info = new List<PoolInfo>();

            if (woolPool != null)
            {
                info.Add(new PoolInfo
                {
                    poolName = "羊毛对象池",
                    available = woolPool.AvailableCount,
                    total = woolPool.TotalCount
                });
            }

            if (collectEffectPool != null)
            {
                info.Add(new PoolInfo
                {
                    poolName = "收集特效池",
                    available = collectEffectPool.AvailableCount,
                    total = collectEffectPool.TotalCount
                });
            }

            if (clickEffectPool != null)
            {
                info.Add(new PoolInfo
                {
                    poolName = "点击特效池",
                    available = clickEffectPool.AvailableCount,
                    total = clickEffectPool.TotalCount
                });
            }

            return info.ToArray();
        }

        #endregion

        private void OnDestroy()
        {
            ClearAllPools(); // 销毁时清空所有池，防止内存泄漏
        }
    }
}
