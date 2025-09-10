using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class GeminiRequest
{
    public Content[] contents;
}

[Serializable]
public class Content
{
    public string role;
    public Part[] parts;
}

[Serializable]
public class Part
{
    public string text;
}

[Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[Serializable]
public class Candidate
{
    public ContentResponse content;
}

[Serializable]
public class ContentResponse
{
    public Part[] parts;
}

public class GeminiAPI : MonoBehaviour
{
    [Header("Gemini Settings")]
    [SerializeField] private string apiKey = "AIzaSyDxxxx..."; // 你的 Gemini API Key
    [SerializeField] private string model = "gemini-2.5-flash";

    private string endpoint => $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

    public IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        // 构造请求体
        var reqObj = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content
                {
                    role = "user",
                    parts = new Part[]{ new Part{ text = prompt } }
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
