using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Display���ô���UI������
/// ר�ſ���ȫ���л��ͶԻ��ٶ����ã�����Ԥ������
/// </summary>
public class DisplaySettingsUI : MonoBehaviour
{
    [Header("UI���")]
    public Toggle fullscreenToggle;                    // ȫ���л�
    public Slider dialogueSpeedSlider;                 // �Ի��ٶȻ���
    public TextMeshProUGUI previewText;                // Ԥ���ı���ʾ

    [Header("�Ի��ٶ�����")]
    public float minSpeed = 0.01f;                     // ��С�ٶȣ���죩
    public float maxSpeed = 0.15f;                     // ����ٶȣ�������

    [Header("Ԥ������")]
    public string previewSampleText = "����һ������Ԥ���Ի���ʾ�ٶȵ�ʾ���ı���";

    // ˽�б���
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
    /// ��ʼ�����
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
    /// ���ص�ǰ����
    /// </summary>
    void LoadCurrentSettings()
    {
        if (GlobalSystemManager.Instance == null)
        {
            Debug.LogWarning("DisplaySettingsUI: GlobalSystemManagerδ��ʼ��");
            return;
        }

        isInitializing = true;

        // ��GlobalSystemManager��ȡ����
        if (dialogueSpeedSlider != null)
            dialogueSpeedSlider.value = GlobalSystemManager.Instance.dialogueSpeed;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = GlobalSystemManager.Instance.isFullscreen;
        isInitializing = false;
    }

    /// <summary>
    /// �����¼�������
    /// </summary>
    void SetupEventListeners()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);

        if (dialogueSpeedSlider != null)
            dialogueSpeedSlider.onValueChanged.AddListener(OnDialogueSpeedChanged);

        // ����Ԥ�����������Ԥ��
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
    /// ȫ���л��¼�
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
    /// �Ի��ٶȱ仯�¼�
    /// </summary>
    void OnDialogueSpeedChanged(float speed)
    {
        if (isInitializing) return;

        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.dialogueSpeed = speed;
            GlobalSystemManager.Instance.SaveSettings();
        }
        StartPreview(); // ���¿�ʼԤ��
    }

    #region Ԥ������

    /// <summary>
    /// ��ʼԤ��
    /// </summary>
    void StartPreview()
    {
        if (previewText == null || string.IsNullOrEmpty(previewSampleText)) return;

        StopPreview();
        previewCoroutine = StartCoroutine(PreviewTypingEffect());
    }

    /// <summary>
    /// ֹͣԤ��
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
    /// Ԥ�����ֻ�Ч��Э��
    /// </summary>
    IEnumerator PreviewTypingEffect()
    {
        if (previewText == null) yield break;

        previewText.text = "";
        float currentSpeed = dialogueSpeedSlider != null ? dialogueSpeedSlider.value : 0.05f;

        // ������ʾ
        foreach (char c in previewSampleText.ToCharArray())
        {
            previewText.text += c;
            yield return new WaitForSeconds(currentSpeed);
        }

        // �ȴ�һ��ʱ������¿�ʼ
        yield return new WaitForSeconds(1.5f);
        if (gameObject.activeInHierarchy)
            StartPreview();
    }

    #endregion

    #region ��������

    void OnEnable()
    {
        // ������ʾʱˢ�����ò���ʼԤ��
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
        // �����¼�������
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggleChanged);

        if (dialogueSpeedSlider != null)
            dialogueSpeedSlider.onValueChanged.RemoveListener(OnDialogueSpeedChanged);

        StopPreview();
    }

    #endregion
}