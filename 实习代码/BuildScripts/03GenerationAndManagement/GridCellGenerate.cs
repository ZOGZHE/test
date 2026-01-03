using UnityEngine;

namespace ConnectMaster
{
    public class GridCellGenerate : MonoBehaviour
    {
        public static GridCellGenerate Instance;
        [Header("生成核心配置")]
        public GridCellControl cellPrefab; 
        public RectTransform gridParent; // 父节点（其中心将与4×4网格中心重合）
        [Space(10)]

        [Header("网格尺寸配置")]
        public int rowCount = 4; // 生成行数
        public int colCount = 4; // 生成列数
        [Space(10)]

        [Header("格子外观配置")]
        public float cellWidth = 100f; // 格子宽度（匹配预制体）
        public float cellHeight = 100f; // 格子高度（匹配预制体）
        public float spacingX = 20f; // 格子水平间隔
        public float spacingY = 20f; // 格子垂直间隔

        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            GenerateGridCells();
        }
        #endregion

        #region 核心生成逻辑
        public void GenerateGridCells()
        {
            if (!CheckConfigValid()) return;

            ClearExistingCells();

            RectTransform parentRect = gridParent;
            // 强制父节点锚点为中心（确保定位基准统一）
            parentRect.pivot = new Vector2(0.5f, 0.5f);
            parentRect.anchorMin = new Vector2(0.5f, 0.5f);
            parentRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 生成格子：外层行（上→下），内层列（左→右）
            for (int row = 1; row <= rowCount; row++)
            {
                for (int col = 1; col <= colCount; col++)
                {
                    InstantiateAndConfigCell(row, col, parentRect);
                }
            }
        }
        //实例化格子并配置「4×4网格中心与父节点中心重合」的位置
        private void InstantiateAndConfigCell(int row, int col, RectTransform parentRect)
        {
            // 1. 实例化格子（父节点设为gridParent，保持局部坐标独立）
            GridCellControl cellInstance = Instantiate(cellPrefab, parentRect);
            RectTransform cellRect = cellInstance.GetComponent<RectTransform>();
            if (cellRect == null) return;
            // 给格子赋值行号和列号（循环变量row/col本身就是1-4的索引，完全匹配属性定义）
            cellInstance.rowIndex = row;
            cellInstance.colIndex = col;

            // 2. 统一格子锚点（确保所有格子定位基准一致，均以自身中心为锚点）
            cellRect.pivot = new Vector2(0.5f, 0.5f);
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);

            // 3. 设置格子尺寸（匹配配置参数，覆盖预制体可能的异常尺寸）
            cellRect.sizeDelta = new Vector2(cellWidth, cellHeight);

            // 4. 关键修改：固定以4×4网格的几何中心为基准
            // 4×4网格的行索引1-4，中心在第2.5行（第2行与第3行中点）
            // 4×4网格的列索引1-4，中心在第2.5列（第2列与第3列中点）
            const float base4x4CenterRow = 2.5f; // 4×4网格的行中心（固定值）
            const float base4x4CenterCol = 2.5f; // 4×4网格的列中心（固定值）

            // 5. 计算当前格子相对于4×4中心的偏移量
            // 行偏移：当前行 - 4×4行中心（上为负，下为正）
            float rowOffset = row - base4x4CenterRow;
            // 列偏移：当前列 - 4×4列中心（左为负，右为正）
            float colOffset = col - base4x4CenterCol;

            // 6. 计算最终位置（基于4×4中心偏移量，保持格子+间隔的统一间距）
            // 水平方向：每偏移1列，移动「格子宽度+水平间隔」
            float finalX = colOffset * (cellWidth + spacingX);
            // 垂直方向：Unity UI Y轴向上，行偏移取反（下偏移→Y减小）
            float finalY = -rowOffset * (cellHeight + spacingY);

            // 7. 赋值最终位置（父节点中心 = 4×4网格中心）
            cellRect.anchoredPosition = new Vector2(finalX, finalY);

            // （可选）给格子赋值行/列索引，方便后续逻辑使用
            cellInstance.gameObject.name = $"Cell_{row}x{col}"; // 调试用命名
        }
        #endregion

        #region 辅助方法
        private bool CheckConfigValid()
        {
            if (cellPrefab == null) { Debug.LogError("❌ 未赋值GridCell预制体！"); return false; }
            if (gridParent == null) { Debug.LogError("❌ 未赋值父节点！"); return false; }
            if (cellPrefab.GetComponent<RectTransform>() == null) { Debug.LogError("❌ 预制体缺少RectTransform！"); return false; }
            if (rowCount <= 0 || colCount <= 0) { Debug.LogError("❌ 行数/列数不能为0！"); return false; }
            return true;
        }

        public void ClearExistingCells()
        {
            if (gridParent == null) return;
            for (int i = gridParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(gridParent.GetChild(i).gameObject);
            }
            //Debug.Log($"🗑️ 清空父节点下所有格子");
        }
        #endregion
    }

}