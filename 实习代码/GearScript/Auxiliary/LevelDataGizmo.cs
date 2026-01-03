using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // 仅在Unity编辑器环境下引入
#endif

namespace SuperGear
{
    /// <summary>
    /// 关卡数据可视化工具类，用于在编辑模式下绘制关卡地形Gizmos和提示积木模型
    /// 特性[ExecuteInEditMode]确保脚本在编辑模式下也能执行（无需进入运行模式）
    /// </summary>
    [ExecuteInEditMode]
    public class LevelDataGizmo : MonoBehaviour
    {
        [Header("关卡数据引用")]
        [Tooltip("指定需要可视化的关卡数据资产（包含地形、积木等配置）")]
        public LevelData levelData;

        #region 地形可视化配置
        [Header("地形网格参数")]
        public float spacing = 1f; // 地形网格中元素的间距（X/Z轴）
        public Vector3 elementScale = new Vector3(0.5f, 0.1f, 0.5f); // 地形单个元素的缩放尺寸
        public Vector3 targetGearOffset = new Vector3(0, 0.2f, 0); // 目标齿轮在Y轴的偏移量（用于区分普通元素）
        public Vector3 powerGearOffset = new Vector3(0, 0.2f, 0); // 动力齿轮在Y轴的偏移量
        public float coordinateTextOffsetY = 0.3f; // 坐标文本的Y轴偏移（避免与元素重叠）

        [Header("坐标文本样式")]
        [Tooltip("坐标文本的字体大小（范围限制8-30）")]
        [Range(8, 30)] public int coordinateTextFontSize = 12;
        [Tooltip("坐标文本的颜色")]
        public Color coordinateTextColor = Color.black;
        [Tooltip("坐标文本是否显示半透明背景")]
        public bool showTextBackground = false;
        [Tooltip("坐标文本背景的颜色（含透明度）")]
        public Color textBackgroundColor = new Color(1, 1, 1, 0.5f);
        #endregion

        #region 地形Gizmos颜色
        [Header("地形Gizmos颜色")]
        public bool drawTerrainGizmos = true; // 是否启用地形Gizmos绘制
        public Color centerPointColor = Color.blue; // 地形中心点的颜色
        public Color gridBorderColor = Color.green; // 地形网格外框的颜色
        public Color normalBinColor = Color.yellow; // 普通接收柱的颜色
        public Color obstacleColor = Color.red; // 障碍物的颜色
        public Color targetGearColor = Color.cyan; // 目标齿轮的颜色
        public Color powerGearColor = Color.magenta; // 动力齿轮的颜色
        public Color missingBinColor = Color.gray; // 缺失接收柱的颜色
        #endregion

        #region 提示积木可视化配置
        [Header("提示积木参数")]
        [Tooltip("是否实例化提示积木模型（用于直观展示关卡中的积木布局）")]
        public bool drawHintModels = true;
        [Tooltip("积木类型与预制体的映射关系，用于实例化对应模型")]
        [SerializeField] private List<BlockTypeMapping> typeMappings = new List<BlockTypeMapping>();

        [Header("刷新设置")]
        [Tooltip("自动刷新提示积木的时间间隔（秒），0则禁用自动刷新")]
        [Min(0)] public float refreshInterval = 1f;
        #endregion

        // 内部状态管理
        private Dictionary<BlockType, GameObject> _typeToPrefabDict = new Dictionary<BlockType, GameObject>(); // 缓存"积木类型-预制体"的字典
        private List<GameObject> _tempInstances = new List<GameObject>(); // 存储临时实例化的提示积木
        private float _lastRefreshTime; // 上次刷新提示积木的时间戳
        private bool _lastDrawHintModels; // 缓存上次drawHintModels的状态
        private float _lastRefreshInterval; // 缓存上次refreshInterval的值
        private GUIStyle _coordinateTextStyle; // 坐标文本的样式缓存


        /// <summary>
        /// 脚本启用时调用（编辑模式下激活物体或进入运行模式时）
        /// 初始化必要的数据和状态
        /// </summary>
        private void OnEnable()
        {
            InitializeTypeMappingDict(); // 初始化类型-预制体映射字典
            _lastDrawHintModels = drawHintModels; // 缓存当前配置状态
            _lastRefreshInterval = refreshInterval;
            // InitializeTextStyle(); // 移除此处调用，延迟到OnDrawGizmos中按需初始化

            // 若启用提示积木且非运行模式，直接生成提示模型
            if (drawHintModels && !Application.isPlaying)
            {
                SpawnHintModels();
                _lastRefreshTime = Time.realtimeSinceStartup; // 记录刷新时间
            }
        }

