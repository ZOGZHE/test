using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.VisualScripting;

namespace ConnectMaster
{
    [CreateAssetMenu(fileName = "LevelDataConfigHelper", menuName = "Game/LevelDataConfigHelper", order = 1)]
    public class LevelDataConfigHelper : ScriptableObject
    {
        public LevelData[] levelDatas;
        public ItemDatabase itemDatabase;
        //public static event Action OnConfigChanged;
        // 存储分类结果的字典
        private Dictionary<HouseType, List<LevelData>> _classifiedLevels;

        #region 生命周期
        
        #endregion

        #region 核心功能
        [ContextMenu("类型对应模型数据自动配置")]
        public void DataConfig()
        {
            RequiredItemsConfig(); //根据rows配置RequiredItems
            Classify(); //按房屋类型分类关卡
            CategoryToModelMappingConfig(); //CategoryToModelMapping配置
            LevelNameConfig();//配置关卡名字
            LastLevelLastCategoryHint();   //配置上关最后使用类型提示
            houseModelProgressConfig();//配置房屋模型进度
        }
        //根据rows配置RequiredItems
        private void RequiredItemsConfig()
        {
            int index = 0;
            foreach (var level in levelDatas)
            {
                level.requiredItems.Clear();
                level.requiredItems = itemDatabase.allItems.Skip(index).Take(level.rows * 4).ToList();
                index += level.rows * 4;
            }
        }

        //按房屋类型分类关卡
        public void Classify()
        {
            if (levelDatas == null || levelDatas.Length == 0)
            {
                Debug.LogWarning("没有可分类的 LevelData 数据！");
                return;
            }

            // 按房屋类型分类
            _classifiedLevels = levelDatas
                .Where(level => level != null) // 过滤空数据
                .GroupBy(level => level._houseType)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(level => level.level).ToList()
                );


            Debug.Log($"数据分类完成！共处理 {levelDatas.Length} 个关卡，分为 {_classifiedLevels.Count} 种房屋类型。");
        }
        //CategoryToModelMapping配置
        public void CategoryToModelMappingConfig()
        {
            foreach (var kvp in _classifiedLevels)
            {
                HouseType houseType = kvp.Key;
                List<LevelData> levelDatalist = kvp.Value;
                int houseTypeMappingIndex = 1;
                //同房屋类型中的不同关
                foreach (var level in levelDatalist)
                {
                    level.SyncCategoryToModelMapping();//同步requiredItems的物品类别去重依次填入targetCategory
                    //同关的所有CategoryToModelMapping
                    foreach (var Mapping in level._categoryToModelMapping)
                    {
                        Mapping.modelIndex = houseTypeMappingIndex;
                        houseTypeMappingIndex++;
                    }
                }
            }
        }

        //配置关卡名字
        private void LevelNameConfig()
        {
            int index = 1;
            foreach (var level in levelDatas)
            {
                level.level = index;
                string LevelName = Convert.ToString(level.level);
                level.levelName = LevelName;
                level.pos = new Vector2(0, -310f);
                index++;
            }
        }
        //配置上关最后使用类型提示
        private void LastLevelLastCategoryHint()
        {
            for (int i = 0; i < levelDatas.Length; i++)
            {
                if (i == 0)
                {
                    continue;
                }
                var LastLevelLastcategoryToModelMapping = levelDatas[i - 1]._categoryToModelMapping.Last();
                levelDatas[i].LastLevelLastCategory = LastLevelLastcategoryToModelMapping.targetCategory;
            }
        }

        //配置房屋模型进度
        private void houseModelProgressConfig()
        {
            foreach (var level in levelDatas)
            {
                level.houseModelProgress = level._categoryToModelMapping[0].modelIndex - 1;
                
            }
        }


        #endregion

    }
}