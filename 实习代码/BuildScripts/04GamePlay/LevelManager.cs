using DG.Tweening;
using LionStudios.Suite.Analytics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ConnectMaster
{
    //关卡管理器：从LevelData读取配置，同步给GridCellGenerate生成格子，仅负责物品填充和关卡控制
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("=== 核心依赖配置 ===")]
        [Tooltip("当前的关卡配置")]
        public List<LevelData> AlllevelData;
        public LevelData currentLevelData;
        public int currentLevelIndex = 0;

        [Header("相机核心配置")]
        public Camera currentCamera; // 拖入场景中的相机组件

        #region 第一组：记录相机当前的位置/旋转（私有隐藏，仅内部使用）
        // 改为私有，不在Inspector显示
        private Vector3 recordedCurrentPos; // 记录当前位置（私有）
        private Quaternion recordedCurrentRot; // 记录当前旋转（四元数，私有）
        #endregion

        #region 第二组：Inspector 手动输入的目标位置/旋转（公开显示）
        [Header("=== 目标相机状态（手动输入）===")]
        [Tooltip("目标相机位置（手动输 X/Y/Z）")]
        public Vector3 targetCameraPos; // 公开显示，手动输入
        [Tooltip("目标相机旋转（欧拉角，手动输 X/Y/Z）")]
        public Vector3 targetCameraRotEuler; // 公开显示，手动输入
        private Quaternion targetCameraRot; // 内部自动转四元数（私有）
        [Tooltip("透视相机目标视野角度（度数）")]
        public float targetFOV = 50f;
        [Tooltip("相机平滑过渡时间（秒）")]
        public float cameraSmoothTime = 0.5f; // 平滑移动时间，可在Inspector调整
        #endregion

        //数据持久化字段
        private const string LEVEL_INDEX_KEY = "ConnectMaster_CurrentLeveIndex";//存储键

        private bool isDataLoaded = false;//避免重复加载标记
        [Header("房屋")]
        //房屋模型进度
        [Tooltip("房屋模型")]
        public HouseControl _houseControl;
        public int houseModelProgress = 0;
        private const string MODEL_INDEX_KEY = "ConnectMaster_ModelIndex";//存储键

        [Tooltip("特效点")]
        public GameObject EffectPoint;//小特效点
        public GameObject rightEffectPoint;//大右特效点
        public GameObject leftEffectPoint;//大左特效点
        [Tooltip("延迟触发特效")]
        public float Delytime1;
        [Tooltip("延迟触发胜利面板")]
        public float Delytime2;


        //关卡进度
        [HideInInspector] public bool isLevelCompleted = false;
        public int TargetRows=0;
        public int HasPairRows=0;

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadCurrentLevelIndex(); // 初始化时加载保存的关卡索引
            }
            else
            {
                Destroy(gameObject);
            }

        }
        private void Start()
        {
            InitializeGame();
        }
        private void Update()
        {
#if UNITY_EDITOR
            //测试 快速通关
            if (Input.GetKeyDown(KeyCode.M))
            {
                OnLevelVictory();
            }
            //测试 重置关卡
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetLevelIndex();
                GoldCoinManager.Instance.ResetGoldToDefault();
            }
            //测试 特效
            if (Input.GetKeyDown(KeyCode.E))
            {
                PassLevelEffect1();
                PassLevelEffect2();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                ItemPairing.Instance.UpdateAllCell();
                foreach (var cell in ItemPairing.Instance.allGridCells)
                {
                    StartCoroutine(cell.ItemDropBounceAnimationSelf());
                    
                }
            }
            
            if (Input.GetKeyDown(KeyCode.F))
            {
                HintManager.Instance.HintCustomItems(4);
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                ItemPairing.Instance.UpdateAllCell();
                foreach (var cell in ItemPairing.Instance.allGridCells)
                {
                    cell.PlayEffect();
                    
                }
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                ItemPairing.Instance.UpdateAllCell();
                foreach (var cell in ItemPairing.Instance.allGridCells)
                {
                    
                    cell.PlayEffect2();
                }
            }

