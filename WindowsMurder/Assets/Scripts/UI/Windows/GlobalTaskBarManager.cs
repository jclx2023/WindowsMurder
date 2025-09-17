using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 全局任务栏管理器 - 管理开始菜单和任务栏功能
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

    // 私有变量
    private bool isStartMenuOpen = false;
    private AudioSource audioSource;
    private string currentSceneName;

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
        // 监听场景变化
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 初始化当前场景
        currentSceneName = SceneManager.GetActiveScene().name;
        UpdateMenuButtonsForScene();
    }

    void OnDestroy()
    {
        // 取消场景监听
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 初始化任务栏
    /// </summary>
    void InitializeTaskBar()
    {
        // 获取音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

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

        Debug.Log("全局任务栏初始化完成");
    }

    /// <summary>
    /// 绑定开始菜单按钮事件
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
                              GlobalSystemManager.Instance.hasGameSave;
            continueButton.gameObject.SetActive(isMainMenuScene);
            continueButton.interactable = hasGameSave;
        }

        Debug.Log($"按钮状态更新 - MainMenu: {isMainMenuScene}, Game: {isGameScene}");
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
    /// 播放音效
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    void PlayButtonClick()
    {
        PlaySound(buttonClickSound);
    }

    // ==================== 按钮事件处理 ====================

    /// <summary>
    /// 返回主菜单
    /// </summary>
    void OnMainMenuClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("返回主菜单");
        StartCoroutine(LoadSceneAsync("MainMenu"));
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    void OnNewGameClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        // 清除之前的存档
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.DeleteSave();
        }

        Debug.Log("开始新游戏");
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    void OnContinueClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        // 检查是否有存档
        if (GlobalSystemManager.Instance != null && GlobalSystemManager.Instance.hasGameSave)
        {
            Debug.Log("继续游戏");
            StartCoroutine(LoadSceneAsync("GameScene"));
        }
        else
        {
            Debug.LogWarning("没有找到存档文件");
            // 可以显示提示信息
        }
    }

    /// <summary>
    /// 语言设置
    /// </summary>
    void OnLanguageClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("打开语言设置");
        // TODO: 实现语言设置窗口
        // LanguageSettingsWindow.Show();
    }

    /// <summary>
    /// 显示设置
    /// </summary>
    void OnDisplayClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("打开显示设置");
        // TODO: 实现显示设置窗口
        // DisplaySettingsWindow.Show();
    }

    /// <summary>
    /// 制作人员
    /// </summary>
    void OnCreditsClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("显示制作人员信息");
        // TODO: 实现制作人员窗口
        // CreditsWindow.Show();
    }

    /// <summary>
    /// 关机/退出游戏
    /// </summary>
    void OnShutdownClicked()
    {
        PlayButtonClick();
        CloseStartMenu();

        Debug.Log("退出游戏");

        // 调用全局系统的退出方法
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.QuitGame();
        }
        else
        {
            // 备用退出方法
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        // 可以在这里添加加载画面
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOperation.isDone)
        {
            // 可以更新加载进度
            yield return null;
        }
    }

    // ==================== 外部调用接口 ====================

    /// <summary>
    /// 外部调用：强制关闭开始菜单
    /// </summary>
    public void ForceCloseStartMenu()
    {
        CloseStartMenu();
    }

    /// <summary>
    /// 外部调用：检查开始菜单是否打开
    /// </summary>
    public bool IsStartMenuOpen()
    {
        return isStartMenuOpen;
    }

    /// <summary>
    /// 外部调用：刷新按钮状态
    /// </summary>
    public void RefreshButtonStates()
    {
        UpdateMenuButtonsForScene();
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