using UnityEngine;

/// <summary>
/// Stage2我的电脑图标交互行为 - 生成文件夹窗口
/// </summary>
public class Stage2MyPCAction : IconAction
{
    [Header("窗口配置")]
    public GameObject folderWindowPrefab;      // 文件夹窗口预制体
    public string defaultPathId = "root";   // 默认打开路径

    private Canvas parentCanvas;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public override void Execute()
    {
        if (folderWindowPrefab != null && parentCanvas != null)
        {
            // 实例化文件夹窗口
            GameObject windowInstance = Instantiate(folderWindowPrefab, parentCanvas.transform);

            // 设置初始路径
            ExplorerManager explorerManager = windowInstance.GetComponent<ExplorerManager>();
            if (explorerManager != null && !string.IsNullOrEmpty(defaultPathId))
            {
                explorerManager.NavigateToPath(defaultPathId);
            }
        }
    }
}