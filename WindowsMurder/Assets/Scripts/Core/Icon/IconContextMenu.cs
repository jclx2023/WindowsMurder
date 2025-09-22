using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ͼ���Ҽ��˵�UI��� - ֧�ֶ�����
/// ������ʾ�͹���ͼ��������Ĳ˵�
/// </summary>
public class IconContextMenu : MonoBehaviour, IPointerExitHandler
{
    [Header("UI�������")]
    public RectTransform menuPanel;              // �˵����
    public Transform menuItemContainer;          // �˵�������
    public GameObject menuItemPrefab;            // �˵���Ԥ����
    public GameObject separatorPrefab;           // �ָ���Ԥ����

    [Header("��ʽ����")]
    public float itemHeight = 30f;               // �˵���߶�
    public float separatorHeight = 5f;           // �ָ��߸߶�
    public Vector2 menuPadding = new Vector2(5f, 5f);  // �˵��ڱ߾�
    public float menuMinWidth = 120f;            // �˵���С���

    // ˽�б���
    private List<ContextMenuItem> currentItems;
    private List<GameObject> instantiatedItems = new List<GameObject>();
    private Action<string> onItemSelected;
    private Canvas parentCanvas;
    private bool isVisible = false;
    private float hideTimer = 0f;
    private const float AUTO_HIDE_DELAY = 3f; // �Զ������ӳ�

    #region Unity��������

    void Awake()
    {
        // ��ȡCanvas������
        parentCanvas = GetComponentInParent<Canvas>();

        // ��ʼ״̬��Ϊ����
        if (menuPanel != null)
        {
            menuPanel.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // ����Ƿ���Ҫ�Զ�����
        if (isVisible)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer > AUTO_HIDE_DELAY)
            {
                Hide();
            }

            // �������������ط�
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

    #region �����ӿ�

    /// <summary>
    /// ��ʾ�Ҽ��˵�
    /// </summary>
    public void Show(Vector2 screenPosition, List<ContextMenuItem> items, Action<string> callback)
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("IconContextMenu: �˵����б�Ϊ��");
            return;
        }

        // ��������
        currentItems = items;
        onItemSelected = callback;

        // ����֮ǰ�Ĳ˵���
        ClearMenuItems();

        // �����˵���
        CreateMenuItems();

        // ��ʾ�˵�
        ShowMenu();

        // �����˵�λ��
        PositionMenu(screenPosition);

