using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Goodbye.txt 属性窗口控制器 - 打开窗口时延迟播放对话
/// </summary>
public class GoodbyeTxtPropertiesWindow : MonoBehaviour
{
    [Header("=== 对话配置 ===")]
    [SerializeField] private string dialogueBlockId = "420";
    [Tooltip("延迟播放对话的时间（秒）")]
    [SerializeField] private float dialogueDelay = 1f;
    [Tooltip("是否在打开对话时关闭窗口")]
    [SerializeField] private bool closeWindowOnDialogue = false;

    [Header("=== 按钮引用 ===")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("=== 线索配置（可选）===")]
    [SerializeField] private bool enableClueUnlock = false;
    [SerializeField] private string clueId = "fake_goodbye";

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = true;

    // 运行时状态
    private bool hasTriggeredDialogue = false;
    private bool hasUnlockedClue = false;

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
        // 检查线索是否已解锁
        CheckInitialClueState();

        // 窗口打开后延迟播放对话
        StartCoroutine(PlayDialogueDelayed());
    }

    /// <summary>
    /// 绑定按钮点击事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (okButton != null)
            okButton.onClick.AddListener(OnOKClick);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);
    }

    /// <summary>
    /// 检查线索初始状态
    /// </summary>
    private void CheckInitialClueState()
    {
        if (!enableClueUnlock) return;

        if (flowController != null)
        {
            hasUnlockedClue = flowController.HasClue(clueId);

            if (hasUnlockedClue)
            {
                LogDebug($"线索 {clueId} 已经解锁");
            }
        }
    }

    #endregion

    #region 对话触发逻辑

    /// <summary>
    /// 延迟播放对话块
    /// </summary>
    private IEnumerator PlayDialogueDelayed()
    {
        LogDebug($"将在 {dialogueDelay} 秒后播放对话块: {dialogueBlockId}");

        yield return new WaitForSeconds(dialogueDelay);

        // 只触发一次对话
        if (!hasTriggeredDialogue)
        {
            hasTriggeredDialogue = true;

            // 调用 GameFlowController 触发对话块
            if (flowController != null)
            {
                flowController.StartDialogueBlock(dialogueBlockId);
                LogDebug($"已触发对话块: {dialogueBlockId}");

                // 解锁线索（如果启用）
                if (enableClueUnlock && !hasUnlockedClue)
                {
                    UnlockClue();
                }

                // 如果配置了对话时关闭窗口
                if (closeWindowOnDialogue)
                {
                    CloseWindow();
                }
            }
        }
    }

    #endregion

    #region 按钮事件处理

    /// <summary>
    /// OK 按钮点击
    /// </summary>
    private void OnOKClick()
    {
        LogDebug("点击 OK 按钮");
        CloseWindow();
    }

    /// <summary>
    /// Apply 按钮点击
    /// </summary>
    private void OnApplyClick()
    {
        LogDebug("点击 Apply 按钮（无操作）");
        // Apply 按钮通常不关闭窗口，只应用更改
    }

    /// <summary>
    /// Cancel 按钮点击
    /// </summary>
    private void OnCancelClick()
    {
        LogDebug("点击 Cancel 按钮");
        CloseWindow();
    }

    #endregion

    #region 核心功能

    /// <summary>
    /// 解锁线索
    /// </summary>
    private void UnlockClue()
    {
        if (!enableClueUnlock || hasUnlockedClue) return;

        if (flowController != null)
        {
            flowController.UnlockClue(clueId);
            hasUnlockedClue = true;
            LogDebug($"成功解锁线索: {clueId}");
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void CloseWindow()
    {
        windowComponent.CloseWindow();
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GoodbyeTxtProperties] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GoodbyeTxtProperties] {message}");
    }

    #endregion
}