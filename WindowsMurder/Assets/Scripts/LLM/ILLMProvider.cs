using System;
using System.Collections;

/// <summary>
/// LLM Providerͳһ�ӿ�
/// ����LLMʵ�֣�Gemini��GPT��DeepSeek������Ҫʵ������ӿ�
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// �����ı�
    /// </summary>
    IEnumerator GenerateText(string prompt, Action<string> onSuccess, Action<string> onError);

    /// <summary>
    /// ��ȡProvider���ƣ����ڵ��ԣ�
    /// </summary>
    string GetProviderName();
}