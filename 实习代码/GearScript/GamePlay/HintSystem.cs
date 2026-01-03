using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SuperGear
{
    public class HintSystem : MonoBehaviour
    {
        public static HintSystem Instance;

        #region 依赖引用
        [Header("依赖引用")]
        [Tooltip("提示虚影预制体（可选，优先使用积木自身预制体）")]
        [SerializeField] private GameObject defaultGhostPrefab;
        #endregion

        #region 提示视觉配置
        [Header("闪烁效果配置")]
        [Tooltip("提示总显示时长（秒）")]
        [SerializeField] private float totalDisplayTime = 4f;
        [Tooltip("闪烁间隔（秒）")]
        [SerializeField] private float flashInterval = 0.25f;
        [Tooltip("闪烁时的高亮颜色（含透明度）")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0f, 0.8f);
        [Tooltip("闪烁时的暗态颜色（含透明度）")]
        [SerializeField] private Color dimColor = new Color(1f, 0.9f, 0f, 0.3f);
        [Tooltip("闪烁时的放大比例")]
        [SerializeField] private float flashScale = 1.1f;

        [Header("消失效果配置")]
        [Tooltip("淡出消失时长（秒）")]
        [SerializeField] private float fadeOutTime = 0.5f;
        [Tooltip("消失时是否播放缩小动画")]
        [SerializeField] private bool useShrinkEffect = true;
        #endregion

        private List<GameObject> activeGhosts = new List<GameObject>(); // 当前显示的提示虚影
        private LevelData currentLevelData; // 当前关卡数据


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

        private void OnDestroy()
        {
            ClearAllGhosts();
        }


        /// <summary>
        /// 触发完整提示流程：回收玩家积木 → 显示闪烁答案 → 自动消失
        /// </summary>
        public void TriggerSolutionHint()
        {
            // 1. 回收所有已放置的积木到备选区
            RecyclePlayerBlocks();

            // 2. 清除残留的提示虚影
            ClearAllGhosts();

            // 3. 获取当前关卡数据
            currentLevelData = LevelManager.Instance?.allLevelDatas[LevelManager.Instance.CurrentLevelIndex];
            if (currentLevelData == null)
            {
                Debug.LogError("[HintSystem] 无法获取当前关卡数据");
                return;
            }

            // 4. 检查是否有预设解法
            if (currentLevelData.demonstrationBlocks == null || currentLevelData.demonstrationBlocks.Count == 0)
            {
                Debug.LogWarning("[HintSystem] 当前关卡未配置示范解法");
                return;
            }

            // 5. 生成答案虚影并开始闪烁动画
            StartCoroutine(PlayHintAnimation());
        }


        /// <summary>回收玩家已放置的所有积木</summary>
        private void RecyclePlayerBlocks()
        {
            if (BlockGenerate.Instance == null)
            {
                Debug.LogError("[HintSystem] 未找到BlockGenerate实例");
                return;
            }

           // BlockGenerate.Instance.RecycleAllPlacedBlocks();
        }


        /// <summary>生成所有答案积木的虚影</summary>
        private List<GameObject> CreateSolutionGhosts()
        {
            List<GameObject> ghosts = new List<GameObject>();

            foreach (var demoBlock in currentLevelData.demonstrationBlocks)
            {
                GameObject ghost = CreateSingleGhost(demoBlock);
                if (ghost != null)
                {
                    ghosts.Add(ghost);
                    activeGhosts.Add(ghost);
                }
            }

            return ghosts;
        }

        /// <summary>生成单个答案积木的虚影</summary>
        private GameObject CreateSingleGhost(DemonstrationBlock demoBlock)
        {
            // 获取对应类型的积木预制体
            GameObject blockPrefab = GetBlockPrefab(demoBlock.blockType);
            if (blockPrefab == null)
            {
                Debug.LogWarning($"[HintSystem] 未找到 {demoBlock.blockType} 的预制体");
                return null;
            }

            // 实例化虚影
            GameObject ghost = Instantiate(
                defaultGhostPrefab != null ? defaultGhostPrefab : blockPrefab,
                demoBlock.worldPosition,
                Quaternion.Euler(demoBlock.worldRotation)
            );

            // 基础设置
            ghost.name = $"HintGhost_{demoBlock.blockType}_{Guid.NewGuid().ToString().Substring(0, 4)}";
            ghost.transform.SetParent(transform); // 父物体设为HintSystem，方便管理
            ghost.transform.localScale = blockPrefab.transform.localScale;

            // 移除交互组件（避免干扰玩家操作）
            RemoveInteractiveComponents(ghost);

            // 设置初始材质（透明高亮）
            SetGhostMaterial(ghost, highlightColor);

            return ghost;
        }

        /// <summary>获取积木预制体（从BlockGenerate的类型映射中）</summary>
        private GameObject GetBlockPrefab(BlockType type)
        {
            if (BlockGenerate.Instance == null) return null;

            foreach (var mapping in BlockGenerate.Instance.typeMappings)
            {
                if (mapping.blockType == type && mapping.blockPrefab != null)
                {
                    return mapping.blockPrefab;
                }
            }
            return null;
        }

        /// <summary>移除虚影的交互组件（碰撞体、BlockControl等）</summary>
        private void RemoveInteractiveComponents(GameObject ghost)
        {
            // 移除碰撞体
            foreach (var collider in ghost.GetComponents<Collider>())
            {
                DestroyImmediate(collider);
            }

            // 移除交互脚本
            DestroyImmediate(ghost.GetComponent<BlockControl>());
            
        }

        /// <summary>设置虚影的材质和颜色</summary>
        private void SetGhostMaterial(GameObject ghost, Color targetColor)
        {
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // 创建临时材质（避免修改原预制体材质）
                Material ghostMat = new Material(renderer.material);
                ghostMat.color = targetColor;
                ghostMat.shader = Shader.Find("Transparent/Diffuse"); // 使用透明 shader
                renderer.material = ghostMat;
            }
        }


        /// <summary>播放提示动画：闪烁 → 淡出消失</summary>
        private IEnumerator PlayHintAnimation()
        {
            // 生成虚影
            List<GameObject> ghosts = CreateSolutionGhosts();
            if (ghosts.Count == 0) yield break;

            // 闪烁阶段
            float flashElapsed = 0;
            bool isHighlighted = true;

            while (flashElapsed < totalDisplayTime)
            {
                // 切换闪烁状态
                isHighlighted = !isHighlighted;
                Color targetColor = isHighlighted ? highlightColor : dimColor;
                float targetScale = isHighlighted ? flashScale : 1f;

                // 更新所有虚影
                foreach (var ghost in ghosts)
                {
                    if (ghost == null) continue;

                    // 更新颜色
                    SetGhostMaterial(ghost, targetColor);

                    // 更新缩放
                    ghost.transform.localScale = Vector3.Lerp(
                        ghost.transform.localScale,
                        Vector3.one * targetScale * GetOriginalScale(ghost),
                        Time.deltaTime * 15f
                    );
                }

                // 等待闪烁间隔
                yield return new WaitForSeconds(flashInterval);
                flashElapsed += flashInterval;
            }

            // 淡出消失阶段
            float fadeElapsed = 0;
            while (fadeElapsed < fadeOutTime)
            {
                float progress = fadeElapsed / fadeOutTime;
                float alpha = Mathf.Lerp(1f, 0f, progress);
                float scale = useShrinkEffect ? Mathf.Lerp(1f, 0f, progress) : 1f;

                // 更新所有虚影
                foreach (var ghost in ghosts)
                {
                    if (ghost == null) continue;

                    // 淡出颜色
                    Color fadeColor = highlightColor;
                    fadeColor.a = alpha;
                    SetGhostMaterial(ghost, fadeColor);

                    // 缩小动画（可选）
                    if (useShrinkEffect)
                    {
                        ghost.transform.localScale = Vector3.one * scale * GetOriginalScale(ghost);
                    }
                }

                fadeElapsed += Time.deltaTime;
                yield return null;
            }

            // 最终清除
            ClearAllGhosts();
        }

        /// <summary>获取虚影的原始缩放比例（用于动画计算）</summary>
        private float GetOriginalScale(GameObject ghost)
        {
            // 假设原始缩放为1（如果有特殊缩放需求，可扩展为从预制体获取）
            return 1f;
        }


        /// <summary>清除所有活跃的提示虚影</summary>
        public void ClearAllGhosts()
        {
            foreach (var ghost in activeGhosts)
            {
                if (ghost != null)
                {
                    Destroy(ghost);
                }
            }
            activeGhosts.Clear();
        }
    }
}