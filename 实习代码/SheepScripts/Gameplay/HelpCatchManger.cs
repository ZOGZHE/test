//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System.Linq;

//namespace WoolyPath
//{
//    public class HelpCatchManger : MonoBehaviour
//    {
//        public static HelpCatchManger Instance { get; private set; }
//        public float WaitTime = 1f;
//        void Start()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//                DontDestroyOnLoad(gameObject);
//            }
//            else
//            {
//                Destroy(gameObject);
//            }
//        }

//        #region 激活收集器查询功能

//        public List<CollectorPlate> GetAllActiveCollectors()
//        {
//            CollectorPlate[] allCollectors = Object.FindObjectsOfType<CollectorPlate>();
//            List<CollectorPlate> activeCollectors = allCollectors
//                .Where(collector =>
//                    collector != null
//                    && !Object.ReferenceEquals(collector, null) // 检查Unity对象是否真正未被销毁
//                    && collector.gameObject != null // 确保关联的GameObject未被销毁
//                    && collector.gameObject.activeInHierarchy
//                    && collector.isUnlocked
//                    && !collector.IsComplete()
//                )
//                .ToList();
//            return activeCollectors;
//        }

//        #endregion


//        #region 按三步逻辑匹配：随机收集器→对应羊→触发点击


//        public void HelpYouProp()
//        {
//            StartCoroutine(HelpYouPropIEnumerator());
//        }
//        private IEnumerator HelpYouPropIEnumerator()
//        {

//            InputManager.Instance.DisableInput();

//            // 循环三次，每次触发方法后等待1秒（最后一次不需要等待）
//            for (int i = 0; i < 3; i++)
//            {
//                // 触发方法
//                ExecuteThreeStepSheepTrigger();
//                yield return new WaitForSeconds(WaitTime);
//            }
//            // yield return new WaitForSeconds(1f);
//            InputManager.Instance.EnableInput();
//        }
//        public void ExecuteThreeStepSheepTrigger()
//        {
//            // ===================== 第一步：从解锁收集器中随机选1个 =====================
//            CollectorPlate randomUnlockedCollector = GetRandomUnlockedCollector();
//            // 若没有可用收集器（如全部未解锁/已完成），直接返回失败
//            if (randomUnlockedCollector == null)
//            {
//                Debug.LogWarning("[HelpCatchManger] 第一步失败：没有找到解锁且未完成的收集器");

//            }
//            Debug.Log($"[HelpCatchManger] 第一步成功：随机选中收集器（颜色：{randomUnlockedCollector.GetTargetColor()}）");


//            // ===================== 第二步：找该收集器颜色对应的羊 =====================
//            WoolColor targetColor = randomUnlockedCollector.GetTargetColor();
//            SheepController matchingSheep = GetMatchingSheepForCollectorColor(targetColor);
//            // 若没有对应颜色的可点击羊，返回失败
//            if (matchingSheep == null)
//            {
//                Debug.LogWarning($"[HelpCatchManger] 第二步失败：没有找到颜色为{targetColor}的可点击羊");

//            }
//            Debug.Log($"[HelpCatchManger] 第二步成功：找到对应羊（名称：{matchingSheep.gameObject.name}）");


//            // ===================== 第三步：触发这只羊的OnClicked =====================
//            matchingSheep.RemoveBlackMask();
//            SheepSpawner.instance.RemoveSheepFromQueue(matchingSheep);
//            matchingSheep.OutClicked();

//            Debug.Log($"[HelpCatchManger] 第三步成功：已触发羊 {matchingSheep.gameObject.name} 的移动序列");

//        }


//        private CollectorPlate GetRandomUnlockedCollector()
//        {
//            // 先获取所有符合条件的收集器（复用原有方法，确保判定逻辑一致）
//            List<CollectorPlate> allUnlockedCollectors = GetAllActiveCollectors();

//            // 若没有可用收集器，返回null
//            if (allUnlockedCollectors.Count == 0)
//                return null;

//            // 随机取一个（使用Unity的Random，避免与系统Random冲突）
//            int randomIndex = UnityEngine.Random.Range(0, allUnlockedCollectors.Count);
//            return allUnlockedCollectors[randomIndex];
//        }


//        private SheepController GetMatchingSheepForCollectorColor(WoolColor targetColor)
//        {
//            // 获取场景中所有羊
//            SheepController[] allSheep = Object.FindObjectsOfType<SheepController>();

//            // 筛选条件：1. 羊激活 2. 颜色匹配 3. 可点击（未被点击+逻辑允许+模式允许）
//            List<SheepController> validSheep = allSheep
//          .Where(sheep =>
//              sheep != null
//              && !Object.ReferenceEquals(sheep, null) // 检查Unity对象是否真正未被销毁
//              && sheep.gameObject != null // 确保羊的GameObject未被销毁
//              && sheep.gameObject.activeInHierarchy
//              && sheep.IsActive()
//              && !sheep.HasBeenClicked()
//              && sheep.GetColor() == targetColor)
//          .ToList();

