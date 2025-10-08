using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Goodbye.txt ���Դ��ڿ����� - �򿪴���ʱ�ӳٲ��ŶԻ�
/// </summary>
public class GoodbyeTxtPropertiesWindow : MonoBehaviour
{
    [Header("=== �Ի����� ===")]
    [SerializeField] private string dialogueBlockId = "420";
    [Tooltip("�ӳٲ��ŶԻ���ʱ�䣨�룩")]
    [SerializeField] private float dialogueDelay = 1f;
    [Tooltip("�Ƿ��ڴ򿪶Ի�ʱ�رմ���")]
    [SerializeField] private bool closeWindowOnDialogue = false;

    [Header("=== ��ť���� ===")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("=== �������ã���ѡ��===")]
    [SerializeField] private bool enableClueUnlock = false;
    [SerializeField] private string clueId = "fake_goodbye";

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = true;

    // ����ʱ״̬
    private bool hasTriggeredDialogue = false;
    private bool hasUnlockedClue = false;

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
        // ��������Ƿ��ѽ���
        CheckInitialClueState();

        // ���ڴ򿪺��ӳٲ��ŶԻ�
        StartCoroutine(PlayDialogueDelayed());
    }

    /// <summary>
    /// �󶨰�ť����¼�
    /// </summary>
    private void BindButtonEvents()
    {
        if (okButton != null)
            okButton.onClick.AddListener(OnOKClick);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClick);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClick);
    }

    /// <summary>
    /// ���������ʼ״̬
    /// </summary>
    private void CheckInitialClueState()
    {
        if (!enableClueUnlock) return;

        if (flowController != null)
        {
            hasUnlockedClue = flowController.HasClue(clueId);

            if (hasUnlockedClue)
            {
                LogDebug($"���� {clueId} �Ѿ�����");
            }
        }
    }

    #endregion

    #region �Ի������߼�

    /// <summary>
    /// �ӳٲ��ŶԻ���
    /// </summary>
    private IEnumerator PlayDialogueDelayed()
    {
        LogDebug($"���� {dialogueDelay} ��󲥷ŶԻ���: {dialogueBlockId}");

        yield return new WaitForSeconds(dialogueDelay);

        // ֻ����һ�ζԻ�
        if (!hasTriggeredDialogue)
        {
            hasTriggeredDialogue = true;

            // ���� GameFlowController �����Ի���
            if (flowController != null)
            {
                flowController.StartDialogueBlock(dialogueBlockId);
                LogDebug($"�Ѵ����Ի���: {dialogueBlockId}");

                // ����������������ã�
                if (enableClueUnlock && !hasUnlockedClue)
                {
                    UnlockClue();
                }

                // ��������˶Ի�ʱ�رմ���
                if (closeWindowOnDialogue)
                {
                    CloseWindow();
                }
            }
        }
    }

    #endregion

    #region ��ť�¼�����

    /// <summary>
    /// OK ��ť���
    /// </summary>
    private void OnOKClick()
    {
        LogDebug("��� OK ��ť");
        CloseWindow();
    }

    /// <summary>
    /// Apply ��ť���
    /// </summary>
    private void OnApplyClick()
    {
        LogDebug("��� Apply ��ť���޲�����");
        // Apply ��ťͨ�����رմ��ڣ�ֻӦ�ø���
    }

    /// <summary>
    /// Cancel ��ť���
    /// </summary>
    private void OnCancelClick()
    {
        LogDebug("��� Cancel ��ť");
        CloseWindow();
    }

    #endregion

    #region ���Ĺ���

    /// <summary>
    /// ��������
    /// </summary>
    private void UnlockClue()
    {
        if (!enableClueUnlock || hasUnlockedClue) return;

        if (flowController != null)
        {
            flowController.UnlockClue(clueId);
            hasUnlockedClue = true;
            LogDebug($"�ɹ���������: {clueId}");
        }
    }

    /// <summary>
    /// �رմ���
    /// </summary>
    private void CloseWindow()
    {
        windowComponent.CloseWindow();
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GoodbyeTxtProperties] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GoodbyeTxtProperties] {message}");
    }

    #endregion
}