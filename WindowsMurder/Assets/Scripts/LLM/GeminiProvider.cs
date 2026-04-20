using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

// ==================== Gemini 数据模型 ====================

/// <summary>
/// 带 system_instruction 的完整请求体（现代 Gemini 格式）
/// </summary>
[Serializable]
public class GeminiRequest
{
    public GeminiSystemInstruction system_instruction;
    public GeminiContent[] contents;
}

/// <summary>system_instruction 节点</summary>
[Serializable]
public class GeminiSystemInstruction
{
    public GeminiPart[] parts;
}

[Serializable]
public class GeminiContent
{
    public string role;
    public GeminiPart[] parts;
}

[Serializable]
public class GeminiPart
{
    public string text;
}

[Serializable]
public class GeminiResponse
{
    public GeminiCandidate[] candidates;
}

[Serializable]
public class GeminiCandidate
{
    public GeminiContentResponse content;
}

[Serializable]
public class GeminiContentResponse
{
    public GeminiPart[] parts;
}

// ==================== Gemini Provider ====================

/// <summary>
/// Gemini API Provider - 实现ILLMProvider接口
/// </summary>
public class GeminiProvider : MonoBehaviour, ILLMProvider
{
    [Header("Gemini Settings")]
    [Tooltip("默认 API Key，玩家未填写时使用此项")]
    [FormerlySerializedAs("apiKey")]   // 兼容旧版字段名，防止 Inspector 已填入的值丢失
    [SerializeField] private string defaultApiKey = "";
    [Tooltip("默认模型名")]
    [SerializeField] private string defaultModel = "gemini-2.0-flash-exp";

    // 运行时覆盖值（由 Configure 注入，优先于 Inspector 默认值）
    private string runtimeApiKey = "";
    private string runtimeModel  = "";

    private string ActiveApiKey => !string.IsNullOrEmpty(runtimeApiKey) ? runtimeApiKey : defaultApiKey;
    private string ActiveModel  => !string.IsNullOrEmpty(runtimeModel)  ? runtimeModel  : defaultModel;

    private string endpoint =>
        $"https://generativelanguage.googleapis.com/v1beta/models/{ActiveModel}:generateContent?key={ActiveApiKey}";

    /// <summary>
    /// 应用运行时配置（供 DialogueManager 在切换供应商时调用）
    /// </summary>
    public void Configure(LLMRuntimeConfig config)
    {
        runtimeApiKey = config?.HasCustomApiKey  == true ? config.customApiKey : "";
        runtimeModel  = config?.HasCustomModel   == true ? config.customModel
                            : LLMPresetDefaults.GetDefaultModel(LLMProvider.Gemini);
        Debug.Log($"[GeminiProvider] 已配置 | " +
                  $"Key={( string.IsNullOrEmpty(runtimeApiKey) ? "Inspector默认" : "自定义" )} | " +
                  $"Model={ActiveModel}");
    }

    /// <summary>清除运行时覆盖</summary>
    public void ClearRuntime() { runtimeApiKey = runtimeModel = ""; }

    // 实现接口方法
    public string GetProviderName()
    {
        return $"Gemini ({ActiveModel})";
    }

    // 实现接口方法
    public IEnumerator GenerateText(
        string           systemPrompt,
        List<LLMMessage> messages,
        Action<string>   onSuccess,
        Action<string>   onError)
    {
        if (string.IsNullOrEmpty(ActiveApiKey))
        {
            onError?.Invoke("[LLM] Gemini API Key 未配置。请在 LLM 设置中填写您的 API Key。");
            yield break;
        }

        // 将 LLMMessage 列表转换为 Gemini GeminiContent 数组
        // Gemini 使用 "model" 表示 AI 回复，而非 "assistant"
        var contents = new GeminiContent[messages.Count];
        for (int i = 0; i < messages.Count; i++)
        {
            contents[i] = new GeminiContent
            {
                role  = messages[i].role == "assistant" ? "model" : "user",
                parts = new GeminiPart[] { new GeminiPart { text = messages[i].content } }
            };
        }

        // 构造请求体：system_instruction 独立于 contents，充分利用 Gemini 的系统指令机制
        var reqObj = new GeminiRequest
        {
            system_instruction = new GeminiSystemInstruction
            {
                parts = new GeminiPart[] { new GeminiPart { text = systemPrompt } }
            },
            contents = contents
        };

        string json   = JsonUtility.ToJson(reqObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    GeminiResponse resp = JsonUtility.FromJson<GeminiResponse>(req.downloadHandler.text);
                    if (resp != null && resp.candidates != null && resp.candidates.Length > 0)
                    {
                        string reply = resp.candidates[0].content.parts[0].text;
                        onSuccess?.Invoke(reply);
                    }
                    else
                    {
                        onError?.Invoke("响应解析失败: " + req.downloadHandler.text);
                    }
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
}
