//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//namespace SuperGear
//{
//    public class BlockPreviewManager : MonoBehaviour
//    {
//        public static BlockPreviewManager Instance { get; private set; }
//        #region 预览窗口配置
//        [Header("预览窗口基础配置")]
//        [Tooltip("预览窗口UI根节点（Canvas下）")]
//        public RectTransform previewWindowRoot;
//        [Tooltip("预览窗口背景图片（用于显示裁剪后的摄像头画面）")]
//        public RawImage previewBackground;
//        [Tooltip("窗口初始位置（屏幕坐标）")]
//        public Vector2 initialWindowPos = new Vector2(50, 50);
//        [Tooltip("窗口大小（仅控制UI容器，不影响裁剪尺寸）")]
//        public Vector2 windowSize = new Vector2(300, 300);
//        [Tooltip("预览积木生成根节点（场景中空对象）")]
//        public Transform BlockPostion;
//        #endregion

//        #region 裁剪区核心配置
//        [Header("裁剪区配置（重点）")]
//        [Tooltip("是否启用裁剪功能")]
//        public bool enableCrop = true;
//        [Tooltip("裁剪后的目标宽度（决定RenderTexture宽度）")]
//        public int cropTargetWidth = 240;
//        [Tooltip("裁剪后的目标高度（决定RenderTexture高度）")]
//        public int cropTargetHeight = 180;
//        [Tooltip("裁剪尺寸最小值（避免过小导致画面异常）")]
//        public Vector2 minCropSize = new Vector2(80, 60);
//        #endregion

//        #region 顶视摄像头配置
//        [Header("顶视摄像头配置")]
//        public Camera previewCamera;
//        [Tooltip("摄像头正交视野基础值（会根据裁剪比适配）")]
//        public float baseCameraOrthographicSize = 3f;
//        [Tooltip("预览区域背景颜色")]
//        public Color previewBgColor = new Color(0.1f, 0.1f, 0.1f, 1f);
//        #endregion

//        #region 积木预览排列配置
//        [Header("积木预览排列配置")]
//        [Tooltip("积木预览实例间距")]
//        public float blockSpacing = 1.5f;
//        [Tooltip("积木预览缩放比例")]
//        public float blockPreviewScale = 0.8f;
//        [Tooltip("积木预览旋转角度（顶视最佳视角）")]
//        public Vector3 blockPreviewRotation = new Vector3(0, 45, 0);
//        [Tooltip("每行最大积木数量")]
//        public int maxBlocksPerRow = 4;
//        #endregion

//        #region 自身积木映射配置
//        [Header("积木类型-预制体映射")]
//        [Tooltip("配置积木类型对应的预制体")]
//        [SerializeField] private List<BlockTypeMapping> typeMappings = new List<BlockTypeMapping>();
//        private Dictionary<BlockType, GameObject> typeToPrefabDict = new Dictionary<BlockType, GameObject>();
//        #endregion

//        #region 私有变量
//        private RenderTexture previewRenderTexture; // 裁剪后的渲染纹理：用于存储摄像头拍摄并裁剪后的画面
//        private List<GameObject> previewBlockInstances = new List<GameObject>(); // 预览积木实例列表：管理所有生成的预览积木
//        private LevelData currentLevelData; // 当前关卡数据：存储当前需要预览的关卡信息
//        private float currentCropRatio; // 当前裁剪宽高比（width/height）：用于适配摄像头视野
//        #endregion

//        #region 生命周期方法
//        private void Awake()
//        {
//            if (Instance == null)
//            {
//                Instance = this;
//            }
//            else
//            {
//                Destroy(gameObject);
//                return;
//            }
//            InitializeTypeMappingDict();
//            // 初始化裁剪比例
//            currentCropRatio = CalculateCropRatio();

//        }
//        private void Update()
//        {
//            UpdateBlockPreview();
//        }

//        private void OnDestroy()
//        {
//            // 释放渲染纹理资源：避免内存泄漏
//            if (previewRenderTexture != null)
//            {
//                RenderTexture.ReleaseTemporary(previewRenderTexture);
//                previewRenderTexture = null;
//            }
//            DestroyAllPreviewBlocks();
//        }
//        #endregion

