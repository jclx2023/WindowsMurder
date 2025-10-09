using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 窗口转换数据 - 用于跨Stage传递窗口位置信息
/// </summary>
[System.Serializable]
public struct WindowTransitionData
{
    public Vector2 windowPosition;

    public WindowTransitionData(Vector2 position)
    {
        windowPosition = position;
    }
}

/// <summary>
/// 游戏流程控制器 - 管理Stage切换、条件判断和进度推进
/// </summary>
public class GameFlowController : MonoBehaviour
{
    [Header("=== Stage配置 ===")]
    [SerializeField] private List<StageConfig> stageConfigs = new List<StageConfig>();
    [SerializeField] private string startingStageId = "Stage01_Desktop";

    [Header("=== 当前状态 ===")]
    [SerializeField] private string currentStageId;
    [SerializeField] private string currentDialogueFile;
    [SerializeField] private string currentDialogueBlockId;
    [SerializeField] private List<string> unlockedClues = new List<string>();
    [SerializeField] private List<string> completedDialogueBlocks = new List<string>();

    [Header("=== 组件引用 ===")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Transform stageContainer; // Canvas下的Stage容器

    [Header("=== 事件 ===")]
    public UnityEvent<string> OnStageChanged;
    public UnityEvent<string> OnClueUnlocked;
    public UnityEvent OnAutoSaveRequested;

    [Header("=== 多语言设置 ===")]
    [SerializeField] private bool useMultiLanguageScripts = true; // 是否启用多语言脚本

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = true;

    // 私有变量
    private Dictionary<string, StageConfig> stageConfigDict;
    private Dictionary<string, GameObject> stageObjectDict;
    private StageConfig currentStage;

    // 【新增】窗口转换数据缓存
    private WindowTransitionData? cachedWindowTransition;

    #region 初始化

    void Awake()
    {
        InitializeConfigs();
        CacheStageObjects();
    }

    void OnEnable()
    {
        // 订阅静态事件
        GameEvents.OnClueUnlockRequested += HandleClueUnlockRequest;
        GameEvents.OnDialogueBlockRequested += HandleDialogueBlockRequest;
        GameEvents.OnStageChangeRequested += HandleStageChangeRequest;

        LogDebug("已订阅游戏事件");
    }

    void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        GameEvents.OnClueUnlockRequested -= HandleClueUnlockRequest;
        GameEvents.OnDialogueBlockRequested -= HandleDialogueBlockRequest;
        GameEvents.OnStageChangeRequested -= HandleStageChangeRequest;

        LogDebug("已取消订阅游戏事件");
    }

    void Start()
    {
        // 查找DialogueManager
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        // 如果没有从存档加载，则从起始Stage开始
        if (string.IsNullOrEmpty(currentStageId))
        {
            LoadStage(startingStageId);
        }
    }

    /// <summary>
    /// 初始化配置字典
    /// </summary>
    private void InitializeConfigs()
    {
        stageConfigDict = new Dictionary<string, StageConfig>();

        foreach (var config in stageConfigs)
        {
            if (config != null && !string.IsNullOrEmpty(config.stageId))
            {
                stageConfigDict[config.stageId] = config;

                // 初始化对话块配置字典
                config.InitializeDictionary();
            }
        }

        LogDebug($"初始化了 {stageConfigDict.Count} 个Stage配置");
    }

    /// <summary>
    /// 缓存Stage GameObject
    /// </summary>
    private void CacheStageObjects()
    {
        stageObjectDict = new Dictionary<string, GameObject>();

        if (stageContainer == null)
        {
            // 尝试自动查找
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                stageContainer = canvas.transform;
            }
            else
            {
                LogError("未找到Stage容器！");
                return;
            }
        }

