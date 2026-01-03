using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ConnectMaster
{
    public class GoldCoinManager : MonoBehaviour
    {
        public static GoldCoinManager Instance { get; private set; }

        // 1. 键名改为常量，统一管理（避免硬编码错误）
        private const string GOLD_COIN_KEY = "GoldCoinManager_GoldCoins";
        // 初始值常量（只定义一次，避免冗余）
        private const int DEFAULT_GOLD = 300;

        // 私有字段，存储实际金币数
        private int _goldCoin;

        // 公开属性，控制读写+自动保存+合法性校验
        public int GoldCoin
        {
            get => _goldCoin;
            set
            {
                // 2. 合法性校验：金币不能为负数（根据业务调整，比如允许0则改为 value >= 0）
                if (value < 0)
                {
                    Debug.LogWarning($"尝试设置金币为负数：{value}，已修正为0");
                    _goldCoin = 0;
                }
                else
                {
                    _goldCoin = value;
                }
                SaveGoldCoins(); // 数值变化自动保存
            }
        }

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 3. 跨场景持久化，避免切换场景丢失
                LoadGoldCoins(); // 初始化加载数据
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region 金币加减
        // 增加金币
        public void AddFixedGold(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"增加金币数量不能为负数：{amount}，忽略此次操作");
                return;
            }
            GoldCoin += amount;
            //Debug.Log($"增加了 {amount} 金币，当前总金币：{GoldCoin}");
        }
        // 增加金币
        public void CostFixedGold(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"减少金币数量不能为负数：{amount}，忽略此次操作");
                return;
            }
            GoldCoin -= amount;
            //Debug.Log($"减少了 {amount} 金币，当前总金币：{GoldCoin}");
        }
        #endregion

        #region 数据持久化
        // 保存金币到本地
        private void SaveGoldCoins()
        {
            try
            {
                PlayerPrefs.SetInt(GOLD_COIN_KEY, _goldCoin);
                PlayerPrefs.Save(); // 立即写入磁盘（确保数据不丢失）
                //Debug.Log($"金币已保存：{_goldCoin}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"金币保存失败：{e.Message}");
            }
        }

        // 从本地加载金币
        private void LoadGoldCoins()
        {
            try
            {
                if (PlayerPrefs.HasKey(GOLD_COIN_KEY))
                {
                    _goldCoin = PlayerPrefs.GetInt(GOLD_COIN_KEY);
                    _goldCoin = Mathf.Max(_goldCoin, 0);
                    //Debug.Log($"金币加载成功：{_goldCoin}");
                }
                else
                {
                    _goldCoin = DEFAULT_GOLD; // 用常量赋值，统一初始值
                    Debug.Log($"无历史金币数据，使用默认值：{DEFAULT_GOLD}");
                }
            }
            catch (System.Exception e)
            {
                _goldCoin = DEFAULT_GOLD; // 加载失败时用默认值兜底，避免程序崩溃
                Debug.LogError($"金币加载失败，使用默认值：{DEFAULT_GOLD}，错误信息：{e.Message}");
            }
        }
        //重置金币到初始值
        public void ResetGoldToDefault()
        {
            GoldCoin = DEFAULT_GOLD;
            //Debug.Log($"金币已重置为默认值：{DEFAULT_GOLD}");
        }
        #endregion
    }
}