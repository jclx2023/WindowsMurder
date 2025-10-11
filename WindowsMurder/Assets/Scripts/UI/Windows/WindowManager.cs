using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ���ڲ㼶��Ϣ
/// </summary>
[System.Serializable]
public class WindowHierarchyInfo
{
    public Transform parentContainer;    // ����������Canvas/Stage1��
    public string containerPath;        // ����·������"Canvas/Stage1"��
    public int hierarchyLevel;          // �㼶���
    public string containerTag;         // ������ǩ�������ڷ��ࣩ

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
/// ��ǿ���ڹ����� - ֧�ֿ糡���Ͳ㼶����
/// </summary>
public class WindowManager : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private Transform windowContainer;

    [Header("���ڹ�������")]
    [SerializeField] private Vector2 defaultWindowPosition = Vector2.zero;
    [SerializeField] private Vector2 cascadeOffset = new Vector2(30f, -30f);
    [SerializeField] private bool autoArrangeNewWindows = true;

    // ���ڹ���
    private List<WindowsWindow> activeWindows = new List<WindowsWindow>();
    private WindowsWindow activeWindow;

    // �����Ͳ㼶����
    private string currentSceneName;
    private Dictionary<string, List<WindowsWindow>> sceneWindows = new Dictionary<string, List<WindowsWindow>>();
    private Dictionary<string, List<WindowsWindow>> hierarchyWindows = new Dictionary<string, List<WindowsWindow>>();
    private Dictionary<WindowsWindow, WindowHierarchyInfo> windowHierarchyMap = new Dictionary<WindowsWindow, WindowHierarchyInfo>();

    // ����
    public static WindowManager Instance { get; private set; }

    // �¼�
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
        // ���Ĵ����¼�
        WindowsWindow.OnWindowClosed += OnWindowClosed;
        WindowsWindow.OnWindowSelected += OnWindowSelected;

        // ���ĳ����л��¼�
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // ��ʼ����ǰ�����Ĵ����б�
        if (!sceneWindows.ContainsKey(currentSceneName))
        {
            sceneWindows[currentSceneName] = new List<WindowsWindow>();
        }

