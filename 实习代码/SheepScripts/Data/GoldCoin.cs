using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WoolyPath
{
    public class GoldCoin : MonoBehaviour
    {
        public static GoldCoin Instance { get; private set; }
        // 当前金币数量
        [SerializeField] private int currentGold=100;

        // 定义一个键名，用于存储和读取数据
        private const string GOLD_KEY = "PlayerGold";

        private void Awake()
        {          
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject); // 销毁重复实例，保证单例唯一性
            }
        // 游戏开始时加载保存的金币数据
        LoadGold();
        }
        void Start()
        {
        
        }

        // 加载本地保存的金币数据
        private void LoadGold()
        {
            // 如果有保存的数据，则加载；否则初始化为0
            currentGold = PlayerPrefs.GetInt(GOLD_KEY, 0);
            //Debug.Log("加载金币: " + currentGold);
        }

        // 保存金币数据到本地
        private void SaveGold()
        {
            PlayerPrefs.SetInt(GOLD_KEY, currentGold);
            // 立即写入磁盘，确保数据被保存
            PlayerPrefs.Save();
            //Debug.Log("保存金币: " + currentGold);
        }
        public void  CleanGold()
        {
            PlayerPrefs.SetInt(GOLD_KEY, 0);
            // 立即写入磁盘，确保数据被保存
            PlayerPrefs.Save();
            //Debug.Log("保存金币: " + currentGold);
            LoadGold();
        }

        // 增加金币
        public void AddGold(int amount)
        {
            if (amount > 0)
            {
                currentGold += amount;
                SaveGold(); // 增加后立即保存
            }
        }

        // 减少金币（返回是否减少成功）
        public bool SpendGold(int amount)
        {
            if (amount > 0 && currentGold >= amount)
            {
                currentGold -= amount;
                SaveGold(); // 减少后立即保存
                return true;
            }
            return false;
        }

        // 获取当前金币数量
        public int GetCurrentGold()
        {
            return currentGold;
        }
    }
}