using UnityEngine;

/// <summary>
/// MainMenu场景Icon统一交互脚本
/// 通过配置不同的功能类型，调用GlobalActionManager的对应方法
/// </summary>
public class MainMenuIconAction : IconAction
{
    /// <summary>
    /// MainMenu支持的全局功能类型
    /// </summary>
    public enum MainMenuFunction
    {
        NewGame,        // 新游戏
        Continue,       // 继续游戏
        Language,       // 语言设置
        Display,        // 显示设置
        Credits         // 制作人员
    }

    [Header("MainMenu功能设置")]
    public MainMenuFunction functionType = MainMenuFunction.NewGame;

    /// <summary>
    /// 检查是否可以执行交互
    /// </summary>
    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;

        // 特殊检查：Continue功能需要有存档
        if (functionType == MainMenuFunction.Continue)
        {
            if (GlobalActionManager.Instance == null)
            {
                Debug.LogWarning("MainMenuIconAction: GlobalActionManager未初始化");
                return false;
            }

            bool hasGameSave = GlobalActionManager.Instance.HasGameSave();
            if (!hasGameSave)
            {
                Debug.Log("MainMenuIconAction: 没有存档，无法继续游戏");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 执行交互行为
    /// </summary>
    public override void Execute()
    {
        // 检查GlobalActionManager是否可用
        if (GlobalActionManager.Instance == null)
        {
            Debug.LogError("MainMenuIconAction: GlobalActionManager未初始化，无法执行操作");
            return;
        }

        // 根据功能类型调用对应的GlobalActionManager方法
        switch (functionType)
        {
            case MainMenuFunction.NewGame:
                GlobalActionManager.Instance.NewGame();
                break;

            case MainMenuFunction.Continue:
                GlobalActionManager.Instance.Continue();
                break;

            case MainMenuFunction.Language:
                GlobalActionManager.Instance.OpenLanguageSettings();
                break;

            case MainMenuFunction.Display:
                GlobalActionManager.Instance.OpenDisplaySettings();
                break;

            case MainMenuFunction.Credits:
                GlobalActionManager.Instance.OpenCredits();
                break;

            default:
                Debug.LogWarning($"MainMenuIconAction: 未知的功能类型 {functionType}");
                break;
        }
    }

    /// <summary>
    /// 交互执行前的回调
    /// </summary>
    protected override void OnBeforeExecute()
    {
        base.OnBeforeExecute();

        // 可以在这里播放特定的音效
        // 或者显示loading效果等
        //Debug.Log($"MainMenuIconAction: 准备执行 {functionType}");
    }

    /// <summary>
    /// 交互执行后的回调
    /// </summary>
    protected override void OnAfterExecute()
    {
        base.OnAfterExecute();

        // 可以在这里记录用户操作日志
        // 或者更新统计信息等
        Debug.Log($"MainMenuIconAction: {functionType} 执行完成");
    }
}