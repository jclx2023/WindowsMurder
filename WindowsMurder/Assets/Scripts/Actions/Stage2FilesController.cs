using System.Linq;
using UnityEngine;

/// <summary>
/// Stage2文件夹特殊交互控制器 - 监听D:\Files路径触发对话
/// </summary>
public class Stage2FilesController : MonoBehaviour
{
    [Header("配置")]
    public string targetPathId = "DFiles";      // 目标路径ID
    public string dialogueBlockId = "002";       // 要触发的对话块ID

    private GameFlowController gameFlowController;
    private bool hasTriggered = false;           // 是否已触发过

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        ExplorerManager.OnAnyWindowPathChanged += OnPathChanged;
    }

    void OnDestroy()
    {
        ExplorerManager.OnAnyWindowPathChanged -= OnPathChanged;
    }

    private void OnPathChanged(ExplorerManager explorerInstance, string newPath)
    {
        if (hasTriggered || newPath != targetPathId) return;

        if (gameFlowController != null && !gameFlowController.GetCompletedBlocksSafe().Contains(dialogueBlockId))
        {
            gameFlowController.StartDialogueBlock(dialogueBlockId);
            hasTriggered = true;
        }
    }
}