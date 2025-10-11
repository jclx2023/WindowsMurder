using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Stage4�ҵĵ���ͼ�꽻����Ϊ - ���ݶԻ������������ɲ�ͬ����
/// </summary>
public class Stage4MyPCAction : IconAction
{
    [Header("��������")]
    public GameObject blockedWindowPrefab;     // δ�������ʱ�Ĵ���
    public GameObject folderWindowPrefab;      // �ļ��д���Ԥ����
    public Transform windowParent;             // �������ɵĸ�����
    public string defaultPathId = "root";      // Ĭ�ϴ�·��

    [Header("��������")]
    public List<string> requiredDialogueBlocks = new List<string>(); // ��Ҫ��ɵĶԻ���ID

    public override void Execute()
    {
        if (windowParent == null) return;

        // ����Ƿ���������
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

        // �����������ɶ�Ӧ����
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