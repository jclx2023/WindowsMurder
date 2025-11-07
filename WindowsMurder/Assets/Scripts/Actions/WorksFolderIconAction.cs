using UnityEngine;

/// <summary>
/// Works文件夹图标专用交互行为
/// 处理：解锁前提示、解锁后推进Stage、右键属性窗口、锁图标管理
/// </summary>
public class WorksFolderIconAction : IconAction
{
    [Header("解锁配置")]
    [SerializeField] private string clueId = "works_folder_unlocked";
    [SerializeField] private GameObject lockIconObject;  // 直接引用锁图标对象

    [Header("窗口预制体")]
    [SerializeField] private GameObject lockedMessagePrefab;
    [SerializeField] private GameObject propertiesWindowPrefab;

    [Header("窗口容器")]
    [SerializeField] private Transform windowContainer;

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 组件引用
    private GameFlowController flowController;
    private InteractableIcon iconComponent;

    #region 初始化

    void Awake()
    {
        flowController = FindObjectOfType<GameFlowController>();
        iconComponent = GetComponent<InteractableIcon>();
    }

    void OnEnable()
    {
        // 检查初始解锁状态
        CheckInitialUnlockStatus();

        // 订阅右键菜单事件
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;

        // 订阅线索解锁事件
        GameEvents.OnClueUnlocked += OnClueUnlocked;

        LogDebug("已订阅事件");
    }

    void OnDisable()
    {
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;
        GameEvents.OnClueUnlocked -= OnClueUnlocked;

        LogDebug("已取消订阅事件");
    }

    /// <summary>
    /// 检查初始解锁状态
    /// </summary>
    private void CheckInitialUnlockStatus()
    {
        if (flowController != null && IsUnlocked())
        {
            HideLockIcon();
            LogDebug("初始状态：已解锁");
        }
    }

    #endregion

    #region 双击交互

    public override void Execute()
    {
        LogDebug("双击 Works 文件夹");

        if (IsUnlocked())
        {
            ProgressToNextStage();
        }
        else
        {
            ShowLockedMessage();
        }
    }

    /// <summary>
    /// 检查是否已解锁
    /// </summary>
    private bool IsUnlocked()
    {
        if (flowController == null) return false;
        return flowController.HasClue(clueId);
    }

    /// <summary>
    /// 推进到下一Stage
    /// </summary>
    private void ProgressToNextStage()
    {
        LogDebug("尝试推进到下一Stage");

        ExplorerManager explorer = GetComponentInParent<ExplorerManager>();
        if (explorer != null)
        {
            WindowsWindow window = explorer.GetComponent<WindowsWindow>();
            if (window != null && window.windowRect != null)
            {
                Vector2 position = window.windowRect.anchoredPosition;

                WindowTransitionData transitionData = new WindowTransitionData(position);
                flowController.CacheWindowTransition(transitionData);

                LogDebug($"已缓存窗口位置: {position}");
            }
        }

        flowController.TryProgressToNextStage();
    }

    /// <summary>
    /// 显示锁定提示弹窗
    /// </summary>
    private void ShowLockedMessage()
    {
        GameObject messageWindow = Instantiate(lockedMessagePrefab, windowContainer);
        LogDebug("已显示锁定提示弹窗");
    }

    #endregion

    #region 右键菜单

    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        if (icon.gameObject != gameObject)
        {
            return;
        }

        if (itemId == "properties")
        {
            ShowPropertiesWindow();
        }
    }

    private void ShowPropertiesWindow()
    {
        GameObject propertiesWindow = Instantiate(propertiesWindowPrefab, windowContainer);
        LogDebug("已生成属性窗口");
    }

    #endregion

    #region 解锁状态管理

    /// <summary>
    /// 线索解锁事件处理
    /// </summary>
    private void OnClueUnlocked(string unlockedClueId)
    {
        if (unlockedClueId == clueId)
        {
            HideLockIcon();
            LogDebug($"Works文件夹已解锁，线索ID: {clueId}");
        }
    }

    /// <summary>
    /// 隐藏锁图标
    /// </summary>
    private void HideLockIcon()
    {
        if (lockIconObject != null)
        {
            lockIconObject.SetActive(false);
            LogDebug($"锁图标已隐藏: {lockIconObject.name}");
        }
        else
        {
            LogDebug("警告：尝试隐藏锁图标，但对象引用为空！");
        }
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[WorksFolderIcon] {message}");
        }
    }

    #endregion
}
