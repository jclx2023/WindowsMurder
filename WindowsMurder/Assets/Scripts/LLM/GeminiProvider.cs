using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ==================== Gemini 数据模型 ====================

[Serializable]
public class GeminiRequest
{
    public GeminiContent[] contents;
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
    [SerializeField] private string apiKey = "";
    [SerializeField] private string model = "gemini-2.0-flash-exp";

    private string endpoint => $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    // 实现接口方法
    public string GetProviderName()
    {
        return "Gemini";
    }

    // 实现接口方法 - 保持原有逻辑不变
    public IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError)
    {

        // 构造请求体
        var reqObj = new GeminiRequest
        {
            contents = new GeminiContent[]
            {
                new GeminiContent
                {
                    role = "user",
                    parts = new GeminiPart[] { new GeminiPart { text = prompt } }
                }
            }
        };

        string json = JsonUtility.ToJson(reqObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
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
