using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 单轮对话记录
/// </summary>
[Serializable]
public class ConversationTurn
{
    public string playerInput; // null 表示初始 AI 问候（无玩家输入）
    public string aiReply;
    public bool disclosed;     // 该轮 AI 是否已给出必要信息
}

/// <summary>
/// 对话历史管理器 - 管理 LLM 多轮对话上下文
/// 新版：结构化 Turn 列表 + 动态 State Block 注入，替代原始字符串拼接
/// </summary>
public class ConversationHistoryManager : MonoBehaviour
{
    [Header("调试")]
    public bool enableDebugLog = true;

    // 最多保留的历史轮数（控制 context window）
    private const int MAX_HISTORY_TURNS = 6;

    // 私有状态
    private string initialSystemPrompt = "";   // 角色 Prompt（固定，每轮重用）
    private List<ConversationTurn> turns;      // 历史对话轮次
    private bool cumulativeDisclosed = false;  // 是否已完成所有必要信息披露
    private bool isLLMActive = false;
    private string lastPlayerInput = "";

    // ==================== 公开接口 ====================

    /// <summary>
    /// 开始新的 LLM 会话并发送初始 Prompt（角色开场白）
    /// </summary>
    public void StartLLMSession(string initialPrompt, Action<string> onResponse, DialogueManager dialogueManager)
    {
        if (string.IsNullOrEmpty(initialPrompt))
        {
            DebugLog("错误: 初始 prompt 为空");
            onResponse?.Invoke("系统错误: 初始 prompt 为空");
            return;
        }

        initialSystemPrompt = initialPrompt;
        turns = new List<ConversationTurn>();
        isLLMActive = true;
        cumulativeDisclosed = false;
        lastPlayerInput = "";

        DebugLog($"开始 LLM 会话，初始 prompt 长度: {initialPrompt.Length}");

        StartCoroutine(SendInitialPrompt(initialPrompt, onResponse, dialogueManager));
    }

    /// <summary>
    /// 构建包含历史和状态块的完整 Prompt（玩家输入后调用）
    /// </summary>
    public string BuildPromptWithHistory(string playerInput)
    {
        if (!isLLMActive)
        {
            DebugLog("警告: LLM 未激活，返回原始输入");
            return playerInput;
        }

        if (string.IsNullOrEmpty(playerInput))
        {
            DebugLog("警告: 玩家输入为空");
            return initialSystemPrompt;
        }

        lastPlayerInput = playerInput;

        // 1. 注入 State Block
        string stateBlock = BuildStateBlock();

        // 2. 构建压缩后的对话历史
        string history = BuildCompressedHistory();

        // 3. 拼接完整 Prompt
        StringBuilder sb = new StringBuilder();
        sb.Append(initialSystemPrompt);
        sb.Append("\n\n");
        sb.Append(stateBlock);

        if (!string.IsNullOrEmpty(history))
        {
            sb.Append("\n\n");
            sb.Append(history);
        }

        sb.Append("\n\n玩家: ");
        sb.Append(playerInput);
        sb.Append("\n\nAI:");

        string fullPrompt = sb.ToString();
        DebugLog($"构建 Prompt 完成，总长度: {fullPrompt.Length}，历史轮数: {turns.Count}");
        return fullPrompt;
    }

    /// <summary>
    /// 记录一轮 AI 回复（由 DialogueManager 在解析 JSON 后调用）
    /// </summary>
    public void AddLLMResponse(string aiReply, bool disclosed = false)
    {
        if (!isLLMActive || string.IsNullOrEmpty(aiReply))
        {
            DebugLog("警告: LLM 未激活或回复为空，跳过记录");
            return;
        }

        turns.Add(new ConversationTurn
        {
            playerInput = string.IsNullOrEmpty(lastPlayerInput) ? null : lastPlayerInput,
            aiReply     = aiReply,
            disclosed   = disclosed
        });

        lastPlayerInput = "";

        if (disclosed) cumulativeDisclosed = true;

        DebugLog($"记录第 {turns.Count} 轮对话，disclosed: {disclosed}，累计披露: {cumulativeDisclosed}");
    }

