using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结局演出控制器
/// 功能：
/// 1. 监听对话块605完成，触发结局演出
/// 2. 延迟后播放对话块999，同时执行画面失真动画
/// 3. 依次播放系统提示、抖动增强、蓝屏、黑屏、启动动画
/// 4. 最后切换到结局后Stage，恢复正常桌面
/// </summary>
public class EndingSequenceController : MonoBehaviour
{
    #region 组件引用

    [Header("=== 系统组件 ===")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameFlowController gameFlowController;

    [Header("=== 需要失真的Image列表 ===")]
    [Tooltip("手动拖入所有需要失真效果的Image（包括背景、图标、立绘等）")]
    [SerializeField] private List<UnityEngine.UI.Image> wiggleImages = new List<UnityEngine.UI.Image>();

    [Header("=== 结局UI ===")]
    [SerializeField] private Canvas endingCanvas;
    [SerializeField] private GameObject systemPrompt1;      // "异常权限调用"窗口
    [SerializeField] private Button yesButton;              // "是"按钮（用于高亮效果）
    [SerializeField] private GameObject systemPrompt2;      // "正在回收权限"窗口
    [SerializeField] private GameObject bsodScreen;         // 蓝屏画面
    [SerializeField] private GameObject blackScreen;        // 黑屏画面
    [SerializeField] private GameObject bootAnimationUI;    // 启动动画UI容器
    [SerializeField] private WinXPLoadingBar loadingBar;    // 启动读条组件（可选）

    #endregion

    #region 配置参数

    [Header("=== Stage配置 ===")]
    [Tooltip("结局演出后要切换到的Stage ID")]
    [SerializeField] private string postEndingStageId = "Stage_PostEnding";

    [Header("=== 时间参数 ===")]
    [Tooltip("对话605结束后的延迟时间")]
    [SerializeField] private float delayAfter605 = 0.5f;

    [Tooltip("画面失真动画持续时间")]
    [SerializeField] private float glitchDuration = 3.0f;

    [Tooltip("系统提示1显示后，自动选择'是'的延迟")]
    [SerializeField] private float promptAutoSelectDelay = 1.2f;

    [Tooltip("系统提示2持续时间")]
    [SerializeField] private float prompt2Duration = 1.5f;

    [Tooltip("蓝屏持续时间")]
    [SerializeField] private float bsodDuration = 2.0f;

    [Tooltip("黑屏持续时间")]
    [SerializeField] private float blackScreenDuration = 3.0f;

    [Tooltip("启动动画循环次数")]
    [SerializeField] private int bootAnimationLoops = 3;

    [Header("=== Shader效果参数 ===")]
    [Tooltip("失真目标倍数（例如0.1表示降到初始值的10%）")]
    [SerializeField] private float glitchTargetMultiplier = 0.1f;

    [SerializeField] private float glitchSteps = 5;

    [SerializeField] private float decayMultiplier = 1.0f;

    [Tooltip("失真的最小像素分辨率")]
    [SerializeField] private float glitchMinResolution = 4.0f;

    [Tooltip("失真速度曲线")]
    [SerializeField] private AnimationCurve glitchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("抖动增强的数值")]
    [SerializeField] private float jitterBoostAmount = 1.5f;

    [Header("=== 调试选项 ===")]
    [SerializeField] private bool enableDebugLog = true;

    #endregion

    #region 私有变量

    // 状态标记
    private bool hasTriggered = false;          // 是否已触发结局（防止重复触发）
    private bool isPlayingEnding = false;       // 是否正在播放结局
    private bool dialogue999Completed = false;  // 对话999是否完成

    // Shader材质缓存
    private List<ShaderMaterialData> cachedShaderMaterials = new List<ShaderMaterialData>();

    #endregion

    #region 数据结构

