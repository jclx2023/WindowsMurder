using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �򵥴��ڹ�����
/// ��������ڵĲ㼶�ͻ�������
/// </summary>
public class WindowManager : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private Transform windowContainer;

    // ���ڹ���
    private List<WindowsWindow> activeWindows = new List<WindowsWindow>();
    private WindowsWindow activeWindow;

    // ����
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
        // ���Ĵ����¼�
        WindowsWindow.OnWindowClosed += OnWindowClosed;
        WindowsWindow.OnWindowSelected += OnWindowSelected;
    }

    void OnDestroy()
    {
        // ȡ������
        WindowsWindow.OnWindowClosed -= OnWindowClosed;
        WindowsWindow.OnWindowSelected -= OnWindowSelected;
    }

    #region ��������

    /// <summary>
    /// ע�ᴰ�ڵ�������
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
    /// ������д���
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

    #region �¼�����

    private void OnWindowClosed(WindowsWindow window)
    {
        if (activeWindows.Contains(window))
        {
            activeWindows.Remove(window);
        }

        // ѡ����һ�������
        if (activeWindow == window)
        {
            activeWindow = activeWindows.Count > 0 ? activeWindows[activeWindows.Count - 1] : null;
        }

        Debug.Log($"�����ѹر�: {window.Title}");
    }

    private void OnWindowSelected(WindowsWindow window)
    {
        activeWindow = window;

        // ��ѡ�еĴ����Ƶ��б�ĩβ����ǰ�棩
        if (activeWindows.Contains(window))
        {
            activeWindows.Remove(window);
            activeWindows.Add(window);
        }
    }

    #endregion
}