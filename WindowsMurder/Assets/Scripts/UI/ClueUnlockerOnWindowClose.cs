using UnityEngine;

/// <summary>
/// ���ڹر�ʱ�����������
/// ��������Ҫ�ڹر�ʱ���������Ĵ���GameObject��
/// </summary>
[RequireComponent(typeof(WindowsWindow))]
public class ClueUnlockerOnWindowClose : MonoBehaviour
{
    [Header("��������")]
    [Tooltip("���ڹرպ�Ҫ����������ID")]
    [SerializeField] private string clueIdToUnlock = "";

    [Header("�ӳ�����")]
    [Tooltip("���ڹرպ�ȴ���ʱ�䣨�룩��ȷ��������ȫ����")]
    [SerializeField] private float delayAfterClose = 0.1f;

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // ������������
    private WindowsWindow cachedWindow;
    private GameFlowController gameFlowController;

    void Awake()
    {
        // ���洰�����
        cachedWindow = GetComponent<WindowsWindow>();
    }

    void Start()
    {
        // ���� GameFlowController
        gameFlowController = FindObjectOfType<GameFlowController>();
    }

    void OnEnable()
    {
        // ���Ĵ��ڹر��¼�
        WindowsWindow.OnWindowClosed += OnWindowClosedHandler;
    }

    void OnDisable()
    {
        // ȡ�����ģ���ֹ�ڴ�й©
        WindowsWindow.OnWindowClosed -= OnWindowClosedHandler;
    }

    /// <summary>
    /// ���ڹر��¼�������
    /// </summary>
    private void OnWindowClosedHandler(WindowsWindow closedWindow)
    {
        // ����Ƿ��ǵ�ǰ����
        if (closedWindow != cachedWindow)
        {
            return;
        }

        LogDebug($"���ڼ����رգ�׼���ӳٽ�������: {clueIdToUnlock}");

        // ��֤ GameFlowController ��Ȼ����
        if (gameFlowController == null)
        {
            // ���β���
            gameFlowController = FindObjectOfType<GameFlowController>();
        }

        gameFlowController.UnlockClueDelayed(clueIdToUnlock, delayAfterClose);
    }

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[ClueUnlocker-{gameObject.name}] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[ClueUnlocker-{gameObject.name}] {message}");
    }

    #endregion
}