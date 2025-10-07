using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用证物调查Action - 支持多线索解锁和右键菜单
/// 适用于所有需要"对话-窗口-多线索-对话"流程的证物icon
/// </summary>
public class EvidenceIconAction : IconAction
{
    [Header("=== 证物配置 ===")]
    public string evidenceId = "knife";

    [Header("=== 线索配置 ===")]
    [Tooltip("需要解锁的所有线索ID")]
    public List<string> requiredClues = new List<string> { "clue_dark_red", "clue_bright_red" };

    [Tooltip("是否需要所有线索（false=任意一个即可）")]
    public bool requireAllClues = true;

    [Header("=== 对话块配置 ===")]
    public string beforeDialogueBlockId = "001";
    public string afterDialogueBlockId = "002";

    [Header("=== 窗口配置 ===")]
    public GameObject windowPrefab;
    public GameObject propertiesWindowPrefab; // 属性窗口预制体
    public Canvas targetCanvas;

    [Header("=== 功能选项 ===")]
    public bool allowReopenAfterComplete = true;
    public bool enableDebugLog = true;

    [Header("=== 状态显示（运行时只读）===")]
    [SerializeField] private InvestigationState currentState = InvestigationState.NotInvestigated;
    [SerializeField] private bool isWaitingForDialogue = false;
    [SerializeField] private bool isWaitingForClues = false;
    [SerializeField] private List<string> unlockedCluesList = new List<string>();

    // 私有变量
    private string waitingForDialogueBlockId;
    private GameFlowController gameFlowController;
    private Canvas canvas;
    private HashSet<string> unlockedClues = new HashSet<string>();
    private InteractableIcon iconComponent;

    /// <summary>
    /// 调查状态枚举
    /// </summary>
    private enum InvestigationState
    {
        NotInvestigated,
        PlayingBeforeDialogue,
        WindowOpen,
        WaitingForClues,
        PlayingAfterDialogue,
        Completed
    }

    #region Unity生命周期

