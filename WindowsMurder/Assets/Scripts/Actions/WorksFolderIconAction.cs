using UnityEngine;

/// <summary>
/// Works�ļ���ͼ��ר�ý�����Ϊ
/// ��������ǰ��ʾ���������ƽ�Stage���Ҽ����Դ��ڡ���ͼ�����
/// </summary>
public class WorksFolderIconAction : IconAction
{
    [Header("��������")]
    [SerializeField] private string clueId = "works_folder_unlocked";
    [SerializeField] private GameObject lockIconObject;  // ֱ��������ͼ�����

    [Header("����Ԥ����")]
    [SerializeField] private GameObject lockedMessagePrefab;
    [SerializeField] private GameObject propertiesWindowPrefab;

    [Header("��������")]
    [SerializeField] private Transform windowContainer;

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // �������
    private GameFlowController flowController;
    private InteractableIcon iconComponent;

    #region ��ʼ��

    void Awake()
    {
        flowController = FindObjectOfType<GameFlowController>();
        iconComponent = GetComponent<InteractableIcon>();
    }

    void OnEnable()
    {
        // ����ʼ����״̬
        CheckInitialUnlockStatus();

        // �����Ҽ��˵��¼�
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;

        // �������������¼�
        GameEvents.OnClueUnlocked += OnClueUnlocked;

        LogDebug("�Ѷ����¼�");
    }

    void OnDisable()
    {
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;
        GameEvents.OnClueUnlocked -= OnClueUnlocked;

        LogDebug("��ȡ�������¼�");
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

    #region ˫������

    public override void Execute()
    {
        LogDebug("˫�� Works �ļ���");

        if (IsUnlocked())
        {
            ProgressToNextStage();
        }
        else
        {
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
            WindowsWindow window = explorer.GetComponent<WindowsWindow>();
            if (window != null && window.windowRect != null)
            {
                Vector2 position = window.windowRect.anchoredPosition;

                WindowTransitionData transitionData = new WindowTransitionData(position);
                flowController.CacheWindowTransition(transitionData);

                LogDebug($"�ѻ��洰��λ��: {position}");
            }
        }

        flowController.TryProgressToNextStage();
    }

    /// <summary>
    /// ��ʾ������ʾ����
    /// </summary>
    private void ShowLockedMessage()
    {
        GameObject messageWindow = Instantiate(lockedMessagePrefab, windowContainer);
        LogDebug("����ʾ������ʾ����");
    }

    #endregion

    #region �Ҽ��˵�

    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        if (icon.gameObject != gameObject)
        {
            return;
        }

        if (itemId == "properties")
        {
            ShowPropertiesWindow();
        }
    }

    private void ShowPropertiesWindow()
    {
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
            LogDebug($"��ͼ��������: {lockIconObject.name}");
        }
        else
        {
            LogDebug("���棺����������ͼ�꣬����������Ϊ�գ�");
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

    #endregion
}