//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace SuperGear
//{
//    public class Preview : MonoBehaviour
//    {
//        public static Preview Instance;

//        [Header("预览配置")]
//        [Tooltip("预览积木的父节点（生成的积木会挂载到此节点下）")]
//        [SerializeField] public Transform previewParent;
//        [Tooltip("积木之间的间距（沿X轴排列，实际间距 = 此值 * 缩放系数）")]
//        [SerializeField] private float blockSpacing = 2.5f;
//        [Tooltip("预览积木的初始生成位置（相对于父节点的本地坐标）")]
//        [SerializeField] private Vector3 startPosition = Vector3.zero;
//        [Tooltip("预览积木的缩放系数（基于预制体原始缩放的倍数）")]
//        [SerializeField] private float previewScaleFactor = 1.0f;

//        [Header("类型与预制体映射")]
//        [SerializeField] public List<BlockTypeMapping> typeMappings = new List<BlockTypeMapping>();
//        private Dictionary<BlockType, GameObject> typeToPrefabDict = new Dictionary<BlockType, GameObject>();

//        // 保存当前关卡数据，用于动态更新预览
//        private List<BlockGenerateItem> currentGenerateItems;

//        #region 生命周期方法
//        private void Awake()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//                InitializeTypeMappingDict();
//                if (previewParent == null)
//                {
//                    GameObject parentObj = new GameObject("BlockPreview_Parent");
//                    previewParent = parentObj.transform;
//                    previewParent.SetParent(transform);
//                    previewParent.localPosition = Vector3.zero;
//                }
//            }
//            else
//            {
//                Destroy(gameObject);
//                return;
//            }
//        }

//        private void OnDestroy()
//        {
//            if (Instance == this)
//            {
//                Instance = null;
//                ClearAllPreviews();
//            }
//        }
//        #endregion

//        #region 初始化与映射
//        private void InitializeTypeMappingDict()
//        {
//            typeToPrefabDict.Clear();
//            foreach (var mapping in typeMappings)
//            {
//                if (mapping.blockPrefab != null && !typeToPrefabDict.ContainsKey(mapping.blockType))
//                {
//                    if (mapping.blockPrefab.GetComponent<BlockControl>() == null)
//                    {
//                        Debug.LogError($"预制体 {mapping.blockPrefab.name} 缺少 BlockControl 组件！");
//                        continue;
//                    }
//                    typeToPrefabDict.Add(mapping.blockType, mapping.blockPrefab);
//                }
//            }
//        }
//        #endregion

//        #region 预览生成核心方法
//        public void GenerateBlockPreviews(LevelData levelData)
//        {
//            if (levelData == null || levelData._blockGenerateData == null)
//            {
//                Debug.LogWarning("生成预览失败：关卡数据或积木生成配置为空！");
//                return;
//            }

//            GenerateBlockPreviews(levelData._blockGenerateData.generateItems);
//        }

//        public void GenerateBlockPreviews(List<BlockGenerateItem> generateItems)
//        {
//            if (generateItems == null || generateItems.Count == 0)
//            {
//                Debug.LogWarning("生成预览失败：积木配置列表为空！");
//                return;
//            }

//            if (previewParent == null)
//            {
//                Debug.LogError("生成预览失败：预览父节点未设置！");
//                return;
//            }

//            // 保存当前数据，用于后续更新
//            currentGenerateItems = generateItems;

//            ClearAllPreviews();

//            // 获取运行时状态数据（用于过滤）
//            List<int> placedIndices = null;
//            List<int> alternativeIndices = null;
//            int currentGlobalIndex = -1;

//            // 安全获取 BlockGenerate 实例状态
//            if (BlockGenerate.Instance != null)
//            {
//                placedIndices = BlockGenerate.Instance.PlacementArea;
//                alternativeIndices = BlockGenerate.Instance.AlternativeArea;

//                // 计算当前激活方块的全局索引
//                if (alternativeIndices != null && alternativeIndices.Count > 0)
//                {
//                    int currentActiveIndex = BlockGenerate.Instance.currentActiveIndex;
//                    if (currentActiveIndex >= 0 && currentActiveIndex < alternativeIndices.Count)
//                    {
//                        currentGlobalIndex = alternativeIndices[currentActiveIndex];
//                    }
//                }

//                // Debug日志：显示过滤状态
//                Debug.Log($"[Preview过滤] 已放置: [{string.Join(", ", placedIndices ?? new List<int>())}], " +
//                         $"备选区: [{string.Join(", ", alternativeIndices ?? new List<int>())}], " +
//                         $"当前激活索引: {currentGlobalIndex}");
//            }
//            else
//            {
//                Debug.LogWarning("[Preview过滤] BlockGenerate.Instance 为空！无法过滤预览");
//            }

//            // 第一遍遍历：统计过滤后实际需要显示的方块数量
//            int displayCount = 0;
//            for (int i = 0; i < generateItems.Count; i++)
//            {
//                // 应用过滤条件
//                if (placedIndices != null && placedIndices.Contains(i)) continue;
//                if (i == currentGlobalIndex) continue;
//                displayCount++;
//            }

//            // 计算居中的起始位置：总宽度的一半向左偏移
//            float totalWidth = (displayCount - 1) * blockSpacing * previewScaleFactor;
//            Vector3 centeredStartPos = startPosition;
//            centeredStartPos.x = startPosition.x - (totalWidth / 2);

