using UnityEngine;

/// <summary>
/// Stage2�ҵĵ���ͼ�꽻����Ϊ - �����ļ��д���
/// </summary>
public class Stage2MyPCAction : IconAction
{
    [Header("��������")]
    public GameObject folderWindowPrefab;      // �ļ��д���Ԥ����
    public string defaultPathId = "root";   // Ĭ�ϴ�·��

    private Canvas parentCanvas;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public override void Execute()
    {
        if (folderWindowPrefab != null && parentCanvas != null)
        {
            // ʵ�����ļ��д���
            GameObject windowInstance = Instantiate(folderWindowPrefab, parentCanvas.transform);

            // ���ó�ʼ·��
            ExplorerManager explorerManager = windowInstance.GetComponent<ExplorerManager>();
            if (explorerManager != null && !string.IsNullOrEmpty(defaultPathId))
            {
                explorerManager.NavigateToPath(defaultPathId);
            }
        }
    }
}