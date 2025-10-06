using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ����ȡɫ���� - �޸�ReadPixels����
/// </summary>
public class EyedropperTool : MonoBehaviour
{
    [Header("=== UI��� ===")]
    [SerializeField] private Button eyedropperButton;
    [SerializeField] private Image buttonIcon;

    [Header("=== ������� ===")]
    [SerializeField] private Texture2D eyedropperCursor;
    [SerializeField] private Vector2 cursorHotspot = new Vector2(0, 32);

    [Header("=== ʵʱԤ�� ===")]
    [SerializeField] private Image colorPreviewImage;
    [SerializeField] private Vector2 previewOffset = new Vector2(40, -40);
    [SerializeField] private int updateInterval = 2; // ������Ϊ2-3������Ƶ��

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = false;

    // ״̬
    private bool waitingForClick = false;
    private bool isReadingPixel = false; // ��ֹ�ظ���ȡ
    private Texture2D screenTexture;
    private Color originalButtonColor;
    private Canvas parentCanvas;
    private RectTransform previewRect;
    private int frameCounter = 0;
    private Color lastSampledColor = Color.white; // �����ϴβ�������ɫ

    // ��̬�¼�
    public static event System.Action<Color> OnAnyColorPicked;

    // ʵ���¼�
    [System.Serializable]
    public class ColorPickedEvent : UnityEngine.Events.UnityEvent<Color> { }
    public ColorPickedEvent OnColorPicked;

    #region ��ʼ��

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
            LogError("δ������ɫԤ��Image��");
        }
    }

    private void SetupButton()
    {
        if (eyedropperButton == null)
        {
            LogError("δ�������ܰ�ť��");
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

    #region ��ť����

    public void OnButtonClick()
    {
        if (waitingForClick)
        {
            LogDebug("���ڵȴ�ȡɫ�������ظ����");
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
        LogDebug("���ܹ����Ѽ���");
    }

    public void CancelEyedropper()
    {
        if (!waitingForClick) return;

        waitingForClick = false;
        isReadingPixel = false;
        RestoreCursor();
        RestoreButtonAppearance();
        HidePreview();

        LogDebug("���ܹ�����ȡ��");
    }

    #endregion

    #region ȡɫ�߼�

    void Update()
    {
        if (!waitingForClick) return;

        // ÿ֡����λ�ã�����Ҫ�ȴ���Ⱦ��
        UpdatePreviewPosition();

        // ������첽������ɫ
        frameCounter++;
        if (frameCounter >= updateInterval && !isReadingPixel)
        {
            frameCounter = 0;
            StartCoroutine(AsyncSampleColor());
        }

        // ���ȷ��ȡɫ
        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(CaptureAndPickColor());
        }

        // �Ҽ���ESCȡ��
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelEyedropper();
        }
    }

    /// <summary>
    /// �첽������ɫ���޸�ReadPixels����
    /// </summary>
    private IEnumerator AsyncSampleColor()
    {
        isReadingPixel = true;

        // �ȴ���Ⱦ���
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
    /// ����Ԥ����ɫ
    /// </summary>
    private void UpdatePreviewColor(Color color)
    {
        if (colorPreviewImage != null)
        {
            colorPreviewImage.color = color;
        }
    }

    /// <summary>
    /// ����Ԥ��λ�ã�ÿִ֡�У�����Ҫ�ȴ���
    /// </summary>
    private void UpdatePreviewPosition()
    {
        if (previewRect == null || parentCanvas == null) return;

        Vector2 mousePosition = Input.mousePosition;

        // ת��ΪCanvas����
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
    /// ȷ��ȡɫ
    /// </summary>
    private IEnumerator CaptureAndPickColor()
    {
        // �ȴ���Ⱦ���
        yield return new WaitForEndOfFrame();

        Vector2 mousePosition = Input.mousePosition;
        Rect pixelRect = new Rect(mousePosition.x, mousePosition.y, 1, 1);

        screenTexture.ReadPixels(pixelRect, 0, 0);
        screenTexture.Apply();

        Color pickedColor = screenTexture.GetPixel(0, 0);
        OnColorPickedSuccess(pickedColor);
    }

    /// <summary>
    /// ȡɫ�ɹ�
    /// </summary>
    private void OnColorPickedSuccess(Color color)
    {
        LogDebug($"ȡɫ: #{ColorUtility.ToHtmlStringRGB(color)}");

        waitingForClick = false;
        isReadingPixel = false;
        RestoreCursor();
        RestoreButtonAppearance();
        HidePreview();

        // �����¼�
        OnColorPicked?.Invoke(color);
        OnAnyColorPicked?.Invoke(color);
    }

    #endregion

    #region Ԥ������

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

    #region ������

    private void SetEyedropperCursor()
    {
        if (eyedropperCursor != null)
        {
            Cursor.SetCursor(eyedropperCursor, cursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.visible = false;
            LogDebug("δ�����Զ����꣬������ϵͳ���");
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

    #region �����ӿ�

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

    #region ���Թ���

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