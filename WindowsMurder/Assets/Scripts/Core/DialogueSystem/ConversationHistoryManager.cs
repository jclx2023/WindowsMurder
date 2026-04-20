using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 单轮对话记录
/// </summary>
[Serializable]
public class ConversationTurn
{
    /// <summary>
    /// 玩家输入。第一轮固定为 INITIAL_TRIGGER，保证历史数组交替合法。
    /// </summary>
    public string   playerInput;
    public string   aiReply;
    public bool     disclosed;    // 该轮 AI 是否已给出必要信息
    public string[] suggestions;  // 该轮给出的 suggestions，用于防止重复提问
}

/// <summary>
/// 对话历史管理器 - 管理 LLM 多轮对话上下文
///
/// 重构后使用结构化消息数组，将角色卡（system）与对话历史（user/assistant）分离，
/// 替代原来把所有内容打包成单条 user 消息的文字补全做法。
/// </summary>
public class ConversationHistoryManager : MonoBehaviour
{
    [Header("调试")]
    public bool enableDebugLog = true;

    /// <summary>
    /// 初始触发常量：第一轮发送的语言中性触发词，让模型输出 OPENING_LINE。
    /// 存入历史记录确保消息数组始终 user/assistant 交替。
    /// </summary>
    public const string INITIAL_TRIGGER = "[START]";

    // 最多保留的历史轮数（控制 context window）
    private const int MAX_HISTORY_TURNS = 6;

    // 私有状态
    private string                 initialSystemPrompt = "";  // 角色卡 + 系统规则（固定，每轮重用为 system prompt）
    private List<ConversationTurn> turns;                     // 历史对话轮次
    private bool                   cumulativeDisclosed = false;
    private bool                   isLLMActive         = false;
    private string                 lastPlayerInput     = "";

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
        turns               = new List<ConversationTurn>();
        isLLMActive         = true;
        cumulativeDisclosed = false;
        lastPlayerInput     = "";

        DebugLog($"开始 LLM 会话，初始 prompt 长度: {initialPrompt.Length}");

