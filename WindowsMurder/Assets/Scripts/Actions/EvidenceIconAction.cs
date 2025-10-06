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
        Completed
    }

    #region Unity��������

    void Start()
    {
        // ��������
        gameFlowController = FindObjectOfType<GameFlowController>();

        // ����Canvas������ʹ���ֶ����ã�����Ӹ�������
        FindTargetCanvas();

        // �ָ�״̬���Ӵ浵��
        RestoreStateFromSave();
    }

    void OnEnable()
    {
        DialogueUI.OnDialogueBlockEnded += HandleDialogueEnded;
        DebugLog("�Ѷ��ĶԻ��¼�");
    }

    void OnDisable()
    {
        DialogueUI.OnDialogueBlockEnded -= HandleDialogueEnded;
        DebugLog("��ȡ�����ĶԻ��¼�");
    }

    #endregion

    #region Canvas����

    /// <summary>
    /// ����Ŀ��Canvas
    /// </summary>
    private void FindTargetCanvas()
    {
        // 1. �Ӹ������������Canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            DebugLog($"�Ӹ����ҵ�Canvas: {canvas.name}");
            return;
        }

        // 2. ���û�ҵ�������ͨ��Tag���ң���Ҫ��ǰ����Tag��
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
                currentState = InvestigationState.WindowOpen;
                DebugLog("�Ӵ浵�ָ�״̬�������Ѵ򿪹�");
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

    private void HandleDialogueEnded(string fileName, string blockId)
    {
        if (blockId == waitingForDialogueBlockId)
        {
            DebugLog($"�������Ի�����: {blockId}");

            waitingForDialogueBlockId = null;
            isWaitingForDialogue = false;

            OnBeforeDialogueComplete();
        }
    }

    private void OnBeforeDialogueComplete()
    {
        DebugLog("ǰ�öԻ���ɣ�׼�����ɴ���");
        CreateWindow();
    }

    #endregion

    #region ���ڹ���

    private void CreateWindow()
    {

        GameObject windowObj = Instantiate(windowPrefab, canvas.transform);
        windowObj.name = $"{evidenceId}_Window";

        currentState = InvestigationState.WindowOpen;

        DebugLog($"�Ѵ�������: {windowObj.name}������Canvas: {canvas.name}");
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