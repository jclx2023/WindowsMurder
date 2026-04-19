using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LLM 引擎设置窗口 UI 控制器（扩展版）
///
/// UI 层级建议：
///   LLMSwitcherWindow
///     ProviderDropdown          ← TMP_Dropdown
///     ApiKeySection
///       ApiKeyLabel             ← TMP_Text（可选，标签）
///       ApiKeyInput             ← TMP_InputField（留空=使用默认Key）
///     ModelSection
///       ModelLabel              ← TMP_Text（可选）
///       ModelInput              ← TMP_InputField（留空=使用预设默认模型）
///     EndpointSection           ← 仅 302.ai / Custom 时显示
///       EndpointLabel           ← TMP_Text（可选）
///       EndpointInput           ← TMP_InputField
///     HintText                  ← TMP_Text（提示/状态信息）
///     ConfirmButton             ← Button
/// </summary>
public class LLMSwitcherUI : MonoBehaviour
{
    [Header("核心 UI 组件")]
    [Tooltip("供应商选择下拉框")]
    public TMP_Dropdown providerDropdown;

    [Tooltip("确认按钮")]
    public Button confirmButton;

    [Header("API Key 设置")]
    [Tooltip("API Key 输入框（留空=使用 Inspector 中的默认 Key）")]
    public TMP_InputField apiKeyInput;

    [Header("模型设置")]
    [Tooltip("模型名输入框（留空=使用该供应商的预设默认模型）")]
    public TMP_InputField modelInput;

    [Header("接口地址设置（302.ai / Custom 时显示）")]
    [Tooltip("接口地址输入框")]
    public TMP_InputField endpointInput;

    [Tooltip("接口地址区域根节点（整体显隐）")]
    public GameObject endpointSection;

    [Header("提示文本（可选）")]
    [Tooltip("在窗口底部显示当前配置摘要或错误提示")]
    public TMP_Text hintText;

    // ---- 内部状态 ----

    // 下拉框选项顺序（与 LLMProvider 枚举值对应）
    private readonly LLMProvider[] providerOrder =
    {
        LLMProvider.Gemini,
        LLMProvider.GPT,
        LLMProvider.DeepSeek,
        LLMProvider.Relay_302ai,
        LLMProvider.Custom
    };

    // ==================== Unity 生命周期 ====================

    void Start()
    {
        BuildDropdown();
        SyncFromGlobalManager();
        UpdateUIVisibility();
        HookEvents();
    }

