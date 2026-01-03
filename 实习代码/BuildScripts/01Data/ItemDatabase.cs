using System.Collections.Generic;
using UnityEngine;

namespace ConnectMaster
{
    //全局物品数据库（ScriptableObject资源）
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database", order = 1)]
    public class ItemDatabase : ScriptableObject
    {
        [Header("所有游戏物品")]
        public List<Item> allItems = new List<Item>();

        //按ID查询物品（核心接口，LevelManager生成物品时使用）
        public Item GetItemById(int id)
        {
            return allItems.Find(item => item.id == id);
        }
        //按名称查询物品（用于调试）
        public Item GetItemByName(string name)
        {
            return allItems.Find(item =>
                item.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}