using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zip文件交互行为 - 解压文件（激活对象）并播放对话
/// </summary>
public class ZipFileAction : IconAction
{
    [Header("解压设置")]
    public List<GameObject> filesToActivate = new List<GameObject>(); // 解压后激活的文件对象

    [Header("对话设置")]
    public string dialogueBlockId = ""; // 解压后播放的对话块ID

    public override void Execute()
    {
        // 激活所有"解压"的文件
        foreach (GameObject file in filesToActivate)
        {
            if (file != null)
            {
                file.SetActive(true);
            }
        }

        // 播放对话块
        if (!string.IsNullOrEmpty(dialogueBlockId))
        {
            GameFlowController gameFlow = FindObjectOfType<GameFlowController>();
            if (gameFlow != null)
            {
                gameFlow.StartDialogueBlock(dialogueBlockId);
            }
        }
    }
}
