using UnityEngine;

/// <summary>
/// �ҵĵ���ͼ�꽻����Ϊ - ���Խ�����һStage
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