        /// <summary>
        /// 脚本禁用时调用（物体失活或退出运行模式时）
        /// 清理临时实例化的提示积木
        /// </summary>
        private void OnDisable() => ClearTempInstances();

        /// <summary>
        /// 物体销毁时调用
        /// 确保清理所有临时实例，避免编辑器残留
        /// </summary>
        private void OnDestroy() => ClearTempInstances();

        /// <summary>
        /// Unity的Gizmos绘制回调（每帧在场景视图中执行）
        /// 负责绘制地形相关的Gizmos并检查提示积木是否需要刷新
        /// </summary>
        private void OnDrawGizmos()
        {
            if (levelData == null) return; // 若无关卡数据，不执行绘制

            // 若启用地形Gizmos，执行绘制逻辑
            if (drawTerrainGizmos)
                DrawTerrainGizmos();

            // 非运行模式下（编辑模式），检查是否需要刷新提示积木
            if (!Application.isPlaying)
                CheckAndRefresh();
        }

        /// <summary>
        /// 初始化坐标文本的样式（调用一次即可，后续通过UpdateTextStyle更新）
        /// </summary>
        private void InitializeTextStyle()
        {
            UpdateTextStyle(); // 初始时直接更新样式
        }

        /// <summary>
        /// 更新坐标文本样式（当相关属性变化时调用，确保样式与配置一致）
        /// </summary>
        private void UpdateTextStyle()
        {
#if UNITY_EDITOR
            // 安全检查：EditorStyles.label在编辑器启动早期可能未初始化
            if (EditorStyles.label == null)
                return;

            if (_coordinateTextStyle == null)
                _coordinateTextStyle = new GUIStyle(EditorStyles.label); // 基于默认标签样式创建

            // 更新字体大小和文本颜色
            _coordinateTextStyle.fontSize = coordinateTextFontSize;
            _coordinateTextStyle.normal.textColor = coordinateTextColor;

            // 根据配置设置文本背景
            if (showTextBackground)
            {
                // 创建背景纹理并设置内边距
                _coordinateTextStyle.normal.background = MakeTex(2, 2, textBackgroundColor);
                _coordinateTextStyle.padding = new RectOffset(2, 2, 1, 1); // 文本与背景边缘的间距
            }
            else
            {
                // 禁用背景时清除纹理和内边距
                _coordinateTextStyle.normal.background = null;
                _coordinateTextStyle.padding = new RectOffset(0, 0, 0, 0);
            }
#endif
        }

