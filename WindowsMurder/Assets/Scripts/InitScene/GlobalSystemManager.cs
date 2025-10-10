using UnityEngine;

/// <summary>
/// LLM引擎类型枚举
/// </summary>
public enum LLMProvider
{
    Gemini,
    GPT,
    DeepSeek
}

/// <summary>
/// 全局系统管理器 - 专注于底层系统服务
/// 提供音频、显示、语言、存档、LLM引擎等基础服务，不管理UI交互
/// </summary>
public class GlobalSystemManager : MonoBehaviour
{
    public static GlobalSystemManager Instance;

    [Header("音频设置")]
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    [Header("显示设置")]
    public bool isFullscreen = true;
    public Vector2Int resolution = new Vector2Int(1920, 1080);

    [Header("对话系统设置")]
    public float dialogueSpeed = 0.05f;

    [Header("LLM引擎设置")]
    public LLMProvider currentLLMProvider = LLMProvider.Gemini;

    [Header("语言系统设置")]
    public string csvFileName = "Localization/LocalizationTable.csv";
    public SupportedLanguage defaultLanguage = SupportedLanguage.Chinese;
    public bool enableLanguageDebug = true;

    [Header("游戏状态")]
    public bool hasGameSave = false;
    public string currentGameProgress = "";

    // 私有变量
    private AudioSource audioSource;

    // 系统就绪事件
    public static System.Action OnLanguageSystemReady;
    public static System.Action OnSystemInitialized;
    public static System.Action OnDialogueSettingsChanged;
    public static System.Action<LLMProvider> OnLLMProviderChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAllSystems();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void InitializeAllSystems()
    {
        InitializeLanguageSystem();
        LoadSettings();
        ApplySettings();
        InitializeAudioSystem();
        InitializeSaveSystem();
        OnSystemInitialized?.Invoke();

        Debug.Log("底层系统初始化完成");
    }

    void InitializeSaveSystem()
    {
        if (SaveManager.Instance != null)
        {
            Debug.Log("存档系统初始化完成");
        }
        else
        {
            Debug.LogError("存档系统初始化失败：未找到 SaveManager");
        }
    }

    void InitializeLanguageSystem()
    {
        if (LanguageManager.Instance == null)
        {
            GameObject langManagerObj = new GameObject("LanguageManager");
            langManagerObj.transform.SetParent(transform);
            var langManager = langManagerObj.AddComponent<LanguageManager>();

            langManager.csvFileName = csvFileName;
            langManager.currentLanguage = defaultLanguage;
            langManager.enableDebugLog = enableLanguageDebug;
        }

        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        OnLanguageSystemReady?.Invoke();
        Debug.Log("语言系统初始化完成");
    }

    void InitializeAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void LoadSettings()
    {
        // 音频设置
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // 显示设置
        isFullscreen = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;
        resolution.x = PlayerPrefs.GetInt("ResolutionX", 1920);
        resolution.y = PlayerPrefs.GetInt("ResolutionY", 1080);

        // 对话系统设置
        dialogueSpeed = PlayerPrefs.GetFloat("DialogueSpeed", 0.05f);

        // LLM引擎设置
        string savedProvider = PlayerPrefs.GetString("LLMProvider", LLMProvider.Gemini.ToString());
        if (System.Enum.TryParse<LLMProvider>(savedProvider, out LLMProvider provider))
        {
            currentLLMProvider = provider;
        }

        // 语言设置
        string savedLanguage = PlayerPrefs.GetString("UserLanguage", defaultLanguage.ToString());
        if (System.Enum.TryParse<SupportedLanguage>(savedLanguage, out SupportedLanguage userLang))
        {
            defaultLanguage = userLang;
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.SetLanguage(userLang);
            }
        }

