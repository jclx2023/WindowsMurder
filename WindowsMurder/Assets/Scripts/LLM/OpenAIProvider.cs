using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;

/// <summary>
/// OpenAI Provider - ʹ��OpenAI Unity Package
/// </summary>
public class OpenAIProvider : MonoBehaviour, ILLMProvider
{
    [Header("OpenAI Settings")]
    [SerializeField] private string modelName = "gpt-4o-mini";

    private OpenAIClient api;

    void Awake()
    {
        // OpenAI package���Զ�������/����������ȡAPI Key
        api = new OpenAIClient();
    }

    public string GetProviderName()
    {
        return "OpenAI";
    }

    public IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        // ֱ�Ӱ�prompt��Ϊuser��Ϣ���ͣ���Geminiһ����
        var messages = new List<Message>
        {
            new Message(Role.User, prompt)
        };

        // ����������״̬
        bool isDone = false;
        string result = null;
        Exception error = null;

        // �첽����API
        AskAsync(messages,
            reply => {
                result = reply;
                isDone = true;
            },
            ex => {
                error = ex;
                isDone = true;
            });

        // �ȴ��첽�������
        yield return new WaitUntil(() => isDone);

        // ���ؽ��
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
    /// �첽����OpenAI API
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