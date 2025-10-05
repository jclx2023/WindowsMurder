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
    [SerializeField] private string windowTitle = "NewWindow"; // �����ڱ༭����ʾ
    [SerializeField] private Sprite windowIcon;
    [SerializeField] private bool canClose = true;

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

    // �¼�
    public static event System.Action<WindowsWindow> OnWindowClosed;
    public static event System.Action<WindowsWindow> OnWindowSelected;
    public static event System.Action<WindowsWindow> OnWindowTitleChanged;

    // ���� - ��̬��ȡtitleText������
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
        // ���ڽ���ʱ��WindowManagerע��
        if (isRegistered && WindowManager.Instance != null)
        {
            WindowManager.Instance.UnregisterWindow(this);
            isRegistered = false;
            Debug.Log($"������ע�������ã�: {Title}");
        }
    }

    void OnDestroy()
    {
        // �����¼�����
        CleanupEventListeners();
    }

    #region ��ʼ��

    /// <summary>
    /// ��ʼ���������
    /// </summary>
    private void InitializeComponents()
    {
        // ��ȡCanvas���
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }

        // ���û������windowRect��ʹ������
        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        // ���ùرհ�ť�¼�
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseWindow);
            closeButton.gameObject.SetActive(canClose);
        }
    }

    /// <summary>
    /// ִ��һ���Գ�ʼ��
    /// </summary>
    private void PerformOneTimeInitialization()
    {
        // ������ʾ
        UpdateDisplay();

        // ȷ�������ڻ�����Χ��
        ClampToCanvas();

        // �������Ա仯�¼�
        LanguageManager.OnLanguageChanged += OnLanguageChanged;

        // ���Ϊ�ѳ�ʼ��
        isInitialized = true;

        Debug.Log($"���ڳ�ʼ�����: {Title}");
    }

    /// <summary>
    /// �����¼�����
    /// </summary>
    private void CleanupEventListeners()
    {
        // ȡ�������¼�����
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;

        // ȷ����WindowManagerע��������ֱ�����ٵ������
        if (isRegistered && WindowManager.Instance != null)
        {
            WindowManager.Instance.UnregisterWindow(this);
            isRegistered = false;
        }
    }

    #endregion

    #region ���ڹ�����ע��

    /// <summary>
    /// �ӳٳ�ʼע�ᣨ�ȴ�LocalizationID������ɣ�
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
    /// ע�ᵽ���ڹ�����
    /// </summary>
    private void RegisterToWindowManager()
    {
        // ��ֹ�ظ�ע��
        if (isRegistered)
        {
            Debug.LogWarning($"���� {Title} �Ѿ�ע����ˣ������ظ�ע��");
            return;
        }

        if (WindowManager.Instance != null)
        {
            WindowManager.Instance.RegisterWindow(this);
            isRegistered = true;
            Debug.Log($"������ע��: {Title}");
        }
        else
        {
            // WindowManager��δ��ʼ�����ӳ�ע��
            StartCoroutine(DelayedRegister());
        }
    }

    /// <summary>
    /// �ӳ�ע�ᣨ�����ʼ��˳�����⣩
    /// </summary>
    private System.Collections.IEnumerator DelayedRegister()
    {
        // �ȴ�WindowManager��ʼ��
        while (WindowManager.Instance == null)
        {
            yield return null;
        }

        // �ٴμ�鴰���Ƿ���Ȼ������δע��
        if (gameObject.activeInHierarchy && !isRegistered)
        {
            WindowManager.Instance.RegisterWindow(this);
            isRegistered = true;
            Debug.Log($"�����ӳ�ע��ɹ�: {Title}");
        }
    }

    /// <summary>
    /// ��ȡ���ڲ㼶·�������ⲿ���ã�
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

    #region �������

    /// <summary>
    /// ���Ա仯�ص�
    /// </summary>
    private void OnLanguageChanged(SupportedLanguage newLanguage)
    {
        StartCoroutine(NotifyTitleChanged());
    }

    /// <summary>
    /// �ӳ�֪ͨ�����Ѹ���
    /// </summary>
    private System.Collections.IEnumerator NotifyTitleChanged()
    {
        yield return null; // �ȴ�һ֡����LocalizationID��ɸ���

        OnWindowTitleChanged?.Invoke(this);
        Debug.Log($"���ڱ����Ѹ���: {Title}");
    }

    #endregion

    #region ��������
    /// <summary>
    /// �رմ��ڣ����ٶ���
    /// </summary>
    public void CloseWindow()
    {
        OnWindowClosed?.Invoke(this);
        Destroy(gameObject);
    }

    /// <summary>
    /// �������Ƶ���ǰ��
    /// </summary>
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

    /// <summary>
    /// ���´�����ʾ
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
    /// ���ƴ����ڻ�����Χ��
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