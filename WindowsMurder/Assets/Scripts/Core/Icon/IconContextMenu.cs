using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 图标右键菜单UI组件 - 支持多语言
/// </summary>
public class IconContextMenu : MonoBehaviour
{
    [Header("UI组件引用")]
    public RectTransform menuPanel;
    public Transform menuItemContainer;
    public GameObject menuItemPrefab;
    public GameObject separatorPrefab;

    [Header("样式设置")]
    public float itemHeight = 40f;
    public float separatorHeight = 3f;
    public Vector2 menuPadding = new Vector2(5f, 5f);
    public float menuMinWidth = 120f;

    [Header("调试")]
    public bool debugMode = false;  // 默认关闭调试

    private List<ContextMenuItem> currentItems;
    private List<GameObject> instantiatedItems = new List<GameObject>();
    private Action<string> onItemSelected;
    private Canvas parentCanvas;
    private bool isVisible = false;
    private GameObject backgroundBlocker;

    #region 初始化

    void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(false);
        }
    }

    #endregion

    #region 公共接口

    public void Show(Vector2 screenPosition, List<ContextMenuItem> items, Action<string> callback)
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[IconContextMenu] 菜单项列表为空");
            return;
        }

        currentItems = items;
        onItemSelected = callback;

        CreateBackgroundBlocker();
        ClearMenuItems();
        CreateMenuItems();
        ShowMenu();
        PositionMenu(screenPosition);

        isVisible = true;
    }

    public void Hide()
    {
        if (!isVisible) return;

        isVisible = false;

        if (backgroundBlocker != null)
        {
            Destroy(backgroundBlocker);
        }

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 背景遮罩

    void CreateBackgroundBlocker()
    {

        backgroundBlocker = new GameObject("MenuBackgroundBlocker");
        backgroundBlocker.transform.SetParent(parentCanvas.transform, false);
        backgroundBlocker.transform.SetSiblingIndex(transform.GetSiblingIndex());

        RectTransform bgRect = backgroundBlocker.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = backgroundBlocker.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0);
        bgImage.raycastTarget = true;

        Button bgButton = backgroundBlocker.AddComponent<Button>();
        bgButton.transition = Selectable.Transition.None;
        bgButton.onClick.AddListener(Hide);
    }

    #endregion

    #region 菜单项创建

    void CreateMenuItems()
    {

        for (int i = 0; i < currentItems.Count; i++)
        {
            ContextMenuItem item = currentItems[i];

            GameObject itemObj = Instantiate(menuItemPrefab, menuItemContainer);
            itemObj.name = $"MenuItem_{item.itemId}";
            instantiatedItems.Add(itemObj);

            ConfigureMenuItem(itemObj, item);

            if (item.showSeparator && separatorPrefab != null)
            {
                GameObject separatorObj = Instantiate(separatorPrefab, menuItemContainer);
                separatorObj.name = $"Separator_{i}";
                instantiatedItems.Add(separatorObj);
            }
        }
    }

    void ConfigureMenuItem(GameObject itemObj, ContextMenuItem itemData)
    {
        Button button = itemObj.GetComponentInChildren<Button>();
        Image buttonImage = button.GetComponent<Image>();
        TextMeshProUGUI tmpText = itemObj.GetComponentInChildren<TextMeshProUGUI>();

        string displayText = GetDisplayText(itemData);
        if (string.IsNullOrEmpty(displayText))
        {
            displayText = itemData.itemId;
        }

        tmpText.text = displayText;
        tmpText.color = itemData.isEnabled ? Color.black : Color.gray;

        button.interactable = itemData.isEnabled;
        button.onClick.RemoveAllListeners();

        if (itemData.isEnabled)
        {
            string capturedItemId = itemData.itemId;
            button.onClick.AddListener(() => OnMenuItemClicked(capturedItemId));
        }

        SetupHoverEffect(button.gameObject, itemData.isEnabled);
    }

    string GetDisplayText(ContextMenuItem itemData)
    {
        if (LanguageManager.Instance != null)
        {
            string translatedText = LanguageManager.Instance.GetText(itemData.itemName);
            if (!string.IsNullOrEmpty(translatedText))
            {
                return translatedText;
            }
        }
        return itemData.itemName;
    }

    void SetupHoverEffect(GameObject buttonObj, bool isEnabled)
    {
        if (!isEnabled) return;

        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = buttonObj.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            Image bg = buttonObj.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0.85f, 0.9f, 1f);
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => {
            Image bg = buttonObj.GetComponent<Image>();
            if (bg != null) bg.color = Color.white;
        });
        trigger.triggers.Add(exit);
    }

    #endregion

    #region 菜单定位

    void PositionMenu(Vector2 screenPosition)
    {
        if (menuPanel == null || parentCanvas == null)
        {
            Debug.LogError("[IconContextMenu] menuPanel 或 parentCanvas 为空");
            return;
        }

        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out canvasPosition);

        Rect canvasRect = (parentCanvas.transform as RectTransform).rect;
        StartCoroutine(PositionAfterLayout(canvasPosition, canvasRect));
    }

    System.Collections.IEnumerator PositionAfterLayout(Vector2 canvasPosition, Rect canvasRect)
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuPanel);
        yield return null;

        Vector2 menuSize = menuPanel.rect.size;

        if (menuSize.x <= 0 || menuSize.y <= 0)
        {
            float h = 0f;
            foreach (var item in currentItems)
            {
                h += itemHeight;
                if (item.showSeparator) h += separatorHeight;
            }
            h += menuPadding.y * 2;
            menuSize = new Vector2(menuMinWidth, h);
        }

        Vector2 offset = new Vector2(menuSize.x * 0.5f, 0f);
        Vector2 targetPosition = canvasPosition + offset;

        // 边界检测
        if (targetPosition.x + menuSize.x > canvasRect.xMax)
        {
            targetPosition.x = canvasPosition.x - menuSize.x;
        }

        if (targetPosition.x < canvasRect.xMin)
        {
            targetPosition.x = canvasRect.xMin;
        }

        if (targetPosition.y - menuSize.y < canvasRect.yMin)
        {
            targetPosition.y = canvasPosition.y + menuSize.y;
        }

        menuPanel.anchoredPosition = targetPosition;
    }

    #endregion

    #region 事件处理

    void ShowMenu()
    {
        if (menuPanel == null) return;
        menuPanel.gameObject.SetActive(true);
    }

    void OnMenuItemClicked(string itemId)
    {
        if (debugMode)
        {
            Debug.Log($"[IconContextMenu] 菜单项被点击: {itemId}");
        }

        onItemSelected?.Invoke(itemId);
        Hide();
    }

    #endregion

    #region 工具方法

    void ClearMenuItems()
    {
        foreach (GameObject item in instantiatedItems)
        {
            if (item != null) Destroy(item);
        }
        instantiatedItems.Clear();
    }

    #endregion
}
