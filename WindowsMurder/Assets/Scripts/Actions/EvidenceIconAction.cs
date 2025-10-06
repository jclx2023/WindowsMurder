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
        Completed
    }

    #region Unity生命周期

    void Start()
    {
        // 查找引用
        gameFlowController = FindObjectOfType<GameFlowController>();

        // 查找Canvas：优先使用手动配置，否则从父级查找
        FindTargetCanvas();

        // 恢复状态（从存档）
        RestoreStateFromSave();
    }

    void OnEnable()
    {
        DialogueUI.OnDialogueBlockEnded += HandleDialogueEnded;
        DebugLog("已订阅对话事件");
    }

    void OnDisable()
    {
        DialogueUI.OnDialogueBlockEnded -= HandleDialogueEnded;
        DebugLog("已取消订阅对话事件");
    }

    #endregion

    #region Canvas查找

    /// <summary>
    /// 查找目标Canvas
    /// </summary>
    private void FindTargetCanvas()
    {
        // 1. 从父级查找最近的Canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            DebugLog($"从父级找到Canvas: {canvas.name}");
            return;
        }

        // 2. 如果没找到，尝试通过Tag查找（需要提前设置Tag）
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
                currentState = InvestigationState.WindowOpen;
                DebugLog("从存档恢复状态：窗口已打开过");
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

    private void HandleDialogueEnded(string fileName, string blockId)
    {
        if (blockId == waitingForDialogueBlockId)
        {
            DebugLog($"监听到对话结束: {blockId}");

            waitingForDialogueBlockId = null;
            isWaitingForDialogue = false;

            OnBeforeDialogueComplete();
        }
    }

    private void OnBeforeDialogueComplete()
    {
        DebugLog("前置对话完成，准备生成窗口");
        CreateWindow();
    }

    #endregion

    #region 窗口管理

    private void CreateWindow()
    {

        GameObject windowObj = Instantiate(windowPrefab, canvas.transform);
        windowObj.name = $"{evidenceId}_Window";

        currentState = InvestigationState.WindowOpen;

        DebugLog($"已创建窗口: {windowObj.name}，父级Canvas: {canvas.name}");
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