        // ���ü�ʱ��
        hideTimer = 0f;
        isVisible = true;
    }

    /// <summary>
    /// �����Ҽ��˵�
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;

        isVisible = false;

        // ֱ�����������˵�GameObject
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region �˵����

    void CreateMenuItems()
    {
        if (menuItemContainer == null || menuItemPrefab == null)
        {
            Debug.LogError("IconContextMenu: ȱ�ٱ�Ҫ��UI�������");
            return;
        }

        foreach (var item in currentItems)
        {
            // �����˵���
            GameObject itemObj = Instantiate(menuItemPrefab, menuItemContainer);
            instantiatedItems.Add(itemObj);

            // ���ò˵����������֧�֣�
            ConfigureMenuItem(itemObj, item);

            // �����ָ���
            if (item.showSeparator && separatorPrefab != null)
            {
                GameObject separatorObj = Instantiate(separatorPrefab, menuItemContainer);
                instantiatedItems.Add(separatorObj);
            }
        }
    }

    void ConfigureMenuItem(GameObject itemObj, ContextMenuItem itemData)
    {
        // ��ȡ��� - ����ʹ��TextMeshPro
        Button button = itemObj.GetComponent<Button>();
        TextMeshProUGUI tmpText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        Text uiText = null;

        // ���û��TMP��������Ի�ȡ��ͳText���
        if (tmpText == null)
        {
            uiText = itemObj.GetComponentInChildren<Text>();
        }

        // �����ı� - ֧�ֶ����Է���
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
            Debug.LogWarning($"IconContextMenu: �˵��� {itemObj.name} û���ҵ��ı����");
        }

        // ���ð�ť����
        if (button != null)
        {
            button.interactable = itemData.isEnabled;

            if (itemData.isEnabled)
            {
                button.onClick.AddListener(() => OnMenuItemClicked(itemData.itemId));
            }
        }

        // ������ͣЧ��
        SetupHoverEffect(itemObj, itemData.isEnabled);

        // ������־
        if (itemData.useLocalizationKey)
        {
            Debug.Log($"�˵���ػ�: Key={itemData.itemName} �� Text={displayText}");
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

        // �������¼�
        EventTrigger.Entry enterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        enterEntry.callback.AddListener((data) => OnMenuItemHover(itemObj, true));
        eventTrigger.triggers.Add(enterEntry);

        // ����뿪�¼�
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

        // �����Զ����ؼ�ʱ��
        if (isHovered)
        {
            hideTimer = 0f;
        }
    }

    #endregion

    #region �˵���λ

    void PositionMenu(Vector2 screenPosition)
    {
        if (menuPanel == null || parentCanvas == null) return;

        // ����Ļ����ת��ΪCanvas��������
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPosition,
            parentCanvas.worldCamera,
            out canvasPosition);

        // ��ȡCanvas��Rect
        Rect canvasRect = (parentCanvas.transform as RectTransform).rect;

        // �ȴ�һ֡�ò��ָ��£�Ȼ���ȡ�˵�ʵ�ʴ�С
        StartCoroutine(PositionAfterLayout(canvasPosition, canvasRect));
    }

    System.Collections.IEnumerator PositionAfterLayout(Vector2 canvasPosition, Rect canvasRect)
    {
        // �ȴ�����ϵͳ��ɼ���
        yield return new WaitForEndOfFrame();

        // ǿ���ؽ�����
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(menuPanel);

        // �ٵ�һ֡ȷ��������ȫ����
        yield return null;

        // ��ȡ�˵���ʵ�ʴ�С
        Vector2 menuSize = menuPanel.rect.size;

        // �������0��ʹ���ֶ�����Ĵ�С
        if (menuSize.x <= 0 || menuSize.y <= 0)
        {
            Debug.LogWarning("�˵���СΪ0��ʹ���ֶ�����");
            float calculatedHeight = (currentItems.Count * itemHeight) + (menuPadding.y * 2);
            float calculatedWidth = menuMinWidth;
            menuSize = new Vector2(calculatedWidth, calculatedHeight);
        }

        // ���ƫ��������λ��
        Vector2 offset = new Vector2(menuSize.x * 0.5f, 0f);

        // Ӧ��ƫ�ƺ��λ�ã����λ����Ϊ�˵����Ͻ�
        Vector2 targetPosition = canvasPosition + offset;

        // �߽���
        if (targetPosition.x + menuSize.x > canvasRect.xMax)
        {
            targetPosition.x = canvasPosition.x - menuSize.x; // �Ƶ�������
        }

        if (targetPosition.x < canvasRect.xMin)
        {
            targetPosition.x = canvasRect.xMin; // ȷ����������߽�
        }

        if (targetPosition.y - menuSize.y < canvasRect.yMin)
        {
            targetPosition.y = canvasPosition.y + menuSize.y; // �Ƶ�����Ϸ�
        }

        // ��������λ��
        menuPanel.anchoredPosition = targetPosition;
    }

    #endregion

    #region ��ʾ�˵�

    void ShowMenu()
    {
        if (menuPanel == null) return;

        menuPanel.gameObject.SetActive(true);
    }

    #endregion

    #region �¼�����

    void OnMenuItemClicked(string itemId)
    {
        Debug.Log($"IconContextMenu: ����˵��� {itemId}");

        // ���ûص�
        onItemSelected?.Invoke(itemId);

        // ���ز˵�
        Hide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ����뿪�˵�����ʱ���ü�ʱ��
        hideTimer = 0f;
    }

    bool IsMouseOverMenu()
    {
        if (menuPanel == null || !menuPanel.gameObject.activeInHierarchy)
            return false;

        Vector2 mousePosition = Input.mousePosition;

        // �������Ƿ��ڲ˵���Χ��
        return RectTransformUtility.RectangleContainsScreenPoint(
            menuPanel,
            mousePosition,
            parentCanvas.worldCamera);
    }

    #endregion

    #region ����

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
        // ����ʱ����Ҫ�ֶ�����Unity���Զ�����
    }

    #endregion

    #region �������߷���

    /// <summary>
    /// ���˵��Ƿ�ɼ�
    /// </summary>
    public bool IsVisible()
    {
        return isVisible && menuPanel != null && menuPanel.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// ˢ�²˵����ı��������л�ʱ���ã�
    /// </summary>
    public void RefreshMenuTexts()
    {
        if (!isVisible || currentItems == null) return;

        for (int i = 0; i < currentItems.Count && i < instantiatedItems.Count; i++)
        {
            GameObject itemObj = instantiatedItems[i];
            if (itemObj == null) continue;

            ContextMenuItem itemData = currentItems[i];

            // ���������ı�
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