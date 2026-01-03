using UnityEngine;
using WoolyPath;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;

namespace WoolyPath
{
    [RequireComponent(typeof(Transform))]
    public class CollectorConveyorGizmo : MonoBehaviour
    {
        [Header("��������")]
        [Tooltip("�����Ĺؿ����ݣ����븳ֵ�������޷���ʾ��")]
        public LevelData targetLevelData;

        [Tooltip("ȫ������ϵ����Ӱ������Gizmo�Ĵ�С��")]
        public float globalScale = 1f;

        // ���ο��ӻ�����
        [Header("���ο��ӻ�����")]
        [Tooltip("�Ƿ���ʾ����Gizmo")]
        public bool showTerrain = true;

        [Tooltip("����Gizmoʵ����ɫ")]
        public Color terrainSolidColor = new Color(0.6f, 0.4f, 0.2f, 0.3f); // ����ɫ��͸��

        [Tooltip("�Ƿ���ʾ������Ϣ��ǩ")]
        public bool showTerrainLabel = true;

        [Tooltip("���α�ǩƫ�Ƹ߶ȣ�����ڵ��ζ�����")]
        public float terrainLabelYOffset = 0.5f;

        // ���γߴ���ڲ���
        [Header("���γߴ�����")]
        [Tooltip("���ο��ȣ�X�ᣩ")]
        public float terrainWidth = 10f;

        [Tooltip("���γ��ȣ�Z�ᣩ")]
        public float terrainLength = 10f;

        [Tooltip("���θ߶ȣ�Y�ᣬ�������κ�ȣ�")]
        public float terrainHeight = 0.5f;


        [Header("�ռ������ӻ�����")]
        [Tooltip("�Ƿ���ʾ�ռ���")]
        public bool showCollectors = true;

        [Tooltip("�ռ���������С")]
        public float collectorBaseSize = 0.5f;

        [Tooltip("�ռ����߶ȱ���������ڻ�����С��")]
        public float collectorHeightRatio = 0.2f;

        [Tooltip("�ռ�����������ϵ��������Z���������ı仯���ȣ�")]
        public float collectorCapacityScale = 0.2f;

        [Tooltip("�Ƿ�ʹ���Զ�����ɫ������ɫģʽ")]
        public bool useCustomCollectorColors = false;

        [Tooltip("�ռ����Զ�����ɫ������useCustomCollectorColorsΪtrueʱ��Ч��")]
        public CollectorColorSettings customCollectorColors = new CollectorColorSettings();

        [Tooltip("�ռ����߿���ɫ")]
        public Color collectorWireframeColor = Color.white;


        [Header("���ʹ�·�����ӻ�����")]
        [Tooltip("�Ƿ���ʾ���ʹ�·��")]
        public bool showConveyorPath = true;

        [Tooltip("���ʹ�·����ɫ")]
        public Color conveyorPathColor = new Color(0f, 1f, 1f, 0.8f);

        [Tooltip("·�����С")]
        public float pathPointSize = 0.1f;


        [Header("���ʹ��ڵ���ӻ�����")]
        [Tooltip("�Ƿ���ʾ���ʹ��ڵ㣨���/�ռ��㣩")]
        public bool showConveyorNodes = true;

        [Tooltip("�ڵ��С")]
        public float nodeSize = 0.2f;

        [Tooltip("��ڽڵ���ɫ")]
        public Color entryNodeColor = new Color(1f, 0.2f, 0.2f, 1f);

        [Tooltip("�ռ���ڵ���ɫ")]
        public Color collectNodeColor = new Color(0.2f, 1f, 0.2f, 1f);


        [Header("���ӹ�ϵ���ӻ�����")]
        [Tooltip("�Ƿ���ʾ�ռ������ռ����������")]
        public bool showCollectorLinks = true;

        [Tooltip("��������ɫ")]
        public Color linkLineColor = new Color(0f, 1f, 1f, 0.5f);


        [Header("��ǩ���ӻ�����")]
        [Tooltip("�Ƿ���ʾ���б�ǩ")]
        public bool showLabels = true;

        [Tooltip("��ǩ�����С")]
        public int labelFontSize = 10;

        [Tooltip("��ǩ������ɫ")]
        public Color labelTextColor = Color.white;

        [Tooltip("��ǩ����͸����")]
        [Range(0, 1)] public float labelBackgroundAlpha = 0.7f;

        [Tooltip("��ǩƫ�Ƹ߶ȣ���������壩")]
        public float labelYOffset = 0.3f;


