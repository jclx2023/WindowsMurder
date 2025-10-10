using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

/// <summary>
/// OpenAI Provider - 使用OpenAI Unity Package
/// </summary>
public class OpenAIProvider : MonoBehaviour, ILLMProvider
{
    [Header("OpenAI Settings")]
    [SerializeField] private string modelName = "gpt-4o-mini";

    private OpenAIClient api;

    void Awake()
    {
        // OpenAI package会自动从配置/环境变量读取API Key
        api = new OpenAIClient();
    }

    public string GetProviderName()
    {
        return "OpenAI";
    }

    public IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        // 直接把prompt作为user消息发送（和Gemini一样）
        var messages = new List<Message>
        {
            new Message(Role.User, prompt)
        };

        // 标记任务完成状态
        bool isDone = false;
        string result = null;
        Exception error = null;

        // 异步调用API
        AskAsync(messages,
            reply => {
                result = reply;
                isDone = true;
            },
            ex => {
                error = ex;
                isDone = true;
            });

        // 等待异步任务完成
        yield return new WaitUntil(() => isDone);

        // 返回结果
        if (error != null)
        {
            onError?.Invoke($"OpenAI Error: {error.Message}");
        }
        else
        {
            onSuccess?.Invoke(result);
        }
    }

    /// <summary>
    /// 异步调用OpenAI API
    /// </summary>
    private async void AskAsync(List<Message> messages, Action<string> onSuccess, Action<Exception> onError)
    {
        try
        {
            var req = new ChatRequest(messages, model: new Model(modelName));
            var resp = await api.ChatEndpoint.GetCompletionAsync(req);
            var reply = resp.FirstChoice.Message.ToString();

            onSuccess?.Invoke(reply);
        }
        catch (Exception e)
        {
            onError?.Invoke(e);
        }
    }
}