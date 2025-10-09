using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ����ת������ - ���ڿ�Stage���ݴ���λ����Ϣ
/// </summary>
[System.Serializable]
public struct WindowTransitionData
{
    public Vector2 windowPosition;

    public WindowTransitionData(Vector2 position)
    {
        windowPosition = position;
    }
}

/// <summary>
/// ��Ϸ���̿����� - ����Stage�л��������жϺͽ����ƽ�
/// </summary>
public class GameFlowController : MonoBehaviour
{
    [Header("=== Stage���� ===")]
    [SerializeField] private List<StageConfig> stageConfigs = new List<StageConfig>();
    [SerializeField] private string startingStageId = "Stage01_Desktop";

    [Header("=== ��ǰ״̬ ===")]
    [SerializeField] private string currentStageId;
    [SerializeField] private string currentDialogueFile;
    [SerializeField] private string currentDialogueBlockId;
    [SerializeField] private List<string> unlockedClues = new List<string>();
    [SerializeField] private List<string> completedDialogueBlocks = new List<string>();

    [Header("=== ������� ===")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Transform stageContainer; // Canvas�µ�Stage����

    [Header("=== �¼� ===")]
    public UnityEvent<string> OnStageChanged;
    public UnityEvent<string> OnClueUnlocked;
    public UnityEvent OnAutoSaveRequested;

    [Header("=== ���������� ===")]
    [SerializeField] private bool useMultiLanguageScripts = true; // �Ƿ����ö����Խű�

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = true;

    // ˽�б���
    private Dictionary<string, StageConfig> stageConfigDict;
    private Dictionary<string, GameObject> stageObjectDict;
    private StageConfig currentStage;

    // ������������ת�����ݻ���
    private WindowTransitionData? cachedWindowTransition;

    #region ��ʼ��

    void Awake()
    {
        InitializeConfigs();
        CacheStageObjects();
    }

    void OnEnable()
    {
        // ���ľ�̬�¼�
        GameEvents.OnClueUnlockRequested += HandleClueUnlockRequest;
        GameEvents.OnDialogueBlockRequested += HandleDialogueBlockRequest;
        GameEvents.OnStageChangeRequested += HandleStageChangeRequest;

        LogDebug("�Ѷ�����Ϸ�¼�");
    }

    void OnDisable()
    {
        // ȡ�����ģ���ֹ�ڴ�й©
        GameEvents.OnClueUnlockRequested -= HandleClueUnlockRequest;
        GameEvents.OnDialogueBlockRequested -= HandleDialogueBlockRequest;
        GameEvents.OnStageChangeRequested -= HandleStageChangeRequest;

        LogDebug("��ȡ��������Ϸ�¼�");
    }

    void Start()
    {
        // ����DialogueManager
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        // ���û�дӴ浵���أ������ʼStage��ʼ
        if (string.IsNullOrEmpty(currentStageId))
        {
            LoadStage(startingStageId);
        }
    }

    /// <summary>
    /// ��ʼ�������ֵ�
    /// </summary>
    private void InitializeConfigs()
    {
        stageConfigDict = new Dictionary<string, StageConfig>();

        foreach (var config in stageConfigs)
        {
            if (config != null && !string.IsNullOrEmpty(config.stageId))
            {
                stageConfigDict[config.stageId] = config;

                // ��ʼ���Ի��������ֵ�
                config.InitializeDictionary();
            }
        }

        LogDebug($"��ʼ���� {stageConfigDict.Count} ��Stage����");
    }

    /// <summary>
    /// ����Stage GameObject
    /// </summary>
    private void CacheStageObjects()
    {
        stageObjectDict = new Dictionary<string, GameObject>();

        if (stageContainer == null)
        {
            // �����Զ�����
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                stageContainer = canvas.transform;
            }
            else
            {
                LogError("δ�ҵ�Stage������");
                return;
            }
        }

        // ���������Ӷ��󣬻���Stage����
        foreach (Transform child in stageContainer)
        {
            // ����Stage��������������� "Stage01_Desktop" �����ĸ�ʽ
            if (child.name.StartsWith("Stage"))
            {
                stageObjectDict[child.name] = child.gameObject;
                LogDebug($"����Stage����: {child.name}");
            }
        }
    }

    #endregion

    #region �¼�������

    /// <summary>
    /// ����������������
    /// </summary>
    private void HandleClueUnlockRequest(string clueId)
    {
        UnlockClue(clueId);
    }

    /// <summary>
    /// ����Ի�������
    /// </summary>
    private void HandleDialogueBlockRequest(string blockId)
    {
        StartDialogueBlock(blockId);
    }

    /// <summary>
    /// ����Stage�л�����
    /// </summary>
    private void HandleStageChangeRequest(string stageId)
    {
        LoadStage(stageId);
    }

