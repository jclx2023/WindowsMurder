using UnityEngine;
using System.Collections;

/// <summary>
/// Stage3��ʼ���� - ����Stage3����ʱ����Explorer���ڲ�Ӧ�ô���ת������
/// </summary>
public class Stage3Initializer : MonoBehaviour
{
    [Header("�������")]
    [SerializeField] private GameFlowController flowController;
    [SerializeField] private Transform canvasTransform;  // �������ɵ�Canvas

    [Header("Ԥ��������")]
    [SerializeField] private GameObject explorerStage3Prefab;  // Stage3ר�õ�ExplorerԤ����

    [Header("Ĭ������")]
    [SerializeField] private Vector2 defaultWindowPosition = Vector2.zero;  // �޻�������ʱ��Ĭ��λ��

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // ��ֹ�ظ���ʼ��
    private bool hasInitialized = false;

    // Controller����
    private Stage3Controller stage3Controller;

    #region Unity��������

    void Awake()
    {
        // ����GameFlowController�����δ��Inspector�����ã�
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
            if (flowController == null)
            {
                LogError("δ�ҵ�GameFlowController��");
            }
        }

        // ����Stage3Controller
        stage3Controller = GetComponent<Stage3Controller>();
    }

    void OnEnable()
    {
        // ��ֹ�ظ���ʼ��
        if (hasInitialized)
        {
            LogDebug("�Ѿ���ʼ����������");
            return;
        }

        // ִ�г�ʼ��
        InitializeStage3();
        hasInitialized = true;
    }

    #endregion

    #region ��ʼ���߼�

    /// <summary>
    /// ��ʼ��Stage3 - ����Explorer����
    /// </summary>
    private void InitializeStage3()
    {
        LogDebug("��ʼ��ʼ��Stage3");

        // ��GameFlowController���Ѵ���ת������
        WindowTransitionData? transitionData = flowController.ConsumeWindowTransition();

        // ȷ������λ��
        Vector2 windowPosition;
        if (transitionData.HasValue)
        {
            windowPosition = transitionData.Value.windowPosition;
            LogDebug($"ʹ�û���Ĵ���λ��: {windowPosition}");
        }
        else
        {
            windowPosition = defaultWindowPosition;
            LogDebug($"�޻������ݣ�ʹ��Ĭ��λ��: {windowPosition}");
        }

        // ʵ����Explorer���ڲ���ȡ����
        ExplorerManager explorerManager = CreateExplorerWindow(windowPosition);

        // ��Explorer���ô��ݸ�Controller
        if (stage3Controller != null && explorerManager != null)
        {
            stage3Controller.SetExplorerReference(explorerManager);
        }

        LogDebug("Stage3��ʼ�����");
    }

    /// <summary>
    /// ����Explorer���ڲ�����λ��
    /// </summary>
    private ExplorerManager CreateExplorerWindow(Vector2 position)
    {
        GameObject explorerWindow = Instantiate(explorerStage3Prefab, canvasTransform);

        // ���������ⲿλ�ã���Start֮ǰ��
        WindowsWindow window = explorerWindow.GetComponent<WindowsWindow>();
        if (window != null)
        {
            window.SetExternalInitialPosition(position);
            LogDebug($"����WindowsWindow���ݳ�ʼλ��: {position}");
        }

        ExplorerManager explorerManager = explorerWindow.GetComponent<ExplorerManager>();

        explorerWindow.transform.SetAsLastSibling();

        LogDebug($"Explorer�����Ѵ���");

        return explorerManager;
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage3Init] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Stage3Init] {message}");
    }

    #endregion
}