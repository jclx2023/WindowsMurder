using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 全局任务栏管理器 - 集成窗口管理功能
/// 负责TaskBar UI显示、用户交互和窗口按钮管理
/// </summary>
public class GlobalTaskBarManager : MonoBehaviour
{
    public static GlobalTaskBarManager Instance;

    [Header("任务栏组件")]
    public Button startButton;
    public GameObject startPanel;

    [Header("开始菜单按钮")]
    public Button mainMenuButton;
    public Button newGameButton;
    public Button continueButton;
    public Button languageButton;
    public Button displayButton;
    public Button creditsButton;
    public Button shutdownButton;

    [Header("窗口按钮管理")]
    public Transform windowButtonContainer;    // 窗口按钮容器（已设置HorizontalLayout）
    public GameObject windowButtonPrefab;      // 窗口按钮预制体
    public int maxVisibleButtons = 10;         // 最大显示按钮数（可选）

    [Header("按钮样式设置")]
    public Color normalButtonColor = Color.white;
    public Color activeButtonColor = Color.cyan;
    public Color hoveredButtonColor = Color.gray;

    [Header("音效")]
    public AudioClip buttonClickSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;

    [Header("ActionManager设置")]
    public GameObject globalActionManagerPrefab;
    public bool createActionManagerAtStart = true;

    // 私有变量
    private bool isStartMenuOpen = false;
    private string currentSceneName;
    private GlobalActionManager actionManager;

    // 窗口按钮管理
    private Dictionary<WindowsWindow, GameObject> windowButtonMap = new Dictionary<WindowsWindow, GameObject>();
    private Dictionary<GameObject, WindowsWindow> buttonWindowMap = new Dictionary<GameObject, WindowsWindow>();
    private WindowsWindow currentActiveWindow;

    void Awake()
    {
        // 单例模式
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
        // 等待GlobalSystemManager初始化完成
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
        // 取消场景监听和系统事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (GlobalSystemManager.OnSystemInitialized != null)
        {
            GlobalSystemManager.OnSystemInitialized -= OnSystemReady;
        }

        // 取消窗口管理器事件监听
        if (WindowManager.Instance != null)
        {
            WindowManager.OnWindowRegistered -= OnWindowRegistered;
            WindowManager.OnWindowUnregistered -= OnWindowUnregistered;
        }

        // 取消窗口选择事件监听
        WindowsWindow.OnWindowSelected -= OnWindowSelected;
    }

    /// <summary>
    /// 系统就绪后的初始化
    /// </summary>
    void OnSystemReady()
    {
        // 监听场景变化
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 监听窗口管理器事件
        if (WindowManager.Instance != null)
        {
            WindowManager.OnWindowRegistered += OnWindowRegistered;
            WindowManager.OnWindowUnregistered += OnWindowUnregistered;
        }

        // 监听窗口选择事件
        WindowsWindow.OnWindowSelected += OnWindowSelected;

        // 初始化当前场景
        currentSceneName = SceneManager.GetActiveScene().name;
        UpdateMenuButtonsForScene();

        Debug.Log("TaskBar系统（含窗口管理）初始化完成");
    }

    /// <summary>
    /// 初始化任务栏
    /// </summary>
    void InitializeTaskBar()
    {
        // 初始状态下关闭开始菜单
        if (startPanel != null)
            startPanel.SetActive(false);

        // 绑定开始按钮事件
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(ToggleStartMenu);
        }

        // 绑定菜单按钮事件
        BindMenuButtons();

        // 初始化ActionManager
        if (createActionManagerAtStart)
        {
            InitializeActionManager();
        }

        // 验证窗口按钮组件
        ValidateWindowButtonComponents();

