using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Icon交互行为基类
/// 所有具体的icon交互都继承自这个类
/// </summary>
public abstract class IconAction : MonoBehaviour
{
    [Header("基础设置")]
    public string actionName;           // 交互行为名称（用于调试）
    public bool isEnabled = true;       // 是否启用交互

    /// <summary>
    /// 执行交互行为 - 子类必须实现
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// 检查是否可以执行交互（可选重写）
    /// </summary>
    public virtual bool CanExecute()
    {
        return isEnabled;
    }

    /// <summary>
    /// 交互执行前的回调（可选重写）
    /// </summary>
    protected virtual void OnBeforeExecute()
    {
        // 可以在这里播放通用音效、显示反馈等
    }

    /// <summary>
    /// 交互执行后的回调（可选重写）
    /// </summary>
    protected virtual void OnAfterExecute()
    {
        // 可以在这里记录日志、更新状态等
    }

    /// <summary>
    /// 执行交互的完整流程
    /// </summary>
    public void TryExecute()
    {
        if (!CanExecute())
        {
            Debug.Log($"IconAction: {actionName} 无法执行");
            return;
        }

        OnBeforeExecute();
        Execute();
        OnAfterExecute();
    }
}

/// <summary>
/// 右键菜单项数据 - 支持多语言Key模式
/// </summary>
[Serializable]
public class ContextMenuItem
{
    [Header("基础设置")]
    public string itemId;           // 菜单项ID，用于识别点击的是哪个选项
    public bool isEnabled = true;   // 是否可用
    public bool showSeparator;      // 在此项后显示分隔线

    [Header("多语言设置")]
    public bool useLocalizationKey = true;  // 是否使用本地化Key
    public string itemName;         // 显示名称：Key模式时存储本地化Key，直接模式时存储显示文本

    [Header("调试信息")]
    [SerializeField] private string previewText; // Inspector中显示的预览文本（运行时忽略）

    /// <summary>
    /// 构造函数 - 本地化Key模式（推荐）
    /// </summary>
    public ContextMenuItem(string id, string localizationKey, bool enabled = true, bool separator = false)
    {
        itemId = id;
        itemName = localizationKey;
        useLocalizationKey = true;
        isEnabled = enabled;
        showSeparator = separator;
    }

    /// <summary>
    /// 构造函数 - 直接文本模式（兼容旧代码）
    /// </summary>
    public static ContextMenuItem CreateDirectText(string id, string displayText, bool enabled = true, bool separator = false)
    {
        var item = new ContextMenuItem();
        item.itemId = id;
        item.itemName = displayText;
        item.useLocalizationKey = false;
        item.isEnabled = enabled;
        item.showSeparator = separator;
        return item;
    }

    /// <summary>
    /// 默认构造函数（用于Inspector）
    /// </summary>
    public ContextMenuItem()
    {
        useLocalizationKey = true;
        isEnabled = true;
        showSeparator = false;
    }

    /// <summary>
    /// 获取显示文本 - 支持运行时翻译
    /// </summary>
    public string GetDisplayText()
    {
        if (!useLocalizationKey)
        {
            return itemName; // 直接返回文本
        }

        // 尝试通过LanguageManager翻译
        if (LanguageManager.Instance != null)
        {
            string translatedText = LanguageManager.Instance.GetText(itemName);
            if (!string.IsNullOrEmpty(translatedText))
            {
                return translatedText;
            }
        }

        // 翻译失败时的降级处理
        return GetFallbackText();
    }

    /// <summary>
    /// 获取降级文本（翻译失败时使用）
    /// </summary>
    private string GetFallbackText()
    {
        // 可以根据Key返回默认的英文文本
        switch (itemName)
        {
            case MenuKeys.OPEN: return "Open";
            case MenuKeys.COPY: return "Copy";
            case MenuKeys.DELETE: return "Delete";
            case MenuKeys.PROPERTIES: return "Properties";
            case MenuKeys.RENAME: return "Rename";
            case MenuKeys.REFRESH: return "Refresh";
            case MenuKeys.EDIT: return "Edit";
            case MenuKeys.VIEW: return "View";
            case MenuKeys.CUT: return "Cut";
            case MenuKeys.PASTE: return "Paste";
            case MenuKeys.RUN: return "Run";
            case MenuKeys.TALK: return "Talk";
            default: return itemName; // 返回Key本身
        }
    }
}

