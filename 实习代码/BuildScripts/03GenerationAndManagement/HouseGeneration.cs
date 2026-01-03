using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConnectMaster
{
    //房屋类型枚举
    public enum HouseType
    {
        LivingRoom,// 客厅
        Kitchen, // 厨房
        SushiShop,// 寿司店
        FruitStore,// 水果店
        ConfectioneryStore,//点心店
        Bedchamber,//卧室
        Bathroom,//浴室
        FlowerShop,//花店
        BeverageStore,//饮品店
    }

    //房屋类型-模型映射类
    [System.Serializable] // 让类可在Unity编辑器中序列化显示
    public class HouseMapping
    {
        [Header("房屋类型")]
        [SerializeField] private HouseType _houseType; // 序列化字段，支持编辑器赋值
        [Header("房屋预制体（需挂载HouseControl脚本）")]
        [SerializeField] private HouseControl _houseModel; // 房屋预制体的控制脚本

        // 公共只读属性，避免外部直接修改字段
        public HouseType HouseType => _houseType;
        public HouseControl HouseModel => _houseModel;
    }


    public class HouseGeneration : MonoBehaviour
    {

        public static HouseGeneration Instance { get; private set; }

        [Header("动画参数")]
        [SerializeField] public float initialScale = 0f;
        [SerializeField] public float peakScale = 1.1f;
        [SerializeField] public float finalScale = 1f;
        [SerializeField] public float scaleUpDuration = 0.3f;
        [SerializeField] public float scaleDownDuration = 0.2f;
        [Header("房屋相关")]
        [SerializeField]private Vector3 spawnPos;
        [SerializeField]private Quaternion spawnRot;
        [SerializeField] public Vector3 EffectrOffect;
        [Header("房屋映射配置")]
        [SerializeField] private HouseMapping[] _houseMappings; // 编辑器配置的映射数组

        public List<HouseControl> _spawnedHouses = new List<HouseControl>();

        // 私有字典（内部维护映射关系）
        public Dictionary<HouseType, HouseMapping> _houseTypeDict = new Dictionary<HouseType, HouseMapping>();

 

        #region 生命周期函数
        private void Awake()
        {
            // 完善单例模式：避免重复实例、持久化到场景切换
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 场景切换时不销毁
            }
            else
            {
                Destroy(gameObject); // 销毁重复实例
                return;
            }

            InitializeHouseDictionary(); // 初始化字典
        }
        #endregion

        #region 初始化：构建类型-预制体映射字典
        //初始化字典：将Inspector配置的映射数组转换为字典（便于快速查询）
        private void InitializeHouseDictionary()
        {
            // 清空字典，避免重复初始化
            _houseTypeDict.Clear();

            // 空值判断：避免数组未赋值导致报错
            if (_houseMappings == null || _houseMappings.Length == 0)
            {
                Debug.LogWarning($"【HouseGeneration】房屋映射数组未配置！请在Inspector中赋值");
                return;
            }

            // 遍历数组，填充字典
            foreach (var mapping in _houseMappings)
            {
                // 跳过空映射
                if (mapping == null)
                {
                    Debug.LogWarning($"【HouseGeneration】映射数组中存在空元素，已跳过");
                    continue;
                }

                // 检查预制体是否配置
                if (mapping.HouseModel == null)
                {
                    Debug.LogWarning($"【HouseGeneration】类型[{mapping.HouseType}]的房屋预制体未配置，已跳过");
                    continue;
                }

                // 检查是否存在重复类型（避免字典键冲突）
                if (_houseTypeDict.ContainsKey(mapping.HouseType))
                {
                    Debug.LogError($"【HouseGeneration】发现重复的房屋类型[{mapping.HouseType}]，已跳过重复项");
                    continue;
                }

                // 添加到字典（键：房屋类型，值：完整映射信息）
                _houseTypeDict.Add(mapping.HouseType, mapping);
            }

           // Debug.Log($"【HouseGeneration】字典初始化完成，共加载{_houseTypeDict.Count}种房屋类型");
        }
        #endregion

        #region 核心生成：根据类型创建房屋实例
        // 根据房屋类型生成实例（核心方法）

        public HouseControl SpawnHouse(HouseType houseType)
        {
           // 清除上关所有房屋模型
            ClearPreviousHouses();
            // 1. 查找字典中的映射信息
            if (!_houseTypeDict.TryGetValue(houseType, out var targetMapping))
            {
                Debug.LogError($"【HouseGeneration】未找到类型[{houseType}]的房屋配置，生成失败");
                return null;
            }

            // 2. 实例化预制体
            GameObject houseObj = Instantiate(targetMapping.HouseModel.gameObject, spawnPos, spawnRot);
            if (houseObj == null)
            {
                Debug.LogError($"【HouseGeneration】实例化类型[{houseType}]的预制体失败");
                return null;
            }

            // 3. 获取实例上的控制脚本（确保预制体已挂载HouseControl）
            HouseControl houseControl = houseObj.GetComponent<HouseControl>();
            houseControl.InitializeHouse();
            LevelManager.Instance._houseControl=houseControl;
            if (houseControl == null)
            {
                Debug.LogError($"【HouseGeneration】房屋预制体[{targetMapping.HouseModel.name}]未挂载HouseControl脚本，已销毁实例");
                Destroy(houseObj);
                return null;
            }

            // 4. 可选：给实例设置名称（便于调试）
            houseObj.name = $"{houseType}_{houseObj.GetInstanceID()}";

            // 将生成的房屋实例加入管理列表
            _spawnedHouses.Add(houseControl);

            //Debug.Log($"【HouseGeneration】成功生成房屋：{houseObj.name}");
            return houseControl;
        }

        #endregion

        #region 辅助方法
        // 清除上关所有房屋模型（外部调用此方法切换关卡）
        public void ClearPreviousHouses()
        {
            // 遍历所有已生成的房屋，逐个销毁
            foreach (var house in _spawnedHouses)
            {
                if (house != null && house.gameObject != null)
                {
                    Destroy(house.gameObject);
                    //Debug.Log($"【HouseGeneration】已销毁房屋：{house.gameObject.name}");
                }
            }
            // 清空列表，避免残留引用
            _spawnedHouses.Clear();
            //Debug.Log($"【HouseGeneration】上关房屋已全部清除，当前剩余房屋数：{_spawnedHouses.Count}");
        }
      
        #endregion
    }

}