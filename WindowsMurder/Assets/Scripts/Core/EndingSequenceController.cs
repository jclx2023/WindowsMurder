using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 结局演出控制器
/// </summary>
public class EndingSequenceController : MonoBehaviour
{
    #region 配置

    [System.Serializable]
    public class WiggleImageConfig
    {
        public Image image;
        public float targetResolution = 20f;
    }

    [Header("系统组件")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private GameFlowController gameFlowController;

    [Header("Wiggle Image配置")]
    [SerializeField] private List<WiggleImageConfig> wiggleImageConfigs = new List<WiggleImageConfig>();

    [Header("结局UI")]
    [SerializeField] private Canvas endingCanvas;
    [SerializeField] private GameObject systemPrompt1;
    [SerializeField] private Button yesButton;
    [SerializeField] private GameObject systemPrompt2;
    [SerializeField] private GameObject bsodScreen;
    [SerializeField] private GameObject blackScreen;
    [SerializeField] private GameObject bootAnimationUI;
    [SerializeField] private WinXPLoadingBar loadingBar;

    [Header("Stage配置")]
    [SerializeField] private string postEndingStageId = "Stage_PostEnding";
    [SerializeField] private AudioClip audioClip;

    [Header("对话配置")]
    [SerializeField] private string dialogueBlock999 = "999";
    [SerializeField] private string glitchTriggerLineId = "2";

    [Header("时间参数")]
    [SerializeField] private float delayAfter605 = 0.5f;
    [SerializeField] private float glitchDuration = 3.0f;
    [SerializeField] private float promptAutoSelectDelay = 1.2f;
    [SerializeField] private float prompt2Duration = 1.5f;
    [SerializeField] private float bsodDuration = 2.0f;
    [SerializeField] private float blackScreenDuration = 3.0f;
    [SerializeField] private int bootAnimationLoops = 3;

    [Header("Shader参数")]
    [SerializeField] private int glitchSteps = 6;
    [SerializeField] private float jitterBoostAmount = 1.5f;

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    #endregion

    #region 私有变量

    private class ShaderMaterialData
    {
        public Material material;
        public float initialPixelResolution;
        public float targetPixelResolution;
        public float initialJitterStrength;
        public float decayRate;  // 每步的衰减倍率

        public ShaderMaterialData(Material mat, float initialRes, float targetRes, float jitter, float rate)
        {
            material = mat;
            initialPixelResolution = initialRes;
            targetPixelResolution = targetRes;
            initialJitterStrength = jitter;
            decayRate = rate;
        }
    }

    private List<ShaderMaterialData> cachedMaterials = new List<ShaderMaterialData>();
    private bool hasTriggered = false;
    private bool isPlayingEnding = false;
    private bool dialogue999Completed = false;
    private bool glitchEffectStarted = false;

    #endregion

    #region Unity生命周期

    void Start()
    {
        if (endingCanvas != null)
            endingCanvas.gameObject.SetActive(false);

        CacheMaterials();
    }

    void OnEnable()
    {
        GameEvents.OnDialogueBlockCompleted += OnDialogueBlockCompleted;
        DialogueUI.OnLineStarted += OnDialogueLineStarted;
    }

    void OnDisable()
    {
        GameEvents.OnDialogueBlockCompleted -= OnDialogueBlockCompleted;
        DialogueUI.OnLineStarted -= OnDialogueLineStarted;
    }

    #endregion

    #region 核心逻辑

    private void CacheMaterials()
    {
        cachedMaterials.Clear();
        int steps = Mathf.Max(1, glitchSteps);

        foreach (var config in wiggleImageConfigs)
        {
            if (config.image == null || config.image.material == null) continue;

            Material mat = config.image.material;
            float initialRes = mat.GetFloat("_PixelResolution");
            float initialJitter = mat.GetFloat("_JitterStrength");

            // 计算衰减倍率：initialRes * rate^steps = targetRes
            float ratio = config.targetResolution / Mathf.Max(1e-6f, initialRes);
            float decayRate = Mathf.Pow(ratio, 1f / steps);

            cachedMaterials.Add(new ShaderMaterialData(
                mat,
                initialRes,
                config.targetResolution,
                initialJitter,
                decayRate
            ));

            if (debugMode) Debug.Log($"[Ending] {config.image.name}: {initialRes:F3} -> {config.targetResolution:F3} (rate:{decayRate:F6}, steps:{steps})");
        }

        if (debugMode) Debug.Log($"[Ending] 缓存了 {cachedMaterials.Count} 个材质");
    }

    /// <summary>
    /// 监听对话行开始事件
    /// </summary>
    private void OnDialogueLineStarted(string lineId, string characterId, string blockId, bool isPresetMode)
    {
        // 只处理对话块999
        if (blockId != dialogueBlock999) return;

        // 检查是否是触发失真的line
        if (lineId == glitchTriggerLineId && !glitchEffectStarted)
        {
            glitchEffectStarted = true;
            StartCoroutine(GlitchEffect());
            if (debugMode) Debug.Log($"[Ending] 对话块{blockId}的lineId={lineId}触发失真效果");
        }
    }

    private void OnDialogueBlockCompleted(string blockId)
    {
        if (blockId == "605" && !hasTriggered)
        {
            TriggerEnding();
        }
        else if (blockId == dialogueBlock999 && isPlayingEnding)
        {
            dialogue999Completed = true;
            if (debugMode) Debug.Log("[Ending] 对话块999完成");
        }
    }

    public void TriggerEnding()
    {
        if (hasTriggered || isPlayingEnding) return;
        hasTriggered = true;
        isPlayingEnding = true;
        StartCoroutine(EndingSequence());
    }

    #endregion

    #region 结局流程

    private IEnumerator EndingSequence()
    {
        if (debugMode) Debug.Log("[Ending] 开始");

        // 延迟
        yield return new WaitForSeconds(delayAfter605);

        // 激活Canvas
        if (endingCanvas != null)
            endingCanvas.gameObject.SetActive(true);

        // 开始对话999（不启动失真，等待lineId=2触发）
        dialogue999Completed = false;
        glitchEffectStarted = false;
        if (dialogueManager != null)
        {
            dialogueManager.StartDialogue(dialogueBlock999);
            if (debugMode) Debug.Log("[Ending] 开始对话999，等待lineId触发失真");
        }

        // 等待对话999完成
        yield return new WaitUntil(() => dialogue999Completed);

        if (debugMode) Debug.Log("[Ending] 对话999结束，继续后续流程");

        // 系统提示1
        yield return ShowPrompt1();

        // 增强抖动
        BoostJitter();
        yield return new WaitForSeconds(0.5f);

        // 系统提示2
        yield return ShowPrompt2();

        // 蓝屏
        yield return ShowBSOD();

        // 黑屏
        yield return ShowBlackScreen();

        // 启动动画
        yield return PlayBootAnimation();

        // 恢复
        RestoreShaders();
        if (endingCanvas != null)
            endingCanvas.gameObject.SetActive(false);

        // 切换Stage
        if (gameFlowController != null)
            gameFlowController.LoadStage(postEndingStageId, true);

        if (audioClip != null)
        {
            GlobalSystemManager.Instance.PlaySFX(audioClip);
        }

        if (debugMode) Debug.Log("[Ending] 完成");
        isPlayingEnding = false;
    }

    #endregion

    #region 失真动画

    private IEnumerator GlitchEffect()
    {
        if (debugMode) Debug.Log("[Ending] 开始失真效果");

        int steps = Mathf.Max(1, glitchSteps);
        float stepInterval = glitchDuration / steps;
        const float EPS = 1e-5f;

        for (int step = 0; step < steps; step++)
        {
            foreach (var data in cachedMaterials)
            {
                if (data.material == null) continue;

                float currentRes = data.material.GetFloat("_PixelResolution");
                float nextRes = currentRes * data.decayRate;

                // 按方向夹紧到 target，避免越界或抖动
                if (data.initialPixelResolution > data.targetPixelResolution)
                {
                    // 递减收敛
                    nextRes = Mathf.Max(nextRes, data.targetPixelResolution);
                }
                else
                {
                    // 递增收敛
                    nextRes = Mathf.Min(nextRes, data.targetPixelResolution);
                }

                // 若已非常接近目标，直接设为目标避免尾差
                if (Mathf.Abs(nextRes - data.targetPixelResolution) <= EPS || step == steps - 1)
                {
                    nextRes = data.targetPixelResolution;
                }

                data.material.SetFloat("_PixelResolution", nextRes);
            }

            yield return new WaitForSeconds(stepInterval);
        }

        if (debugMode) Debug.Log("[Ending] 失真效果完成");
    }

    private void BoostJitter()
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material == null) continue;
            float current = data.material.GetFloat("_JitterStrength");
            data.material.SetFloat("_JitterStrength", current + jitterBoostAmount);
        }

        if (debugMode) Debug.Log("[Ending] 抖动已增强");
    }

    private void RestoreShaders()
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material == null) continue;
            data.material.SetFloat("_PixelResolution", data.initialPixelResolution);
            data.material.SetFloat("_JitterStrength", data.initialJitterStrength);
        }

        if (debugMode) Debug.Log("[Ending] Shader已恢复初始状态");
    }

    #endregion

    #region UI阶段

    private IEnumerator ShowPrompt1()
    {
        if (systemPrompt1 != null)
            systemPrompt1.SetActive(true);

        yield return new WaitForSeconds(promptAutoSelectDelay);

        if (yesButton != null)
            yield return HighlightButton(yesButton, 0.3f);

        if (systemPrompt1 != null)
            systemPrompt1.SetActive(false);
    }

    private IEnumerator ShowPrompt2()
    {
        if (systemPrompt2 != null)
            systemPrompt2.SetActive(true);

        yield return new WaitForSeconds(prompt2Duration);

        if (systemPrompt2 != null)
            systemPrompt2.SetActive(false);
    }

    private IEnumerator ShowBSOD()
    {
        if (bsodScreen != null)
            bsodScreen.SetActive(true);

        yield return new WaitForSeconds(bsodDuration);

        if (bsodScreen != null)
            bsodScreen.SetActive(false);
    }

    private IEnumerator ShowBlackScreen()
    {
        if (blackScreen != null)
            blackScreen.SetActive(true);

        yield return new WaitForSeconds(blackScreenDuration);

        if (blackScreen != null)
            blackScreen.SetActive(false);
    }

    private IEnumerator PlayBootAnimation()
    {
        if (bootAnimationUI != null)
            bootAnimationUI.SetActive(true);

        if (loadingBar != null)
        {
            yield return StartCoroutine(loadingBar.PlayCycles(bootAnimationLoops));
        }
        else
        {
            yield return new WaitForSeconds(2.5f * bootAnimationLoops);
        }

        if (bootAnimationUI != null)
            bootAnimationUI.SetActive(false);
    }

    private IEnumerator HighlightButton(Button button, float duration)
    {
        if (button == null) yield break;

        Image img = button.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 4f, 1f);
            img.color = Color.Lerp(original, Color.white, t);
            yield return null;
        }

        img.color = original;
    }

    #endregion

    #region 调试

#if UNITY_EDITOR
    [ContextMenu("测试结局")]
    void DebugTrigger()
    {
        if (Application.isPlaying)
            TriggerEnding();
    }

    [ContextMenu("测试失真")]
    void DebugGlitch()
    {
        if (Application.isPlaying)
        {
            CacheMaterials();
            StartCoroutine(GlitchEffect());
        }
    }

    [ContextMenu("清除失真")]
    void DebugRestore()
    {
        if (Application.isPlaying)
        {
            RestoreShaders();
            Debug.Log("[Ending] 已清除所有失真效果并恢复初始Shader参数");
        }
        else
        {
            // 在非运行状态下手动重置材质参数
            if (cachedMaterials == null || cachedMaterials.Count == 0)
            {
                CacheMaterials();
            }

            foreach (var data in cachedMaterials)
            {
                if (data.material == null) continue;
                data.material.SetFloat("_PixelResolution", data.initialPixelResolution);
                data.material.SetFloat("_JitterStrength", data.initialJitterStrength);
            }

            Debug.Log("[Ending] (编辑器模式) 已清除失真并恢复初始参数");
        }
    }
#endif

    #endregion
}
