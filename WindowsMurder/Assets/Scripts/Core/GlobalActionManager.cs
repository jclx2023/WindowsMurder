using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 全局功能管理器 - 专注于业务逻辑处理
/// 由GlobalTaskBarManager管理，处理用户操作的具体业务流程
/// </summary>
public class GlobalActionManager : MonoBehaviour
{
    public static GlobalActionManager Instance;

    [Header("窗口预设体")]
    public GameObject languageSettingsPrefab;
    public GameObject displaySettingsPrefab;
    public GameObject creditsWindowPrefab;

    [Header("音效")]
    public AudioClip windowOpenSound;
    public AudioClip sceneTransitionSound;

    void Awake()
    {
        // 检查是否被TaskBar管理
        if (GlobalTaskBarManager.Instance == null)
        {
            Debug.LogError("GlobalActionManager: 应该由GlobalTaskBarManager创建和管理！");
            Destroy(gameObject);
            return;
        }

        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 初始化管理器（由GlobalTaskBarManager调用）
    /// </summary>
    public void InitializeManager()
    {
        Debug.Log("GlobalActionManager 初始化完成，等待处理用户交互");
    }

    /// <summary>
    /// 播放音效 - 委托给GlobalSystemManager
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.PlaySFX(clip);
        }
    }

    /// <summary>
    /// 创建窗口的通用方法
    /// </summary>
    GameObject CreateWindow(GameObject windowPrefab)
    {
        // 查找当前场景的Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("GlobalActionManager: 找不到Canvas，无法创建窗口");
            return null;
        }

        // 创建窗口
        GameObject window = Instantiate(windowPrefab, canvas.transform);
        PlaySound(windowOpenSound);

        Debug.Log($"GlobalActionManager: 创建了窗口");
        return window;
    }

    // ==================== 全局功能接口 ====================

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void BackToMainMenu()
    {
        Debug.Log("GlobalActionManager: 返回主菜单");
        PlaySound(sceneTransitionSound);
        StartCoroutine(LoadSceneAsync("MainMenu"));
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void NewGame()
    {
        Debug.Log("GlobalActionManager: 开始新游戏");

        // 通过GlobalSystemManager删除旧存档
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.DeleteSave();
        }

        PlaySound(sceneTransitionSound);
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void Continue()
    {
        if (!HasGameSave())
        {
            Debug.LogWarning("GlobalActionManager: 无法继续游戏，没有找到存档");
            // 可以在这里显示提示窗口
            return;
        }

        Debug.Log("GlobalActionManager: 继续游戏");
        PlaySound(sceneTransitionSound);
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// 打开语言设置
    /// </summary>
    public void OpenLanguageSettings()
    {
        Debug.Log("GlobalActionManager: 打开语言设置");
        CreateWindow(languageSettingsPrefab);
    }

    /// <summary>
    /// 打开显示设置
    /// </summary>
    public void OpenDisplaySettings()
    {
        Debug.Log("GlobalActionManager: 打开显示设置");
        CreateWindow(displaySettingsPrefab);
    }

    /// <summary>
    /// 打开制作人员名单
    /// </summary>
    public void OpenCredits()
    {
        Debug.Log("GlobalActionManager: 打开制作人员名单");
        CreateWindow(creditsWindowPrefab);
    }

    /// <summary>
    /// 关机/退出游戏
    /// </summary>
    public void Shutdown()
    {
        Debug.Log("GlobalActionManager: 退出游戏");

        // 通过GlobalSystemManager退出
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.QuitApplication();
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

    // ==================== 状态查询接口（委托给GlobalSystemManager） ====================

    /// <summary>
    /// 检查是否有游戏存档
    /// </summary>
    public bool HasGameSave()
    {
        if (GlobalSystemManager.Instance != null)
        {
            return GlobalSystemManager.Instance.HasGameSave();
        }
        return false;
    }

    /// <summary>
    /// 获取当前场景名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// 检查是否在主菜单场景
    /// </summary>
    public bool IsInMainMenuScene()
    {
        return GetCurrentSceneName() == "MainMenu";
    }

    /// <summary>
    /// 检查是否在游戏场景
    /// </summary>
    public bool IsInGameScene()
    {
        string sceneName = GetCurrentSceneName();
        return sceneName.Contains("Game") || sceneName == "GameScene";
    }

    // ==================== 场景管理 ====================

    /// <summary>
    /// 异步加载场景
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"GlobalActionManager: 开始加载场景 {sceneName}");

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        Debug.Log($"GlobalActionManager: 场景 {sceneName} 加载完成");
    }

    // ==================== 便捷方法（委托给GlobalSystemManager） ====================

    /// <summary>
    /// 获取翻译文本
    /// </summary>
    public string GetText(string key)
    {
        if (GlobalSystemManager.Instance != null)
        {
            return GlobalSystemManager.Instance.GetText(key);
        }
        return key;
    }

    /// <summary>
    /// 设置语言
    /// </summary>
    public void SetLanguage(SupportedLanguage language)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetLanguage(language);
        }
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    public void SetVolume(float master, float sfx, float music)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetVolume(master, sfx, music);
        }
    }

    /// <summary>
    /// 设置显示模式
    /// </summary>
    public void SetDisplay(bool fullscreen, Vector2Int resolution)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetDisplay(fullscreen, resolution);
        }
    }
}