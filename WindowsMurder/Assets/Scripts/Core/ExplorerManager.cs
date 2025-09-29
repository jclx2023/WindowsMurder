using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 路径信息 - 简化的路径数据结构
/// </summary>
[Serializable]
public class PathInfo
{
    [Header("路径配置")]
    public string pathId;                    // 路径ID
    public string displayName;               // 显示名称
    public GameObject contentContainer;      // 该路径下的内容容器

    [Header("状态设置")]
    public bool isAccessible = true;         // 是否可访问
    public bool requiresPermission = false;  // 是否需要权限验证

    [Header("权限条件")]
    [Tooltip("需要的线索ID列表（全部满足才能访问）")]
    public List<string> requiredClues = new List<string>();

    [Header("调试信息")]
    [SerializeField] public bool isCurrentPath = false; // Inspector中显示是否为当前路径

    public PathInfo(string id, string name, GameObject container)
    {
        pathId = id;
        displayName = name;
        contentContainer = container;
        isAccessible = true;
        requiresPermission = false;
    }
}

/// <summary>
/// 资源管理器核心控制器 - 窗口实例版本
/// 负责单个窗口的路径切换、内容显隐、地址栏更新
/// </summary>
public class ExplorerManager : MonoBehaviour
{
    [Header("UI组件引用")]
    public TextMeshProUGUI windowTitleText;          // 窗口标题文本（显示当前路径）

    [Header("权限提示窗口")]
    [SerializeField] private GameObject accessDeniedWindowPrefab;  // 权限被拒绝提示窗口预制体
    [SerializeField] private Transform notificationParent;          // 提示窗口的父对象（通常是Canvas）

    [Header("路径配置")]
    public List<PathInfo> allPaths = new List<PathInfo>();   // 所有可用路径
    public string defaultPathId = "root";                     // 默认路径

    [Header("窗口设置")]
    public string windowTypeName = "文件资源管理器";         // 窗口类型名称
    public bool showPathInTitle = true;                       // 是否在标题中显示路径

    [Header("调试设置")]
    public bool enableDebugLog = true;                        // 是否启用调试日志

    // 实例事件委托 - 每个窗口实例独立的事件
    public event Action<string> OnPathChanged;                // 路径切换事件
    public event Action<string> OnPathAccessDenied;           // 路径访问被拒绝事件

    // 静态事件委托 - 全局事件（可选）
    public static event Action<ExplorerManager, string> OnAnyWindowPathChanged;

    // 私有变量
    private string currentPathId;
    private PathInfo currentPath;
    private Dictionary<string, PathInfo> pathDictionary;
    private GameFlowController flowController;

    // 静态管理 - 跟踪所有窗口实例
    public static List<ExplorerManager> AllInstances { get; private set; } = new List<ExplorerManager>();

    #region Unity生命周期

    void Awake()
    {
        InitializeManager();
    }

    void OnEnable()
    {
        // 注册到全局实例列表
        if (!AllInstances.Contains(this))
        {
            AllInstances.Add(this);
        }

        // 订阅线索解锁事件，当线索解锁时刷新路径权限
        GameEvents.OnClueUnlocked += OnClueUnlockedHandler;
    }

    void Start()
    {
        // 导航到默认路径
        NavigateToPath(defaultPathId);
    }

    void OnDisable()
    {
        // 从全局实例列表移除
        AllInstances.Remove(this);

        // 取消订阅
        GameEvents.OnClueUnlocked -= OnClueUnlockedHandler;
    }

    void OnDestroy()
    {
        AllInstances.Remove(this);
    }

    #endregion

    #region 初始化

    void InitializeManager()
    {
        flowController = FindObjectOfType<GameFlowController>();
        // 构建路径字典
        pathDictionary = new Dictionary<string, PathInfo>();

        foreach (var path in allPaths)
        {
            if (!string.IsNullOrEmpty(path.pathId))
            {
                pathDictionary[path.pathId] = path;

                // 初始时隐藏所有内容容器
                if (path.contentContainer != null)
                {
                    path.contentContainer.SetActive(false);
                }
            }
        }

        if (enableDebugLog)
        {
            Debug.Log($"ExplorerManager ({name}): 初始化完成，加载了 {pathDictionary.Count} 个路径");
        }
    }
    #endregion

