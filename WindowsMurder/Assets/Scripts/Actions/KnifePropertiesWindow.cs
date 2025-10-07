using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Knife ���Դ��ڿ����� - Tab �л� + �״β鿴�Ի�����
/// </summary>
public class KnifePropertiesWindow : MonoBehaviour
{
    [Header("=== Tab ������� ===")]
    [SerializeField] private GameObject generalPanel;
    [SerializeField] private GameObject detailsPanel;

    [Header("=== Details �״β鿴�Ի����� ===")]
    [SerializeField] private string detailsDialogueBlockId = "knife_details_dialogue";
    [SerializeField] private float dialogueDelay = 1f;
    [Tooltip("�Ƿ��ڴ򿪶Ի�ʱ�رմ���")]
    [SerializeField] private bool closeWindowOnDialogue = false;

    [Header("=== ��ť���� ===")]
    [SerializeField] private Button generalTabButton;
    [SerializeField] private Button detailsTabButton;
    [SerializeField] private Button okButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button applyButton;

    [Header("=== Tab ��ť�Ӿ�״̬����ѡ��===")]
    [SerializeField] private Color activeTabColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveTabColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = true;

    // ����ʱ״̬
    private bool hasViewedDetails = false;

    // �������
    private GameFlowController flowController;
    private WindowsWindow windowComponent;

    // Tab ��ť�� Image ����������Ӿ�������
    private Image generalTabImage;
    private Image detailsTabImage;

    #region ��ʼ��

    void Awake()
    {
        // ��ȡ�������
        flowController = FindObjectOfType<GameFlowController>();
        windowComponent = GetComponent<WindowsWindow>();

        // ��ȡ Tab ��ť�� Image ���
        if (generalTabButton != null)
            generalTabImage = generalTabButton.GetComponent<Image>();
        if (detailsTabButton != null)
            detailsTabImage = detailsTabButton.GetComponent<Image>();

        // �󶨰�ť�¼�
        BindButtonEvents();
    }

    void Start()
    {
        // Ĭ����ʾ General ҳ��
        ShowGeneralTab();
    }

    /// <summary>
    /// �����а�ť����¼�
    /// </summary>
    private void BindButtonEvents()
    {
        if (generalTabButton != null)
            generalTabButton.onClick.AddListener(OnGeneralTabClick);

        if (detailsTabButton != null)
            detailsTabButton.onClick.AddListener(OnDetailsTabClick);

        if (okButton != null)
            okButton.onClick.AddListener(OnOKClick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClick);
    }

    #endregion

    #region Tab �л��߼�

    /// <summary>
    /// ��� General Tab ��ť
    /// </summary>
    private void OnGeneralTabClick()
    {
        LogDebug("�л��� General ҳ��");
        ShowGeneralTab();
    }

    /// <summary>
    /// ��� Details Tab ��ť
    /// </summary>
    private void OnDetailsTabClick()
    {
        LogDebug("�л��� Details ҳ��");
        ShowDetailsTab();

        // ��һ�β鿴 Details ʱ�����Ի�
        if (!hasViewedDetails)
        {
            hasViewedDetails = true;
            StartCoroutine(PlayDetailsDialogueDelayed());
        }
    }

    /// <summary>
    /// ��ʾ General ҳ�棨ͨ���㼶���ƣ�
    /// </summary>
    private void ShowGeneralTab()
    {
        if (generalPanel != null)
        {
            // �� General ����Ƶ����ϲ㣨���� Sibling Index��
            generalPanel.transform.SetAsLastSibling();
            UpdateTabButtonStates(true);
        }
    }

    /// <summary>
    /// ��ʾ Details ҳ�棨ͨ���㼶���ƣ�
    /// </summary>
    private void ShowDetailsTab()
    {
        if (detailsPanel != null)
        {
            // �� Details ����Ƶ����ϲ㣨���� Sibling Index��
            detailsPanel.transform.SetAsLastSibling();
            UpdateTabButtonStates(false);
        }
    }

    /// <summary>
    /// ���� Tab ��ť���Ӿ�״̬
    /// </summary>
    private void UpdateTabButtonStates(bool isGeneralActive)
    {
        if (generalTabImage != null)
        {
            generalTabImage.color = isGeneralActive ? activeTabColor : inactiveTabColor;
        }

        if (detailsTabImage != null)
        {
            detailsTabImage.color = isGeneralActive ? inactiveTabColor : activeTabColor;
        }

        LogDebug($"Tab ״̬���� - General ����: {isGeneralActive}");
    }

    #endregion

    #region �Ի������߼�

    /// <summary>
    /// �ӳٲ��� Details �Ի���
    /// </summary>
    private IEnumerator PlayDetailsDialogueDelayed()
    {
        LogDebug($"���� {dialogueDelay} ��󲥷ŶԻ���: {detailsDialogueBlockId}");

        yield return new WaitForSeconds(dialogueDelay);

        // ֱ�ӵ��� GameFlowController �����Ի���
        if (flowController != null)
        {
            flowController.StartDialogueBlock(detailsDialogueBlockId);
            LogDebug($"�Ѵ����Ի���: {detailsDialogueBlockId}");
        }
    }

    #endregion

    #region ��ť�¼�����

    /// <summary>
    /// OK ��ť��� - �رմ���
    /// </summary>
    private void OnOKClick()
    {
        LogDebug("��� OK ��ť");
        CloseWindow();
    }

    /// <summary>
    /// Cancel ��ť��� - �رմ���
    /// </summary>
    private void OnCancelClick()
    {
        LogDebug("��� Cancel ��ť");
        CloseWindow();
    }

    /// <summary>
    /// Apply ��ť��� - ���ִ��ڴ򿪣�WinXP ���
    /// </summary>
    private void OnApplyClick()
    {
        LogDebug("��� Apply ��ť���޲�����");
    }

    #endregion

    #region ���Ĺ���

    /// <summary>
    /// �رմ���
    /// </summary>
    private void CloseWindow()
    {
        if (windowComponent != null)
        {
            windowComponent.CloseWindow();
        }
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[KnifeProperties] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[KnifeProperties] {message}");
    }
    #endregion
}