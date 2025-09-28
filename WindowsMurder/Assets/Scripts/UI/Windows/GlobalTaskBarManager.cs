using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// ȫ�������������� - ���ɴ��ڹ�����
/// ����TaskBar UI��ʾ���û������ʹ��ڰ�ť����
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

    [Header("���ڰ�ť����")]
    public Transform windowButtonContainer;    // ���ڰ�ť������������HorizontalLayout��
    public GameObject windowButtonPrefab;      // ���ڰ�ťԤ����
    public int maxVisibleButtons = 10;         // �����ʾ��ť������ѡ��

    [Header("��ť��ʽ����")]
    public Color normalButtonColor = Color.white;
    public Color activeButtonColor = Color.cyan;
    public Color hoveredButtonColor = Color.gray;

    [Header("��Ч")]
    public AudioClip buttonClickSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;

    [Header("ActionManager����")]
    public GameObject globalActionManagerPrefab;
    public bool createActionManagerAtStart = true;

    // ˽�б���
    private bool isStartMenuOpen = false;
    private string currentSceneName;
    private GlobalActionManager actionManager;

    // ���ڰ�ť����
    private Dictionary<WindowsWindow, GameObject> windowButtonMap = new Dictionary<WindowsWindow, GameObject>();
    private Dictionary<GameObject, WindowsWindow> buttonWindowMap = new Dictionary<GameObject, WindowsWindow>();
    private WindowsWindow currentActiveWindow;

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
            OnSystemReady();
        }
        else
        {
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

        // ȡ�����ڹ������¼�����
        if (WindowManager.Instance != null)
        {
            WindowManager.OnWindowRegistered -= OnWindowRegistered;
            WindowManager.OnWindowUnregistered -= OnWindowUnregistered;
        }

        // ȡ������ѡ���¼�����
        WindowsWindow.OnWindowSelected -= OnWindowSelected;
    }

    /// <summary>
    /// ϵͳ������ĳ�ʼ��
    /// </summary>
    void OnSystemReady()
    {
        // ���������仯
        SceneManager.sceneLoaded += OnSceneLoaded;

        // �������ڹ������¼�
        if (WindowManager.Instance != null)
        {
            WindowManager.OnWindowRegistered += OnWindowRegistered;
            WindowManager.OnWindowUnregistered += OnWindowUnregistered;
        }

        // ��������ѡ���¼�
        WindowsWindow.OnWindowSelected += OnWindowSelected;

        // ��ʼ����ǰ����
        currentSceneName = SceneManager.GetActiveScene().name;
        UpdateMenuButtonsForScene();

        Debug.Log("TaskBarϵͳ�������ڹ�����ʼ�����");
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

        // ��֤���ڰ�ť���
        ValidateWindowButtonComponents();

        Debug.Log("ȫ����������ʼ�����");
    }

    /// <summary>
    /// ��֤���ڰ�ť������
    /// </summary>
    void ValidateWindowButtonComponents()
    {
        if (windowButtonContainer == null)
        {
            Debug.LogError("TaskBar: windowButtonContainer δ���ã�");
        }

        if (windowButtonPrefab == null)
        {
            Debug.LogError("TaskBar: windowButtonPrefab δ���ã�");
        }

        // ���Ԥ�����Ƿ���Button��Text���
        if (windowButtonPrefab != null)
        {
            Button button = windowButtonPrefab.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("TaskBar: windowButtonPrefab ȱ�� Button �����");
            }

            TextMeshProUGUI text = windowButtonPrefab.GetComponentInChildren<TextMeshProUGUI>();
            Text uiText = windowButtonPrefab.GetComponentInChildren<Text>();
            if (text == null && uiText == null)
            {
                Debug.LogError("TaskBar: windowButtonPrefab ȱ�� Text �� TextMeshProUGUI �����");
            }
        }
    }

    #region ���ڰ�ť����

    /// <summary>
    /// ����ע���¼�����
    /// </summary>
    void OnWindowRegistered(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        CreateWindowButton(window, hierarchyInfo);
    }

    /// <summary>
    /// ����ע���¼�����
    /// </summary>
    void OnWindowUnregistered(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        RemoveWindowButton(window);
    }

    /// <summary>
    /// ����ѡ���¼�����
    /// </summary>
    void OnWindowSelected(WindowsWindow window)
    {
        UpdateActiveWindowButton(window);
    }

    /// <summary>
    /// �������ڰ�ť
    /// </summary>
    void CreateWindowButton(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        if (window == null || windowButtonContainer == null || windowButtonPrefab == null)
        {
            Debug.LogError("TaskBar: �������ڰ�ťʧ�� - ȱ�ٱ�Ҫ���");
            return;
        }

        // ����Ƿ��Ѿ����ڰ�ť
        if (windowButtonMap.ContainsKey(window))
        {
            Debug.LogWarning($"TaskBar: ���� {window.Title} �İ�ť�Ѵ���");
            return;
        }

        // ʵ������ť
        GameObject buttonObj = Instantiate(windowButtonPrefab, windowButtonContainer);

        // ���ð�ť�ı�
        UpdateButtonText(buttonObj, window.Title);

        // ���ð�ť����¼�
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnWindowButtonClicked(window));
        }

        // ���ð�ť��ʽ
        SetButtonStyle(buttonObj, false);

        // ����ӳ���ϵ
        windowButtonMap[window] = buttonObj;
        buttonWindowMap[buttonObj] = window;

        Debug.Log($"TaskBar: �Ѵ������ڰ�ť - {window.Title} (�㼶: {hierarchyInfo.containerPath})");
    }

    /// <summary>
    /// �Ƴ����ڰ�ť
    /// </summary>
    void RemoveWindowButton(WindowsWindow window)
    {
        if (window == null || !windowButtonMap.ContainsKey(window))
        {
            return;
        }

        GameObject buttonObj = windowButtonMap[window];

        // ����ӳ���ϵ
        windowButtonMap.Remove(window);
        if (buttonWindowMap.ContainsKey(buttonObj))
        {
            buttonWindowMap.Remove(buttonObj);
        }

        // ����ǵ�ǰ����ڣ��������
        if (currentActiveWindow == window)
        {
            currentActiveWindow = null;
        }

        // ���ٰ�ť����
        if (buttonObj != null)
        {
            Destroy(buttonObj);
        }

        Debug.Log($"TaskBar: ���Ƴ����ڰ�ť - {window.Title}");
    }

    /// <summary>
    /// ���ڰ�ť����¼�
    /// </summary>
    void OnWindowButtonClicked(WindowsWindow window)
    {
        PlayButtonClick();

        if (WindowManager.Instance != null && window != null)
        {
            WindowManager.Instance.ActivateWindow(window);
            Debug.Log($"TaskBar: ͨ����ť����� - {window.Title}");
        }
        else
        {
            Debug.LogError("TaskBar: �޷������ - WindowManager�򴰿�Ϊ��");
        }
    }

    /// <summary>
    /// ���»���ڰ�ť��ʽ
    /// </summary>
    void UpdateActiveWindowButton(WindowsWindow newActiveWindow)
    {
        // ����֮ǰ�Ļ��ť��ʽ
        if (currentActiveWindow != null && windowButtonMap.ContainsKey(currentActiveWindow))
        {
            SetButtonStyle(windowButtonMap[currentActiveWindow], false);
        }

        // �����µĻ��ť��ʽ
        currentActiveWindow = newActiveWindow;
        if (currentActiveWindow != null && windowButtonMap.ContainsKey(currentActiveWindow))
        {
            SetButtonStyle(windowButtonMap[currentActiveWindow], true);
        }
    }

    /// <summary>
    /// ���ð�ť��ʽ
    /// </summary>
    void SetButtonStyle(GameObject buttonObj, bool isActive)
    {
        if (buttonObj == null) return;

        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = isActive ? activeButtonColor : normalButtonColor;
            colors.highlightedColor = hoveredButtonColor;
            colors.pressedColor = activeButtonColor;
            button.colors = colors;
        }
    }

    /// <summary>
    /// ���°�ť�ı�
    /// </summary>
    void UpdateButtonText(GameObject buttonObj, string text)
    {
        if (buttonObj == null) return;

        // ����TextMeshProUGUI
        TextMeshProUGUI tmpText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            return;
        }

        // ����Text���
        Text uiText = buttonObj.GetComponentInChildren<Text>();
        if (uiText != null)
        {
            uiText.text = text;
        }
    }

    /// <summary>
    /// ˢ�����д��ڰ�ť�������ڱ���仯��
    /// </summary>
    public void RefreshAllWindowButtons()
    {
        foreach (var kvp in windowButtonMap)
        {
            WindowsWindow window = kvp.Key;
            GameObject buttonObj = kvp.Value;

            if (window != null && buttonObj != null)
            {
                UpdateButtonText(buttonObj, window.Title);
            }
        }
    }

    #endregion

    #region ԭ�����������ܣ����ֲ��䣩

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
        else
        {
            GameObject actionManagerObj = new GameObject("GlobalActionManager");
            actionManagerObj.transform.SetParent(transform);
            actionManager = actionManagerObj.AddComponent<GlobalActionManager>();
        }

        if (actionManager != null)
        {
            actionManager.InitializeManager();
            Debug.Log("GlobalActionManager ��TaskBar��������ʼ�����");
        }
    }

    public GlobalActionManager GetActionManager()
    {
        if (actionManager == null && !createActionManagerAtStart)
        {
            InitializeActionManager();
        }
        return actionManager;
    }

    void BindMenuButtons()
    {
        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(() => ExecuteAction("BackToMainMenu"));
        newGameButton.onClick.RemoveAllListeners();
        newGameButton.onClick.AddListener(() => ExecuteAction("NewGame"));
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => ExecuteAction("Continue"));
        languageButton.onClick.RemoveAllListeners();
        languageButton.onClick.AddListener(() => ExecuteAction("OpenLanguageSettings"));
        displayButton.onClick.RemoveAllListeners();
        displayButton.onClick.AddListener(() => ExecuteAction("OpenDisplaySettings"));
        creditsButton.onClick.RemoveAllListeners();
        creditsButton.onClick.AddListener(() => ExecuteAction("OpenCredits"));
        shutdownButton.onClick.RemoveAllListeners();
        shutdownButton.onClick.AddListener(() => ExecuteAction("Shutdown"));
    }

    void ExecuteAction(string actionName)
    {
        PlayButtonClick();
        CloseStartMenu();

        if (actionManager == null)
        {
            InitializeActionManager();
        }

        if (actionManager != null && GlobalActionManager.Instance != null)
        {
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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        UpdateMenuButtonsForScene();
        CloseStartMenu();
        Debug.Log($"�����л���: {currentSceneName}");
    }

    void UpdateMenuButtonsForScene()
    {
        bool isMainMenuScene = (currentSceneName == "MainMenu");
        bool isGameScene = currentSceneName == "GameScene";

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(isGameScene);
        }
        if (newGameButton != null)
        {
            newGameButton.gameObject.SetActive(isMainMenuScene);
        }
        if (continueButton != null)
        {
            bool hasGameSave = GlobalSystemManager.Instance != null &&
                              GlobalSystemManager.Instance.HasGameSave();
            continueButton.gameObject.SetActive(isMainMenuScene);
            continueButton.interactable = hasGameSave;
        }
        Debug.Log($"TaskBar��ť״̬���� - MainMenu: {isMainMenuScene}, Game: {isGameScene}");
    }

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

    public void OpenStartMenu()
    {
        if (startPanel != null && !isStartMenuOpen)
        {
            PlaySound(menuOpenSound);
            startPanel.SetActive(true);
            isStartMenuOpen = true;
            UpdateMenuButtonsForScene();
            Debug.Log("��ʼ�˵��Ѵ�");
        }
    }

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

    void PlaySound(AudioClip clip)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.PlaySFX(clip);
        }
    }

    void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isStartMenuOpen)
        {
            CloseStartMenu();
        }
    }

    #endregion
}