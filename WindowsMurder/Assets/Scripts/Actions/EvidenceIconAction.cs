using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ͨ��֤�����Action - ֧�ֶ������������Ҽ��˵�
/// ������������Ҫ"�Ի�-����-������-�Ի�"���̵�֤��icon
/// </summary>
public class EvidenceIconAction : IconAction
{
    [Header("=== ֤������ ===")]
    public string evidenceId = "knife";

    [Header("=== �������� ===")]
    [Tooltip("��Ҫ��������������ID")]
    public List<string> requiredClues = new List<string> { "clue_dark_red", "clue_bright_red" };

    [Tooltip("�Ƿ���Ҫ����������false=����һ�����ɣ�")]
    public bool requireAllClues = true;

    [Header("=== �Ի������� ===")]
    public string beforeDialogueBlockId = "001";
    public string afterDialogueBlockId = "002";

    [Header("=== �������� ===")]
    public GameObject windowPrefab;
    public GameObject propertiesWindowPrefab; // ���Դ���Ԥ����
    public Canvas targetCanvas;

    [Header("=== ����ѡ�� ===")]
    public bool allowReopenAfterComplete = true;
    public bool enableDebugLog = true;

    [Header("=== ״̬��ʾ������ʱֻ����===")]
    [SerializeField] private InvestigationState currentState = InvestigationState.NotInvestigated;
    [SerializeField] private bool isWaitingForDialogue = false;
    [SerializeField] private bool isWaitingForClues = false;
    [SerializeField] private List<string> unlockedCluesList = new List<string>();

    // ˽�б���
    private string waitingForDialogueBlockId;
    private GameFlowController gameFlowController;
    private Canvas canvas;
    private HashSet<string> unlockedClues = new HashSet<string>();
    private InteractableIcon iconComponent;

    /// <summary>
    /// ����״̬ö��
    /// </summary>
    private enum InvestigationState
    {
        NotInvestigated,
        PlayingBeforeDialogue,
        WindowOpen,
        WaitingForClues,
        PlayingAfterDialogue,
        Completed
    }

    #region Unity��������

