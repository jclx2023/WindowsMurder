using UnityEngine;

/// <summary>
/// 双击打开对话的交互行为
/// </summary>
public class OpenDialogueAction : IconAction
{
    [Header("对话设置")]
    public string blockId = "001";

    private DialogueManager dialogueManager;

    void Start()
    {
        dialogueManager = FindObjectOfType<DialogueManager>();
    }

    public override void Execute()
    {
        dialogueManager.StartDialogue(blockId);
    }
}
