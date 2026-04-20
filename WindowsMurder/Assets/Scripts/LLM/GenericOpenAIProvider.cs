using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 通用 OpenAI 兼容 API Provider
/// 适用于：GPT (OpenAI)、DeepSeek、302.ai 中转站、任意自定义 OpenAI 兼容接口
///
/// 【默认 Key 设计】
///   每个供应商在 Inspector 中各自配置一个 defaultApiKey，
///   这样 GPT、DeepSeek 等可以使用完全不同的个人 Key 作为默认值。
///   玩家通过 LLMSwitcherUI 填写的自定义 Key 优先级更高，会覆盖 Inspector 默认值。
///   自定义 Key 留空 = 回退到该供应商对应的 Inspector 默认 Key。
/// </summary>
public class GenericOpenAIProvider : MonoBehaviour, ILLMProvider
{
    // ==================== 各供应商默认 API Key（Inspector 配置） ====================

    [Header("各供应商默认 API Key（开发者在此填写个人 Key）")]

    [Tooltip("OpenAI / GPT 默认 Key")]
    [SerializeField] private string defaultApiKey_GPT = "";

    [Tooltip("DeepSeek 默认 Key")]
    [SerializeField] private string defaultApiKey_DeepSeek = "";

    [Tooltip("302.ai 中转站默认 Key")]
    [SerializeField] private string defaultApiKey_302ai = "";

    [Tooltip("自定义供应商默认 Key（Custom 模式的兜底值）")]
    [SerializeField] private string defaultApiKey_Custom = "";

    // ==================== 通用默认模型 / 地址（Inspector 配置） ====================

    [Header("通用默认值（一般无需修改，由预设系统覆盖）")]

    [Tooltip("当运行时和预设均无模型名时的最终兜底模型")]
    [SerializeField] private string fallbackModel = "gpt-4o-mini";

    [Tooltip("当运行时和预设均无地址时的最终兜底地址")]
    [SerializeField] private string fallbackEndpoint = "https://api.openai.com/v1/chat/completions";

    // ==================== 运行时覆盖值（由 Configure 注入） ====================

    private string runtimeApiKey   = "";
    private string runtimeModel    = "";
    private string runtimeEndpoint = "";
    private LLMProvider currentPreset = LLMProvider.GPT; // 记录当前激活的供应商，用于取对应 defaultKey

    // ==================== 实际生效值 ====================

    private string ActiveApiKey
    {
        get
        {
            // 1. 玩家自定义 Key（最高优先）
            if (!string.IsNullOrEmpty(runtimeApiKey)) return runtimeApiKey;
            // 2. Inspector 中对应供应商的默认 Key
            string inspectorKey = GetInspectorDefaultKey(currentPreset);
            if (!string.IsNullOrEmpty(inspectorKey)) return inspectorKey;
            // 3. 找不到，返回空（后续会报错提示用户）
            return "";
        }
    }

    private string ActiveModel
        => !string.IsNullOrEmpty(runtimeModel) ? runtimeModel : fallbackModel;

    private string ActiveEndpoint
        => !string.IsNullOrEmpty(runtimeEndpoint) ? runtimeEndpoint : fallbackEndpoint;

    // ==================== 配置方法 ====================

    /// <summary>
    /// 根据 LLMRuntimeConfig 和目标预设进行配置。
    /// 未填写的字段自动回退到预设默认值（地址/模型），Key 回退到 Inspector 对应字段。
    /// </summary>
    public void Configure(LLMRuntimeConfig config, LLMProvider preset)
    {
        currentPreset = preset;

        if (config == null)
        {
            runtimeApiKey   = "";   // → 回退到 GetInspectorDefaultKey(preset)
            runtimeModel    = LLMPresetDefaults.GetDefaultModel(preset);
            runtimeEndpoint = LLMPresetDefaults.GetDefaultEndpoint(preset);
        }
        else
        {
            runtimeApiKey   = config.customApiKey;   // 空串 → 回退到 Inspector 默认 Key
            runtimeModel    = config.HasCustomModel    ? config.customModel
                                                       : LLMPresetDefaults.GetDefaultModel(preset);
            runtimeEndpoint = config.HasCustomEndpoint ? config.customEndpoint
                                                       : LLMPresetDefaults.GetDefaultEndpoint(preset);
        }

        string keySource = !string.IsNullOrEmpty(runtimeApiKey) ? "玩家自定义"
                         : !string.IsNullOrEmpty(GetInspectorDefaultKey(preset)) ? $"Inspector默认({preset})"
                         : "⚠️ 未配置";

        Debug.Log($"[GenericOpenAI] 已配置 | Preset={preset} | Key来源={keySource} | " +
                  $"Model={ActiveModel} | Endpoint={ActiveEndpoint}");
    }

