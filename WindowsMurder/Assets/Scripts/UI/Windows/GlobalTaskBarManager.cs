using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ȫ�������������� - ����ʼ�˵�������������
/// </summary>
public class GlobalTaskBarManager : MonoBehaviour
{
    public static GlobalTaskBarManager Instance;

    [Header("���������")]
    public Button startButton;
    public GameObject startPanel;

    [Header("��ʼ�˵���ť")]
    public Button mainMenuButton;
    public Button newGameButton;
    public Button continueButton;
    public Button languageButton;
    public Button displayButton;
    public Button creditsButton;
    public Button shutdownButton;

    [Header("��Ч")]
    public AudioClip buttonClickSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;

    // ˽�б���
    private bool isStartMenuOpen = false;
    private AudioSource audioSource;
    private string currentSceneName;

    void Awake()
    {
        // ����ģʽ
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTaskBar();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // ���������仯
        SceneManager.sceneLoaded += OnSceneLoaded;

        // ��ʼ����ǰ����
        currentSceneName = SceneManager.GetActiveScene().name;
        UpdateMenuButtonsForScene();
    }

    void OnDestroy()
    {
        // ȡ����������
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// ��ʼ��������
    /// </summary>
    void InitializeTaskBar()
    {
        // ��ȡ��Ƶ���
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // ��ʼ״̬�¹رտ�ʼ�˵�
        if (startPanel != null)
            startPanel.SetActive(false);

        // �󶨿�ʼ��ť�¼�
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(ToggleStartMenu);
        }

        // �󶨲˵���ť�¼�
        BindMenuButtons();

        Debug.Log("ȫ����������ʼ�����");
    }

