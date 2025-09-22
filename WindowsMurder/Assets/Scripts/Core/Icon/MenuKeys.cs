/// <summary>
/// 右键菜单项的本地化Key常量定义
/// Key范围：2000-2999 (右键菜单项专用)
/// </summary>
public static class MenuKeys
{
    // 文件操作类
    public const string OPEN = "2001";           // 打开
    public const string OPEN_WITH = "2002";     // 打开方式
    public const string EDIT = "2003";          // 编辑
    public const string VIEW = "2004";          // 查看

    // 编辑操作类
    public const string COPY = "2010";          // 复制
    public const string CUT = "2011";           // 剪切
    public const string PASTE = "2012";         // 粘贴
    public const string DELETE = "2013";        // 删除
    public const string RENAME = "2014";        // 重命名

    // 属性操作类
    public const string PROPERTIES = "2020";    // 属性
    public const string PERMISSIONS = "2021";   // 权限
    public const string DETAILS = "2022";       // 详细信息

    // 系统工具类
    public const string REFRESH = "2030";       // 刷新
    public const string EMPTY_RECYCLE = "2031"; // 清空回收站
    public const string RESTORE = "2032";       // 还原
    public const string SEND_TO = "2033";       // 发送到

    // 创建操作类
    public const string NEW_FOLDER = "2040";    // 新建文件夹
    public const string NEW_FILE = "2041";      // 新建文件

    // 程序操作类
    public const string RUN = "2050";           // 运行
    public const string RUN_AS_ADMIN = "2051";  // 以管理员身份运行
    public const string PIN_TO_TASKBAR = "2052"; // 固定到任务栏
    public const string UNINSTALL = "2053";     // 卸载

    // 角色交互类
    public const string TALK = "2060";          // 对话
    public const string QUESTION = "2061";      // 询问
    public const string GIVE_ITEM = "2062";     // 给予物品
}

/// <summary>
/// 菜单项创建工具类
/// 提供便捷的本地化菜单项创建方法
/// </summary>
public static class MenuItemFactory
{
    // 文件操作
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

    // 编辑操作
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

    // 属性操作
    public static ContextMenuItem CreatePropertiesItem()
    {
        return new ContextMenuItem("properties", MenuKeys.PROPERTIES);
    }

    public static ContextMenuItem CreateDetailsItem()
    {
        return new ContextMenuItem("details", MenuKeys.DETAILS);
    }

    // 系统工具
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

    // 程序操作
    public static ContextMenuItem CreateRunItem()
    {
        return new ContextMenuItem("run", MenuKeys.RUN);
    }

    public static ContextMenuItem CreateRunAsAdminItem()
    {
        return new ContextMenuItem("run_admin", MenuKeys.RUN_AS_ADMIN);
    }

    // 角色交互
    public static ContextMenuItem CreateTalkItem()
    {
        return new ContextMenuItem("talk", MenuKeys.TALK);
    }

    public static ContextMenuItem CreateQuestionItem()
    {
        return new ContextMenuItem("question", MenuKeys.QUESTION);
    }

    // 带分隔线的菜单项
    public static ContextMenuItem CreateDeleteItemWithSeparator()
    {
        return new ContextMenuItem("delete", MenuKeys.DELETE, true, true);
    }

    public static ContextMenuItem CreatePropertiesItemWithSeparator()
    {
        return new ContextMenuItem("properties", MenuKeys.PROPERTIES, true, true);
    }
}