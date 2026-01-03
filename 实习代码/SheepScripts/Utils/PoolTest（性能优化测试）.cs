using UnityEngine;

namespace WoolyPath
{
    /// <summary>
    /// 简单的测试脚本，用于验证对象池功能是否正常工作
    /// </summary>
    public class PoolTest : MonoBehaviour
    {
        [Header("测试设置")] // 在Inspector面板中为以下字段添加标题
        [SerializeField] private GameObject testPrefab; // 要放入对象池的预制体（在Inspector中赋值）
        [SerializeField] private int testCount = 5; // 测试时要从池中获取的对象数量

        private GameObjectPool testPool; // 对象池实例，用于管理游戏对象

        private void Start()
        {
            // 测试游戏对象池功能
            if (testPrefab != null) // 检查是否已赋值预制体
            {
                Debug.Log("[PoolTest] 创建测试对象池...");
                // 初始化对象池：参数分别为（预制体，父节点，初始容量，最大容量）
                testPool = new GameObjectPool(testPrefab, transform, 3, 10);

                // 测试从池中获取对象
                for (int i = 0; i < testCount; i++)
                {
                    GameObject obj = testPool.Get(); // 从对象池获取一个对象
                    Debug.Log($"[PoolTest] 获取到对象 {i}：{obj.name}");

                    // 延迟一段时间后将对象归还池
                    StartCoroutine(ReturnAfterDelay(obj, 2f));
                }

                // 打印当前对象池状态
                Debug.Log($"[PoolTest] 对象池状态 - 可用数量：{testPool.AvailableCount}，总数量：{testPool.TotalCount}");
            }
            else
            {
                Debug.LogWarning("[PoolTest] 未指定测试预制体！"); // 如果未赋值预制体，打印警告
            }
        }

        /// <summary>
        /// 延迟指定时间后将对象归还到对象池
        /// </summary>
        /// <param name="obj">要归还的对象</param>
        /// <param name="delay">延迟时间（秒）</param>
        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay); // 等待指定秒数

            if (testPool != null) // 检查对象池是否存在
            {
                testPool.Return(obj); // 将对象归还到池
                Debug.Log($"[PoolTest] 已归还对象：{obj.name}");
                // 打印归还后的对象池状态
                Debug.Log($"[PoolTest] 对象池状态 - 可用数量：{testPool.AvailableCount}，总数量：{testPool.TotalCount}");
            }
        }

        private void OnDestroy()
        {
            testPool?.Clear(); // 当脚本所在对象销毁时，清空对象池（?. 是null条件运算符，避免空引用错误）
        }
    }
}