using UnityEngine;

/// <summary>
/// 我的电脑图标交互行为 - 尝试进入下一Stage
/// </summary>
public class Stage1MyPCAction : IconAction
{
    private GameFlowController gameFlowController;

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
    }

    public override void Execute()
    {
        if (gameFlowController != null)
        {
            gameFlowController.TryProgressToNextStage();
        }
    }
}