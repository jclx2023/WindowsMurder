using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 全局任务栏管理器 - 管理UI交互和ActionManager
/// 负责TaskBar UI显示和用户交互，管理GlobalActionManager处理业务逻辑
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

    [Header("音效")]
    public AudioClip buttonClickSound;
    public AudioClip menuOpenSound;
    public AudioClip menuCloseSound;

    [Header("ActionManager设置")]
    public GameObject globalActionManagerPrefab; // GlobalActionManager预设体
    public bool createActionManagerAtStart = true; // 是否在启动时自动创建

    // 私有变量
    private bool isStartMenuOpen = false;
    private string currentSceneName;
    private GlobalActionManager actionManager;

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
            // 如果系统已初始化，直接开始
            OnSystemReady();
        }
        else
        {
            // 等待系统初始化完成
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
    }

    /// <summary>
    /// 系统就绪后的初始化
    /// </summary>
    void OnSystemReady()
    {
        // 监听场景变化
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 初始化当前场景
        currentSceneName = SceneManager.GetActiveScene().name;
        UpdateMenuButtonsForScene();

        Debug.Log("TaskBar系统初始化完成");
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

        Debug.Log("全局任务栏初始化完成");
    }

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

        // 方式1：通过预设体创建
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
        // 方式2：动态创建
        else
        {
            GameObject actionManagerObj = new GameObject("GlobalActionManager");
            actionManagerObj.transform.SetParent(transform);
            actionManager = actionManagerObj.AddComponent<GlobalActionManager>();
        }

        // 确保ActionManager初始化
        if (actionManager != null)
        {
            actionManager.InitializeManager();
            Debug.Log("GlobalActionManager 由TaskBar创建并初始化完成");
        }
    }

    /// <summary>
    /// 获取ActionManager实例（懒加载）
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
    /// 绑定开始菜单按钮事件 - 通过ActionManager处理业务逻辑
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
    /// 执行操作的统一入口 - 处理音效和菜单关闭，然后调用ActionManager
    /// </summary>
    void ExecuteAction(string actionName)
    {
        PlayButtonClick();
        CloseStartMenu();

        // 确保ActionManager存在
        if (actionManager == null)
        {
            InitializeActionManager();
        }

        // 检查ActionManager和GlobalActionManager.Instance
        if (actionManager != null && GlobalActionManager.Instance != null)
        {
            // 根据操作名称调用相应的ActionManager方法
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

    /// <summary>
    /// 场景加载时的回调
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneName = scene.name;
        UpdateMenuButtonsForScene();

        // 场景切换时关闭开始菜单
        CloseStartMenu();

        Debug.Log($"场景切换至: {currentSceneName}");
    }

    /// <summary>
    /// 根据当前场景更新菜单按钮状态
    /// </summary>
    void UpdateMenuButtonsForScene()
    {
        bool isMainMenuScene = (currentSceneName == "MainMenu");
        bool isGameScene = (currentSceneName.Contains("Game") || currentSceneName == "GameScene");

        // 主菜单按钮：仅在游戏场景中显示
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(isGameScene);
        }

        // 新游戏按钮：仅在主菜单场景显示
        if (newGameButton != null)
        {
            newGameButton.gameObject.SetActive(isMainMenuScene);
        }

        // 继续游戏按钮：仅在主菜单场景显示，且需要检查存档
        if (continueButton != null)
        {
            bool hasGameSave = GlobalSystemManager.Instance != null &&
                              GlobalSystemManager.Instance.HasGameSave();
            continueButton.gameObject.SetActive(isMainMenuScene);
            continueButton.interactable = hasGameSave;
        }

        Debug.Log($"TaskBar按钮状态更新 - MainMenu: {isMainMenuScene}, Game: {isGameScene}");
    }

    /// <summary>
    /// 切换开始菜单显示状态
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
    /// 打开开始菜单
    /// </summary>
    public void OpenStartMenu()
    {
        if (startPanel != null && !isStartMenuOpen)
        {
            PlaySound(menuOpenSound);
            startPanel.SetActive(true);
            isStartMenuOpen = true;

            // 更新按钮状态
            UpdateMenuButtonsForScene();

            Debug.Log("开始菜单已打开");
        }
    }

    /// <summary>
    /// 关闭开始菜单
    /// </summary>
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

    /// <summary>
    /// 播放音效 - 通过GlobalSystemManager
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.PlaySFX(clip);
        }
    }

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }
    // ==================== Unity事件 ====================

    void Update()
    {
        // ESC键关闭开始菜单
        if (Input.GetKeyDown(KeyCode.Escape) && isStartMenuOpen)
        {
            CloseStartMenu();
        }

        // 点击空白区域关闭菜单（可选）
        if (isStartMenuOpen && Input.GetMouseButtonDown(0))
        {
            // 检查鼠标是否点击在菜单外部
            // 这里需要根据实际UI布局来判断
        }
    }
}