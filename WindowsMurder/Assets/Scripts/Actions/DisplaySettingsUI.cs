using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display设置窗口UI控制器
/// 专门控制全屏切换和对话速度设置，包含预览功能
/// </summary>
public class DisplaySettingsUI : MonoBehaviour
{
    [Header("UI组件")]
    public Toggle fullscreenToggle;                    // 全屏切换
    public Slider dialogueSpeedSlider;                 // 对话速度滑条
    public TextMeshProUGUI previewText;                // 预览文本显示

    [Header("对话速度设置")]
    public float minSpeed = 0.01f;                     // 最小速度（最快）
    public float maxSpeed = 0.15f;                     // 最大速度（最慢）

    [Header("预览设置")]
    public string previewSampleText = "这是一段用于预览对话显示速度的示例文本。";

    // 私有变量
    private Coroutine previewCoroutine;
    private bool isInitializing = false;

    void Start()
    {
        InitializeComponents();
        LoadCurrentSettings();
        SetupEventListeners();
        StartPreview();
    }

    /// <summary>
    /// 初始化组件
    /// </summary>
    void InitializeComponents()
    {
        if (dialogueSpeedSlider != null)
        {
            dialogueSpeedSlider.minValue = minSpeed;
            dialogueSpeedSlider.maxValue = maxSpeed;
        }
    }

    /// <summary>
    /// 加载当前设置
    /// </summary>
    void LoadCurrentSettings()
    {
        if (GlobalSystemManager.Instance == null)
        {
            Debug.LogWarning("DisplaySettingsUI: GlobalSystemManager未初始化");
            return;
        }

        isInitializing = true;

        // 从GlobalSystemManager读取设置
        if (dialogueSpeedSlider != null)
            dialogueSpeedSlider.value = GlobalSystemManager.Instance.dialogueSpeed;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = GlobalSystemManager.Instance.isFullscreen;
        isInitializing = false;
    }

    /// <summary>
    /// 设置事件监听器
    /// </summary>
    void SetupEventListeners()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);

        if (dialogueSpeedSlider != null)
            dialogueSpeedSlider.onValueChanged.AddListener(OnDialogueSpeedChanged);

        // 监听预览面板点击重启预览
        if (previewText != null)
        {
            Button previewButton = previewText.gameObject.GetComponent<Button>();
            if (previewButton == null)
            {
                previewButton = previewText.gameObject.AddComponent<Button>();
                previewButton.transition = Selectable.Transition.None;
            }
            previewButton.onClick.AddListener(StartPreview);
        }
    }

    /// <summary>
    /// 全屏切换事件
    /// </summary>
    void OnFullscreenToggleChanged(bool isFullscreen)
    {
        if (isInitializing) return;

        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetDisplay(isFullscreen, GlobalSystemManager.Instance.resolution);
        }
    }

    /// <summary>
    /// 对话速度变化事件
    /// </summary>
    void OnDialogueSpeedChanged(float speed)
    {
        if (isInitializing) return;

        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.dialogueSpeed = speed;
            GlobalSystemManager.Instance.SaveSettings();
        }
        StartPreview(); // 重新开始预览
    }

    #region 预览功能

    /// <summary>
    /// 开始预览
    /// </summary>
    void StartPreview()
    {
        if (previewText == null || string.IsNullOrEmpty(previewSampleText)) return;

        StopPreview();
        previewCoroutine = StartCoroutine(PreviewTypingEffect());
    }

    /// <summary>
    /// 停止预览
    /// </summary>
    void StopPreview()
    {
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }
    }

    /// <summary>
    /// 预览打字机效果协程
    /// </summary>
    IEnumerator PreviewTypingEffect()
    {
        if (previewText == null) yield break;

        previewText.text = "";
        float currentSpeed = dialogueSpeedSlider != null ? dialogueSpeedSlider.value : 0.05f;

        // 逐字显示
        foreach (char c in previewSampleText.ToCharArray())
        {
            previewText.text += c;
            yield return new WaitForSeconds(currentSpeed);
        }

        // 等待一段时间后重新开始
        yield return new WaitForSeconds(1.5f);
        if (gameObject.activeInHierarchy)
            StartPreview();
    }

    #endregion

    #region 生命周期

    void OnEnable()
    {
        // 窗口显示时刷新设置并开始预览
        if (!isInitializing)
        {
            LoadCurrentSettings();
            StartPreview();
        }
    }

    void OnDisable()
    {
        StopPreview();
    }

    void OnDestroy()
    {
        // 清理事件监听器
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);

        if (dialogueSpeedSlider != null)
            dialogueSpeedSlider.onValueChanged.RemoveListener(OnDialogueSpeedChanged);

        StopPreview();
    }

    #endregion
}