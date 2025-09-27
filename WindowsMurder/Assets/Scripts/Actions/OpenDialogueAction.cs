using UnityEngine;

/// <summary>
/// ˫���򿪶Ի��Ľ�����Ϊ
/// </summary>
public class OpenDialogueAction : IconAction
{
    [Header("�Ի�����")]
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