//        #region 初始化方法
//        //初始化积木数据
//        public void InitializePreviewBlock(LevelData levelData)
//        {
//            currentLevelData = levelData;
//            UpdateBlockPreview();
//        }
//        //更新积木预览
//        public void UpdateBlockPreview()
//        {
//            if (currentLevelData == null)
//            {
//                return;
//            }
//            InitializePreviewWindow();
//            InitializePreviewCamera();
//            CreateCropRenderTexture();
//            DestroyAllPreviewBlocks();
//            GenerateLevelBlockPreviews();
//        }
//        private void InitializePreviewWindow()
//        {
//            // 设置预览窗口的位置、大小
//            previewWindowRoot.anchoredPosition = initialWindowPos;
//            previewWindowRoot.sizeDelta = windowSize;
//        }
//        // 初始化顶视摄像头
//        private void InitializePreviewCamera()
//        {// 先校验摄像头是否赋值，避免空引用崩溃
//            if (previewCamera == null)
//            {
//                Debug.LogError("BlockPreviewManager：预览摄像头未赋值！");
//                return;
//            }
//            previewCamera.backgroundColor = previewBgColor;
//            previewCamera.orthographic = true;
//            AdjustCameraSizeByCropRatio();
//            // 让摄像头只渲染BlockPreview层
//            int previewLayer = LayerMask.NameToLayer("BlockPreview");
//            if (previewLayer == -1)
//            {
//                Debug.LogError("BlockPreviewManager：请在Unity编辑器Layer列表中创建「BlockPreview」层！");
//                return;
//            }
//            previewCamera.cullingMask = 1 << previewLayer; // 仅勾选BlockPreview层，屏蔽其他所有层
//        }

//        //初始化积木类型映射字典：将配置的类型-预制体列表转换为字典，提高查询效率
//        private void InitializeTypeMappingDict()
//        {
//            typeToPrefabDict.Clear();
//            if (typeMappings == null || typeMappings.Count == 0)
//            {
//                Debug.LogWarning("BlockPreviewManager：未配置任何积木映射！");
//                return;
//            }

//            foreach (var mapping in typeMappings)
//            {
//                if (mapping.blockPrefab == null)
//                {
//                    Debug.LogWarning($"BlockPreviewManager：积木类型 {mapping.blockType} 未指定预制体！");
//                    continue;
//                }

//                if (!typeToPrefabDict.ContainsKey(mapping.blockType))
//                {
//                    typeToPrefabDict.Add(mapping.blockType, mapping.blockPrefab);
//                }
//                else
//                {
//                    Debug.LogWarning($"BlockPreviewManager：积木类型 {mapping.blockType} 重复配置，已忽略！");
//                }
//            }
//        }

//        // 创建裁剪后的RenderTexture：根据裁剪配置生成对应尺寸的渲染纹理，并绑定到摄像头和UI
//        private void CreateCropRenderTexture()
//        {
//            if (!enableCrop || previewCamera == null || previewBackground == null)
//            {
//                Debug.LogWarning("渲染纹理创建失败：裁剪功能未启用或组件未赋值！");
//                return;
//            }

//            // 校验裁剪尺寸
//            int finalCropWidth = Mathf.Max(cropTargetWidth, (int)minCropSize.x);
//            int finalCropHeight = Mathf.Max(cropTargetHeight, (int)minCropSize.y);

//            // 释放旧纹理
//            if (previewRenderTexture != null)
//            {
//                previewCamera.targetTexture = null;
//                RenderTexture.ReleaseTemporary(previewRenderTexture);
//                previewRenderTexture = null;
//            }

//            try
//            {
//                // 创建新的RenderTexture
//                previewRenderTexture = RenderTexture.GetTemporary(
//                    finalCropWidth,
//                    finalCropHeight,
//                    16, // 降低深度缓冲区位数
//                    RenderTextureFormat.ARGB32
//                );

//                if (previewRenderTexture == null)
//                {
//                    Debug.LogError("RenderTexture创建失败！");
//                    return;
//                }

//                // 配置RenderTexture
//                previewRenderTexture.name = "BlockPreviewRT";
//                previewRenderTexture.autoGenerateMips = false;
//                previewRenderTexture.filterMode = FilterMode.Bilinear;
//                previewRenderTexture.Create();

//                // 绑定到摄像头和UI
//                previewCamera.targetTexture = previewRenderTexture;
//                previewBackground.texture = previewRenderTexture;

//                Debug.Log($"渲染纹理创建成功：{finalCropWidth}x{finalCropHeight}");
//                Debug.Log($"摄像头目标纹理：{(previewCamera.targetTexture != null ? "已设置" : "未设置")}");
//                Debug.Log($"RawImage纹理：{(previewBackground.texture != null ? "已设置" : "未设置")}");
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"创建RenderTexture失败：{e.Message}");
//            }
//        }
//        #endregion

//        #region 裁剪
//        // <returns>裁剪区域的宽高比</returns>
//        private float CalculateCropRatio()
//        {
//            if (!enableCrop || cropTargetHeight == 0)
//                return 1f; // 默认1:1比例

