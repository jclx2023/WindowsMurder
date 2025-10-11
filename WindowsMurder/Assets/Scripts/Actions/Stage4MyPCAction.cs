using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Stage4我的电脑图标交互行为 - 根据对话块完成情况生成不同窗口
/// </summary>
public class Stage4MyPCAction : IconAction
{
    [Header("窗口配置")]
    public GameObject blockedWindowPrefab;     // 未完成条件时的窗口
    public GameObject folderWindowPrefab;      // 文件夹窗口预制体
    public Transform windowParent;             // 窗口生成的父对象
    public string defaultPathId = "root";      // 默认打开路径

    [Header("解锁条件")]
    public List<string> requiredDialogueBlocks = new List<string>(); // 需要完成的对话块ID

    public override void Execute()
    {
        if (windowParent == null) return;

        // 检查是否满足条件
        GameFlowController gameFlow = FindObjectOfType<GameFlowController>();
        bool isUnlocked = true;

        if (gameFlow != null && requiredDialogueBlocks.Count > 0)
        {
            var completedBlocks = gameFlow.GetCompletedBlocksSafe();
            foreach (string blockId in requiredDialogueBlocks)
            {
                if (!completedBlocks.Contains(blockId))
                {
                    isUnlocked = false;
                    break;
                }
            }
        }

        // 根据条件生成对应窗口
        if (isUnlocked && folderWindowPrefab != null)
        {
            GameObject windowInstance = Instantiate(folderWindowPrefab, windowParent);
            ExplorerManager explorerManager = windowInstance.GetComponent<ExplorerManager>();
            if (explorerManager != null && !string.IsNullOrEmpty(defaultPathId))
            {
                explorerManager.NavigateToPath(defaultPathId);
            }
        }
        else if (!isUnlocked && blockedWindowPrefab != null)
        {
            Instantiate(blockedWindowPrefab, windowParent);
        }
    }
}