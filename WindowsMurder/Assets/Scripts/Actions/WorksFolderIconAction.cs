using UnityEngine;

/// <summary>
/// Works�ļ���ͼ��ר�ý�����Ϊ
/// ��������ǰ��ʾ���������ƽ�Stage���Ҽ����Դ��ڡ���ͼ�����
/// </summary>
public class WorksFolderIconAction : IconAction
{
    [Header("��������")]
    [SerializeField] private string clueId = "works_folder_unlocked";
    [SerializeField] private string lockIconName = "Image_Lock";

    [Header("����Ԥ����")]
    [SerializeField] private GameObject lockedMessagePrefab;     // ������ʾ����
    [SerializeField] private GameObject propertiesWindowPrefab;  // ���Դ���

    [Header("��������")]
    [SerializeField] private Transform windowContainer;          // �������ɵĸ�����

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // �������
    private GameFlowController flowController;
    private GameObject lockIconObject;
    private InteractableIcon iconComponent;

    #region ��ʼ��

    void Awake()
    {
        // ��ȡ�������
        flowController = FindObjectOfType<GameFlowController>();
        iconComponent = GetComponent<InteractableIcon>();
    }

    void Start()
    {
        // ������ͼ���Ӷ���
        FindLockIcon();

        // ����ʼ����״̬
        CheckInitialUnlockStatus();
    }

    void OnEnable()
    {
        // �����Ҽ��˵��¼�
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;

        // �������������¼�
        GameEvents.OnClueUnlocked += OnClueUnlocked;

        LogDebug("�Ѷ����¼�");
    }

    void OnDisable()
    {
        // ȡ������
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;
        GameEvents.OnClueUnlocked -= OnClueUnlocked;

        LogDebug("��ȡ�������¼�");
    }

    /// <summary>
    /// ������ͼ���Ӷ���
    /// </summary>
    private void FindLockIcon()
    {
        if (!string.IsNullOrEmpty(lockIconName))
        {
            Transform lockTransform = transform.Find(lockIconName);
            if (lockTransform != null)
            {
                lockIconObject = lockTransform.gameObject;
                LogDebug($"�ҵ���ͼ��: {lockIconName}");
            }
        }
    }

    /// <summary>
    /// ����ʼ����״̬
    /// </summary>
    private void CheckInitialUnlockStatus()
    {
        if (flowController != null && IsUnlocked())
        {
            HideLockIcon();
            LogDebug("��ʼ״̬���ѽ���");
        }
    }

    #endregion

    #region ˫������ - IconAction��д

    public override void Execute()
    {
        LogDebug("˫�� Works �ļ���");

        if (IsUnlocked())
        {
            // �ѽ��� - �ƽ�����һStage
            ProgressToNextStage();
        }
        else
        {
            // δ���� - ��ʾ��ʾ����
            ShowLockedMessage();
        }
    }

    /// <summary>
    /// ����Ƿ��ѽ���
    /// </summary>
    private bool IsUnlocked()
    {
        if (flowController == null) return false;
        return flowController.HasClue(clueId);
    }

    /// <summary>
    /// �ƽ�����һStage
    /// </summary>
    private void ProgressToNextStage()
    {
        LogDebug("�����ƽ�����һStage");

        ExplorerManager explorer = GetComponentInParent<ExplorerManager>();
        if (explorer != null)
        {
            LogDebug("�ҵ�ExplorerManager");

            WindowsWindow window = explorer.GetComponent<WindowsWindow>();
            if (window != null && window.windowRect != null)
            {
                // ��ȡ����λ��
                Vector2 position = window.windowRect.anchoredPosition;

                // ���浽GameFlowController
                WindowTransitionData transitionData = new WindowTransitionData(position);
                flowController.CacheWindowTransition(transitionData);

                LogDebug($"�ѻ��洰��λ��: {position}");
            }
            else
            {
                LogError("�޷��ҵ�WindowsWindow�����windowRectΪ��");
            }
        }
        else
        {
            LogError("�޷��ҵ�ExplorerManager������λ�ý����ᱻ����");
        }

        flowController.TryProgressToNextStage();
    }

    /// <summary>
    /// ��ʾ������ʾ����
    /// </summary>
    private void ShowLockedMessage()
    {
        // ʵ������ʾ����
        GameObject messageWindow = Instantiate(lockedMessagePrefab, windowContainer);
        LogDebug("����ʾ������ʾ����");
    }

    #endregion

    #region �Ҽ��˵� - ���Դ���

    /// <summary>
    /// �Ҽ��˵������¼�����
    /// </summary>
    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        // ����Ƿ����Լ�������
        if (icon.gameObject != gameObject)
        {
            Debug.Log("�����Լ������ģ�����");
            return;
        }

        // ����Ƿ���Properties�˵���
        if (itemId == "properties")
        {
            Debug.Log("ƥ��ɹ�����ʾ���Դ���");
            ShowPropertiesWindow();
        }
        else
        {
            Debug.Log($"itemId��ƥ�䣬����'properties'��ʵ��'{itemId}'");
        }
    }

    /// <summary>
    /// ��ʾ���Դ���
    /// </summary>
    private void ShowPropertiesWindow()
    {
        // ʵ�������Դ���
        GameObject propertiesWindow = Instantiate(propertiesWindowPrefab, windowContainer);
        LogDebug("���������Դ���");
    }

    #endregion

    #region ����״̬����

    /// <summary>
    /// ���������¼�����
    /// </summary>
    private void OnClueUnlocked(string unlockedClueId)
    {
        if (unlockedClueId == clueId)
        {
            HideLockIcon();
            LogDebug($"Works�ļ����ѽ���������ID: {clueId}");
        }
    }

    /// <summary>
    /// ������ͼ��
    /// </summary>
    private void HideLockIcon()
    {
        if (lockIconObject != null)
        {
            lockIconObject.SetActive(false);
            LogDebug("��ͼ��������");
        }
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[WorksFolderIcon] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[WorksFolderIcon] {message}");
    }

    #endregion
}