        [Header("��������������ӻ�����")]
        [Tooltip("�Ƿ���ʾ�������ɵ���������")]
        public bool showSheepGrid = true;

        [Tooltip("�����������ת�Ƕȣ�ŷ���ǣ�����X/Y/Z����ת��")]
        public Vector3 sheepGridRotation = Vector3.zero;

        [Tooltip("�����߿���ɫ")]
        public Color gridWireframeColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

        [Tooltip("����ʵ�������ɫ����͸���������ڵ���")]
        public Color gridSolidColor = new Color(0.3f, 0.5f, 0.7f, 0.1f);

        [Tooltip("�Ƿ���ʾ������Ϣ�ܱ�ǩ���ߴ硢��ࡢ��ʼλ�ã�")]
        public bool showSheepGridTotalLabel = true;

        [Tooltip("�Ƿ���ʾÿ������Ԫ��������ǩ")]
        public bool showSheepGridCellLabels = false;

        [Tooltip("����Ԫ���ǩƫ�Ƹ߶�")]
        public float gridCellLabelYOffset = 0.2f;

        [Header("��Ԫ���߿����ã���̬��ʾ��")]
        [Tooltip("��Ԫ���߿���ɫ")]
        public Color cellWireframeColor = Color.yellow;

        [Tooltip("��Ԫ���߿��С������ڵ�Ԫ���С�ı�����")]
        [Range(0.5f, 0.95f)] public float cellWireframeSize = 0.8f;


        [Header("��ɫģʽ����")]
        [Tooltip("Gizmo��ɫģʽ��ѡ������Ϸ��һ�»����Ŀ����ɫ��")]
        public ColorMode currentColorMode = ColorMode.VibrantOpaque;


        /// <summary>
        /// ��ɫģʽö��
        /// </summary>
        public enum ColorMode
        {
            [Tooltip("�߱��Ͳ�͸��ɫ��Scene��ͼ�и���Ŀ��")]
            VibrantOpaque,
            [Tooltip("��Ϸ��ԭʼ��ɫ")]
            OriginalGameColor
        }

        /// <summary>
        /// �ռ�����ɫ������
        /// </summary>
        [System.Serializable]
        public class CollectorColorSettings
        {
            public Color Green = new Color(0.2f, 0.8f, 0.2f, 1f);
            public Color Yellow = new Color(0.9f, 0.9f, 0.2f, 1f);
            public Color Pink = new Color(1f, 0.3f, 0.7f, 1f);
            public Color Orange = new Color(1f, 0.5f, 0.1f, 1f);
            public Color Blue = new Color(0.2f, 0.5f, 1f);
            public Color Purple = new Color(0.8f, 0.2f, 0.8f, 1f);
            public Color Black = new Color(0.2f, 0.2f, 0.2f, 1f);
            public Color Default = Color.gray;
        }


        private void OnDrawGizmos()
        {
            if (targetLevelData == null)
                return;

            // �����ռ���
            if (showCollectors)
                DrawCollectorGizmos();

            // ���ƴ��ʹ�
            if (showConveyorPath || showConveyorNodes)
                DrawConveyorGizmos();

            // ���Ƶ���
            if (showTerrain)
                DrawTerrainGizmos();

            // ��������������������
            if (showSheepGrid)
                DrawSheepGridGizmos();

            // ��̬��ʾ������ÿ����Ԫ����߿�
            DrawCellWireframes();
        }

        /// <summary>
        /// ����ÿ����Ԫ����߿�����������Ϊ���������ģ�
        /// </summary>
        private void DrawCellWireframes()
        {
            if (targetLevelData == null)
                return;

            Vector2Int gridSize = targetLevelData.GridSize;
            if (gridSize.x <= 0 || gridSize.y <= 0)
                return;

            Gizmos.color = cellWireframeColor;
            Matrix4x4 originalGizmosMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(targetLevelData.GridStartPosition, Quaternion.Euler(sheepGridRotation), Vector3.one);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    // ����������(x,y)��Ϊ����������
                    Vector3 cellCenter = new Vector3(
                        x * targetLevelData.GridSpacing.x,
                        0,
                        y * targetLevelData.GridSpacing.y
                    );

                    // �߿��С��������������ñ���
                    float wireframeSizeX = targetLevelData.GridSpacing.x * cellWireframeSize;
                    float wireframeSizeZ = targetLevelData.GridSpacing.y * cellWireframeSize;

                    Gizmos.DrawWireCube(
                        cellCenter + Vector3.up * 0.01f,
                        new Vector3(wireframeSizeX, 0.1f, wireframeSizeZ)
                    );
                }
            }

