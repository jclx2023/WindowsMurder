using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 图标右键菜单UI组件 - 支持多语言
/// 用于显示和管理图标的上下文菜单
/// </summary>
public class IconContextMenu : MonoBehaviour, IPointerExitHandler
{
    [Header("UI组件引用")]
    public RectTransform menuPanel;              // 菜单面板
    public Transform menuItemContainer;          // 菜单项容器
    public GameObject menuItemPrefab;            // 菜单项预制体
    public GameObject separatorPrefab;           // 分隔线预制体

    [Header("样式设置")]
    public float itemHeight = 30f;               // 菜单项高度
    public float separatorHeight = 5f;           // 分隔线高度
    public Vector2 menuPadding = new Vector2(5f, 5f);  // 菜单内边距
    public float menuMinWidth = 120f;            // 菜单最小宽度

    // 私有变量
    private List<ContextMenuItem> currentItems;
    private List<GameObject> instantiatedItems = new List<GameObject>();
    private Action<string> onItemSelected;
    private Canvas parentCanvas;
    private bool isVisible = false;
    private float hideTimer = 0f;
    private const float AUTO_HIDE_DELAY = 3f; // 自动隐藏延迟

    #region Unity生命周期

    void Awake()
    {
        // 获取Canvas相关组件
        parentCanvas = GetComponentInParent<Canvas>();

        // 初始状态设为隐藏
        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 检查是否需要自动隐藏
        if (isVisible)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer > AUTO_HIDE_DELAY)
            {
                Hide();
            }

