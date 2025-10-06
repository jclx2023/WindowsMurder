using System.Linq;
using UnityEngine;

/// <summary>
/// ͨ��֤�����Action - ����˫�������Ի������ɴ���
/// ������������Ҫ"�Ի�-����"���̵�֤��icon
/// </summary>
public class EvidenceIconAction : IconAction
{
    [Header("=== ֤������ ===")]
    public string evidenceId = "recyclebin";

    public string clueId = "clue_recyclebin";

    [Header("=== �Ի������� ===")]
    public string beforeDialogueBlockId = "001";
    public string afterDialogueBlockId = "002";

    [Header("=== �������� ===")]
    public GameObject windowPrefab;
    public Canvas targetCanvas;

    [Header("=== ����ѡ�� ===")]
    public bool allowReopenAfterComplete = true;
    public bool enableDebugLog = true;

    [Header("=== ״̬��ʾ������ʱֻ����===")]
    [SerializeField] private InvestigationState currentState = InvestigationState.NotInvestigated;
    [SerializeField] private bool isWaitingForDialogue = false;
    [SerializeField] private bool isWaitingForClue = false;

    // ˽�б���
    private string waitingForDialogueBlockId;
    private GameFlowController gameFlowController;
    private Canvas canvas;

    /// <summary>
    /// ����״̬ö��
    /// </summary>
    private enum InvestigationState
    {
        NotInvestigated,
        PlayingBeforeDialogue,
        WindowOpen,
        WaitingForClue,         // �����Ѵ򿪣��ȴ���������
        PlayingAfterDialogue,   // �����ѽ��������ڲ���after�Ի�
        Completed
    }

    #region Unity��������

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

        DebugLog("�Ѷ����¼�");
    }

    void OnDisable()
    {
        DialogueUI.OnDialogueBlockEnded -= HandleDialogueEnded;
        GameEvents.OnClueUnlocked -= HandleClueUnlocked;
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

        if (completedBlocks.Contains(beforeDialogueBlockId))
        {
            if (completedBlocks.Contains(afterDialogueBlockId))
            {
                currentState = InvestigationState.Completed;
                DebugLog("�Ӵ浵�ָ�״̬�������");
            }
            else
            {
                // ��������Ƿ��ѽ���
                if (gameFlowController.HasClue(clueId))
                {
                    currentState = InvestigationState.PlayingAfterDialogue;
                    DebugLog("�Ӵ浵�ָ�״̬�������ѽ���");
                }
                else
                {
                    currentState = InvestigationState.WaitingForClue;
                    isWaitingForClue = true;
                    DebugLog("�Ӵ浵�ָ�״̬���ȴ���������");
                }
            }
        }
    }

    #endregion

    #region ����ִ��

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
            case InvestigationState.WaitingForClue:
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
    /// �������������¼��������߼������������󴥷�after�Ի���
    /// </summary>
    private void HandleClueUnlocked(string unlockedClueId)
    {
        // ֻ�е������ڵȴ��������ʱ�Ŵ���
        if (unlockedClueId == clueId && isWaitingForClue)
        {
            DebugLog($"��������������: {clueId}��׼������after�Ի�");

            isWaitingForClue = false;

            // ����after�Ի�
            PlayAfterDialogue();
        }
    }

    #endregion

    #region ���ڹ���

    private void CreateWindow()
    {
        GameObject windowObj = Instantiate(windowPrefab, canvas.transform);
        windowObj.name = $"{evidenceId}_Window";

        currentState = InvestigationState.WaitingForClue;
        isWaitingForClue = true;

        DebugLog($"�Ѵ�������: {windowObj.name}���ȴ���������");
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