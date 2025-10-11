using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 窗口层级信息
/// </summary>
[System.Serializable]
public class WindowHierarchyInfo
{
    public Transform parentContainer;    // 父容器（如Canvas/Stage1）
    public string containerPath;        // 完整路径（如"Canvas/Stage1"）
    public int hierarchyLevel;          // 层级深度
    public string containerTag;         // 容器标签（可用于分类）

    public WindowHierarchyInfo(Transform parent)
    {
        parentContainer = parent;
        containerPath = GetFullPath(parent);
        hierarchyLevel = GetHierarchyDepth(parent);
        containerTag = parent.tag;
    }

    private string GetFullPath(Transform transform)
    {
        if (transform.parent == null) return transform.name;
        return GetFullPath(transform.parent) + "/" + transform.name;
    }

    private int GetHierarchyDepth(Transform transform)
    {
        int depth = 0;
        Transform current = transform;
        while (current.parent != null)
        {
            depth++;
            current = current.parent;
        }
        return depth;
    }
}

/// <summary>
/// 增强窗口管理器 - 支持跨场景和层级管理
/// </summary>
public class WindowManager : MonoBehaviour
{
    [Header("窗口容器")]
    [SerializeField] private Transform windowContainer;

    [Header("窗口管理设置")]
    [SerializeField] private Vector2 defaultWindowPosition = Vector2.zero;
    [SerializeField] private Vector2 cascadeOffset = new Vector2(30f, -30f);
    [SerializeField] private bool autoArrangeNewWindows = true;

    // 窗口管理
    private List<WindowsWindow> activeWindows = new List<WindowsWindow>();
    private WindowsWindow activeWindow;

    // 场景和层级管理
    private string currentSceneName;
    private Dictionary<string, List<WindowsWindow>> sceneWindows = new Dictionary<string, List<WindowsWindow>>();
    private Dictionary<string, List<WindowsWindow>> hierarchyWindows = new Dictionary<string, List<WindowsWindow>>();
    private Dictionary<WindowsWindow, WindowHierarchyInfo> windowHierarchyMap = new Dictionary<WindowsWindow, WindowHierarchyInfo>();

    // 单例
    public static WindowManager Instance { get; private set; }

