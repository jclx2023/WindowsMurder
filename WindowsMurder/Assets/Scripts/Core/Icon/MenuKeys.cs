/// <summary>
/// �Ҽ��˵���ı��ػ�Key��������
/// Key��Χ��2000-2999 (�Ҽ��˵���ר��)
/// </summary>
public static class MenuKeys
{
    // �ļ�������
    public const string OPEN = "2001";           // ��
    public const string OPEN_WITH = "2002";     // �򿪷�ʽ
    public const string EDIT = "2003";          // �༭
    public const string VIEW = "2004";          // �鿴

    // �༭������
    public const string COPY = "2010";          // ����
    public const string CUT = "2011";           // ����
    public const string PASTE = "2012";         // ճ��
    public const string DELETE = "2013";        // ɾ��
    public const string RENAME = "2014";        // ������

    // ���Բ�����
    public const string PROPERTIES = "2020";    // ����
    public const string PERMISSIONS = "2021";   // Ȩ��
    public const string DETAILS = "2022";       // ��ϸ��Ϣ

    // ϵͳ������
    public const string REFRESH = "2030";       // ˢ��
    public const string EMPTY_RECYCLE = "2031"; // ��ջ���վ
    public const string RESTORE = "2032";       // ��ԭ
    public const string SEND_TO = "2033";       // ���͵�

    // ����������
    public const string NEW_FOLDER = "2040";    // �½��ļ���
    public const string NEW_FILE = "2041";      // �½��ļ�

    // ���������
    public const string RUN = "2050";           // ����
    public const string RUN_AS_ADMIN = "2051";  // �Թ���Ա�������
    public const string PIN_TO_TASKBAR = "2052"; // �̶���������
    public const string UNINSTALL = "2053";     // ж��

    // ��ɫ������
    public const string TALK = "2060";          // �Ի�
    public const string QUESTION = "2061";      // ѯ��
    public const string GIVE_ITEM = "2062";     // ������Ʒ
}

/// <summary>
/// �˵����������
/// �ṩ��ݵı��ػ��˵��������
/// </summary>
public static class MenuItemFactory
{
    // �ļ�����
    public static ContextMenuItem CreateOpenItem()
    {
        return new ContextMenuItem("open", MenuKeys.OPEN);
    }

    public static ContextMenuItem CreateEditItem()
    {
        return new ContextMenuItem("edit", MenuKeys.EDIT);
    }

    public static ContextMenuItem CreateViewItem()
    {
        return new ContextMenuItem("view", MenuKeys.VIEW);
    }

    // �༭����
    public static ContextMenuItem CreateCopyItem()
    {
        return new ContextMenuItem("copy", MenuKeys.COPY);
    }

    public static ContextMenuItem CreateCutItem()
    {
        return new ContextMenuItem("cut", MenuKeys.CUT);
    }

    public static ContextMenuItem CreatePasteItem()
    {
        return new ContextMenuItem("paste", MenuKeys.PASTE);
    }

    public static ContextMenuItem CreateDeleteItem()
    {
        return new ContextMenuItem("delete", MenuKeys.DELETE);
    }

    public static ContextMenuItem CreateRenameItem()
    {
        return new ContextMenuItem("rename", MenuKeys.RENAME);
    }

    // ���Բ���
    public static ContextMenuItem CreatePropertiesItem()
    {
        return new ContextMenuItem("properties", MenuKeys.PROPERTIES);
    }

    public static ContextMenuItem CreateDetailsItem()
    {
        return new ContextMenuItem("details", MenuKeys.DETAILS);
    }

    // ϵͳ����
    public static ContextMenuItem CreateRefreshItem()
    {
        return new ContextMenuItem("refresh", MenuKeys.REFRESH);
    }

    public static ContextMenuItem CreateEmptyRecycleItem()
    {
        return new ContextMenuItem("empty_recycle", MenuKeys.EMPTY_RECYCLE);
    }

    public static ContextMenuItem CreateRestoreItem()
    {
        return new ContextMenuItem("restore", MenuKeys.RESTORE);
    }

    // �������
    public static ContextMenuItem CreateRunItem()
    {
        return new ContextMenuItem("run", MenuKeys.RUN);
    }

    public static ContextMenuItem CreateRunAsAdminItem()
    {
        return new ContextMenuItem("run_admin", MenuKeys.RUN_AS_ADMIN);
    }

    // ��ɫ����
    public static ContextMenuItem CreateTalkItem()
    {
        return new ContextMenuItem("talk", MenuKeys.TALK);
    }

    public static ContextMenuItem CreateQuestionItem()
    {
        return new ContextMenuItem("question", MenuKeys.QUESTION);
    }

    // ���ָ��ߵĲ˵���
    public static ContextMenuItem CreateDeleteItemWithSeparator()
    {
        return new ContextMenuItem("delete", MenuKeys.DELETE, true, true);
    }

    public static ContextMenuItem CreatePropertiesItemWithSeparator()
    {
        return new ContextMenuItem("properties", MenuKeys.PROPERTIES, true, true);
    }
}