            // 检查鼠标点击其他地方
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                if (!IsMouseOverMenu())
                {
                    Hide();
                }
            }
        }
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 显示右键菜单
    /// </summary>
    public void Show(Vector2 screenPosition, List<ContextMenuItem> items, Action<string> callback)
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("IconContextMenu: 菜单项列表为空");
            return;
        }

        // 保存数据
        currentItems = items;
        onItemSelected = callback;

        // 清理之前的菜单项
        ClearMenuItems();

        // 创建菜单项
        CreateMenuItems();

        // 显示菜单
        ShowMenu();

        // 调整菜单位置
        PositionMenu(screenPosition);

        // 重置计时器
        hideTimer = 0f;
        isVisible = true;
    }

    /// <summary>
    /// 隐藏右键菜单
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;

        isVisible = false;

        // 直接销毁整个菜单GameObject
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 菜单项创建

    void CreateMenuItems()
    {
        if (menuItemContainer == null || menuItemPrefab == null)
        {
            Debug.LogError("IconContextMenu: 缺少必要的UI组件引用");
            return;
        }

        foreach (var item in currentItems)
        {
            // 创建菜单项
            GameObject itemObj = Instantiate(menuItemPrefab, menuItemContainer);
            instantiatedItems.Add(itemObj);

            // 配置菜单项（带多语言支持）
            ConfigureMenuItem(itemObj, item);

            // 创建分隔线
            if (item.showSeparator && separatorPrefab != null)
            {
                GameObject separatorObj = Instantiate(separatorPrefab, menuItemContainer);
                instantiatedItems.Add(separatorObj);
            }
        }
    }

    void ConfigureMenuItem(GameObject itemObj, ContextMenuItem itemData)
    {
        // 获取组件 - 优先使用TextMeshPro
        Button button = itemObj.GetComponent<Button>();
        TextMeshProUGUI tmpText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        Text uiText = null;

        // 如果没有TMP组件，尝试获取传统Text组件
        if (tmpText == null)
        {
            uiText = itemObj.GetComponentInChildren<Text>();
        }

        // 设置文本 - 支持多语言翻译
        string displayText = itemData.GetDisplayText();

        if (tmpText != null)
        {
            tmpText.text = displayText;
            tmpText.color = itemData.isEnabled ? Color.black : Color.gray;
        }
        else if (uiText != null)
        {
            uiText.text = displayText;
            uiText.color = itemData.isEnabled ? Color.black : Color.gray;
        }
        else
        {
            Debug.LogWarning($"IconContextMenu: 菜单项 {itemObj.name} 没有找到文本组件");
        }

        // 设置按钮交互
        if (button != null)
        {
            button.interactable = itemData.isEnabled;

            if (itemData.isEnabled)
            {
                button.onClick.AddListener(() => OnMenuItemClicked(itemData.itemId));
            }
        }

        // 设置悬停效果
        SetupHoverEffect(itemObj, itemData.isEnabled);

        // 调试日志
        if (itemData.useLocalizationKey)
        {
            Debug.Log($"菜单项本地化: Key={itemData.itemName} → Text={displayText}");
        }
    }

    void SetupHoverEffect(GameObject itemObj, bool isEnabled)
    {
        if (!isEnabled) return;

        EventTrigger eventTrigger = itemObj.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = itemObj.AddComponent<EventTrigger>();
        }

        // 鼠标进入事件
        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => OnMenuItemHover(itemObj, true));
        eventTrigger.triggers.Add(enterEntry);

        // 鼠标离开事件
        EventTrigger.Entry exitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        exitEntry.callback.AddListener((data) => OnMenuItemHover(itemObj, false));
        eventTrigger.triggers.Add(exitEntry);
    }

    void OnMenuItemHover(GameObject itemObj, bool isHovered)
    {
        Image background = itemObj.GetComponent<Image>();
        if (background != null)
        {
            background.color = isHovered ? new Color(0.9f, 0.9f, 1f, 1f) : Color.white;
        }

        // 重置自动隐藏计时器
        if (isHovered)
        {
            hideTimer = 0f;
        }
    }

    #endregion

    #region 菜单定位

    void PositionMenu(Vector2 screenPosition)
    {
        if (menuPanel == null || parentCanvas == null) return;

        // 将屏幕坐标转换为Canvas本地坐标
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out canvasPosition);

        // 获取Canvas的Rect
        Rect canvasRect = (parentCanvas.transform as RectTransform).rect;

        // 等待一帧让布局更新，然后获取菜单实际大小
        StartCoroutine(PositionAfterLayout(canvasPosition, canvasRect));
    }

    System.Collections.IEnumerator PositionAfterLayout(Vector2 canvasPosition, Rect canvasRect)
    {
        // 等待布局系统完成计算
        yield return new WaitForEndOfFrame();

        // 强制重建布局
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuPanel);

        // 再等一帧确保布局完全更新
        yield return null;

        // 获取菜单的实际大小
        Vector2 menuSize = menuPanel.rect.size;

        // 如果还是0，使用手动计算的大小
        if (menuSize.x <= 0 || menuSize.y <= 0)
        {
            Debug.LogWarning("菜单大小为0，使用手动计算");
            float calculatedHeight = (currentItems.Count * itemHeight) + (menuPadding.y * 2);
            float calculatedWidth = menuMinWidth;
            menuSize = new Vector2(calculatedWidth, calculatedHeight);
        }

        // 添加偏移量修正位置
        Vector2 offset = new Vector2(menuSize.x * 0.5f, 0f);

        // 应用偏移后的位置：鼠标位置作为菜单左上角
        Vector2 targetPosition = canvasPosition + offset;

        // 边界检测
        if (targetPosition.x + menuSize.x > canvasRect.xMax)
        {
            targetPosition.x = canvasPosition.x - menuSize.x; // 移到鼠标左侧
        }

        if (targetPosition.x < canvasRect.xMin)
        {
            targetPosition.x = canvasRect.xMin; // 确保不超出左边界
        }

        if (targetPosition.y - menuSize.y < canvasRect.yMin)
        {
            targetPosition.y = canvasPosition.y + menuSize.y; // 移到鼠标上方
        }

        // 设置最终位置
        menuPanel.anchoredPosition = targetPosition;
    }

    #endregion

    #region 显示菜单

    void ShowMenu()
    {
        if (menuPanel == null) return;

        menuPanel.gameObject.SetActive(true);
    }

    #endregion

    #region 事件处理

    void OnMenuItemClicked(string itemId)
    {
        Debug.Log($"IconContextMenu: 点击菜单项 {itemId}");

        // 调用回调
        onItemSelected?.Invoke(itemId);

        // 隐藏菜单
        Hide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标离开菜单区域时重置计时器
        hideTimer = 0f;
    }

    bool IsMouseOverMenu()
    {
        if (menuPanel == null || !menuPanel.gameObject.activeInHierarchy)
            return false;

        Vector2 mousePosition = Input.mousePosition;

        // 检查鼠标是否在菜单范围内
        return RectTransformUtility.RectangleContainsScreenPoint(
            menuPanel,
            mousePosition,
            parentCanvas.worldCamera);
    }

    #endregion

    #region 清理

    void ClearMenuItems()
    {
        foreach (GameObject item in instantiatedItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }
        instantiatedItems.Clear();
    }

    void OnDestroy()
    {
        // 销毁时不需要手动清理，Unity会自动处理
    }

    #endregion

    #region 公共工具方法

    /// <summary>
    /// 检查菜单是否可见
    /// </summary>
    public bool IsVisible()
    {
        return isVisible && menuPanel != null && menuPanel.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// 刷新菜单项文本（语言切换时调用）
    /// </summary>
    public void RefreshMenuTexts()
    {
        if (!isVisible || currentItems == null) return;

        for (int i = 0; i < currentItems.Count && i < instantiatedItems.Count; i++)
        {
            GameObject itemObj = instantiatedItems[i];
            if (itemObj == null) continue;

            ContextMenuItem itemData = currentItems[i];

            // 重新设置文本
            TextMeshProUGUI tmpText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            Text uiText = itemObj.GetComponentInChildren<Text>();

            string displayText = itemData.GetDisplayText();

            if (tmpText != null)
            {
                tmpText.text = displayText;
            }
            else if (uiText != null)
            {
                uiText.text = displayText;
            }
        }
    }

    #endregion
}