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
    /// <summary>
    /// 以结构化消息格式生成文本。
    /// 将系统指令与对话历史分离，充分利用现代对话模型的 system / user / assistant 角色机制。
    /// </summary>
    /// <param name="systemPrompt">角色卡 + 系统规则（固定，每轮不变）</param>
    /// <param name="messages">对话历史 + 当前输入，role 为 "user" 或 "assistant"</param>
    IEnumerator GenerateText(
        string           systemPrompt,
        List<LLMMessage> messages,
        Action<string>   onSuccess,
        Action<string>   onError);

    /// <summary>获取 Provider 名称（用于调试）</summary>
    string GetProviderName();
}
