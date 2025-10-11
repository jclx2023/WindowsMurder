using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 目标颜色配置
/// </summary>
[System.Serializable]
public class TargetColorConfig
{
    [Header("标识")]
    public string colorId = "dark_red";

    [Header("颜色设置")]
    public Color targetColor = new Color(0.545f, 0, 0); // RGB(139, 0, 0)
    [Range(0f, 0.2f)]
    public float tolerance = 0.05f;

    [Header("对话配置")]
    public string dialogueBlockId = "dialogue_dark_red";

    [Header("描述（调试用）")]
    [TextArea(2, 3)]
    public string description = "刀上的深红色血迹";
}

/// <summary>
/// 颜色证据控制器 - 管理取色证据流程
/// </summary>
public class ColorEvidenceController : MonoBehaviour
{
    [Header("=== 目标颜色 ===")]
    [SerializeField] private List<TargetColorConfig> targetColors = new List<TargetColorConfig>();

    [Header("=== 完成配置 ===")]
    [SerializeField] private string completionDialogueId = "dialogue_blood_complete";
    [SerializeField] private string unlockedClueId = "evidence_fake_blood";
    [SerializeField] private bool requireAllColors = true;

    [Header("=== 引用 ===")]
    [SerializeField] private GameFlowController gameFlowController;

    [Header("=== 运行时状态（只读）===")]
    [SerializeField] private List<string> pickedColorIdsList = new List<string>();
    [SerializeField] private bool isCompleted = false;
    [SerializeField] private int pickAttempts = 0;

    [Header("=== 调试 ===")]
    [SerializeField] private bool debugMode = true;

    // 内部状态
    private HashSet<string> pickedColorIds = new HashSet<string>();

    #region 生命周期

    void Awake()
    {
        if (gameFlowController == null)
        {
            gameFlowController = FindObjectOfType<GameFlowController>();
        }
    }

    void OnEnable()
    {
        // 订阅全局取色事件
        EyedropperTool.OnAnyColorPicked += HandleColorPicked;
    }

    void OnDisable()
    {
        // 取消订阅
        EyedropperTool.OnAnyColorPicked -= HandleColorPicked;
    }

    void Start()
    {
    }

    #endregion

    #region 核心逻辑

    /// <summary>
    /// 处理取色事件
    /// </summary>
    private void HandleColorPicked(Color pickedColor)
    {
        // 如果已完成，忽略后续取色
        if (isCompleted)
        {
            LogDebug("证据已完成，忽略取色");
            return;
        }

        pickAttempts++;
        LogDebug($"收到取色事件 #{pickAttempts}: #{ColorUtility.ToHtmlStringRGB(pickedColor)}");

        // 遍历目标颜色，查找匹配
        foreach (var target in targetColors)
        {
            if (IsColorMatch(pickedColor, target.targetColor, target.tolerance))
            {
                LogDebug($"匹配到目标颜色: {target.colorId}");
                OnTargetColorPicked(target);
                return; // 只匹配一个就够了
            }
        }

        // 没有匹配到任何目标
        LogDebug("未匹配到任何目标颜色");
    }

    /// <summary>
    /// 颜色匹配判断
    /// </summary>
    private bool IsColorMatch(Color pickedColor, Color targetColor, float tolerance)
    {
        // 计算欧氏距离
        float distance = Mathf.Sqrt(
            Mathf.Pow(pickedColor.r - targetColor.r, 2) +
            Mathf.Pow(pickedColor.g - targetColor.g, 2) +
            Mathf.Pow(pickedColor.b - targetColor.b, 2)
        );

        bool isMatch = distance < tolerance;

        if (debugMode)
        {
            LogDebug($"颜色距离: {distance:F4}, 容差: {tolerance}, 匹配: {isMatch}");
        }

        return isMatch;
    }

    /// <summary>
    /// 目标颜色命中处理
    /// </summary>
    private void OnTargetColorPicked(TargetColorConfig target)
    {
        // 检查是否已经取过
        if (pickedColorIds.Contains(target.colorId))
        {
            LogDebug($"颜色 [{target.colorId}] 已经取过，忽略");
            return;
        }

        // 记录已取到的颜色
        pickedColorIds.Add(target.colorId);
        pickedColorIdsList.Add(target.colorId);
        LogDebug($"记录颜色: {target.colorId}，当前已取 {pickedColorIds.Count}/{targetColors.Count}");

        if (CheckCompletion())
        {
            // 完成了，只播放完成对话，不播放单个颜色的对话
            OnEvidenceCompleted();
        }
        else
        {
            // 未完成，播放单个颜色的对话
            TriggerDialogue(target.dialogueBlockId);
        }
    }

    /// <summary>
    /// 检查是否完成（返回是否完成）
    /// </summary>
    private bool CheckCompletion()
    {
        if (!requireAllColors)
        {
            // 如果不要求全部取到，取到任意一个就算完成（暂不使用）
            return pickedColorIds.Count > 0;
        }

        // 检查是否所有目标颜色都取到了
        foreach (var target in targetColors)
        {
            if (!pickedColorIds.Contains(target.colorId))
            {
                return false; // 还有未取到的
            }
        }

        return true; // 全部取到
    }

    /// <summary>
    /// 证据完成处理
    /// </summary>
    private void OnEvidenceCompleted()
    {
        if (isCompleted)
        {
            LogDebug("证据已标记为完成，跳过重复处理");
            return;
        }

        isCompleted = true;

        // 触发完成对话
        TriggerDialogue(completionDialogueId);

        // 解锁线索
        if (!string.IsNullOrEmpty(unlockedClueId))
        {
            UnlockClue(unlockedClueId);
        }
    }

    #endregion

    #region GameFlowController调用

    /// <summary>
    /// 触发对话块
    /// </summary>
    private void TriggerDialogue(string dialogueBlockId)
    {
        LogDebug($"触发对话块: {dialogueBlockId}");
        gameFlowController.StartDialogueBlock(dialogueBlockId);
    }

    /// <summary>
    /// 解锁线索
    /// </summary>
    private void UnlockClue(string clueId)
    {

        LogDebug($"解锁线索: {clueId}");
        gameFlowController.UnlockClue(clueId);
    }

    #endregion

    #region 调试工具
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            //Debug.Log($"[ColorEvidence:] {message}");
        }
    }

    #endregion
}