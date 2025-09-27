using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    #region 初始化

    void Awake()
    {
        InitializeConfigs();
        CacheStageObjects();
    }

    void Start()
    {
        // 查找DialogueManager
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
            if (dialogueManager == null)
            {
                LogError("未找到DialogueManager组件！");
            }
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

    #region Stage管理

    /// <summary>
    /// 加载指定Stage
    /// </summary>
    public void LoadStage(string stageId)
    {
        if (string.IsNullOrEmpty(stageId))
        {
            LogError("Stage ID不能为空");
            return;
        }

        if (!stageConfigDict.ContainsKey(stageId))
        {
            LogError($"找不到Stage配置: {stageId}");
            return;
        }

        if (!stageObjectDict.ContainsKey(stageId))
        {
            LogError($"找不到Stage对象: {stageId}");
            return;
        }

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

        // 触发事件
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
        if (string.IsNullOrEmpty(dialogueBlockFileId))
        {
            LogError("对话块ID不能为空");
            return;
        }

        if (currentStage == null)
        {
            LogError("当前Stage为空");
            return;
        }

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

            OnClueUnlocked?.Invoke(clueId);

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
    // 提供外部安全访问 Stage 配置
    public IReadOnlyList<StageConfig> GetStageConfigsSafe()
    {
        return stageConfigs.AsReadOnly();
    }

    // 提供当前 StageId
    public string GetCurrentStageIdSafe()
    {
        return currentStageId;
    }

    // 提供已完成的对话块列表
    public IReadOnlyList<string> GetCompletedBlocksSafe()
    {
        return completedDialogueBlocks.AsReadOnly();
    }

    // 提供已解锁的线索
    public IReadOnlyList<string> GetUnlockedCluesSafe()
    {
        return unlockedClues.AsReadOnly();
    }

    // 判断是否启用多语言
    public bool IsMultiLanguageEnabled()
    {
        return useMultiLanguageScripts;
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

            // 注意：这里不自动重新开始对话，让玩家手动触发
            LogDebug($"恢复未完成的对话状态: {currentDialogueFile}:{currentDialogueBlockId}");
        }
    }

    #endregion

    #region 调试工具

    [ContextMenu("解锁所有线索")]
    private void Debug_UnlockAllClues()
    {
        foreach (var stage in stageConfigs)
        {
            foreach (var block in stage.dialogueBlocks)
            {
                if (block.unlocksClues != null)
                {
                    foreach (string clue in block.unlocksClues)
                    {
                        UnlockClue(clue);
                    }
                }
            }
        }
    }

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