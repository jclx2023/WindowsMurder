using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单窗口管理器
/// 负责管理窗口的层级和基本操作
/// </summary>
public class WindowManager : MonoBehaviour
{
    [Header("窗口容器")]
    [SerializeField] private Transform windowContainer;

    // 窗口管理
    private List<WindowsWindow> activeWindows = new List<WindowsWindow>();
    private WindowsWindow activeWindow;

    // 单例
    public static WindowManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
    }

    void OnDestroy()
    {
        // 取消订阅
        WindowsWindow.OnWindowClosed -= OnWindowClosed;
        WindowsWindow.OnWindowSelected -= OnWindowSelected;
    }

    #region 公共方法

    /// <summary>
    /// 注册窗口到管理器
    /// </summary>
    public void RegisterWindow(WindowsWindow window)
    {
        if (!activeWindows.Contains(window))
        {
            activeWindows.Add(window);
            activeWindow = window;
        }
    }

    /// <summary>
    /// 层叠排列窗口
    /// </summary>
    public void CascadeWindows()
    {
        for (int i = 0; i < activeWindows.Count; i++)
        {
            if (activeWindows[i] != null)
            {
                RectTransform rect = activeWindows[i].GetComponent<RectTransform>();
                Vector2 cascadeOffset = new Vector2(i * 30f, -i * 30f);
                rect.anchoredPosition = cascadeOffset;
            }
        }
    }

    #endregion

    #region 事件处理

    private void OnWindowClosed(WindowsWindow window)
    {
        if (activeWindows.Contains(window))
        {
            activeWindows.Remove(window);
        }

        // 选择下一个活动窗口
        if (activeWindow == window)
        {
            activeWindow = activeWindows.Count > 0 ? activeWindows[activeWindows.Count - 1] : null;
        }

        Debug.Log($"窗口已关闭: {window.Title}");
    }

    private void OnWindowSelected(WindowsWindow window)
    {
        activeWindow = window;

        // 将选中的窗口移到列表末尾（最前面）
        if (activeWindows.Contains(window))
        {
            activeWindows.Remove(window);
            activeWindows.Add(window);
        }
    }

    #endregion
}