    /// <summary>
    /// Shader材质数据 - 用于缓存和恢复Shader参数
    /// </summary>
    private class ShaderMaterialData
    {
        public Material material;               // 材质实例引用
        public float initialPixelResolution;    // 初始像素分辨率
        public float initialJitterStrength;     // 初始抖动强度
        public string objectName;               // 对象名称（用于调试）

        public ShaderMaterialData(Material mat, float pixelRes, float jitter, string name)
        {
            material = mat;
            initialPixelResolution = pixelRes;
            initialJitterStrength = jitter;
            objectName = name;
        }
    }

    #endregion

    #region Unity生命周期

    void Start()
    {
        // 初始化：隐藏结局Canvas
        if (endingCanvas != null)
        {
            endingCanvas.gameObject.SetActive(false);
        }

        // 自动查找组件引用
        FindRequiredComponents();

        // 缓存场景中所有Shader材质
        CacheAllShaderMaterials();

        Log("EndingSequenceController 初始化完成");
    }

    void OnEnable()
    {
        // 订阅对话完成事件
        GameEvents.OnDialogueBlockCompleted += OnDialogueBlockCompleted;
        Log("已订阅对话完成事件");
    }

    void OnDisable()
    {
        // 取消订阅事件
        GameEvents.OnDialogueBlockCompleted -= OnDialogueBlockCompleted;
        Log("已取消订阅对话完成事件");
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 查找必需的组件引用
    /// </summary>
    private void FindRequiredComponents()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
            if (dialogueManager == null)
            {
                LogError("找不到DialogueManager组件");
            }
        }

