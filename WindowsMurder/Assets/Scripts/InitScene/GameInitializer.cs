using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 游戏初始化器 - 游戏启动的第一个场景
/// 负责创建全局系统、跳转主菜单
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("场景设置")]
    public string mainMenuSceneName = "MainMenu";

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

        // 2. 创建全局系统
        CreateGlobalSystem();

        // 3. 等待全局系统完全加载
        yield return StartCoroutine(WaitForSystemReady());

        // 4. 跳转到主菜单
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