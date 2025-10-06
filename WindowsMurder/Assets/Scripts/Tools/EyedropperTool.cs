using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 吸管取色工具 - 修复ReadPixels错误
/// </summary>
public class EyedropperTool : MonoBehaviour
{
    [Header("=== UI组件 ===")]
    [SerializeField] private Button eyedropperButton;
    [SerializeField] private Image buttonIcon;

    [Header("=== 光标设置 ===")]
    [SerializeField] private Texture2D eyedropperCursor;
    [SerializeField] private Vector2 cursorHotspot = new Vector2(0, 32);

    [Header("=== 实时预览 ===")]
    [SerializeField] private Image colorPreviewImage;
    [SerializeField] private Vector2 previewOffset = new Vector2(40, -40);
    [SerializeField] private int updateInterval = 2; // 建议设为2-3，降低频率

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = false;

    // 状态
    private bool waitingForClick = false;
    private bool isReadingPixel = false; // 防止重复读取
    private Texture2D screenTexture;
    private Color originalButtonColor;
    private Canvas parentCanvas;
    private RectTransform previewRect;
    private int frameCounter = 0;
    private Color lastSampledColor = Color.white; // 缓存上次采样的颜色

    // 静态事件
    public static event System.Action<Color> OnAnyColorPicked;

    // 实例事件
    [System.Serializable]
    public class ColorPickedEvent : UnityEngine.Events.UnityEvent<Color> { }
    public ColorPickedEvent OnColorPicked;

    #region 初始化

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        SetupButton();
    }

    void OnDestroy()
    {
        CleanUp();
    }

    private void InitializeComponents()
    {
        screenTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);
        parentCanvas = GetComponentInParent<Canvas>();

        if (buttonIcon != null)
        {
            originalButtonColor = buttonIcon.color;
        }

        if (colorPreviewImage != null)
        {
            previewRect = colorPreviewImage.GetComponent<RectTransform>();
            colorPreviewImage.gameObject.SetActive(false);
        }
        else
        {
            LogError("未设置颜色预览Image！");
        }
    }

    private void SetupButton()
    {
        if (eyedropperButton == null)
        {
            LogError("未设置吸管按钮！");
            return;
        }

        eyedropperButton.onClick.RemoveAllListeners();
        eyedropperButton.onClick.AddListener(OnButtonClick);
    }

    private void CleanUp()
    {
        if (screenTexture != null)
        {
            Destroy(screenTexture);
        }

        if (waitingForClick)
        {
            RestoreCursor();
            HidePreview();
        }
    }

    #endregion

    #region 按钮交互

    public void OnButtonClick()
    {
        if (waitingForClick)
        {
            LogDebug("已在等待取色，忽略重复点击");
            return;
        }

        ActivateEyedropper();
    }

    private void ActivateEyedropper()
    {
        waitingForClick = true;
        isReadingPixel = false;
        frameCounter = 0;

        SetEyedropperCursor();

        if (buttonIcon != null)
        {
            buttonIcon.color = Color.yellow;
        }

        ShowPreview();
        LogDebug("吸管工具已激活");
    }

    public void CancelEyedropper()
    {
        if (!waitingForClick) return;

        waitingForClick = false;
        isReadingPixel = false;
        RestoreCursor();
        RestoreButtonAppearance();
        HidePreview();

        LogDebug("吸管工具已取消");
    }

    #endregion

    #region 取色逻辑

    void Update()
    {
        if (!waitingForClick) return;

        // 每帧更新位置（不需要等待渲染）
        UpdatePreviewPosition();

        // 按间隔异步更新颜色
        frameCounter++;
        if (frameCounter >= updateInterval && !isReadingPixel)
        {
            frameCounter = 0;
            StartCoroutine(AsyncSampleColor());
        }

        // 左键确认取色
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(CaptureAndPickColor());
        }

        // 右键或ESC取消
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelEyedropper();
        }
    }

    /// <summary>
    /// 异步采样颜色（修复ReadPixels错误）
    /// </summary>
    private IEnumerator AsyncSampleColor()
    {
        isReadingPixel = true;

        // 等待渲染完成
        yield return new WaitForEndOfFrame();

        Vector2 mousePosition = Input.mousePosition;
        Rect pixelRect = new Rect(mousePosition.x, mousePosition.y, 1, 1);

        screenTexture.ReadPixels(pixelRect, 0, 0);
        screenTexture.Apply();

        lastSampledColor = screenTexture.GetPixel(0, 0);
        UpdatePreviewColor(lastSampledColor);

        isReadingPixel = false;
    }

    /// <summary>
    /// 更新预览颜色
    /// </summary>
    private void UpdatePreviewColor(Color color)
    {
        if (colorPreviewImage != null)
        {
            colorPreviewImage.color = color;
        }
    }

    /// <summary>
    /// 更新预览位置（每帧执行，不需要等待）
    /// </summary>
    private void UpdatePreviewPosition()
    {
        if (previewRect == null || parentCanvas == null) return;

        Vector2 mousePosition = Input.mousePosition;

        // 转换为Canvas坐标
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePosition + previewOffset,
            parentCanvas.worldCamera,
            out canvasPosition
        );

        previewRect.anchoredPosition = canvasPosition;
    }

    /// <summary>
    /// 确认取色
    /// </summary>
    private IEnumerator CaptureAndPickColor()
    {
        // 等待渲染完成
        yield return new WaitForEndOfFrame();

        Vector2 mousePosition = Input.mousePosition;
        Rect pixelRect = new Rect(mousePosition.x, mousePosition.y, 1, 1);

        screenTexture.ReadPixels(pixelRect, 0, 0);
        screenTexture.Apply();

        Color pickedColor = screenTexture.GetPixel(0, 0);
        OnColorPickedSuccess(pickedColor);
    }

    /// <summary>
    /// 取色成功
    /// </summary>
    private void OnColorPickedSuccess(Color color)
    {
        LogDebug($"取色: #{ColorUtility.ToHtmlStringRGB(color)}");

        waitingForClick = false;
        isReadingPixel = false;
        RestoreCursor();
        RestoreButtonAppearance();
        HidePreview();

        // 触发事件
        OnColorPicked?.Invoke(color);
        OnAnyColorPicked?.Invoke(color);
    }

    #endregion

    #region 预览管理

    private void ShowPreview()
    {
        if (colorPreviewImage != null)
        {
            colorPreviewImage.gameObject.SetActive(true);
            colorPreviewImage.transform.SetAsLastSibling();
        }
    }

    private void HidePreview()
    {
        if (colorPreviewImage != null)
        {
            colorPreviewImage.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 光标管理

    private void SetEyedropperCursor()
    {
        if (eyedropperCursor != null)
        {
            Cursor.SetCursor(eyedropperCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.visible = false;
            LogDebug("未设置自定义光标，已隐藏系统光标");
        }
    }

    private void RestoreCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
    }

    private void RestoreButtonAppearance()
    {
        if (buttonIcon != null)
        {
            buttonIcon.color = originalButtonColor;
        }
    }

    #endregion

    #region 公共接口

    public void ActivateTool()
    {
        OnButtonClick();
    }

    public void CancelTool()
    {
        CancelEyedropper();
    }

    public bool IsWaitingForPick()
    {
        return waitingForClick;
    }

    #endregion

    #region 调试工具

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[EyedropperTool] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[EyedropperTool] {message}");
    }

    #endregion
}