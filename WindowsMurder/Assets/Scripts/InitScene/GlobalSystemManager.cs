using System.Collections.Generic;
using UnityEngine;

// LLMProvider 枚举已移至 LLMRuntimeConfig.cs（扩展版，含 Relay_302ai / Custom）

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
    public LLMProvider currentLLMProvider = LLMProvider.DeepSeek;

    // 每个供应商的自定义配置（Key=枚举int值）
    private Dictionary<int, LLMRuntimeConfig> providerConfigs = new Dictionary<int, LLMRuntimeConfig>();

    [Header("语言系统设置")]
    public string csvFileName = "Localization/LocalizationTable.csv";
    public SupportedLanguage defaultLanguage = SupportedLanguage.English;
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
    /// <summary>当某供应商的配置（Key/Model/Endpoint）变更时触发</summary>
    public static System.Action<LLMProvider, LLMRuntimeConfig> OnLLMConfigChanged;

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
        LoadProviderConfigs();
        LoadSettings();
        ApplySettings();
        InitializeAudioSystem();
        InitializeSaveSystem();
        OnSystemInitialized?.Invoke();

        // 读取静音状态
        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;
        AudioListener.volume = isMuted ? 0f : masterVolume;

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

        // 加载各供应商自定义配置
        LoadProviderConfigs();

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

        // 保存各供应商自定义配置
        SaveProviderConfigs();

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
    /// 切换 LLM 引擎
    /// </summary>
    public void SetLLMProvider(LLMProvider provider)
    {
        LLMProvider oldProvider = currentLLMProvider;
        currentLLMProvider = provider;
        SaveSettings();
        OnLLMProviderChanged?.Invoke(currentLLMProvider);
        Debug.Log($"LLM引擎已切换: {oldProvider} → {currentLLMProvider}");
    }

    /// <summary>
    /// 获取当前 LLM 引擎
    /// </summary>
    public LLMProvider GetCurrentLLMProvider()
    {
        return currentLLMProvider;
    }

    /// <summary>
    /// 设置指定供应商的自定义配置，并持久化
    /// </summary>
    public void SetLLMConfig(LLMProvider provider, LLMRuntimeConfig config)
    {
        int key = (int)provider;
        if (config == null)
            providerConfigs.Remove(key);
        else
            providerConfigs[key] = config;

        SaveProviderConfigs();
        OnLLMConfigChanged?.Invoke(provider, config);
        Debug.Log($"[GSM] 已保存 {provider} 的自定义配置");
    }

    /// <summary>
    /// 获取指定供应商的自定义配置（不存在则返回 null，表示使用默认值）
    /// </summary>
    public LLMRuntimeConfig GetLLMConfig(LLMProvider provider)
    {
        providerConfigs.TryGetValue((int)provider, out var config);
        return config;
    }

    /// <summary>
    /// 同时切换供应商 + 应用新配置
    /// </summary>
    public void SetLLMProviderAndConfig(LLMProvider provider, LLMRuntimeConfig config)
    {
        SetLLMConfig(provider, config);
        SetLLMProvider(provider);
    }

    // ---- 配置持久化 ----

    private void LoadProviderConfigs()
    {
        if (providerConfigs == null)
            providerConfigs = new Dictionary<int, LLMRuntimeConfig>();

        foreach (LLMProvider p in System.Enum.GetValues(typeof(LLMProvider)))
        {
            string prefsKey = LLMPresetDefaults.GetPrefsKey(p);
            if (PlayerPrefs.HasKey(prefsKey))
            {
                string json = PlayerPrefs.GetString(prefsKey);
                var cfg = LLMRuntimeConfig.FromJson(json);
                // 只有实际填了内容才保存，避免存空对象
                if (cfg.HasCustomApiKey || cfg.HasCustomModel || cfg.HasCustomEndpoint)
                    providerConfigs[(int)p] = cfg;
            }
        }
    }

    private void SaveProviderConfigs()
    {
        foreach (var kvp in providerConfigs)
        {
            LLMProvider provider = (LLMProvider)kvp.Key;
            string prefsKey = LLMPresetDefaults.GetPrefsKey(provider);
            PlayerPrefs.SetString(prefsKey, kvp.Value.ToJson());
        }
        PlayerPrefs.Save();
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
    // ==================== 静音控制 ====================

    private bool isMuted = false;

    public void ToggleMute()
    {
        isMuted = !isMuted;

        if (isMuted)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = masterVolume;
        }

        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsMuted()
    {
        return isMuted;
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
