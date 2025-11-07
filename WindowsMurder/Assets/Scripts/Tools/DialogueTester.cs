using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话系统测试器 - 用于测试对话流程
/// 忽略所有条件限制，按顺序播放对话块
/// </summary>
public class DialogueTester : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool autoStartTest = false;
    [SerializeField] private float delayBetweenDialogues = 1.5f;
    [SerializeField] private float delayBetweenStages = 2.5f;

    [Header("组件引用")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;

    private List<StageConfig> stageConfigs = new List<StageConfig>();
    private int currentStageIndex = 0;
    private int currentDialogueIndex = 0;
    private bool isTesting = false;
    private Coroutine testCoroutine;

    private bool dialogueRunning = false;

    void Start()
    {
        InitializeTester();

        if (autoStartTest)
            StartCoroutine(DelayedAutoStart());
    }

    private void InitializeTester()
    {
        if (gameFlowController == null)
            gameFlowController = FindObjectOfType<GameFlowController>();

        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();

        if (gameFlowController == null)
        {
            LogError("未找到 GameFlowController 组件！");
            return;
        }

        stageConfigs = new List<StageConfig>(gameFlowController.GetStageConfigsSafe());
        LogDebug($"初始化完成，找到 {stageConfigs.Count} 个 Stage");
    }

    private IEnumerator DelayedAutoStart()
    {
        yield return new WaitForSeconds(1f);
        StartDialogueTest();
    }

    #region 公共接口

    [ContextMenu("开始对话测试")]
    public void StartDialogueTest()
    {
        if (isTesting || stageConfigs.Count == 0) return;

        isTesting = true;
        currentStageIndex = 0;
        currentDialogueIndex = 0;

        LogDebug("=== 开始对话系统测试 ===");
        testCoroutine = StartCoroutine(TestAllStages());
    }

    [ContextMenu("停止对话测试")]
    public void StopDialogueTest()
    {
        if (!isTesting) return;

        isTesting = false;
        if (testCoroutine != null)
            StopCoroutine(testCoroutine);

        LogDebug("=== 对话系统测试已停止 ===");
    }

    [ContextMenu("测试当前Stage")]
    public void TestCurrentStage()
    {
        if (gameFlowController == null) return;

        string stageId = gameFlowController.GetCurrentStageIdSafe();
        StageConfig stage = stageConfigs.Find(s => s.stageId == stageId);
        if (stage == null)
        {
            LogError($"找不到 Stage 配置: {stageId}");
            return;
        }

        if (isTesting) StopDialogueTest();

        isTesting = true;
        testCoroutine = StartCoroutine(TestSingleStage(stage));
    }

    #endregion

    #region 测试协程

    private IEnumerator TestAllStages()
    {
        for (currentStageIndex = 0; currentStageIndex < stageConfigs.Count; currentStageIndex++)
        {
            if (!isTesting) break;

            StageConfig stage = stageConfigs[currentStageIndex];
            LogDebug($"=== 开始测试 Stage: {stage.stageId} ===");

            gameFlowController.LoadStage(stage.stageId);

            yield return StartCoroutine(TestSingleStage(stage));

            if (!isTesting) break;

            if (currentStageIndex < stageConfigs.Count - 1)
                yield return new WaitForSeconds(delayBetweenStages);
        }

        isTesting = false;
        LogDebug("=== 所有 Stage 测试完成 ===");
    }

    private IEnumerator TestSingleStage(StageConfig stage)
    {
        if (stage.dialogueBlocks == null || stage.dialogueBlocks.Count == 0)
        {
            LogDebug($"Stage {stage.stageId} 没有对话块");
            yield break;
        }

        for (currentDialogueIndex = 0; currentDialogueIndex < stage.dialogueBlocks.Count; currentDialogueIndex++)
        {
            if (!isTesting) break;

            DialogueBlockConfig block = stage.dialogueBlocks[currentDialogueIndex];
            LogDebug($"播放对话块 {currentDialogueIndex + 1}/{stage.dialogueBlocks.Count}: {block.dialogueBlockFileId}");

            yield return StartCoroutine(TestSingleDialogue(block));

            if (!isTesting) break;
            yield return new WaitForSeconds(delayBetweenDialogues);
        }

        gameFlowController.TryProgressToNextStage();
    }

    private IEnumerator TestSingleDialogue(DialogueBlockConfig block)
    {
        dialogueRunning = true;

        // 开始对话块
        gameFlowController.StartDialogueBlock(block.dialogueBlockFileId);

        // 等待对话完成（由 GameFlowController.OnDialogueBlockComplete 触发）
        System.Action<string> onComplete = (id) =>
        {
            if (id == block.dialogueBlockFileId)
                dialogueRunning = false;
        };
        gameFlowController.OnStageChanged.AddListener(_ => dialogueRunning = false); // 保险：Stage切换也能退出
        gameFlowController.OnAutoSaveRequested.AddListener(() => dialogueRunning = false);

        yield return new WaitUntil(() => !dialogueRunning);

        LogDebug($"对话块 {block.dialogueBlockFileId} 播放完成");
    }

    #endregion

    #region 调试 GUI

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!enableDebugLog) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== 对话测试器 ===", new GUIStyle("label") { fontStyle = FontStyle.Bold });
        GUILayout.Label($"测试状态: {(isTesting ? "进行中" : "停止")}");
        GUILayout.Label($"当前Stage: {(currentStageIndex < stageConfigs.Count ? stageConfigs[currentStageIndex].stageId : "-")}");
        GUILayout.Label($"对话块索引: {currentDialogueIndex}");

        GUILayout.Space(10);

        if (GUILayout.Button(isTesting ? "停止测试" : "开始测试"))
        {
            if (isTesting) StopDialogueTest();
            else StartDialogueTest();
        }

        if (GUILayout.Button("测试当前Stage"))
            TestCurrentStage();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
#endif

    #endregion

    private void LogDebug(string msg)
    {
        if (enableDebugLog) Debug.Log($"[DialogueTester] {msg}");
    }

    private void LogError(string msg)
    {
        Debug.LogError($"[DialogueTester] {msg}");
    }
}
