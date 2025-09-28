using System.Linq;
using UnityEngine;

/// <summary>
/// Stage2�ļ������⽻�������� - ����D:\Files·�������Ի�
/// </summary>
public class Stage2FilesController : MonoBehaviour
{
    [Header("����")]
    public string targetPathId = "DFiles";      // Ŀ��·��ID
    public string dialogueBlockId = "002";       // Ҫ�����ĶԻ���ID

    private GameFlowController gameFlowController;
    private bool hasTriggered = false;           // �Ƿ��Ѵ�����

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