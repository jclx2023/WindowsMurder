using UnityEngine;

/// <summary>
/// LLM��������ö��
/// </summary>
public enum LLMProvider
{
    Gemini,
    GPT,
    DeepSeek
}

/// <summary>
/// ȫ��ϵͳ������ - רע�ڵײ�ϵͳ����
/// �ṩ��Ƶ����ʾ�����ԡ��浵��LLM����Ȼ������񣬲�����UI����
/// </summary>
public class GlobalSystemManager : MonoBehaviour
{
    public static GlobalSystemManager Instance;

    [Header("��Ƶ����")]
    public float masterVolume = 1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    [Header("��ʾ����")]
    public bool isFullscreen = true;
    public Vector2Int resolution = new Vector2Int(1920, 1080);

    [Header("�Ի�ϵͳ����")]
    public float dialogueSpeed = 0.05f;

    [Header("LLM��������")]
    public LLMProvider currentLLMProvider = LLMProvider.Gemini;

    [Header("����ϵͳ����")]
    public string csvFileName = "Localization/LocalizationTable.csv";
    public SupportedLanguage defaultLanguage = SupportedLanguage.Chinese;
    public bool enableLanguageDebug = true;

    [Header("��Ϸ״̬")]
    public bool hasGameSave = false;
    public string currentGameProgress = "";

    // ˽�б���
    private AudioSource audioSource;

    // ϵͳ�����¼�
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

        Debug.Log("�ײ�ϵͳ��ʼ�����");
    }

    void InitializeSaveSystem()
    {
        if (SaveManager.Instance != null)
        {
            Debug.Log("�浵ϵͳ��ʼ�����");
        }
        else
        {
            Debug.LogError("�浵ϵͳ��ʼ��ʧ�ܣ�δ�ҵ� SaveManager");
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
        Debug.Log("����ϵͳ��ʼ�����");
    }

    void InitializeAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void LoadSettings()
    {
        // ��Ƶ����
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // ��ʾ����
        isFullscreen = PlayerPrefs.GetInt("IsFullscreen", 1) == 1;
        resolution.x = PlayerPrefs.GetInt("ResolutionX", 1920);
        resolution.y = PlayerPrefs.GetInt("ResolutionY", 1080);

        // �Ի�ϵͳ����
        dialogueSpeed = PlayerPrefs.GetFloat("DialogueSpeed", 0.05f);

        // LLM��������
        string savedProvider = PlayerPrefs.GetString("LLMProvider", LLMProvider.Gemini.ToString());
        if (System.Enum.TryParse<LLMProvider>(savedProvider, out LLMProvider provider))
        {
            currentLLMProvider = provider;
        }

        // ��������
        string savedLanguage = PlayerPrefs.GetString("UserLanguage", defaultLanguage.ToString());
        if (System.Enum.TryParse<SupportedLanguage>(savedLanguage, out SupportedLanguage userLang))
        {
            defaultLanguage = userLang;
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.SetLanguage(userLang);
            }
        }

        // ��Ϸ����
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
        // ������Ƶ����
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

        // ������ʾ����
        PlayerPrefs.SetInt("IsFullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionX", resolution.x);
        PlayerPrefs.SetInt("ResolutionY", resolution.y);

        // ����Ի�ϵͳ����
        PlayerPrefs.SetFloat("DialogueSpeed", dialogueSpeed);

        // ����LLM��������
        PlayerPrefs.SetString("LLMProvider", currentLLMProvider.ToString());

        // ������������
        if (LanguageManager.Instance != null)
        {
            PlayerPrefs.SetString("UserLanguage", LanguageManager.Instance.currentLanguage.ToString());
        }

        PlayerPrefs.Save();
        Debug.Log("ϵͳ�����ѱ���");
    }

    // ==================== ��Ƶ���� ====================

    public void SetVolume(float master, float sfx, float music)
    {
        masterVolume = Mathf.Clamp01(master);
        sfxVolume = Mathf.Clamp01(sfx);
        musicVolume = Mathf.Clamp01(music);
        AudioListener.volume = masterVolume;
        SaveSettings();
        Debug.Log($"�������ã�������{masterVolume:F2}, ��Ч{sfxVolume:F2}, ����{musicVolume:F2}");
    }

    public void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    // ==================== ��ʾ���� ====================

    public void SetDisplay(bool fullscreen, Vector2Int res)
    {
        isFullscreen = fullscreen;
        resolution = res;
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
        SaveSettings();
        Debug.Log($"��ʾ���ã�{resolution.x}x{resolution.y}, ȫ��:{isFullscreen}");
    }

    // ==================== �Ի�ϵͳ���� ====================

    public void SetDialogueSpeed(float speed)
    {
        dialogueSpeed = Mathf.Clamp(speed, 0.01f, 0.2f);
        SaveSettings();
        OnDialogueSettingsChanged?.Invoke();
        Debug.Log($"�Ի��ٶ����ã�{dialogueSpeed:F3}");
    }

    public float GetDialogueSettings()
    {
        return dialogueSpeed;
    }

    // ==================== LLM������� ====================

    /// <summary>
    /// �л�LLM����
    /// </summary>
    public void SetLLMProvider(LLMProvider provider)
    {
        if (currentLLMProvider == provider)
        {
            Debug.Log($"LLM�����Ѿ��� {provider}�������л�");
            return;
        }

        LLMProvider oldProvider = currentLLMProvider;
        currentLLMProvider = provider;
        SaveSettings();
        OnLLMProviderChanged?.Invoke(currentLLMProvider);
        Debug.Log($"LLM�������л�: {oldProvider} �� {currentLLMProvider}");
    }

    /// <summary>
    /// ��ȡ��ǰLLM����
    /// </summary>
    public LLMProvider GetCurrentLLMProvider()
    {
        return currentLLMProvider;
    }

    // ==================== ���Է��� ====================

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
        Debug.Log($"GlobalSystemManager: �����л��� {newLanguage}");
        SaveSettings();
    }

    // ==================== �浵���� ====================

    public void SaveGameProgress(string progressData)
    {
        currentGameProgress = progressData;
        hasGameSave = !string.IsNullOrEmpty(progressData);
        PlayerPrefs.SetString("GameProgress", progressData);
        PlayerPrefs.Save();
        Debug.Log("��Ϸ�����ѱ���");
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
        Debug.Log("�浵��ɾ��");
    }

    public bool HasGameSave()
    {
        return hasGameSave;
    }

    // ==================== Ӧ�ó������ ====================

    public void QuitApplication()
    {
        SaveSettings();
        Debug.Log("Ӧ�ó��򼴽��˳�");

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