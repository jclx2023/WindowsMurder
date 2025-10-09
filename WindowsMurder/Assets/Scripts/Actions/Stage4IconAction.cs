using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// �Ի��׶�ö��
/// </summary>
public enum DialogueStage
{
    Locked,      // ��δ�����������
    Active,      // �������Ի�
    Completed    // �������Ҫ�Ի�
}

/// <summary>
/// Stage4ר�õ�Icon��������
/// ֧��ǰ�á���Ҫ���������׶ζԻ�����
/// </summary>
public class Stage4IconAction : IconAction
{
    [Header("=== Stage4 �Ի����� ===")]
    [Tooltip("ǰ�öԻ���ID������������ʱ���ţ���ѡ��")]
    [SerializeField] private string preDialogueBlockId = "";
    [SerializeField] private string mainDialogueBlockId = "";
    [SerializeField] private string postDialogueBlockId = "";

    [Header("=== �������� ===")]
    [SerializeField] private List<string> requiredClues = new List<string>();

    [SerializeField] private List<string> requiredDialogueBlocks = new List<string>();

    [Header("=== ������Ϣ ===")]
    [SerializeField] private DialogueStage currentStage;
    [SerializeField] private bool debugMode = true;

    // ���������
    private GameFlowController flowController;

    #region ��ʼ��

    protected virtual void Awake()
    {
        // ����GameFlowController
        flowController = FindObjectOfType<GameFlowController>();
    }

    protected virtual void OnEnable()
    {
        // ���ĶԻ�����¼����Զ�����״̬
        GameEvents.OnDialogueBlockCompleted += OnDialogueBlockCompleted;
    }

    protected virtual void OnDisable()
    {
        // ȡ������
        GameEvents.OnDialogueBlockCompleted -= OnDialogueBlockCompleted;
    }

    #endregion

    #region IconActionʵ��

    public override void Execute()
    {

        // ���µ�ǰ״̬
        currentStage = GetCurrentDialogueStage();

        // ����״̬���Ŷ�Ӧ�Ի�
        PlayDialogueByStage();
    }

    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;
        if (flowController == null) return false;

        // ����״̬�����ж�
        currentStage = GetCurrentDialogueStage();

        // ����״̬�ж��Ƿ��пɲ��ŵĶԻ�
        switch (currentStage)
        {
            case DialogueStage.Locked:
                return !string.IsNullOrEmpty(preDialogueBlockId);

            case DialogueStage.Active:
                return !string.IsNullOrEmpty(mainDialogueBlockId);

            case DialogueStage.Completed:
                return !string.IsNullOrEmpty(postDialogueBlockId);

            default:
                return false;
        }
    }

    #endregion

    #region ״̬�ж�

    /// <summary>
    /// ��ȡ��ǰ�Ի��׶�
    /// </summary>
    private DialogueStage GetCurrentDialogueStage()
    {
        // ������Ի��Ƿ������
        if (IsDialogueBlockCompleted(mainDialogueBlockId))
        {
            return DialogueStage.Completed;
        }

        // ����Ƿ������������
        if (CheckLockedConditions())
        {
            return DialogueStage.Locked;
        }

        // ����������δ���
        return DialogueStage.Active;
    }

    /// <summary>
    /// ����Ƿ�������״̬��ǰ�����������㣩
    /// </summary>
    private bool CheckLockedConditions()
    {
        // �����������
        if (requiredClues != null && requiredClues.Count > 0)
        {
            foreach (string clueId in requiredClues)
            {
                if (!flowController.HasClue(clueId))
                {
                    LogDebug($"ȱ�ٱ�������: {clueId}");
                    return true; // ���������㣬��������
                }
            }
        }

        // �������ǰ�öԻ���
        if (requiredDialogueBlocks != null && requiredDialogueBlocks.Count > 0)
        {
            var completedBlocks = flowController.GetCompletedBlocksSafe();

            foreach (string blockId in requiredDialogueBlocks)
            {
                if (!completedBlocks.Contains(blockId))
                {
                    LogDebug($"ȱ�ٱ���Ի���: {blockId}");
                    return true; // ���������㣬��������
                }
            }
        }

        return false; // ������������
    }

    /// <summary>
    /// ���Ի����Ƿ������
    /// </summary>
    private bool IsDialogueBlockCompleted(string blockId)
    {
        if (string.IsNullOrEmpty(blockId)) return false;

        var completedBlocks = flowController.GetCompletedBlocksSafe();
        return completedBlocks.Contains(blockId);
    }

    #endregion

    #region �Ի�����

    /// <summary>
    /// ���ݵ�ǰ�׶β��Ŷ�Ӧ�Ի�
    /// </summary>
    private void PlayDialogueByStage()
    {
        string dialogueBlockId = "";

        switch (currentStage)
        {
            case DialogueStage.Locked:
                dialogueBlockId = preDialogueBlockId;
                LogDebug("���Բ���ǰ�öԻ�");
                break;

            case DialogueStage.Active:
                dialogueBlockId = mainDialogueBlockId;
                LogDebug("������Ҫ�Ի�");
                break;

            case DialogueStage.Completed:
                dialogueBlockId = postDialogueBlockId;
                LogDebug("���ź����Ի�");
                break;
        }

        // ����Ի���IDΪ�գ����������
        if (string.IsNullOrEmpty(dialogueBlockId))
        {
            LogDebug($"{currentStage}�׶�û�����öԻ��飬�޷�����");
            ShowNoDialogueHint();
            return;
        }

        // ���ŶԻ�
        PlayDialogue(dialogueBlockId);
    }

    /// <summary>
    /// ����ָ���ĶԻ���
    /// </summary>
    protected virtual void PlayDialogue(string dialogueBlockId)
    {
        if (string.IsNullOrEmpty(dialogueBlockId))
        {
            LogError("�Ի���IDΪ�գ�");
            return;
        }

        LogDebug($"��ʼ���ŶԻ���: {dialogueBlockId}");

        // ͨ��GameFlowController�����Ի�
        flowController.StartDialogueBlock(dialogueBlockId);
    }

    /// <summary>
    /// ��ʾ"�޶Ի�"��ʾ���ɱ�������д��ʵ������Ч����
    /// </summary>
    protected virtual void ShowNoDialogueHint()
    {
        LogDebug("�ý׶�û�жԻ�����");
    }
    #endregion

    #region �¼��ص�

    /// <summary>
    /// �Ի������ʱ�Ļص�
    /// </summary>
    private void OnDialogueBlockCompleted(string completedBlockId)
    {
        // ����Ƿ��ǵ�ǰicon��صĶԻ���
        if (completedBlockId == mainDialogueBlockId)
        {
            LogDebug($"��Ҫ�Ի��� {completedBlockId} �����");

            // ����״̬
            currentStage = DialogueStage.Completed;

            // ������ɻص�����������չ��
            OnMainDialogueCompleted();
        }
    }

    /// <summary>
    /// ��Ҫ�Ի����ʱ�Ļص�����������д��
    /// </summary>
    protected virtual void OnMainDialogueCompleted()
    {
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage4Icon:{actionName}] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Stage4Icon:{actionName}] {message}");
    }

    #endregion
}