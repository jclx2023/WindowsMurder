using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// LLM 对话消息单元
/// role: "user" 或 "assistant"
/// </summary>
[Serializable]
public class LLMMessage
{
    public string role;
    public string content;

    public LLMMessage() { }

    public LLMMessage(string role, string content)
    {
        this.role    = role;
        this.content = content;
    }
}

/// <summary>
/// LLM Provider 统一接口
/// 所有 LLM 实现（Gemini、GPT、DeepSeek）都需要实现此接口
/// </summary>
public interface ILLMProvider
{
    IEnumerator GenerateText(
        string           systemPrompt,
        List<LLMMessage> messages,
        Action<string>   onSuccess,
        Action<string>   onError);
    string GetProviderName();
}
