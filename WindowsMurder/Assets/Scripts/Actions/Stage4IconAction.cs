using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 对话阶段枚举
/// </summary>
public enum DialogueStage
{
    Locked,      // 尚未满足访问条件
    Active,      // 可正常对话
    Completed    // 已完成主要对话
}

/// <summary>
/// Stage4专用的Icon交互基类
/// 支持前置、主要、后续三阶段对话管理
/// </summary>
public class Stage4IconAction : IconAction
{
    [Header("=== Stage4 对话配置 ===")]
    [Tooltip("前置对话块ID（条件不满足时播放，可选）")]
    [SerializeField] private string preDialogueBlockId = "";
    [SerializeField] private string mainDialogueBlockId = "";
    [SerializeField] private string postDialogueBlockId = "";

    [Header("=== 解锁条件 ===")]
    [SerializeField] private List<string> requiredClues = new List<string>();

    [SerializeField] private List<string> requiredDialogueBlocks = new List<string>();

    [Header("=== 调试信息 ===")]
    [SerializeField] private DialogueStage currentStage;
    [SerializeField] private bool debugMode = true;

    // 缓存的引用
    private GameFlowController flowController;

    #region 初始化

    protected virtual void Awake()
    {
        // 缓存GameFlowController
        flowController = FindObjectOfType<GameFlowController>();
    }

    protected virtual void OnEnable()
    {
        // 订阅对话完成事件，自动更新状态
        GameEvents.OnDialogueBlockCompleted += OnDialogueBlockCompleted;
    }

    protected virtual void OnDisable()
    {
        // 取消订阅
        GameEvents.OnDialogueBlockCompleted -= OnDialogueBlockCompleted;
    }

    #endregion

    #region IconAction实现

    public override void Execute()
    {

        // 更新当前状态
        currentStage = GetCurrentDialogueStage();

        // 根据状态播放对应对话
        PlayDialogueByStage();
    }

    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;
        if (flowController == null) return false;

        // 更新状态用于判断
        currentStage = GetCurrentDialogueStage();

        // 根据状态判断是否有可播放的对话
        switch (currentStage)
        {
            case DialogueStage.Locked:
                return !string.IsNullOrEmpty(preDialogueBlockId);

            case DialogueStage.Active:
                return !string.IsNullOrEmpty(mainDialogueBlockId);

            case DialogueStage.Completed:
                return !string.IsNullOrEmpty(postDialogueBlockId);

            default:
                return false;
        }
    }

    #endregion

    #region 状态判断

    /// <summary>
    /// 获取当前对话阶段
    /// </summary>
    private DialogueStage GetCurrentDialogueStage()
    {
        // 检查主对话是否已完成
        if (IsDialogueBlockCompleted(mainDialogueBlockId))
        {
            return DialogueStage.Completed;
        }

        // 检查是否满足解锁条件
        if (CheckLockedConditions())
        {
            return DialogueStage.Locked;
        }

        // 条件满足且未完成
        return DialogueStage.Active;
    }

    /// <summary>
    /// 检查是否处于锁定状态（前置条件不满足）
    /// </summary>
    private bool CheckLockedConditions()
    {
        // 检查必需的线索
        if (requiredClues != null && requiredClues.Count > 0)
        {
            foreach (string clueId in requiredClues)
            {
                if (!flowController.HasClue(clueId))
                {
                    LogDebug($"缺少必需线索: {clueId}");
                    return true; // 条件不满足，保持锁定
                }
            }
        }

        // 检查必需的前置对话块
        if (requiredDialogueBlocks != null && requiredDialogueBlocks.Count > 0)
        {
            var completedBlocks = flowController.GetCompletedBlocksSafe();

            foreach (string blockId in requiredDialogueBlocks)
            {
                if (!completedBlocks.Contains(blockId))
                {
                    LogDebug($"缺少必需对话块: {blockId}");
                    return true; // 条件不满足，保持锁定
                }
            }
        }

        return false; // 所有条件满足
    }

    /// <summary>
    /// 检查对话块是否已完成
    /// </summary>
    private bool IsDialogueBlockCompleted(string blockId)
    {
        if (string.IsNullOrEmpty(blockId)) return false;

        var completedBlocks = flowController.GetCompletedBlocksSafe();
        return completedBlocks.Contains(blockId);
    }

    #endregion

    #region 对话播放

    /// <summary>
    /// 根据当前阶段播放对应对话
    /// </summary>
    private void PlayDialogueByStage()
    {
        string dialogueBlockId = "";

        switch (currentStage)
        {
            case DialogueStage.Locked:
                dialogueBlockId = preDialogueBlockId;
                LogDebug("尝试播放前置对话");
                break;

            case DialogueStage.Active:
                dialogueBlockId = mainDialogueBlockId;
                LogDebug("播放主要对话");
                break;

            case DialogueStage.Completed:
                dialogueBlockId = postDialogueBlockId;
                LogDebug("播放后续对话");
                break;
        }

        // 如果对话块ID为空，则不允许访问
        if (string.IsNullOrEmpty(dialogueBlockId))
        {
            LogDebug($"{currentStage}阶段没有配置对话块，无法访问");
            ShowNoDialogueHint();
            return;
        }

        // 播放对话
        PlayDialogue(dialogueBlockId);
    }

    /// <summary>
    /// 播放指定的对话块
    /// </summary>
    protected virtual void PlayDialogue(string dialogueBlockId)
    {
        if (string.IsNullOrEmpty(dialogueBlockId))
        {
            LogError("对话块ID为空！");
            return;
        }

        LogDebug($"开始播放对话块: {dialogueBlockId}");

        // 通过GameFlowController开启对话
        flowController.StartDialogueBlock(dialogueBlockId);
    }

    /// <summary>
    /// 显示"无对话"提示（可被子类重写来实现特殊效果）
    /// </summary>
    protected virtual void ShowNoDialogueHint()
    {
        LogDebug("该阶段没有对话内容");
    }
    #endregion

    #region 事件回调

    /// <summary>
    /// 对话块完成时的回调
    /// </summary>
    private void OnDialogueBlockCompleted(string completedBlockId)
    {
        // 检查是否是当前icon相关的对话块
        if (completedBlockId == mainDialogueBlockId)
        {
            LogDebug($"主要对话块 {completedBlockId} 已完成");

            // 更新状态
            currentStage = DialogueStage.Completed;

            // 触发完成回调（供子类扩展）
            OnMainDialogueCompleted();
        }
    }

    /// <summary>
    /// 主要对话完成时的回调（供子类重写）
    /// </summary>
    protected virtual void OnMainDialogueCompleted()
    {
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage4Icon:{actionName}] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Stage4Icon:{actionName}] {message}");
    }

    #endregion
}