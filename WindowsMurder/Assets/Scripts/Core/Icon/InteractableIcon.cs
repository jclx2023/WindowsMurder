using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Icon������Ϊ����
/// ���о����icon�������̳��������
/// </summary>
public abstract class IconAction : MonoBehaviour
{
    [Header("��������")]
    public string actionName;           // ������Ϊ���ƣ����ڵ��ԣ�
    public bool isEnabled = true;       // �Ƿ����ý���

    /// <summary>
    /// ִ�н�����Ϊ - �������ʵ��
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// ����Ƿ����ִ�н�������ѡ��д��
    /// </summary>
    public virtual bool CanExecute()
    {
        return isEnabled;
    }

    /// <summary>
    /// ����ִ��ǰ�Ļص�����ѡ��д��
    /// </summary>
    protected virtual void OnBeforeExecute()
    {
        // ���������ﲥ��ͨ����Ч����ʾ������
    }

    /// <summary>
    /// ����ִ�к�Ļص�����ѡ��д��
    /// </summary>
    protected virtual void OnAfterExecute()
    {
        // �����������¼��־������״̬��
    }

    /// <summary>
    /// ִ�н�������������
    /// </summary>
    public void TryExecute()
    {
        if (!CanExecute())
        {
            Debug.Log($"IconAction: {actionName} �޷�ִ��");
            return;
        }

        OnBeforeExecute();
        Execute();
        OnAfterExecute();
    }
}

/// <summary>
/// �Ҽ��˵������� - ֧�ֶ�����Keyģʽ
/// </summary>
[Serializable]
public class ContextMenuItem
{
    [Header("��������")]
    public string itemId;           // �˵���ID������ʶ���������ĸ�ѡ��
    public bool isEnabled = true;   // �Ƿ����
    public bool showSeparator;      // �ڴ������ʾ�ָ���

    [Header("����������")]
    public bool useLocalizationKey = true;  // �Ƿ�ʹ�ñ��ػ�Key
    public string itemName;         // ��ʾ���ƣ�Keyģʽʱ�洢���ػ�Key��ֱ��ģʽʱ�洢��ʾ�ı�

    [Header("������Ϣ")]
    [SerializeField] private string previewText; // Inspector����ʾ��Ԥ���ı�������ʱ���ԣ�

    /// <summary>
    /// ���캯�� - ���ػ�Keyģʽ���Ƽ���
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
    /// ���캯�� - ֱ���ı�ģʽ�����ݾɴ��룩
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
    /// Ĭ�Ϲ��캯��������Inspector��
    /// </summary>
    public ContextMenuItem()
    {
        useLocalizationKey = true;
        isEnabled = true;
        showSeparator = false;
    }

    /// <summary>
    /// ��ȡ��ʾ�ı� - ֧������ʱ����
    /// </summary>
    public string GetDisplayText()
    {
        if (!useLocalizationKey)
        {
            return itemName; // ֱ�ӷ����ı�
        }

        // ����ͨ��LanguageManager����
        if (LanguageManager.Instance != null)
        {
            string translatedText = LanguageManager.Instance.GetText(itemName);
            if (!string.IsNullOrEmpty(translatedText))
            {
                return translatedText;
            }
        }

        // ����ʧ��ʱ�Ľ�������
        return GetFallbackText();
    }

    /// <summary>
    /// ��ȡ�����ı�������ʧ��ʱʹ�ã�
    /// </summary>
    private string GetFallbackText()
    {
        // ���Ը���Key����Ĭ�ϵ�Ӣ���ı�
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
            default: return itemName; // ����Key����
        }
    }
}

/// <summary>
/// �ɽ�������ͼ��������
/// ֧��˫���������Ҽ��˵��������˽�����Ϊ����
/// </summary>
public class InteractableIcon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI�������")]
    public Image iconImage;         // ͼ��ͼƬ���
    public TextMeshProUGUI nameText;           // �����ı����
    public GameObject selectionHighlight;  // ѡ�и���Ч��

    [Header("�Ҽ��˵�")]
    public bool canShowContextMenu = true;     // �Ƿ������ʾ�Ҽ��˵�
    public List<ContextMenuItem> contextMenuItems = new List<ContextMenuItem>();
    public GameObject contextMenuPrefab;       // �Ҽ��˵�Ԥ��������

    [Header("״̬����")]
    public bool isLocked = false;              // �Ƿ��������޷�������
    public bool isCorrupted = false;           // �Ƿ���״̬
    public bool isHidden = false;              // �Ƿ�����״̬

    // �������� - Ӳ���볣��
    private const float DOUBLE_CLICK_THRESHOLD = 0.5f;
    private const bool SHOW_TOOLTIP = true;

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
        // Ӧ�ó�ʼ״̬Ч��
        ApplyVisualState();
    }

    void OnEnable()
    {
        if (!AllIcons.Contains(this))
            AllIcons.Add(this);
    }

    void OnDisable()
    {
        // ��������״̬
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

    #region ״̬����

    /// <summary>
    /// ����icon�����н���״̬���ڽ��û�����ʱ���ã�
    /// </summary>
    private void CleanupState()
    {
        // ֹͣ����Э��
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        // ���ظ���Ч��
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }

        // ����ѡ��״̬
        isSelected = false;

        // �����Ҽ��˵�
        HideContextMenu();

        //Debug.Log($"InteractableIcon: {name} ������״̬");
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ������ͣЧ��
        if (selectionHighlight != null && !isSelected)
        {
            selectionHighlight.SetActive(false);
        }
    }

    #endregion

    #region ��������

    void HandleLeftClick()
    {
        float currentTime = Time.time;
        bool isDoubleClick = (currentTime - lastClickTime) < DOUBLE_CLICK_THRESHOLD;

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
        // ����Ƿ�������ʾ�Ҽ��˵�
        if (!canShowContextMenu)
        {
            Debug.Log($"InteractableIcon: {name} ��֧���Ҽ��˵�");
            return;
        }

        // ����Ƿ��в˵���
        if (contextMenuItems.Count == 0)
        {
            Debug.Log($"InteractableIcon: {name} û�������Ҽ��˵���");
            return;
        }

        // ��ѡ��ͼ��
        SelectIcon();

        // ��ʾ�Ҽ��˵�
        ShowContextMenu(eventData.position);
    }

    void HandleDoubleClick()
    {
        // ˫��ʱ������ͣЧ������ֹ��������ͣЧ��������
        CleanupHoverEffect();

        // ִ�н�����Ϊ
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
    /// ����icon��˫������ - ���ɵĽ��������߼�
    /// </summary>
    private void HandleIconInteraction()
    {
        // ����icon�ϵĽ�����Ϊ���
        IconAction iconAction = GetComponent<IconAction>();

        if (iconAction != null)
        {
            Debug.Log($"InteractableIcon: ִ�� {name} �Ľ�����Ϊ");
            iconAction.TryExecute();
        }
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

    #region �Ҽ��˵�

    void ShowContextMenu(Vector2 screenPosition)
    {
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
        // ֻ������̬�¼���������ϵͳ�����Ҽ��˵��߼�
        OnContextMenuItemClicked?.Invoke(this, itemId);
        Debug.Log($"InteractableIcon: {name} ѡ���˲˵��� {itemId}");
    }

    #endregion

    #region ״̬����

    void ApplyVisualState()
    {
        if (iconImage == null) return;

        // �ȱ�����ǰ alpha
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
            iconColor = new Color(1f, 1f, 1f, 0.5f); // ��͸��
        }

        iconImage.color = iconColor;
    }
    #endregion
}