using UnityEngine;

/// <summary>
/// 全局系统管理器 - 整合多语言系统
/// 整合所有需要跨场景的功能到一个脚本中
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

    [Header("多语言设置")]
    public string csvFilePath = "Assets/Localization/LocalizationTable.csv";
    public SupportedLanguage defaultLanguage = SupportedLanguage.Chinese;
    public bool enableLanguageDebug = true;

    [Header("游戏状态")]
    public bool hasGameSave = false;
    public string currentGameProgress = "";

    // 私有变量
    private AudioSource audioSource;
    private bool isLanguageSystemReady = false;

    // 语言系统就绪事件
    public static System.Action OnLanguageSystemReady;

    void Awake()
    {
        // 单例模式
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

    /// <summary>
    /// 初始化所有系统
    /// </summary>
    void InitializeAllSystems()
    {
        // 1. 首先初始化语言系统
        InitializeLanguageSystem();

        // 2. 加载保存的设置
        LoadSettings();

        // 3. 应用设置
        ApplySettings();

        // 4. 初始化音频组件
        InitializeAudioSystem();

        Debug.Log("全局系统初始化完成");
    }

    /// <summary>
    /// 初始化语言系统
    /// </summary>
    void InitializeLanguageSystem()
    {
        if (LanguageManager.Instance == null)
        {
            GameObject langManagerObj = new GameObject("LanguageManager");
            langManagerObj.transform.SetParent(transform);

            var langManager = langManagerObj.AddComponent<LanguageManager>();
            langManager.csvFilePath = csvFilePath;
            langManager.currentLanguage = defaultLanguage;
            langManager.enableDebugLog = enableLanguageDebug;
        }

        // 监听语言切换事件
        LanguageManager.OnLanguageChanged += OnLanguageChanged;

        isLanguageSystemReady = true;
        OnLanguageSystemReady?.Invoke();

        Debug.Log("语言系统初始化完成");
    }

    /// <summary>
    /// 初始化音频系统
    /// </summary>
    void InitializeAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// 加载设置
    /// </summary>
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

        // 语言设置 - 从PlayerPrefs加载用户偏好
        string savedLanguage = PlayerPrefs.GetString("UserLanguage", defaultLanguage.ToString());
        if (System.Enum.TryParse<SupportedLanguage>(savedLanguage, out SupportedLanguage userLang))
        {
            defaultLanguage = userLang;
            // 如果LanguageManager已经初始化，立即设置语言
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.SetLanguage(userLang);
            }
        }

        // 游戏进度
        hasGameSave = PlayerPrefs.HasKey("GameProgress");
        currentGameProgress = PlayerPrefs.GetString("GameProgress", "");
    }

    /// <summary>
    /// 应用设置
    /// </summary>
    void ApplySettings()
    {
        // 应用音量设置
        AudioListener.volume = masterVolume;

        // 应用显示设置
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
    }

    /// <summary>
    /// 保存所有设置
    /// </summary>
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

        // 保存语言设置
        if (LanguageManager.Instance != null)
        {
            PlayerPrefs.SetString("UserLanguage", LanguageManager.Instance.currentLanguage.ToString());
        }

        // 立即写入磁盘
        PlayerPrefs.Save();

        Debug.Log("设置已保存");
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    public void SetVolume(float master, float sfx, float music)
    {
        masterVolume = Mathf.Clamp01(master);
        sfxVolume = Mathf.Clamp01(sfx);
        musicVolume = Mathf.Clamp01(music);

        // 立即应用主音量
        AudioListener.volume = masterVolume;

        SaveSettings();
        Debug.Log($"音量设置：主音量{masterVolume:F2}, 音效{sfxVolume:F2}, 音乐{musicVolume:F2}");
    }

    /// <summary>
    /// 设置显示模式
    /// </summary>
    public void SetDisplay(bool fullscreen, Vector2Int res)
    {
        isFullscreen = fullscreen;
        resolution = res;

        // 立即应用显示设置
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);

        SaveSettings();
        Debug.Log($"显示设置：{resolution.x}x{resolution.y}, 全屏:{isFullscreen}");
    }

    /// <summary>
    /// 设置语言 - 通过LanguageManager
    /// </summary>
    public void SetLanguage(SupportedLanguage language)
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(language);
            // 保存设置在OnLanguageChanged中处理
        }
        else
        {
            Debug.LogWarning("LanguageManager未初始化，无法切换语言");
        }
    }

    /// <summary>
    /// 重新加载语言表
    /// </summary>
    public void ReloadLanguageTable()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.ReloadTranslations();
            Debug.Log("语言表已重新加载");
        }
    }

    /// <summary>
    /// 语言切换事件处理
    /// </summary>
    void OnLanguageChanged(SupportedLanguage newLanguage)
    {
        Debug.Log($"GlobalSystemManager: 语言切换到 {newLanguage}");

        // 自动保存语言设置
        SaveSettings();

        // 这里可以触发其他需要响应语言切换的系统
        // 例如：更新UI、重新加载本地化资源等
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    /// <summary>
    /// 保存游戏进度
    /// </summary>
    public void SaveGameProgress(string progressData)
    {
        currentGameProgress = progressData;
        hasGameSave = !string.IsNullOrEmpty(progressData);

        PlayerPrefs.SetString("GameProgress", progressData);
        PlayerPrefs.Save();

        Debug.Log("游戏进度已保存");
    }

    /// <summary>
    /// 加载游戏进度
    /// </summary>
    public string LoadGameProgress()
    {
        return PlayerPrefs.GetString("GameProgress", "");
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey("GameProgress");
        PlayerPrefs.Save();

        hasGameSave = false;
        currentGameProgress = "";

        Debug.Log("存档已删除");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        // 确保退出前保存设置
        SaveSettings();

        Debug.Log("游戏即将退出");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 获取当前设置的摘要信息（用于调试）
    /// </summary>
    public string GetSettingsSummary()
    {
        string currentLang = LanguageManager.Instance != null ?
            LanguageManager.Instance.currentLanguage.ToString() :
            "未初始化";

        return $"音量: {masterVolume:F2}/{sfxVolume:F2}/{musicVolume:F2} | " +
               $"显示: {resolution.x}x{resolution.y} {(isFullscreen ? "全屏" : "窗口")} | " +
               $"语言: {currentLang} | " +
               $"存档: {(hasGameSave ? "有" : "无")}";
    }

    /// <summary>
    /// 获取翻译文本的便捷方法
    /// </summary>
    public string GetText(string key)
    {
        if (LanguageManager.Instance != null)
        {
            return LanguageManager.Instance.GetText(key);
        }
        return key; // 降级返回Key本身
    }

    void OnDestroy()
    {
    }

    #region 编辑器调试方法

#if UNITY_EDITOR
    [ContextMenu("打印设置摘要")]
    void PrintSettingsSummary()
    {
        Debug.Log("=== 当前设置摘要 ===");
        Debug.Log(GetSettingsSummary());

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.PrintTranslationStats();
        }
    }

    [ContextMenu("重新加载语言表")]
    void EditorReloadLanguageTable()
    {
        ReloadLanguageTable();
    }

    [ContextMenu("测试语言切换")]
    void TestLanguageSwitching()
    {
        if (LanguageManager.Instance != null)
        {
            var currentLang = LanguageManager.Instance.currentLanguage;
            var nextLang = currentLang == SupportedLanguage.Chinese ?
                SupportedLanguage.English : SupportedLanguage.Chinese;

            Debug.Log($"切换语言：{currentLang} → {nextLang}");
            SetLanguage(nextLang);
        }
    }
#endif

    #endregion
}