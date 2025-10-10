using System;
using System.Collections;

/// <summary>
/// LLM Provider统一接口
/// 所有LLM实现（Gemini、GPT、DeepSeek）都需要实现这个接口
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// 生成文本
    /// </summary>
    IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError);

    /// <summary>
    /// 获取Provider名称（用于调试）
    /// </summary>
    string GetProviderName();
}