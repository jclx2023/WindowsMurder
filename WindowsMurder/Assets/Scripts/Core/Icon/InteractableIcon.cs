using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// �Ҽ��˵�������
/// </summary>
[Serializable]
public class ContextMenuItem
{
    public string itemName;     // �˵������ƣ���"��"��"����"��"ɾ��"
    public string itemId;       // �˵���ID������ʶ���������ĸ�ѡ��
    public Sprite itemIcon;     // �˵���ͼ�꣨��ѡ��
    public bool isEnabled = true;   // �Ƿ����
    public bool showSeparator;  // �ڴ������ʾ�ָ���
}

/// <summary>
/// ͼ������ö��
/// </summary>
public enum IconType
{
    File,           // �ļ�
    Folder,         // �ļ���
    Program,        // ����
    SystemTool,     // ϵͳ���ߣ������վ��������壩
    Character       // ���˻���ɫ
}

/// <summary>
/// �ɽ�������ͼ��������
/// ֧��˫���������Ҽ��˵�
/// </summary>
public class InteractableIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ͼ���������")]
    public string iconName = "δ����";
    public IconType iconType = IconType.File;
    public Sprite iconSprite;
    public string iconId;           // Ψһ��ʶ��

    [Header("UI�������")]
    public Image iconImage;         // ͼ��ͼƬ���
    public Text nameText;           // �����ı����
    public GameObject selectionHighlight;  // ѡ�и���Ч��

    [Header("��������")]
    public float doubleClickThreshold = 0.5f;  // ˫��ʱ����ֵ
    public bool showTooltip = true;            // �Ƿ���ʾ��ͣ��ʾ

    [Header("�Ҽ��˵�")]
    public List<ContextMenuItem> contextMenuItems = new List<ContextMenuItem>();
    public GameObject contextMenuPrefab;       // �Ҽ��˵�Ԥ��������

    [Header("״̬����")]
    public bool isLocked = false;              // �Ƿ��������޷�������
    public bool isCorrupted = false;           // �Ƿ���״̬
    public bool isHidden = false;              // �Ƿ�����״̬

    // �¼�ί��
    public static event Action<InteractableIcon> OnIconSelected;
    public static event Action<InteractableIcon> OnIconDoubleClicked;
    public static event Action<InteractableIcon, string> OnContextMenuItemClicked;
    public static event Action<InteractableIcon> OnIconHovered;

    // ˽�б���
    private bool isSelected = false;
    private float lastClickTime = 0f;
    private IconContextMenu activeContextMenu;
    private Coroutine tooltipCoroutine;

    // ��̬����
    public static InteractableIcon CurrentSelectedIcon { get; private set; }
    public static List<InteractableIcon> AllIcons { get; private set; } = new List<InteractableIcon>();

    #region Unity��������

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

    #region ��ʼ��

    void Initialize()
    {
        // ����ͼ����ı�
        if (iconImage != null && iconSprite != null)
        {
            iconImage.sprite = iconSprite;
        }

        if (nameText != null)
        {
            nameText.text = iconName;
        }

        // Ӧ��״̬Ч��
        ApplyVisualState();
    }

    #endregion

    #region ��꽻���¼�

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

        // ������ͣ�¼�
        OnIconHovered?.Invoke(this);

        // ��ʾ��ͣЧ��
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(true);
        }

        // ��ʾ������ʾ
        if (showTooltip)
        {
            if (tooltipCoroutine != null)
                StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ������ͣЧ��
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(false);
        }

        // ���ع�����ʾ
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        HideTooltip();
    }

    #endregion

    #region ��������

    void HandleLeftClick()
    {
        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime) < doubleClickThreshold;

        if (isDoubleClick)
        {
            // ˫������
            HandleDoubleClick();
        }
        else
        {
            // �������� - ѡ��ͼ��
            SelectIcon();
        }

        lastClickTime = currentTime;
    }

    void HandleRightClick(PointerEventData eventData)
    {
        // ��ѡ��ͼ��
        SelectIcon();

        // ��ʾ�Ҽ��˵�
        ShowContextMenu(eventData.position);
    }

    void HandleDoubleClick()
    {
        Debug.Log($"˫����ͼ��: {iconName} (����: {iconType})");
        OnIconDoubleClicked?.Invoke(this);
    }

    #endregion

    #region ѡ��״̬����

    public void SelectIcon()
    {
        // ȡ������ͼ���ѡ��״̬
        if (CurrentSelectedIcon != null && CurrentSelectedIcon != this)
        {
            CurrentSelectedIcon.DeselectIcon();
        }

        // ���õ�ǰͼ��Ϊѡ��״̬
        isSelected = true;
        CurrentSelectedIcon = this;

        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(true);
        }

        OnIconSelected?.Invoke(this);
        Debug.Log($"ѡ����ͼ��: {iconName}");
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

    #region �Ҽ��˵�

    void ShowContextMenu(Vector2 screenPosition)
    {
        if (contextMenuItems.Count == 0) return;

        // �����Ѵ��ڵĲ˵�
        HideContextMenu();

        // �����µ��Ҽ��˵�
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
            Debug.LogWarning($"InteractableIcon {iconName}: ȱ��contextMenuPrefab����");
        }
    }

    void HideContextMenu()
    {
        if (activeContextMenu != null)
        {
            activeContextMenu.Hide();
            // ����Hide()������GameObject�����Բ���Ҫ����Ϊnull
            // GameObject���ٺ����û��Զ����null
        }
    }

    void OnContextMenuItemSelected(string itemId)
    {
        Debug.Log($"�Ҽ��˵�ѡ��: {iconName} -> {itemId}");
        OnContextMenuItemClicked?.Invoke(this, itemId);
    }

    #endregion

    #region ������ʾ

    IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSeconds(1f); // ��ͣ1�����ʾ��ʾ

        // ���������ʾ������ʾ
        Debug.Log($"��ʾ��ʾ: {iconName} ({iconType})");
        // ʵ����Ŀ�п��Ե���UI��������ʾ��ʾ��
    }

    void HideTooltip()
    {
        // ���ع�����ʾ
        // ʵ����Ŀ�п��Ե���UI������������ʾ��
    }

    #endregion

    #region ״̬����

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
            iconColor = new Color(1f, 1f, 1f, 0.5f); // ��͸��
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

    #region �����ӿ�

    /// <summary>
    /// ����ͼ����ʾ
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
    /// ����Ҽ��˵���
    /// </summary>
    public void AddContextMenuItem(ContextMenuItem item)
    {
        contextMenuItems.Add(item);
    }

    /// <summary>
    /// �Ƴ��Ҽ��˵���
    /// </summary>
    public void RemoveContextMenuItem(string itemId)
    {
        contextMenuItems.RemoveAll(item => item.itemId == itemId);
    }

    /// <summary>
    /// ����Ҽ��˵�
    /// </summary>
    public void ClearContextMenu()
    {
        contextMenuItems.Clear();
    }

    /// <summary>
    /// ���������Ҽ��˵�
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
    /// ����/���ò˵���
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
    /// ����Ƿ�ѡ��
    /// </summary>
    public bool IsSelected()
    {
        return isSelected;
    }

    #endregion
}