        if (gameFlowController == null)
        {
            gameFlowController = FindObjectOfType<GameFlowController>();
            if (gameFlowController == null)
            {
                LogError("找不到GameFlowController组件");
            }
        }
    }

    /// <summary>
    /// 缓存场景中所有使用JitterPixelSprite Shader的材质
    /// </summary>
    private void CacheAllShaderMaterials()
    {
        cachedShaderMaterials.Clear();

        int cachedCount = 0;

        // 1. 查找所有SpriteRenderer（包括inactive）
        SpriteRenderer[] allSpriteRenderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        foreach (SpriteRenderer renderer in allSpriteRenderers)
        {
            // 跳过预制体和非场景对象
            if (renderer.gameObject.scene.name == null) continue;

            if (TryCacheMaterial(renderer.material, renderer.gameObject, ref cachedCount))
            {
                Log($"  └─ [SpriteRenderer] {renderer.gameObject.name}");
            }
        }

        // 2. 查找所有UI Image（如果有的话）
        UnityEngine.UI.Image[] allImages = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>();
        foreach (UnityEngine.UI.Image image in allImages)
        {
            // 跳过预制体和非场景对象
            if (image.gameObject.scene.name == null) continue;

            if (TryCacheMaterial(image.material, image.gameObject, ref cachedCount))
            {
                Log($"  └─ [Image] {image.gameObject.name}");
            }
        }

        if (cachedCount == 0)
        {
            LogWarning("未找到任何使用JitterPixelSprite Shader的材质！");
            LogWarning("请检查：");
            LogWarning("  1. Shader名称是否包含 'JitterPixelSprite'");
            LogWarning("  2. 材质是否正确应用到场景对象上");
            LogWarning("  3. 是否在正确的时机调用缓存方法");
        }
        else
        {
            Log($"成功缓存 {cachedCount} 个Shader材质");
        }
    }

    /// <summary>
    /// 尝试缓存材质
    /// </summary>
    private bool TryCacheMaterial(Material mat, GameObject obj, ref int count)
    {
        if (mat == null || mat.shader == null)
        {
            return false;
        }

        string shaderName = mat.shader.name;

        // 检查Shader名称
        if (!shaderName.Contains("JitterPixelSprite"))
        {
            return false;
        }

        // 检查材质是否有必要的属性
        if (!mat.HasProperty("_PixelResolution") || !mat.HasProperty("_JitterStrength"))
        {
            LogWarning($"材质 {mat.name} 使用了正确的Shader，但缺少必要的属性");
            return false;
        }

        // 读取初始参数
        float initialPixelRes = mat.GetFloat("_PixelResolution");
        float initialJitter = mat.GetFloat("_JitterStrength");

        // 添加到缓存列表
        cachedShaderMaterials.Add(new ShaderMaterialData(
            mat,
            initialPixelRes,
            initialJitter,
            obj.name
        ));

        count++;
        return true;
    }

    #endregion

    #region 事件监听

    /// <summary>
    /// 对话块完成事件回调
    /// </summary>
    private void OnDialogueBlockCompleted(string blockId)
    {
        Log($"收到对话完成事件: {blockId}");

        // 监听对话块605：触发结局演出
        if (blockId == "605" && !hasTriggered)
        {
            Log("检测到对话605完成，触发结局演出");
            TriggerEnding();
        }

        // 监听对话块999：标记完成，继续演出流程
        if (blockId == "999" && isPlayingEnding)
        {
            Log("检测到对话999完成");
            dialogue999Completed = true;
        }
    }

    #endregion

    #region 结局流程控制

    /// <summary>
    /// 触发结局演出（公共接口）
    /// </summary>
    public void TriggerEnding()
    {
        // 防止重复触发
        if (hasTriggered || isPlayingEnding)
        {
            Log("结局演出已触发或正在播放，跳过重复触发");
            return;
        }

        hasTriggered = true;
        isPlayingEnding = true;

        // 启动结局演出协程
        StartCoroutine(EndingSequenceCoroutine());
    }

    /// <summary>
    /// 结局演出主协程
    /// </summary>
    private IEnumerator EndingSequenceCoroutine()
    {
        Log("========== 结局演出开始 ==========");

        // ===== 阶段1: 短暂停顿 =====
        Log($"[阶段1] 短暂停顿 {delayAfter605}秒");
        yield return new WaitForSeconds(delayAfter605);

        // ===== 阶段2: 播放对话999 + 失真动画（并行） =====
        Log("[阶段2] 播放对话999 + 失真动画");

        // 激活结局Canvas
        if (endingCanvas != null)
        {
            endingCanvas.gameObject.SetActive(true);
        }

        // 并行：启动失真动画
        StartCoroutine(GlitchEffectCoroutine());

        // 并行：播放对话999
        dialogue999Completed = false;
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue("999");
        }
        else
        {
            LogError("DialogueManager未找到，跳过对话999");
            dialogue999Completed = true; // 避免卡死
        }

        // 等待对话999完成
        yield return new WaitUntil(() => dialogue999Completed);
        Log("[阶段2] 对话999播放完毕");

        // ===== 阶段3: 系统提示窗口1 =====
        Log("[阶段3] 显示系统提示窗口1");
        yield return ShowSystemPrompt1();

        // ===== 阶段4: 增强抖动效果 =====
        Log("[阶段4] 增强抖动效果");
        BoostAllJitterStrength();
        yield return new WaitForSeconds(0.5f); // 让玩家感受抖动效果

        // ===== 阶段5: 系统提示窗口2 =====
        Log("[阶段5] 显示系统提示窗口2");
        yield return ShowSystemPrompt2();

        // ===== 阶段6: 蓝屏死机 =====
        Log("[阶段6] 蓝屏死机");
        yield return ShowBSOD();

        // ===== 阶段7: 黑屏静默 =====
        Log("[阶段7] 黑屏静默");
        yield return ShowBlackScreen();

        // ===== 阶段8: 启动动画 =====
        Log($"[阶段8] 播放启动动画（{bootAnimationLoops}次循环）");
        yield return PlayBootAnimation();

        // ===== 阶段9: 恢复桌面 =====
        Log("[阶段9] 恢复桌面");
        RestoreDesktop();

        // ===== 阶段10: 切换到结局后Stage =====
        Log($"[阶段10] 切换到Stage: {postEndingStageId}");
        SwitchToPostEndingStage();

        Log("========== 结局演出完成 ==========");
        isPlayingEnding = false;
    }

    #endregion

    #region 各阶段实现

    /// <summary>
    /// 失真动画协程 - 分段式突变效果
    /// </summary>
    private IEnumerator GlitchEffectCoroutine()
    {
        Log($"失真动画开始，总时长 {glitchDuration}秒，分 {glitchSteps} 步");

        // 计算每一步的间隔时间
        float stepInterval = glitchDuration / glitchSteps;

        for (int step = 0; step < glitchSteps; step++)
        {
            Log($"  失真步骤 {step + 1}/{glitchSteps}");

            // 对所有缓存的材质进行一次突变
            foreach (var data in cachedShaderMaterials)
            {
                if (data.material == null) continue;

                // 获取当前分辨率
                float currentRes = data.material.GetFloat("_PixelResolution");

                // 按倍率降低
                float nextRes = currentRes * decayMultiplier;

                // 限制最小值
                nextRes = Mathf.Max(nextRes, glitchMinResolution);

                // 立即应用（突变效果）
                data.material.SetFloat("_PixelResolution", nextRes);

                Log($"    {data.objectName}: {currentRes:F1} -> {nextRes:F1}");
            }

            // 等待下一步
            yield return new WaitForSeconds(stepInterval);
        }

        Log("失真动画完成");
    }

    /// <summary>
    /// 增强所有材质的抖动强度
    /// </summary>
    private void BoostAllJitterStrength()
    {
        foreach (var data in cachedShaderMaterials)
        {
            if (data.material == null) continue;

            float currentJitter = data.material.GetFloat("_JitterStrength");
            float boostedJitter = currentJitter + jitterBoostAmount;

            data.material.SetFloat("_JitterStrength", boostedJitter);
        }

        Log($"已增强所有材质的抖动强度（+{jitterBoostAmount}）");
    }

    /// <summary>
    /// 显示系统提示窗口1
    /// </summary>
    private IEnumerator ShowSystemPrompt1()
    {
        if (systemPrompt1 != null)
        {
            systemPrompt1.SetActive(true);
        }

        // 等待延迟
        yield return new WaitForSeconds(promptAutoSelectDelay);

        // 高亮"是"按钮（可选效果）
        if (yesButton != null)
        {
            yield return HighlightButtonEffect(yesButton, 0.3f);
        }

        // 关闭窗口
        if (systemPrompt1 != null)
        {
            systemPrompt1.SetActive(false);
        }
    }

    /// <summary>
    /// 显示系统提示窗口2
    /// </summary>
    private IEnumerator ShowSystemPrompt2()
    {
        if (systemPrompt2 != null)
        {
            systemPrompt2.SetActive(true);
        }

        yield return new WaitForSeconds(prompt2Duration);

        if (systemPrompt2 != null)
        {
            systemPrompt2.SetActive(false);
        }
    }

    /// <summary>
    /// 显示蓝屏
    /// </summary>
    private IEnumerator ShowBSOD()
    {
        if (bsodScreen != null)
        {
            bsodScreen.SetActive(true);
        }

        yield return new WaitForSeconds(bsodDuration);

        if (bsodScreen != null)
        {
            bsodScreen.SetActive(false);
        }
    }

    /// <summary>
    /// 显示黑屏
    /// </summary>
    private IEnumerator ShowBlackScreen()
    {
        if (blackScreen != null)
        {
            blackScreen.SetActive(true);
        }

        yield return new WaitForSeconds(blackScreenDuration);

        if (blackScreen != null)
        {
            blackScreen.SetActive(false);
        }
    }

    /// <summary>
    /// 播放启动动画
    /// </summary>
    private IEnumerator PlayBootAnimation()
    {
        // 显示启动动画UI
        if (bootAnimationUI != null)
        {
            bootAnimationUI.SetActive(true);
        }

        // 如果有WinXPLoadingBar组件
        if (loadingBar != null)
        {
            // 计算单次循环时长
            float cycleTime = (loadingBar.maxUnits * loadingBar.unitSpawnInterval) +
                             loadingBar.cycleDelay;

            // 启动读条
            loadingBar.StartLoading();

            // 等待指定循环次数
            yield return new WaitForSeconds(cycleTime * bootAnimationLoops);

            // 停止读条（简单粗暴：销毁组件）
            Destroy(loadingBar);
        }
        else
        {
            // 没有LoadingBar组件，用简单等待代替
            LogWarning("未找到WinXPLoadingBar组件，使用简单等待");
            yield return new WaitForSeconds(2.5f * bootAnimationLoops);
        }

        // 隐藏启动动画UI
        if (bootAnimationUI != null)
        {
            bootAnimationUI.SetActive(false);
        }
    }

    /// <summary>
    /// 恢复桌面
    /// </summary>
    private void RestoreDesktop()
    {
        // 恢复所有Shader参数到初始状态
        foreach (var data in cachedShaderMaterials)
        {
            if (data.material == null) continue;

            data.material.SetFloat("_PixelResolution", data.initialPixelResolution);
            data.material.SetFloat("_JitterStrength", data.initialJitterStrength);
        }

        Log("已恢复所有Shader参数");

        // 隐藏结局Canvas
        if (endingCanvas != null)
        {
            endingCanvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 切换到结局后的Stage
    /// </summary>
    private void SwitchToPostEndingStage()
    {
        if (gameFlowController == null)
        {
            LogError("GameFlowController未找到，无法切换Stage");
            return;
        }

        if (string.IsNullOrEmpty(postEndingStageId))
        {
            LogWarning("postEndingStageId未设置，跳过Stage切换");
            return;
        }

        // 调用GameFlowController切换Stage
        gameFlowController.LoadStage(postEndingStageId, triggerAutoSave: true);
        Log($"已切换到Stage: {postEndingStageId}");
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 按钮高亮效果
    /// </summary>
    private IEnumerator HighlightButtonEffect(Button button, float duration)
    {
        if (button == null) yield break;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage == null) yield break;

        Color originalColor = buttonImage.color;
        Color highlightColor = Color.yellow;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            buttonImage.color = Color.Lerp(originalColor, highlightColor, t);
            yield return null;
        }

        buttonImage.color = originalColor;
    }

    #endregion

    #region 日志工具

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[EndingSequence] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[EndingSequence] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[EndingSequence] {message}");
    }

    #endregion

    #region 编辑器调试工具