            Gizmos.matrix = originalGizmosMatrix;
        }

        /// <summary>
        /// ����������ת��Ϊ�������꣨��������Ϊ���������ģ�
        /// </summary>
        private Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            if (targetLevelData == null)
                return Vector3.zero;

            // ��������ֱ�Ӷ�Ӧ����������λ��
            Vector3 localOffset = new Vector3(
                gridPos.x * targetLevelData.GridSpacing.x,
                0,
                gridPos.y * targetLevelData.GridSpacing.y
            );

            Quaternion gridRotation = Quaternion.Euler(sheepGridRotation);
            Vector3 rotatedOffset = gridRotation * localOffset;

            return targetLevelData.GridStartPosition + rotatedOffset;
        }


        /// <summary>
        /// ���Ƶ���Gizmo
        /// </summary>
        private void DrawTerrainGizmos()
        {
            if (targetLevelData._terrainPreset == null)
            {
                Debug.LogWarning($"[CollectorConveyorGizmo] �����Ĺؿ����ݡ�{targetLevelData.name}��δ����TerrainPreset������LevelData�и�ֵ��");
                return;
            }

            TerrainPreset terrainPreset = targetLevelData._terrainPreset;
            Vector3 terrainPos = terrainPreset.GenerationLocation;
            Quaternion terrainRot = Quaternion.Euler(terrainPreset.GenerationRotation);

            float scaledWidth = terrainWidth * globalScale;
            float scaledLength = terrainLength * globalScale;
            float scaledHeight = terrainHeight * globalScale;
            Vector3 terrainSize = new Vector3(scaledWidth, scaledHeight, scaledLength);

            Gizmos.color = terrainSolidColor;
            Gizmos.matrix = Matrix4x4.TRS(terrainPos, terrainRot, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, terrainSize);

            if (showTerrainLabel && showLabels)
            {
                Vector3 labelPos = terrainPos + Vector3.up * (scaledHeight / 2 + terrainLabelYOffset * globalScale);
                string labelText = $"��������: {terrainPreset.mapType}\n�ߴ�: {scaledWidth:F1}x{scaledLength:F1}\nλ��: {terrainPos.ToString("F2")}";
                #if UNITY_EDITOR
                Handles.Label(labelPos, labelText, GetLabelStyle());
                #endif
            }
        }


        /// <summary>
        /// �����ռ���Gizmo
        /// </summary>
        private void DrawCollectorGizmos()
        {
            if (targetLevelData.Collectors == null)
                return;

            foreach (var collector in targetLevelData.Collectors)
            {
                Color collectorColor = GetCollectorColor(collector.targetColor);
                Gizmos.color = collectorColor;

                float scaledBase = collectorBaseSize * globalScale;
                float sizeX = scaledBase;
                float sizeY = scaledBase * collectorHeightRatio;
                float sizeZ = scaledBase * (1 +  collectorCapacityScale);
                Vector3 gizmoSize = new Vector3(sizeX, sizeY, sizeZ);

                Gizmos.DrawCube(collector.position, gizmoSize);
                Gizmos.color = collectorWireframeColor;
                Gizmos.DrawWireCube(collector.position, gizmoSize);

                if (showLabels)
                {
                    Vector3 labelPos = collector.position + new Vector3(0, sizeY + labelYOffset * globalScale, 0);
                    #if UNITY_EDITOR
                Handles.Label(labelPos, $"����: {collector.capacity}", GetLabelStyle());
                    #endif
                }
            }
        }


        /// <summary>
        /// ���ƴ��ʹ�Gizmo
        /// </summary>
        private void DrawConveyorGizmos()
        {
            if (targetLevelData.conveyorPath == null || targetLevelData.conveyorPath.pathPoints == null)
                return;

            var conveyorData = targetLevelData.conveyorPath;
            int pathPointCount = conveyorData.pathPoints.Length;

            if (showConveyorPath && pathPointCount > 1)
            {
                Gizmos.color = conveyorPathColor;
                for (int i = 0; i < pathPointCount - 1; i++)
                {
                    Gizmos.DrawLine(conveyorData.pathPoints[i], conveyorData.pathPoints[i + 1]);
                }

                foreach (var point in conveyorData.pathPoints)
                {
                    Gizmos.DrawSphere(point, pathPointSize * globalScale);
                }
            }

            if (showConveyorNodes)
            {
                // ��ڽڵ�
                Gizmos.color = entryNodeColor;
                Gizmos.DrawSphere(conveyorData.entry, nodeSize * globalScale);
                if (showLabels)
                {
                    Vector3 entryLabelPos = conveyorData.entry + new Vector3(0, labelYOffset * globalScale, 0);
                    #if UNITY_EDITOR
                Handles.Label(entryLabelPos, "���ʹ����", GetLabelStyle());
                    #endif
                }

                // �ռ���ڵ�
                Gizmos.color = collectNodeColor;
                Gizmos.DrawSphere(conveyorData.collect, nodeSize * globalScale);
                if (showLabels)
                {
                    Vector3 collectLabelPos = conveyorData.collect + new Vector3(0, labelYOffset * globalScale, 0);
                    #if UNITY_EDITOR
                Handles.Label(collectLabelPos, "��ë�ռ���", GetLabelStyle());
                    #endif
                }
            }

            if (showCollectorLinks && targetLevelData.Collectors != null)
            {
                Gizmos.color = linkLineColor;
                foreach (var collector in targetLevelData.Collectors)
                {
                    Gizmos.DrawLine(collector.position, conveyorData.collect);
                }
            }
        }


        /// <summary>
        /// ����������������������������������Ϊ���������ģ�
        /// </summary>
        private void DrawSheepGridGizmos()
        {
            if (targetLevelData == null) return;
            Vector2Int gridSize = targetLevelData.GridSize;
            Vector2 gridSpacing = targetLevelData.GridSpacing;
            Vector3 gridStartPos = targetLevelData.GridStartPosition;

            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                Debug.LogWarning($"[CollectorConveyorGizmo] �ؿ���{targetLevelData.name}��������ߴ粻�Ϸ��������0��");
                return;
            }

            // ������������ߴ磨��(0,0)��(max,max)Ϊ�Խǵ����������ģ�
            float totalWidth = (gridSize.x - 1) * gridSpacing.x;
            float totalLength = (gridSize.y - 1) * gridSpacing.y;

            Quaternion gridRotation = Quaternion.Euler(sheepGridRotation);
            Matrix4x4 gridMatrix = Matrix4x4.TRS(gridStartPos, gridRotation, Vector3.one);
            Matrix4x4 originalGizmosMatrix = Gizmos.matrix;
            Gizmos.matrix = gridMatrix;

            // �����������ģ��ֲ����꣩
            Vector3 gridLocalCenter = new Vector3(totalWidth / 2, 0, totalLength / 2);
            Vector3 gridHalfExtents = new Vector3(totalWidth / 2, 0.01f, totalLength / 2);

            // 1. ��������ʵ��
            Gizmos.color = gridSolidColor;
            Gizmos.DrawCube(gridLocalCenter, gridHalfExtents * 2);

            // 2. ���������߿�
            Gizmos.color = gridWireframeColor;
            DrawGridWireframes(Vector3.zero, gridSize, gridSpacing);

            Gizmos.matrix = originalGizmosMatrix;

            // 3. ��������Ϣ��ǩ
            if (showSheepGridTotalLabel && showLabels)
            {
                Vector3 worldCenter = gridMatrix.MultiplyPoint(gridLocalCenter);
                Vector3 totalLabelPos = worldCenter + Vector3.up * (gridCellLabelYOffset + 0.3f) * globalScale;
                string totalLabelText = $"��������\n�ߴ�: {gridSize.x}��{gridSize.y}��Ԫ��\n���: {gridSpacing.x}��{gridSpacing.y}\n��ת: {sheepGridRotation.ToString("F1")}";
                #if UNITY_EDITOR
                Handles.Label(totalLabelPos, totalLabelText, GetLabelStyle());
                #endif
            }

            // 4. ���Ƶ�Ԫ�������ǩ
            if (showSheepGridCellLabels && showLabels)
            {
                DrawGridCellLabels(gridStartPos, gridSize, gridSpacing, gridRotation);
            }
        }

        /// <summary>
        /// ����������Χ�߿��������Ķ�λ��
        /// </summary>
        private void DrawGridWireframes(Vector3 startPos, Vector2Int gridSize, Vector2 spacing)
        {
            float cellWidth = spacing.x;
            float cellLength = spacing.y;

            // ��������߽磨�����Ե�Ԫ������Ϊ�����ģʽ��
            float minX = startPos.x;
            float maxX = startPos.x + (gridSize.x - 1) * cellWidth;
            float minZ = startPos.z;
            float maxZ = startPos.z + (gridSize.y - 1) * cellLength;

            // �������
            Gizmos.DrawLine(new Vector3(minX, startPos.y, minZ), new Vector3(maxX, startPos.y, minZ));
            Gizmos.DrawLine(new Vector3(maxX, startPos.y, minZ), new Vector3(maxX, startPos.y, maxZ));
            Gizmos.DrawLine(new Vector3(maxX, startPos.y, maxZ), new Vector3(minX, startPos.y, maxZ));
            Gizmos.DrawLine(new Vector3(minX, startPos.y, maxZ), new Vector3(minX, startPos.y, minZ));

            // ����ָ��ߣ��У�
            for (int col = 1; col < gridSize.x; col++)
            {
                float currentX = startPos.x + col * cellWidth;
                Gizmos.DrawLine(new Vector3(currentX, startPos.y, minZ), new Vector3(currentX, startPos.y, maxZ));
            }

            // ����ָ��ߣ��У�
            for (int row = 1; row < gridSize.y; row++)
            {
                float currentZ = startPos.z + row * cellLength;
                Gizmos.DrawLine(new Vector3(minX, startPos.y, currentZ), new Vector3(maxX, startPos.y, currentZ));
            }
        }

        /// <summary>
        /// ���Ƶ�Ԫ�������ǩ������������Ϊ���ģ�
        /// </summary>
        private void DrawGridCellLabels(Vector3 startPos, Vector2Int gridSize, Vector2 spacing, Quaternion rotation)
        {
            float cellWidth = spacing.x;
            float cellLength = spacing.y;

            for (int col = 0; col < gridSize.x; col++)
            {
                for (int row = 0; row < gridSize.y; row++)
                {
                    // ��ǩλ��ֱ�Ӷ�Ӧ������������
                    Vector3 localCenter = new Vector3(
                        col * cellWidth,
                        0,
                        row * cellLength
                    );

                    Vector3 worldCenter = startPos + rotation * localCenter;
                    Vector3 labelPos = worldCenter + Vector3.up * gridCellLabelYOffset * globalScale;

                    string cellLabelText = $"({col},{row})";
                    #if UNITY_EDITOR
                Handles.Label(labelPos, cellLabelText, GetLabelStyle());
                    #endif
                }
            }
        }


        /// <summary>
        /// ��ȡ�ռ�����ɫ
        /// </summary>
        private Color GetCollectorColor(WoolColor woolColor)
        {
            if (useCustomCollectorColors)
            {
                return woolColor switch
                {
                    WoolColor.Green => customCollectorColors.Green,
                    WoolColor.Yellow => customCollectorColors.Yellow,
                    WoolColor.Pink => customCollectorColors.Pink,
                    WoolColor.Orange => customCollectorColors.Orange,
                    WoolColor.Blue => customCollectorColors.Blue,
                    WoolColor.Purple => customCollectorColors.Purple,
                    WoolColor.Black => customCollectorColors.Black,
                    _ => customCollectorColors.Default
                };
            }

            switch (currentColorMode)
            {
                case ColorMode.VibrantOpaque:
                    return woolColor switch
                    {
                        WoolColor.Green => new Color(0.2f, 0.8f, 0.2f, 1f),
                        WoolColor.Yellow => new Color(0.9f, 0.9f, 0.2f, 1f),
                        WoolColor.Pink => new Color(1f, 0.3f, 0.7f, 1f),
                        WoolColor.Orange => new Color(1f, 0.5f, 0.1f, 1f),
                        WoolColor.Blue => new Color(0.2f, 0.5f, 1f, 1f),
                        WoolColor.Purple => new Color(0.8f, 0.2f, 0.8f, 1f),
                        WoolColor.Black => new Color(0.2f, 0.2f, 0.2f, 1f),
                        _ => Color.gray
                    };

                case ColorMode.OriginalGameColor:
                    return woolColor.ToUnityColor();

                default:
                    return Color.gray;
            }
        }


        #if UNITY_EDITOR
        /// <summary>
        /// ��ȡ��ǩ��ʽ
        /// </summary>
        private GUIStyle GetLabelStyle()
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = labelFontSize;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = labelTextColor;
            style.padding = new RectOffset(2, 2, 1, 1);
            style.normal.background = CreateLabelBackground();
            return style;
        }


        /// <summary>
        /// ������ǩ��������
        /// </summary>
        private Texture2D CreateLabelBackground()
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0f, 0f, 0f, labelBackgroundAlpha));
            tex.Apply();
            return tex;
        }
        #endif
    }
}
