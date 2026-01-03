using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConnectMaster
{
    public class NoviceHint : MonoBehaviour
    {
        public static NoviceHint Instance ;
        public GameObject hintPrefab1;
        public GameObject hintPrefab2;
        public Vector2 pos1;
        public Vector2 pos2;

        public GameObject NoviceHintImage;

        public Coroutine coroutine1;
        public Coroutine coroutine2;

      [Header("移动配置")]
        [Tooltip("平滑移动总时长（秒）")]
        public float moveDuration = 1.0f; // 移动耗时，可在Inspector调整
        [Tooltip("到达目标后隐藏的时长（秒）")]
        public float hideDuration = 0.5f; // 隐藏时长，可在Inspector调整
        [Tooltip("是否启用缓入缓出（更自然的移动效果）")]
        public bool useSmoothStep = true; // 缓动开关

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

        void Start()
        {
            hintPrefab1.SetActive(false);
            hintPrefab2.SetActive(false);
            NoviceHintImage.SetActive(false);
        }

        public void Move1()
        {
            stopMove1();
            hintPrefab1.SetActive(true);
           coroutine1 = StartCoroutine(PrefabMove(hintPrefab1, pos1));
        }
        public void Move2()
        {
            
            hintPrefab2.SetActive(true);
            coroutine2 = StartCoroutine(PrefabMove(hintPrefab2, pos2));
        }
        public void stopMove1()
        {
            if (coroutine1 != null)
                StopCoroutine(coroutine1);
            hintPrefab1.SetActive(false);
        }
        public void stopMove2()
        {
          
            if(coroutine2 != null) 
           StopCoroutine(coroutine2);
            hintPrefab2.SetActive(false);
            NoviceHintImage.SetActive(false);
        }


        //预制体平滑移动+隐藏+循环协程
        private IEnumerator PrefabMove(GameObject prefab, Vector2 targetPos)
        {
            // 参数合法性校验（避免空引用和异常）
            if (prefab == null)
            {
                Debug.LogWarning("PrefabMove：传入的预制体为空！");
                yield break;
            }
            if (moveDuration <= 0)
                moveDuration = 0.1f; // 避免除零错误
            if (hideDuration < 0)
                hideDuration = 0; // 隐藏时长不能为负

            // 区分UI物体（RectTransform）和普通物体（Transform）
            RectTransform rectTrans = prefab.GetComponent<RectTransform>();
            bool isUI = rectTrans != null;

            // 保存初始位置
            Vector3 initialPos = isUI ? rectTrans.anchoredPosition : prefab.transform.position;
            // 目标位置转换为Vector3（适配3D坐标）
            Vector3 targetPos3D = targetPos;

            // 循环重复运动
            while (true)
            {
                // 激活预制体
                prefab.SetActive(true);

                // 平滑移动核心逻辑
                float elapsedTime = 0f; // 已消耗时间
                while (elapsedTime < moveDuration)
                {
                    // 计算插值比例（0→1）
                    float t = elapsedTime / moveDuration;
                    // 启用缓入缓出（让移动开始和结束更柔和）
                    if (useSmoothStep)
                        t = Mathf.SmoothStep(0f, 1f, t);

                    // 更新位置（根据物体类型选择坐标方式）
                    if (isUI)
                        rectTrans.anchoredPosition = Vector3.Lerp(initialPos, targetPos3D, t);
                    else
                        prefab.transform.position = Vector3.Lerp(initialPos, targetPos3D, t);

                    // 累加时间并等待一帧
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // 确保最终位置准确到达目标点（避免插值误差）
                if (isUI)
                    rectTrans.anchoredPosition = targetPos3D;
                else
                    prefab.transform.position = targetPos3D;

                // 到达目标后隐藏
                prefab.SetActive(false);

                // 等待自定义隐藏时长
                yield return new WaitForSeconds(hideDuration);

                // 重置位置到初始位置（为下一轮移动做准备）
                if (isUI)
                    rectTrans.anchoredPosition = initialPos;
                else
                    prefab.transform.position = initialPos;
            }
        }
    }
}