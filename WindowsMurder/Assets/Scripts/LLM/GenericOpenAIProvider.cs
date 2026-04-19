using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 通用 OpenAI 兼容 API Provider
/// 适用于：GPT (OpenAI)、DeepSeek、302.ai 中转站、任意自定义 OpenAI 兼容接口
///
/// Inspector 中配置个人默认值（API Key / 模型 / 地址）；
/// 玩家可通过 LLMSwitcherUI 填写自定义值，通过 Configure() 注入，优先于 Inspector 默认值。
/// 留空运行时覆盖 = 使用 Inspector 默认值。
/// </summary>
public class GenericOpenAIProvider : MonoBehaviour, ILLMProvider
{
    [Header("默认设置（Inspector 配置 / 个人 API）")]
    [Tooltip("默认 API Key，玩家未填写时使用此项")]
    [SerializeField] private string defaultApiKey = "";

    [Tooltip("默认模型名，玩家未指定时使用此项")]
    [SerializeField] private string defaultModel = "gpt-5.4-mini";

    [Tooltip("默认接口地址，玩家未填写时使用此项")]
    [SerializeField] private string defaultEndpoint = "https://api.openai.com/v1/chat/completions";

    // ---- 运行时覆盖值（由 Configure 注入） ----
    private string runtimeApiKey   = "";
    private string runtimeModel    = "";
    private string runtimeEndpoint = "";

    // ---- 实际生效值（运行时 > Inspector 默认） ----
    private string ActiveApiKey   => !string.IsNullOrEmpty(runtimeApiKey)   ? runtimeApiKey   : defaultApiKey;
    private string ActiveModel    => !string.IsNullOrEmpty(runtimeModel)    ? runtimeModel    : defaultModel;
    private string ActiveEndpoint => !string.IsNullOrEmpty(runtimeEndpoint) ? runtimeEndpoint : defaultEndpoint;

    // ==================== 配置方法 ====================

    /// <summary>
    /// 根据 LLMRuntimeConfig 和目标预设进行配置。
    /// 未填写的字段会自动回退到预设默认值（不是 Inspector 默认值）。
    /// </summary>
    public void Configure(LLMRuntimeConfig config, LLMProvider preset)
    {
        if (config == null)
        {
            // 切换到没有自定义配置的预设，使用预设的地址/模型，Key 用 Inspector 默认
            runtimeApiKey   = "";
            runtimeModel    = LLMPresetDefaults.GetDefaultModel(preset);
            runtimeEndpoint = LLMPresetDefaults.GetDefaultEndpoint(preset);
        }
        else
        {
            runtimeApiKey   = config.customApiKey;
            runtimeModel    = config.HasCustomModel    ? config.customModel    : LLMPresetDefaults.GetDefaultModel(preset);
            runtimeEndpoint = config.HasCustomEndpoint ? config.customEndpoint : LLMPresetDefaults.GetDefaultEndpoint(preset);
        }

        Debug.Log($"[GenericOpenAI] 已配置 | " +
                  $"Key={( string.IsNullOrEmpty(runtimeApiKey) ? "Inspector默认" : "自定义" )} | " +
                  $"Model={ActiveModel} | Endpoint={ActiveEndpoint}");
    }

    /// <summary>
    /// 快捷配置（直接传字符串，空字符串表示使用 Inspector 默认值）
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
            onError?.Invoke("[LLM] API Key 未配置。请在 LLM 设置中填写您的 API Key，或联系开发者设置默认 Key。");
            yield break;
        }

        if (string.IsNullOrEmpty(ActiveEndpoint))
        {
            onError?.Invoke("[LLM] 接口地址（Endpoint）未配置。请填写完整的 API URL。");
            yield break;
        }

        // --- 构造请求（复用 DeepSeek 的数据模型，格式完全相同）---
        var reqObj = new DeepSeekRequest
        {
            model  = ActiveModel,
            messages = new[]
            {
                new DeepSeekMessage { role = "user", content = prompt }
            },
            stream = false
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

    private static string ShortenUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "(未配置)";
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url.Length > 30 ? url.Substring(0, 30) + "…" : url;
        }
    }
}