    void Awake()
    {
        iconComponent = GetComponent<InteractableIcon>();
    }

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        FindTargetCanvas();
        RestoreStateFromSave();
    }

    void OnEnable()
    {
        // 订阅对话结束事件
        DialogueUI.OnDialogueBlockEnded += HandleDialogueEnded;

        // 订阅线索解锁事件
        GameEvents.OnClueUnlocked += HandleClueUnlocked;

        // 订阅右键菜单事件
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;

        DebugLog("已订阅事件");
    }

    void OnDisable()
    {
        DialogueUI.OnDialogueBlockEnded -= HandleDialogueEnded;
        GameEvents.OnClueUnlocked -= HandleClueUnlocked;
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;

        DebugLog("已取消订阅事件");
    }

    #endregion

    #region Canvas查找

    private void FindTargetCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            DebugLog($"从父级找到Canvas: {canvas.name}");
            return;
        }

        GameObject canvasObj = GameObject.FindWithTag("WindowCanvas");
        if (canvasObj != null)
        {
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                DebugLog($"通过Tag找到Canvas: {canvas.name}");
                return;
            }
        }
    }

    #endregion

    #region 状态恢复

    private void RestoreStateFromSave()
    {
        if (gameFlowController == null) return;

        var completedBlocks = gameFlowController.GetCompletedBlocksSafe();

        // 检查前置对话是否完成
        if (completedBlocks.Contains(beforeDialogueBlockId))
        {
            // 检查后置对话是否完成
            if (completedBlocks.Contains(afterDialogueBlockId))
            {
                currentState = InvestigationState.Completed;
                DebugLog("从存档恢复状态：已完成");
                return;
            }

            // 检查已解锁的线索
            foreach (string clueId in requiredClues)
            {
                if (gameFlowController.HasClue(clueId))
                {
                    unlockedClues.Add(clueId);
                    unlockedCluesList.Add(clueId);
                }
            }

            // 判断是否所有线索都已解锁
            if (AreAllCluesUnlocked())
            {
                currentState = InvestigationState.PlayingAfterDialogue;
                DebugLog($"从存档恢复状态：所有线索已解锁 ({unlockedClues.Count}/{requiredClues.Count})");
            }
            else
            {
                currentState = InvestigationState.WaitingForClues;
                isWaitingForClues = true;
                DebugLog($"从存档恢复状态：等待线索解锁 ({unlockedClues.Count}/{requiredClues.Count})");
            }
        }
    }

    #endregion

    #region 双击交互

    public override void Execute()
    {
        DebugLog($"Execute() 被调用 - 当前状态: {currentState}");

        switch (currentState)
        {
            case InvestigationState.NotInvestigated:
                PlayBeforeDialogue();
                break;

            case InvestigationState.PlayingBeforeDialogue:
                DebugLog("对话播放中，忽略重复交互");
                break;

            case InvestigationState.WindowOpen:
            case InvestigationState.WaitingForClues:
            case InvestigationState.PlayingAfterDialogue:
            case InvestigationState.Completed:
                if (allowReopenAfterComplete)
                {
                    DebugLog("重新打开窗口");
                    CreateWindow();
                }
                else
                {
                    DebugLog("已调查完成，不允许重新打开");
                }
                break;
        }
    }

    public override bool CanExecute()
    {
        if (string.IsNullOrEmpty(beforeDialogueBlockId) || windowPrefab == null)
        {
            return false;
        }

        if (isWaitingForDialogue)
        {
            return false;
        }

        return base.CanExecute();
    }

    #endregion

    #region 右键菜单

    /// <summary>
    /// 右键菜单项点击事件处理
    /// </summary>
    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        // 检查是否是本图标的事件
        if (icon.gameObject != gameObject)
        {
            return;
        }

        DebugLog($"右键菜单点击: {itemId}");

        switch (itemId)
        {
            case "properties":
                ShowPropertiesWindow();
                break;

            case "open":
                // 可选：右键也能打开
                Execute();
                break;

            // 可以添加更多自定义菜单项
            default:
                DebugLog($"未处理的菜单项: {itemId}");
                break;
        }
    }

    /// <summary>
    /// 显示属性窗口
    /// </summary>
    private void ShowPropertiesWindow()
    {
        if (canvas == null)
        {
            canvas = targetCanvas != null ? targetCanvas : FindObjectOfType<Canvas>();
        }

        GameObject propertiesWindow = Instantiate(propertiesWindowPrefab, canvas.transform);
        propertiesWindow.name = $"{evidenceId}_Properties";

        DebugLog($"已生成属性窗口: {propertiesWindow.name}");
    }

    #endregion

    #region 对话处理

    private void PlayBeforeDialogue()
    {
        currentState = InvestigationState.PlayingBeforeDialogue;
        isWaitingForDialogue = true;
        waitingForDialogueBlockId = beforeDialogueBlockId;

        gameFlowController.StartDialogueBlock(beforeDialogueBlockId);

        DebugLog($"开始播放前置对话: {beforeDialogueBlockId}");
    }

    private void PlayAfterDialogue()
    {
        currentState = InvestigationState.PlayingAfterDialogue;
        isWaitingForDialogue = true;
        waitingForDialogueBlockId = afterDialogueBlockId;

        gameFlowController.StartDialogueBlock(afterDialogueBlockId);

        DebugLog($"开始播放后置对话: {afterDialogueBlockId}");
    }

    /// <summary>
    /// 处理对话结束事件
    /// </summary>
    private void HandleDialogueEnded(string fileName, string blockId)
    {
        // 检查是否是我们等待的对话
        if (blockId == waitingForDialogueBlockId)
        {
            DebugLog($"监听到对话结束: {blockId}");

            waitingForDialogueBlockId = null;
            isWaitingForDialogue = false;

            // 根据是哪个对话块完成来处理
            if (blockId == beforeDialogueBlockId)
            {
                OnBeforeDialogueComplete();
            }
            else if (blockId == afterDialogueBlockId)
            {
                OnAfterDialogueComplete();
            }
        }
    }

    private void OnBeforeDialogueComplete()
    {
        DebugLog("前置对话完成，准备生成窗口");
        CreateWindow();
    }

    private void OnAfterDialogueComplete()
    {
        DebugLog("后置对话完成，调查完成");
        currentState = InvestigationState.Completed;
    }

    #endregion

    #region 线索解锁处理

    /// <summary>
    /// 处理线索解锁事件（核心逻辑：所有线索解锁后触发after对话）
    /// </summary>
    private void HandleClueUnlocked(string unlockedClueId)
    {
        // 检查是否是我们需要的线索之一
        if (!requiredClues.Contains(unlockedClueId))
        {
            return;
        }

        // 检查是否已经解锁过
        if (unlockedClues.Contains(unlockedClueId))
        {
            DebugLog($"线索 [{unlockedClueId}] 已经解锁过，忽略");
            return;
        }

        // 记录解锁的线索
        unlockedClues.Add(unlockedClueId);
        unlockedCluesList.Add(unlockedClueId);

        DebugLog($"线索已解锁: {unlockedClueId} ({unlockedClues.Count}/{requiredClues.Count})");

        // 检查是否所有线索都已解锁
        if (isWaitingForClues && AreAllCluesUnlocked())
        {
            DebugLog("所有线索已集齐，准备播放after对话");
            isWaitingForClues = false;

            // 触发after对话
            PlayAfterDialogue();
        }
    }

    /// <summary>
    /// 检查是否所有线索都已解锁
    /// </summary>
    private bool AreAllCluesUnlocked()
    {
        if (requireAllClues)
        {
            // 需要所有线索
            foreach (string clueId in requiredClues)
            {
                if (!unlockedClues.Contains(clueId))
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            // 只需要任意一个线索
            return unlockedClues.Count > 0;
        }
    }

    #endregion

    #region 窗口管理

    private void CreateWindow()
    {
        if (canvas == null)
        {
            canvas = targetCanvas != null ? targetCanvas : FindObjectOfType<Canvas>();
        }
        GameObject windowObj = Instantiate(windowPrefab, canvas.transform);
        windowObj.name = $"{evidenceId}_Window";

        currentState = InvestigationState.WaitingForClues;
        isWaitingForClues = true;

        DebugLog($"已创建窗口: {windowObj.name}，等待线索解锁 (需要 {requiredClues.Count} 个)");
    }

    #endregion


    #region 调试工具
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EvidenceIconAction - {evidenceId}] {message}");
        }
    }
    #endregion
}