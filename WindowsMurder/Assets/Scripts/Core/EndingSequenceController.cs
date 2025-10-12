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
    }

    void OnDisable()
    {
        GameEvents.OnDialogueBlockCompleted -= OnDialogueBlockCompleted;
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

            Debug.Log($"[Ending] {config.image.name}: {initialRes:F3} -> {config.targetResolution:F3} (rate:{decayRate:F6}, steps:{steps})");
        }

        Debug.Log($"[Ending] 缓存了 {cachedMaterials.Count} 个材质");
    }


    private void OnDialogueBlockCompleted(string blockId)
    {
        if (blockId == "605" && !hasTriggered)
        {
            TriggerEnding();
        }
        else if (blockId == "999" && isPlayingEnding)
        {
            dialogue999Completed = true;
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
        Debug.Log("[Ending] 开始");

        // 延迟
        yield return new WaitForSeconds(delayAfter605);

        // 激活Canvas
        if (endingCanvas != null)
            endingCanvas.gameObject.SetActive(true);

        // 失真动画 + 对话999
        StartCoroutine(GlitchEffect());
        dialogue999Completed = false;
        if (dialogueManager != null)
            dialogueManager.StartDialogue("999");

        yield return new WaitUntil(() => dialogue999Completed);

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

        Debug.Log("[Ending] 完成");
        isPlayingEnding = false;
    }

    #endregion

    #region 失真动画

    private IEnumerator GlitchEffect()
    {
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
    }


    private void BoostJitter()
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material == null) continue;
            float current = data.material.GetFloat("_JitterStrength");
            data.material.SetFloat("_JitterStrength", current + jitterBoostAmount);
        }
    }

    private void RestoreShaders()
    {
        foreach (var data in cachedMaterials)
        {
            if (data.material == null) continue;
            data.material.SetFloat("_PixelResolution", data.initialPixelResolution);
            data.material.SetFloat("_JitterStrength", data.initialJitterStrength);
        }
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