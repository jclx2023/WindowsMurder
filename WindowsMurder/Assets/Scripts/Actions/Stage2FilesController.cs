using System.Linq;
using UnityEngine;

/// <summary>
/// Stage2文件夹特殊交互控制器 - 监听D:\Files路径触发对话
/// 根据解锁状态触发不同对话块
/// </summary>
public class Stage2FilesController : MonoBehaviour
{
    [Header("路径配置")]
    [SerializeField] private string targetPathId = "DFiles";

    [Header("对话块配置")]
    [SerializeField] private string firstDialogueBlockId = "002";
    [SerializeField] private string unlockedDialogueBlockId = "003";

    [Header("线索配置")]
    [SerializeField] private string unlockClueId = "works_folder_unlocked";

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    private GameFlowController gameFlowController;
    private ExplorerManager currentExplorer; // 追踪当前Explorer实例

    private bool hasTriggeredFirst = false;
    private bool hasTriggeredUnlocked = false;

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        // 订阅路径变化事件
        ExplorerManager.OnAnyWindowPathChanged += OnPathChanged;

        // 订阅线索解锁事件（新增）
        GameEvents.OnClueUnlocked += OnClueUnlocked;
    }

    void OnDestroy()
    {
        ExplorerManager.OnAnyWindowPathChanged -= OnPathChanged;
        GameEvents.OnClueUnlocked -= OnClueUnlocked;
    }

    /// <summary>
    /// 路径变化事件处理
    /// </summary>
    private void OnPathChanged(ExplorerManager explorerInstance, string newPath)
    {
        LogDebug($"路径变化: {newPath}");

        // 追踪当前的Explorer实例
        if (newPath == targetPathId)
        {
            currentExplorer = explorerInstance;
            LogDebug($"进入目标路径: {targetPathId}");
        }
        else if (currentExplorer == explorerInstance)
        {
            // 离开目标路径
            currentExplorer = null;
            LogDebug($"离开目标路径");
        }

        // 检查是否是目标路径
        if (newPath != targetPathId) return;

        if (gameFlowController == null) return;

        // 检查解锁状态并触发对应对话
        bool isUnlocked = gameFlowController.HasClue(unlockClueId);

        if (isUnlocked)
        {
            TriggerUnlockedDialogue();
        }
        else
        {
            TriggerFirstDialogue();
        }
    }

    /// <summary>
    /// 线索解锁事件处理（新增）
    /// </summary>
    private void OnClueUnlocked(string clueId)
    {
        // 只关心works解锁事件
        if (clueId != unlockClueId) return;

        LogDebug($"检测到线索解锁: {clueId}");

        // 检查当前是否在目标路径
        if (currentExplorer != null)
        {
            LogDebug($"当前在目标路径，尝试触发解锁后对话");
            TriggerUnlockedDialogue();
        }
        else
        {
            LogDebug($"当前不在目标路径，等待进入时触发");
        }
    }

    /// <summary>
    /// 触发首次对话（未解锁状态）
    /// </summary>
    private void TriggerFirstDialogue()
    {
        if (hasTriggeredFirst)
        {
            LogDebug("首次对话已触发过");
            return;
        }

        if (gameFlowController.GetCompletedBlocksSafe().Contains(firstDialogueBlockId))
        {
            LogDebug($"对话块 {firstDialogueBlockId} 已完成");
            hasTriggeredFirst = true;
            return;
        }

        LogDebug($"触发首次对话块: {firstDialogueBlockId}");
        gameFlowController.StartDialogueBlock(firstDialogueBlockId);
        hasTriggeredFirst = true;
    }

    /// <summary>
    /// 触发解锁后对话
    /// </summary>
    private void TriggerUnlockedDialogue()
    {
        if (hasTriggeredUnlocked)
        {
            LogDebug("解锁后对话已触发过");
            return;
        }

        if (gameFlowController.GetCompletedBlocksSafe().Contains(unlockedDialogueBlockId))
        {
            LogDebug($"对话块 {unlockedDialogueBlockId} 已完成");
            hasTriggeredUnlocked = true;
            return;
        }

        LogDebug($"触发解锁后对话块: {unlockedDialogueBlockId}");
        gameFlowController.StartDialogueBlock(unlockedDialogueBlockId);
        hasTriggeredUnlocked = true;
    }

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage2Files] {message}");
        }
    }
    #endregion
}
