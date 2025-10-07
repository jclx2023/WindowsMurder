using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Knife 属性窗口控制器 - Tab 切换 + 首次查看对话触发
/// </summary>
public class KnifePropertiesWindow : MonoBehaviour
{
    [Header("=== Tab 面板配置 ===")]
    [SerializeField] private GameObject generalPanel;
    [SerializeField] private GameObject detailsPanel;

    [Header("=== Details 首次查看对话配置 ===")]
    [SerializeField] private string detailsDialogueBlockId = "knife_details_dialogue";
    [SerializeField] private float dialogueDelay = 1f;
    [Tooltip("是否在打开对话时关闭窗口")]
    [SerializeField] private bool closeWindowOnDialogue = false;

    [Header("=== 按钮引用 ===")]
    [SerializeField] private Button generalTabButton;
    [SerializeField] private Button detailsTabButton;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button applyButton;

    [Header("=== Tab 按钮视觉状态（可选）===")]
    [SerializeField] private Color activeTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = true;

    // 运行时状态
    private bool hasViewedDetails = false;

    // 组件引用
    private GameFlowController flowController;
    private WindowsWindow windowComponent;

    // Tab 按钮的 Image 组件（用于视觉反馈）
    private Image generalTabImage;
    private Image detailsTabImage;

    #region 初始化

    void Awake()
    {
        // 获取组件引用
        flowController = FindObjectOfType<GameFlowController>();
        windowComponent = GetComponent<WindowsWindow>();

        // 获取 Tab 按钮的 Image 组件
        if (generalTabButton != null)
            generalTabImage = generalTabButton.GetComponent<Image>();
        if (detailsTabButton != null)
            detailsTabImage = detailsTabButton.GetComponent<Image>();

        // 绑定按钮事件
        BindButtonEvents();
    }

    void Start()
    {
        // 默认显示 General 页面
        ShowGeneralTab();
    }

    /// <summary>
    /// 绑定所有按钮点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (generalTabButton != null)
            generalTabButton.onClick.AddListener(OnGeneralTabClick);

        if (detailsTabButton != null)
            detailsTabButton.onClick.AddListener(OnDetailsTabClick);

        if (okButton != null)
            okButton.onClick.AddListener(OnOKClick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClick);
    }

    #endregion

    #region Tab 切换逻辑

    /// <summary>
    /// 点击 General Tab 按钮
    /// </summary>
    private void OnGeneralTabClick()
    {
        LogDebug("切换到 General 页面");
        ShowGeneralTab();
    }

    /// <summary>
    /// 点击 Details Tab 按钮
    /// </summary>
    private void OnDetailsTabClick()
    {
        LogDebug("切换到 Details 页面");
        ShowDetailsTab();

        // 第一次查看 Details 时触发对话
        if (!hasViewedDetails)
        {
            hasViewedDetails = true;
            StartCoroutine(PlayDetailsDialogueDelayed());
        }
    }

    /// <summary>
    /// 显示 General 页面（通过层级控制）
    /// </summary>
    private void ShowGeneralTab()
    {
        if (generalPanel != null)
        {
            // 将 General 面板移到最上层（最后的 Sibling Index）
            generalPanel.transform.SetAsLastSibling();
            UpdateTabButtonStates(true);
        }
    }

    /// <summary>
    /// 显示 Details 页面（通过层级控制）
    /// </summary>
    private void ShowDetailsTab()
    {
        if (detailsPanel != null)
        {
            // 将 Details 面板移到最上层（最后的 Sibling Index）
            detailsPanel.transform.SetAsLastSibling();
            UpdateTabButtonStates(false);
        }
    }

    /// <summary>
    /// 更新 Tab 按钮的视觉状态
    /// </summary>
    private void UpdateTabButtonStates(bool isGeneralActive)
    {
        if (generalTabImage != null)
        {
            generalTabImage.color = isGeneralActive ? activeTabColor : inactiveTabColor;
        }

        if (detailsTabImage != null)
        {
            detailsTabImage.color = isGeneralActive ? inactiveTabColor : activeTabColor;
        }

        LogDebug($"Tab 状态更新 - General 激活: {isGeneralActive}");
    }

    #endregion

    #region 对话触发逻辑

    /// <summary>
    /// 延迟播放 Details 对话块
    /// </summary>
    private IEnumerator PlayDetailsDialogueDelayed()
    {
        LogDebug($"将在 {dialogueDelay} 秒后播放对话块: {detailsDialogueBlockId}");

        yield return new WaitForSeconds(dialogueDelay);

        // 直接调用 GameFlowController 触发对话块
        if (flowController != null)
        {
            flowController.StartDialogueBlock(detailsDialogueBlockId);
            LogDebug($"已触发对话块: {detailsDialogueBlockId}");
        }
    }

    #endregion

    #region 按钮事件处理

    /// <summary>
    /// OK 按钮点击 - 关闭窗口
    /// </summary>
    private void OnOKClick()
    {
        LogDebug("点击 OK 按钮");
        CloseWindow();
    }

    /// <summary>
    /// Cancel 按钮点击 - 关闭窗口
    /// </summary>
    private void OnCancelClick()
    {
        LogDebug("点击 Cancel 按钮");
        CloseWindow();
    }

    /// <summary>
    /// Apply 按钮点击 - 保持窗口打开（WinXP 风格）
    /// </summary>
    private void OnApplyClick()
    {
        LogDebug("点击 Apply 按钮（无操作）");
    }

    #endregion

    #region 核心功能

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void CloseWindow()
    {
        if (windowComponent != null)
        {
            windowComponent.CloseWindow();
        }
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[KnifeProperties] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[KnifeProperties] {message}");
    }
    #endregion
}