    /// <summary>
    /// 快捷配置（直接传字符串，空字符串表示回退到对应 Inspector 默认值）
    /// </summary>
    public void Configure(string apiKey, string model, string endpoint)
    {
        runtimeApiKey   = apiKey   ?? "";
        runtimeModel    = model    ?? "";
        runtimeEndpoint = endpoint ?? "";
    }

    /// <summary>
    /// 清除运行时覆盖，完全使用 Inspector 默认值
    /// </summary>
    public void ClearRuntime()
    {
        runtimeApiKey = runtimeModel = runtimeEndpoint = "";
    }

    // ==================== ILLMProvider ====================

    public string GetProviderName()
    {
        string host = ShortenUrl(ActiveEndpoint);
        return $"OpenAI-Compat ({ActiveModel} @ {host})";
    }

    public IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        // --- 前置检查 ---
        if (string.IsNullOrEmpty(ActiveApiKey))
        {
            string hint = GetMissingKeyHint(currentPreset);
            onError?.Invoke($"[LLM] {currentPreset} 的 API Key 未配置。{hint}");
            yield break;
        }

        if (string.IsNullOrEmpty(ActiveEndpoint))
        {
            onError?.Invoke("[LLM] 接口地址（Endpoint）未配置。请在 LLM 设置中填写完整的 API URL。");
            yield break;
        }

        // --- 构造请求（复用 DeepSeek 数据模型，格式与 OpenAI 相同）---
        var reqObj = new DeepSeekRequest
        {
            model    = ActiveModel,
            messages = new[] { new DeepSeekMessage { role = "user", content = prompt } },
            stream   = false
        };

        byte[] bodyRaw = Encoding.UTF8.GetBytes(JsonUtility.ToJson(reqObj));

        using (var req = new UnityWebRequest(ActiveEndpoint, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {ActiveApiKey}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var resp = JsonUtility.FromJson<DeepSeekResponse>(req.downloadHandler.text);
                    if (resp?.choices != null && resp.choices.Length > 0)
                        onSuccess?.Invoke(resp.choices[0].message.content);
                    else
                        onError?.Invoke("响应解析失败: " + req.downloadHandler.text);
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON 解析异常: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke($"HTTP {req.responseCode}: {req.error}\n{req.downloadHandler.text}");
            }
        }
    }

    // ==================== 工具方法 ====================

    /// <summary>根据供应商类型取 Inspector 中对应的默认 Key</summary>
    private string GetInspectorDefaultKey(LLMProvider preset)
    {
        switch (preset)
        {
            case LLMProvider.GPT:         return defaultApiKey_GPT;
            case LLMProvider.DeepSeek:    return defaultApiKey_DeepSeek;
            case LLMProvider.Relay_302ai: return defaultApiKey_302ai;
            case LLMProvider.Custom:      return defaultApiKey_Custom;
            default:                      return "";
        }
    }

    /// <summary>Key 缺失时给出具体提示，告诉开发者去哪里填</summary>
    private static string GetMissingKeyHint(LLMProvider preset)
    {
        switch (preset)
        {
            case LLMProvider.GPT:
                return "请在 GenericOpenAIProvider 的 Inspector 中填写 defaultApiKey_GPT，或在游戏内 LLM 设置中填写自定义 Key。";
            case LLMProvider.DeepSeek:
                return "请在 GenericOpenAIProvider 的 Inspector 中填写 defaultApiKey_DeepSeek，或在游戏内 LLM 设置中填写自定义 Key。";
            case LLMProvider.Relay_302ai:
                return "请在 GenericOpenAIProvider 的 Inspector 中填写 defaultApiKey_302ai，或在游戏内 LLM 设置中填写自定义 Key。";
            default:
                return "请在游戏内 LLM 设置中填写自定义 Key。";
        }
    }

    private static string ShortenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "(未配置)";
        try   { return new Uri(url).Host; }
        catch { return url.Length > 30 ? url.Substring(0, 30) + "…" : url; }
    }
}
