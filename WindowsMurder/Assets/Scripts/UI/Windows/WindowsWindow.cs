using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Windows风格基础窗口组件 - 支持动态标题和激活状态管理
/// </summary>
public class WindowsWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("窗口设置")]
    [SerializeField] private string windowTitle = "NewWindow"; // 仅用于编辑器显示
    [SerializeField] private Sprite windowIcon;
    [SerializeField] private bool canClose = true;

    [Header("UI组件引用")]
    [SerializeField] public RectTransform windowRect;
    [SerializeField] private RectTransform titleBarRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button closeButton;

    // 拖拽相关
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    // 初始化标记
    private bool isInitialized = false;
    private bool isRegistered = false;

    // 事件
    public static event System.Action<WindowsWindow> OnWindowClosed;
    public static event System.Action<WindowsWindow> OnWindowSelected;
    public static event System.Action<WindowsWindow> OnWindowTitleChanged;

    // 属性 - 动态读取titleText的内容
    public string Title => titleText != null ? titleText.text : windowTitle;

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        PerformOneTimeInitialization();

        if (gameObject.activeInHierarchy && isInitialized)
        {
            StartCoroutine(DelayedInitialRegister());
        }
    }

    void OnEnable()
    {
        if (isInitialized && !isRegistered)
        {
            RegisterToWindowManager();
        }
    }

    void OnDisable()
    {
        // 窗口禁用时从WindowManager注销
        if (isRegistered && WindowManager.Instance != null)
        {
            WindowManager.Instance.UnregisterWindow(this);
            isRegistered = false;
            Debug.Log($"窗口已注销（禁用）: {Title}");
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        CleanupEventListeners();
    }

    #region 初始化

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        // 获取Canvas组件
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }

        // 如果没有设置windowRect，使用自身
        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        // 设置关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseWindow);
            closeButton.gameObject.SetActive(canClose);
        }
    }

    /// <summary>
    /// 执行一次性初始化
    /// </summary>
    private void PerformOneTimeInitialization()
    {
        // 更新显示
        UpdateDisplay();

        // 确保窗口在画布范围内
        ClampToCanvas();

        // 监听语言变化事件
        LanguageManager.OnLanguageChanged += OnLanguageChanged;

        // 标记为已初始化
        isInitialized = true;

        Debug.Log($"窗口初始化完成: {Title}");
    }

    /// <summary>
    /// 清理事件监听
    /// </summary>
    private void CleanupEventListeners()
    {
        // 取消语言事件监听
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;

        // 确保从WindowManager注销（处理直接销毁的情况）
        if (isRegistered && WindowManager.Instance != null)
        {
            WindowManager.Instance.UnregisterWindow(this);
            isRegistered = false;
        }
    }

    #endregion

    #region 窗口管理器注册

    /// <summary>
    /// 延迟初始注册（等待LocalizationID更新完成）
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialRegister()
    {
        yield return null;
        yield return null;
        if (gameObject.activeInHierarchy && !isRegistered)
        {
            RegisterToWindowManager();
        }
    }

    /// <summary>
    /// 注册到窗口管理器
    /// </summary>
    private void RegisterToWindowManager()
    {
        // 防止重复注册
        if (isRegistered)
        {
            Debug.LogWarning($"窗口 {Title} 已经注册过了，跳过重复注册");
            return;
        }

        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.RegisterWindow(this);
            isRegistered = true;
            Debug.Log($"窗口已注册: {Title}");
        }
        else
        {
            // WindowManager还未初始化，延迟注册
            StartCoroutine(DelayedRegister());
        }
    }

    /// <summary>
    /// 延迟注册（处理初始化顺序问题）
    /// </summary>
    private System.Collections.IEnumerator DelayedRegister()
    {
        // 等待WindowManager初始化
        while (WindowManager.Instance == null)
        {
            yield return null;
        }

        // 再次检查窗口是否仍然激活且未注册
        if (gameObject.activeInHierarchy && !isRegistered)
        {
            WindowManager.Instance.RegisterWindow(this);
            isRegistered = true;
            Debug.Log($"窗口延迟注册成功: {Title}");
        }
    }

    /// <summary>
    /// 获取窗口层级路径（供外部调用）
    /// </summary>
    public string GetHierarchyPath()
    {
        if (WindowManager.Instance != null)
        {
            var hierarchyInfo = WindowManager.Instance.GetWindowHierarchyInfo(this);
            return hierarchyInfo?.containerPath ?? "";
        }
        return "";
    }

    #endregion

    #region 标题管理

    /// <summary>
    /// 语言变化回调
    /// </summary>
    private void OnLanguageChanged(SupportedLanguage newLanguage)
    {
        StartCoroutine(NotifyTitleChanged());
    }

    /// <summary>
    /// 延迟通知标题已更改
    /// </summary>
    private System.Collections.IEnumerator NotifyTitleChanged()
    {
        yield return null; // 等待一帧，让LocalizationID完成更新

        OnWindowTitleChanged?.Invoke(this);
        Debug.Log($"窗口标题已更新: {Title}");
    }

    #endregion

    #region 公共方法
    /// <summary>
    /// 关闭窗口（销毁对象）
    /// </summary>
    public void CloseWindow()
    {
        OnWindowClosed?.Invoke(this);
        Destroy(gameObject);
    }

    /// <summary>
    /// 将窗口移到最前面
    /// </summary>
    public void BringToFront()
    {
        transform.SetAsLastSibling();
        OnWindowSelected?.Invoke(this);
    }

    #endregion

    #region 拖拽功能

    public void OnPointerDown(PointerEventData eventData)
    {
        BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (titleBarRect != null && RectTransformUtility.RectangleContainsScreenPoint(
            titleBarRect, eventData.position, eventData.pressEventCamera))
        {
            isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out lastMousePosition);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            Vector2 currentMousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out currentMousePosition);

            Vector2 deltaPosition = currentMousePosition - lastMousePosition;
            windowRect.anchoredPosition += deltaPosition;

            lastMousePosition = currentMousePosition;
            ClampToCanvas();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 更新窗口显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (iconImage != null)
        {
            iconImage.sprite = windowIcon;
            iconImage.gameObject.SetActive(windowIcon != null);
        }
    }

    /// <summary>
    /// 限制窗口在画布范围内
    /// </summary>
    private void ClampToCanvas()
    {
        if (canvasRect == null) return;

        Vector2 canvasSize = canvasRect.sizeDelta;
        Vector2 windowSize = windowRect.sizeDelta;
        Vector2 position = windowRect.anchoredPosition;

        float titleBarHeight = titleBarRect != null ? titleBarRect.sizeDelta.y : 30f;

        float minX = -canvasSize.x / 2 + windowSize.x / 2;
        float maxX = canvasSize.x / 2 - windowSize.x / 2;
        float minY = -canvasSize.y / 2 + titleBarHeight;
        float maxY = canvasSize.y / 2 - windowSize.y / 2;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        windowRect.anchoredPosition = position;
    }

    #endregion
}