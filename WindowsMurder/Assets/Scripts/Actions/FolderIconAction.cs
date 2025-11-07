using UnityEngine;

/// <summary>
/// 文件夹图标交互行为 - 导航到指定路径
/// </summary>
public class FolderIconAction : IconAction
{
    [Header("文件夹配置")]
    public string targetPathId = "C_Drive";    // 目标路径ID

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