    #endregion

    #region Stage����

    /// <summary>
    /// ����ָ��Stage
    /// </summary>
    public void LoadStage(string stageId)
    {
        LogDebug($"����Stage: {stageId}");

        // ��������Stage
        foreach (var kvp in stageObjectDict)
        {
            kvp.Value.SetActive(false);
        }

        // ��ʾĿ��Stage
        stageObjectDict[stageId].SetActive(true);

        // ���µ�ǰ״̬
        currentStageId = stageId;
        currentStage = stageConfigDict[stageId];

        // ��ʼ��Stage�ڵĿɽ�������
        InitializeStageInteractables();

        // ����Unity�¼�
        OnStageChanged?.Invoke(stageId);

        LogDebug($"Stage {stageId} �������");
    }

    /// <summary>
    /// ��ʼ��Stage�ڵĿɽ�������
    /// </summary>
    private void InitializeStageInteractables()
    {
        if (currentStage == null) return;

        GameObject stageObj = stageObjectDict[currentStageId];

        // �������жԻ�������
        foreach (var dialogueBlock in currentStage.dialogueBlocks)
        {
            // ����й�����GameObject���������������伤��״̬
            if (!string.IsNullOrEmpty(dialogueBlock.interactableObjectName))
            {
                Transform interactable = stageObj.transform.Find(dialogueBlock.interactableObjectName);
                if (interactable != null)
                {
                    // ����Ƿ������������
                    bool canInteract = CheckDialogueBlockCondition(dialogueBlock);
                    interactable.gameObject.SetActive(canInteract);

                    LogDebug($"�ɽ������� {dialogueBlock.interactableObjectName} ״̬: {canInteract}");
                }
            }
        }
    }

    /// <summary>
    /// ���Խ�����һ��Stage
    /// </summary>
    public void TryProgressToNextStage()
    {
        if (currentStage == null) return;

        // ����Ƿ����������һStage������
        if (CheckStageProgressCondition())
        {
            if (!string.IsNullOrEmpty(currentStage.nextStageId))
            {
                LogDebug($"����������������һStage: {currentStage.nextStageId}");
                LoadStage(currentStage.nextStageId);
            }
            else
            {
                LogDebug("�ѵ�������Stage");
            }
        }
        else
        {
            LogDebug("�����������һStage������");
        }
    }

    #endregion

    #region �Ի������

    /// <summary>
    /// ��ʼ�Ի��飨ʹ��dialogueBlockFileId��Ϊ��ʶ��
    /// </summary>
    public void StartDialogueBlock(string dialogueBlockFileId)
    {
        // ���ҶԻ�������
        var dialogueBlock = currentStage.GetDialogueBlock(dialogueBlockFileId);
        if (dialogueBlock == null)
        {
            LogError($"�Ҳ����Ի�������: {dialogueBlockFileId}");
            return;
        }

        // �������
        if (!CheckDialogueBlockCondition(dialogueBlock))
        {
            LogDebug($"�Ի��� {dialogueBlockFileId} ����������");
            return;
        }

        // ���ݵ�ǰ�������ù����ļ���
        string fileName = GetCurrentScriptFileName();

        currentDialogueFile = fileName;
        currentDialogueBlockId = dialogueBlockFileId;
        dialogueManager.StartDialogue(dialogueBlock.dialogueBlockFileId);
        LogDebug($"��ʼ�Ի���: {dialogueBlockFileId} -> �ļ�: {fileName}");
    }

    /// <summary>
    /// ���ݵ�ǰ�������û�ȡ�ű��ļ���
    /// </summary>
    private string GetCurrentScriptFileName()
    {
        string fileName = "zh"; // Ĭ������

        // �� LanguageManager ��ȡ��ǰ����
        if (LanguageManager.Instance != null)
        {
            switch (LanguageManager.Instance.currentLanguage)
            {
                case SupportedLanguage.Chinese:
                    fileName = "zh";
                    break;
                case SupportedLanguage.English:
                    fileName = "en";
                    break;
                case SupportedLanguage.Japanese:
                    fileName = "jp";
                    break;
                default:
                    fileName = "zh";
                    break;
            }
        }

        return fileName;
    }

