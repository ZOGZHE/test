using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace WoolyPath
{
    public class RefreshManager : MonoBehaviour
    {
        public static RefreshManager Instance { get; private set; }

        [Header("交换模式设置")]
        [SerializeField] private bool useParallelSwapMode = false; // 开关：false为串行模式，true为并行模式

        private bool isRefreshing = false; // 防止刷新过程中重复触发

        private void Awake()
        {
            // 单例模式初始化，确保全局唯一实例
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 可选：若场景切换需保留，可添加此句
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 外部调用入口：触发所有羊的随机交换
        /// </summary>
        public void TriggerRandomSwapAllSheep()
        {
           // if (isRefreshing || SwapManager.Instance.IsInSwapMode()) return; // 正在刷新或交换时，忽略重复调用

            if (useParallelSwapMode)
            {
                StartCoroutine(PerformParallelRandomSwapAllSheep());
            }
            else
            {
                StartCoroutine(PerformRandomSwapAllSheep());
            }
        }

        /// <summary>
        /// 原模式：串行交换（一对一对顺序交换）
        /// </summary>
        private IEnumerator PerformRandomSwapAllSheep()
        {
            isRefreshing = true;
            // 1. 禁用输入，防止交换中操作其他羊
            InputManager.Instance?.DisableInput();

            // 2. 获取场景中所有"活跃"的羊（过滤非活跃状态的羊）
            SheepController[] allActiveSheep = FindObjectsOfType<SheepController>()
                .Where(sheep => sheep.IsActive())
                .ToArray();

            // 3. 边界判断：羊数量不足2只时，无需交换
            if (allActiveSheep.Length < 2)
            {
                Debug.LogWarning("[RefreshManager] 活跃羊数量不足2只，无法执行随机交换");
                InputManager.Instance?.EnableInput();
                isRefreshing = false;
                yield break;
            }

            Debug.Log($"[RefreshManager] 开始串行随机交换，共{allActiveSheep.Length}只活跃羊");

            // 4. 打乱羊的顺序（生成随机序列）
            List<SheepController> shuffledSheepList = allActiveSheep.OrderBy(_ => Random.value).ToList();

            // 5. 使用SwapManager成对交换羊
            for (int i = 0; i < shuffledSheepList.Count - 1; i += 2)
            {
                // 启动交换模式
                SwapManager.Instance.StartSwapMode();

                // 选择两只羊进行交换
                SwapManager.Instance.SelectSheep(shuffledSheepList[i]);
                SwapManager.Instance.SelectSheep(shuffledSheepList[i + 1]);

                // 等待交换完成
                while (SwapManager.Instance.IsInSwapMode() || IsAnySheepSwapping(shuffledSheepList[i], shuffledSheepList[i + 1]))
                {
                    yield return null;
                }
            }

            // 6. 处理奇数数量的羊：最后一只羊与随机选择的一只羊交换
            if (allActiveSheep.Length % 2 != 0)
            {
                SheepController lastSheep = shuffledSheepList.Last();
                // 随机选择另一只羊进行交换（不能是自己）
                SheepController randomSheep;
                do
                {
                    randomSheep = shuffledSheepList[Random.Range(0, shuffledSheepList.Count - 1)];
                } while (randomSheep == lastSheep);

                Debug.Log($"[RefreshManager] 处理奇数羊，将 {lastSheep.name} 与 {randomSheep.name} 额外交换一次");

                // 启动交换模式
                SwapManager.Instance.StartSwapMode();

                // 选择两只羊进行交换
                SwapManager.Instance.SelectSheep(lastSheep);
                SwapManager.Instance.SelectSheep(randomSheep);

                // 等待交换完成
                while (SwapManager.Instance.IsInSwapMode() || IsAnySheepSwapping(lastSheep, randomSheep))
                {
                    yield return null;
                }
            }

            // 7. 交换完成，恢复输入
            Debug.Log("[RefreshManager] 所有羊串行随机交换完成");
            InputManager.Instance?.EnableInput();
            isRefreshing = false;
        }

        /// <summary>
        /// 新模式：并行交换（所有羊同时交换）
        /// </summary>
        private IEnumerator PerformParallelRandomSwapAllSheep()
        {
            isRefreshing = true;
  
            // 1. 禁用输入，防止交换中操作其他羊
            InputManager.Instance?.DisableInput();

            // 2. 获取场景中所有"活跃"的羊
            SheepController[] allActiveSheep = FindObjectsOfType<SheepController>()
                .Where(sheep => sheep.IsActive())
                .ToArray();

            // 3. 边界判断：羊数量不足2只时，无需交换
            if (allActiveSheep.Length < 2)
            {
               // Debug.LogWarning("[RefreshManager] 活跃羊数量不足2只，无法执行随机交换");
                InputManager.Instance?.EnableInput();
                isRefreshing = false;
                yield break;
            }

            //Debug.Log($"[RefreshManager] 开始并行随机交换，共{allActiveSheep.Length}只活跃羊");

            // 4. 打乱羊的顺序
            List<SheepController> shuffledSheepList = allActiveSheep.OrderBy(_ => Random.value).ToList();

            // 5. 创建所有交换任务
            List<IEnumerator> swapCoroutines = new List<IEnumerator>();

            // 6. 成对交换羊
            for (int i = 0; i < shuffledSheepList.Count - 1; i += 2)
            {
                SheepController sheep1 = shuffledSheepList[i];
                SheepController sheep2 = shuffledSheepList[i + 1];

                swapCoroutines.Add(PerformDirectSwap(sheep1, sheep2));
            }

            //// 7. 处理奇数数量的羊：最后一只羊与随机选择的一只羊交换
            //if (allActiveSheep.Length % 2 != 0)
            //{
            //    SheepController lastSheep = shuffledSheepList.Last();
            //    SheepController randomSheep;
            //    do
            //    {
            //        randomSheep = shuffledSheepList[Random.Range(0, shuffledSheepList.Count - 1)];
            //    } while (randomSheep == lastSheep);

            //    Debug.Log($"[RefreshManager] 处理奇数羊，将 {lastSheep.name} 与 {randomSheep.name} 额外交换一次");
            //    swapCoroutines.Add(PerformDirectSwap(lastSheep, randomSheep));
            //}

            // 8. 并行执行所有交换
            yield return StartCoroutine(ExecuteParallelCoroutines(swapCoroutines));
            yield return new WaitForSeconds(1.2f);
            // 9. 交换完成，恢复输入
           // Debug.Log("[RefreshManager] 所有羊并行随机交换完成");
            InputManager.Instance?.EnableInput();
            isRefreshing = false;
        }

        /// <summary>
        /// 直接执行交换（不通过SwapManager的UI交互）
        /// </summary>
        private IEnumerator PerformDirectSwap(SheepController sheep1, SheepController sheep2)
        {
            //Debug.Log($"[RefreshManager] 并行交换: {sheep1.name} 和 {sheep2.name}");

            // 记录原始颜色和数据
            WoolColor color1 = sheep1.GetColor();
            WoolColor color2 = sheep2.GetColor();

            // 使用深拷贝进行数据交换
            SheepData tempData1 = sheep1.sheepData.Clone();
            SheepData tempData2 = sheep2.sheepData.Clone();

            // 执行交换动画
            yield return StartCoroutine(SwapAnimation(sheep1, sheep2, tempData1, tempData2));

           // Debug.Log($"[RefreshManager] 并行交换完成: {sheep1.name} -> {color2}, {sheep2.name} -> {color1}");
        }

        /// <summary>
        /// 交换动画协程（从SwapManager复制过来）
        /// </summary>
        private IEnumerator SwapAnimation(SheepController sheep1, SheepController sheep2, SheepData data1, SheepData data2)
        {
            Vector3 startPos1 = sheep1.transform.position;
            Vector3 startPos2 = sheep2.transform.position;
            Vector3 midPoint = (startPos1 + startPos2) / 2f;

            float timer = 0f;
            float swapAnimationDuration = 1f; // 可以调整这个值
            AnimationCurve swapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            while (timer < swapAnimationDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / swapAnimationDuration;
                float curveValue = swapCurve.Evaluate(progress);

                // 计算弧形路径
                Vector3 arcOffset = Vector3.up * Mathf.Sin(progress * Mathf.PI) * 2f;

                // 更新位置
                sheep1.transform.position = Vector3.Lerp(startPos1, startPos2, curveValue) + arcOffset;
                sheep2.transform.position = Vector3.Lerp(startPos2, startPos1, curveValue) + arcOffset;

                yield return null;
            }

            // 确保最终位置正确
            sheep1.transform.position = startPos1;
            sheep2.transform.position = startPos2;

            // 实际交换数据
            sheep1.SwitchToOtherSheep(data2);
            sheep2.SwitchToOtherSheep(data1);
        }

        /// <summary>
        /// 并行执行多个协程
        /// </summary>
        private IEnumerator ExecuteParallelCoroutines(List<IEnumerator> coroutines)
        {
            int completedCount = 0;
            int totalCount = coroutines.Count;

            // 启动所有协程
            foreach (var coroutine in coroutines)
            {
                StartCoroutine(RunCoroutineWithCallback(coroutine, () => completedCount++));
            }

            // 等待所有协程完成
            while (completedCount < totalCount)
            {     
                yield return null;
            }
        }

        /// <summary>
        /// 运行协程并在完成后执行回调
        /// </summary>
        private IEnumerator RunCoroutineWithCallback(IEnumerator coroutine, System.Action onComplete)
        {
            yield return StartCoroutine(coroutine);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 检查两只羊中是否有正在交换的
        /// </summary>
        private bool IsAnySheepSwapping(SheepController sheepA, SheepController sheepB)
        {
            // 这里假设SheepController有判断是否正在交换的方法
            // 如果没有，可以通过其他方式判断，如检查位置是否在变化等
            return false; // 需要根据实际实现修改
        }

        /// <summary>
        /// 设置交换模式（外部调用）
        /// </summary>
        /// <param name="useParallel">true为并行模式，false为串行模式</param>
        public void SetSwapMode(bool useParallel)
        {
            useParallelSwapMode = useParallel;
            //Debug.Log($"[RefreshManager] 交换模式已设置为: {(useParallel ? "并行" : "串行")}");
        }

        /// <summary>
        /// 切换交换模式
        /// </summary>
        public void ToggleSwapMode()
        {
            useParallelSwapMode = !useParallelSwapMode;
            //Debug.Log($"[RefreshManager] 交换模式已切换为: {(useParallelSwapMode ? "并行" : "串行")}");
        }

        /// <summary>
        /// 获取当前交换模式
        /// </summary>
        public bool GetCurrentSwapMode()
        {
            return useParallelSwapMode;
        }
    }
}