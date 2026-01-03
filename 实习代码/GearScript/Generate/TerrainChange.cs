using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    public class TerrainChange : MonoBehaviour
    {
        #region 数据配置
        public static TerrainChange Instance;
        public GameObject TheTerrain; // 地形 GameObject（拖入需要改变材质的地形）
        [Header("预设材质球列表")]
        [Tooltip("在这里提前拖入所有要切换的材质球")]
        public List<Material> presetMaterials = new List<Material>(); // 预设材质列表
        private Renderer terrainRenderer; // 地形的渲染组件（用于设置材质）
        #endregion

        #region 生命周期函数
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

        private void Start()
        {
            // 初始化：获取地形的渲染组件
            InitTerrainRenderer();
        }
        #endregion

        #region 初始化与检查
        //初始化地形的渲染组件（确保能设置材质）
        private void InitTerrainRenderer()
        {
            if (TheTerrain == null)
            {
                Debug.LogError("请先给 TheTerrain 赋值（拖入地形 GameObject）！");
                return;
            }

            // 获取地形的渲染组件（支持 MeshRenderer 或 TerrainRenderer 等）
            terrainRenderer = TheTerrain.GetComponent<Renderer>();
            if (terrainRenderer == null)
            {
                Debug.LogError("地形 GameObject 上没有 Renderer 组件，无法设置材质！");
            }
        }
        #endregion

        #region 材质切换方法
        //通过索引切换到预设的材质球
        public void ChangeToPresetMaterial(int materialIndex)
        {
            //Debug.LogError("ChangeToPresetMaterial");
            // 检查渲染组件是否有效
            if (terrainRenderer == null)
            {
                InitTerrainRenderer(); // 重新尝试获取
                if (terrainRenderer == null) return;
            }

            // 检查索引是否有效
            if (materialIndex < 0 || materialIndex >= presetMaterials.Count)
            {
                Debug.LogError($"材质索引 {materialIndex} 无效！预设材质数量：{presetMaterials.Count}");
                return;
            }

            // 检查材质是否存在
            Material targetMaterial = presetMaterials[materialIndex];
            if (targetMaterial == null)
            {
                Debug.LogError($"预设材质列表中索引 {materialIndex} 的材质为空！");
                return;
            }

            // 应用材质
            terrainRenderer.material = targetMaterial;
            //Debug.Log($"地形材质已切换为：{targetMaterial.name}");
        }

        //直接切换到指定材质球（不通过预设列表）
        public void ChangeToCustomMaterial(Material customMaterial)
        {
            if (terrainRenderer == null)
            {
                InitTerrainRenderer();
                if (terrainRenderer == null) return;
            }

            if (customMaterial == null)
            {
                Debug.LogError("传入的材质为空！");
                return;
            }

            terrainRenderer.material = customMaterial;
            Debug.Log($"地形材质已切换为：{customMaterial.name}");
        }
        #endregion
    }
}