        // 遍历所有子对象，缓存Stage对象
        foreach (Transform child in stageContainer)
        {
            // 假设Stage对象的命名规则是 "Stage01_Desktop" 这样的格式
            if (child.name.StartsWith("Stage"))
            {
                stageObjectDict[child.name] = child.gameObject;
                LogDebug($"缓存Stage对象: {child.name}");
            }
        }
    }

    #endregion

    #region 事件处理器

    /// <summary>
    /// 处理线索解锁请求
    /// </summary>
    private void HandleClueUnlockRequest(string clueId)
    {
        UnlockClue(clueId);
    }

    /// <summary>
    /// 处理对话块请求
    /// </summary>
    private void HandleDialogueBlockRequest(string blockId)
    {
        StartDialogueBlock(blockId);
    }

    /// <summary>
    /// 处理Stage切换请求
    /// </summary>
    private void HandleStageChangeRequest(string stageId)
    {
        LoadStage(stageId);
    }

    #endregion

    #region Stage管理

    /// <summary>
    /// 加载指定Stage
    /// </summary>
    public void LoadStage(string stageId)
    {
        LogDebug($"加载Stage: {stageId}");

        // 隐藏所有Stage
        foreach (var kvp in stageObjectDict)
        {
            kvp.Value.SetActive(false);
        }

        // 显示目标Stage
        stageObjectDict[stageId].SetActive(true);

        // 更新当前状态
        currentStageId = stageId;
        currentStage = stageConfigDict[stageId];

        // 初始化Stage内的可交互对象
        InitializeStageInteractables();

        // 触发Unity事件
        OnStageChanged?.Invoke(stageId);

        LogDebug($"Stage {stageId} 加载完成");
    }

    /// <summary>
    /// 初始化Stage内的可交互对象
    /// </summary>
    private void InitializeStageInteractables()
    {
        if (currentStage == null) return;

        GameObject stageObj = stageObjectDict[currentStageId];

        // 遍历所有对话块配置
        foreach (var dialogueBlock in currentStage.dialogueBlocks)
        {
            // 如果有关联的GameObject，根据条件设置其激活状态
            if (!string.IsNullOrEmpty(dialogueBlock.interactableObjectName))
            {
                Transform interactable = stageObj.transform.Find(dialogueBlock.interactableObjectName);
                if (interactable != null)
                {
                    // 检查是否满足解锁条件
                    bool canInteract = CheckDialogueBlockCondition(dialogueBlock);
                    interactable.gameObject.SetActive(canInteract);

                    LogDebug($"可交互对象 {dialogueBlock.interactableObjectName} 状态: {canInteract}");
                }
            }
        }
    }

    /// <summary>
    /// 尝试进入下一个Stage
    /// </summary>
    public void TryProgressToNextStage()
    {
        if (currentStage == null) return;

        // 检查是否满足进入下一Stage的条件
        if (CheckStageProgressCondition())
        {
            if (!string.IsNullOrEmpty(currentStage.nextStageId))
            {
                LogDebug($"满足条件，进入下一Stage: {currentStage.nextStageId}");
                LoadStage(currentStage.nextStageId);
            }
            else
            {
                LogDebug("已到达最后的Stage");
            }
        }
        else
        {
            LogDebug("不满足进入下一Stage的条件");
        }
    }

    #endregion

    #region 对话块管理

    /// <summary>
    /// 开始对话块（使用dialogueBlockFileId作为标识）
    /// </summary>
    public void StartDialogueBlock(string dialogueBlockFileId)
    {
        // 查找对话块配置
        var dialogueBlock = currentStage.GetDialogueBlock(dialogueBlockFileId);
        if (dialogueBlock == null)
        {
            LogError($"找不到对话块配置: {dialogueBlockFileId}");
            return;
        }

        // 检查条件
        if (!CheckDialogueBlockCondition(dialogueBlock))
        {
            LogDebug($"对话块 {dialogueBlockFileId} 条件不满足");
            return;
        }

        // 根据当前语言设置构建文件名
        string fileName = GetCurrentScriptFileName();

        currentDialogueFile = fileName;
        currentDialogueBlockId = dialogueBlockFileId;
        dialogueManager.StartDialogue(dialogueBlock.dialogueBlockFileId);
        LogDebug($"开始对话块: {dialogueBlockFileId} -> 文件: {fileName}");
    }

    /// <summary>
    /// 根据当前语言设置获取脚本文件名
    /// </summary>
    private string GetCurrentScriptFileName()
    {
        string fileName = "zh"; // 默认中文

        // 从 LanguageManager 获取当前语言
        if (LanguageManager.Instance != null)
        {
            switch (LanguageManager.Instance.currentLanguage)
            {
                case SupportedLanguage.Chinese:
                    fileName = "zh";
                    break;
                case SupportedLanguage.English:
                    fileName = "en";
                    break;
                case SupportedLanguage.Japanese:
                    fileName = "jp";
                    break;
                default:
                    fileName = "zh";
                    break;
            }
        }

        return fileName;
    }

    /// <summary>
    /// 对话块完成时调用
    /// </summary>
    public void OnDialogueBlockComplete(string dialogueBlockFileId)
    {
        if (string.IsNullOrEmpty(dialogueBlockFileId)) return;

        LogDebug($"对话块完成: {dialogueBlockFileId}");

        // 标记为已完成
        if (!completedDialogueBlocks.Contains(dialogueBlockFileId))
        {
            completedDialogueBlocks.Add(dialogueBlockFileId);
        }

        // 解锁对应线索
        if (currentStage != null)
        {
            var dialogueBlock = currentStage.GetDialogueBlock(dialogueBlockFileId);
            if (dialogueBlock != null && dialogueBlock.unlocksClues != null)
            {
                foreach (string clue in dialogueBlock.unlocksClues)
                {
                    UnlockClue(clue);
                }
            }
        }

        // 清空当前对话状态
        currentDialogueFile = null;
        currentDialogueBlockId = null;

        // 更新可交互对象状态
        InitializeStageInteractables();

        // 触发自动存档
        OnAutoSaveRequested?.Invoke();
        GameEvents.NotifyDialogueBlockCompleted(dialogueBlockFileId);
    }

    /// <summary>
    /// 检查对话块条件
    /// </summary>
    private bool CheckDialogueBlockCondition(DialogueBlockConfig dialogueBlock)
    {
        if (dialogueBlock == null) return false;

        // 检查必需的线索
        if (dialogueBlock.requiredClues != null && dialogueBlock.requiredClues.Count > 0)
        {
            foreach (string clue in dialogueBlock.requiredClues)
            {
                if (!unlockedClues.Contains(clue))
                {
                    return false;
                }
            }
        }

        // 检查必需的对话块
        if (dialogueBlock.requiredDialogueBlocks != null && dialogueBlock.requiredDialogueBlocks.Count > 0)
        {
            foreach (string blockId in dialogueBlock.requiredDialogueBlocks)
            {
                if (!completedDialogueBlocks.Contains(blockId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region 线索管理

    /// <summary>
    /// 解锁线索
    /// </summary>
    public void UnlockClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return;

        if (!unlockedClues.Contains(clueId))
        {
            unlockedClues.Add(clueId);
            LogDebug($"解锁线索: {clueId}");

            // 触发Unity事件
            OnClueUnlocked?.Invoke(clueId);

            // 触发静态事件
            GameEvents.NotifyClueUnlocked(clueId);

            // 更新可交互对象状态
            InitializeStageInteractables();
        }
    }

    /// <summary>
    /// 检查是否拥有线索
    /// </summary>
    public bool HasClue(string clueId)
    {
        return unlockedClues.Contains(clueId);
    }

    #endregion

    #region 条件检查
    /// <summary>
    /// 检查当前Stage是否满足进度条件（公开接口）
    /// </summary>
    public bool IsStageProgressConditionMet()
    {
        return CheckStageProgressCondition();
    }
    /// <summary>
    /// 检查Stage进度条件
    /// </summary>
    private bool CheckStageProgressCondition()
    {
        if (currentStage == null) return false;

        // 检查必需的线索
        if (currentStage.requiredCluesForProgress != null && currentStage.requiredCluesForProgress.Count > 0)
        {
            foreach (string clue in currentStage.requiredCluesForProgress)
            {
                if (!unlockedClues.Contains(clue))
                {
                    return false;
                }
            }
        }

        // 检查必需完成的对话块
        if (currentStage.requiredDialogueBlocksForProgress != null && currentStage.requiredDialogueBlocksForProgress.Count > 0)
        {
            foreach (string blockId in currentStage.requiredDialogueBlocksForProgress)
            {
                if (!completedDialogueBlocks.Contains(blockId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region 公共接口
    public void UnlockClueDelayed(string clueId, float delaySeconds)
    {
        LogDebug($"收到延迟解锁请求 - 线索: {clueId}, 延迟: {delaySeconds}秒");
        StartCoroutine(DelayedUnlockCoroutine(clueId, delaySeconds));
    }

    private System.Collections.IEnumerator DelayedUnlockCoroutine(string clueId, float delaySeconds)
    {
        // 等待指定时间
        yield return new WaitForSeconds(delaySeconds);

        // 执行解锁
        UnlockClue(clueId);

        LogDebug($"延迟解锁完成 - 线索: {clueId}");
    }
    public IReadOnlyList<StageConfig> GetStageConfigsSafe()
    {
        return stageConfigs.AsReadOnly();
    }

    public string GetCurrentStageIdSafe()
    {
        return currentStageId;
    }

    public IReadOnlyList<string> GetCompletedBlocksSafe()
    {
        return completedDialogueBlocks.AsReadOnly();
    }

    public void CacheWindowTransition(WindowTransitionData data)
    {
        cachedWindowTransition = data;
        LogDebug($"缓存窗口转换数据 - 位置: {data.windowPosition}");
    }

    public WindowTransitionData? ConsumeWindowTransition()
    {
        var data = cachedWindowTransition;
        cachedWindowTransition = null; // 消费后清空

        if (data.HasValue)
        {
            LogDebug($"消费窗口转换数据 - 位置: {data.Value.windowPosition}");
        }
        else
        {
            LogDebug("无缓存的窗口转换数据");
        }

        return data;
    }

    #endregion

    #region 存档支持

    /// <summary>
    /// 获取当前游戏状态（用于存档）
    /// </summary>
    public GameFlowState GetCurrentState()
    {
        return new GameFlowState
        {
            currentStageId = currentStageId,
            currentDialogueFile = currentDialogueFile,
            currentDialogueBlockId = currentDialogueBlockId,
            unlockedClues = new List<string>(unlockedClues),
            completedDialogueBlocks = new List<string>(completedDialogueBlocks)
        };
    }

    /// <summary>
    /// 恢复游戏状态（从存档加载）
    /// </summary>
    public void RestoreState(GameFlowState state)
    {
        if (state == null) return;

        LogDebug("恢复游戏流程状态");

        // 恢复数据
        unlockedClues = new List<string>(state.unlockedClues ?? new List<string>());
        completedDialogueBlocks = new List<string>(state.completedDialogueBlocks ?? new List<string>());

        // 加载Stage
        if (!string.IsNullOrEmpty(state.currentStageId))
        {
            LoadStage(state.currentStageId);
        }

        // 如果有未完成的对话块，恢复它
        if (!string.IsNullOrEmpty(state.currentDialogueBlockId))
        {
            currentDialogueFile = state.currentDialogueFile;
            currentDialogueBlockId = state.currentDialogueBlockId;

            LogDebug($"恢复未完成的对话状态: {currentDialogueFile}:{currentDialogueBlockId}");
        }
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GameFlow] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameFlow] {message}");
    }

    #endregion
}

/// <summary>
/// Stage配置
/// </summary>
[System.Serializable]
public class StageConfig
{
    [Header("基础信息")]
    public string stageId = "Stage01_Desktop";

    [Header("对话块配置")]
    public List<DialogueBlockConfig> dialogueBlocks = new List<DialogueBlockConfig>();

    [Header("进度条件")]
    [Tooltip("进入下一Stage需要的线索")]
    public List<string> requiredCluesForProgress = new List<string>();

    [Tooltip("进入下一Stage需要完成的对话块")]
    public List<string> requiredDialogueBlocksForProgress = new List<string>();

    [Header("流程控制")]
    public string nextStageId = "";

    // 内部使用的字典
    private Dictionary<string, DialogueBlockConfig> dialogueBlockDict;

    /// <summary>
    /// 初始化内部字典
    /// </summary>
    public void InitializeDictionary()
    {
        dialogueBlockDict = new Dictionary<string, DialogueBlockConfig>();
        foreach (var block in dialogueBlocks)
        {
            if (!string.IsNullOrEmpty(block.dialogueBlockFileId))
            {
                dialogueBlockDict[block.dialogueBlockFileId] = block;
            }
        }
    }

    /// <summary>
    /// 获取对话块配置
    /// </summary>
    public DialogueBlockConfig GetDialogueBlock(string dialogueBlockFileId)
    {
        if (dialogueBlockDict == null)
        {
            InitializeDictionary();
        }

        if (dialogueBlockDict.ContainsKey(dialogueBlockFileId))
        {
            return dialogueBlockDict[dialogueBlockFileId];
        }

        return null;
    }
}

/// <summary>
/// 对话块配置
/// </summary>
[System.Serializable]
public class DialogueBlockConfig
{
    [Header("剧本配置")]
    [Tooltip("剧本文件中的对话块ID")]
    public string dialogueBlockFileId = "001";

    [Header("交互设置")]
    [Tooltip("关联的可交互对象名称（可选）")]
    public string interactableObjectName = "";

    [Header("解锁条件")]
    [Tooltip("需要的线索")]
    public List<string> requiredClues = new List<string>();

    [Tooltip("需要完成的其他对话块（使用dialogueBlockFileId）")]
    public List<string> requiredDialogueBlocks = new List<string>();

    [Header("完成后解锁")]
    [Tooltip("完成后解锁的线索")]
    public List<string> unlocksClues = new List<string>();
}

/// <summary>
/// 游戏流程状态（用于存档）
/// </summary>
[System.Serializable]
public class GameFlowState
{
    public string currentStageId;
    public string currentDialogueFile;
    public string currentDialogueBlockId;
    public List<string> unlockedClues;
    public List<string> completedDialogueBlocks;
}