using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stage3流程控制器 - 管理Stage3的所有流程逻辑
/// </summary>
public class Stage3Controller : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private GameFlowController flowController;

    [Header("桌面Icons")]
    [SerializeField] private List<GameObject> desktopIcons = new List<GameObject>();  // 桌面上的所有图标

    [Header("流程配置")]
    [SerializeField] private string targetPathId = "DFilesWorks";  // 目标导航路径
    [SerializeField] private string dialogueBlockId = "004";       // 要触发的对话块

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 私有变量
    private ExplorerManager explorerManager;
    private GameObject ieIconInExplorer;  // 运行时获取
    private List<GameObject> explorerIcons;  // 运行时获取
    private bool flowStarted = false;

    #region Unity生命周期

    void Awake()
    {
        // 查找GameFlowController
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
            if (flowController == null)
            {
                LogError("未找到GameFlowController！");
            }
        }
    }

    void Start()
    {
        // 订阅对话行事件
        DialogueUI.OnLineStarted += OnDialogueLineStarted;
        LogDebug("已订阅对话行事件");
    }

    void OnDestroy()
    {
        // 取消事件订阅
        DialogueUI.OnLineStarted -= OnDialogueLineStarted;
        LogDebug("已取消订阅对话行事件");
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 设置Explorer引用（由Initializer调用）
    /// </summary>
    public void SetExplorerReference(ExplorerManager explorer)
    {
        explorerManager = explorer;
        LogDebug("接收到Explorer引用");

        // 获取Explorer中的icons引用
        if (!GetExplorerIcons())
        {
            LogError("无法获取Explorer中的icons引用！");
            return;
        }

        // 开始Stage3流程
        if (!flowStarted)
        {
            StartCoroutine(StartStage3Flow());
            flowStarted = true;
        }
    }

    #endregion

    #region Explorer Icons获取

    /// <summary>
    /// 从ExplorerIconGetter获取icons引用
    /// </summary>
    private bool GetExplorerIcons()
    {

        // 查找ExplorerIconGetter组件
        ExplorerIconGetter iconGetter = explorerManager.GetComponent<ExplorerIconGetter>();
        if (iconGetter == null)
        {
            LogError("Explorer预制体上缺少ExplorerIconGetter组件！");
            return false;
        }

        // 获取icons引用
        ieIconInExplorer = iconGetter.GetIEIcon();
        explorerIcons = iconGetter.GetProgramIcons();

        LogDebug($"成功获取Explorer icons: IE + {explorerIcons.Count}个程序图标");
        return true;
    }

    #endregion

    #region Stage3流程

    /// <summary>
    /// 启动Stage3流程
    /// </summary>
    private IEnumerator StartStage3Flow()
    {
        LogDebug("开始Stage3流程");

        // 等待2帧，确保ExplorerManager完成初始化
        yield return null;
        yield return null;

        // 导航到Works文件夹
        bool navigationSuccess = NavigateToWorksFolder();

        if (navigationSuccess)
        {
            LogDebug("导航成功，触发对话块");
            // 触发对话块004
            flowController.StartDialogueBlock(dialogueBlockId);
        }
        else
        {
            LogError("导航失败！无法触发对话块");
        }
    }

    /// <summary>
    /// 导航到Works文件夹
    /// </summary>
    private bool NavigateToWorksFolder()
    {
        if (explorerManager == null)
        {
            LogError("ExplorerManager引用为空，无法导航");
            return false;
        }

        LogDebug($"尝试导航到: {targetPathId}");
        bool success = explorerManager.NavigateToPath(targetPathId);

        if (success)
        {
            LogDebug($"成功导航到: {targetPathId}");
        }
        else
        {
            LogError($"导航到 {targetPathId} 失败");
        }

        return success;
    }

    #endregion

    #region 对话事件处理

    /// <summary>
    /// 对话行开始事件处理
    /// </summary>
    private void OnDialogueLineStarted(string lineId, string characterId, string blockId, bool isPresetMode)
    {
        // 只处理对话块004的事件
        if (blockId != dialogueBlockId)
        {
            return;
        }

        LogDebug($"检测到对话块004的对话行: lineId={lineId}");

        // lineId == 11: IE出现
        if (lineId == "11")
        {
            ShowIEIcon();
        }
        // lineId == 13: 程序返回桌面
        else if (lineId == "13")
        {
            ProgramsReturnToDesktop();
        }
    }

    /// <summary>
    /// 显示IE图标
    /// </summary>
    private void ShowIEIcon()
    {
        if (ieIconInExplorer != null)
        {
            ieIconInExplorer.SetActive(true);
            LogDebug("IE图标已显示");
        }
        else
        {
            LogError("IE图标引用为空！");
        }
    }

    /// <summary>
    /// 程序返回桌面
    /// </summary>
    private void ProgramsReturnToDesktop()
    {
        LogDebug("程序返回桌面：隐藏Explorer icons，显示桌面icons");

        // 隐藏Explorer中的所有程序icons
        HideExplorerIcons();

        // 显示桌面上的所有icons
        ShowDesktopIcons();
    }

    /// <summary>
    /// 隐藏Explorer中的所有程序icons
    /// </summary>
    private void HideExplorerIcons()
    {
        // 隐藏IE
        if (ieIconInExplorer != null)
        {
            ieIconInExplorer.SetActive(false);
        }

        // 隐藏其他程序
        if (explorerIcons != null)
        {
            foreach (var icon in explorerIcons)
            {
                if (icon != null)
                {
                    icon.SetActive(false);
                }
            }
        }

        LogDebug($"已隐藏Explorer中的 {(explorerIcons != null ? explorerIcons.Count + 1 : 1)} 个图标");
    }

    /// <summary>
    /// 显示桌面上的所有icons
    /// </summary>
    private void ShowDesktopIcons()
    {
        if (desktopIcons != null)
        {
            foreach (var icon in desktopIcons)
            {
                if (icon != null)
                {
                    icon.SetActive(true);
                }
            }

            LogDebug($"已显示桌面上的 {desktopIcons.Count} 个图标");
        }
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage3Controller] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Stage3Controller] {message}");
    }

    #endregion
}