using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    public class GoldCoinManager : MonoBehaviour
    {
        public static GoldCoinManager Instance;
        private int goldCoin =100; // 改为私有变量，通过属性控制读写

        // 公开属性，设置时自动保存
        public int GoldCoin
        {
            get => goldCoin;
            set
            {
                goldCoin = value;
                SaveGoldCoins(); // 数值变化时自动保存
            }
        }

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadGoldCoins(); // 初始化时加载金币数据
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {

        }

        void Update()
        {

        }
        #endregion
        public void AddFixedGold(int amount)
        {
            // 直接通过属性累加，自动触发保存
            GoldCoin += amount;
            Debug.Log($"增加了 {amount} 金币，当前总金币：{GoldCoin}");
        }
        #region 数据持久化
        // 保存金币数据到本地
        private void SaveGoldCoins()
        {
            PlayerPrefs.SetInt("GoldCoins", goldCoin);
            PlayerPrefs.Save(); // 立即写入磁盘
        }

        // 从本地加载金币数据
        private void LoadGoldCoins()
        {
            // 有存档则加载，无存档则默认0
            goldCoin = PlayerPrefs.HasKey("GoldCoins") ? PlayerPrefs.GetInt("GoldCoins") :100;
        }
        #endregion
    }
}