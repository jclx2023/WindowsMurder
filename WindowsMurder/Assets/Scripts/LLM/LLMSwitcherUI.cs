using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// LLM引擎切换窗口UI控制器
/// </summary>
public class LLMSwitcherUI : MonoBehaviour
{
    [Header("UI组件")]
    public TMP_Dropdown providerDropdown;
    public Button confirmButton;

    void Start()
    {
        InitializeDropdown();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    /// <summary>
    /// 初始化下拉框选项
    /// </summary>
    void InitializeDropdown()
    {

        // 清空现有选项
        providerDropdown.ClearOptions();

        // 添加所有LLM引擎选项
        List<string> options = new List<string>
        {
            "Gemini",
            "GPT-5",
            "DeepSeek"
        };

        providerDropdown.AddOptions(options);

        // 同步当前选择
        SyncCurrentSelection();
    }

    /// <summary>
    /// 同步当前选择（从GlobalSystemManager读取）
    /// </summary>
    void SyncCurrentSelection()
    {
        if (GlobalSystemManager.Instance == null)
            return;

        LLMProvider currentProvider = GlobalSystemManager.Instance.GetCurrentLLMProvider();

        // 将枚举值转换为dropdown索引
        int index = (int)currentProvider;
        providerDropdown.value = index;
    }

    /// <summary>
    /// 确认按钮点击事件
    /// </summary>
    void OnConfirmClicked()
    {

        // 获取选择的引擎
        int selectedIndex = providerDropdown.value;
        LLMProvider selectedProvider = (LLMProvider)selectedIndex;

        // 通知GlobalSystemManager切换
        GlobalSystemManager.Instance.SetLLMProvider(selectedProvider);

        Debug.Log($"LLM引擎已切换到: {selectedProvider}");
    }

    void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }
}