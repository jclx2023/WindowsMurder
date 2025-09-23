using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局功能管理器 - 整合转场效果和业务逻辑处理
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

    [Header("转场效果设置")]
    public float clearPixel = 500f;     // 清晰态
    public float blurPixel = 6f;        // 模糊态（像素化程度，数值越小越糊）
    public float preDuration = 0.5f;   // 离场前变糊时长
    public float postDuration = 0.55f;  // 入场后变清晰时长
    public float jitterDuring = 0.9f;   // 过渡中抖动强度
    public float jitterClear = 0.9f;    // 稳定后抖动强度
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 运行时携带到新场景的初始值（保证无缝）
    private float carryPixel;
    private float carryJitter;
    private bool postSceneReady;
    private Material postSceneMat; // 新场景找到的材质

    // 标记是否是新游戏还是继续游戏
    private bool isNewGame = false;
    private bool isContinueGame = false;

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

        // 确保SaveManager存在
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("GlobalActionManager: SaveManager未初始化，尝试创建");
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
        }
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

        // 如果在游戏场景，先保存进度
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                Debug.Log("GlobalActionManager: 已保存当前游戏进度");
            }
        }

        // 重置标记
        isNewGame = false;
        isContinueGame = false;

        PlaySound(sceneTransitionSound);
        SwitchSceneWithEffect("MainMenu");
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void NewGame()
    {
        Debug.Log("GlobalActionManager: 开始新游戏");

        // 删除旧存档
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            Debug.Log("GlobalActionManager: 已删除旧存档");
        }

        // 同时通过GlobalSystemManager删除（保持兼容）
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.DeleteSave();
        }

        // 设置标记
        isNewGame = true;
        isContinueGame = false;

        PlaySound(sceneTransitionSound);
        SwitchSceneWithEffect("GameScene");
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void Continue()
    {
        // 检查是否有存档
        bool hasSave = false;

        // 优先使用SaveManager检查
        if (SaveManager.Instance != null)
        {
            hasSave = SaveManager.Instance.HasSaveData();
        }
        // 备用：通过GlobalSystemManager检查
        else if (GlobalSystemManager.Instance != null)
        {
            hasSave = GlobalSystemManager.Instance.HasGameSave();
        }

        if (!hasSave)
        {
            Debug.LogWarning("GlobalActionManager: 无法继续游戏，没有找到存档");
            // TODO: 可以在这里显示提示窗口
            return;
        }

        Debug.Log("GlobalActionManager: 继续游戏");

        // 加载存档数据
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.LoadGame())
            {
                Debug.Log("GlobalActionManager: 存档数据已加载到内存");
            }
            else
            {
                Debug.LogError("GlobalActionManager: 加载存档失败");
                return;
            }
        }

        // 设置标记
        isNewGame = false;
        isContinueGame = true;

        PlaySound(sceneTransitionSound);
        SwitchSceneWithEffect("GameScene");
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

        // 退出前保存游戏（如果在游戏场景）
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                Debug.Log("GlobalActionManager: 退出前已保存游戏");
            }
        }

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

    // ==================== 状态查询接口 ====================

    /// <summary>
    /// 检查是否有游戏存档
    /// </summary>
    public bool HasGameSave()
    {
        // 优先使用SaveManager
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.HasSaveData();
        }
        // 备用：使用GlobalSystemManager
        else if (GlobalSystemManager.Instance != null)
        {
            return GlobalSystemManager.Instance.HasGameSave();
        }
        return false;
    }

    /// <summary>
    /// 获取当前游戏模式（新游戏/继续游戏）
    /// </summary>
    public bool IsNewGame() { return isNewGame; }
    public bool IsContinueGame() { return isContinueGame; }

    // ==================== 转场效果系统 ====================

    /// <summary>
    /// 带转场效果的场景切换
    /// </summary>
    public void SwitchSceneWithEffect(string sceneName)
    {
        StartCoroutine(SceneTransitionCoroutine(sceneName));
    }

    private IEnumerator SceneTransitionCoroutine(string sceneName)
    {
        Debug.Log($"GlobalActionManager: 开始转场切换到 {sceneName}");

        // 1) 离开场景前：找到当前场景的 MainMenuBack 并把它从清晰→模糊
        Material curMat = FindMainMenuBackMaterial();

        float startPixel = GetFloatSafe(curMat, "_PixelResolution", clearPixel);
        float startJitter = GetFloatSafe(curMat, "_JitterStrength", jitterClear);

        // 预过渡：清晰 → 模糊
        yield return AnimateTwoFloats(
            curMat,
            "_PixelResolution", startPixel, blurPixel,
            "_JitterStrength", startJitter, Mathf.Max(jitterDuring, startJitter),
            preDuration, ease
        );

        // 2) 异步加载新场景，准备把新场景的 MainMenuBack 初始化为"模糊态"
        carryPixel = blurPixel;
        carryJitter = jitterDuring;
        postSceneReady = false;
        postSceneMat = null;

        SceneManager.sceneLoaded += OnSceneLoadedSetInitial;

        var async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;
        while (async.progress < 0.9f) // 等待加载完全（Unity 的 0.9 表示已完成，待激活）
            yield return null;

        // 允许场景激活，激活后会立刻触发 sceneLoaded 回调
        async.allowSceneActivation = true;

        // 等待我们在回调里把新场景的 MainMenuBack 初始化好
        while (!postSceneReady)
            yield return null;

        SceneManager.sceneLoaded -= OnSceneLoadedSetInitial;

        // 3) 入场后：把新场景的 MainMenuBack 从模糊→清晰
        if (postSceneMat != null)
        {
            yield return AnimateTwoFloats(
                postSceneMat,
                "_PixelResolution", carryPixel, clearPixel,
                "_JitterStrength", carryJitter, jitterClear,
                postDuration, ease
            );

            // 确保最终状态正确设置
            postSceneMat.SetFloat("_PixelResolution", clearPixel);
            postSceneMat.SetFloat("_JitterStrength", jitterClear);
            Debug.Log($"转场完成，最终状态: PixelResolution={clearPixel}, JitterStrength={jitterClear}");
        }

        // 4) 场景切换完成后的额外处理
        if (sceneName == "GameScene")
        {
            // 通知SaveManager场景已加载完成
            // SaveManager会在OnSceneLoaded中自动处理恢复逻辑
            Debug.Log($"GlobalActionManager: GameScene加载完成，模式: 新游戏={isNewGame}, 继续={isContinueGame}");
        }

        // 场景切换完成后重置标记
        if (sceneName == "MainMenu")
        {
            isNewGame = false;
            isContinueGame = false;
        }
    }

    // 在新场景激活时调用：先找到 MainMenuBack 并把材质直接设为"模糊起点"，避免一帧闪清晰
    private void OnSceneLoadedSetInitial(Scene scene, LoadSceneMode mode)
    {
        var mat = FindMainMenuBackMaterial();
        if (mat != null)
        {
            SetFloatSafe(mat, "_PixelResolution", carryPixel);
            SetFloatSafe(mat, "_JitterStrength", carryJitter);
            postSceneMat = mat;
        }
        postSceneReady = true;
    }

    // ==================== 转场效果工具方法 ====================

    private Material FindMainMenuBackMaterial()
    {
        // 先用 GameObject.Find（只找激活对象）
        GameObject go = GameObject.Find("MainMenuBack");
        Image img = null;

        if (go != null) img = go.GetComponent<Image>();

        // 确保为当前对象创建独立实例，避免改到 sharedMaterial
        //（只在需要控制动画的对象上这样做）
        img.material = new Material(img.material);
        return img.material;
    }

    private IEnumerator AnimateTwoFloats(
        Material mat,
        string propA, float fromA, float toA,
        string propB, float fromB, float toB,
        float duration, AnimationCurve curve)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            float e = curve != null ? curve.Evaluate(k) : k;

            mat.SetFloat(propA, Mathf.Lerp(fromA, toA, e));
            mat.SetFloat(propB, Mathf.Lerp(fromB, toB, e));

            yield return null;
        }

        // 确保最终值精确设置
        mat.SetFloat(propA, toA);
        mat.SetFloat(propB, toB);
    }

    private float GetFloatSafe(Material m, string name, float fallback)
    {
        return m.HasProperty(name) ? m.GetFloat(name) : fallback;
    }

    private void SetFloatSafe(Material m, string name, float v)
    {
        if (m != null && m.HasProperty(name)) m.SetFloat(name, v);
    }
}