//            // 若有多个符合条件的羊，随机选1个（也可改为取第一个，根据需求调整）
//            return validSheep.Count > 0 ? validSheep[UnityEngine.Random.Range(0, validSheep.Count)] : null;
//        }
//        #endregion
//    }
//}



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WoolyPath
{
    public class HelpCatchManger : MonoBehaviour
    {
        public static HelpCatchManger Instance { get; private set; }
        public float WaitTime = 1f;

        // 新增：记录已处理的收集器，避免重复选择
        private List<CollectorPlate> processedCollectors = new List<CollectorPlate>();

        void Start()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region 激活收集器查询功能

        public List<CollectorPlate> GetAllActiveCollectors()
        {
            CollectorPlate[] allCollectors = Object.FindObjectsOfType<CollectorPlate>();
            List<CollectorPlate> activeCollectors = allCollectors
                .Where(collector =>
                    collector != null
                    && !Object.ReferenceEquals(collector, null)
                    && collector.gameObject != null
                    && collector.gameObject.activeInHierarchy
                    && collector.isUnlocked
                    && !collector.IsComplete()
                )
                .ToList();
            return activeCollectors;
        }

        #endregion


        #region 按三步逻辑匹配：随机收集器→对应羊→触发点击


        public void HelpYouProp()
        {
            // 重置已处理收集器列表
            processedCollectors.Clear();
            StartCoroutine(HelpYouPropIEnumerator());
        }

        private IEnumerator HelpYouPropIEnumerator()
        {
            // 关键：先检查InputManager实例是否存在
            if (InputManager.Instance == null)
            {
                //Debug.LogError("[HelpCatchManger] 致命错误：InputManager.Instance 为 null！无法禁用输入");
                yield break; // 直接终止协程，避免后续错误
            }

            InputManager.Instance.DisableInput();
            //Debug.LogError("[HelpCatchManger] 已调用 InputManager.DisableInput()，尝试禁用输入");

            // 循环最多三次，但会根据可用收集器数量动态调整
            int attempts = 0;
            int maxAttempts = 3;

            while (attempts < maxAttempts)
            {
                // 检查是否还有可用收集器
                List<CollectorPlate> remainingCollectors = GetAllActiveCollectors();
                if (remainingCollectors.Count == 0)
                {
                    Debug.LogWarning("[HelpCatchManger] 没有可用的收集器了，提前结束帮助流程");
                    break;
                }

                // 执行一次操作
                bool success = ExecuteThreeStepSheepTrigger();
                attempts++;

                // 如果还有剩余次数且不是最后一次，等待
                if (attempts < maxAttempts)
                {
                    yield return new WaitForSeconds(WaitTime);
                }
            }

            InputManager.Instance.EnableInput();
        }

        public bool ExecuteThreeStepSheepTrigger()
        {
            // ===================== 第一步：从解锁收集器中随机选1个 =====================
            CollectorPlate randomUnlockedCollector = GetRandomUnlockedCollector();
            if (randomUnlockedCollector == null)
            {
                Debug.LogWarning("[HelpCatchManger] 第一步失败：没有找到解锁且未完成的收集器");
                return false; // 操作失败
            }
            Debug.Log($"[HelpCatchManger] 第一步成功：随机选中收集器（颜色：{randomUnlockedCollector.GetTargetColor()}）");


            // ===================== 第二步：找该收集器颜色对应的羊 =====================
            WoolColor targetColor = randomUnlockedCollector.GetTargetColor();
            SheepController matchingSheep = GetMatchingSheepForCollectorColor(targetColor);
            if (matchingSheep == null)
            {
                Debug.LogWarning($"[HelpCatchManger] 第二步失败：没有找到颜色为{targetColor}的可点击羊");
                return false; // 操作失败
            }
            Debug.Log($"[HelpCatchManger] 第二步成功：找到对应羊（名称：{matchingSheep.gameObject.name}）");


            // ===================== 第三步：触发这只羊的OnClicked =====================
            matchingSheep.RemoveBlackMask();
            SheepSpawner.instance.RemoveSheepFromQueue(matchingSheep);
            matchingSheep.OutClicked();

            Debug.Log($"[HelpCatchManger] 第三步成功：已触发羊 {matchingSheep.gameObject.name} 的移动序列");

            // 记录已处理的收集器，尽量避免重复选择
            if (!processedCollectors.Contains(randomUnlockedCollector))
            {
                processedCollectors.Add(randomUnlockedCollector);
            }

            return true; // 操作成功
        }


        private CollectorPlate GetRandomUnlockedCollector()
        {
            List<CollectorPlate> allUnlockedCollectors = GetAllActiveCollectors();

            if (allUnlockedCollectors.Count == 0)
                return null;

            // 优先选择未处理过的收集器
            List<CollectorPlate> unprocessedCollectors = allUnlockedCollectors
                .Where(c => !processedCollectors.Contains(c))
                .ToList();

            // 如果有未处理的收集器，从中选择；否则从所有可用收集器中选择
            List<CollectorPlate> candidates = unprocessedCollectors.Count > 0 ? unprocessedCollectors : allUnlockedCollectors;

            int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
            return candidates[randomIndex];
        }


        private SheepController GetMatchingSheepForCollectorColor(WoolColor targetColor)
        {
            SheepController[] allSheep = Object.FindObjectsOfType<SheepController>();

            List<SheepController> validSheep = allSheep
          .Where(sheep =>
              sheep != null
              && !Object.ReferenceEquals(sheep, null)
              && sheep.gameObject != null
              && sheep.gameObject.activeInHierarchy
              && sheep.IsActive()
              && !sheep.HasBeenClicked()
              && sheep.GetColor() == targetColor)
          .ToList();

            return validSheep.Count > 0 ? validSheep[UnityEngine.Random.Range(0, validSheep.Count)] : null;
        }
        #endregion
    }
}
