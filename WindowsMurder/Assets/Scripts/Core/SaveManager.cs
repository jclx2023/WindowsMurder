using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 存档管理器 - 负责游戏进度的保存和加载
/// </summary>
public class SaveManager : MonoBehaviour
{
    [Header("=== 配置 ===")]
    [SerializeField] private string saveKey = "WindowsMurder_SaveData";
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool autoSaveEnabled = true;

    [Header("=== 当前存档数据 ===")]
    [SerializeField] private SaveData currentSaveData;

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = true;

    // 单例
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

    // 组件引用
    private GameFlowController gameFlowController;
    private DialogueManager dialogueManager;

    #region 初始化

    void Awake()
    {
        // 确保单例
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 初始化存档数据
        if (currentSaveData == null)
        {
            currentSaveData = new SaveData();
        }
    }

    void Start()
    {
        // 订阅场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 清理事件订阅
        if (gameFlowController != null)
        {
            gameFlowController.OnAutoSaveRequested.RemoveListener(AutoSave);
        }
    }

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogDebug($"场景加载完成: {scene.name}");

        if (scene.name == gameSceneName)
        {
            // 查找游戏场景中的组件
            FindGameComponents();

            // 根据进入方式决定是否恢复存档
            HandleGameSceneLoaded();
        }
        else if (scene.name == mainMenuSceneName)
        {
            // 清理游戏组件引用
            ClearGameComponents();
        }
    }

    /// <summary>
    /// 处理游戏场景加载
    /// </summary>
    private void HandleGameSceneLoaded()
    {
        // 检查是通过什么方式进入游戏场景的
        if (GlobalActionManager.Instance != null)
        {
            if (GlobalActionManager.Instance.IsContinueGame())
            {
                LogDebug("检测到继续游戏模式，准备恢复存档");
                if (currentSaveData != null && !string.IsNullOrEmpty(currentSaveData.stageId))
                {
                    RestoreGameState();
                }
                else
                {
                    LogError("存档数据无效，无法恢复游戏状态");
                }
            }
            else if (GlobalActionManager.Instance.IsNewGame())
            {
                // 新游戏 - 不需要恢复
                LogDebug("检测到新游戏模式，不恢复存档");
                currentSaveData = new SaveData();
            }
        }
    }

    /// <summary>
    /// 查找游戏组件
    /// </summary>
    private void FindGameComponents()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (gameFlowController == null)
        {
            LogError("未找到GameFlowController！");
        }
        else
        {
            // 订阅自动存档事件
            if (autoSaveEnabled)
            {
                gameFlowController.OnAutoSaveRequested.RemoveListener(AutoSave);
                gameFlowController.OnAutoSaveRequested.AddListener(AutoSave);
            }

            LogDebug("已连接GameFlowController");
        }

        if (dialogueManager == null)
        {
            LogWarning("未找到DialogueManager");
        }
    }

    /// <summary>
    /// 清理游戏组件引用
    /// </summary>
    private void ClearGameComponents()
    {
        if (gameFlowController != null)
        {
            gameFlowController.OnAutoSaveRequested.RemoveListener(AutoSave);
        }

        gameFlowController = null;
        dialogueManager = null;

        LogDebug("已清理游戏组件引用");
    }

    #endregion

    #region 存档操作

    /// <summary>
    /// 保存游戏
    /// </summary>
    public void SaveGame()
    {
        if (gameFlowController == null)
        {
            LogWarning("无法保存：GameFlowController为空");
            return;
        }

        // 获取游戏流程状态
        GameFlowState flowState = gameFlowController.GetCurrentState();

        // 更新存档数据
        currentSaveData.saveId = "Slot1";
        currentSaveData.stageId = flowState.currentStageId;
        currentSaveData.dialogueBlockId = flowState.currentDialogueBlockId;
        currentSaveData.cluesUnlocked = flowState.unlockedClues;
        currentSaveData.completedDialogueBlocks = flowState.completedDialogueBlocks;
        currentSaveData.createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        currentSaveData.playTimeSeconds = Time.time;

        // 序列化并保存
        string json = JsonUtility.ToJson(currentSaveData, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        LogDebug($"游戏已保存 - Stage: {currentSaveData.stageId}, 线索数: {currentSaveData.cluesUnlocked.Count}");
    }

    /// <summary>
    /// 加载游戏存档数据到内存
    /// </summary>
    public bool LoadGame()
    {
        if (!HasSaveData())
        {
            LogDebug("没有找到存档数据");
            return false;
        }

        try
        {
            string json = PlayerPrefs.GetString(saveKey);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);


            LogDebug($"存档加载成功 - Stage: {currentSaveData.stageId}, 保存时间: {currentSaveData.createdAt}");
            return true;
        }
        catch (Exception e)
        {
            LogError($"加载存档失败: {e.Message}");
            currentSaveData = new SaveData();
            return false;
        }
    }

    /// <summary>
    /// 自动保存
    /// </summary>
    private void AutoSave()
    {
        if (!autoSaveEnabled) return;

        SaveGame();
        LogDebug("自动保存完成");
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public void DeleteSave()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
        }

        currentSaveData = new SaveData();

        LogDebug("存档已删除");
    }

    /// <summary>
    /// 检查是否有存档
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.HasKey(saveKey);
    }

    #endregion

    #region 游戏状态恢复

    /// <summary>
    /// 恢复游戏状态（同步方法）
    /// </summary>
    private void RestoreGameState()
    {

        LogDebug("开始恢复游戏状态...");

        // 构建游戏流程状态
        GameFlowState flowState = new GameFlowState
        {
            currentStageId = currentSaveData.stageId,
            currentDialogueBlockId = currentSaveData.dialogueBlockId,
            unlockedClues = currentSaveData.cluesUnlocked ?? new List<string>(),
            completedDialogueBlocks = currentSaveData.completedDialogueBlocks ?? new List<string>()
        };

        gameFlowController.RestoreState(flowState, isFromSave: true);

        LogDebug($"游戏状态恢复完成 - Stage: {currentSaveData.stageId}, 线索: {flowState.unlockedClues.Count}个");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取Stage显示名称
    /// </summary>
    private string GetStageName(string stageId)
    {
        switch (stageId)
        {
            case "Stage01_Desktop":
                return "第一幕：桌面";
            case "Stage02_WorkFolder":
                return "第二幕：案发现场";
            case "Stage03_Investigation":
                return "第三幕：调查取证";
            case "Stage04_Interrogation":
                return "第四幕：审问";
            case "Stage05_Deduction":
                return "第五幕：推理";
            default:
                return string.IsNullOrEmpty(stageId) ? "未开始" : stageId;
        }
    }

    /// <summary>
    /// 格式化游戏时间
    /// </summary>
    private string FormatPlayTime(float seconds)
    {
        if (seconds <= 0) return "0秒";

        int hours = (int)(seconds / 3600);
        int minutes = (int)((seconds % 3600) / 60);
        int secs = (int)(seconds % 60);

        if (hours > 0)
        {
            return $"{hours}小时{minutes}分钟";
        }
        else if (minutes > 0)
        {
            return $"{minutes}分钟{secs}秒";
        }
        else
        {
            return $"{secs}秒";
        }
    }

    /// <summary>
    /// 计算游戏进度百分比
    /// </summary>
    private int CalculateProgress(SaveData data = null)
    {
        if (data == null) data = currentSaveData;
        if (data == null) return 0;

        // 根据Stage计算基础进度
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

        // 根据线索数量微调（假设总共需要10个线索）
        int clueBonus = Math.Min((data.cluesUnlocked?.Count ?? 0) * 1, 10);

        return Math.Min(baseProgress + clueBonus, 100);
    }

    #endregion

    #region 调试

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
/// 存档数据结构
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