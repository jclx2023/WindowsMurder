using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// LLM�����л�����UI������
/// </summary>
public class LLMSwitcherUI : MonoBehaviour
{
    [Header("UI���")]
    public TMP_Dropdown providerDropdown;
    public Button confirmButton;

    void Start()
    {
        InitializeDropdown();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    /// <summary>
    /// ��ʼ��������ѡ��
    /// </summary>
    void InitializeDropdown()
    {

        // �������ѡ��
        providerDropdown.ClearOptions();

        // �������LLM����ѡ��
        List<string> options = new List<string>
        {
            "Gemini",
            "GPT-5",
            "DeepSeek"
        };

        providerDropdown.AddOptions(options);

        // ͬ����ǰѡ��
        SyncCurrentSelection();
    }

    /// <summary>
    /// ͬ����ǰѡ�񣨴�GlobalSystemManager��ȡ��
    /// </summary>
    void SyncCurrentSelection()
    {
        if (GlobalSystemManager.Instance == null)
            return;

        LLMProvider currentProvider = GlobalSystemManager.Instance.GetCurrentLLMProvider();

        // ��ö��ֵת��Ϊdropdown����
        int index = (int)currentProvider;
        providerDropdown.value = index;
    }

    /// <summary>
    /// ȷ�ϰ�ť����¼�
    /// </summary>
    void OnConfirmClicked()
    {

        // ��ȡѡ�������
        int selectedIndex = providerDropdown.value;
        LLMProvider selectedProvider = (LLMProvider)selectedIndex;

        // ֪ͨGlobalSystemManager�л�
        GlobalSystemManager.Instance.SetLLMProvider(selectedProvider);

        Debug.Log($"LLM�������л���: {selectedProvider}");
    }

    void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }
}