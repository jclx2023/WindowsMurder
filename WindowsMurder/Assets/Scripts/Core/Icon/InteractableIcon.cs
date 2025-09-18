using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 右键菜单项数据
/// </summary>
[Serializable]
public class ContextMenuItem
{
    public string itemName;     // 菜单项名称，如"打开"、"属性"、"删除"
    public string itemId;       // 菜单项ID，用于识别点击的是哪个选项
    public Sprite itemIcon;     // 菜单项图标（可选）
    public bool isEnabled = true;   // 是否可用
    public bool showSeparator;  // 在此项后显示分隔线
}

/// <summary>
/// 图标类型枚举
/// </summary>
public enum IconType
{
    File,           // 文件
    Folder,         // 文件夹
    Program,        // 程序
    SystemTool,     // 系统工具（如回收站、控制面板）
    Character       // 拟人化角色
}

/// <summary>
/// 可交互桌面图标基础组件
/// 支持双击交互和右键菜单
/// </summary>
public class InteractableIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("图标基础设置")]
    public string iconName = "未命名";
    public IconType iconType = IconType.File;
    public Sprite iconSprite;
    public string iconId;           // 唯一标识符

    [Header("UI组件引用")]
    public Image iconImage;         // 图标图片组件
    public Text nameText;           // 名称文本组件
    public GameObject selectionHighlight;  // 选中高亮效果

    [Header("交互设置")]
    public float doubleClickThreshold = 0.5f;  // 双击时间阈值
    public bool showTooltip = true;            // 是否显示悬停提示

    [Header("右键菜单")]
    public List<ContextMenuItem> contextMenuItems = new List<ContextMenuItem>();
    public GameObject contextMenuPrefab;       // 右键菜单预制体引用

    [Header("状态设置")]
    public bool isLocked = false;              // 是否锁定（无法交互）
    public bool isCorrupted = false;           // 是否损坏状态
    public bool isHidden = false;              // 是否隐藏状态

    // 事件委托
    public static event Action<InteractableIcon> OnIconSelected;
    public static event Action<InteractableIcon> OnIconDoubleClicked;
    public static event Action<InteractableIcon, string> OnContextMenuItemClicked;
    public static event Action<InteractableIcon> OnIconHovered;

    // 私有变量
    private bool isSelected = false;
    private float lastClickTime = 0f;
    private IconContextMenu activeContextMenu;
    private Coroutine tooltipCoroutine;

    // 静态管理
    public static InteractableIcon CurrentSelectedIcon { get; private set; }
    public static List<InteractableIcon> AllIcons { get; private set; } = new List<InteractableIcon>();

    #region Unity生命周期

    void Start()
    {
        Initialize();
    }

    void OnEnable()
    {
        if (!AllIcons.Contains(this))
            AllIcons.Add(this);
    }

    void OnDisable()
    {
        AllIcons.Remove(this);
        if (CurrentSelectedIcon == this)
            CurrentSelectedIcon = null;
    }

    void OnDestroy()
    {
        AllIcons.Remove(this);
        if (CurrentSelectedIcon == this)
            CurrentSelectedIcon = null;
    }

    #endregion

    #region 初始化

    void Initialize()
    {
        // 设置图标和文本
        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
        }

        if (nameText != null)
        {
            nameText.text = iconName;
        }

        // 应用状态效果
        ApplyVisualState();
    }

    #endregion

    #region 鼠标交互事件

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            HandleRightClick(eventData);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isLocked) return;

        // 触发悬停事件
        OnIconHovered?.Invoke(this);

        // 显示悬停效果
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(true);
        }

        // 显示工具提示
        if (showTooltip)
        {
            if (tooltipCoroutine != null)
                StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 隐藏悬停效果
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(false);
        }

        // 隐藏工具提示
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        HideTooltip();
    }

    #endregion

    #region 交互处理

    void HandleLeftClick()
    {
        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime) < doubleClickThreshold;

        if (isDoubleClick)
        {
            // 双击处理
            HandleDoubleClick();
        }
        else
        {
            // 单击处理 - 选中图标
            SelectIcon();
        }

        lastClickTime = currentTime;
    }

    void HandleRightClick(PointerEventData eventData)
    {
        // 先选中图标
        SelectIcon();

        // 显示右键菜单
        ShowContextMenu(eventData.position);
    }

    void HandleDoubleClick()
    {
        Debug.Log($"双击了图标: {iconName} (类型: {iconType})");
        OnIconDoubleClicked?.Invoke(this);
    }

    #endregion

    #region 选中状态管理

    public void SelectIcon()
    {
        // 取消其他图标的选中状态
        if (CurrentSelectedIcon != null && CurrentSelectedIcon != this)
        {
            CurrentSelectedIcon.DeselectIcon();
        }

        // 设置当前图标为选中状态
        isSelected = true;
        CurrentSelectedIcon = this;

        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(true);
        }

        OnIconSelected?.Invoke(this);
        Debug.Log($"选中了图标: {iconName}");
    }

    public void DeselectIcon()
    {
        isSelected = false;
        if (CurrentSelectedIcon == this)
        {
            CurrentSelectedIcon = null;
        }

        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
    }

    public static void DeselectAllIcons()
    {
        foreach (var icon in AllIcons)
        {
            icon.DeselectIcon();
        }
        CurrentSelectedIcon = null;
    }

    #endregion

    #region 右键菜单

    void ShowContextMenu(Vector2 screenPosition)
    {
        if (contextMenuItems.Count == 0) return;

        // 隐藏已存在的菜单
        HideContextMenu();

        // 创建新的右键菜单
        if (contextMenuPrefab != null)
        {
            GameObject menuObject = Instantiate(contextMenuPrefab, GetComponentInParent<Canvas>().transform);
            activeContextMenu = menuObject.GetComponent<IconContextMenu>();

            if (activeContextMenu != null)
            {
                activeContextMenu.Show(screenPosition, contextMenuItems, OnContextMenuItemSelected);
            }
        }
        else
        {
            Debug.LogWarning($"InteractableIcon {iconName}: 缺少contextMenuPrefab引用");
        }
    }

    void HideContextMenu()
    {
        if (activeContextMenu != null)
        {
            activeContextMenu.Hide();
            // 由于Hide()会销毁GameObject，所以不需要设置为null
            // GameObject销毁后引用会自动变成null
        }
    }

    void OnContextMenuItemSelected(string itemId)
    {
        Debug.Log($"右键菜单选择: {iconName} -> {itemId}");
        OnContextMenuItemClicked?.Invoke(this, itemId);
    }

    #endregion

    #region 工具提示

    IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSeconds(1f); // 悬停1秒后显示提示

        // 这里可以显示工具提示
        Debug.Log($"显示提示: {iconName} ({iconType})");
        // 实际项目中可以调用UI管理器显示提示框
    }

    void HideTooltip()
    {
        // 隐藏工具提示
        // 实际项目中可以调用UI管理器隐藏提示框
    }

    #endregion

    #region 状态管理

    void ApplyVisualState()
    {
        if (iconImage == null) return;

        Color iconColor = Color.white;

        if (isLocked)
        {
            iconColor = Color.gray;
        }
        else if (isCorrupted)
        {
            iconColor = Color.red;
        }
        else if (isHidden)
        {
            iconColor = new Color(1f, 1f, 1f, 0.5f); // 半透明
        }

        iconImage.color = iconColor;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        ApplyVisualState();
    }

    public void SetCorrupted(bool corrupted)
    {
        isCorrupted = corrupted;
        ApplyVisualState();
    }

    public void SetHidden(bool hidden)
    {
        isHidden = hidden;
        ApplyVisualState();
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 更新图标显示
    /// </summary>
    public void UpdateIcon(Sprite newSprite, string newName = null)
    {
        if (newSprite != null && iconImage != null)
        {
            iconSprite = newSprite;
            iconImage.sprite = newSprite;
        }

        if (!string.IsNullOrEmpty(newName))
        {
            iconName = newName;
            if (nameText != null)
            {
                nameText.text = newName;
            }
        }
    }

    /// <summary>
    /// 添加右键菜单项
    /// </summary>
    public void AddContextMenuItem(ContextMenuItem item)
    {
        contextMenuItems.Add(item);
    }

    /// <summary>
    /// 移除右键菜单项
    /// </summary>
    public void RemoveContextMenuItem(string itemId)
    {
        contextMenuItems.RemoveAll(item => item.itemId == itemId);
    }

    /// <summary>
    /// 清空右键菜单
    /// </summary>
    public void ClearContextMenu()
    {
        contextMenuItems.Clear();
    }

    /// <summary>
    /// 设置整个右键菜单
    /// </summary>
    public void SetContextMenu(List<ContextMenuItem> newItems)
    {
        contextMenuItems.Clear();
        if (newItems != null)
        {
            contextMenuItems.AddRange(newItems);
        }
    }

    /// <summary>
    /// 启用/禁用菜单项
    /// </summary>
    public void SetMenuItemEnabled(string itemId, bool enabled)
    {
        var item = contextMenuItems.Find(i => i.itemId == itemId);
        if (item != null)
        {
            item.isEnabled = enabled;
        }
    }

    /// <summary>
    /// 检查是否选中
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }

    #endregion
}