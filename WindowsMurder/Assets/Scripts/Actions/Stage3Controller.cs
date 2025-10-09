using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Stage3流程控制器
/// </summary>
public class Stage3Controller : MonoBehaviour
{
    private enum Stage3Phase
    {
        InitialDialogue,        // 004播放中
        WaitingForExploration,  // 等待单击触发005
        ExplorationDialogue,    // 005播放中
        Exploring,              // 探索阶段
        WaitingForFinale,       // 等待单击触发009
        FinaleDialogue,         // 009播放中
    }

    [Header("组件引用")]
    [SerializeField] private GameFlowController flowController;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("桌面Icons")]
    [SerializeField] private List<GameObject> desktopIcons;

    [Header("流程配置")]
    [SerializeField] private string targetPathId = "DFilesWorks";
    [SerializeField] private string dialogueBlock004 = "004";
    [SerializeField] private string dialogueBlock005 = "005";
    [SerializeField] private string dialogueBlock009 = "009";
    [SerializeField] private string nextStageId = "Stage4_Desktop";

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    private ExplorerManager explorerManager;
    private GameObject ieIconInExplorer;
    private List<GameObject> explorerIcons;
    private Stage3Phase currentPhase = Stage3Phase.InitialDialogue;
    private bool mouseListeningEnabled = false;
    private bool flowStarted = false;

    void OnEnable()
    {
        DialogueUI.OnLineStarted += OnDialogueLineStarted;
        GameEvents.OnDialogueBlockCompleted += OnDialogueBlockCompleted;
        flowController.OnClueUnlocked.AddListener(OnAnyClueUnlocked);
    }

    void OnDisable()
    {
        DialogueUI.OnLineStarted -= OnDialogueLineStarted;
        GameEvents.OnDialogueBlockCompleted -= OnDialogueBlockCompleted;
        flowController.OnClueUnlocked.RemoveListener(OnAnyClueUnlocked);
    }

    void Update()
    {
        if (mouseListeningEnabled && Input.GetMouseButtonDown(0))
        {
            OnMouseClicked();
        }
    }

    /// <summary>
    /// 设置Explorer引用（由Initializer调用）
    /// </summary>
    public void SetExplorerReference(ExplorerManager explorer)
    {
        explorerManager = explorer;
        ExplorerIconGetter iconGetter = explorerManager.GetComponent<ExplorerIconGetter>();
        ieIconInExplorer = iconGetter.GetIEIcon();
        explorerIcons = iconGetter.GetProgramIcons();

        if (!flowStarted)
        {
            StartCoroutine(StartStage3Flow());
            flowStarted = true;
        }
    }

    private IEnumerator StartStage3Flow()
    {
        yield return null;
        yield return null;

        explorerManager.NavigateToPath(targetPathId);
        flowController.StartDialogueBlock(dialogueBlock004);

        if (debugMode) Debug.Log("[Stage3] 开始初始对话004");
    }

    private void SwitchPhase(Stage3Phase newPhase)
    {
        currentPhase = newPhase;

        switch (newPhase)
        {
            case Stage3Phase.WaitingForExploration:
            case Stage3Phase.WaitingForFinale:
                mouseListeningEnabled = true;
                break;
            default:
                mouseListeningEnabled = false;
                break;
        }

        if (debugMode) Debug.Log($"[Stage3] 阶段切换: {newPhase}");
    }

    private void OnDialogueLineStarted(string lineId, string characterId, string blockId, bool isPresetMode)
    {
        if (blockId != dialogueBlock004) return;

        if (lineId == "11")
        {
            ieIconInExplorer.SetActive(true);
        }
        else if (lineId == "13")
        {
            ieIconInExplorer.SetActive(false);
            foreach (var icon in explorerIcons) icon.SetActive(false);
            foreach (var icon in desktopIcons) icon.SetActive(true);
        }
    }

    private void OnDialogueBlockCompleted(string blockId)
    {
        if (blockId == dialogueBlock004 && currentPhase == Stage3Phase.InitialDialogue)
        {
            SwitchPhase(Stage3Phase.WaitingForExploration);
        }
        else if (blockId == dialogueBlock005 && currentPhase == Stage3Phase.ExplorationDialogue)
        {
            SwitchPhase(Stage3Phase.Exploring);
        }
        else if (blockId == dialogueBlock009 && currentPhase == Stage3Phase.FinaleDialogue)
        {
            flowController.LoadStage(nextStageId);
        }
    }

    private void OnAnyClueUnlocked(string clueId)
    {
        if (currentPhase != Stage3Phase.Exploring) return;

        if (flowController.IsStageProgressConditionMet() && !dialogueManager.IsDialogueActive())
        {
            SwitchPhase(Stage3Phase.WaitingForFinale);
        }
    }

    private void OnMouseClicked()
    {
        mouseListeningEnabled = false;

        if (currentPhase == Stage3Phase.WaitingForExploration)
        {
            SwitchPhase(Stage3Phase.ExplorationDialogue);
            flowController.StartDialogueBlock(dialogueBlock005);
        }
        else if (currentPhase == Stage3Phase.WaitingForFinale)
        {
            SwitchPhase(Stage3Phase.FinaleDialogue);
            flowController.StartDialogueBlock(dialogueBlock009);
        }
    }
}