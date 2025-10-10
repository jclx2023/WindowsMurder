using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// �浵������ - ������Ϸ���ȵı���ͼ���
/// </summary>
public class SaveManager : MonoBehaviour
{
    [Header("=== ���� ===")]
    [SerializeField] private string saveKey = "WindowsMurder_SaveData";
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool autoSaveEnabled = true;

    [Header("=== ��ǰ�浵���� ===")]
    [SerializeField] private SaveData currentSaveData;

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = true;

    // ����
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SaveManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    instance = go.AddComponent<SaveManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    // �������
    private GameFlowController gameFlowController;
    private DialogueManager dialogueManager;

    #region ��ʼ��

    void Awake()
    {
        // ȷ������
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // ��ʼ���浵����
        if (currentSaveData == null)
        {
            currentSaveData = new SaveData();
        }
    }

    void Start()
    {
        // ���ĳ��������¼�
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // �����¼�����
        if (gameFlowController != null)
        {
            gameFlowController.OnAutoSaveRequested.RemoveListener(AutoSave);
        }
    }

    /// <summary>
    /// ����������ɻص�
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogDebug($"�����������: {scene.name}");

        if (scene.name == gameSceneName)
        {
            // ������Ϸ�����е����
            FindGameComponents();

            // ���ݽ��뷽ʽ�����Ƿ�ָ��浵
            HandleGameSceneLoaded();
        }
        else if (scene.name == mainMenuSceneName)
        {
            // ������Ϸ�������
            ClearGameComponents();
        }
    }

    /// <summary>
    /// ������Ϸ��������
    /// </summary>
    private void HandleGameSceneLoaded()
    {
        // �����ͨ��ʲô��ʽ������Ϸ������
        if (GlobalActionManager.Instance != null)
        {
            if (GlobalActionManager.Instance.IsContinueGame())
            {
                LogDebug("��⵽������Ϸģʽ��׼���ָ��浵");
                if (currentSaveData != null && !string.IsNullOrEmpty(currentSaveData.stageId))
                {
                    RestoreGameState();
                }
                else
                {
                    LogError("�浵������Ч���޷��ָ���Ϸ״̬");
                }
            }
            else if (GlobalActionManager.Instance.IsNewGame())
            {
                // ����Ϸ - ����Ҫ�ָ�
                LogDebug("��⵽����Ϸģʽ�����ָ��浵");
                currentSaveData = new SaveData();
            }
        }
    }

    /// <summary>
    /// ������Ϸ���
    /// </summary>
    private void FindGameComponents()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (gameFlowController == null)
        {
            LogError("δ�ҵ�GameFlowController��");
        }
        else
        {
            // �����Զ��浵�¼�
            if (autoSaveEnabled)
            {
                gameFlowController.OnAutoSaveRequested.RemoveListener(AutoSave);
                gameFlowController.OnAutoSaveRequested.AddListener(AutoSave);
            }

            LogDebug("������GameFlowController");
        }

        if (dialogueManager == null)
        {
            LogWarning("δ�ҵ�DialogueManager");
        }
    }

    /// <summary>
    /// ������Ϸ�������
    /// </summary>
    private void ClearGameComponents()
    {
        if (gameFlowController != null)
        {
            gameFlowController.OnAutoSaveRequested.RemoveListener(AutoSave);
        }

        gameFlowController = null;
        dialogueManager = null;

        LogDebug("��������Ϸ�������");
    }

    #endregion

    #region �浵����

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public void SaveGame()
    {
        if (gameFlowController == null)
        {
            LogWarning("�޷����棺GameFlowControllerΪ��");
            return;
        }

        // ��ȡ��Ϸ����״̬
        GameFlowState flowState = gameFlowController.GetCurrentState();

        // ���´浵����
        currentSaveData.saveId = "Slot1";
        currentSaveData.stageId = flowState.currentStageId;
        currentSaveData.dialogueBlockId = flowState.currentDialogueBlockId;
        currentSaveData.cluesUnlocked = flowState.unlockedClues;
        currentSaveData.completedDialogueBlocks = flowState.completedDialogueBlocks;
        currentSaveData.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentSaveData.playTimeSeconds = Time.time;

        // ���л�������
        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        LogDebug($"��Ϸ�ѱ��� - Stage: {currentSaveData.stageId}, ������: {currentSaveData.cluesUnlocked.Count}");
    }

    /// <summary>
    /// ������Ϸ�浵���ݵ��ڴ�
    /// </summary>
    public bool LoadGame()
    {
        if (!HasSaveData())
        {
            LogDebug("û���ҵ��浵����");
            return false;
        }

        try
        {
            string json = PlayerPrefs.GetString(saveKey);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);


            LogDebug($"�浵���سɹ� - Stage: {currentSaveData.stageId}, ����ʱ��: {currentSaveData.createdAt}");
            return true;
        }
        catch (Exception e)
        {
            LogError($"���ش浵ʧ��: {e.Message}");
            currentSaveData = new SaveData();
            return false;
        }
    }

    /// <summary>
    /// �Զ�����
    /// </summary>
    private void AutoSave()
    {
        if (!autoSaveEnabled) return;

        SaveGame();
        LogDebug("�Զ��������");
    }

    /// <summary>
    /// ɾ���浵
    /// </summary>
    public void DeleteSave()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
        }

        currentSaveData = new SaveData();

        LogDebug("�浵��ɾ��");
    }

    /// <summary>
    /// ����Ƿ��д浵
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(saveKey);
    }

    #endregion

    #region ��Ϸ״̬�ָ�

    /// <summary>
    /// �ָ���Ϸ״̬��ͬ��������
    /// </summary>
    private void RestoreGameState()
    {

        LogDebug("��ʼ�ָ���Ϸ״̬...");

        // ������Ϸ����״̬
        GameFlowState flowState = new GameFlowState
        {
            currentStageId = currentSaveData.stageId,
            currentDialogueBlockId = currentSaveData.dialogueBlockId,
            unlockedClues = currentSaveData.cluesUnlocked ?? new List<string>(),
            completedDialogueBlocks = currentSaveData.completedDialogueBlocks ?? new List<string>()
        };

        gameFlowController.RestoreState(flowState, isFromSave: true);

        LogDebug($"��Ϸ״̬�ָ���� - Stage: {currentSaveData.stageId}, ����: {flowState.unlockedClues.Count}��");
    }

    #endregion

    #region ��������

    /// <summary>
    /// ��ȡStage��ʾ����
    /// </summary>
    private string GetStageName(string stageId)
    {
        switch (stageId)
        {
            case "Stage01_Desktop":
                return "��һĻ������";
            case "Stage02_WorkFolder":
                return "�ڶ�Ļ�������ֳ�";
            case "Stage03_Investigation":
                return "����Ļ������ȡ֤";
            case "Stage04_Interrogation":
                return "����Ļ������";
            case "Stage05_Deduction":
                return "����Ļ������";
            default:
                return string.IsNullOrEmpty(stageId) ? "δ��ʼ" : stageId;
        }
    }

    /// <summary>
    /// ��ʽ����Ϸʱ��
    /// </summary>
    private string FormatPlayTime(float seconds)
    {
        if (seconds <= 0) return "0��";

        int hours = (int)(seconds / 3600);
        int minutes = (int)((seconds % 3600) / 60);
        int secs = (int)(seconds % 60);

        if (hours > 0)
        {
            return $"{hours}Сʱ{minutes}����";
        }
        else if (minutes > 0)
        {
            return $"{minutes}����{secs}��";
        }
        else
        {
            return $"{secs}��";
        }
    }

    /// <summary>
    /// ������Ϸ���Ȱٷֱ�
    /// </summary>
    private int CalculateProgress(SaveData data = null)
    {
        if (data == null) data = currentSaveData;
        if (data == null) return 0;

        // ����Stage�����������
        int baseProgress = 0;
        switch (data.stageId)
        {
            case "Stage01_Desktop": baseProgress = 20; break;
            case "Stage02_WorkFolder": baseProgress = 40; break;
            case "Stage03_Investigation": baseProgress = 60; break;
            case "Stage04_Interrogation": baseProgress = 80; break;
            case "Stage05_Deduction": baseProgress = 90; break;
            default: baseProgress = 0; break;
        }

        // ������������΢���������ܹ���Ҫ10��������
        int clueBonus = Math.Min((data.cluesUnlocked?.Count ?? 0) * 1, 10);

        return Math.Min(baseProgress + clueBonus, 100);
    }

    #endregion

    #region ����

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SaveManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SaveManager] {message}");
    }

    #endregion
}

/// <summary>
/// �浵���ݽṹ
/// </summary>
[System.Serializable]
public class SaveData
{
    public string saveId = "Slot1";
    public string stageId = "";
    public string dialogueBlockId = "";
    public List<string> cluesUnlocked = new List<string>();
    public List<string> completedDialogueBlocks = new List<string>();
    public string createdAt = "";
    public float playTimeSeconds = 0f;
}