    // 事件
    public static event System.Action<WindowsWindow, WindowHierarchyInfo> OnWindowRegistered;
    public static event System.Action<WindowsWindow, WindowHierarchyInfo> OnWindowUnregistered;
    public static event System.Action<string> OnSceneWindowsChanged;
    public static event System.Action<string> OnHierarchyWindowsChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentSceneName = SceneManager.GetActiveScene().name;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (windowContainer == null)
            windowContainer = transform;
    }

    void Start()
    {
        // 订阅窗口事件
        WindowsWindow.OnWindowClosed += OnWindowClosed;
        WindowsWindow.OnWindowSelected += OnWindowSelected;

        // 订阅场景切换事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // 初始化当前场景的窗口列表
        if (!sceneWindows.ContainsKey(currentSceneName))
        {
            sceneWindows[currentSceneName] = new List<WindowsWindow>();
        }

        Debug.Log($"WindowManager初始化完成，当前场景: {currentSceneName}");
    }

    void OnDestroy()
    {
        // 取消订阅
        WindowsWindow.OnWindowClosed -= OnWindowClosed;
        WindowsWindow.OnWindowSelected -= OnWindowSelected;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    #region 公共方法

    /// <summary>
    /// 注册窗口到管理器（窗口自注册使用）
    /// </summary>
    public void RegisterWindow(WindowsWindow window)
    {
        if (window == null)
        {
            Debug.LogWarning("WindowManager: 尝试注册空窗口");
            return;
        }

        // 获取窗口层级信息
        WindowHierarchyInfo hierarchyInfo = new WindowHierarchyInfo(window.transform.parent);
        RegisterWindow(window, hierarchyInfo);
    }

    /// <summary>
    /// 注册窗口到管理器（带层级信息）
    /// </summary>
    public void RegisterWindow(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        if (window == null)
        {
            Debug.LogWarning("WindowManager: 尝试注册空窗口");
            return;
        }

        if (activeWindows.Contains(window))
        {
            Debug.LogWarning($"窗口已经注册过: {window.Title}");
            return;
        }

        // 确定窗口所属场景
        string windowSceneName = GetWindowSceneName(window);

        // 添加到总列表
        activeWindows.Add(window);
        activeWindow = window;

        // 保存层级信息
        windowHierarchyMap[window] = hierarchyInfo;

        // 添加到场景列表
        if (!sceneWindows.ContainsKey(windowSceneName))
        {
            sceneWindows[windowSceneName] = new List<WindowsWindow>();
        }
        sceneWindows[windowSceneName].Add(window);

        // 添加到层级列表
        string hierarchyKey = hierarchyInfo.containerPath;
        if (!hierarchyWindows.ContainsKey(hierarchyKey))
        {
            hierarchyWindows[hierarchyKey] = new List<WindowsWindow>();
        }
        hierarchyWindows[hierarchyKey].Add(window);

        // 处理窗口位置（仅在同一容器内自动排列）
        if (autoArrangeNewWindows && !window.ShouldSkipAutoArrange())
        {
            ArrangeWindowInHierarchy(window, hierarchyKey);
        }

        // 通知TaskBar和其他监听者
        OnWindowRegistered?.Invoke(window, hierarchyInfo);
        OnSceneWindowsChanged?.Invoke(windowSceneName);
        OnHierarchyWindowsChanged?.Invoke(hierarchyKey);

        //Debug.Log($"窗口已注册: {window.Title} (场景: {windowSceneName}, 层级: {hierarchyKey})");
    }

    /// <summary>
    /// 手动注销窗口
    /// </summary>
    public void UnregisterWindow(WindowsWindow window)
    {
        if (!activeWindows.Contains(window)) return;

        string windowSceneName = GetWindowSceneName(window);
        WindowHierarchyInfo hierarchyInfo = windowHierarchyMap.ContainsKey(window) ? windowHierarchyMap[window] : null;
        string hierarchyKey = hierarchyInfo?.containerPath ?? "";

        // 从各个列表中移除
        activeWindows.Remove(window);

        if (sceneWindows.ContainsKey(windowSceneName))
        {
            sceneWindows[windowSceneName].Remove(window);
        }

        if (!string.IsNullOrEmpty(hierarchyKey) && hierarchyWindows.ContainsKey(hierarchyKey))
        {
            hierarchyWindows[hierarchyKey].Remove(window);
        }

        if (windowHierarchyMap.ContainsKey(window))
        {
            windowHierarchyMap.Remove(window);
        }

        // 通知监听者
        OnWindowUnregistered?.Invoke(window, hierarchyInfo);
        OnSceneWindowsChanged?.Invoke(windowSceneName);
        if (!string.IsNullOrEmpty(hierarchyKey))
        {
            OnHierarchyWindowsChanged?.Invoke(hierarchyKey);
        }

        // 选择下一个活动窗口
        if (activeWindow == window)
        {
            activeWindow = activeWindows.Count > 0 ? activeWindows[activeWindows.Count - 1] : null;
        }

    }

    /// <summary>
    /// 激活指定窗口（供TaskBar调用）
    /// </summary>
    public void ActivateWindow(WindowsWindow window)
    {
        if (window == null)
        {
            Debug.LogWarning("WindowManager: 尝试激活空窗口");
            return;
        }

        if (activeWindows.Contains(window))
        {
            // 获取窗口层级信息
            if (windowHierarchyMap.ContainsKey(window))
            {
                WindowHierarchyInfo hierarchyInfo = windowHierarchyMap[window];

                // 在同一层级内激活窗口
                ActivateWindowInHierarchy(window, hierarchyInfo);
            }
            else
            {
                // 后备方案：直接激活
                window.BringToFront();
            }

            Debug.Log($"窗口已激活: {window.Title}");
        }
        else
        {
            Debug.LogWarning($"窗口未注册，无法激活: {window.Title}");
        }
    }

    /// <summary>
    /// 获取窗口的层级信息
    /// </summary>
    public WindowHierarchyInfo GetWindowHierarchyInfo(WindowsWindow window)
    {
        return windowHierarchyMap.ContainsKey(window) ? windowHierarchyMap[window] : null;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取窗口所属场景名称
    /// </summary>
    private string GetWindowSceneName(WindowsWindow window)
    {
        if (window.transform.root.gameObject.scene.name == "DontDestroyOnLoad")
        {
            return "DontDestroyOnLoad";
        }
        return window.gameObject.scene.name;
    }

    /// <summary>
    /// 在层级内排列窗口位置
    /// </summary>
    private void ArrangeWindowInHierarchy(WindowsWindow window, string hierarchyKey)
    {
        if (!hierarchyWindows.ContainsKey(hierarchyKey)) return;

        int windowIndex = hierarchyWindows[hierarchyKey].Count - 1;
        Vector2 newPosition = defaultWindowPosition + cascadeOffset * windowIndex;

        RectTransform rect = window.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = newPosition;
        }
    }

    /// <summary>
    /// 在层级内激活窗口
    /// </summary>
    private void ActivateWindowInHierarchy(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        // 在同一父容器内将窗口置顶
        window.transform.SetAsLastSibling();

        // 调用窗口的激活方法
        window.BringToFront();

        Debug.Log($"在层级 '{hierarchyInfo.containerPath}' 中激活窗口: {window.Title}");
    }

    /// <summary>
    /// 清理无效的窗口引用
    /// </summary>
    private void CleanupInvalidWindows()
    {
        // 清理总列表
        activeWindows.RemoveAll(w => w == null);

        // 清理场景列表
        var scenesToRemove = new List<string>();
        foreach (var kvp in sceneWindows)
        {
            kvp.Value.RemoveAll(w => w == null);
            if (kvp.Value.Count == 0)
            {
                scenesToRemove.Add(kvp.Key);
            }
        }
        foreach (var scene in scenesToRemove)
        {
            sceneWindows.Remove(scene);
        }

        // 清理层级列表
        var hierarchiesToRemove = new List<string>();
        foreach (var kvp in hierarchyWindows)
        {
            kvp.Value.RemoveAll(w => w == null);
            if (kvp.Value.Count == 0)
            {
                hierarchiesToRemove.Add(kvp.Key);
            }
        }
        foreach (var hierarchy in hierarchiesToRemove)
        {
            hierarchyWindows.Remove(hierarchy);
        }

        // 清理层级映射
        var keysToRemove = new List<WindowsWindow>();
        foreach (var kvp in windowHierarchyMap)
        {
            if (kvp.Key == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            windowHierarchyMap.Remove(key);
        }

        // 更新活动窗口
        if (activeWindow == null && activeWindows.Count > 0)
        {
            activeWindow = activeWindows[activeWindows.Count - 1];
        }
    }

    #endregion

    #region 事件处理

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        currentSceneName = sceneName;

        if (!sceneWindows.ContainsKey(sceneName))
        {
            sceneWindows[sceneName] = new List<WindowsWindow>();
        }

        StartCoroutine(DelayedCleanup());
        Debug.Log($"场景加载完成: {sceneName}");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        string sceneName = scene.name;

        if (sceneWindows.ContainsKey(sceneName))
        {
            var sceneWindowList = new List<WindowsWindow>(sceneWindows[sceneName]);
            foreach (var window in sceneWindowList)
            {
                if (window != null)
                {
                    UnregisterWindow(window);
                }
            }
            sceneWindows.Remove(sceneName);
        }

        Debug.Log($"场景卸载完成: {sceneName}，已清理相关窗口");
    }

    private IEnumerator DelayedCleanup()
    {
        yield return null;
        CleanupInvalidWindows();
    }

    private void OnWindowClosed(WindowsWindow window)
    {
        UnregisterWindow(window);
    }

    private void OnWindowSelected(WindowsWindow window)
    {
        activeWindow = window;

        if (activeWindows.Contains(window))
        {
            activeWindows.Remove(window);
            activeWindows.Add(window);
        }

    }

    #endregion
}