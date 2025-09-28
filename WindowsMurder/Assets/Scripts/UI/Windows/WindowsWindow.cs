using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Windows�������������
/// ֧����ק���رա���ʾͼ��ͱ���
/// </summary>
public class WindowsWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("��������")]
    [SerializeField] private string windowTitle = "NewWindow";
    [SerializeField] private Sprite windowIcon;
    [SerializeField] private bool canClose = true;

    [Header("UI�������")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private RectTransform titleBarRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button closeButton;

    // ��ק���
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    // �¼�
    public static event System.Action<WindowsWindow> OnWindowClosed;
    public static event System.Action<WindowsWindow> OnWindowSelected;

    // ����
    public string Title => windowTitle;

    void Awake()
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
            closeButton.onClick.AddListener(CloseWindow);
            closeButton.gameObject.SetActive(canClose);
        }
    }

    void Start()
    {
        // ԭ�еĸ�����ʾ����
        UpdateDisplay();

        // ȷ�������ڻ�����Χ��
        ClampToCanvas();

        // �Զ�ע�ᵽWindowManager
        RegisterToWindowManager();
    }

    /// <summary>
    /// �Զ�ע�ᵽ���ڹ�����
    /// </summary>
    private void RegisterToWindowManager()
    {
        if (WindowManager.Instance != null)
        {
            // ֱ��ע�ᣬWindowManager���Զ���ȡ�㼶��Ϣ
            WindowManager.Instance.RegisterWindow(this);

            Debug.Log($"���� '{windowTitle}' ���Զ�ע�ᵽWindowManager");
            LogHierarchyInfo();
        }
        else
        {
            // ���WindowManager��δ��ʼ�����ӳ�ע��
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

        // ע�ᵽWindowManager
        WindowManager.Instance.RegisterWindow(this);

        Debug.Log($"���� '{windowTitle}' �ӳ�ע�ᵽWindowManager�ɹ�");
        LogHierarchyInfo();
    }

    /// <summary>
    /// ��ӡ�㼶��Ϣ�������ã�
    /// </summary>
    private void LogHierarchyInfo()
    {
        if (WindowManager.Instance != null)
        {
            var hierarchyInfo = WindowManager.Instance.GetWindowHierarchyInfo(this);
            if (hierarchyInfo != null)
            {
                Debug.Log($"���ڲ㼶��Ϣ - ·��: {hierarchyInfo.containerPath}, ���: {hierarchyInfo.hierarchyLevel}, ��ǩ: {hierarchyInfo.containerTag}");
            }
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

    /// <summary>
    /// �ڴ�������ʱȷ��ע��
    /// </summary>
    void OnDestroy()
    {
        WindowManager.Instance.UnregisterWindow(this);
    }

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
        // �������ʱ�����Ƶ���ǰ��
        BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // ����Ƿ��ڱ�����������ק
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

            // �����ڻ�����Χ��
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

        // ����߽磨ȷ���������ɼ���
        float titleBarHeight = titleBarRect != null ? titleBarRect.sizeDelta.y : 30f;

        float minX = -canvasSize.x / 2 + windowSize.x / 2;
        float maxX = canvasSize.x / 2 - windowSize.x / 2;
        float minY = -canvasSize.y / 2 + titleBarHeight;
        float maxY = canvasSize.y / 2 - windowSize.y / 2;

        // ����λ��
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        windowRect.anchoredPosition = position;
    }

    #endregion
}