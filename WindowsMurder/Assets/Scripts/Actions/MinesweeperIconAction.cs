using System.Linq;
using UnityEngine;

/// <summary>
/// 扫雷游戏Icon交互
/// 首次播放介绍对话后启动游戏，后续直接启动游戏（支持多窗口）
/// </summary>
public class MinesweeperIconAction : IconAction
{
    [Header("=== 对话配置 ===")]
    [SerializeField] private string introDialogueBlockId = "011";

    [Header("=== 游戏配置 ===")]
    [SerializeField] private GameObject minesweeperPrefab;
    [SerializeField] private Transform spawnParent;

    [Header("=== 调试 ===")]
    [SerializeField] private bool hasPlayedIntro = false;

    private GameFlowController flowController;

    void Awake()
    {
        flowController = FindObjectOfType<GameFlowController>();
    }

    void OnEnable()
    {
        GameEvents.OnDialogueBlockCompleted += OnDialogueCompleted;
    }

    void OnDisable()
    {
        GameEvents.OnDialogueBlockCompleted -= OnDialogueCompleted;
    }

    void Start()
    {
        // 检查介绍对话是否已完成
        if (flowController != null)
        {
            hasPlayedIntro = flowController.GetCompletedBlocksSafe().Contains(introDialogueBlockId);
        }
    }

    public override void Execute()
    {
        if (hasPlayedIntro)
        {
            // 已播放过介绍，直接启动游戏
            LaunchGame();
        }
        else
        {
            // 首次交互，播放介绍对话
            PlayIntroDialogue();
        }
    }

    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;

        // 已播放介绍：检查是否有预制体
        if (hasPlayedIntro)
        {
            return minesweeperPrefab != null;
        }

        // 未播放介绍：检查是否有对话块配置
        return !string.IsNullOrEmpty(introDialogueBlockId) && flowController != null;
    }

    /// <summary>
    /// 播放介绍对话
    /// </summary>
    private void PlayIntroDialogue()
    {
        Debug.Log($"[{actionName}] 播放介绍对话: {introDialogueBlockId}");
        flowController.StartDialogueBlock(introDialogueBlockId);
    }

    /// <summary>
    /// 启动扫雷游戏
    /// </summary>
    private void LaunchGame()
    {
        // 确定生成父级
        Transform parent = spawnParent;

        // 生成游戏实例（支持多窗口）
        Instantiate(minesweeperPrefab, parent);
        Debug.Log($"[{actionName}] 启动扫雷游戏");
    }

    /// <summary>
    /// 对话完成回调
    /// </summary>
    private void OnDialogueCompleted(string blockId)
    {
        if (blockId == introDialogueBlockId)
        {
            hasPlayedIntro = true;

            // 介绍对话完成后立即启动游戏
            LaunchGame();
        }
    }
}