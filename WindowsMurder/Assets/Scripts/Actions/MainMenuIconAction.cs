using UnityEngine;

/// <summary>
/// MainMenu����Iconͳһ�����ű�
/// ͨ�����ò�ͬ�Ĺ������ͣ�����GlobalActionManager�Ķ�Ӧ����
/// </summary>
public class MainMenuIconAction : IconAction
{
    /// <summary>
    /// MainMenu֧�ֵ�ȫ�ֹ�������
    /// </summary>
    public enum MainMenuFunction
    {
        NewGame,        // ����Ϸ
        Continue,       // ������Ϸ
        Language,       // ��������
        Display,        // ��ʾ����
        Credits         // ������Ա
    }

    [Header("MainMenu��������")]
    public MainMenuFunction functionType = MainMenuFunction.NewGame;

    /// <summary>
    /// ����Ƿ����ִ�н���
    /// </summary>
    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;

        // �����飺Continue������Ҫ�д浵
        if (functionType == MainMenuFunction.Continue)
        {
            if (GlobalActionManager.Instance == null)
            {
                Debug.LogWarning("MainMenuIconAction: GlobalActionManagerδ��ʼ��");
                return false;
            }

            bool hasGameSave = GlobalActionManager.Instance.HasGameSave();
            if (!hasGameSave)
            {
                Debug.Log("MainMenuIconAction: û�д浵���޷�������Ϸ");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// ִ�н�����Ϊ
    /// </summary>
    public override void Execute()
    {
        // ���GlobalActionManager�Ƿ����
        if (GlobalActionManager.Instance == null)
        {
            Debug.LogError("MainMenuIconAction: GlobalActionManagerδ��ʼ�����޷�ִ�в���");
            return;
        }

        // ���ݹ������͵��ö�Ӧ��GlobalActionManager����
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
                Debug.LogWarning($"MainMenuIconAction: δ֪�Ĺ������� {functionType}");
                break;
        }
    }

    /// <summary>
    /// ����ִ��ǰ�Ļص�
    /// </summary>
    protected override void OnBeforeExecute()
    {
        base.OnBeforeExecute();

        // ���������ﲥ���ض�����Ч
        // ������ʾloadingЧ����
        //Debug.Log($"MainMenuIconAction: ׼��ִ�� {functionType}");
    }

    /// <summary>
    /// ����ִ�к�Ļص�
    /// </summary>
    protected override void OnAfterExecute()
    {
        base.OnAfterExecute();

        // �����������¼�û�������־
        // ���߸���ͳ����Ϣ��
        Debug.Log($"MainMenuIconAction: {functionType} ִ�����");
    }
}