//            // 第二遍遍历：生成预览方块
//            Vector3 currentLocalPos = centeredStartPos;
//            int generatedCount = 0;

//            for (int i = 0; i < generateItems.Count; i++)
//            {
//                var item = generateItems[i];

//                // 过滤条件1：跳过已放置的方块
//                if (placedIndices != null && placedIndices.Contains(i))
//                {
//                    Debug.Log($"[Preview过滤] 跳过已放置方块 索引:{i} 类型:{item.blockType}");
//                    continue;
//                }

//                // 过滤条件2：跳过当前激活/查看的方块
//                if (i == currentGlobalIndex)
//                {
//                    Debug.Log($"[Preview过滤] 跳过当前激活方块 索引:{i} 类型:{item.blockType}");
//                    continue;
//                }

//                GenerateSingleBlockPreview(item, currentLocalPos);
//                // 沿X轴正方向（右方）计算下一个位置，间距随缩放系数动态调整
//                currentLocalPos.x += blockSpacing * previewScaleFactor;
//                generatedCount++;
//            }

//            Debug.Log($"已生成 {generatedCount}/{generateItems.Count} 个预览积木（已过滤 {generateItems.Count - generatedCount} 个），居中显示");
//        }

//        private void GenerateSingleBlockPreview(BlockGenerateItem item, Vector3 localPos)
//        {
//            if (!typeToPrefabDict.TryGetValue(item.blockType, out GameObject blockPrefab))
//            {
//                Debug.LogError($"未找到 {item.blockType} 对应的预制体，跳过生成！");
//                return;
//            }

//            GameObject blockObj = Instantiate(blockPrefab, previewParent);
//            blockObj.name = $"Preview_{item.blockType}";
//            // 关键：使用父节点本地坐标设置位置，确保相对父节点向右排列
//            blockObj.transform.localPosition = localPos;

//            // 设置旋转：粉色方块(BlockPink02)默认增加90度Y轴旋转
//            if (item.blockType == BlockType.BlockPink02)
//            {
//                blockObj.transform.localRotation = Quaternion.Euler(0, 90, 0);
//            }
//            else
//            {
//                blockObj.transform.localRotation = Quaternion.identity;
//            }

//            // 应用缩放并同步影响间距
//            Vector3 originalScale = blockPrefab.transform.localScale;
//            blockObj.transform.localScale = originalScale * previewScaleFactor;

//            ConfigureGeneratedBlockGears(blockObj, item);
//        }
//        #endregion

//        #region 积木显示更新
//        /// <summary>
//        /// 动态更新预览（响应方块放置/移除/切换事件）
//        /// </summary>
//        public void UpdatePreview()
//        {
//            if (currentGenerateItems == null || currentGenerateItems.Count == 0)
//            {
//                Debug.LogWarning("UpdatePreview: 当前没有可用的积木数据！");
//                return;
//            }

//            // 重新生成预览（会应用最新的过滤规则）
//            GenerateBlockPreviews(currentGenerateItems);
//        }
//        #endregion

//        #region 齿轮配置与交互禁用
//        private void ConfigureGeneratedBlockGears(GameObject blockObj, BlockGenerateItem generateItem)
//        {
//            BlockControl blockControl = blockObj.GetComponent<BlockControl>();
//            BlockData blockData = blockControl?._blockData;

//            if (blockData == null || blockData._gearobject == null || blockData._gearobject.Length == 0)
//            {
//                Debug.LogWarning($"预览积木 {blockObj.name} 未配置BlockData或齿轮数据！");
//                return;
//            }

//            for (int j = 0; j < blockData._gearobject.Length; j++)
//            {
//                GearData gearData = blockData._gearobject[j];
//                if (gearData == null || gearData.GearObject == null)
//                    continue;

//                bool isGearEnabled = j switch
//                {
//                    0 => generateItem.gear1,
//                    1 => generateItem.gear2,
//                    2 => generateItem.gear3,
//                    3 => generateItem.gear4,
//                    _ => false
//                };

//                gearData.GearObject.SetActive(isGearEnabled);
//                gearData.IsActive = false;
//                gearData.IsShowForHint = true;

//                GearControl gearControl = gearData.GearObject.GetComponent<GearControl>();
//                if (gearControl != null)
//                {
//                    gearControl.IsShowForHint = true;
//                    gearControl.StopRotation();
//                }
//            }

//            DisableBlockInteraction(blockControl);
//        }

//        private void DisableBlockInteraction(BlockControl blockControl)
//        {
//            if (blockControl == null) return;

//            blockControl.enabled = false;
//            blockControl.OnRemoved -= blockControl.OnBlockRemovedStopGear;

//            Collider collider = blockControl.GetComponent<Collider>();
//            if (collider != null)
//            {
//                collider.enabled = false;
//            }

//            foreach (var childCollider in blockControl.GetComponentsInChildren<Collider>(true))
//            {
//                childCollider.enabled = false;
//            }
//        }
//        #endregion

//        #region 预览清理
//        public void ClearAllPreviews()
//        {
//            if (previewParent == null) return;

//            foreach (Transform child in previewParent)
//            {
//                Destroy(child.gameObject);
//            }
//        }
//        #endregion
//    }

//}