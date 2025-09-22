using UnityEngine;

/// <summary>
/// ȫ��ϵͳ������ - ���϶�����ϵͳ
/// ����������Ҫ�糡���Ĺ��ܵ�һ���ű���
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

    [Header("����������")]
    public string csvFilePath = "Assets/Localization/LocalizationTable.csv";
    public SupportedLanguage defaultLanguage = SupportedLanguage.Chinese;
    public bool enableLanguageDebug = true;

    [Header("��Ϸ״̬")]
    public bool hasGameSave = false;
    public string currentGameProgress = "";

    // ˽�б���
    private AudioSource audioSource;
    private bool isLanguageSystemReady = false;

    // ����ϵͳ�����¼�
    public static System.Action OnLanguageSystemReady;

    void Awake()
    {
        // ����ģʽ
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
    /// ��ʼ������ϵͳ
    /// </summary>
    void InitializeAllSystems()
    {
        // 1. ���ȳ�ʼ������ϵͳ
        InitializeLanguageSystem();

        // 2. ���ر��������
        LoadSettings();

        // 3. Ӧ������
        ApplySettings();

        // 4. ��ʼ����Ƶ���
        InitializeAudioSystem();

        Debug.Log("ȫ��ϵͳ��ʼ�����");
    }

    /// <summary>
    /// ��ʼ������ϵͳ
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

        // ���������л��¼�
        LanguageManager.OnLanguageChanged += OnLanguageChanged;

        isLanguageSystemReady = true;
        OnLanguageSystemReady?.Invoke();

        Debug.Log("����ϵͳ��ʼ�����");
    }

    /// <summary>
    /// ��ʼ����Ƶϵͳ
    /// </summary>
    void InitializeAudioSystem()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// ��������
    /// </summary>
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

        // �������� - ��PlayerPrefs�����û�ƫ��
        string savedLanguage = PlayerPrefs.GetString("UserLanguage", defaultLanguage.ToString());
        if (System.Enum.TryParse<SupportedLanguage>(savedLanguage, out SupportedLanguage userLang))
        {
            defaultLanguage = userLang;
            // ���LanguageManager�Ѿ���ʼ����������������
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.SetLanguage(userLang);
            }
        }

        // ��Ϸ����
        hasGameSave = PlayerPrefs.HasKey("GameProgress");
        currentGameProgress = PlayerPrefs.GetString("GameProgress", "");
    }

    /// <summary>
    /// Ӧ������
    /// </summary>
    void ApplySettings()
    {
        // Ӧ����������
        AudioListener.volume = masterVolume;

        // Ӧ����ʾ����
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
    }

    /// <summary>
    /// ������������
    /// </summary>
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

        // ������������
        if (LanguageManager.Instance != null)
        {
            PlayerPrefs.SetString("UserLanguage", LanguageManager.Instance.currentLanguage.ToString());
        }

        // ����д�����
        PlayerPrefs.Save();

        Debug.Log("�����ѱ���");
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetVolume(float master, float sfx, float music)
    {
        masterVolume = Mathf.Clamp01(master);
        sfxVolume = Mathf.Clamp01(sfx);
        musicVolume = Mathf.Clamp01(music);

        // ����Ӧ��������
        AudioListener.volume = masterVolume;

        SaveSettings();
        Debug.Log($"�������ã�������{masterVolume:F2}, ��Ч{sfxVolume:F2}, ����{musicVolume:F2}");
    }

    /// <summary>
    /// ������ʾģʽ
    /// </summary>
    public void SetDisplay(bool fullscreen, Vector2Int res)
    {
        isFullscreen = fullscreen;
        resolution = res;

        // ����Ӧ����ʾ����
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);

        SaveSettings();
        Debug.Log($"��ʾ���ã�{resolution.x}x{resolution.y}, ȫ��:{isFullscreen}");
    }

    /// <summary>
    /// �������� - ͨ��LanguageManager
    /// </summary>
    public void SetLanguage(SupportedLanguage language)
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(language);
            // ����������OnLanguageChanged�д���
        }
        else
        {
            Debug.LogWarning("LanguageManagerδ��ʼ�����޷��л�����");
        }
    }

    /// <summary>
    /// ���¼������Ա�
    /// </summary>
    public void ReloadLanguageTable()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.ReloadTranslations();
            Debug.Log("���Ա������¼���");
        }
    }

    /// <summary>
    /// �����л��¼�����
    /// </summary>
    void OnLanguageChanged(SupportedLanguage newLanguage)
    {
        Debug.Log($"GlobalSystemManager: �����л��� {newLanguage}");

        // �Զ�������������
        SaveSettings();

        // ������Դ���������Ҫ��Ӧ�����л���ϵͳ
        // ���磺����UI�����¼��ر��ػ���Դ��
    }

    /// <summary>
    /// ������Ч
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    /// <summary>
    /// ������Ϸ����
    /// </summary>
    public void SaveGameProgress(string progressData)
    {
        currentGameProgress = progressData;
        hasGameSave = !string.IsNullOrEmpty(progressData);

        PlayerPrefs.SetString("GameProgress", progressData);
        PlayerPrefs.Save();

        Debug.Log("��Ϸ�����ѱ���");
    }

    /// <summary>
    /// ������Ϸ����
    /// </summary>
    public string LoadGameProgress()
    {
        return PlayerPrefs.GetString("GameProgress", "");
    }

    /// <summary>
    /// ɾ���浵
    /// </summary>
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey("GameProgress");
        PlayerPrefs.Save();

        hasGameSave = false;
        currentGameProgress = "";

        Debug.Log("�浵��ɾ��");
    }

    /// <summary>
    /// �˳���Ϸ
    /// </summary>
    public void QuitGame()
    {
        // ȷ���˳�ǰ��������
        SaveSettings();

        Debug.Log("��Ϸ�����˳�");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// ��ȡ��ǰ���õ�ժҪ��Ϣ�����ڵ��ԣ�
    /// </summary>
    public string GetSettingsSummary()
    {
        string currentLang = LanguageManager.Instance != null ?
            LanguageManager.Instance.currentLanguage.ToString() :
            "δ��ʼ��";

        return $"����: {masterVolume:F2}/{sfxVolume:F2}/{musicVolume:F2} | " +
               $"��ʾ: {resolution.x}x{resolution.y} {(isFullscreen ? "ȫ��" : "����")} | " +
               $"����: {currentLang} | " +
               $"�浵: {(hasGameSave ? "��" : "��")}";
    }

    /// <summary>
    /// ��ȡ�����ı��ı�ݷ���
    /// </summary>
    public string GetText(string key)
    {
        if (LanguageManager.Instance != null)
        {
            return LanguageManager.Instance.GetText(key);
        }
        return key; // ��������Key����
    }

    void OnDestroy()
    {
    }

    #region �༭�����Է���

#if UNITY_EDITOR
    [ContextMenu("��ӡ����ժҪ")]
    void PrintSettingsSummary()
    {
        Debug.Log("=== ��ǰ����ժҪ ===");
        Debug.Log(GetSettingsSummary());

        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.PrintTranslationStats();
        }
    }

    [ContextMenu("���¼������Ա�")]
    void EditorReloadLanguageTable()
    {
        ReloadLanguageTable();
    }

    [ContextMenu("���������л�")]
    void TestLanguageSwitching()
    {
        if (LanguageManager.Instance != null)
        {
            var currentLang = LanguageManager.Instance.currentLanguage;
            var nextLang = currentLang == SupportedLanguage.Chinese ?
                SupportedLanguage.English : SupportedLanguage.Chinese;

            Debug.Log($"�л����ԣ�{currentLang} �� {nextLang}");
            SetLanguage(nextLang);
        }
    }
#endif

    #endregion
}