        Debug.Log($"WindowManager��ʼ����ɣ���ǰ����: {currentSceneName}");
    }

    void OnDestroy()
    {
        // ȡ������
        WindowsWindow.OnWindowClosed -= OnWindowClosed;
        WindowsWindow.OnWindowSelected -= OnWindowSelected;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    #region ��������

    /// <summary>
    /// ע�ᴰ�ڵ���������������ע��ʹ�ã�
    /// </summary>
    public void RegisterWindow(WindowsWindow window)
    {
        if (window == null)
        {
            Debug.LogWarning("WindowManager: ����ע��մ���");
            return;
        }

        // ��ȡ���ڲ㼶��Ϣ
        WindowHierarchyInfo hierarchyInfo = new WindowHierarchyInfo(window.transform.parent);
        RegisterWindow(window, hierarchyInfo);
    }

    /// <summary>
    /// ע�ᴰ�ڵ������������㼶��Ϣ��
    /// </summary>
    public void RegisterWindow(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        if (window == null)
        {
            Debug.LogWarning("WindowManager: ����ע��մ���");
            return;
        }

        if (activeWindows.Contains(window))
        {
            Debug.LogWarning($"�����Ѿ�ע���: {window.Title}");
            return;
        }

        // ȷ��������������
        string windowSceneName = GetWindowSceneName(window);

        // ��ӵ����б�
        activeWindows.Add(window);
        activeWindow = window;

        // ����㼶��Ϣ
        windowHierarchyMap[window] = hierarchyInfo;

        // ��ӵ������б�
        if (!sceneWindows.ContainsKey(windowSceneName))
        {
            sceneWindows[windowSceneName] = new List<WindowsWindow>();
        }
        sceneWindows[windowSceneName].Add(window);

        // ��ӵ��㼶�б�
        string hierarchyKey = hierarchyInfo.containerPath;
        if (!hierarchyWindows.ContainsKey(hierarchyKey))
        {
            hierarchyWindows[hierarchyKey] = new List<WindowsWindow>();
        }
        hierarchyWindows[hierarchyKey].Add(window);

        // ������λ�ã�����ͬһ�������Զ����У�
        if (autoArrangeNewWindows && !window.ShouldSkipAutoArrange())
        {
            ArrangeWindowInHierarchy(window, hierarchyKey);
        }

        // ֪ͨTaskBar������������
        OnWindowRegistered?.Invoke(window, hierarchyInfo);
        OnSceneWindowsChanged?.Invoke(windowSceneName);
        OnHierarchyWindowsChanged?.Invoke(hierarchyKey);

        //Debug.Log($"������ע��: {window.Title} (����: {windowSceneName}, �㼶: {hierarchyKey})");
    }

    /// <summary>
    /// �ֶ�ע������
    /// </summary>
    public void UnregisterWindow(WindowsWindow window)
    {
        if (!activeWindows.Contains(window)) return;

        string windowSceneName = GetWindowSceneName(window);
        WindowHierarchyInfo hierarchyInfo = windowHierarchyMap.ContainsKey(window) ? windowHierarchyMap[window] : null;
        string hierarchyKey = hierarchyInfo?.containerPath ?? "";

        // �Ӹ����б����Ƴ�
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

        // ֪ͨ������
        OnWindowUnregistered?.Invoke(window, hierarchyInfo);
        OnSceneWindowsChanged?.Invoke(windowSceneName);
        if (!string.IsNullOrEmpty(hierarchyKey))
        {
            OnHierarchyWindowsChanged?.Invoke(hierarchyKey);
        }

        // ѡ����һ�������
        if (activeWindow == window)
        {
            activeWindow = activeWindows.Count > 0 ? activeWindows[activeWindows.Count - 1] : null;
        }

    }

    /// <summary>
    /// ����ָ�����ڣ���TaskBar���ã�
    /// </summary>
    public void ActivateWindow(WindowsWindow window)
    {
        if (window == null)
        {
            Debug.LogWarning("WindowManager: ���Լ���մ���");
            return;
        }

        if (activeWindows.Contains(window))
        {
            // ��ȡ���ڲ㼶��Ϣ
            if (windowHierarchyMap.ContainsKey(window))
            {
                WindowHierarchyInfo hierarchyInfo = windowHierarchyMap[window];

                // ��ͬһ�㼶�ڼ����
                ActivateWindowInHierarchy(window, hierarchyInfo);
            }
            else
            {
                // �󱸷�����ֱ�Ӽ���
                window.BringToFront();
            }

            Debug.Log($"�����Ѽ���: {window.Title}");
        }
        else
        {
            Debug.LogWarning($"����δע�ᣬ�޷�����: {window.Title}");
        }
    }

    /// <summary>
    /// ��ȡ���ڵĲ㼶��Ϣ
    /// </summary>
    public WindowHierarchyInfo GetWindowHierarchyInfo(WindowsWindow window)
    {
        return windowHierarchyMap.ContainsKey(window) ? windowHierarchyMap[window] : null;
    }

    #endregion

    #region ˽�з���

    /// <summary>
    /// ��ȡ����������������
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
    /// �ڲ㼶�����д���λ��
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
    /// �ڲ㼶�ڼ����
    /// </summary>
    private void ActivateWindowInHierarchy(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        // ��ͬһ�������ڽ������ö�
        window.transform.SetAsLastSibling();

        // ���ô��ڵļ����
        window.BringToFront();

        Debug.Log($"�ڲ㼶 '{hierarchyInfo.containerPath}' �м����: {window.Title}");
    }

    /// <summary>
    /// ������Ч�Ĵ�������
    /// </summary>
    private void CleanupInvalidWindows()
    {
        // �������б�
        activeWindows.RemoveAll(w => w == null);

        // �������б�
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

        // ����㼶�б�
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

        // ����㼶ӳ��
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

        // ���»����
        if (activeWindow == null && activeWindows.Count > 0)
        {
            activeWindow = activeWindows[activeWindows.Count - 1];
        }
    }

    #endregion

    #region �¼�����

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        currentSceneName = sceneName;

        if (!sceneWindows.ContainsKey(sceneName))
        {
            sceneWindows[sceneName] = new List<WindowsWindow>();
        }

        StartCoroutine(DelayedCleanup());
        Debug.Log($"�����������: {sceneName}");
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

        Debug.Log($"����ж�����: {sceneName}����������ش���");
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