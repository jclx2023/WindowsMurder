using UnityEngine;

/// <summary>
/// ȫ��ϵͳ������ - �򻯰�
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

    [Header("��������")]
    public SystemLanguage currentLanguage = SystemLanguage.ChineseSimplified;

    [Header("��Ϸ״̬")]
    public bool hasGameSave = false;
    public string currentGameProgress = "";

    // ˽�б���
    private AudioSource audioSource;

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
        // ���ر��������
        LoadSettings();

        // Ӧ������
        ApplySettings();

        // ��ʼ����Ƶ���
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        Debug.Log("ȫ��ϵͳ��ʼ�����");
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

        // ��������
        currentLanguage = (SystemLanguage)PlayerPrefs.GetInt("GameLanguage", (int)SystemLanguage.ChineseSimplified);

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
        PlayerPrefs.SetInt("GameLanguage", (int)currentLanguage);

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
    /// ��������
    /// </summary>
    public void SetLanguage(SystemLanguage language)
    {
        if (currentLanguage != language)
        {
            currentLanguage = language;
            SaveSettings();

            // ������Դ������Ա���¼�
            // ֪ͨ����ϵͳ����UI�ı�
            Debug.Log($"����������Ϊ��{currentLanguage}");
        }
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
    /// ������������ΪĬ��ֵ
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

        Debug.Log("����������ΪĬ��ֵ");
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
        return $"����: {masterVolume:F2}/{sfxVolume:F2}/{musicVolume:F2} | " +
               $"��ʾ: {resolution.x}x{resolution.y} {(isFullscreen ? "ȫ��" : "����")} | " +
               $"����: {currentLanguage} | " +
               $"�浵: {(hasGameSave ? "��" : "��")}";
    }
}