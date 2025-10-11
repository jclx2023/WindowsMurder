using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Goodbye.txt 属性窗口控制器 - 点击任意位置播放对话
/// </summary>
public class GoodbyeTxtPropertiesWindow : MonoBehaviour
{
    [Header("=== 对话配置 ===")]
    [SerializeField] private string dialogueBlockId = "420";

    [Header("=== 按钮引用 ===")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = true;

    // 运行时状态
    private bool hasTriggeredDialogue = false;

    // 组件引用
    private GameFlowController flowController;
    private WindowsWindow windowComponent;

    void Awake()
    {
        flowController = FindObjectOfType<GameFlowController>();
        windowComponent = GetComponent<WindowsWindow>();

        // 绑定按钮事件
        if (okButton != null)
            okButton.onClick.AddListener(CloseWindow);

        if (applyButton != null)
            applyButton.onClick.AddListener(() => LogDebug("点击 Apply 按钮"));

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CloseWindow);
    }

    void Update()
    {
        // 监听鼠标点击
        if (!hasTriggeredDialogue && Input.GetMouseButtonDown(0))
        {
            PlayDialogue();
        }
    }

    /// <summary>
    /// 播放对话块
    /// </summary>
    private void PlayDialogue()
    {
        hasTriggeredDialogue = true;

        if (flowController != null)
        {
            flowController.StartDialogueBlock(dialogueBlockId);
            LogDebug($"已触发对话块: {dialogueBlockId}");
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    private void CloseWindow()
    {
        windowComponent.CloseWindow();
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GoodbyeTxtProperties] {message}");
        }
    }
}