        StartCoroutine(SendInitialPrompt(initialPrompt, onResponse, dialogueManager));
    }

    /// <summary>
    /// 构建结构化消息数组（玩家输入后调用）。
    /// 返回 (systemPrompt, messages)，直接传入 ILLMProvider.GenerateText。
    /// STATE Block 作为当前 user 消息的前缀自动注入。
    /// </summary>
    public (string systemPrompt, List<LLMMessage> messages) BuildMessages(string playerInput)
    {
        if (!isLLMActive)
        {
            DebugLog("警告: LLM 未激活");
            return (playerInput, new List<LLMMessage> { new LLMMessage("user", playerInput) });
        }

        lastPlayerInput = playerInput;

        var messages = new List<LLMMessage>();

        // 添加历史轮次（最近 MAX_HISTORY_TURNS 轮）
        int startIndex = Mathf.Max(0, turns.Count - MAX_HISTORY_TURNS);
        for (int i = startIndex; i < turns.Count; i++)
        {
            ConversationTurn turn = turns[i];
            messages.Add(new LLMMessage("user",      turn.playerInput ?? INITIAL_TRIGGER));
            messages.Add(new LLMMessage("assistant", turn.aiReply));
        }

        // 当前玩家输入：STATE Block 作为前缀
        string stateBlock = BuildStateBlock();
        messages.Add(new LLMMessage("user", $"{stateBlock}\n\n{playerInput}"));

        DebugLog($"构建消息完成，历史轮数: {turns.Count}，消息总数: {messages.Count}");
        return (initialSystemPrompt, messages);
    }

    /// <summary>
    /// 记录一轮 AI 回复（由 DialogueManager 在解析 JSON 后调用）
    /// </summary>
    public void AddLLMResponse(string aiReply, bool disclosed = false, string[] suggestions = null)
    {
        if (!isLLMActive || string.IsNullOrEmpty(aiReply))
        {
            DebugLog("警告: LLM 未激活或回复为空，跳过记录");
            return;
        }

        turns.Add(new ConversationTurn
        {
            playerInput = string.IsNullOrEmpty(lastPlayerInput) ? INITIAL_TRIGGER : lastPlayerInput,
            aiReply     = aiReply,
            disclosed   = disclosed,
            suggestions = suggestions
        });

        lastPlayerInput = "";

        if (disclosed) cumulativeDisclosed = true;

        DebugLog($"记录第 {turns.Count} 轮对话，disclosed: {disclosed}，累计披露: {cumulativeDisclosed}");
    }

    /// <summary>更新信息披露状态（可由外部直接调用）</summary>
    public void UpdateDisclosureState(bool disclosed)
    {
        if (disclosed && !cumulativeDisclosed)
        {
            cumulativeDisclosed = true;
            DebugLog("必要信息披露状态已更新为 true");
        }
    }

    /// <summary>结束当前 LLM 会话，清理状态</summary>
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
    private IEnumerator SendInitialPrompt(
        string          initialPrompt,
        Action<string>  onResponse,
        DialogueManager dialogueManager)
    {
        bool   responseReceived = false;
        string rawResponse      = "";

        // 初始轮：以 INITIAL_TRIGGER 触发，让模型输出 OPENING_LINE
        var initialMessages = new List<LLMMessage>
        {
            new LLMMessage("user", INITIAL_TRIGGER)
        };

        yield return StartCoroutine(dialogueManager.GetCurrentProvider().GenerateText(
            initialPrompt,
            initialMessages,
            response =>
            {
                rawResponse      = response;
                responseReceived = true;
            },
            error =>
            {
                rawResponse      = "";
                responseReceived = true;
                DebugLog($"初始 Prompt 请求失败: {error}");
            }
        ));

        while (!responseReceived) yield return null;

        // 解析 JSON
        LLMJsonResponse parsed = dialogueManager.TryParseJsonResponse(rawResponse);

        // 更新披露状态
        if (parsed.disclosed) cumulativeDisclosed = true;

        // 触发 Suggestions
        DialogueManager.FireSuggestionsReady(parsed.suggestions);

        // 记录初始轮：playerInput = INITIAL_TRIGGER，保证后续历史交替合法
        turns.Add(new ConversationTurn
        {
            playerInput = INITIAL_TRIGGER,
            aiReply     = parsed.reply,
            disclosed   = parsed.disclosed,
            suggestions = parsed.suggestions
        });

        // 兜底：disclosed=true 时强制 end=true
        if (parsed.disclosed && !parsed.end)
        {
            DebugLog("初始轮：disclosed=true but end=false — forcing end=true.");
            parsed.end = true;
        }

        string replyForUI = parsed.end
            ? parsed.reply.TrimEnd() + "\nend"
            : parsed.reply;

        DebugLog($"初始 LLM 响应完成，reply 长度: {parsed.reply.Length}，end: {parsed.end}");
        onResponse?.Invoke(replyForUI);
    }

    /// <summary>
    /// 构建 STATE Block（每轮动态注入对话状态 + suggestion 边界约束）
    /// </summary>
    private string BuildStateBlock()
    {
        int    turnNumber   = (turns?.Count ?? 0) + 1;
        string disclosedStr = cumulativeDisclosed ? "true" : "false";

        string stateBlock =
            $"[STATE]\nTURN: {turnNumber}\nDISCLOSED: {disclosedStr}\n" +
            "SUGGESTION_RULE: Only reference concepts from PLAYER_PRIOR_CONTEXT " +
            "or concepts already explicitly mentioned in this conversation. " +
            "Do NOT use any undisclosed information units in suggestions.";

        // 所有必要信息已给出时，强制要求本轮结束
        if (cumulativeDisclosed)
        {
            stateBlock +=
                "\nACTION_REQUIRED: All required information has been disclosed. " +
                "You MUST set end=true in this reply.";
        }

        return stateBlock;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[ConversationHistoryManager] {message}");
    }
}
