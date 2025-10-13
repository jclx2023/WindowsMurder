using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Windows������������� - ֧�ֶ�̬����ͼ���״̬����
/// </summary>
public class WindowsWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("��������")]
    [SerializeField] private string windowTitle = "NewWindow";
    [SerializeField] private Sprite windowIcon;
    [SerializeField] private bool canClose = true;
    [SerializeField] private AudioClip audioClip;

    [Header("UI�������")]
    [SerializeField] public RectTransform windowRect;
    [SerializeField] private RectTransform titleBarRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button closeButton;

    // ��ק���
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    // ��ʼ�����
    private bool isInitialized = false;
    private bool isRegistered = false;

    // �ⲿλ������
    private Vector2? externalInitialPosition = null;
    private bool hasAppliedExternalPosition = false;
    private bool skipAutoArrange = false;

    // �¼�
    public static event System.Action<WindowsWindow> OnWindowClosed;
    public static event System.Action<WindowsWindow> OnWindowSelected;
    public static event System.Action<WindowsWindow> OnWindowTitleChanged;

    // ����
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

    #region ��ʼ��

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

    #region �ⲿλ������

    /// <summary>
    /// �����ⲿ��ʼλ�ã�������Awake֮��Start֮ǰ���ã�
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
    /// ����Ƿ�Ӧ�������Զ����У��� WindowManager ���ã�
    /// </summary>
    public bool ShouldSkipAutoArrange()
    {
        return skipAutoArrange;
    }

    #endregion

    #region ���ڹ�����ע��

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

    #region �������

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

    #region ��������

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

    #region ��ק����

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

    #region ˽�з���

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