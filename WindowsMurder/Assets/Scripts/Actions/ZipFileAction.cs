using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zip�ļ�������Ϊ - ��ѹ�ļ���������󣩲����ŶԻ�
/// </summary>
public class ZipFileAction : IconAction
{
    [Header("��ѹ����")]
    public List<GameObject> filesToActivate = new List<GameObject>(); // ��ѹ�󼤻���ļ�����

    [Header("�Ի�����")]
    public string dialogueBlockId = ""; // ��ѹ�󲥷ŵĶԻ���ID

    public override void Execute()
    {
        // ��������"��ѹ"���ļ�
        foreach (GameObject file in filesToActivate)
        {
            if (file != null)
            {
                file.SetActive(true);
            }
        }

        // ���ŶԻ���
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
