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
    [SerializeField] private string windowTitle = "NewWindow";
    [SerializeField] private Sprite windowIcon;
    [SerializeField] private bool canClose = true;
    [SerializeField] private AudioClip audioClip;

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

    // 外部位置设置
    private Vector2? externalInitialPosition = null;
    private bool hasAppliedExternalPosition = false;
    private bool skipAutoArrange = false;

    // 事件
    public static event System.Action<WindowsWindow> OnWindowClosed;
    public static event System.Action<WindowsWindow> OnWindowSelected;
    public static event System.Action<WindowsWindow> OnWindowTitleChanged;

    // 属性
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
        if (audioClip != null)
        {
            GlobalSystemManager.Instance.PlaySFX(audioClip);
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
        if (isRegistered && WindowManager.Instance != null)
        {
            WindowManager.Instance.UnregisterWindow(this);
            isRegistered = false;
        }
    }

    void OnDestroy()
    {
        CleanupEventListeners();
    }

    #region 初始化

    private void InitializeComponents()
    {

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }

        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseWindow);
            closeButton.gameObject.SetActive(canClose);
        }
    }

    private void PerformOneTimeInitialization()
    {
        UpdateDisplay();

        if (externalInitialPosition.HasValue && !hasAppliedExternalPosition)
        {
            windowRect.anchoredPosition = externalInitialPosition.Value;
            hasAppliedExternalPosition = true;
        }

        ClampToCanvas();
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        isInitialized = true;
    }

    private void CleanupEventListeners()
    {
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;

        if (isRegistered && WindowManager.Instance != null)
        {
            WindowManager.Instance.UnregisterWindow(this);
            isRegistered = false;
        }
    }

    #endregion

    #region 外部位置设置

    /// <summary>
    /// 设置外部初始位置（必须在Awake之后、Start之前调用）
    /// </summary>
    public void SetExternalInitialPosition(Vector2 position)
    {
        externalInitialPosition = position;
        skipAutoArrange = true;

        if (isInitialized && !hasAppliedExternalPosition)
        {
            windowRect.anchoredPosition = position;
            hasAppliedExternalPosition = true;
            ClampToCanvas();
        }
    }

    /// <summary>
    /// 检查是否应该跳过自动排列（供 WindowManager 调用）
    /// </summary>
    public bool ShouldSkipAutoArrange()
    {
        return skipAutoArrange;
    }

    #endregion

    #region 窗口管理器注册

    private System.Collections.IEnumerator DelayedInitialRegister()
    {
        yield return null;
        yield return null;
        if (gameObject.activeInHierarchy && !isRegistered)
        {
            RegisterToWindowManager();
        }
    }

    private void RegisterToWindowManager()
    {
        if (isRegistered)
        {
            return;
        }

        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.RegisterWindow(this);
            isRegistered = true;
        }
        else
        {
            StartCoroutine(DelayedRegister());
        }
    }

    private System.Collections.IEnumerator DelayedRegister()
    {
        while (WindowManager.Instance == null)
        {
            yield return null;
        }

        if (gameObject.activeInHierarchy && !isRegistered)
        {
            WindowManager.Instance.RegisterWindow(this);
            isRegistered = true;
        }
    }

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

    private void OnLanguageChanged(SupportedLanguage newLanguage)
    {
        StartCoroutine(NotifyTitleChanged());
    }

    private System.Collections.IEnumerator NotifyTitleChanged()
    {
        yield return null;
        OnWindowTitleChanged?.Invoke(this);
    }

    #endregion

    #region 公共方法

    public void CloseWindow()
    {
        OnWindowClosed?.Invoke(this);
        Destroy(gameObject);
    }

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

    private void UpdateDisplay()
    {
        if (iconImage != null)
        {
            iconImage.sprite = windowIcon;
            iconImage.gameObject.SetActive(windowIcon != null);
        }
    }

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