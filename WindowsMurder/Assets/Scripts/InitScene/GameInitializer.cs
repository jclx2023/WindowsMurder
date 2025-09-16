using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏初始化器 - 游戏启动的第一个场景
/// 负责WinXP开机动画、创建全局系统、跳转主菜单
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("场景设置")]
    public string mainMenuSceneName = "MainMenu";

    [Header("开机动画")]
    public GameObject bootAnimationPanel;
    public UnityEngine.UI.Slider progressBar;
    public TMPro.TextMeshProUGUI loadingText;
    public float bootAnimationDuration = 3f;

    [Header("系统加载")]
    public float systemLoadingDelay = 1f; // 等待全局系统加载完成的延迟

    [Header("音效")]
    public AudioClip startupSound;

    [Header("全局系统")]
    public GameObject globalSystemPrefab; // 全局系统预制体

    // 私有变量
    private AudioSource audioSource;

    void Start()
    {
        // 获取音频组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 开始初始化流程
        StartCoroutine(InitializationSequence());
    }

    /// <summary>
    /// 完整的初始化序列
    /// </summary>
    IEnumerator InitializationSequence()
    {
        Debug.Log("游戏初始化开始...");

        // 1. 播放开机音效
        PlayStartupSound();

        // 2. 显示开机动画面板
        if (bootAnimationPanel != null)
            bootAnimationPanel.SetActive(true);

        // 3. 播放WinXP开机动画
        yield return StartCoroutine(PlayBootAnimation());

        // 4. 创建全局系统
        CreateGlobalSystem();

        // 5. 等待全局系统完全加载
        yield return StartCoroutine(WaitForSystemReady());

        // 6. 跳转到主菜单
        yield return StartCoroutine(TransitionToMainMenu());
    }

    /// <summary>
    /// 播放开机音效
    /// </summary>
    void PlayStartupSound()
    {
        if (startupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startupSound);
            Debug.Log("播放Windows启动音效");
        }
    }

    /// <summary>
    /// 播放WinXP开机动画
    /// </summary>
    IEnumerator PlayBootAnimation()
    {
        float elapsedTime = 0f;

        // 开机动画的加载文本
        string[] loadingTexts = {
            "正在启动 Windows XP...",
            "正在加载系统组件...",
            "正在初始化设备...",
            "正在准备桌面...",
            "启动完成"
        };

        Debug.Log("开始播放开机动画");

        while (elapsedTime < bootAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / bootAnimationDuration;

            // 更新进度条
            if (progressBar != null)
                progressBar.value = progress;

            // 更新加载文本
            if (loadingText != null)
            {
                int textIndex = Mathf.Min(
                    Mathf.FloorToInt(progress * loadingTexts.Length),
                    loadingTexts.Length - 1
                );
                loadingText.text = loadingTexts[textIndex];
            }

            yield return null;
        }

        // 确保动画完成
        if (progressBar != null) progressBar.value = 1f;
        if (loadingText != null) loadingText.text = loadingTexts[loadingTexts.Length - 1];

        Debug.Log("开机动画播放完成");
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>
    /// 创建全局系统
    /// </summary>
    void CreateGlobalSystem()
    {
        Debug.Log("开始创建全局系统...");

        GameObject globalSystem = null;

        // 尝试从预制体创建
        if (globalSystemPrefab != null)
        {
            globalSystem = Instantiate(globalSystemPrefab);
            globalSystem.name = "GlobalSystemManager"; // 去掉(Clone)后缀
            Debug.Log("从预制体创建全局系统");
        }
        else
        {
            // 尝试从Resources加载
            GameObject prefab = Resources.Load<GameObject>("Prefabs/GlobalSystemManager");
            if (prefab != null)
            {
                globalSystem = Instantiate(prefab);
                globalSystem.name = "GlobalSystemManager";
                Debug.Log("从Resources加载全局系统");
            }
            else
            {
                // 直接创建GameObject并添加组件
                globalSystem = new GameObject("GlobalSystemManager");
                globalSystem.AddComponent<GlobalSystemManager>();
                Debug.Log("直接创建全局系统对象");
            }
        }

        // 设置为跨场景不销毁
        if (globalSystem != null)
        {
            DontDestroyOnLoad(globalSystem);
            Debug.Log("全局系统创建成功，已设置为DontDestroyOnLoad");
        }
        else
        {
            Debug.LogError("全局系统创建失败！");
        }
    }

    /// <summary>
    /// 等待全局系统准备就绪
    /// </summary>
    IEnumerator WaitForSystemReady()
    {
        Debug.Log("等待全局系统初始化完成...");

        // 等待固定时间，确保GlobalSystemManager的Awake和Start完成
        yield return new WaitForSeconds(systemLoadingDelay);

        // 检查GlobalSystemManager是否存在并初始化完成
        GlobalSystemManager globalManager = FindObjectOfType<GlobalSystemManager>();

        if (globalManager != null && GlobalSystemManager.Instance != null)
        {
            Debug.Log("全局系统初始化完成");

            // 可以在这里更新加载文本
            if (loadingText != null)
                loadingText.text = "系统准备就绪";
        }
        else
        {
            Debug.LogWarning("全局系统可能未正确初始化");
        }

        // 额外的安全延迟
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 跳转到主菜单场景
    /// </summary>
    IEnumerator TransitionToMainMenu()
    {
        Debug.Log("准备跳转到主菜单...");

        // 更新UI反馈
        if (loadingText != null)
            loadingText.text = "正在进入桌面...";

        // 隐藏开机动画（可选，也可以淡出）
        if (bootAnimationPanel != null)
            bootAnimationPanel.SetActive(false);

        // 短暂延迟，模拟真实的系统响应时间
        yield return new WaitForSeconds(0.5f);

        // 加载主菜单场景
        Debug.Log($"加载场景: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// 应用程序暂停时的处理（可选）
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("游戏初始化过程中应用暂停");
        }
    }

    /// <summary>
    /// 应用程序焦点变化时的处理（可选）
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Debug.Log("游戏初始化过程中失去焦点");
        }
    }
}