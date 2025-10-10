using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ==================== DeepSeek ����ģ�� ====================

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
/// DeepSeek API Provider - ʹ��UnityWebRequest
/// </summary>
public class DeepSeekProvider : MonoBehaviour, ILLMProvider
{
    [Header("DeepSeek Settings")]
    [SerializeField] private string apiKey = ""; // ��Inspector������
    [SerializeField] private string model = "deepseek-chat";
    [SerializeField] private string endpoint = "https://api.deepseek.com/chat/completions";

    public string GetProviderName()
    {
        return "DeepSeek";
    }

    public IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        // ���API Key
        if (string.IsNullOrEmpty(apiKey))
        {
            onError?.Invoke("DeepSeek API Keyδ���ã�����Inspector������");
            yield break;
        }

        // ���������� - ֱ�Ӱ�prompt��Ϊuser��Ϣ
        var reqObj = new DeepSeekRequest
        {
            model = model,
            messages = new DeepSeekMessage[]
            {
                new DeepSeekMessage
                {
                    role = "user",
                    content = prompt
                }
            },
            stream = false
        };

        string json = JsonUtility.ToJson(reqObj);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(endpoint, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    DeepSeekResponse resp = JsonUtility.FromJson<DeepSeekResponse>(req.downloadHandler.text);
                    if (resp != null && resp.choices != null && resp.choices.Length > 0)
                    {
                        string reply = resp.choices[0].message.content;
                        onSuccess?.Invoke(reply);
                    }
                    else
                    {
                        onError?.Invoke("��Ӧ����ʧ��: " + req.downloadHandler.text);
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke("JSON �����쳣: " + e.Message);
                }
            }
            else
            {
                onError?.Invoke($"HTTP {req.responseCode}: {req.error}\n{req.downloadHandler.text}");
            }
        }
    }
}