//            int finalWidth = Mathf.Max(cropTargetWidth, (int)minCropSize.x);
//            int finalHeight = Mathf.Max(cropTargetHeight, (int)minCropSize.y);
//            return (float)finalWidth / finalHeight;
//        }
//        //根据裁剪比例调整摄像头视野：确保画面按比例填充裁剪区域，避免拉伸
//        private void AdjustCameraSizeByCropRatio()
//        {
//            if (previewCamera == null || !enableCrop)
//                return;

//            // 基础视野值 × 比例系数：确保画面填充裁剪区
//            float adjustedSize = baseCameraOrthographicSize * (currentCropRatio > 1f ? 1f / currentCropRatio : 1f);
//            previewCamera.orthographicSize = Mathf.Max(adjustedSize, 0.5f); // 最小视野限制，避免过小
//        }

//        #endregion

//        #region 生成积木预览核心逻辑


//        //生成关卡积木预览：根据关卡数据中的积木配置，实例化对应积木并排列
//        private void GenerateLevelBlockPreviews()
//        {
//            BlockGenerateData levelBlockData = currentLevelData._blockGenerateData;
//            if (levelBlockData == null || levelBlockData.generateItems == null || levelBlockData.generateItems.Count == 0)
//            {
//                Debug.LogWarning($"BlockPreviewManager：关卡 {currentLevelData.LevelIndex} 未配置可用积木！");
//                return;
//            }
//            if (BlockPostion == null)
//            {
//                Debug.LogError("BlockPreviewManager：预览积木生成根节点（BlockPostion）未赋值！");
//                return;
//            }
//            // 创建预览父对象（统一管理）：便于后续批量销毁
//            GameObject previewParent = new GameObject($"BlockPreviewInstances_Level{currentLevelData.LevelIndex}");
//            previewParent.transform.parent = transform;
//            previewParent.transform.position = BlockPostion.position;
//            previewParent.transform.rotation = Quaternion.identity;

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
//                Debug.Log($"[预览过滤] 已放置: [{string.Join(", ", placedIndices ?? new List<int>())}], " +
//                         $"备选区: [{string.Join(", ", alternativeIndices ?? new List<int>())}], " +
//                         $"当前激活索引: {currentGlobalIndex}");
//            }
//            else
//            {
//                Debug.LogWarning("[预览过滤] BlockGenerate.Instance 为空！无法过滤预览");
//            }

//            // 遍历生成积木预览
//            for (int i = 0; i < levelBlockData.generateItems.Count; i++)
//            {
//                BlockGenerateItem generateItem = levelBlockData.generateItems[i];

//                // 过滤条件1：跳过已放置的方块
//                if (placedIndices != null && placedIndices.Contains(i))
//                {
//                    Debug.Log($"[预览过滤] 跳过已放置方块 索引:{i} 类型:{generateItem.blockType}");
//                    continue;
//                }

//                // 过滤条件2：跳过当前激活/查看的方块
//                if (i == currentGlobalIndex)
//                {
//                    Debug.Log($"[预览过滤] 跳过当前激活方块 索引:{i} 类型:{generateItem.blockType}");
//                    continue;
//                }
//                if (!typeToPrefabDict.TryGetValue(generateItem.blockType, out GameObject blockPrefab))
//                {
//                    Debug.LogWarning($"BlockPreviewManager：积木类型 {generateItem.blockType} 未配置预制体！");
//                    continue;
//                }

//                // 计算排列位置（中心对齐）：确保积木整体居中显示
//                int rowIndex = i / maxBlocksPerRow; // 行索引
//                int colIndex = i % maxBlocksPerRow; // 列索引
//                float totalRowWidth = (Mathf.Min(maxBlocksPerRow, levelBlockData.generateItems.Count) - 1) * blockSpacing; // 行总宽度
//                float totalColHeight = (Mathf.CeilToInt(levelBlockData.generateItems.Count / (float)maxBlocksPerRow) - 1) * blockSpacing; // 列总高度
//                float xPos = (colIndex * blockSpacing) - (totalRowWidth / 2); // X轴位置（居中）
//                float zPos = (rowIndex * blockSpacing) - (totalColHeight / 2); // Z轴位置（居中）
//                Vector3 spawnPos = new Vector3(xPos, 0, zPos - 1); // 微调Z轴避免与其他物体重叠