    void Awake()
    {
        iconComponent = GetComponent<InteractableIcon>();
    }

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        FindTargetCanvas();
        RestoreStateFromSave();
    }

    void OnEnable()
    {
        // ���ĶԻ������¼�
        DialogueUI.OnDialogueBlockEnded += HandleDialogueEnded;

        // �������������¼�
        GameEvents.OnClueUnlocked += HandleClueUnlocked;

        // �����Ҽ��˵��¼�
        InteractableIcon.OnContextMenuItemClicked += OnContextMenuItemClicked;

        DebugLog("�Ѷ����¼�");
    }

    void OnDisable()
    {
        DialogueUI.OnDialogueBlockEnded -= HandleDialogueEnded;
        GameEvents.OnClueUnlocked -= HandleClueUnlocked;
        InteractableIcon.OnContextMenuItemClicked -= OnContextMenuItemClicked;

        DebugLog("��ȡ�������¼�");
    }

    #endregion

    #region Canvas����

    private void FindTargetCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            DebugLog($"�Ӹ����ҵ�Canvas: {canvas.name}");
            return;
        }

        GameObject canvasObj = GameObject.FindWithTag("WindowCanvas");
        if (canvasObj != null)
        {
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                DebugLog($"ͨ��Tag�ҵ�Canvas: {canvas.name}");
                return;
            }
        }
    }

    #endregion

    #region ״̬�ָ�

    private void RestoreStateFromSave()
    {
        if (gameFlowController == null) return;

        var completedBlocks = gameFlowController.GetCompletedBlocksSafe();

        // ���ǰ�öԻ��Ƿ����
        if (completedBlocks.Contains(beforeDialogueBlockId))
        {
            // �����öԻ��Ƿ����
            if (completedBlocks.Contains(afterDialogueBlockId))
            {
                currentState = InvestigationState.Completed;
                DebugLog("�Ӵ浵�ָ�״̬�������");
                return;
            }

            // ����ѽ���������
            foreach (string clueId in requiredClues)
            {
                if (gameFlowController.HasClue(clueId))
                {
                    unlockedClues.Add(clueId);
                    unlockedCluesList.Add(clueId);
                }
            }

            // �ж��Ƿ������������ѽ���
            if (AreAllCluesUnlocked())
            {
                currentState = InvestigationState.PlayingAfterDialogue;
                DebugLog($"�Ӵ浵�ָ�״̬�����������ѽ��� ({unlockedClues.Count}/{requiredClues.Count})");
            }
            else
            {
                currentState = InvestigationState.WaitingForClues;
                isWaitingForClues = true;
                DebugLog($"�Ӵ浵�ָ�״̬���ȴ��������� ({unlockedClues.Count}/{requiredClues.Count})");
            }
        }
    }

    #endregion

    #region ˫������

    public override void Execute()
    {
        DebugLog($"Execute() ������ - ��ǰ״̬: {currentState}");

        switch (currentState)
        {
            case InvestigationState.NotInvestigated:
                PlayBeforeDialogue();
                break;

            case InvestigationState.PlayingBeforeDialogue:
                DebugLog("�Ի������У������ظ�����");
                break;

            case InvestigationState.WindowOpen:
            case InvestigationState.WaitingForClues:
            case InvestigationState.PlayingAfterDialogue:
            case InvestigationState.Completed:
                if (allowReopenAfterComplete)
                {
                    DebugLog("���´򿪴���");
                    CreateWindow();
                }
                else
                {
                    DebugLog("�ѵ�����ɣ����������´�");
                }
                break;
        }
    }

    public override bool CanExecute()
    {
        if (string.IsNullOrEmpty(beforeDialogueBlockId) || windowPrefab == null)
        {
            return false;
        }

        if (isWaitingForDialogue)
        {
            return false;
        }

        return base.CanExecute();
    }

    #endregion

    #region �Ҽ��˵�

    /// <summary>
    /// �Ҽ��˵������¼�����
    /// </summary>
    private void OnContextMenuItemClicked(InteractableIcon icon, string itemId)
    {
        // ����Ƿ��Ǳ�ͼ����¼�
        if (icon.gameObject != gameObject)
        {
            return;
        }

        DebugLog($"�Ҽ��˵����: {itemId}");

        switch (itemId)
        {
            case "properties":
                ShowPropertiesWindow();
                break;

            case "open":
                // ��ѡ���Ҽ�Ҳ�ܴ�
                Execute();
                break;

            // ������Ӹ����Զ���˵���
            default:
                DebugLog($"δ����Ĳ˵���: {itemId}");
                break;
        }
    }

    /// <summary>
    /// ��ʾ���Դ���
    /// </summary>
    private void ShowPropertiesWindow()
    {
        if (canvas == null)
        {
            canvas = targetCanvas != null ? targetCanvas : FindObjectOfType<Canvas>();
        }

        GameObject propertiesWindow = Instantiate(propertiesWindowPrefab, canvas.transform);
        propertiesWindow.name = $"{evidenceId}_Properties";

        DebugLog($"���������Դ���: {propertiesWindow.name}");
    }

    #endregion

    #region �Ի�����

    private void PlayBeforeDialogue()
    {
        currentState = InvestigationState.PlayingBeforeDialogue;
        isWaitingForDialogue = true;
        waitingForDialogueBlockId = beforeDialogueBlockId;

        gameFlowController.StartDialogueBlock(beforeDialogueBlockId);

        DebugLog($"��ʼ����ǰ�öԻ�: {beforeDialogueBlockId}");
    }

    private void PlayAfterDialogue()
    {
        currentState = InvestigationState.PlayingAfterDialogue;
        isWaitingForDialogue = true;
        waitingForDialogueBlockId = afterDialogueBlockId;

        gameFlowController.StartDialogueBlock(afterDialogueBlockId);

        DebugLog($"��ʼ���ź��öԻ�: {afterDialogueBlockId}");
    }

    /// <summary>
    /// ����Ի������¼�
    /// </summary>
    private void HandleDialogueEnded(string fileName, string blockId)
    {
        // ����Ƿ������ǵȴ��ĶԻ�
        if (blockId == waitingForDialogueBlockId)
        {
            DebugLog($"�������Ի�����: {blockId}");

            waitingForDialogueBlockId = null;
            isWaitingForDialogue = false;

            // �������ĸ��Ի������������
            if (blockId == beforeDialogueBlockId)
            {
                OnBeforeDialogueComplete();
            }
            else if (blockId == afterDialogueBlockId)
            {
                OnAfterDialogueComplete();
            }
        }
    }

    private void OnBeforeDialogueComplete()
    {
        DebugLog("ǰ�öԻ���ɣ�׼�����ɴ���");
        CreateWindow();
    }

    private void OnAfterDialogueComplete()
    {
        DebugLog("���öԻ���ɣ��������");
        currentState = InvestigationState.Completed;
    }

    #endregion

    #region ������������

    /// <summary>
    /// �������������¼��������߼����������������󴥷�after�Ի���
    /// </summary>
    private void HandleClueUnlocked(string unlockedClueId)
    {
        // ����Ƿ���������Ҫ������֮һ
        if (!requiredClues.Contains(unlockedClueId))
        {
            return;
        }

        // ����Ƿ��Ѿ�������
        if (unlockedClues.Contains(unlockedClueId))
        {
            DebugLog($"���� [{unlockedClueId}] �Ѿ�������������");
            return;
        }

        // ��¼����������
        unlockedClues.Add(unlockedClueId);
        unlockedCluesList.Add(unlockedClueId);

        DebugLog($"�����ѽ���: {unlockedClueId} ({unlockedClues.Count}/{requiredClues.Count})");

        // ����Ƿ������������ѽ���
        if (isWaitingForClues && AreAllCluesUnlocked())
        {
            DebugLog("���������Ѽ��룬׼������after�Ի�");
            isWaitingForClues = false;

            // ����after�Ի�
            PlayAfterDialogue();
        }
    }

    /// <summary>
    /// ����Ƿ������������ѽ���
    /// </summary>
    private bool AreAllCluesUnlocked()
    {
        if (requireAllClues)
        {
            // ��Ҫ��������
            foreach (string clueId in requiredClues)
            {
                if (!unlockedClues.Contains(clueId))
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            // ֻ��Ҫ����һ������
            return unlockedClues.Count > 0;
        }
    }

    #endregion

    #region ���ڹ���

    private void CreateWindow()
    {
        if (canvas == null)
        {
            canvas = targetCanvas != null ? targetCanvas : FindObjectOfType<Canvas>();
        }
        GameObject windowObj = Instantiate(windowPrefab, canvas.transform);
        windowObj.name = $"{evidenceId}_Window";

        currentState = InvestigationState.WaitingForClues;
        isWaitingForClues = true;

        DebugLog($"�Ѵ�������: {windowObj.name}���ȴ��������� (��Ҫ {requiredClues.Count} ��)");
    }

    #endregion


    #region ���Թ���
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EvidenceIconAction - {evidenceId}] {message}");
        }
    }
    #endregion
}