    /// <summary>
    /// �󶨿�ʼ�˵���ť�¼�
    /// </summary>
    void BindMenuButtons()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => OnMainMenuClicked());
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(() => OnNewGameClicked());
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => OnContinueClicked());
        }

        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(() => OnLanguageClicked());
        }

        if (displayButton != null)
        {
            displayButton.onClick.RemoveAllListeners();
            displayButton.onClick.AddListener(() => OnDisplayClicked());
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(() => OnCreditsClicked());
        }

        if (shutdownButton != null)
        {
            shutdownButton.onClick.RemoveAllListeners();
            shutdownButton.onClick.AddListener(() => OnShutdownClicked());
        }
    }

    /// <summary>
    /// ��������ʱ�Ļص�
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        UpdateMenuButtonsForScene();

        // �����л�ʱ�رտ�ʼ�˵�
        CloseStartMenu();

        Debug.Log($"�����л���: {currentSceneName}");
    }

    /// <summary>
    /// ���ݵ�ǰ�������²˵���ť״̬
    /// </summary>
    void UpdateMenuButtonsForScene()
    {
        bool isMainMenuScene = (currentSceneName == "MainMenu");
        bool isGameScene = (currentSceneName.Contains("Game") || currentSceneName == "GameScene");

        // ���˵���ť��������Ϸ��������ʾ
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(isGameScene);
        }

        // ����Ϸ��ť���������˵�������ʾ
        if (newGameButton != null)
        {
            newGameButton.gameObject.SetActive(isMainMenuScene);
        }

        // ������Ϸ��ť���������˵�������ʾ������Ҫ���浵
        if (continueButton != null)
        {
            bool hasGameSave = GlobalSystemManager.Instance != null &&
                              GlobalSystemManager.Instance.hasGameSave;
            continueButton.gameObject.SetActive(isMainMenuScene);
            continueButton.interactable = hasGameSave;
        }

        Debug.Log($"��ť״̬���� - MainMenu: {isMainMenuScene}, Game: {isGameScene}");
    }

    /// <summary>
    /// �л���ʼ�˵���ʾ״̬
    /// </summary>
    public void ToggleStartMenu()
    {
        if (isStartMenuOpen)
        {
            CloseStartMenu();
        }
        else
        {
            OpenStartMenu();
        }
    }

    /// <summary>
    /// �򿪿�ʼ�˵�
    /// </summary>
    public void OpenStartMenu()
    {
        if (startPanel != null && !isStartMenuOpen)
        {
            PlaySound(menuOpenSound);
            startPanel.SetActive(true);
            isStartMenuOpen = true;

            // ���°�ť״̬
            UpdateMenuButtonsForScene();

            Debug.Log("��ʼ�˵��Ѵ�");
        }
    }

    /// <summary>
    /// �رտ�ʼ�˵�
    /// </summary>
    public void CloseStartMenu()
    {
        if (startPanel != null && isStartMenuOpen)
        {
            PlaySound(menuCloseSound);
            startPanel.SetActive(false);
            isStartMenuOpen = false;

            Debug.Log("��ʼ�˵��ѹر�");
        }
    }

    /// <summary>
    /// ������Ч
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// ���Ű�ť�����Ч
    /// </summary>
    void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    // ==================== ��ť�¼����� ====================

    /// <summary>
    /// �������˵�
    /// </summary>
    void OnMainMenuClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("�������˵�");
        StartCoroutine(LoadSceneAsync("MainMenu"));
    }

    /// <summary>
    /// ��ʼ����Ϸ
    /// </summary>
    void OnNewGameClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        // ���֮ǰ�Ĵ浵
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.DeleteSave();
        }

        Debug.Log("��ʼ����Ϸ");
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    void OnContinueClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        // ����Ƿ��д浵
        if (GlobalSystemManager.Instance != null && GlobalSystemManager.Instance.hasGameSave)
        {
            Debug.Log("������Ϸ");
            StartCoroutine(LoadSceneAsync("GameScene"));
        }
        else
        {
            Debug.LogWarning("û���ҵ��浵�ļ�");
            // ������ʾ��ʾ��Ϣ
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    void OnLanguageClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("����������");
        // TODO: ʵ���������ô���
        // LanguageSettingsWindow.Show();
    }

    /// <summary>
    /// ��ʾ����
    /// </summary>
    void OnDisplayClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("����ʾ����");
        // TODO: ʵ����ʾ���ô���
        // DisplaySettingsWindow.Show();
    }

    /// <summary>
    /// ������Ա
    /// </summary>
    void OnCreditsClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("��ʾ������Ա��Ϣ");
        // TODO: ʵ��������Ա����
        // CreditsWindow.Show();
    }

    /// <summary>
    /// �ػ�/�˳���Ϸ
    /// </summary>
    void OnShutdownClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("�˳���Ϸ");

        // ����ȫ��ϵͳ���˳�����
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.QuitGame();
        }
        else
        {
            // �����˳�����
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// �첽���س���
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        // ������������Ӽ��ػ���
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOperation.isDone)
        {
            // ���Ը��¼��ؽ���
            yield return null;
        }
    }

    // ==================== �ⲿ���ýӿ� ====================

    /// <summary>
    /// �ⲿ���ã�ǿ�ƹرտ�ʼ�˵�
    /// </summary>
    public void ForceCloseStartMenu()
    {
        CloseStartMenu();
    }

    /// <summary>
    /// �ⲿ���ã���鿪ʼ�˵��Ƿ��
    /// </summary>
    public bool IsStartMenuOpen()
    {
        return isStartMenuOpen;
    }

    /// <summary>
    /// �ⲿ���ã�ˢ�°�ť״̬
    /// </summary>
    public void RefreshButtonStates()
    {
        UpdateMenuButtonsForScene();
    }

    // ==================== Unity�¼� ====================

    void Update()
    {
        // ESC���رտ�ʼ�˵�
        if (Input.GetKeyDown(KeyCode.Escape) && isStartMenuOpen)
        {
            CloseStartMenu();
        }

        // ����հ�����رղ˵�����ѡ��
        if (isStartMenuOpen && Input.GetMouseButtonDown(0))
        {
            // �������Ƿ����ڲ˵��ⲿ
            // ������Ҫ����ʵ��UI�������ж�
        }
    }
}