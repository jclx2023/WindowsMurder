using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Works�ļ������Դ��ڿ����� - ����Unlock�����߼�
/// </summary>
public class WorksFolderPropertiesWindow : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private string clueId = "works_folder_unlocked";

    [Header("��ť����")]
    [SerializeField] private Button unlockButton;
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // ����ʱ״̬
    private bool unlockFlag = false;
    private bool isAlreadyUnlocked = false;

    // �������
    private GameFlowController flowController;
    private WindowsWindow windowComponent;

    #region ��ʼ��

    void Awake()
    {
        // ��ȡ�������
        flowController = FindObjectOfType<GameFlowController>();
        windowComponent = GetComponent<WindowsWindow>();
        // �󶨰�ť�¼�
        BindButtonEvents();
    }

    void Start()
    {
        // ��������Ƿ��Ѿ�����
        CheckInitialUnlockState();

        // ���°�ť״̬
        UpdateButtonStates();
    }

    /// <summary>
    /// �󶨰�ť����¼�
    /// </summary>
    private void BindButtonEvents()
    {
        unlockButton.onClick.AddListener(OnUnlockClick);
        okButton.onClick.AddListener(OnOKClick);
        applyButton.onClick.AddListener(OnApplyClick);
        cancelButton.onClick.AddListener(OnCancelClick);
    }

    /// <summary>
    /// ���������ʼ����״̬
    /// </summary>
    private void CheckInitialUnlockState()
    {
        if (flowController != null)
        {
            isAlreadyUnlocked = flowController.HasClue(clueId);

            if (isAlreadyUnlocked)
            {
                unlockFlag = true;
                LogDebug($"���� {clueId} �Ѿ�����");
            }
        }
    }

    #endregion

    #region ��ť�¼�����

    /// <summary>
    /// Unlock��ť���
    /// </summary>
    private void OnUnlockClick()
    {
        LogDebug("��� Unlock ��ť");

        unlockFlag = true;
        UpdateButtonStates();
    }

    /// <summary>
    /// Apply��ť���
    /// </summary>
    private void OnApplyClick()
    {
        LogDebug("��� Apply ��ť");

        if (unlockFlag && !isAlreadyUnlocked)
        {
            UnlockClue();
        }

        // Apply���رմ��ڣ�ֻ����״̬
        UpdateButtonStates();
    }

    /// <summary>
    /// OK��ť���
    /// </summary>
    private void OnOKClick()
    {
        LogDebug("��� OK ��ť");

        // ���unlockFlagΪtrue�һ�δ�������Ƚ�������
        if (unlockFlag && !isAlreadyUnlocked)
        {
            UnlockClue();
        }

        // �رմ���
        CloseWindow();
    }

    /// <summary>
    /// Cancel��ť���
    /// </summary>
    private void OnCancelClick()
    {
        LogDebug("��� Cancel ��ť");

        // ����unlockFlag������δApplyʱ��Ч��
        if (!isAlreadyUnlocked)
        {
            unlockFlag = false;
        }

        // �رմ���
        CloseWindow();
    }

    #endregion

    #region ���Ĺ���

    /// <summary>
    /// ��������
    /// </summary>
    private void UnlockClue()
    {

        // ����GameFlowController��������
        flowController.UnlockClue(clueId);

        // ���Ϊ�ѽ���
        isAlreadyUnlocked = true;

        LogDebug($"�ɹ���������: {clueId}");
    }

    /// <summary>
    /// �رմ���
    /// </summary>
    private void CloseWindow()
    {
        if (windowComponent != null)
        {
            windowComponent.CloseWindow();
        }
        else
        {
            LogError("WindowsWindow ������ö�ʧ���޷��رմ��ڣ�");
        }
    }

    #endregion

    #region UI״̬����

    /// <summary>
    /// ���°�ť״̬
    /// </summary>
    private void UpdateButtonStates()
    {
        // Unlock��ť���ѽ������ѵ�������
        if (unlockButton != null)
        {
            unlockButton.interactable = !unlockFlag;
        }

        // Apply��ť��ֻ�е����Unlock��δApplyʱ�ſ���
        if (applyButton != null)
        {
            applyButton.interactable = unlockFlag && !isAlreadyUnlocked;
        }

        // OK��Cancel��ťʼ�տ���
        if (okButton != null)
        {
            okButton.interactable = true;
        }

        if (cancelButton != null)
        {
            cancelButton.interactable = true;
        }

        LogDebug($"��ť״̬�Ѹ��� - unlockFlag: {unlockFlag}, isAlreadyUnlocked: {isAlreadyUnlocked}");
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[WorksFolderProperties] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[WorksFolderProperties] {message}");
    }

    #endregion
}