        /// <summary>
        /// 创建简单的纯色纹理（用于文本背景）
        /// </summary>
        /// <param name="width">纹理宽度</param>
        /// <param name="height">纹理高度</param>
        /// <param name="col">纹理颜色（含透明度）</param>
        /// <returns>创建的纯色纹理</returns>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height]; // 纹理像素数组
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col; // 填充颜色
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix); // 设置像素
            result.Apply(); // 应用修改
            return result;
        }

        /// <summary>
        /// 检查是否需要刷新提示积木（编辑模式下）
        /// 触发条件：配置变化或达到自动刷新间隔
        /// </summary>
        private void CheckAndRefresh()
        {
            if (Application.isPlaying) return; // 运行模式下不执行

            // 若禁用提示积木，清理现有实例并返回
            if (!drawHintModels)
            {
                if (_tempInstances.Count > 0) ClearTempInstances();
                return;
            }

            // 检测配置是否变化（绘制开关、刷新间隔、类型映射）
            bool configChanged =
                _lastDrawHintModels != drawHintModels || // 绘制开关状态变化
                !Mathf.Approximately(_lastRefreshInterval, refreshInterval) || // 刷新间隔变化
                CheckTypeMappingsChanged(); // 类型映射关系变化

            // 检测是否达到自动刷新间隔
            bool intervalReached = refreshInterval > 0 &&
                                 Time.realtimeSinceStartup - _lastRefreshTime >= refreshInterval;

            // 若满足任一条件，刷新提示积木
            if (configChanged || intervalReached)
            {
                ClearTempInstances(); // 清理旧实例
                SpawnHintModels(); // 生成新实例
                // 更新缓存状态
                _lastRefreshTime = Time.realtimeSinceStartup;
                _lastDrawHintModels = drawHintModels;
                _lastRefreshInterval = refreshInterval;
            }
        }

        /// <summary>
        /// 检查"积木类型-预制体"映射关系是否发生变化
        /// </summary>
        /// <returns>true表示映射有变化，false表示无变化</returns>
        private bool CheckTypeMappingsChanged()
        {
            // 数量不同直接判定为变化
            if (typeMappings.Count != _typeToPrefabDict.Count) return true;

            // 逐个检查映射关系是否一致
            foreach (var mapping in typeMappings)
            {
                // 若字典中无此类型，或预制体不匹配，判定为变化
                if (!_typeToPrefabDict.ContainsKey(mapping.blockType) ||
                    _typeToPrefabDict[mapping.blockType] != mapping.blockPrefab)
                {
                    InitializeTypeMappingDict(); // 重新初始化字典（同步最新映射）
                    return true;
                }
            }
            return false; // 所有映射一致，无变化
        }

        /// <summary>
        /// 上下文菜单方法（在Inspector右键菜单中显示）
        /// 用于手动刷新提示积木（方便编辑时立即更新）
        /// </summary>
        [ContextMenu("手动刷新提示积木")]
        public void ManualRefreshHintModels()
        {
            if (drawHintModels && !Application.isPlaying)
            {
                ClearTempInstances();
                SpawnHintModels();
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
            else if (Application.isPlaying)
            {
                ClearTempInstances(); // 运行模式下仅清理实例
            }
        }

        #region 地形可视化实现（含坐标显示）
        /// <summary>
        /// 绘制地形相关的Gizmos（中心点、网格、元素、坐标文本等）
        /// </summary>
        private void DrawTerrainGizmos()
        {
            if (levelData._receivingBinGenerateData == null) return; // 若无地形数据，不绘制

            UpdateTextStyle(); // 每次绘制前更新文本样式（确保配置变化立即生效）

            ReceivingBinGenerateData binData = levelData._receivingBinGenerateData;

            // 绘制地形中心点（蓝色球体）
            Gizmos.color = centerPointColor;
            Gizmos.DrawSphere(binData.centerPosition, 0.15f);

            // 计算网格外框尺寸并绘制（绿色线框）
            float totalXLength = (binData.xRowCount - 1) * spacing; // X轴总长度（元素数量-1乘以间距）
            float totalZLength = (binData.zColumnCount - 1) * spacing; // Z轴总长度
            Vector3 gridBounds = new Vector3(totalXLength, 0.05f, totalZLength); // Y轴高度为0.05（避免与地面重叠）
            Gizmos.color = gridBorderColor;
            Gizmos.DrawWireCube(binData.centerPosition, gridBounds);

            // 计算网格起始偏移（基于中心点的0索引起始位置）
            float xStartOffset = -(binData.xRowCount - 1) * spacing / 2f; // X轴起始偏移（左移半长）
            float zStartOffset = -(binData.zColumnCount - 1) * spacing / 2f; // Z轴起始偏移

            // 遍历所有网格元素，绘制单个元素和坐标
            for (int x = 0; x < binData.xRowCount; x++)
            {
                for (int z = 0; z < binData.zColumnCount; z++)
                {
                    // 计算元素基础位置（基于中心点和偏移量）
                    Vector3 basePos = binData.centerPosition + new Vector3(
                        xStartOffset + x * spacing, // X轴位置：起始偏移 + 当前索引*间距
                        0f,
                        zStartOffset + z * spacing  // Z轴位置：起始偏移 + 当前索引*间距
                    );

                    // 根据元素类型调整最终位置（齿轮叠加偏移）
                    Vector3 finalPos = basePos;
                    if (IsPositionInArray(binData.targetGearPositions, x, z))
                        finalPos += targetGearOffset;
                    else if (IsPositionInArray(binData.powerGearPositions, x, z))
                        finalPos += powerGearOffset;

                    // 设置Gizmo颜色并绘制（线框+半透明实体）
                    SetGizmoColorByType(binData, x, z);
                    Gizmos.DrawWireCube(finalPos, elementScale); // 线框（边框）
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f); // 降低透明度
                    Gizmos.DrawCube(finalPos, elementScale); // 实体（填充）

                    // 仅在编辑模式下绘制坐标文本
#if UNITY_EDITOR
                    DrawCoordinateText(finalPos, x, z);
#endif
                }
            }
        }

        /// <summary>
        /// 在地形元素上方绘制1索引坐标文本（便于编辑时定位）
        /// </summary>
        /// <param name="elementPos">元素的世界位置</param>
        /// <param name="x0">元素的0索引X坐标（循环变量）</param>
        /// <param name="z0">元素的0索引Z坐标（循环变量）</param>
        private void DrawCoordinateText(Vector3 elementPos, int x0, int z0)
        {
#if UNITY_EDITOR
            // 转换为1索引坐标（编辑器中通常以1开始计数）
            int x1 = x0 + 1;
            int z1 = z0 + 1;
            string coordText = $"({x1},{z1})"; // 格式化坐标文本

            // 计算文本位置（在元素上方偏移）
            Vector3 textPos = elementPos + new Vector3(0, coordinateTextOffsetY, 0);

            // 使用自定义样式绘制文本（通过Handles.Label在场景视图中显示）
            // 防御性检查：如果样式未初始化（EditorStyles早期不可用），跳过文本绘制
            if (_coordinateTextStyle != null)
                Handles.Label(textPos, coordText, _coordinateTextStyle);
#endif
        }

        /// <summary>
        /// 根据元素类型设置Gizmo颜色（区分普通、障碍、齿轮等）
        /// </summary>
        /// <param name="data">地形数据</param>
        /// <param name="x">元素的0索引X坐标</param>
        /// <param name="z">元素的0索引Z坐标</param>
        private void SetGizmoColorByType(ReceivingBinGenerateData data, int x, int z)
        {
            if (IsPositionInArray(data.missingBinPositions, x, z))
                Gizmos.color = missingBinColor;
            else if (IsPositionInArray(data.targetGearPositions, x, z))
                Gizmos.color = targetGearColor;
            else if (IsPositionInArray(data.powerGearPositions, x, z))
                Gizmos.color = powerGearColor;
            else if (IsPositionInArray(data.obstaclePositions, x, z))
                Gizmos.color = obstacleColor;
            else
                Gizmos.color = normalBinColor; // 默认普通接收柱
        }

        /// <summary>
        /// 检查目标位置（0索引）是否存在于存储1索引坐标的数组中
        /// （适配数据中坐标以1索引存储的情况）
        /// </summary>
        /// <param name="array">存储1索引坐标的数组（Vector2Int.x=X坐标，y=Z坐标）</param>
        /// <param name="x0">目标0索引X坐标</param>
        /// <param name="z0">目标0索引Z坐标</param>
        /// <returns>true表示存在，false表示不存在</returns>
        private bool IsPositionInArray(Vector2Int[] array, int x0, int z0)
        {
            if (array == null || array.Length == 0) return false;

            foreach (var pos1 in array)
            {
                // 将数组中的1索引坐标转为0索引，与循环变量（0索引）匹配
                int x1 = pos1.x; // 1索引X
                int z1 = pos1.y; // 1索引Z
                if (x1 - 1 == x0 && z1 - 1 == z0)
                    return true;
            }
            return false;
        }
        #endregion


        #region 提示积木实例化逻辑（核心修改部分）
        /// <summary>
        /// 深拷贝BlockData，避免多个积木实例共用同一引用
        /// </summary>
        private BlockData CopyBlockData(BlockData original)
        {
            if (original == null) return null;

            BlockData copy = new BlockData();
            // 复制基础字段
            copy._blockType = original._blockType;
            copy.gear1 = original.gear1;
            copy.gear2 = original.gear2;
            copy.gear3 = original.gear3;
            copy.gear4 = original.gear4;

            // 复制插槽列表（值类型，直接添加）
            copy.SlotPoints.Clear();
            foreach (var slotPoint in original.SlotPoints)
            {
                copy.SlotPoints.Add(slotPoint);
            }

            // 深拷贝GearData数组（关键：避免齿轮数据共用）
            if (original._gearobject != null && original._gearobject.Length > 0)
            {
                copy._gearobject = new GearData[original._gearobject.Length];
                for (int i = 0; i < original._gearobject.Length; i++)
                {
                    GearData originalGear = original._gearobject[i];
                    if (originalGear == null) continue;

                    // 复制单个GearData的所有属性
                    GearData gearCopy = new GearData();
                    gearCopy.GearObject = originalGear.GearObject;
                    gearCopy.CurrentDirection = originalGear.CurrentDirection;
                    gearCopy.RotationSpeed = originalGear.RotationSpeed;
                    gearCopy.IsRotating = originalGear.IsRotating;
                    gearCopy.IsActive = originalGear.IsActive;
                    gearCopy.IsShowForHint = originalGear.IsShowForHint;

                    copy._gearobject[i] = gearCopy;
                }
            }

            return copy;
        }

        /// <summary>
        /// 实例化提示积木模型（完全基于DemonstrationBlock配置）
        /// 仅在编辑模式下执行，实例化的物体标记为不保存（避免污染场景）
        /// </summary>
        private void SpawnHintModels()
        {
            if (Application.isPlaying) return; // 运行模式下不实例化

            // 数据完整性检查（重点校验DemonstrationBlock）
            if (levelData == null
                || levelData.demonstrationBlocks == null
                || levelData.demonstrationBlocks.Count == 0
                || typeMappings.Count == 0)
            {
                Debug.LogWarning("LevelDataGizmo：关卡数据/预设解法/预制体映射不完整，无法生成提示积木", this);
                return;
            }

            InitializeTypeMappingDict(); // 确保映射字典最新

            // 创建临时父物体（用于统一管理提示积木实例）
            GameObject tempParent = new GameObject($"HintModels_{DateTime.Now.Ticks}"); // 用时间戳命名避免重复
            tempParent.transform.SetParent(transform); // 挂载到当前物体下
            tempParent.hideFlags = HideFlags.DontSave; // 标记为不保存（退出编辑模式后自动清理）
            _tempInstances.Add(tempParent); // 加入临时实例列表

            // 遍历所有预设解法积木，实例化并配置
            foreach (var demoBlock in levelData.demonstrationBlocks)
            {
                // 1. 查找当前积木类型对应的预制体
                if (!_typeToPrefabDict.TryGetValue(demoBlock.blockType, out GameObject blockPrefab))
                {
                    Debug.LogWarning($"LevelDataGizmo：未找到 {demoBlock.blockType} 对应的预制体映射", this);
                    continue;
                }

                // 2. 实例化预制体到临时父物体下
                GameObject instance = Instantiate(blockPrefab, tempParent.transform);
                instance.hideFlags = HideFlags.DontSave; // 标记为不保存
                instance.name = $"{demoBlock.blockType}_Hint"; // 命名便于识别
                // 设置位置、旋转（使用预制体原始缩放，避免拉伸）
                instance.transform.position = demoBlock.worldPosition;
                instance.transform.rotation = Quaternion.Euler(demoBlock.worldRotation);
                instance.transform.localScale = blockPrefab.transform.localScale;
                _tempInstances.Add(instance); // 加入临时实例列表

                // 3. 获取积木控制组件，深拷贝BlockData避免引用共用
                BlockControl blockControl = instance.GetComponent<BlockControl>();
                if (blockControl == null || blockControl._blockData == null)
                {
                    Debug.LogWarning($"LevelDataGizmo：积木预制体 {blockPrefab.name} 缺少 BlockControl 或 BlockData", this);
                    continue;
                }
                // 深拷贝BlockData，替换实例的原始数据（核心：解决共用问题）
                blockControl._blockData = CopyBlockData(blockControl._blockData);

                // 4. 直接使用DemonstrationBlock的配置初始化齿轮
                ConfigureBlockGears(instance, demoBlock);
            }
        }

        /// <summary>
        /// 配置积木的齿轮状态（完全基于DemonstrationBlock的gear1-gear4）
        /// </summary>
        private void ConfigureBlockGears(GameObject blockObj, DemonstrationBlock demoBlock)
        {
            // 1. 获取积木的控制组件和深拷贝后的BlockData
            BlockControl blockControl = blockObj.GetComponent<BlockControl>();
            if (blockControl == null)
            {
                Debug.LogWarning($"LevelDataGizmo：积木 {blockObj.name} 缺少 BlockControl 组件", this);
                return;
            }

            BlockData blockData = blockControl._blockData;
            if (blockData == null || blockData._gearobject == null || blockData._gearobject.Length == 0)
            {
                Debug.LogWarning($"LevelDataGizmo：积木 {blockObj.name} 未配置 BlockData 或齿轮数据", this);
                return;
            }

            // 2. 遍历所有齿轮，按DemonstrationBlock的配置启用/禁用
            for (int j = 0; j < blockData._gearobject.Length; j++)
            {
                GearData gearData = blockData._gearobject[j];
                if (gearData == null || gearData.GearObject == null)
                    continue;

                // 根据齿轮索引匹配DemonstrationBlock的配置（超过4个齿轮默认禁用）
                bool isGearEnabled = j switch
                {
                    0 => demoBlock.gear1,
                    1 => demoBlock.gear2,
                    2 => demoBlock.gear3,
                    3 => demoBlock.gear4,
                    _ => false
                };

                // 3. 设置齿轮状态：激活/隐藏 + 禁用交互
                gearData.GearObject.SetActive(isGearEnabled);
                gearData.IsActive = false; // 提示齿轮禁用交互
                gearData.IsShowForHint = true; // 标记为提示用齿轮，禁止旋转

                // 4. 控制齿轮组件：强制停止旋转 + 同步提示标记
                GearControl gearControl = gearData.GearObject.GetComponent<GearControl>();
                if (gearControl != null)
                {
                    gearControl.IsShowForHint = true;
                    gearControl.StopRotation(); // 清除残留旋转状态
                }
            }
        }

        /// <summary>
        /// 清理所有临时实例化的提示积木
        /// 根据运行状态使用不同的销毁方法（编辑模式用DestroyImmediate，运行模式用Destroy）
        /// </summary>
        private void ClearTempInstances()
        {
            foreach (var instance in _tempInstances)
            {
                if (instance == null) continue;

#if UNITY_EDITOR
                // 编辑模式下立即销毁，运行模式下延迟销毁
                if (Application.isPlaying)
                    Destroy(instance);
                else
                    DestroyImmediate(instance);
#else
                Destroy(instance); // 非编辑器环境下直接销毁
#endif
            }
            _tempInstances.Clear(); // 清空列表
        }

        /// <summary>
        /// 初始化"积木类型-预制体"映射字典（从typeMappings同步数据）
        /// 处理空预制体的警告，避免重复添加相同类型
        /// </summary>
        private void InitializeTypeMappingDict()
        {
            _typeToPrefabDict.Clear(); // 先清空现有数据
            foreach (var mapping in typeMappings)
            {
                if (mapping.blockPrefab == null)
                {
                    Debug.LogWarning($"积木类型 {mapping.blockType} 的预制体未赋值", this);
                    continue;
                }

                // 仅添加未包含的类型（避免重复）
                if (!_typeToPrefabDict.ContainsKey(mapping.blockType))
                {
                    _typeToPrefabDict.Add(mapping.blockType, mapping.blockPrefab);
                }
            }
        }
        #endregion
    }

    // 补充：积木类型-预制体映射类（原代码可能遗漏，需添加）
    [System.Serializable]
    public class BlockTypeMapping
    {
        public BlockType blockType;
        public GameObject blockPrefab;
    }

#if UNITY_EDITOR
    /// <summary>
    /// LevelDataGizmo的自定义编辑器类
    /// 扩展Inspector界面，添加手动刷新按钮
    /// </summary>
    [CustomEditor(typeof(LevelDataGizmo))]
    public class LevelDataGizmoEditor : Editor
    {
        /// <summary>
        /// 绘制Inspector界面
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); // 绘制默认 Inspector 控件
            LevelDataGizmo levelDataGizmo = (LevelDataGizmo)target; // 获取目标组件

            GUILayout.Space(10); // 增加间距
            // 添加手动刷新按钮，点击时调用刷新方法
            if (GUILayout.Button("手动刷新提示积木", GUILayout.Height(30)))
            {
                levelDataGizmo.ManualRefreshHintModels();
                Repaint(); // 重绘Inspector
            }
        }
    }
#endif
}
