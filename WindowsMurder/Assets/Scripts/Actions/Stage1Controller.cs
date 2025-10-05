using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage1流程控制器
/// </summary>
public class Stage1Controller : MonoBehaviour
{
    [Header("UI组件")]
    public Image flashImage;            // 闪烁效果的图片组件

    [Header("动效设置")]
    public float flashDuration = 0.5f;  // 单次闪烁持续时间

    // 私有变量
    private bool waitingForClick = true;
    private bool dialogueStarted = false;
    private DialogueManager dialogueManager;

    void Start()
    {
        InitializeStage();
    }

    void Update()
    {
        // 等待玩家点击任意位置开始对话
        if (waitingForClick && Input.GetMouseButtonDown(0))
        {
            StartStage1Dialogue();
        }
    }

    /// <summary>
    /// 初始化Stage1
    /// </summary>
    private void InitializeStage()
    {
        // 查找DialogueManager
        dialogueManager = FindObjectOfType<DialogueManager>();

        // 确保闪烁图片初始状态为透明
        if (flashImage != null)
        {
            Color color = flashImage.color;
            color.a = 0f;
            flashImage.color = color;
        }

        // 订阅对话事件
        SubscribeToDialogueEvents();
    }

    /// <summary>
    /// 开始Stage1对话
    /// </summary>
    private void StartStage1Dialogue()
    {
        if (dialogueStarted) return;

        waitingForClick = false;
        dialogueStarted = true;
        dialogueManager.StartDialogue("001");
    }

    /// <summary>
    /// 订阅对话事件
    /// </summary>
    private void SubscribeToDialogueEvents()
    {
        // 订阅DialogueUI的对话行开始事件
        DialogueUI.OnLineStarted += OnDialogueLineStarted;
    }

    /// <summary>
    /// 对话行开始事件处理
    /// </summary>
    private void OnDialogueLineStarted(string lineId, string characterId, string blockId, bool isPresetMode)
    {
        // 检查是否是特定的对话块和对话行
        if (lineId == "2" && blockId == "001" && flashImage != null)
        {
            StartCoroutine(FlashEffect());
        }
    }

    /// <summary>
    /// 闪烁效果协程
    /// </summary>
    private IEnumerator FlashEffect()
    {
        if (flashImage == null) yield break;

        Debug.Log("触发闪烁效果");

        // 闪烁两次
        for (int i = 0; i < 4; i++)
        {
            // 淡入 (0 -> 1)
            yield return StartCoroutine(FadeImage(0f, 1f, flashDuration));

            // 淡出 (1 -> 0)
            yield return StartCoroutine(FadeImage(1f, 0f, flashDuration));
        }

        Debug.Log("闪烁效果完成");
    }

    /// <summary>
    /// 图片淡入淡出协程
    /// </summary>
    private IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
    {
        Color color = flashImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

            color.a = alpha;
            flashImage.color = color;

            yield return null;
        }

        // 确保最终值准确
        color.a = endAlpha;
        flashImage.color = color;
    }

    /// <summary>
    /// 清理事件订阅
    /// </summary>
    private void OnDestroy()
    {
        // 取消事件订阅
        DialogueUI.OnLineStarted -= OnDialogueLineStarted;
    }

    #region 调试工具

    [ContextMenu("测试闪烁效果")]
    private void TestFlashEffect()
    {
        if (flashImage != null)
        {
            StartCoroutine(FlashEffect());
        }
    }

    #endregion
}