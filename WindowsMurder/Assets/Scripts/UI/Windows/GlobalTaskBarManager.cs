using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ȫ�������������� - ����UI������ActionManager
/// ����TaskBar UI��ʾ���û�����������GlobalActionManager����ҵ���߼�
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

    [Header("ActionManager����")]
    public GameObject globalActionManagerPrefab; // GlobalActionManagerԤ����
    public bool createActionManagerAtStart = true; // �Ƿ�������ʱ�Զ�����

    // ˽�б���
    private bool isStartMenuOpen = false;
    private string currentSceneName;
    private GlobalActionManager actionManager;

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
        // �ȴ�GlobalSystemManager��ʼ�����
        if (GlobalSystemManager.Instance != null)
        {
            // ���ϵͳ�ѳ�ʼ����ֱ�ӿ�ʼ
            OnSystemReady();
        }
        else
        {
            // �ȴ�ϵͳ��ʼ�����
            GlobalSystemManager.OnSystemInitialized += OnSystemReady;
        }
    }

    void OnDestroy()
    {
        // ȡ������������ϵͳ�¼�
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (GlobalSystemManager.OnSystemInitialized != null)
        {
            GlobalSystemManager.OnSystemInitialized -= OnSystemReady;
        }
    }

    /// <summary>
    /// ϵͳ������ĳ�ʼ��
    /// </summary>
    void OnSystemReady()
    {
        // ���������仯
        SceneManager.sceneLoaded += OnSceneLoaded;

        // ��ʼ����ǰ����
        currentSceneName = SceneManager.GetActiveScene().name;
        UpdateMenuButtonsForScene();

        Debug.Log("TaskBarϵͳ��ʼ�����");
    }

    /// <summary>
    /// ��ʼ��������
    /// </summary>
    void InitializeTaskBar()
    {
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

        // ��ʼ��ActionManager
        if (createActionManagerAtStart)
        {
            InitializeActionManager();
        }

        Debug.Log("ȫ����������ʼ�����");
    }

    /// <summary>
    /// ��ʼ��ActionManager
    /// </summary>
    void InitializeActionManager()
    {
        if (actionManager != null)
        {
            Debug.LogWarning("GlobalActionManager�Ѿ����ڣ�������ʼ��");
            return;
        }

        // ��ʽ1��ͨ��Ԥ���崴��
        if (globalActionManagerPrefab != null)
        {
            GameObject actionManagerObj = Instantiate(globalActionManagerPrefab, transform);
            actionManager = actionManagerObj.GetComponent<GlobalActionManager>();

            if (actionManager == null)
            {
                Debug.LogError("Ԥ������û���ҵ�GlobalActionManager���");
                Destroy(actionManagerObj);
                return;
            }
        }
        // ��ʽ2����̬����
        else
        {
            GameObject actionManagerObj = new GameObject("GlobalActionManager");
            actionManagerObj.transform.SetParent(transform);
            actionManager = actionManagerObj.AddComponent<GlobalActionManager>();
        }

        // ȷ��ActionManager��ʼ��
        if (actionManager != null)
        {
            actionManager.InitializeManager();
            Debug.Log("GlobalActionManager ��TaskBar��������ʼ�����");
        }
    }

    /// <summary>
    /// ��ȡActionManagerʵ���������أ�
    /// </summary>
    public GlobalActionManager GetActionManager()
    {
        if (actionManager == null && !createActionManagerAtStart)
        {
            InitializeActionManager();
        }
        return actionManager;
    }

    /// <summary>
    /// �󶨿�ʼ�˵���ť�¼� - ͨ��ActionManager����ҵ���߼�
    /// </summary>
    void BindMenuButtons()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => ExecuteAction("BackToMainMenu"));
        }

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(() => ExecuteAction("NewGame"));
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => ExecuteAction("Continue"));
        }

        if (languageButton != null)
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(() => ExecuteAction("OpenLanguageSettings"));
        }

        if (displayButton != null)
        {
            displayButton.onClick.RemoveAllListeners();
            displayButton.onClick.AddListener(() => ExecuteAction("OpenDisplaySettings"));
        }

        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveAllListeners();
            creditsButton.onClick.AddListener(() => ExecuteAction("OpenCredits"));
        }

        if (shutdownButton != null)
        {
            shutdownButton.onClick.RemoveAllListeners();
            shutdownButton.onClick.AddListener(() => ExecuteAction("Shutdown"));
        }
    }

    /// <summary>
    /// ִ�в�����ͳһ��� - ������Ч�Ͳ˵��رգ�Ȼ�����ActionManager
    /// </summary>
    void ExecuteAction(string actionName)
    {
        PlayButtonClick();
        CloseStartMenu();

        // ȷ��ActionManager����
        if (actionManager == null)
        {
            InitializeActionManager();
        }

        // ���ActionManager��GlobalActionManager.Instance
        if (actionManager != null && GlobalActionManager.Instance != null)
        {
            // ���ݲ������Ƶ�����Ӧ��ActionManager����
            switch (actionName)
            {
                case "BackToMainMenu":
                    GlobalActionManager.Instance.BackToMainMenu();
                    break;
                case "NewGame":
                    GlobalActionManager.Instance.NewGame();
                    break;
                case "Continue":
                    GlobalActionManager.Instance.Continue();
                    break;
                case "OpenLanguageSettings":
                    GlobalActionManager.Instance.OpenLanguageSettings();
                    break;
                case "OpenDisplaySettings":
                    GlobalActionManager.Instance.OpenDisplaySettings();
                    break;
                case "OpenCredits":
                    GlobalActionManager.Instance.OpenCredits();
                    break;
                case "Shutdown":
                    GlobalActionManager.Instance.Shutdown();
                    break;
                default:
                    Debug.LogWarning($"TaskBar: δ֪�Ĳ��� {actionName}");
                    break;
            }
        }
        else
        {
            Debug.LogError("TaskBar: GlobalActionManager δ��ȷ��ʼ�����޷�ִ�в���");
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
                              GlobalSystemManager.Instance.HasGameSave();
            continueButton.gameObject.SetActive(isMainMenuScene);
            continueButton.interactable = hasGameSave;
        }

        Debug.Log($"TaskBar��ť״̬���� - MainMenu: {isMainMenuScene}, Game: {isGameScene}");
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
    /// ������Ч - ͨ��GlobalSystemManager
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.PlaySFX(clip);
        }
    }

    /// <summary>
    /// ���Ű�ť�����Ч
    /// </summary>
    void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
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