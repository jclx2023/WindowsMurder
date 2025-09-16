using UnityEngine;

/// <summary>
/// 全局系统管理器 - 简化版
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

    [Header("语言设置")]
    public SystemLanguage currentLanguage = SystemLanguage.ChineseSimplified;

    [Header("游戏状态")]
    public bool hasGameSave = false;
    public string currentGameProgress = "";

    // 私有变量
    private AudioSource audioSource;

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
        // 加载保存的设置
        LoadSettings();

        // 应用设置
        ApplySettings();

        // 初始化音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        Debug.Log("全局系统初始化完成");
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

        // 语言设置
        currentLanguage = (SystemLanguage)PlayerPrefs.GetInt("GameLanguage", (int)SystemLanguage.ChineseSimplified);

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
        PlayerPrefs.SetInt("GameLanguage", (int)currentLanguage);

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
    /// 设置语言
    /// </summary>
    public void SetLanguage(SystemLanguage language)
    {
        if (currentLanguage != language)
        {
            currentLanguage = language;
            SaveSettings();

            // 这里可以触发语言变更事件
            // 通知其他系统更新UI文本
            Debug.Log($"语言已设置为：{currentLanguage}");
        }
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
    /// 重置所有设置为默认值
    /// </summary>
    public void ResetToDefaults()
    {
        masterVolume = 1f;
        sfxVolume = 1f;
        musicVolume = 1f;

        isFullscreen = true;
        resolution = new Vector2Int(1920, 1080);

        currentLanguage = SystemLanguage.ChineseSimplified;

        ApplySettings();
        SaveSettings();

        Debug.Log("设置已重置为默认值");
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
        return $"音量: {masterVolume:F2}/{sfxVolume:F2}/{musicVolume:F2} | " +
               $"显示: {resolution.x}x{resolution.y} {(isFullscreen ? "全屏" : "窗口")} | " +
               $"语言: {currentLanguage} | " +
               $"存档: {(hasGameSave ? "有" : "无")}";
    }
}