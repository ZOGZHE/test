using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SuperGear;

namespace SuperGear
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;

        #region  倒计时相关
        // 时间缩放相关
        private float _previousTimeScale = 1f; // 记录暂停前的时间缩放值
        public bool IsGamePaused => Time.timeScale == 0; // 当前是否处于暂停状态
        // 倒计时相关
        private float _remainingTime; // 剩余时间
        private float _totalTime; // 总时长（用于UI显示比例）
        public event Action<float> OnCountdownUpdated; // 传递(剩余时间, 总时长)
        public event Action OnCountdownEnd; // 倒计时结束事件
        public bool _isCountingDown = false; // 倒计时状态标记                             
        public bool _hasResumedCountdown = false;//标记是否已恢复过倒计时（确保只触发一次）
        #endregion


        #region 生命周期函数
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdateCountdown();
        }
        #endregion

        #region 倒计时控制
        // 初始化并开始倒计时（从当前关卡配置获取时长，每关调用时会重置总时长）
        public void StartCountdown()
        {
            var currentLevelData = LevelManager.Instance.allLevelDatas[(LevelManager.Instance.CurrentLevelIndex) % (LevelManager.Instance.allLevelDatas.Count)];
            _totalTime = currentLevelData.countdownDuration; // 从当前关卡数据重置总时长
            _remainingTime = _totalTime;
            _isCountingDown = true; // 开始倒计时
            OnCountdownUpdated?.Invoke(_remainingTime); // 初始通知
        }

        // 倒计时更新逻辑
        private void UpdateCountdown()
        {
            if (!_isCountingDown) return;

            _remainingTime -= Time.deltaTime;

            // 确保时间不会小于0
            if (_remainingTime <= 0)
            {
                _remainingTime = 0;
                _isCountingDown = false;
                OnCountdownEnd?.Invoke();
            }

            OnCountdownUpdated?.Invoke(_remainingTime);
        }
        #endregion

        #region 外部便捷调用时间方法

        public void StopCountdown()
        {
            _isCountingDown = false;
        }

        public void ResumeCountdown()
        {
            if (_remainingTime > 0 && !_isCountingDown)
            {
                _isCountingDown = true;
            }
        }

        public void PauseGame()
        {
            // 保存当前时间缩放值（支持从慢动作等状态暂停）
            _previousTimeScale = Time.timeScale;
            // 冻结时间
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            // 还原到暂停前的时间缩放值
            Time.timeScale = _previousTimeScale;
        }
        #endregion

        #region  BlockGenerate旧方法

        #region 初始化与生成(旧)
        //private void InitializeTypeMappingDict()
        //{
        //    typeToPrefabDict.Clear();
        //    foreach (var mapping in typeMappings)
        //    {
        //        if (mapping.blockPrefab != null && !typeToPrefabDict.ContainsKey(mapping.blockType))
        //        {
        //            typeToPrefabDict.Add(mapping.blockType, mapping.blockPrefab);

        //            if (mapping.blockPrefab.GetComponent<BlockControl>() == null)
        //            {
        //                Debug.LogError($"预制体 {mapping.blockPrefab.name} 缺少 BlockControl 组件！");
        //            }
        //        }
        //    }
        //}

        //public void InitializeBlockGenerateData(LevelData levelData)
        //{
        //    DestroyOldBlocks();
        //    var generateData = levelData._blockGenerateData;
        //    if (generateData == null || generateData.generateItems.Count == 0)
        //    {
        //        Debug.LogWarning("当前关卡没有配置积木生成数据！");
        //        return;
        //    }

        //    // 使用中父节点作为生成位置
        //    foreach (var item in generateData.generateItems)
        //    {
        //        GenerateBlockInstance(item, middleBlockParent.position);
        //    }
        //    UpdateBlockDisplay();
        //}

        //// 生成积木实例
        //private void GenerateBlockInstance(BlockGenerateItem config, Vector3 spawnPosition)
        //{
        //    if (!typeToPrefabDict.TryGetValue(config.blockType, out GameObject prefab))
        //    {
        //        Debug.LogError($"未找到 {config.blockType} 对应的预制体，请检查类型映射配置！");
        //        return;
        //    }

        //    GameObject blockInstance = Instantiate(prefab, middleBlockParent);
        //    blockInstance.transform.position = spawnPosition;
        //    blockInstance.transform.rotation = Quaternion.identity;
        //    blockInstance.SetActive(false);

        //    BlockControl blockControl = blockInstance.GetComponent<BlockControl>();
        //    if (blockControl != null)
        //    {
        //        if (blockControl._blockData == null)
        //        {
        //            Debug.LogError($"预制体 {blockInstance.name} 的 BlockControl 组件中 _blockData 未赋值！");
        //            return;
        //        }

        //        // 1. 原有齿轮和数据配置逻辑
        //        blockControl._blockData.gear1 = config.gear1;
        //        blockControl._blockData.gear2 = config.gear2;
        //        blockControl._blockData.gear3 = config.gear3;
        //        blockControl._blockData.gear4 = config.gear4;

        //        UpdateGearActivation(blockControl._blockData);

        //        // 2. 先添加实例到列表，再赋值PrefabIndex
        //        blockInstances.Add(blockInstance);
        //        int instanceIndex = blockInstances.Count - 1;  // 获取当前实例索引
        //        blockControl._blockData._blockType = config.blockType;
        //        blockControl.PrefabIndex = instanceIndex;  // 赋值PrefabIndex
        //        blockControl.OnPlaced += HandleBlockPlaced;
        //        blockControl.OnRemoved += HandleBlockRemoved;


        //        // 3. 备选区添加索引（与PrefabIndex一致）
        //        AlternativeArea.Add(instanceIndex);
        //    }
        //    else
        //    {
        //        Debug.LogError($"预制体 {blockInstance.name} 缺少 BlockControl 组件！");
        //        Destroy(blockInstance);  // 避免无组件实例残留
        //    }
        //}

        //// 齿轮设置
        //private void UpdateGearActivation(BlockData blockData)
        //{
        //    if (blockData._gearobject == null) return;

        //    for (int i = 0; i < blockData._gearobject.Length; i++)
        //    {
        //        if (blockData._gearobject[i]?.GearObject == null) continue;

        //        bool isActive = i switch
        //        {
        //            0 => blockData.gear1,
        //            1 => blockData.gear2,
        //            2 => blockData.gear3,
        //            3 => blockData.gear4,
        //            _ => false
        //        };

        //        blockData._gearobject[i].IsActive = isActive;
        //        blockData._gearobject[i].GearObject.SetActive(isActive);

        //    }
        //}
        #endregion

        #region 切换逻辑 (旧)
        //public void PreviousBlock()
        //{

        //    if (AlternativeArea.Count <= 1) return;

        //    int currentPrefabIndex = AlternativeArea[currentActiveIndex];
        //    GameObject currentBlock = blockInstances[currentPrefabIndex];
        //    BlockControl currentBlockCtrl = currentBlock.GetComponent<BlockControl>();
        //    // 若正在复位或切换，直接退出
        //    if (currentBlockCtrl != null && currentBlockCtrl.isResetting || isSwitchingAnimationPlaying)
        //    {
        //        return;
        //    }

        //    // 计算上一个索引（循环）
        //    int newIndex = (currentActiveIndex - 1 + AlternativeArea.Count) % AlternativeArea.Count;
        //    int newPrefabIndex = AlternativeArea[newIndex];
        //    GameObject newBlock = blockInstances[newPrefabIndex];
        //    if (LevelManager.Instance.CurrentLevelIndex == 1)
        //        LevelManager.Instance.HideGuidance();
        //    // 启动切换动画协程
        //    StartCoroutine(PreviousBlockAnimation(currentBlock, newBlock, newIndex));
        //}

        //public void NextBlock()
        //{
        //    if (AlternativeArea.Count <= 1) return;

        //    int currentPrefabIndex = AlternativeArea[currentActiveIndex];
        //    GameObject currentBlock = blockInstances[currentPrefabIndex];

        //    // 计算下一个索引（循环）
        //    int newIndex = (currentActiveIndex + 1) % AlternativeArea.Count;
        //    int newPrefabIndex = AlternativeArea[newIndex];
        //    GameObject newBlock = blockInstances[newPrefabIndex];
        //    BlockControl currentBlockCtrl = currentBlock.GetComponent<BlockControl>();
        //    // 若正在复位或切换，直接退出
        //    if (currentBlockCtrl != null && currentBlockCtrl.isResetting || isSwitchingAnimationPlaying)
        //    {
        //        return;
        //    }
        //    if (LevelManager.Instance.CurrentLevelIndex == 1)
        //        LevelManager.Instance.HideGuidance();
        //    // 启动切换动画协程
        //    StartCoroutine(NextBlockAnimation(currentBlock, newBlock, newIndex));
        //}
        //// 左切换动画协程
        //private IEnumerator PreviousBlockAnimation(GameObject currentBlock, GameObject newBlock, int newIndex)
        //{
        //    if (isSwitchingAnimationPlaying)
        //    {
        //        yield break;
        //    }
        //    if (currentBlock == null || newBlock == null)
        //    {
        //        //Debug.LogWarning($"切换动画对象为空：current={currentBlock?.name}, new={newBlock?.name}");
        //        isSwitchingAnimationPlaying = false; // 关键修复：重置状态
        //        yield break;
        //    }
        //    // 如果动画正在执行，直接退出，避免重复触发
        //    if (isSwitchingAnimationPlaying) yield break;
        //    isSwitchingAnimationPlaying = true;
        //    // 隐藏新积木并初始化位置（移除缩放设置）
        //    newBlock.SetActive(true);
        //    newBlock.transform.SetParent(leftBlockParent);
        //    newBlock.transform.position = leftBlockParent.position;
        //    newBlock.SetActive(false);

        //    // 当前积木移向右父节点（仅保留位置动画）
        //    float elapsed = 0;
        //    Vector3 currStartPos = currentBlock.transform.position;
        //    Vector3 currEndPos = rightBlockParent.position;

        //    while (elapsed < animationDuration)
        //    {
        //        float t = easeCurve.Evaluate(elapsed / animationDuration); // 用缓动曲线计算进度
        //        currentBlock.transform.position = Vector3.Lerp(currStartPos, currEndPos, t);
        //        elapsed += Time.deltaTime;
        //        yield return null;
        //    }

        //    // 确保当前积木到位并隐藏（移除缩放恢复）
        //    currentBlock.transform.position = currEndPos;
        //    currentBlock.SetActive(false);

        //    // 新积木从中父节点入场（仅保留位置动画）
        //    newBlock.SetActive(true);
        //    newBlock.transform.SetParent(middleBlockParent);
        //    Vector3 newStartPos = newBlock.transform.position;
        //    Vector3 newEndPos = middleBlockParent.position;
        //    elapsed = 0;

        //    while (elapsed < animationDuration)
        //    {
        //        float t = easeCurve.Evaluate(elapsed / animationDuration);
        //        newBlock.transform.position = Vector3.Lerp(newStartPos, newEndPos, t);
        //        elapsed += Time.deltaTime;
        //        yield return null;
        //    }

        //    // 确保新积木到位（移除缩放恢复）
        //    newBlock.transform.position = newEndPos;
        //    currentActiveIndex = newIndex;
        //    UIManager.Instance.UpdateButtonStates();
        //    // 更新预览（动态隐藏当前激活的方块）
        //    Preview.Instance?.UpdatePreview();
        //    yield return null; // 等待一帧确保所有操作完成
        //    isSwitchingAnimationPlaying = false; // 解锁动画状态
        //}

        //// 右切换动画协程
        //private IEnumerator NextBlockAnimation(GameObject currentBlock, GameObject newBlock, int newIndex)
        //{
        //    if (isSwitchingAnimationPlaying)
        //    {
        //        yield break;
        //    }
        //    if (currentBlock == null || newBlock == null)
        //    {
        //        //Debug.LogWarning($"切换动画对象为空：current={currentBlock?.name}, new={newBlock?.name}");
        //        isSwitchingAnimationPlaying = false; // 关键修复：重置状态
        //        yield break;
        //    }
        //    // 如果动画正在执行，直接退出，避免重复触发
        //    if (isSwitchingAnimationPlaying) yield break;
        //    isSwitchingAnimationPlaying = true; // 锁定动画状态
        //    // 隐藏新积木并初始化位置（移除缩放设置）
        //    newBlock.SetActive(true);
        //    newBlock.transform.SetParent(rightBlockParent);
        //    newBlock.transform.position = rightBlockParent.position;
        //    newBlock.SetActive(false);

        //    // 当前积木移向左父节点（仅保留位置动画）
        //    float elapsed = 0;
        //    Vector3 currStartPos = currentBlock.transform.position;
        //    Vector3 currEndPos = leftBlockParent.position;

        //    while (elapsed < animationDuration)
        //    {
        //        float t = easeCurve.Evaluate(elapsed / animationDuration);
        //        currentBlock.transform.position = Vector3.Lerp(currStartPos, currEndPos, t);
        //        elapsed += Time.deltaTime;
        //        yield return null;
        //    }

        //    // 确保当前积木到位并隐藏（移除缩放恢复）
        //    currentBlock.transform.position = currEndPos;
        //    currentBlock.SetActive(false);

        //    // 新积木从中父节点入场（仅保留位置动画）
        //    newBlock.SetActive(true);
        //    newBlock.transform.SetParent(middleBlockParent);
        //    Vector3 newStartPos = newBlock.transform.position;
        //    Vector3 newEndPos = middleBlockParent.position;
        //    elapsed = 0;

        //    while (elapsed < animationDuration)
        //    {
        //        float t = easeCurve.Evaluate(elapsed / animationDuration);
        //        newBlock.transform.position = Vector3.Lerp(newStartPos, newEndPos, t);
        //        elapsed += Time.deltaTime;
        //        yield return null;
        //    }

        //    // 确保新积木到位（移除缩放恢复）
        //    newBlock.transform.position = newEndPos;
        //    currentActiveIndex = newIndex;
        //    UIManager.Instance.UpdateButtonStates();
        //    // 更新预览（动态隐藏当前激活的方块）
        //    Preview.Instance?.UpdatePreview();
        //    yield return null; // 等待一帧确保所有操作完成
        //    isSwitchingAnimationPlaying = false; // 解锁动画状态
        //}
        #endregion

        #region 积木实例隐藏显示逻辑(旧)
        //public void ActiveBlock(int prefabIndex)
        //{
        //    if (prefabIndex < 0 || prefabIndex >= blockInstances.Count) return;

        //    var block = blockInstances[prefabIndex];
        //    if (block != null)
        //    {
        //        block.SetActive(true);
        //    }
        //}

        //public void HideBlock(int prefabIndex)
        //{
        //    if (prefabIndex < 0 || prefabIndex >= blockInstances.Count) return;

        //    var block = blockInstances[prefabIndex];
        //    if (block != null)
        //    {
        //        block.SetActive(false);
        //    }
        //}

        //private void UpdateBlockDisplay()
        //{
        //    // 隐藏所有备选区积木
        //    foreach (int prefabIndex in AlternativeArea)
        //    {
        //        HideBlock(prefabIndex);
        //    }

        //    // 激活当前选中的备选区积木
        //    if (AlternativeArea.Count > 0 && currentActiveIndex < AlternativeArea.Count)
        //    {
        //        int activePrefabIndex = AlternativeArea[currentActiveIndex];
        //        ActiveBlock(activePrefabIndex);
        //        var block = blockInstances[activePrefabIndex];
        //        if (block != null)
        //        {
        //            block.transform.SetParent(middleBlockParent);

        //            // 只有在没有动画时才同步位置
        //            BlockControl blockCtrl = block.GetComponent<BlockControl>();
        //            if (blockCtrl != null && !blockCtrl.isResetting)
        //            {

        //                // 等待一帧确保父节点切换完成
        //                StartCoroutine(SetPositionAfterFrame(block, middleBlockParent.position));
        //            }
        //        }
        //    }

        //    UIManager.Instance.UpdateButtonStates();
        //}

        //private IEnumerator SetPositionAfterFrame(GameObject block, Vector3 position)
        //{
        //    yield return null; // 等待一帧，确保父节点切换完成
        //    block.transform.position = position;
        //}
        #endregion

        #region 事件处理(旧)
        //private void HandleBlockPlaced(BlockControl block, int prefabIndex)
        //{
        //    if (!AlternativeArea.Contains(prefabIndex)) return; // 避免重复处理

        //    //Debug.Log($"积木 {prefabIndex} 被放置，从备选区移至放置区");

        //    // 从备选区移除，添加到放置区
        //    int removedIndex = AlternativeArea.IndexOf(prefabIndex);
        //    AlternativeArea.Remove(prefabIndex);
        //    PlacementArea.Add(prefabIndex);

        //    // 切换父节点为放置区父对象
        //    block.transform.SetParent(blockPlaceParent);
        //    // 调整当前激活索引
        //    if (AlternativeArea.Count > 0)
        //    {
        //        currentActiveIndex = removedIndex % AlternativeArea.Count;
        //    }
        //    else
        //    {
        //        currentActiveIndex = 0;
        //    }

        //    UpdateBlockDisplay();
        //    // 任意积木放置时，销毁所有提示积木
        //    UIManager.Instance?.ClearOrdinaryHints();
        //    // 更新预览（动态隐藏已放置的方块）
        //    Preview.Instance?.UpdatePreview();
        //}

        //private void HandleBlockRemoved(BlockControl block, int prefabIndex)
        //{
        //    if (!PlacementArea.Contains(prefabIndex)) return; // 避免重复处理

        //    //Debug.Log($"积木 {prefabIndex} 被取下，从放置区移回备选区");

        //    // 切换父节点为备选区父对象
        //    block.transform.SetParent(middleBlockParent);
        //    //block.transform.position = middleBlockParent.position;
        //    // 从放置区移除，添加到备选区
        //    PlacementArea.Remove(prefabIndex);
        //    AlternativeArea.Add(prefabIndex);

        //    // 调整当前激活索引
        //    currentActiveIndex = AlternativeArea.Count - 1;

        //    UpdateBlockDisplay();
        //    // 更新预览（动态显示取下的方块）
        //    Preview.Instance?.UpdatePreview();
        //}
        #endregion

        #endregion
    }
}