using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Windows风格基础窗口组件
/// 支持拖拽、关闭、显示图标和标题
/// </summary>
public class WindowsWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("窗口设置")]
    [SerializeField] private string windowTitle = "NewWindow";
    [SerializeField] private Sprite windowIcon;
    [SerializeField] private bool canClose = true;

    [Header("UI组件引用")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private RectTransform titleBarRect;
    [SerializeField] private Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button closeButton;

    // 拖拽相关
    private Vector2 lastMousePosition;
    private bool isDragging = false;
    private Canvas parentCanvas;
    private RectTransform canvasRect;

    // 事件
    public static event System.Action<WindowsWindow> OnWindowClosed;
    public static event System.Action<WindowsWindow> OnWindowSelected;

    // 属性
    public string Title => windowTitle;

    void Awake()
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
            closeButton.onClick.AddListener(CloseWindow);
            closeButton.gameObject.SetActive(canClose);
        }
    }

    void Start()
    {
        // 更新显示
        UpdateDisplay();

        // 确保窗口在画布范围内
        ClampToCanvas();
    }

    #region 公共方法

    /// <summary>
    /// 设置窗口标题
    /// </summary>
    public void SetTitle(string title)
    {
        windowTitle = title;
        if (titleText != null)
            titleText.text = title;
    }

    /// <summary>
    /// 设置窗口图标
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        windowIcon = icon;
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    public void CloseWindow()
    {
        OnWindowClosed?.Invoke(this);
        gameObject.SetActive(false);
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
        // 点击窗口时将其移到最前面
        BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查是否在标题栏区域拖拽
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

            // 限制在画布范围内
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
        if (titleText != null)
            titleText.text = windowTitle;

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

        // 计算边界（确保标题栏可见）
        float titleBarHeight = titleBarRect != null ? titleBarRect.sizeDelta.y : 30f;

        float minX = -canvasSize.x / 2 + windowSize.x / 2;
        float maxX = canvasSize.x / 2 - windowSize.x / 2;
        float minY = -canvasSize.y / 2 + titleBarHeight;
        float maxY = canvasSize.y / 2 - windowSize.y / 2;

        // 限制位置
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        windowRect.anchoredPosition = position;
    }

    #endregion
}