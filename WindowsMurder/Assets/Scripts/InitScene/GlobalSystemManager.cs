using UnityEngine;

/// <summary>
/// ȫ��ϵͳ������ - רע�ڵײ�ϵͳ����
/// �ṩ��Ƶ����ʾ�����ԡ��浵�Ȼ������񣬲�����UI����
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

    // ϵͳ�����¼�
    public static System.Action OnLanguageSystemReady;
    public static System.Action OnSystemInitialized;

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
    /// ��ʼ�����еײ�ϵͳ
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

        // 5. ֪ͨ����ϵͳ��ʼ�����
        OnSystemInitialized?.Invoke();

        Debug.Log("�ײ�ϵͳ��ʼ�����");
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

        Debug.Log("ϵͳ�����ѱ���");
    }

    // ==================== ��Ƶ���� ====================

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
    /// ������Ч
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    // ==================== ��ʾ���� ====================

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

    // ==================== ���Է��� ====================

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
    /// ��ȡ�����ı�
    /// </summary>
    public string GetText(string key)
    {
        if (LanguageManager.Instance != null)
        {
            return LanguageManager.Instance.GetText(key);
        }
        return key; // ��������Key����
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
    }

    // ==================== �浵���� ====================

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
    /// ����Ƿ�����Ϸ�浵
    /// </summary>
    public bool HasGameSave()
    {
        return hasGameSave;
    }

    // ==================== Ӧ�ó������ ====================

    /// <summary>
    /// �˳���Ϸ
    /// </summary>
    public void QuitApplication()
    {
        // ȷ���˳�ǰ��������
        SaveSettings();

        Debug.Log("Ӧ�ó��򼴽��˳�");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================== ���Ժ�״̬��ѯ ====================

    void OnDestroy()
    {
        // ȡ���¼�����
        if (LanguageManager.Instance != null)
        {
            LanguageManager.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    #region �༭�����Է���

#if UNITY_EDITOR

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