using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ==================== DeepSeek 数据模型 ====================

[Serializable]
public class DeepSeekRequest
{
    public string model;
    public DeepSeekMessage[] messages;
    public bool stream = false;
}

[Serializable]
public class DeepSeekMessage
{
    public string role;
    public string content;
}

[Serializable]
public class DeepSeekResponse
{
    public DeepSeekChoice[] choices;
}

[Serializable]
public class DeepSeekChoice
{
    public DeepSeekMessage message;
}

// ==================== DeepSeek Provider ====================

/// <summary>
/// DeepSeek API Provider - 使用UnityWebRequest
/// </summary>
public class DeepSeekProvider : MonoBehaviour, ILLMProvider
{
    [Header("DeepSeek Settings")]
    [SerializeField] private string apiKey = ""; // 在Inspector中配置
    [SerializeField] private string model = "deepseek-chat";
    [SerializeField] private string endpoint = "https://api.deepseek.com/chat/completions";

    public string GetProviderName()
    {
        return "DeepSeek";
    }

    public IEnumerator GenerateText(
        string           systemPrompt,
        List<LLMMessage> messages,
        Action<string>   onSuccess,
        Action<string>   onError)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            onError?.Invoke("DeepSeek API Key未配置，请在Inspector中设置");
            yield break;
        }

        // 构造消息数组：system 消息 + 对话历史
        var allMessages = new DeepSeekMessage[messages.Count + 1];
        allMessages[0] = new DeepSeekMessage { role = "system", content = systemPrompt };
        for (int i = 0; i < messages.Count; i++)
            allMessages[i + 1] = new DeepSeekMessage { role = messages[i].role, content = messages[i].content };

        var reqObj = new DeepSeekRequest
        {
            model    = model,
            messages = allMessages,
            stream   = false
        };

        string json   = JsonUtility.ToJson(reqObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    DeepSeekResponse resp = JsonUtility.FromJson<DeepSeekResponse>(req.downloadHandler.text);
                    if (resp != null && resp.choices != null && resp.choices.Length > 0)
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
}
