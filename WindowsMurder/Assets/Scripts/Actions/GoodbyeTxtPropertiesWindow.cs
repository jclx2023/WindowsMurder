using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Goodbye.txt ���Դ��ڿ����� - �������λ�ò��ŶԻ�
/// </summary>
public class GoodbyeTxtPropertiesWindow : MonoBehaviour
{
    [Header("=== �Ի����� ===")]
    [SerializeField] private string dialogueBlockId = "420";

    [Header("=== ��ť���� ===")]
    [SerializeField] private Button okButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = true;

    // ����ʱ״̬
    private bool hasTriggeredDialogue = false;

    // �������
    private GameFlowController flowController;
    private WindowsWindow windowComponent;

    void Awake()
    {
        flowController = FindObjectOfType<GameFlowController>();
        windowComponent = GetComponent<WindowsWindow>();

        // �󶨰�ť�¼�
        if (okButton != null)
            okButton.onClick.AddListener(CloseWindow);

        if (applyButton != null)
            applyButton.onClick.AddListener(() => LogDebug("��� Apply ��ť"));

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CloseWindow);
    }

    void Update()
    {
        // ���������
        if (!hasTriggeredDialogue && Input.GetMouseButtonDown(0))
        {
            PlayDialogue();
        }
    }

    /// <summary>
    /// ���ŶԻ���
    /// </summary>
    private void PlayDialogue()
    {
        hasTriggeredDialogue = true;

        if (flowController != null)
        {
            flowController.StartDialogueBlock(dialogueBlockId);
            LogDebug($"�Ѵ����Ի���: {dialogueBlockId}");
        }
    }

    /// <summary>
    /// �رմ���
    /// </summary>
    private void CloseWindow()
    {
        windowComponent.CloseWindow();
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GoodbyeTxtProperties] {message}");
        }
    }
}