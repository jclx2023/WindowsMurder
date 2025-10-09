using UnityEngine;

/// <summary>
/// 窗口关闭时解锁线索组件
/// 挂载在需要在关闭时解锁线索的窗口GameObject上
/// </summary>
[RequireComponent(typeof(WindowsWindow))]
public class ClueUnlockerOnWindowClose : MonoBehaviour
{
    [Header("线索配置")]
    [Tooltip("窗口关闭后要解锁的线索ID")]
    [SerializeField] private string clueIdToUnlock = "";

    [Header("延迟设置")]
    [Tooltip("窗口关闭后等待的时间（秒），确保窗口完全销毁")]
    [SerializeField] private float delayAfterClose = 0.1f;

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 缓存的组件引用
    private WindowsWindow cachedWindow;
    private GameFlowController gameFlowController;

    void Awake()
    {
        // 缓存窗口组件
        cachedWindow = GetComponent<WindowsWindow>();
    }

    void Start()
    {
        // 查找 GameFlowController
        gameFlowController = FindObjectOfType<GameFlowController>();
    }

    void OnEnable()
    {
        // 订阅窗口关闭事件
        WindowsWindow.OnWindowClosed += OnWindowClosedHandler;
    }

    void OnDisable()
    {
        // 取消订阅，防止内存泄漏
        WindowsWindow.OnWindowClosed -= OnWindowClosedHandler;
    }

    /// <summary>
    /// 窗口关闭事件处理器
    /// </summary>
    private void OnWindowClosedHandler(WindowsWindow closedWindow)
    {
        // 检查是否是当前窗口
        if (closedWindow != cachedWindow)
        {
            return;
        }

        LogDebug($"窗口即将关闭，准备延迟解锁线索: {clueIdToUnlock}");

        // 验证 GameFlowController 仍然存在
        if (gameFlowController == null)
        {
            // 二次查找
            gameFlowController = FindObjectOfType<GameFlowController>();
        }

        gameFlowController.UnlockClueDelayed(clueIdToUnlock, delayAfterClose);
    }

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[ClueUnlocker-{gameObject.name}] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ClueUnlocker-{gameObject.name}] {message}");
    }

    #endregion
}