    /// <summary>
    /// �Ի������ʱ����
    /// </summary>
    public void OnDialogueBlockComplete(string dialogueBlockFileId)
    {
        if (string.IsNullOrEmpty(dialogueBlockFileId)) return;

        LogDebug($"�Ի������: {dialogueBlockFileId}");

        // ���Ϊ�����
        if (!completedDialogueBlocks.Contains(dialogueBlockFileId))
        {
            completedDialogueBlocks.Add(dialogueBlockFileId);
        }

        // ������Ӧ����
        if (currentStage != null)
        {
            var dialogueBlock = currentStage.GetDialogueBlock(dialogueBlockFileId);
            if (dialogueBlock != null && dialogueBlock.unlocksClues != null)
            {
                foreach (string clue in dialogueBlock.unlocksClues)
                {
                    UnlockClue(clue);
                }
            }
        }

        // ��յ�ǰ�Ի�״̬
        currentDialogueFile = null;
        currentDialogueBlockId = null;

        // ���¿ɽ�������״̬
        InitializeStageInteractables();

        // �����Զ��浵
        OnAutoSaveRequested?.Invoke();
        GameEvents.NotifyDialogueBlockCompleted(dialogueBlockFileId);
    }

    /// <summary>
    /// ���Ի�������
    /// </summary>
    private bool CheckDialogueBlockCondition(DialogueBlockConfig dialogueBlock)
    {
        if (dialogueBlock == null) return false;

        // �����������
        if (dialogueBlock.requiredClues != null && dialogueBlock.requiredClues.Count > 0)
        {
            foreach (string clue in dialogueBlock.requiredClues)
            {
                if (!unlockedClues.Contains(clue))
                {
                    return false;
                }
            }
        }

        // ������ĶԻ���
        if (dialogueBlock.requiredDialogueBlocks != null && dialogueBlock.requiredDialogueBlocks.Count > 0)
        {
            foreach (string blockId in dialogueBlock.requiredDialogueBlocks)
            {
                if (!completedDialogueBlocks.Contains(blockId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region ��������

    /// <summary>
    /// ��������
    /// </summary>
    public void UnlockClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId)) return;

        if (!unlockedClues.Contains(clueId))
        {
            unlockedClues.Add(clueId);
            LogDebug($"��������: {clueId}");

            // ����Unity�¼�
            OnClueUnlocked?.Invoke(clueId);

            // ������̬�¼�
            GameEvents.NotifyClueUnlocked(clueId);

            // ���¿ɽ�������״̬
            InitializeStageInteractables();
        }
    }

    /// <summary>
    /// ����Ƿ�ӵ������
    /// </summary>
    public bool HasClue(string clueId)
    {
        return unlockedClues.Contains(clueId);
    }

    #endregion

    #region �������
    /// <summary>
    /// ��鵱ǰStage�Ƿ�������������������ӿڣ�
    /// </summary>
    public bool IsStageProgressConditionMet()
    {
        return CheckStageProgressCondition();
    }
    /// <summary>
    /// ���Stage��������
    /// </summary>
    private bool CheckStageProgressCondition()
    {
        if (currentStage == null) return false;

        // �����������
        if (currentStage.requiredCluesForProgress != null && currentStage.requiredCluesForProgress.Count > 0)
        {
            foreach (string clue in currentStage.requiredCluesForProgress)
            {
                if (!unlockedClues.Contains(clue))
                {
                    return false;
                }
            }
        }

        // ��������ɵĶԻ���
        if (currentStage.requiredDialogueBlocksForProgress != null && currentStage.requiredDialogueBlocksForProgress.Count > 0)
        {
            foreach (string blockId in currentStage.requiredDialogueBlocksForProgress)
            {
                if (!completedDialogueBlocks.Contains(blockId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    #endregion

    #region �����ӿ�
    public void UnlockClueDelayed(string clueId, float delaySeconds)
    {
        LogDebug($"�յ��ӳٽ������� - ����: {clueId}, �ӳ�: {delaySeconds}��");
        StartCoroutine(DelayedUnlockCoroutine(clueId, delaySeconds));
    }

    private System.Collections.IEnumerator DelayedUnlockCoroutine(string clueId, float delaySeconds)
    {
        // �ȴ�ָ��ʱ��
        yield return new WaitForSeconds(delaySeconds);

        // ִ�н���
        UnlockClue(clueId);

        LogDebug($"�ӳٽ������ - ����: {clueId}");
    }
    public IReadOnlyList<StageConfig> GetStageConfigsSafe()
    {
        return stageConfigs.AsReadOnly();
    }

    public string GetCurrentStageIdSafe()
    {
        return currentStageId;
    }

    public IReadOnlyList<string> GetCompletedBlocksSafe()
    {
        return completedDialogueBlocks.AsReadOnly();
    }

    public void CacheWindowTransition(WindowTransitionData data)
    {
        cachedWindowTransition = data;
        LogDebug($"���洰��ת������ - λ��: {data.windowPosition}");
    }

    public WindowTransitionData? ConsumeWindowTransition()
    {
        var data = cachedWindowTransition;
        cachedWindowTransition = null; // ���Ѻ����

        if (data.HasValue)
        {
            LogDebug($"���Ѵ���ת������ - λ��: {data.Value.windowPosition}");
        }
        else
        {
            LogDebug("�޻���Ĵ���ת������");
        }

        return data;
    }

    #endregion

    #region �浵֧��

    /// <summary>
    /// ��ȡ��ǰ��Ϸ״̬�����ڴ浵��
    /// </summary>
    public GameFlowState GetCurrentState()
    {
        return new GameFlowState
        {
            currentStageId = currentStageId,
            currentDialogueFile = currentDialogueFile,
            currentDialogueBlockId = currentDialogueBlockId,
            unlockedClues = new List<string>(unlockedClues),
            completedDialogueBlocks = new List<string>(completedDialogueBlocks)
        };
    }

    /// <summary>
    /// �ָ���Ϸ״̬���Ӵ浵���أ�
    /// </summary>
    public void RestoreState(GameFlowState state)
    {
        if (state == null) return;

        LogDebug("�ָ���Ϸ����״̬");

        // �ָ�����
        unlockedClues = new List<string>(state.unlockedClues ?? new List<string>());
        completedDialogueBlocks = new List<string>(state.completedDialogueBlocks ?? new List<string>());

        // ����Stage
        if (!string.IsNullOrEmpty(state.currentStageId))
        {
            LoadStage(state.currentStageId);
        }

        // �����δ��ɵĶԻ��飬�ָ���
        if (!string.IsNullOrEmpty(state.currentDialogueBlockId))
        {
            currentDialogueFile = state.currentDialogueFile;
            currentDialogueBlockId = state.currentDialogueBlockId;

            LogDebug($"�ָ�δ��ɵĶԻ�״̬: {currentDialogueFile}:{currentDialogueBlockId}");
        }
    }

    #endregion

    #region ���Թ���

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[GameFlow] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[GameFlow] {message}");
    }

    #endregion
}

/// <summary>
/// Stage����
/// </summary>
[System.Serializable]
public class StageConfig
{
    [Header("������Ϣ")]
    public string stageId = "Stage01_Desktop";

    [Header("�Ի�������")]
    public List<DialogueBlockConfig> dialogueBlocks = new List<DialogueBlockConfig>();

    [Header("��������")]
    [Tooltip("������һStage��Ҫ������")]
    public List<string> requiredCluesForProgress = new List<string>();

    [Tooltip("������һStage��Ҫ��ɵĶԻ���")]
    public List<string> requiredDialogueBlocksForProgress = new List<string>();

    [Header("���̿���")]
    public string nextStageId = "";

    // �ڲ�ʹ�õ��ֵ�
    private Dictionary<string, DialogueBlockConfig> dialogueBlockDict;

    /// <summary>
    /// ��ʼ���ڲ��ֵ�
    /// </summary>
    public void InitializeDictionary()
    {
        dialogueBlockDict = new Dictionary<string, DialogueBlockConfig>();
        foreach (var block in dialogueBlocks)
        {
            if (!string.IsNullOrEmpty(block.dialogueBlockFileId))
            {
                dialogueBlockDict[block.dialogueBlockFileId] = block;
            }
        }
    }

    /// <summary>
    /// ��ȡ�Ի�������
    /// </summary>
    public DialogueBlockConfig GetDialogueBlock(string dialogueBlockFileId)
    {
        if (dialogueBlockDict == null)
        {
            InitializeDictionary();
        }

        if (dialogueBlockDict.ContainsKey(dialogueBlockFileId))
        {
            return dialogueBlockDict[dialogueBlockFileId];
        }

        return null;
    }
}

/// <summary>
/// �Ի�������
/// </summary>
[System.Serializable]
public class DialogueBlockConfig
{
    [Header("�籾����")]
    [Tooltip("�籾�ļ��еĶԻ���ID")]
    public string dialogueBlockFileId = "001";

    [Header("��������")]
    [Tooltip("�����Ŀɽ����������ƣ���ѡ��")]
    public string interactableObjectName = "";

    [Header("��������")]
    [Tooltip("��Ҫ������")]
    public List<string> requiredClues = new List<string>();

    [Tooltip("��Ҫ��ɵ������Ի��飨ʹ��dialogueBlockFileId��")]
    public List<string> requiredDialogueBlocks = new List<string>();

    [Header("��ɺ����")]
    [Tooltip("��ɺ����������")]
    public List<string> unlocksClues = new List<string>();
}

/// <summary>
/// ��Ϸ����״̬�����ڴ浵��
/// </summary>
[System.Serializable]
public class GameFlowState
{
    public string currentStageId;
    public string currentDialogueFile;
    public string currentDialogueBlockId;
    public List<string> unlockedClues;
    public List<string> completedDialogueBlocks;
}