#endif
        }
        private void OnDestroy()
        {
            ClearLevel();
        }
        #endregion

        #region 初始化
        private void InitializeGame()
        {
            RecordCurrentCameraTransform();//记录相机位置数据
            ////优先加载保存的索引，无效则加载第0关
            //if (!isDataLoaded || currentLevelIndex < 0)
            //{
            //    LoadLevel(0);
            //}
            //else
            //{
            //    LoadLevel(currentLevelIndex);//加载保存的关卡
            //}
            LoadCurrentLevel();
        }


        //更新本关需要配对行数
        public void UpdateTargetRows()
        {
            TargetRows = currentLevelData.rows; // 目标=物品总row数（比如9行）
            HasPairRows = 0;
        }
        #endregion

        #region 相机相关

        //记录相机
        private void RecordCurrentCameraTransform()
        {
            if (currentCamera == null)
            {
                Debug.LogError("请给 currentCamera 赋值！");
                return;
            }
            recordedCurrentPos = currentCamera.transform.position;
            recordedCurrentRot = currentCamera.transform.rotation; // 仅记录四元数
        }

        //关卡加载相机位置重置
        private void InitializeCamera()
        {
            currentCamera.transform.position = recordedCurrentPos;
            currentCamera.transform.rotation = recordedCurrentRot;
            currentCamera.fieldOfView = 60f;
        }
        // 平滑移动协程（欧拉角转四元数插值）
        private IEnumerator SmoothMoveCoroutine()
        {
            Vector3 startPos = currentCamera.transform.position;
            Quaternion startRot = currentCamera.transform.rotation;
            targetCameraRot = Quaternion.Euler(targetCameraRotEuler); // 欧拉角转四元数
            float elapsedTime = 0f;

            while (elapsedTime < cameraSmoothTime)
            {
                // 位置线性插值
                currentCamera.transform.position = Vector3.Lerp(startPos, targetCameraPos, elapsedTime / cameraSmoothTime);
                // 旋转球面插值（四元数插值更平滑，无万向锁）
                currentCamera.transform.rotation = Quaternion.Slerp(startRot, targetCameraRot, elapsedTime / cameraSmoothTime);
                currentCamera.fieldOfView =Mathf.Lerp(60, targetFOV, elapsedTime / cameraSmoothTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 最终校准，确保精准到达目标状态
            currentCamera.transform.position = targetCameraPos;
            currentCamera.transform.rotation = targetCameraRot;
            currentCamera.fieldOfView = targetFOV;
            UIManager.Instance.SetOtherWinPanel(true);//打开阶段胜利面板
        }

        // 编辑模式同步：目标欧拉角自动转四元数
        private void OnValidate()
        {
            if (currentCamera == null) return;
            targetCameraRot = Quaternion.Euler(targetCameraRotEuler); // 实时转换
        }

        #endregion

        #region 加载关卡
        public void LoadCurrentLevel()
        {
            LoadLevel(currentLevelIndex);
            UIManager.Instance.NewLoading();

            //---------数据检测别删------------
            //开始
            LionAnalytics.MissionStarted(
                missionType:"main",
                missionName: $"main_level_{currentLevelIndex+1}",
                missionID: currentLevelIndex+1,
                missionAttempt: null,
                additionalData: null,
                isGamePlay: true
                );
            //---------数据检测别删------------    
        }

        public void LoadLevel(int targetLevelIndex)
        {
            //1.安全校验 AlllevelData 是否为空
            if (AlllevelData == null || AlllevelData.Count == 0)
            {
                Debug.LogError("LevelManager：加载关卡失败，AlllevelData 为空！请在Inspector添加LevelData资源");
                return;
            }

            //2.记录当前关卡配置
            int levelCount = AlllevelData.Count;
            int loopedIndex = (targetLevelIndex) % levelCount;
            LevelData targetlevel = AlllevelData[loopedIndex];
            currentLevelData = targetlevel;

            //3.更新数据配置与初始化
            InitializeCamera(); //关卡加载相机位置重置
            ClearLevel();// 清理上一关卡残留
            UpdateTargetRows(); //更新本关需要配对行数
            UpdateHouseModelProgress(targetLevelIndex);//重置大关卡模型进度
            ItemPairing.Instance.SupplementNum = (currentLevelData.rows - 6);  //更新本关可补充生成次数
            HintManager.Instance.InitializeHint(); //更新提示词框状态
            UIManager.Instance.Rainbow.SetActive(false);//Rainbow

            //4.生成器初始化
            SyncsummaryBoxParent(); //同步归纳框位置
            SyncLevelConfigToItemGenerate();//同步给ItemGenerate
            SyncLevelConfigToGridCellGenerate(); //同步给GridCellGenerate

            //5.具体生成
            HouseGeneration.Instance.SpawnHouse(currentLevelData._houseType); //生成房屋                                                                            
            GridCellGenerate.Instance.GenerateGridCells();// 调用GridCellGenerate生成格子
            StartCoroutine(DelayedGenerateItems()); // 延迟调用生成物品，确保InitAllItemLists完成后再生成

        }
        // 延迟生成物品的协程
        private System.Collections.IEnumerator DelayedGenerateItems()
        {
            // 等待一秒，确保InitAllItemLists完全执行完成
            yield return new WaitForSeconds(0.2f);

            if(currentLevelIndex!=0)
            {
                // 正常生成关卡物品
                ItemGenerate.Instance.GenerateItems();
            }
            else
            {
                //新手关卡生成物品
                ItemGenerate.Instance.NoviceGenerate();
            }

            SaveCurrentLevelIndex();//保存进度
        }

        #endregion

        #region 生成器关卡数据配置
        //同步归纳框位置
        public void SyncsummaryBoxParent()
        {
            
            if (currentLevelIndex == 0)
            {
                //新手关卡
                ItemPairing.Instance.summaryBoxParent.anchoredPosition = new Vector2(currentLevelData.pos.x, currentLevelData.pos.y - 200);
            }
            else
            {
                // 正常生成
                ItemPairing.Instance.summaryBoxParent.anchoredPosition = currentLevelData.pos;
            }
        }
        // 从LevelData读取配置，同步到ItemGenerate
        private void SyncLevelConfigToItemGenerate()
        {
            if (ItemGenerate.Instance == null || currentLevelData == null) return;
            ItemGenerate.Instance.InitAllItemLists(currentLevelData, currentLevelData.requiredItems);//调用物品生成初始化，分配初次生成与三次补充生成
        }
        // 从LevelData读取配置，同步到GridCellGenerate
        private void SyncLevelConfigToGridCellGenerate()
        {
            if (GridCellGenerate.Instance == null || currentLevelData == null) return;
            //同步生成父节点位置
            if (currentLevelIndex==0)
            {
                //新手关卡
                GridCellGenerate.Instance.gridParent.anchoredPosition = new Vector2( currentLevelData.pos.x, currentLevelData.pos.y-200);
            }
            else
            {
                // 正常生成
                GridCellGenerate.Instance.gridParent.anchoredPosition = currentLevelData.pos;
            }
          
            
            // 同步行列数
            GridCellGenerate.Instance.rowCount = currentLevelData.Cellrows;
            GridCellGenerate.Instance.colCount = currentLevelData.Cellcols;

            // 同步格子布局尺寸
            GridCellGenerate.Instance.cellWidth = currentLevelData.cellWidth;
            GridCellGenerate.Instance.cellHeight = currentLevelData.cellHeight;

            // 同步格子间距
            GridCellGenerate.Instance.spacingX = currentLevelData.spacingX;
            GridCellGenerate.Instance.spacingY = currentLevelData.spacingY;
        }
        #endregion

        #region 胜利条件与后续
        public void AddHasPairRows()
        {
            HasPairRows++;
        }
        public bool CheckHasVictory()
        {
            if(TargetRows!= HasPairRows)
            {
                return false;
            }
            OnLevelVictory();
            return true;
        }
        // 关卡胜利后进入下一关
        public void OnLevelVictory()
        {
             //结束新手指引
            NoviceHint.Instance.stopMove1();
            NoviceHint.Instance.stopMove2();      

            ////模型显现进度增加
            //houseModelProgress += TargetRows; 

            //计时器暂停
            GameStateManager.Instance.StopCountdown();
            UIManager.Instance.SetHudPanel(false);

            //判断是否为一个场景的结束来选择对应的通关界面
            if (AlllevelData[currentLevelIndex % AlllevelData.Count]._houseType == AlllevelData[(currentLevelIndex + 1) % AlllevelData.Count]._houseType)
            {
                Invoke("DelyPassLevelEffect1", Delytime1);//延迟触发特效
                Invoke("DelySetWinPanel", Delytime2);//延迟触发胜利面板 
            }
            else
            {
                Invoke("DelyPassLevelEffect2", Delytime1);//延迟触发特效
                Invoke("DelySetOtherWinPanel", Delytime2);//延迟触发胜利面板
            }

            //---------数据检测别删------------
            //胜利
            var rewardProduct = new LionStudios.Suite.Analytics.Product();
            rewardProduct.virtualCurrencies = new List<VirtualCurrency> { new VirtualCurrency("coins", "gold", 50) };
            Reward reward = new Reward(rewardProduct);
            LionAnalytics.MissionCompleted(
            missionType: "main",
            missionName: $"main_level_{currentLevelIndex+1}",
            missionID: currentLevelIndex+1,
            missionAttempt: null,
            additionalData: null,
            reward: reward,
            isGamePlay: true
            );
            //---------数据检测别删------------
            

        }
        //延迟触发特效
        private void DelyPassLevelEffect1()
        {
            PassLevelEffect1();
        }
        //延迟触发特效
        private void DelyPassLevelEffect2()
        {
            PassLevelEffect2();
        }
        //延迟触发胜利面板
        private void DelySetWinPanel()
        {
            UIManager.Instance.SetWinPanel(true);
            UIManager.Instance.SetGoldCoinsPanel(true); 
        }
        private void DelySetOtherWinPanel()
        {
            StartCoroutine(SmoothMoveCoroutine());
            _houseControl.StartShake(8);
            UIManager.Instance.Rainbow.SetActive(true);//Rainbow
            UIManager.Instance.SetGamePlayPanel(false);//关闭游玩界面
            //UIManager.Instance.SetHudPanel(false);
        }
        #endregion

        #region 大小通关特效
        //小关通关特效
        public void PassLevelEffect1()
        {
            EffectManager.Instance.CreateEffect("2", EffectPoint.transform.position, EffectPoint.transform.rotation, EffectPoint.transform);
        }
        //大关通关特效
        public void PassLevelEffect2()
        {
            //Debug.Log("PassLevelEffect2");
            EffectManager.Instance.CreateEffect("3", rightEffectPoint.transform.position, rightEffectPoint.transform.rotation, rightEffectPoint.transform);
            EffectManager.Instance.CreateEffect("4", leftEffectPoint.transform.position, leftEffectPoint.transform.rotation, leftEffectPoint.transform);
        }
        #endregion

        #region 失败触发
        //失败时先放回所有物品避免出错
        public void OnLevelLose()
        {
            List<ItemControl> allItems = new List<ItemControl>();
            // 查找场景中所有激活的 ItemControl 组件（包含未激活的也可以，确保所有物品都放回）
            ItemControl[] itemControls = FindObjectsOfType<ItemControl>(includeInactive: true);

            if (itemControls == null || itemControls.Length == 0)
            {
                Debug.LogWarning("未找到任何物品（ItemControl）");
                return;
            }

            // 过滤掉 null 项，确保列表纯净
            allItems.AddRange(itemControls.Where(item => item != null));
            //Debug.Log($"成功获取 {allItems.Count} 个物品，开始强制放回原格子");

            // 遍历所有物品，调用强制放回方法（不再调用 OnEndDrag）
            foreach (var item in allItems)
            {
                //强制放回
                //item.BackHierarchy();
            }
        }
        #endregion

        #region 辅助方法
        // 清理当前关卡：调用GridCellGenerate清空格子，清空格子列表
        public void ClearLevel()
        {
            ItemGenerate.Instance.ClearAllItems();// 清除所有物品
            GridCellGenerate.Instance?.ClearExistingCells();//同时清除所有格子和物品
            ItemPairing.Instance.ClearAllSummaryBox();//清除收纳框
        }
        //重置大关卡模型进度
        public void UpdateHouseModelProgress(int targetLevelIndex)
        {
            houseModelProgress=currentLevelData.houseModelProgress;
            //if (AlllevelData == null || AlllevelData.Count == 0 || currentLevelData == null)
            //{
            //    Debug.LogWarning("UpdateHouseModelProgress - 必要数据为空");
            //    return;
            //}

            //int levelCount = AlllevelData.Count;
            //// 计算上一关索引（处理循环边界）
            //int prevLevelIndex = (targetLevelIndex - 1 + levelCount) % levelCount;
            //LevelData prevLevelData = AlllevelData[prevLevelIndex];

            //// 比对上下两关房屋类型
            //if (currentLevelData._houseType != prevLevelData._houseType)
            //{
            //    houseModelProgress = 0;
            //    Debug.Log($"房屋类型变更（{prevLevelData._houseType}→{currentLevelData._houseType}），重置模型进度");
            //    SaveCurrentLevelIndex();
            //}
        }
        #endregion

        #region 数据持久化核心方法
        // 保存当前关卡索引到本地
        public void SaveCurrentLevelIndex()
        {
            PlayerPrefs.SetInt(LEVEL_INDEX_KEY, currentLevelIndex);
            PlayerPrefs.SetInt(MODEL_INDEX_KEY, houseModelProgress);
            PlayerPrefs.Save(); // 立即保存到磁盘
            //Debug.Log($"LevelManager：已保存关卡索引 -> {currentLevelIndex}");
        }
        //从本地加载关卡索引
        public void LoadCurrentLevelIndex()
        {
            if (PlayerPrefs.HasKey(LEVEL_INDEX_KEY)&& PlayerPrefs.HasKey(MODEL_INDEX_KEY))
            {
                currentLevelIndex = PlayerPrefs.GetInt(LEVEL_INDEX_KEY);
              

                houseModelProgress = PlayerPrefs.GetInt(MODEL_INDEX_KEY);
               
                isDataLoaded = true;
                //Debug.Log($"LevelManager：已加载保存的关卡索引 -> {currentLevelIndex}");
            }
            else
            {
                currentLevelIndex = 0;
                houseModelProgress =0;
                isDataLoaded = false;
                //Debug.Log("LevelManager：无保存的关卡数据，默认从第0关开始");
            }

        }
        // 重置关卡进度（回到第0关）
        public void ResetLevelIndex()
        {
            currentLevelIndex = 0;
            houseModelProgress = 0;
            SaveCurrentLevelIndex();
            LoadLevel(0);
            UIManager.Instance.NewLoading(); 
            //Debug.Log("LevelManager：已重置关卡进度到第0关");
        }
        #endregion
    }
}