using UnityEngine;
using System.Collections;

/// <summary>
/// Stage3初始化器 - 负责Stage3激活时创建Explorer窗口并应用窗口转换数据
/// </summary>
public class Stage3Initializer : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private GameFlowController flowController;
    [SerializeField] private Transform canvasTransform;  // 窗口生成的Canvas

    [Header("预制体配置")]
    [SerializeField] private GameObject explorerStage3Prefab;  // Stage3专用的Explorer预制体

    [Header("默认配置")]
    [SerializeField] private Vector2 defaultWindowPosition = Vector2.zero;  // 无缓存数据时的默认位置

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 防止重复初始化
    private bool hasInitialized = false;

    // Controller引用
    private Stage3Controller stage3Controller;

    #region Unity生命周期

    void Awake()
    {
        // 查找GameFlowController（如果未在Inspector中设置）
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
            if (flowController == null)
            {
                LogError("未找到GameFlowController！");
            }
        }

        // 查找Stage3Controller
        stage3Controller = GetComponent<Stage3Controller>();
    }

    void OnEnable()
    {
        // 防止重复初始化
        if (hasInitialized)
        {
            LogDebug("已经初始化过，跳过");
            return;
        }

        // 执行初始化
        InitializeStage3();
        hasInitialized = true;
    }

    #endregion

    #region 初始化逻辑

    /// <summary>
    /// 初始化Stage3 - 创建Explorer窗口
    /// </summary>
    private void InitializeStage3()
    {
        LogDebug("开始初始化Stage3");

        // 从GameFlowController消费窗口转换数据
        WindowTransitionData? transitionData = flowController.ConsumeWindowTransition();

        // 确定窗口位置
        Vector2 windowPosition;
        if (transitionData.HasValue)
        {
            windowPosition = transitionData.Value.windowPosition;
            LogDebug($"使用缓存的窗口位置: {windowPosition}");
        }
        else
        {
            windowPosition = defaultWindowPosition;
            LogDebug($"无缓存数据，使用默认位置: {windowPosition}");
        }

        // 实例化Explorer窗口并获取引用
        ExplorerManager explorerManager = CreateExplorerWindow(windowPosition);

        // 将Explorer引用传递给Controller
        if (stage3Controller != null && explorerManager != null)
        {
            stage3Controller.SetExplorerReference(explorerManager);
        }

        LogDebug("Stage3初始化完成");
    }

    /// <summary>
    /// 创建Explorer窗口并设置位置
    /// </summary>
    private ExplorerManager CreateExplorerWindow(Vector2 position)
    {
        GameObject explorerWindow = Instantiate(explorerStage3Prefab, canvasTransform);

        // 立即设置外部位置（在Start之前）
        WindowsWindow window = explorerWindow.GetComponent<WindowsWindow>();
        if (window != null)
        {
            window.SetExternalInitialPosition(position);
            LogDebug($"已向WindowsWindow传递初始位置: {position}");
        }

        ExplorerManager explorerManager = explorerWindow.GetComponent<ExplorerManager>();

        explorerWindow.transform.SetAsLastSibling();

        LogDebug($"Explorer窗口已创建");

        return explorerManager;
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage3Init] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Stage3Init] {message}");
    }

    #endregion
}
