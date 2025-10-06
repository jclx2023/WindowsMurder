using System.Linq;
using UnityEngine;

/// <summary>
/// 通用证物调查Action - 处理双击触发对话和生成窗口
/// 适用于所有需要"对话-窗口"流程的证物icon
/// </summary>
public class EvidenceIconAction : IconAction
{
    [Header("=== 证物配置 ===")]
    public string evidenceId = "recyclebin";

    public string clueId = "clue_recyclebin";

    [Header("=== 对话块配置 ===")]
    public string beforeDialogueBlockId = "001";
    public string afterDialogueBlockId = "002";

    [Header("=== 窗口配置 ===")]
    public GameObject windowPrefab;
    public Canvas targetCanvas;

    [Header("=== 功能选项 ===")]
    public bool allowReopenAfterComplete = true;
    public bool enableDebugLog = true;

    [Header("=== 状态显示（运行时只读）===")]
    [SerializeField] private InvestigationState currentState = InvestigationState.NotInvestigated;
    [SerializeField] private bool isWaitingForDialogue = false;
    [SerializeField] private bool isWaitingForClue = false;

    // 私有变量
    private string waitingForDialogueBlockId;
    private GameFlowController gameFlowController;
    private Canvas canvas;

    /// <summary>
    /// 调查状态枚举
    /// </summary>
    private enum InvestigationState
    {
        NotInvestigated,
        PlayingBeforeDialogue,
        WindowOpen,
        WaitingForClue,         // 窗口已打开，等待线索解锁
        PlayingAfterDialogue,   // 线索已解锁，正在播放after对话
        Completed
    }

    #region Unity生命周期

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

        DebugLog("已订阅事件");
    }

    void OnDisable()
    {
        DialogueUI.OnDialogueBlockEnded -= HandleDialogueEnded;
        GameEvents.OnClueUnlocked -= HandleClueUnlocked;
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

        if (completedBlocks.Contains(beforeDialogueBlockId))
        {
            if (completedBlocks.Contains(afterDialogueBlockId))
            {
                currentState = InvestigationState.Completed;
                DebugLog("从存档恢复状态：已完成");
            }
            else
            {
                // 检查线索是否已解锁
                if (gameFlowController.HasClue(clueId))
                {
                    currentState = InvestigationState.PlayingAfterDialogue;
                    DebugLog("从存档恢复状态：线索已解锁");
                }
                else
                {
                    currentState = InvestigationState.WaitingForClue;
                    isWaitingForClue = true;
                    DebugLog("从存档恢复状态：等待线索解锁");
                }
            }
        }
    }

    #endregion

    #region 交互执行

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
            case InvestigationState.WaitingForClue:
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
    /// 处理线索解锁事件（核心逻辑：线索解锁后触发after对话）
    /// </summary>
    private void HandleClueUnlocked(string unlockedClueId)
    {
        // 只有当我们在等待这个线索时才处理
        if (unlockedClueId == clueId && isWaitingForClue)
        {
            DebugLog($"监听到线索解锁: {clueId}，准备播放after对话");

            isWaitingForClue = false;

            // 触发after对话
            PlayAfterDialogue();
        }
    }

    #endregion

    #region 窗口管理

    private void CreateWindow()
    {
        GameObject windowObj = Instantiate(windowPrefab, canvas.transform);
        windowObj.name = $"{evidenceId}_Window";

        currentState = InvestigationState.WaitingForClue;
        isWaitingForClue = true;

        DebugLog($"已创建窗口: {windowObj.name}，等待线索解锁");
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