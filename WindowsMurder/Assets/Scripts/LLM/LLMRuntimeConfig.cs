using System.Collections.Generic;
using UnityEngine;

// ========== LLM 供应商枚举（扩展版） ==========
// 注意：枚举值与旧版保持兼容（Gemini=0, GPT=1, DeepSeek=2），新增 302.ai 和 Custom

/// <summary>
/// LLM 供应商类型枚举
/// </summary>
public enum LLMProvider
{
    Gemini      = 0,
    GPT         = 1,
    DeepSeek    = 2,
    Relay_302ai = 3,
    Custom      = 4
}

// ========== 运行时配置 ==========

/// <summary>
/// 单个 LLM 供应商的运行时配置，可覆盖 Inspector 中的默认值。
/// 字段留空表示使用该供应商的默认值。
/// </summary>
[System.Serializable]
public class LLMRuntimeConfig
{
    /// <summary>自定义 API Key（空 = 使用 Inspector 中的默认 Key）</summary>
    public string customApiKey = "";

    /// <summary>自定义模型名（空 = 使用供应商预设默认模型）</summary>
    public string customModel = "";

    /// <summary>自定义接口地址（空 = 使用供应商预设地址）</summary>
    public string customEndpoint = "";

    public bool HasCustomApiKey   => !string.IsNullOrEmpty(customApiKey);
    public bool HasCustomModel    => !string.IsNullOrEmpty(customModel);
    public bool HasCustomEndpoint => !string.IsNullOrEmpty(customEndpoint);

    public LLMRuntimeConfig() { }

    public LLMRuntimeConfig(string apiKey, string model, string endpoint)
    {
        customApiKey   = apiKey    ?? "";
        customModel    = model     ?? "";
        customEndpoint = endpoint  ?? "";
    }

    /// <summary>序列化为 JSON（用于 PlayerPrefs 持久化）</summary>
    public string ToJson() => JsonUtility.ToJson(this);

    /// <summary>从 JSON 字符串反序列化</summary>
    public static LLMRuntimeConfig FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return new LLMRuntimeConfig();
        try   { return JsonUtility.FromJson<LLMRuntimeConfig>(json); }
        catch { return new LLMRuntimeConfig(); }
    }
}

// ========== 供应商预设信息 ==========

/// <summary>
/// 各供应商的预设端点、模型等静态信息
/// </summary>
public static class LLMPresetDefaults
{
    public struct PresetInfo
    {
        public string displayName;
        public string defaultEndpoint;
        public string defaultModel;
        /// <summary>是否为 OpenAI 兼容 API（使用 /v1/chat/completions 格式）</summary>
        public bool isOpenAICompatible;
        /// <summary>UI 中是否显示"接口地址"输入框</summary>
        public bool showEndpointField;
    }

    public static readonly Dictionary<LLMProvider, PresetInfo> Presets =
        new Dictionary<LLMProvider, PresetInfo>
    {
        {
            LLMProvider.Gemini, new PresetInfo
            {
                displayName        = "Gemini (Google)",
                defaultEndpoint    = "",   // Gemini 使用特殊 URL 结构，由 GeminiProvider 自行处理
                defaultModel       = "gemini-2.0-flash-exp",
                isOpenAICompatible = false,
                showEndpointField  = false
            }
        },
        {
            LLMProvider.GPT, new PresetInfo
            {
                displayName        = "GPT (OpenAI)",
                defaultEndpoint    = "https://api.openai.com/v1/chat/completions",
                defaultModel       = "gpt-4o-mini",
                isOpenAICompatible = true,
                showEndpointField  = false
            }
        },
        {
            LLMProvider.DeepSeek, new PresetInfo
            {
                displayName        = "DeepSeek",
                defaultEndpoint    = "https://api.deepseek.com/chat/completions",
                defaultModel       = "deepseek-chat",
                isOpenAICompatible = true,
                showEndpointField  = false
            }
        },
        {
            LLMProvider.Relay_302ai, new PresetInfo
            {
                displayName        = "302.ai 中转",
                defaultEndpoint    = "https://api.302.ai/v1/chat/completions",
                defaultModel       = "gpt-4o-mini",
                isOpenAICompatible = true,
                showEndpointField  = true
            }
        },
        {
            LLMProvider.Custom, new PresetInfo
            {
                displayName        = "自定义 / Custom",
                defaultEndpoint    = "",
                defaultModel       = "",
                isOpenAICompatible = true,
                showEndpointField  = true
            }
        },
    };

    public static PresetInfo Get(LLMProvider provider)
    {
        return Presets.TryGetValue(provider, out var info) ? info : default;
    }

    public static string GetDisplayName(LLMProvider provider)     => Get(provider).displayName;
    public static string GetDefaultEndpoint(LLMProvider provider) => Get(provider).defaultEndpoint;
    public static string GetDefaultModel(LLMProvider provider)    => Get(provider).defaultModel;
    public static bool   IsOpenAICompatible(LLMProvider provider) => Get(provider).isOpenAICompatible;
    public static bool   ShowEndpointField(LLMProvider provider)  => Get(provider).showEndpointField;

    /// <summary>
    /// PlayerPrefs 中存储某供应商配置的 Key
    /// </summary>
    public static string GetPrefsKey(LLMProvider provider) => $"LLMConfig_{(int)provider}";
}