    /// <summary>
    /// 更新信息披露状态（可由外部直接调用）
    /// </summary>
    public void UpdateDisclosureState(bool disclosed)
    {
        if (disclosed && !cumulativeDisclosed)
        {
            cumulativeDisclosed = true;
            DebugLog("必要信息披露状态已更新为 true");
        }
    }

    /// <summary>
    /// 结束当前 LLM 会话，清理状态
    /// </summary>
    public void EndLLMSession()
    {
        if (!isLLMActive)
        {
            DebugLog("LLM 会话未激活，无需结束");
            return;
        }

        DebugLog($"结束 LLM 会话，共 {turns?.Count ?? 0} 轮对话，最终披露: {cumulativeDisclosed}");

        initialSystemPrompt = "";
        turns               = null;
        lastPlayerInput     = "";
        cumulativeDisclosed = false;
        isLLMActive         = false;
    }

    // ==================== 私有方法 ====================

    /// <summary>
    /// 发送初始 Prompt，获取 AI 开场白
    /// </summary>
    private System.Collections.IEnumerator SendInitialPrompt(
        string initialPrompt,
        Action<string> onResponse,
        DialogueManager dialogueManager)
    {
        bool responseReceived = false;
        string rawResponse = "";

        yield return StartCoroutine(dialogueManager.GetCurrentProvider().GenerateText(
            initialPrompt,
            response =>
            {
                rawResponse = response;
                responseReceived = true;
            },
            error =>
            {
                rawResponse = "";
                responseReceived = true;
                DebugLog($"初始 Prompt 请求失败: {error}");
            }
        ));

        while (!responseReceived) yield return null;

        // 解析 JSON（LLMJsonResponse 是顶层类，直接使用，无需 DialogueManager. 前缀）
        LLMJsonResponse parsed = dialogueManager.TryParseJsonResponse(rawResponse);

        // 更新披露状态
        if (parsed.disclosed) cumulativeDisclosed = true;

        // 触发 Suggestions（通过 DialogueManager 的静态代理方法，规避 C# 外部 Invoke 限制）
        DialogueManager.FireSuggestionsReady(parsed.suggestions);

        // 记录初始轮（无玩家输入）
        turns.Add(new ConversationTurn
        {
            playerInput = null,
            aiReply     = parsed.reply,
            disclosed   = parsed.disclosed
        });

        // 构建发往 UI 的回复（需要结束时附加 end 标记，DialogueUI 会检测并清理）
        string replyForUI = parsed.end
            ? parsed.reply.TrimEnd() + "\nend"
            : parsed.reply;

        DebugLog($"初始 LLM 响应完成，reply 长度: {parsed.reply.Length}，end: {parsed.end}");
        onResponse?.Invoke(replyForUI);
    }

    /// <summary>
    /// 构建 State Block（动态注入当前对话状态）
    /// </summary>
    private string BuildStateBlock()
    {
        int turnNumber = (turns?.Count ?? 0) + 1;
        string disclosedStr = cumulativeDisclosed ? "true" : "false";
        return $"[STATE]\nTURN: {turnNumber}\nDISCLOSED: {disclosedStr}";
    }

    /// <summary>
    /// 构建压缩历史（最近 MAX_HISTORY_TURNS 轮）
    /// </summary>
    private string BuildCompressedHistory()
    {
        if (turns == null || turns.Count == 0) return "";

        int startIndex = Mathf.Max(0, turns.Count - MAX_HISTORY_TURNS);
        StringBuilder sb = new StringBuilder();

        for (int i = startIndex; i < turns.Count; i++)
        {
            ConversationTurn turn = turns[i];

            // 初始开场白没有玩家输入，只输出 AI 行
            if (!string.IsNullOrEmpty(turn.playerInput))
            {
                sb.Append("玩家: ");
                sb.AppendLine(turn.playerInput);
            }

            sb.Append("AI: ");
            sb.Append(turn.aiReply);

            if (i < turns.Count - 1) sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[ConversationHistoryManager] {message}");
    }
}
