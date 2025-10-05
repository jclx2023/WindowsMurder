using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stage3���̿����� - ����Stage3�����������߼�
/// </summary>
public class Stage3Controller : MonoBehaviour
{
    [Header("�������")]
    [SerializeField] private GameFlowController flowController;

    [Header("����Icons")]
    [SerializeField] private List<GameObject> desktopIcons = new List<GameObject>();  // �����ϵ�����ͼ��

    [Header("��������")]
    [SerializeField] private string targetPathId = "DFilesWorks";  // Ŀ�굼��·��
    [SerializeField] private string dialogueBlockId = "004";       // Ҫ�����ĶԻ���

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // ˽�б���
    private ExplorerManager explorerManager;
    private GameObject ieIconInExplorer;  // ����ʱ��ȡ
    private List<GameObject> explorerIcons;  // ����ʱ��ȡ
    private bool flowStarted = false;

    #region Unity��������

    void Awake()
    {
        // ����GameFlowController
        if (flowController == null)
        {
            flowController = FindObjectOfType<GameFlowController>();
            if (flowController == null)
            {
                LogError("δ�ҵ�GameFlowController��");
            }
        }
    }

    void Start()
    {
        // ���ĶԻ����¼�
        DialogueUI.OnLineStarted += OnDialogueLineStarted;
        LogDebug("�Ѷ��ĶԻ����¼�");
    }

    void OnDestroy()
    {
        // ȡ���¼�����
        DialogueUI.OnLineStarted -= OnDialogueLineStarted;
        LogDebug("��ȡ�����ĶԻ����¼�");
    }

    #endregion

    #region �����ӿ�

    /// <summary>
    /// ����Explorer���ã���Initializer���ã�
    /// </summary>
    public void SetExplorerReference(ExplorerManager explorer)
    {
        explorerManager = explorer;
        LogDebug("���յ�Explorer����");

        // ��ȡExplorer�е�icons����
        if (!GetExplorerIcons())
        {
            LogError("�޷���ȡExplorer�е�icons���ã�");
            return;
        }

        // ��ʼStage3����
        if (!flowStarted)
        {
            StartCoroutine(StartStage3Flow());
            flowStarted = true;
        }
    }

    #endregion

    #region Explorer Icons��ȡ

    /// <summary>
    /// ��ExplorerIconGetter��ȡicons����
    /// </summary>
    private bool GetExplorerIcons()
    {

        // ����ExplorerIconGetter���
        ExplorerIconGetter iconGetter = explorerManager.GetComponent<ExplorerIconGetter>();
        if (iconGetter == null)
        {
            LogError("ExplorerԤ������ȱ��ExplorerIconGetter�����");
            return false;
        }

        // ��ȡicons����
        ieIconInExplorer = iconGetter.GetIEIcon();
        explorerIcons = iconGetter.GetProgramIcons();

        LogDebug($"�ɹ���ȡExplorer icons: IE + {explorerIcons.Count}������ͼ��");
        return true;
    }

    #endregion

    #region Stage3����

    /// <summary>
    /// ����Stage3����
    /// </summary>
    private IEnumerator StartStage3Flow()
    {
        LogDebug("��ʼStage3����");

        // �ȴ�2֡��ȷ��ExplorerManager��ɳ�ʼ��
        yield return null;
        yield return null;

        // ������Works�ļ���
        bool navigationSuccess = NavigateToWorksFolder();

        if (navigationSuccess)
        {
            LogDebug("�����ɹ��������Ի���");
            // �����Ի���004
            flowController.StartDialogueBlock(dialogueBlockId);
        }
        else
        {
            LogError("����ʧ�ܣ��޷������Ի���");
        }
    }

    /// <summary>
    /// ������Works�ļ���
    /// </summary>
    private bool NavigateToWorksFolder()
    {
        if (explorerManager == null)
        {
            LogError("ExplorerManager����Ϊ�գ��޷�����");
            return false;
        }

        LogDebug($"���Ե�����: {targetPathId}");
        bool success = explorerManager.NavigateToPath(targetPathId);

        if (success)
        {
            LogDebug($"�ɹ�������: {targetPathId}");
        }
        else
        {
            LogError($"������ {targetPathId} ʧ��");
        }

        return success;
    }

    #endregion

    #region �Ի��¼�����

    /// <summary>
    /// �Ի��п�ʼ�¼�����
    /// </summary>
    private void OnDialogueLineStarted(string lineId, string characterId, string blockId, bool isPresetMode)
    {
        // ֻ����Ի���004���¼�
        if (blockId != dialogueBlockId)
        {
            return;
        }

        LogDebug($"��⵽�Ի���004�ĶԻ���: lineId={lineId}");

        // lineId == 11: IE����
        if (lineId == "11")
        {
            ShowIEIcon();
        }
        // lineId == 13: ���򷵻�����
        else if (lineId == "13")
        {
            ProgramsReturnToDesktop();
        }
    }

    /// <summary>
    /// ��ʾIEͼ��
    /// </summary>
    private void ShowIEIcon()
    {
        if (ieIconInExplorer != null)
        {
            ieIconInExplorer.SetActive(true);
            LogDebug("IEͼ������ʾ");
        }
        else
        {
            LogError("IEͼ������Ϊ�գ�");
        }
    }

    /// <summary>
    /// ���򷵻�����
    /// </summary>
    private void ProgramsReturnToDesktop()
    {
        LogDebug("���򷵻����棺����Explorer icons����ʾ����icons");

        // ����Explorer�е����г���icons
        HideExplorerIcons();

        // ��ʾ�����ϵ�����icons
        ShowDesktopIcons();
    }

    /// <summary>
    /// ����Explorer�е����г���icons
    /// </summary>
    private void HideExplorerIcons()
    {
        // ����IE
        if (ieIconInExplorer != null)
        {
            ieIconInExplorer.SetActive(false);
        }

        // ������������
        if (explorerIcons != null)
        {
            foreach (var icon in explorerIcons)
            {
                if (icon != null)
                {
                    icon.SetActive(false);
                }
            }
        }

        LogDebug($"������Explorer�е� {(explorerIcons != null ? explorerIcons.Count + 1 : 1)} ��ͼ��");
    }

    /// <summary>
    /// ��ʾ�����ϵ�����icons
    /// </summary>
    private void ShowDesktopIcons()
    {
        if (desktopIcons != null)
        {
            foreach (var icon in desktopIcons)
            {
                if (icon != null)
                {
                    icon.SetActive(true);
                }
            }

            LogDebug($"����ʾ�����ϵ� {desktopIcons.Count} ��ͼ��");
        }
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[Stage3Controller] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[Stage3Controller] {message}");
    }

    #endregion
}