//                // 实例化积木并配置
//                GameObject previewBlock = Instantiate(blockPrefab, previewParent.transform);
//                previewBlock.name = $"Preview_{generateItem.blockType}_Level{currentLevelData.LevelIndex}";
//                previewBlock.transform.localPosition = spawnPos;
//                previewBlock.transform.rotation = Quaternion.Euler(blockPreviewRotation); // 应用预设旋转角度
//                previewBlock.transform.localScale = blockPrefab.transform.localScale * blockPreviewScale; // 应用缩放
//                previewBlock.layer = LayerMask.NameToLayer("BlockPreview"); // 设置专用层，便于摄像头筛选
//                SetLayerRecursively(previewBlock, LayerMask.NameToLayer("BlockPreview")); // 递归设置子物体层

//                // 配置齿轮启用状态（原逻辑保留）
//                ConfigureBlockGears(previewBlock, generateItem);

//                previewBlockInstances.Add(previewBlock);
//            }

//            // 适配裁剪区：调整摄像头视野（结合积木数量和裁剪比）
//            AdjustCameraSizeByCropAndBlockCount(levelBlockData.generateItems.Count);
//        }

//        // 结合裁剪比例和积木数量调整摄像头视野：确保所有积木在裁剪区内可见
//        private void AdjustCameraSizeByCropAndBlockCount(int blockCount)
//        {
//            if (previewCamera == null || !enableCrop)
//                return;

//            // 计算积木排列所需的基础视野
//            int rowCount = Mathf.CeilToInt(blockCount / (float)maxBlocksPerRow); // 行数
//            float maxWidth = (Mathf.Min(maxBlocksPerRow, blockCount) - 1) * blockSpacing; // 最大宽度（一行内的总间距）
//            float maxHeight = (rowCount - 1) * blockSpacing; // 最大高度（一列内的总间距）
//            float baseRequiredSize = Mathf.Max(maxWidth, maxHeight) * 0.5f; // 基础视野需求（取最大边的一半）

//            // 结合裁剪比例调整最终视野：适配裁剪区比例，避免画面拉伸
//            float finalSize = baseRequiredSize * (currentCropRatio > 1f ? 1f / currentCropRatio : 1f);
//            previewCamera.orthographicSize = Mathf.Max(finalSize, 0.5f); // 最小限制
//        }

//        //配置积木齿轮的启用状态：根据关卡配置，设置对应索引的齿轮是否激活
//        private void ConfigureBlockGears(GameObject blockObj, BlockGenerateItem generateItem)
//        {
//            // 先获取BlockControl组件
//            BlockControl blockControl = blockObj.GetComponent<BlockControl>();
//            if (blockControl == null)
//            {
//                Debug.LogWarning($"BlockPreviewManager：积木 {blockObj.name} 未挂载 BlockControl 组件，无法获取 BlockData！");
//                return;
//            }

//            BlockData blockData = blockControl._blockData;
//            if (blockData == null || blockData._gearobject == null || blockData._gearobject.Length == 0)
//            {
//                Debug.LogWarning($"BlockPreviewManager：积木 {blockObj.name} 未配置齿轮数据！");
//                return;
//            }

//            // 按索引匹配齿轮启用状态（gear1→索引0，以此类推）
//            for (int j = 0; j < blockData._gearobject.Length; j++)
//            {
//                GearData gearData = blockData._gearobject[j];
//                if (gearData == null || gearData.GearObject == null)
//                    continue;

//                // 根据齿轮索引设置启用状态
//                bool isEnabled = j switch
//                {
//                    0 => generateItem.gear1,
//                    1 => generateItem.gear2,
//                    2 => generateItem.gear3,
//                    3 => generateItem.gear4,
//                    _ => false
//                };

//                gearData.GearObject.SetActive(isEnabled);
//                gearData.IsActive = isEnabled;
//            }
//        }
//        #endregion

//        #region 通用工具方法
//        private void SetLayerRecursively(GameObject obj, int layer)
//        {
//            if (obj == null)
//                return;

//            obj.layer = layer;
//            foreach (Transform child in obj.transform)
//            {
//                SetLayerRecursively(child.gameObject, layer);
//            }
//        }
//        //销毁所有预览积木：清除当前所有生成的预览积木实例和父对象，避免残留
//        private void DestroyAllPreviewBlocks()
//        {
//            foreach (var block in previewBlockInstances)
//            {
//                if (block != null) Destroy(block);
//            }
//            previewBlockInstances.Clear();

//            // 销毁预览父对象：确保完全清理
//            foreach (var parent in GameObject.FindObjectsOfType<GameObject>(true))
//            {
//                if (parent.name.StartsWith("BlockPreviewInstances_"))
//                {
//                    Destroy(parent);
//                }
//            }
//        }
//        #endregion
//    }
//}