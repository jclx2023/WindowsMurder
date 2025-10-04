using System.Linq;
using UnityEngine;

/// <summary>
/// Stage2�ļ������⽻�������� - ����D:\Files·�������Ի�
/// ���ݽ���״̬������ͬ�Ի���
/// </summary>
public class Stage2FilesController : MonoBehaviour
{
    [Header("·������")]
    [SerializeField] private string targetPathId = "DFiles";

    [Header("�Ի�������")]
    [SerializeField] private string firstDialogueBlockId = "002";
    [SerializeField] private string unlockedDialogueBlockId = "003";

    [Header("��������")]
    [SerializeField] private string unlockClueId = "works_folder_unlocked";

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    private GameFlowController gameFlowController;
    private ExplorerManager currentExplorer; // ׷�ٵ�ǰExplorerʵ��

    private bool hasTriggeredFirst = false;
    private bool hasTriggeredUnlocked = false;

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        // ����·���仯�¼�
        ExplorerManager.OnAnyWindowPathChanged += OnPathChanged;

        // �������������¼���������
        GameEvents.OnClueUnlocked += OnClueUnlocked;
    }

    void OnDestroy()
    {
        ExplorerManager.OnAnyWindowPathChanged -= OnPathChanged;
        GameEvents.OnClueUnlocked -= OnClueUnlocked;
    }

    /// <summary>
    /// ·���仯�¼�����
    /// </summary>
    private void OnPathChanged(ExplorerManager explorerInstance, string newPath)
    {
        LogDebug($"·���仯: {newPath}");

        // ׷�ٵ�ǰ��Explorerʵ��
        if (newPath == targetPathId)
        {
            currentExplorer = explorerInstance;
            LogDebug($"����Ŀ��·��: {targetPathId}");
        }
        else if (currentExplorer == explorerInstance)
        {
            // �뿪Ŀ��·��
            currentExplorer = null;
            LogDebug($"�뿪Ŀ��·��");
        }

        // ����Ƿ���Ŀ��·��
        if (newPath != targetPathId) return;

        if (gameFlowController == null) return;

        // ������״̬��������Ӧ�Ի�
        bool isUnlocked = gameFlowController.HasClue(unlockClueId);

        if (isUnlocked)
        {
            TriggerUnlockedDialogue();
        }
        else
        {
            TriggerFirstDialogue();
        }
    }

    /// <summary>
    /// ���������¼�����������
    /// </summary>
    private void OnClueUnlocked(string clueId)
    {
        // ֻ����works�����¼�
        if (clueId != unlockClueId) return;

        LogDebug($"��⵽��������: {clueId}");

        // ��鵱ǰ�Ƿ���Ŀ��·��
        if (currentExplorer != null)
        {
            LogDebug($"��ǰ��Ŀ��·�������Դ���������Ի�");
            TriggerUnlockedDialogue();
        }
        else
        {
            LogDebug($"��ǰ����Ŀ��·�����ȴ�����ʱ����");
        }
    }

    /// <summary>
    /// �����״ζԻ���δ����״̬��
    /// </summary>
    private void TriggerFirstDialogue()
    {
        if (hasTriggeredFirst)
        {
            LogDebug("�״ζԻ��Ѵ�����");
            return;
        }

        if (gameFlowController.GetCompletedBlocksSafe().Contains(firstDialogueBlockId))
        {
            LogDebug($"�Ի��� {firstDialogueBlockId} �����");
            hasTriggeredFirst = true;
            return;
        }

        LogDebug($"�����״ζԻ���: {firstDialogueBlockId}");
        gameFlowController.StartDialogueBlock(firstDialogueBlockId);
        hasTriggeredFirst = true;
    }

    /// <summary>
    /// ����������Ի�
    /// </summary>
    private void TriggerUnlockedDialogue()
    {
        if (hasTriggeredUnlocked)
        {
            LogDebug("������Ի��Ѵ�����");
            return;
        }

        if (gameFlowController.GetCompletedBlocksSafe().Contains(unlockedDialogueBlockId))
        {
            LogDebug($"�Ի��� {unlockedDialogueBlockId} �����");
            hasTriggeredUnlocked = true;
            return;
        }

        LogDebug($"����������Ի���: {unlockedDialogueBlockId}");
        gameFlowController.StartDialogueBlock(unlockedDialogueBlockId);
        hasTriggeredUnlocked = true;
    }

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage2Files] {message}");
        }
    }
    #endregion
}