        // 游戏进度
        hasGameSave = PlayerPrefs.HasKey("GameProgress");
        currentGameProgress = PlayerPrefs.GetString("GameProgress", "");
    }

    void ApplySettings()
    {
        AudioListener.volume = masterVolume;
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
    }

    public void SaveSettings()
    {
        // 保存音频设置
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        // 保存显示设置
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionX", resolution.x);
        PlayerPrefs.SetInt("ResolutionY", resolution.y);

        // 保存对话系统设置
        PlayerPrefs.SetFloat("DialogueSpeed", dialogueSpeed);

        // 保存LLM引擎设置
        PlayerPrefs.SetString("LLMProvider", currentLLMProvider.ToString());

        // 保存语言设置
        if (LanguageManager.Instance != null)
        {
            PlayerPrefs.SetString("UserLanguage", LanguageManager.Instance.currentLanguage.ToString());
        }

        PlayerPrefs.Save();
        Debug.Log("系统设置已保存");
    }

    // ==================== 音频服务 ====================

    public void SetVolume(float master, float sfx, float music)
    {
        masterVolume = Mathf.Clamp01(master);
        sfxVolume = Mathf.Clamp01(sfx);
        musicVolume = Mathf.Clamp01(music);
        AudioListener.volume = masterVolume;
        SaveSettings();
        Debug.Log($"音量设置：主音量{masterVolume:F2}, 音效{sfxVolume:F2}, 音乐{musicVolume:F2}");
    }

    public void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    // ==================== 显示服务 ====================

    public void SetDisplay(bool fullscreen, Vector2Int res)
    {
        isFullscreen = fullscreen;
        resolution = res;
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
        SaveSettings();
        Debug.Log($"显示设置：{resolution.x}x{resolution.y}, 全屏:{isFullscreen}");
    }

    // ==================== 对话系统服务 ====================

    public void SetDialogueSpeed(float speed)
    {
        dialogueSpeed = Mathf.Clamp(speed, 0.01f, 0.2f);
        SaveSettings();
        OnDialogueSettingsChanged?.Invoke();
        Debug.Log($"对话速度设置：{dialogueSpeed:F3}");
    }

    public float GetDialogueSettings()
    {
        return dialogueSpeed;
    }

    // ==================== LLM引擎服务 ====================

    /// <summary>
    /// 切换LLM引擎
    /// </summary>
    public void SetLLMProvider(LLMProvider provider)
    {
        if (currentLLMProvider == provider)
        {
            Debug.Log($"LLM引擎已经是 {provider}，无需切换");
            return;
        }

        LLMProvider oldProvider = currentLLMProvider;
        currentLLMProvider = provider;
        SaveSettings();
        OnLLMProviderChanged?.Invoke(currentLLMProvider);
        Debug.Log($"LLM引擎已切换: {oldProvider} → {currentLLMProvider}");
    }

    /// <summary>
    /// 获取当前LLM引擎
    /// </summary>
    public LLMProvider GetCurrentLLMProvider()
    {
        return currentLLMProvider;
    }

    // ==================== 语言服务 ====================

    public string GetText(string key)
    {
        if (LanguageManager.Instance != null)
        {
            return LanguageManager.Instance.GetText(key);
        }
        return key;
    }

    void OnLanguageChanged(SupportedLanguage newLanguage)
    {
        Debug.Log($"GlobalSystemManager: 语言切换到 {newLanguage}");
        SaveSettings();
    }

    // ==================== 存档服务 ====================

    public void SaveGameProgress(string progressData)
    {
        currentGameProgress = progressData;
        hasGameSave = !string.IsNullOrEmpty(progressData);
        PlayerPrefs.SetString("GameProgress", progressData);
        PlayerPrefs.Save();
        Debug.Log("游戏进度已保存");
    }

    public string LoadGameProgress()
    {
        return PlayerPrefs.GetString("GameProgress", "");
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey("GameProgress");
        PlayerPrefs.Save();
        hasGameSave = false;
        currentGameProgress = "";
        Debug.Log("存档已删除");
    }

    public bool HasGameSave()
    {
        return hasGameSave;
    }

    // ==================== 应用程序服务 ====================

    public void QuitApplication()
    {
        SaveSettings();
        Debug.Log("应用程序即将退出");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.OnLanguageChanged -= OnLanguageChanged;
        }
    }
}