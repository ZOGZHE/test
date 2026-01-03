using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace WoolyPath
{
    public class SwapManager : MonoBehaviour
    {
        public static SwapManager Instance { get; private set; }

        [Header("交换设置")]
        [SerializeField] private float swapAnimationDuration = 1f;
        [SerializeField] private AnimationCurve swapCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private List<SheepController> selectedSheep = new List<SheepController>();
        private bool isSwapMode = false;
        private bool isSwapping = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // 开始交换模式
        public void StartSwapMode()
        {
            if (isSwapMode) return;

            isSwapMode = true;
            selectedSheep.Clear();

            // 设置所有羊为可选择状态
            SetAllSheepClickable(true);

   
        }

        // 结束交换模式
        public void EndSwapMode()
        {
            isSwapMode = false;
            selectedSheep.Clear();

            // 恢复所有羊的原始可点击状态
            SetAllSheepClickable(false);
        }

        // 设置所有羊的可点击状态
        private void SetAllSheepClickable(bool clickable)
        {
            SheepController[] allSheep = FindObjectsOfType<SheepController>();
            foreach (SheepController sheep in allSheep)
            {
                SetOneSheepClickable(sheep, clickable);
            }
        }
        private void SetOneSheepClickable(SheepController sheep,bool clickable)
        {
            sheep.SetClickablebyMode(clickable);
        }
        

        // 选择羊（在InputManager中调用）
        public void SelectSheep(SheepController sheep)
        {
            if (!isSwapMode || selectedSheep.Contains(sheep)) return;

            selectedSheep.Add(sheep);
            sheep.SetClickablebyMode(false);

            // 播放选中效果
            PlaySelectionEffect(sheep);

            //Debug.Log($"[SwapManager] 已选择羊: {sheep.name}, 颜色: {sheep.GetColor()}");

            // 如果已经选择了两只羊，执行交换
            if (selectedSheep.Count >= 2)
            {
                if (isSwapping) return;
                StartCoroutine(PerformSwap());
            }
        }

        // 执行交换协程
        private IEnumerator PerformSwap()
        {
            if (selectedSheep.Count < 2) yield break;
            isSwapping = true;
            SheepController sheep1 = selectedSheep[0];
            SheepController sheep2 = selectedSheep[1];

            // 禁用输入，防止在交换过程中选择其他羊
            InputManager.Instance?.DisableInput();

            //Debug.Log($"[SwapManager] 开始交换: {sheep1.name} 和 {sheep2.name}");

            // 记录原始颜色
            WoolColor color1 = sheep1.GetColor();
            WoolColor color2 = sheep2.GetColor();

            // 播放交换开始效果
            if (EffectsManager.Instance != null)
            {
                //EffectsManager.Instance.PlaySwapEffect(sheep1.transform.position, sheep2.transform.position);
            }

            // 执行交换动画
            yield return StartCoroutine(SwapAnimation(sheep1, sheep2,color1,color2));


            //Debug.Log($"[SwapManager] 交换完成: {sheep1.name} -> {color2}, {sheep2.name} -> {color1}");

            // 恢复输入
            InputManager.Instance?.EnableInput();

            // 结束交换模式
            EndSwapMode();
        }

        // 交换动画协程
        private IEnumerator SwapAnimation(SheepController sheep1, SheepController sheep2, WoolColor color1, WoolColor color2)
        {
            Vector3 startPos1 = sheep1.transform.position;
            Vector3 startPos2 = sheep2.transform.position;
            Vector3 midPoint = (startPos1 + startPos2) / 2f;

            float timer = 0f;

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
            //// 实际交换颜色
            // 使用深拷贝进行数据交换
            SheepData tempData1 = sheep1.sheepData.Clone(); // 假设实现了Clone方法
            SheepData tempData2 = sheep2.sheepData.Clone();

            // 用缓存的原始数据进行交换
            sheep1.SwitchToOtherSheep(tempData2);
            sheep2.SwitchToOtherSheep(tempData1);
            isSwapping = false;

        }


        // 播放选中效果
        private void PlaySelectionEffect(SheepController sheep)
        {
            if (EffectsManager.Instance != null)
            {
                //EffectsManager.Instance.PlaySelectionEffect(sheep.transform.position, sheep.GetColor());
            }
        }

        // 检查是否在交换模式中
        public bool IsInSwapMode()
        {
            return isSwapMode;
        }

        // 取消交换模式
        public void CancelSwapMode()
        {
            if (isSwapMode)
            {
                EndSwapMode();
                Debug.Log("[SwapManager] 交换模式已取消");
            }
        }
    }
}