/// <summary>
/// 可交互桌面图标基础组件
/// 支持双击交互和右键菜单，集成了交互行为管理
/// </summary>
public class InteractableIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI组件引用")]
    public Image iconImage;         // 图标图片组件
    public TextMeshProUGUI nameText;           // 名称文本组件
    public GameObject selectionHighlight;  // 选中高亮效果

    [Header("右键菜单")]
    public bool canShowContextMenu = true;     // 是否可以显示右键菜单
    public List<ContextMenuItem> contextMenuItems = new List<ContextMenuItem>();
    public GameObject contextMenuPrefab;       // 右键菜单预制体引用

    [Header("状态设置")]
    public bool isLocked = false;              // 是否锁定（无法交互）
    public bool isCorrupted = false;           // 是否损坏状态
    public bool isHidden = false;              // 是否隐藏状态

    // 交互设置 - 硬编码常量
    private const float DOUBLE_CLICK_THRESHOLD = 0.5f;
    private const bool SHOW_TOOLTIP = true;

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
        // 应用初始状态效果
        ApplyVisualState();
    }

    void OnEnable()
    {
        if (!AllIcons.Contains(this))
            AllIcons.Add(this);
    }

    void OnDisable()
    {
        // 清理所有状态
        CleanupState();

        AllIcons.Remove(this);
        if (CurrentSelectedIcon == this)
            CurrentSelectedIcon = null;
    }

    void OnDestroy()
    {
        CleanupState();

        AllIcons.Remove(this);
        if (CurrentSelectedIcon == this)
            CurrentSelectedIcon = null;
    }

    #endregion

    #region 状态清理

    /// <summary>
    /// 清理icon的所有交互状态（在禁用或销毁时调用）
    /// </summary>
    private void CleanupState()
    {
        // 停止所有协程
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        // 隐藏高亮效果
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }

        // 重置选中状态
        isSelected = false;

        // 隐藏右键菜单
        HideContextMenu();

        //Debug.Log($"InteractableIcon: {name} 已清理状态");
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 隐藏悬停效果
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(false);
        }
    }

    #endregion

    #region 交互处理

    void HandleLeftClick()
    {
        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime) < DOUBLE_CLICK_THRESHOLD;

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
        // 检查是否允许显示右键菜单
        if (!canShowContextMenu)
        {
            Debug.Log($"InteractableIcon: {name} 不支持右键菜单");
            return;
        }

        // 检查是否有菜单项
        if (contextMenuItems.Count == 0)
        {
            Debug.Log($"InteractableIcon: {name} 没有配置右键菜单项");
            return;
        }

        // 先选中图标
        SelectIcon();

        // 显示右键菜单
        ShowContextMenu(eventData.position);
    }

    void HandleDoubleClick()
    {
        // 双击时清理悬停效果（防止交互后悬停效果残留）
        CleanupHoverEffect();

        // 执行交互行为
        HandleIconInteraction();
    }
    private void CleanupHoverEffect()
    {
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(false);
        }
    }
    /// <summary>
    /// 处理icon的双击交互 - 集成的交互管理逻辑
    /// </summary>
    private void HandleIconInteraction()
    {
        // 查找icon上的交互行为组件
        IconAction iconAction = GetComponent<IconAction>();

        if (iconAction != null)
        {
            Debug.Log($"InteractableIcon: 执行 {name} 的交互行为");
            iconAction.TryExecute();
        }
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

    #endregion

    #region 右键菜单

    void ShowContextMenu(Vector2 screenPosition)
    {
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
    }

    void HideContextMenu()
    {
        CleanupHoverEffect();
        if (activeContextMenu != null)
        {
            activeContextMenu.Hide();
            activeContextMenu = null;
        }
    }

    void OnContextMenuItemSelected(string itemId)
    {
        // 只触发静态事件，让其他系统处理右键菜单逻辑
        OnContextMenuItemClicked?.Invoke(this, itemId);
        Debug.Log($"InteractableIcon: {name} 选择了菜单项 {itemId}");
    }

    #endregion

    #region 状态管理

    void ApplyVisualState()
    {
        if (iconImage == null) return;

        // 先保留当前 alpha
        float currentAlpha = iconImage.color.a;
        Color iconColor = new Color(1f, 1f, 1f, currentAlpha);

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
    #endregion
}