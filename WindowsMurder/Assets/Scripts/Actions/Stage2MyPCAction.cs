using UnityEngine;

/// <summary>
/// Stage2�ҵĵ���ͼ�꽻����Ϊ - �����ļ��д���
/// </summary>
public class Stage2MyPCAction : IconAction
{
    [Header("��������")]
    public GameObject folderWindowPrefab;      // �ļ��д���Ԥ����
    public Transform windowParent;             // �������ɵĸ�����
    public string defaultPathId = "root";      // Ĭ�ϴ�·��

    public override void Execute()
    {
        if (folderWindowPrefab != null && windowParent != null)
        {
            // ʵ�����ļ��д���
            GameObject windowInstance = Instantiate(folderWindowPrefab, windowParent);

            // ���ó�ʼ·��
            ExplorerManager explorerManager = windowInstance.GetComponent<ExplorerManager>();
            if (explorerManager != null && !string.IsNullOrEmpty(defaultPathId))
            {
                explorerManager.NavigateToPath(defaultPathId);
            }
        }
    }
}