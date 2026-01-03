using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ConnectMaster
{
    public class HouseControl: MonoBehaviour
    {
        [Header("动画参数")]
        private float initialScale = 0f;
        private float peakScale = 1.1f;
        private float finalScale = 1f;
        private float scaleUpDuration = 0.3f;
        private float scaleDownDuration = 0.2f;

        // 核心引用：控制协程启停
        private Coroutine _shakeCoroutine;
        // 记录初始旋转，用于复原
        private Quaternion _originalRotation;

        public GameObject[] HousePartModel;
        public Dictionary<int, GameObject> HousePartModelDictionary = new Dictionary<int, GameObject>();
        private int EffectIndex = 0;
        


        #region 生命周期函数
        private void Awake()
        {

            InitializeDictionary();//初始化字典
        }
        private void Start()
        {
            InitializeAnimationData();
            EffectIndex = LevelManager.Instance.houseModelProgress;
        }
        #endregion

        #region 初始化
        private void InitializeAnimationData()
        {
            initialScale = HouseGeneration.Instance.initialScale;
            peakScale= HouseGeneration.Instance.peakScale;
            finalScale= HouseGeneration.Instance.finalScale;
            scaleUpDuration= HouseGeneration.Instance.scaleUpDuration;
            scaleDownDuration= HouseGeneration.Instance.scaleDownDuration;
        }
        //初始化字典
        private void InitializeDictionary()
        {
            int index =1;
            foreach (var pair in HousePartModel)
            {
                HousePartModelDictionary.Add(index, pair);
                index++;
            }
        }
        //初始化保留上关显示模型
        public void InitializeHouse()
        {
            HideAllPartModel();
            LevelData currentleveldata = LevelManager.Instance.currentLevelData;

            for (int i = 0; i<LevelManager.Instance.houseModelProgress; i++)
            {
                SetPartModelActive(i+1);
            }
           //Debug.Log("初始化保留上关显示模型");
        }

        #endregion

        #region 核心显现
        //隐藏所有的需要隐藏的部分房屋模型
        private void HideAllPartModel()
        {
            foreach (var pair in HousePartModel)
            {
                pair.SetActive(false);
            }
            //Debug.Log("隐藏所有模型");
        }
        public void SetPartModelActive(int index)
        {
            if (index == 0)
            {
                return;
            }
            GameObject TargetModel = HousePartModelDictionary[index];
           
           if (TargetModel != null)
            {
                TargetModel.SetActive(true);

            }
        }
        public void SetPartModelActive(int index,bool useEffect)
        {
            GameObject TargetModel = HousePartModelDictionary[index];
            StartCoroutine(PartModelAnimation(TargetModel)); //房屋出现动画


        }
        #endregion

        #region 房屋出现动画
        // 房屋出现动画
        public IEnumerator PartModelAnimation(GameObject TargetModel)
        {

            if (TargetModel == null) yield break;

            PartModelEffect(TargetModel);// 房屋出现特效
            yield return new WaitForSeconds(0.5f);
            TargetModel.SetActive(true);
            Transform targetTransform = TargetModel.transform;

            // 保存模型原有的scale
            Vector3 originalScale = targetTransform.localScale;

            // 第一阶段：从初始缩放放大到峰值缩放
            float elapsedTime = 0f;
            Vector3 startScale = originalScale * initialScale; // 使用原有scale乘以初始比例
            Vector3 peakScaleVector = originalScale * peakScale; // 使用原有scale乘以峰值比例

            targetTransform.localScale = startScale;

            // 放大到峰值
            while (elapsedTime < scaleUpDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / scaleUpDuration;
                targetTransform.localScale = Vector3.Lerp(startScale, peakScaleVector, progress);
                yield return null;
            }

            targetTransform.localScale = peakScaleVector;

            // 第二阶段：从峰值缩放到最终缩放（模型原有scale）
            elapsedTime = 0f;
            Vector3 finalScaleVector = originalScale; // 直接使用模型原有的scale

            while (elapsedTime < scaleDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / scaleDownDuration;
                targetTransform.localScale = Vector3.Lerp(peakScaleVector, finalScaleVector, progress);
                yield return null;
            }

            targetTransform.localScale = finalScaleVector;
            
        }

        #endregion

        #region 房屋出现特效
        // 房屋出现特效
        private void PartModelEffect(GameObject TargetModel)
        {
            if (TargetModel != null)
            {
                
                //特效位置
                Vector3 targetpos = new Vector3(TargetModel.transform.position.x + HouseGeneration.Instance.EffectrOffect.x, TargetModel.transform.position.y + HouseGeneration.Instance.EffectrOffect.y, TargetModel.transform.position.z + HouseGeneration.Instance.EffectrOffect.z);

                EffectManager.Instance.CreateEffect(
                effectKey: "1", // 第一个参数：字符串类型（匹配EffectManager配置的effectKey）
                position: targetpos, // 第二个参数：位置
                rotation: TargetModel.transform.rotation, // 第三个参数：旋转
                parent: transform // 第四个参数：父物体
                ); // 补充分号
                EffectIndex++;
            }
        }


        #endregion

        #region 房子左右摇晃动画
        //启动左右摇晃循环（自定义角度）
        public void StartShake(float shakeDegrees, float stepDuration = 2f)
        {
            StopShake(); // 先停旧协程
            _originalRotation = transform.localRotation;
            _shakeCoroutine = StartCoroutine(ShakeLoopCoroutine(shakeDegrees, stepDuration));
        }
        // 停止摇晃并复原
        public void StopShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }
            transform.localRotation = _originalRotation;
        }

        // 摇晃循环协程（左→复原→右→复原 往复）
        private IEnumerator ShakeLoopCoroutine(float shakeDegrees, float stepDuration)
        {
            while (true)
            {
                // 左摇指定角度（绕Y轴，可改X/Z）
                yield return RotateToY(-shakeDegrees, stepDuration);
                // 复原
                yield return RotateToY(_originalRotation.eulerAngles.y, stepDuration);
                // 右摇指定角度
                yield return RotateToY(shakeDegrees, stepDuration);
                // 复原
                yield return RotateToY(_originalRotation.eulerAngles.y, stepDuration);
            }
        }

        // 平滑旋转到目标Y轴角度
        private IEnumerator RotateToY(float targetY, float duration)
        {
            float startY = transform.localEulerAngles.y;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentY = Mathf.LerpAngle(startY, targetY, elapsed / duration);
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, currentY, transform.localEulerAngles.z);
                yield return null;
            }
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, targetY, transform.localEulerAngles.z);
        }
        #endregion
    }
}

