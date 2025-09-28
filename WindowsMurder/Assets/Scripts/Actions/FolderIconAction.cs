using UnityEngine;

/// <summary>
/// �ļ���ͼ�꽻����Ϊ - ������ָ��·��
/// </summary>
public class FolderIconAction : IconAction
{
    [Header("�ļ�������")]
    public string targetPathId = "C_Drive";    // Ŀ��·��ID

    private ExplorerManager explorerManager;

    void Start()
    {
        explorerManager = GetComponentInParent<ExplorerManager>();
    }

    public override void Execute()
    {
        if (explorerManager != null)
        {
            explorerManager.NavigateToPath(targetPathId);
        }
    }
}