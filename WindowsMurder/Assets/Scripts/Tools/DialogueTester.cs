using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �Ի�ϵͳ������ - ���ڲ��ԶԻ�����
/// ���������������ƣ���˳�򲥷ŶԻ���
/// </summary>
public class DialogueTester : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private bool autoStartTest = false;
    [SerializeField] private float delayBetweenDialogues = 1.5f;
    [SerializeField] private float delayBetweenStages = 2.5f;

    [Header("�������")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("����")]
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
            LogError("δ�ҵ� GameFlowController �����");
            return;
        }

        stageConfigs = new List<StageConfig>(gameFlowController.GetStageConfigsSafe());
        LogDebug($"��ʼ����ɣ��ҵ� {stageConfigs.Count} �� Stage");
    }

    private IEnumerator DelayedAutoStart()
    {
        yield return new WaitForSeconds(1f);
        StartDialogueTest();
    }

    #region �����ӿ�

    [ContextMenu("��ʼ�Ի�����")]
    public void StartDialogueTest()
    {
        if (isTesting || stageConfigs.Count == 0) return;

        isTesting = true;
        currentStageIndex = 0;
        currentDialogueIndex = 0;

        LogDebug("=== ��ʼ�Ի�ϵͳ���� ===");
        testCoroutine = StartCoroutine(TestAllStages());
    }

    [ContextMenu("ֹͣ�Ի�����")]
    public void StopDialogueTest()
    {
        if (!isTesting) return;

        isTesting = false;
        if (testCoroutine != null)
            StopCoroutine(testCoroutine);

        LogDebug("=== �Ի�ϵͳ������ֹͣ ===");
    }

    [ContextMenu("���Ե�ǰStage")]
    public void TestCurrentStage()
    {
        if (gameFlowController == null) return;

        string stageId = gameFlowController.GetCurrentStageIdSafe();
        StageConfig stage = stageConfigs.Find(s => s.stageId == stageId);
        if (stage == null)
        {
            LogError($"�Ҳ��� Stage ����: {stageId}");
            return;
        }

        if (isTesting) StopDialogueTest();

        isTesting = true;
        testCoroutine = StartCoroutine(TestSingleStage(stage));
    }

    #endregion

    #region ����Э��

    private IEnumerator TestAllStages()
    {
        for (currentStageIndex = 0; currentStageIndex < stageConfigs.Count; currentStageIndex++)
        {
            if (!isTesting) break;

            StageConfig stage = stageConfigs[currentStageIndex];
            LogDebug($"=== ��ʼ���� Stage: {stage.stageId} ===");

            gameFlowController.LoadStage(stage.stageId);

            yield return StartCoroutine(TestSingleStage(stage));

            if (!isTesting) break;

            if (currentStageIndex < stageConfigs.Count - 1)
                yield return new WaitForSeconds(delayBetweenStages);
        }

        isTesting = false;
        LogDebug("=== ���� Stage ������� ===");
    }

    private IEnumerator TestSingleStage(StageConfig stage)
    {
        if (stage.dialogueBlocks == null || stage.dialogueBlocks.Count == 0)
        {
            LogDebug($"Stage {stage.stageId} û�жԻ���");
            yield break;
        }

        for (currentDialogueIndex = 0; currentDialogueIndex < stage.dialogueBlocks.Count; currentDialogueIndex++)
        {
            if (!isTesting) break;

            DialogueBlockConfig block = stage.dialogueBlocks[currentDialogueIndex];
            LogDebug($"���ŶԻ��� {currentDialogueIndex + 1}/{stage.dialogueBlocks.Count}: {block.dialogueBlockFileId}");

            yield return StartCoroutine(TestSingleDialogue(block));

            if (!isTesting) break;
            yield return new WaitForSeconds(delayBetweenDialogues);
        }

        gameFlowController.TryProgressToNextStage();
    }

    private IEnumerator TestSingleDialogue(DialogueBlockConfig block)
    {
        dialogueRunning = true;

        // ��ʼ�Ի���
        gameFlowController.StartDialogueBlock(block.dialogueBlockFileId);

        // �ȴ��Ի���ɣ��� GameFlowController.OnDialogueBlockComplete ������
        System.Action<string> onComplete = (id) =>
        {
            if (id == block.dialogueBlockFileId)
                dialogueRunning = false;
        };
        gameFlowController.OnStageChanged.AddListener(_ => dialogueRunning = false); // ���գ�Stage�л�Ҳ���˳�
        gameFlowController.OnAutoSaveRequested.AddListener(() => dialogueRunning = false);

        yield return new WaitUntil(() => !dialogueRunning);

        LogDebug($"�Ի��� {block.dialogueBlockFileId} �������");
    }

    #endregion

    #region ���� GUI

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!enableDebugLog) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== �Ի������� ===", new GUIStyle("label") { fontStyle = FontStyle.Bold });
        GUILayout.Label($"����״̬: {(isTesting ? "������" : "ֹͣ")}");
        GUILayout.Label($"��ǰStage: {(currentStageIndex < stageConfigs.Count ? stageConfigs[currentStageIndex].stageId : "-")}");
        GUILayout.Label($"�Ի�������: {currentDialogueIndex}");

        GUILayout.Space(10);

        if (GUILayout.Button(isTesting ? "ֹͣ����" : "��ʼ����"))
        {
            if (isTesting) StopDialogueTest();
            else StartDialogueTest();
        }

        if (GUILayout.Button("���Ե�ǰStage"))
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
