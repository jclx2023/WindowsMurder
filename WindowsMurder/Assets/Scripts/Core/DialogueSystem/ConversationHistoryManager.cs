using UnityEngine;
using System;

/// <summary>
/// 对话历史管理器 - 专门处理LLM多轮对话的上下文管理
/// </summary>
public class ConversationHistoryManager : MonoBehaviour
{
    [Header("调试")]
    public bool enableDebugLog = true;

    // 私有变量
    private string currentLLMHistory = "";
    private bool isLLMActive = false;
    private string lastPlayerInput = "";

    /// <summary>
    /// 开始新的LLM会话并发送初始prompt
    /// </summary>
    public void StartLLMSession(string initialPrompt, Action<string> onResponse, DialogueManager dialogueManager)
    {
        if (string.IsNullOrEmpty(initialPrompt))
        {
            DebugLog("错误: 初始prompt为空");
            onResponse?.Invoke("系统错误: 初始prompt为空");
            return;
        }

        // 初始化会话状态
        currentLLMHistory = initialPrompt;
        isLLMActive = true;
        lastPlayerInput = "";

        DebugLog($"开始LLM会话，初始prompt: {initialPrompt.Substring(0, Math.Min(50, initialPrompt.Length))}...");

        // 直接发送初始prompt给LLM
        StartCoroutine(SendInitialPrompt(initialPrompt, onResponse, dialogueManager));
    }

    /// <summary>
    /// 发送初始prompt的协程
    /// </summary>
    private System.Collections.IEnumerator SendInitialPrompt(string initialPrompt, Action<string> onResponse, DialogueManager dialogueManager)
    {
        bool responseReceived = false;
        string aiResponse = "";

        // 使用当前Provider（不再hardcoded使用geminiAPI）
        yield return StartCoroutine(dialogueManager.GetCurrentProvider().GenerateText(
            initialPrompt,
            response =>
            {
                aiResponse = dialogueManager.CleanAIResponsePublic(response);
                responseReceived = true;
            },
            error =>
            {
                aiResponse = "系统故障...无法连接...";
                responseReceived = true;
                DebugLog($"初始prompt请求失败: {error}");
            }
        ));

        // 等待响应
        while (!responseReceived)
        {
            yield return null;
        }

        // 将AI回复添加到历史
        AddLLMResponse(aiResponse);

        // 返回结果
        onResponse?.Invoke(aiResponse);

        DebugLog($"初始LLM响应完成: {aiResponse.Substring(0, Math.Min(30, aiResponse.Length))}...");
    }

    /// <summary>
    /// 添加LLM回复到历史记录
    /// </summary>
    public void AddLLMResponse(string aiResponse)
    {
        if (!isLLMActive || string.IsNullOrEmpty(aiResponse))
        {
            DebugLog("警告: LLM未激活或回复为空，跳过添加回复");
            return;
        }

        // 如果有玩家输入，先添加玩家输入再添加AI回复
        if (!string.IsNullOrEmpty(lastPlayerInput))
        {
            currentLLMHistory += $"\n\n玩家: {lastPlayerInput}";
            lastPlayerInput = "";
        }

        currentLLMHistory += $"\nAI: {aiResponse}";

        DebugLog($"添加LLM回复到历史，当前历史长度: {currentLLMHistory.Length}");
    }

    /// <summary>
    /// 构建包含历史的完整prompt
    /// </summary>
    public string BuildPromptWithHistory(string playerInput)
    {
        if (!isLLMActive)
        {
            DebugLog("警告: LLM未激活，返回原始输入");
            return playerInput;
        }

        if (string.IsNullOrEmpty(playerInput))
        {
            DebugLog("警告: 玩家输入为空");
            return currentLLMHistory;
        }

        // 缓存玩家输入
        lastPlayerInput = playerInput;

        // 构建完整prompt
        string fullPrompt = $"{currentLLMHistory}\n\n玩家: {playerInput}\n\nAI:";

        DebugLog($"构建包含历史的prompt，总长度: {fullPrompt.Length}");
        DebugLog($"当前prompt预览: {fullPrompt.Substring(0, Math.Min(100, fullPrompt.Length))}...");

        return fullPrompt;
    }

    /// <summary>
    /// 结束当前LLM会话
    /// </summary>
    public void EndLLMSession()
    {
        if (!isLLMActive)
        {
            DebugLog("LLM会话未激活，无需结束");
            return;
        }

        DebugLog($"结束LLM会话，历史记录长度: {currentLLMHistory.Length}");

        // 清理会话数据
        currentLLMHistory = "";
        lastPlayerInput = "";
        isLLMActive = false;
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ConversationHistoryManager] {message}");
        }
    }
}