    void OnDestroy()
    {
        if (providerDropdown != null)
            providerDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    // ==================== 初始化 ====================

    /// <summary>根据供应商枚举列表构建下拉框选项</summary>
    private void BuildDropdown()
    {
        if (providerDropdown == null) return;

        providerDropdown.ClearOptions();

        var options = new List<string>();
        foreach (var p in providerOrder)
            options.Add(LLMPresetDefaults.GetDisplayName(p));

        providerDropdown.AddOptions(options);
    }

    /// <summary>从 GlobalSystemManager 读取当前设置，同步到 UI</summary>
    private void SyncFromGlobalManager()
    {
        if (GlobalSystemManager.Instance == null) return;

        LLMProvider current = GlobalSystemManager.Instance.GetCurrentLLMProvider();
        LLMRuntimeConfig cfg = GlobalSystemManager.Instance.GetLLMConfig(current);

        // 同步 Dropdown
        int idx = System.Array.IndexOf(providerOrder, current);
        if (idx >= 0 && providerDropdown != null)
            providerDropdown.value = idx;

        // 同步输入框
        if (cfg != null)
        {
            SetInputSafe(apiKeyInput,   cfg.customApiKey);
            SetInputSafe(modelInput,    cfg.customModel);
            SetInputSafe(endpointInput, cfg.customEndpoint);
        }
        else
        {
            ClearAllInputs();
        }

        // 更新 Model 输入框的 Placeholder 提示
        UpdateModelPlaceholder(current);
        UpdateEndpointPlaceholder(current);
        UpdateHint(current, cfg);
    }

    /// <summary>绑定事件</summary>
    private void HookEvents()
    {
        if (providerDropdown != null)
            providerDropdown.onValueChanged.AddListener(OnDropdownChanged);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    // ==================== 事件响应 ====================

    /// <summary>Dropdown 值变化时，刷新 UI 可见性和 Placeholder</summary>
    private void OnDropdownChanged(int index)
    {
        LLMProvider selected = GetSelectedProvider();

        // 加载该供应商已保存的配置（如有）
        LLMRuntimeConfig savedCfg = GlobalSystemManager.Instance?.GetLLMConfig(selected);
        if (savedCfg != null)
        {
            SetInputSafe(apiKeyInput,   savedCfg.customApiKey);
            SetInputSafe(modelInput,    savedCfg.customModel);
            SetInputSafe(endpointInput, savedCfg.customEndpoint);
        }
        else
        {
            // 切换到新供应商时清空输入，让玩家重新填写
            ClearAllInputs();
        }

        UpdateUIVisibility();
        UpdateModelPlaceholder(selected);
        UpdateEndpointPlaceholder(selected);
        UpdateHint(selected, savedCfg);
    }

    /// <summary>点击确认按钮：读取 UI → 构建配置 → 通知 GlobalSystemManager</summary>
    private void OnConfirmClicked()
    {
        LLMProvider selected = GetSelectedProvider();

        string apiKey   = GetInputValue(apiKeyInput);
        string model    = GetInputValue(modelInput);
        string endpoint = GetInputValue(endpointInput);

        // 只有实际填了内容才写入配置，否则置为 null（使用Inspector默认值）
        LLMRuntimeConfig config = null;
        if (!string.IsNullOrEmpty(apiKey) || !string.IsNullOrEmpty(model) || !string.IsNullOrEmpty(endpoint))
        {
            config = new LLMRuntimeConfig(apiKey, model, endpoint);
        }

        // 同时切换供应商 + 保存配置
        GlobalSystemManager.Instance?.SetLLMProviderAndConfig(selected, config);

        UpdateHint(selected, config);

        Debug.Log($"[LLMSwitcherUI] 已确认切换到: {selected} | " +
                  $"Key={(string.IsNullOrEmpty(apiKey) ? "默认" : "自定义")} | " +
                  $"Model={(string.IsNullOrEmpty(model) ? LLMPresetDefaults.GetDefaultModel(selected) : model)} | " +
                  $"Endpoint={(string.IsNullOrEmpty(endpoint) ? LLMPresetDefaults.GetDefaultEndpoint(selected) : endpoint)}");
    }

    // ==================== UI 工具方法 ====================

    /// <summary>根据当前选中的供应商，显示或隐藏 Endpoint 区域</summary>
    private void UpdateUIVisibility()
    {
        LLMProvider selected = GetSelectedProvider();
        bool showEndpoint    = LLMPresetDefaults.ShowEndpointField(selected);

        if (endpointSection != null)
            endpointSection.SetActive(showEndpoint);
        else if (endpointInput != null)
            endpointInput.gameObject.SetActive(showEndpoint);
    }

    /// <summary>更新模型输入框的 Placeholder 为供应商默认模型名</summary>
    private void UpdateModelPlaceholder(LLMProvider provider)
    {
        if (modelInput == null) return;

        string defaultModel = LLMPresetDefaults.GetDefaultModel(provider);
        var placeholder = modelInput.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.text = string.IsNullOrEmpty(defaultModel)
                ? "输入模型名（如 gpt-4o）"
                : $"留空使用默认: {defaultModel}";
        }
    }

    /// <summary>更新接口地址输入框的 Placeholder</summary>
    private void UpdateEndpointPlaceholder(LLMProvider provider)
    {
        if (endpointInput == null) return;

        string defaultEndpoint = LLMPresetDefaults.GetDefaultEndpoint(provider);
        var placeholder = endpointInput.placeholder as TMP_Text;
        if (placeholder != null)
        {
            placeholder.text = string.IsNullOrEmpty(defaultEndpoint)
                ? "输入 API 接口地址（如 https://api.302.ai/v1/chat/completions）"
                : $"留空使用默认: {defaultEndpoint}";
        }
    }

    /// <summary>更新底部状态提示文本</summary>
    private void UpdateHint(LLMProvider provider, LLMRuntimeConfig cfg)
    {
        if (hintText == null) return;

        string providerName = LLMPresetDefaults.GetDisplayName(provider);
        string model        = (cfg?.HasCustomModel    == true) ? cfg.customModel    : LLMPresetDefaults.GetDefaultModel(provider);
        string keyStatus    = (cfg?.HasCustomApiKey   == true) ? "自定义 Key"       : "默认 Key（Inspector）";

        hintText.text = $"当前: {providerName}\n模型: {(string.IsNullOrEmpty(model) ? "(未指定)" : model)}\nKey: {keyStatus}";
    }

    // ---- 小工具 ----

    private LLMProvider GetSelectedProvider()
    {
        if (providerDropdown == null) return LLMProvider.Gemini;
        int idx = providerDropdown.value;
        return (idx >= 0 && idx < providerOrder.Length) ? providerOrder[idx] : LLMProvider.Gemini;
    }

    private static string GetInputValue(TMP_InputField field)
    {
        if (field == null) return "";
        return field.text?.Trim() ?? "";
    }

    private static void SetInputSafe(TMP_InputField field, string value)
    {
        if (field == null) return;
        field.text = value ?? "";
    }

    private void ClearAllInputs()
    {
        SetInputSafe(apiKeyInput,   "");
        SetInputSafe(modelInput,    "");
        SetInputSafe(endpointInput, "");
    }
}