    #region 路径导航

    /// <summary>
    /// 导航到指定路径
    /// </summary>
    public bool NavigateToPath(string targetPathId)
    {

        PathInfo targetPath = pathDictionary[targetPathId];

        // 检查访问权限
        if (!CanAccessPath(targetPath))
        {
            // 触发事件
            OnPathAccessDenied?.Invoke(targetPathId);
            // 显示权限被拒绝提示窗口
            ShowAccessDeniedNotification();

            return false;
        }

        // 执行路径切换
        return SwitchToPath(targetPath);
    }

    /// <summary>
    /// 切换到指定路径
    /// </summary>
    private bool SwitchToPath(PathInfo targetPath)
    {
        string previousPathId = currentPathId;

        // 隐藏当前路径的内容
        if (currentPath?.contentContainer != null)
        {
            currentPath.contentContainer.SetActive(false);
            currentPath.isCurrentPath = false;
        }

        // 切换到新路径
        currentPathId = targetPath.pathId;
        currentPath = targetPath;

        // 显示新路径的内容
        if (currentPath.contentContainer != null)
        {
            currentPath.contentContainer.SetActive(true);
            currentPath.isCurrentPath = true;
        }

        // 更新地址栏
        UpdateWindowTitle();

        // 触发事件
        OnPathChanged?.Invoke(currentPathId);
        OnAnyWindowPathChanged?.Invoke(this, currentPathId);

        if (enableDebugLog)
        {
            Debug.Log($"ExplorerManager ({name}): 路径切换成功 - {previousPathId} → {currentPathId}");
        }

        return true;
    }

    #endregion

    #region 权限检查

    /// <summary>
    /// 检查是否可以访问指定路径
    /// </summary>
    private bool CanAccessPath(PathInfo path)
    {
        // 检查基础可访问性
        if (!path.isAccessible)
        {
            return false;
        }

        if (!path.requiresPermission)
        {
            return true;
        }

        // 检查是否需要线索权限
        if (path.requiredClues != null && path.requiredClues.Count > 0)
        {
            // 检查每个必需的线索
            foreach (string clueId in path.requiredClues)
            {
                if (!flowController.HasClue(clueId))
                {
                    if (enableDebugLog)
                    {
                        Debug.Log($"ExplorerManager ({name}): 缺少必需线索 - {clueId}");
                    }
                    return false;
                }
            }
        }

        // 通过所有检查
        return true;
    }

    /// <summary>
    /// 线索解锁事件处理器
    /// </summary>
    private void OnClueUnlockedHandler(string clueId)
    {
        // 当线索解锁时，检查是否有路径因此变得可访问
        foreach (var path in allPaths)
        {
            if (path.requiresPermission && path.requiredClues.Contains(clueId))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"ExplorerManager ({name}): 线索 {clueId} 解锁，路径 {path.pathId} 可能变得可访问");
                }
            }
        }
    }

    #endregion

    #region 权限提示窗口

    private void ShowAccessDeniedNotification()
    {
        // 确定生成位置
        Transform parent = notificationParent;
        if (parent == null)
        {
            // 查找名为 "DialogueCanvas" 的 GameObject
            GameObject dialogueCanvasObj = GameObject.Find("DialogueCanvas");
            if (dialogueCanvasObj != null)
            {
                parent = dialogueCanvasObj.transform;
            }
        }
        // 生成提示窗口（预制体中已经预设好所有内容）
        GameObject notificationObj = Instantiate(accessDeniedWindowPrefab, parent);
    }

    #endregion

    #region UI更新

    /// <summary>
    /// 更新窗口标题（地址栏）
    /// </summary>
    private void UpdateWindowTitle()
    {
        if (windowTitleText != null && currentPath != null)
        {
            string displayText;

            if (showPathInTitle)
            {
                // 组合显示：窗口类型名 - 路径
                string pathName = currentPath.displayName;
                displayText = $"{pathName}";
            }
            else
            {
                // 仅显示窗口类型名
                displayText = windowTypeName;
            }

            windowTitleText.text = displayText;
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 获取当前路径ID
    /// </summary>
    public string GetCurrentPathId()
    {
        return currentPathId;
    }

    #endregion
}