        Debug.Log("全局任务栏初始化完成");
    }

    /// <summary>
    /// 验证窗口按钮相关组件
    /// </summary>
    void ValidateWindowButtonComponents()
    {
        if (windowButtonContainer == null)
        {
            Debug.LogError("TaskBar: windowButtonContainer 未设置！");
        }

        if (windowButtonPrefab == null)
        {
            Debug.LogError("TaskBar: windowButtonPrefab 未设置！");
        }

        // 检查预制体是否有Button和Text组件
        if (windowButtonPrefab != null)
        {
            Button button = windowButtonPrefab.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("TaskBar: windowButtonPrefab 缺少 Button 组件！");
            }

            TextMeshProUGUI text = windowButtonPrefab.GetComponentInChildren<TextMeshProUGUI>();
            Text uiText = windowButtonPrefab.GetComponentInChildren<Text>();
            if (text == null && uiText == null)
            {
                Debug.LogError("TaskBar: windowButtonPrefab 缺少 Text 或 TextMeshProUGUI 组件！");
            }
        }
    }

    #region 窗口按钮管理

    /// <summary>
    /// 窗口注册事件处理
    /// </summary>
    void OnWindowRegistered(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        CreateWindowButton(window, hierarchyInfo);
    }

    /// <summary>
    /// 窗口注销事件处理
    /// </summary>
    void OnWindowUnregistered(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        RemoveWindowButton(window);
    }

    /// <summary>
    /// 窗口选择事件处理
    /// </summary>
    void OnWindowSelected(WindowsWindow window)
    {
        UpdateActiveWindowButton(window);
    }

    /// <summary>
    /// 创建窗口按钮
    /// </summary>
    void CreateWindowButton(WindowsWindow window, WindowHierarchyInfo hierarchyInfo)
    {
        if (window == null || windowButtonContainer == null || windowButtonPrefab == null)
        {
            Debug.LogError("TaskBar: 创建窗口按钮失败 - 缺少必要组件");
            return;
        }

        // 检查是否已经存在按钮
        if (windowButtonMap.ContainsKey(window))
        {
            Debug.LogWarning($"TaskBar: 窗口 {window.Title} 的按钮已存在");
            return;
        }

        // 实例化按钮
        GameObject buttonObj = Instantiate(windowButtonPrefab, windowButtonContainer);

        // 设置按钮文本
        UpdateButtonText(buttonObj, window.Title);

        // 配置按钮点击事件
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnWindowButtonClicked(window));
        }

        // 设置按钮样式
        SetButtonStyle(buttonObj, false);

        // 保存映射关系
        windowButtonMap[window] = buttonObj;
        buttonWindowMap[buttonObj] = window;

        Debug.Log($"TaskBar: 已创建窗口按钮 - {window.Title} (层级: {hierarchyInfo.containerPath})");
    }

    /// <summary>
    /// 移除窗口按钮
    /// </summary>
    void RemoveWindowButton(WindowsWindow window)
    {
        if (window == null || !windowButtonMap.ContainsKey(window))
        {
            return;
        }

        GameObject buttonObj = windowButtonMap[window];

        // 清理映射关系
        windowButtonMap.Remove(window);
        if (buttonWindowMap.ContainsKey(buttonObj))
        {
            buttonWindowMap.Remove(buttonObj);
        }

        // 如果是当前活动窗口，清除引用
        if (currentActiveWindow == window)
        {
            currentActiveWindow = null;
        }

        // 销毁按钮对象
        if (buttonObj != null)
        {
            Destroy(buttonObj);
        }

        Debug.Log($"TaskBar: 已移除窗口按钮 - {window.Title}");
    }

    /// <summary>
    /// 窗口按钮点击事件
    /// </summary>
    void OnWindowButtonClicked(WindowsWindow window)
    {
        PlayButtonClick();

        if (WindowManager.Instance != null && window != null)
        {
            WindowManager.Instance.ActivateWindow(window);
            Debug.Log($"TaskBar: 通过按钮激活窗口 - {window.Title}");
        }
        else
        {
            Debug.LogError("TaskBar: 无法激活窗口 - WindowManager或窗口为空");
        }
    }

    /// <summary>
    /// 更新活动窗口按钮样式
    /// </summary>
    void UpdateActiveWindowButton(WindowsWindow newActiveWindow)
    {
        // 重置之前的活动按钮样式
        if (currentActiveWindow != null && windowButtonMap.ContainsKey(currentActiveWindow))
        {
            SetButtonStyle(windowButtonMap[currentActiveWindow], false);
        }

        // 设置新的活动按钮样式
        currentActiveWindow = newActiveWindow;
        if (currentActiveWindow != null && windowButtonMap.ContainsKey(currentActiveWindow))
        {
            SetButtonStyle(windowButtonMap[currentActiveWindow], true);
        }
    }

    /// <summary>
    /// 设置按钮样式
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
    /// 更新按钮文本
    /// </summary>
    void UpdateButtonText(GameObject buttonObj, string text)
    {
        if (buttonObj == null) return;

        // 尝试TextMeshProUGUI
        TextMeshProUGUI tmpText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = text;
            return;
        }

        // 尝试Text组件
        Text uiText = buttonObj.GetComponentInChildren<Text>();
        if (uiText != null)
        {
            uiText.text = text;
        }
    }

    /// <summary>
    /// 刷新所有窗口按钮（处理窗口标题变化）
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

    #region 原有任务栏功能（保持不变）

    /// <summary>
    /// 初始化ActionManager
    /// </summary>
    void InitializeActionManager()
    {
        if (actionManager != null)
        {
            Debug.LogWarning("GlobalActionManager已经存在，跳过初始化");
            return;
        }

        if (globalActionManagerPrefab != null)
        {
            GameObject actionManagerObj = Instantiate(globalActionManagerPrefab, transform);
            actionManager = actionManagerObj.GetComponent<GlobalActionManager>();

            if (actionManager == null)
            {
                Debug.LogError("预设体中没有找到GlobalActionManager组件");
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
            Debug.Log("GlobalActionManager 由TaskBar创建并初始化完成");
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
                    Debug.LogWarning($"TaskBar: 未知的操作 {actionName}");
                    break;
            }
        }
        else
        {
            Debug.LogError("TaskBar: GlobalActionManager 未正确初始化，无法执行操作");
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        UpdateMenuButtonsForScene();
        CloseStartMenu();
        Debug.Log($"场景切换至: {currentSceneName}");
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
        Debug.Log($"TaskBar按钮状态更新 - MainMenu: {isMainMenuScene}, Game: {isGameScene}");
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
            Debug.Log("开始菜单已打开");
        }
    }

    public void CloseStartMenu()
    {
        if (startPanel != null && isStartMenuOpen)
        {
            PlaySound(menuCloseSound);
            startPanel.SetActive(false);
            isStartMenuOpen = false;
            Debug.Log("开始菜单已关闭");
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