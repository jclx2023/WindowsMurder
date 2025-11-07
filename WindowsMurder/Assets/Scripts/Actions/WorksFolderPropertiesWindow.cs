using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Works文件夹属性窗口控制器 - 处理Unlock交互逻辑
/// </summary>
public class WorksFolderPropertiesWindow : MonoBehaviour
{
    [Header("线索配置")]
    [SerializeField] private string clueId = "works_folder_unlocked";

    [Header("按钮引用")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 运行时状态
    private bool unlockFlag = false;
    private bool isAlreadyUnlocked = false;

    // 组件引用
    private GameFlowController flowController;
    private WindowsWindow windowComponent;

    #region 初始化

    void Awake()
    {
        // 获取组件引用
        flowController = FindObjectOfType<GameFlowController>();
        windowComponent = GetComponent<WindowsWindow>();
        // 绑定按钮事件
        BindButtonEvents();
    }

    void Start()
    {
        // 检查线索是否已经解锁
        CheckInitialUnlockState();

        // 更新按钮状态
        UpdateButtonStates();
    }

    /// <summary>
    /// 绑定按钮点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        unlockButton.onClick.AddListener(OnUnlockClick);
        okButton.onClick.AddListener(OnOKClick);
        applyButton.onClick.AddListener(OnApplyClick);
        cancelButton.onClick.AddListener(OnCancelClick);
    }

    /// <summary>
    /// 检查线索初始解锁状态
    /// </summary>
    private void CheckInitialUnlockState()
    {
        if (flowController != null)
        {
            isAlreadyUnlocked = flowController.HasClue(clueId);

            if (isAlreadyUnlocked)
            {
                unlockFlag = true;
                LogDebug($"线索 {clueId} 已经解锁");
            }
        }
    }

    #endregion

    #region 按钮事件处理

    /// <summary>
    /// Unlock按钮点击
    /// </summary>
    private void OnUnlockClick()
    {
        LogDebug("点击 Unlock 按钮");

        unlockFlag = true;
        UpdateButtonStates();
    }

    /// <summary>
    /// Apply按钮点击
    /// </summary>
    private void OnApplyClick()
    {
        LogDebug("点击 Apply 按钮");

        if (unlockFlag && !isAlreadyUnlocked)
        {
            UnlockClue();
        }

        // Apply不关闭窗口，只更新状态
        UpdateButtonStates();
    }

    /// <summary>
    /// OK按钮点击
    /// </summary>
    private void OnOKClick()
    {
        LogDebug("点击 OK 按钮");

        // 如果unlockFlag为true且还未解锁，先解锁线索
        if (unlockFlag && !isAlreadyUnlocked)
        {
            UnlockClue();
        }

        // 关闭窗口
        CloseWindow();
    }

    /// <summary>
    /// Cancel按钮点击
    /// </summary>
    private void OnCancelClick()
    {
        LogDebug("点击 Cancel 按钮");

        // 重置unlockFlag（仅在未Apply时有效）
        if (!isAlreadyUnlocked)
        {
            unlockFlag = false;
        }

        // 关闭窗口
        CloseWindow();
    }

    #endregion

    #region 核心功能

    /// <summary>
    /// 解锁线索
    /// </summary>
    private void UnlockClue()
    {

        // 调用GameFlowController解锁线索
        flowController.UnlockClue(clueId);

        // 标记为已解锁
        isAlreadyUnlocked = true;

        LogDebug($"成功解锁线索: {clueId}");
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void CloseWindow()
    {
        if (windowComponent != null)
        {
            windowComponent.CloseWindow();
        }
        else
        {
            LogError("WindowsWindow 组件引用丢失，无法关闭窗口！");
        }
    }

    #endregion

    #region UI状态更新

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonStates()
    {
        // Unlock按钮：已解锁或已点击后禁用
        if (unlockButton != null)
        {
            unlockButton.interactable = !unlockFlag;
        }

        // Apply按钮：只有点击了Unlock且未Apply时才可用
        if (applyButton != null)
        {
            applyButton.interactable = unlockFlag && !isAlreadyUnlocked;
        }

        // OK和Cancel按钮始终可用
        if (okButton != null)
        {
            okButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.interactable = true;
        }

        LogDebug($"按钮状态已更新 - unlockFlag: {unlockFlag}, isAlreadyUnlocked: {isAlreadyUnlocked}");
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[WorksFolderProperties] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[WorksFolderProperties] {message}");
    }

    #endregion
}
