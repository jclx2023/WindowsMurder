using UnityEngine;

/// <summary>
/// Works文件夹图标专用交互行为
/// 处理：解锁前提示、解锁后推进Stage、右键属性窗口、锁图标管理
/// </summary>
public class WorksFolderIconAction : IconAction
{
    [Header("解锁配置")]
    [SerializeField] private string clueId = "works_folder_unlocked";
    [SerializeField] private string lockIconName = "Image_Lock";

    [Header("窗口预制体")]
    [SerializeField] private GameObject lockedMessagePrefab;     // 锁定提示弹窗
    [SerializeField] private GameObject propertiesWindowPrefab;  // 属性窗口

    [Header("窗口容器")]
    [SerializeField] private Transform windowContainer;          // 窗口生成的父对象

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 组件引用
    private GameFlowController flowController;
    private GameObject lockIconObject;
    private InteractableIcon iconComponent;

    #region 初始化

    void Awake()
    {
        // 获取组件引用
        flowController = FindObjectOfType<GameFlowController>();
        iconComponent = GetComponent<InteractableIcon>();
    }

    void Start()
    {
        // 查找锁图标子对象
        FindLockIcon();

        // 检查初始解锁状态
        CheckInitialUnlockStatus();
    }

    void OnEnable()
    {
        // 订阅右键菜单事件
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;

        // 订阅线索解锁事件
        GameEvents.OnClueUnlocked += OnClueUnlocked;

        LogDebug("已订阅事件");
    }

    void OnDisable()
    {
        // 取消订阅
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;
        GameEvents.OnClueUnlocked -= OnClueUnlocked;

        LogDebug("已取消订阅事件");
    }

    /// <summary>
    /// 查找锁图标子对象
    /// </summary>
    private void FindLockIcon()
    {
        if (!string.IsNullOrEmpty(lockIconName))
        {
            Transform lockTransform = transform.Find(lockIconName);
            if (lockTransform != null)
            {
                lockIconObject = lockTransform.gameObject;
                LogDebug($"找到锁图标: {lockIconName}");
            }
        }
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

    #region 双击交互 - IconAction重写

    public override void Execute()
    {
        LogDebug("双击 Works 文件夹");

        if (IsUnlocked())
        {
            // 已解锁 - 推进到下一Stage
            ProgressToNextStage();
        }
        else
        {
            // 未解锁 - 显示提示弹窗
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
            LogDebug("找到ExplorerManager");

            WindowsWindow window = explorer.GetComponent<WindowsWindow>();
            if (window != null && window.windowRect != null)
            {
                // 获取窗口位置
                Vector2 position = window.windowRect.anchoredPosition;

                // 缓存到GameFlowController
                WindowTransitionData transitionData = new WindowTransitionData(position);
                flowController.CacheWindowTransition(transitionData);

                LogDebug($"已缓存窗口位置: {position}");
            }
            else
            {
                LogError("无法找到WindowsWindow组件或windowRect为空");
            }
        }
        else
        {
            LogError("无法找到ExplorerManager，窗口位置将不会被缓存");
        }

        flowController.TryProgressToNextStage();
    }

    /// <summary>
    /// 显示锁定提示弹窗
    /// </summary>
    private void ShowLockedMessage()
    {
        // 实例化提示弹窗
        GameObject messageWindow = Instantiate(lockedMessagePrefab, windowContainer);
        LogDebug("已显示锁定提示弹窗");
    }

    #endregion

    #region 右键菜单 - 属性窗口

    /// <summary>
    /// 右键菜单项点击事件处理
    /// </summary>
    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        // 检查是否是自己触发的
        if (icon.gameObject != gameObject)
        {
            Debug.Log("不是自己触发的，忽略");
            return;
        }

        // 检查是否是Properties菜单项
        if (itemId == "properties")
        {
            Debug.Log("匹配成功，显示属性窗口");
            ShowPropertiesWindow();
        }
        else
        {
            Debug.Log($"itemId不匹配，期望'properties'，实际'{itemId}'");
        }
    }

    /// <summary>
    /// 显示属性窗口
    /// </summary>
    private void ShowPropertiesWindow()
    {
        // 实例化属性窗口
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
            LogDebug("锁图标已隐藏");
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

    private void LogError(string message)
    {
        Debug.LogError($"[WorksFolderIcon] {message}");
    }

    #endregion
}