#if UNITY_EDITOR

    [ContextMenu("▶ 触发结局演出")]
    private void EditorTriggerEnding()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在运行时使用此功能");
            return;
        }

        Log("[编辑器] 手动触发结局演出");
        TriggerEnding();
    }

    [ContextMenu("▶ 测试失真效果")]
    private void EditorTestGlitch()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在运行时使用此功能");
            return;
        }

        Log("[编辑器] 测试失真效果");
        CacheAllShaderMaterials();
        StartCoroutine(GlitchEffectCoroutine());
    }

    [ContextMenu("▶ 诊断Shader材质")]
    private void EditorDiagnoseMaterials()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在运行时使用此功能");
            return;
        }

        Debug.Log("========== Shader材质诊断开始 ==========");

        // 1. 诊断SpriteRenderer
        SpriteRenderer[] allSpriteRenderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        int validSpriteCount = 0;
        int sceneSpriteCount = 0;

        foreach (SpriteRenderer renderer in allSpriteRenderers)
        {
            if (renderer.gameObject.scene.name == null || renderer.gameObject.scene.name == "") continue;
            sceneSpriteCount++;

            if (renderer.material != null && renderer.material.shader != null)
            {
                string shaderName = renderer.material.shader.name;
                if (shaderName.ToLower().Contains("wiggle"))
                {
                    validSpriteCount++;
                    Debug.Log($"  ✓ [SpriteRenderer] {renderer.gameObject.name}");
                    Debug.Log($"      Material: {renderer.material.name}");
                    Debug.Log($"      Shader: {shaderName}");
                }
            }
        }

        Debug.Log($"SpriteRenderer统计: 场景中{sceneSpriteCount}个，使用Wiggle的{validSpriteCount}个");
        Debug.Log("---");

        // 2. 诊断UI Image（重点）
        UnityEngine.UI.Image[] allImages = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Image>();
        int validImageCount = 0;
        int sceneImageCount = 0;
        int noMaterialCount = 0;
        int wrongShaderCount = 0;

        Debug.Log($"找到 {allImages.Length} 个Image组件（包括预制体）");

        foreach (UnityEngine.UI.Image image in allImages)
        {
            // 检查是否在场景中
            string sceneName = image.gameObject.scene.name;
            bool inScene = !string.IsNullOrEmpty(sceneName);

            if (!inScene) continue;
            sceneImageCount++;

            // 获取完整路径
            string path = GetGameObjectPath(image.gameObject);

            // 检查材质
            Material mat = image.material;

            if (mat == null)
            {
                noMaterialCount++;
                Debug.Log($"  ✗ [Image] {path}");
                Debug.Log($"      ⚠ 没有材质");
                continue;
            }

            if (mat.shader == null)
            {
                Debug.Log($"  ✗ [Image] {path}");
                Debug.Log($"      Material: {mat.name}");
                Debug.Log($"      ⚠ 材质没有Shader");
                continue;
            }

            string shaderName = mat.shader.name;

            if (shaderName.ToLower().Contains("wiggle"))
            {
                validImageCount++;
                Debug.Log($"  ✓ [Image] {path}");
                Debug.Log($"      Material: {mat.name}");
                Debug.Log($"      Shader: {shaderName}");

                // 检查Shader属性
                if (mat.HasProperty("_PixelResolution"))
                {
                    float pixelRes = mat.GetFloat("_PixelResolution");
                    Debug.Log($"      _PixelResolution: {pixelRes}");
                }
                else
                {
                    Debug.Log($"      ⚠ 缺少 _PixelResolution 属性");
                }

                if (mat.HasProperty("_JitterStrength"))
                {
                    float jitter = mat.GetFloat("_JitterStrength");
                    Debug.Log($"      _JitterStrength: {jitter}");
                }
                else
                {
                    Debug.Log($"      ⚠ 缺少 _JitterStrength 属性");
                }
            }
            else
            {
                wrongShaderCount++;
                Debug.Log($"  ✗ [Image] {path}");
                Debug.Log($"      Material: {mat.name}");
                Debug.Log($"      Shader: {shaderName} (不包含'wiggle')");
            }
        }

        Debug.Log("---");
        Debug.Log($"UI Image统计:");
        Debug.Log($"  场景中Image总数: {sceneImageCount}");
        Debug.Log($"  使用Wiggle Shader: {validImageCount}");
        Debug.Log($"  使用其他Shader: {wrongShaderCount}");
        Debug.Log($"  没有材质: {noMaterialCount}");
        Debug.Log("========== 诊断完成 ==========");
    }

    /// <summary>
    /// 获取GameObject的完整路径
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    [ContextMenu("▶ 重新缓存材质")]
    private void EditorRecacheMaterials()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("请在运行时使用此功能");
            return;
        }

        Log("[编辑器] 重新缓存材质");
        CacheAllShaderMaterials();
    }

    void Update()
    {
        // 快捷键：按P触发结局
        if (Input.GetKeyDown(KeyCode.P))
        {
            EditorTriggerEnding();
        }
    }

#endif

    #endregion
}