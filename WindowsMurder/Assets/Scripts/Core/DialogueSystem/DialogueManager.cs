using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// LLM 返回的结构化 JSON 响应
/// 对应新版 Prompt 要求 LLM 输出的格式：
/// { "reply": "...", "suggestions": [...], "disclosed": bool, "end": bool }
/// </summary>
[Serializable]
public class LLMJsonResponse
{
    public string reply = "";
    public string[] suggestions;
    public bool disclosed = false;
    public bool end = false;
}

public class DialogueManager : MonoBehaviour
{
    [Header("组件引用")]
    public DialogueUI dialogueUI;
    public ConversationHistoryManager historyManager;

    [Header("调试")]
    public bool enableDebugLog = true;

    [Header("LLM Providers")]
    public GeminiProvider geminiProvider;
    public OpenAIProvider openaiProvider;
    public DeepSeekProvider deepseekProvider;

    // 当前使用的 Provider
    private ILLMProvider currentProvider;

    // 私有变量
    private Dictionary<string, List<string>> conversationHistory;
    private string currentDialogueFile;
    private string currentDialogueBlockId;
    private bool isProcessingLLM = false;

    // 对话队列
    private Queue<(string fileName, string blockId)> dialogueQueue = new Queue<(string, string)>();
    private bool isPlayingDialogue = false;

    // ==================== 静态事件：Suggestions ====================

    /// <summary>
    /// 当 LLM 返回建议问题时触发，DialogueUI 订阅后更新输入框 Placeholder
    /// </summary>
    public static event Action<string[]> OnSuggestionsReady;

    /// <summary>
    /// 供外部类触发 OnSuggestionsReady 的代理方法。
    /// C# 规定静态事件只能在声明类内部 Invoke，外部类需通过此方法调用。
    /// </summary>
    public static void FireSuggestionsReady(string[] suggestions)
    {
        if (suggestions != null && suggestions.Length > 0)
            OnSuggestionsReady?.Invoke(suggestions);
    }

    // ==================== Unity 生命周期 ====================

    void Start()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        conversationHistory = new Dictionary<string, List<string>>();

        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (historyManager == null)
            historyManager = FindObjectOfType<ConversationHistoryManager>();

        InitializeProviders();
        DebugLog("DialogueManager 初始化完成");
    }

    void InitializeProviders()
    {
        if (geminiProvider == null) Debug.LogError("GeminiProvider 未配置");
        if (openaiProvider == null) Debug.LogError("OpenAIProvider 未配置");
        if (deepseekProvider == null) Debug.LogError("DeepSeekProvider 未配置");
        UpdateCurrentProvider();
    }

    private void UpdateCurrentProvider()
    {
        if (GlobalSystemManager.Instance == null)
        {
            Debug.LogError("GlobalSystemManager 未找到，使用默认 Gemini Provider");
            currentProvider = geminiProvider;
            return;
        }

        LLMProvider providerType = GlobalSystemManager.Instance.GetCurrentLLMProvider();
        switch (providerType)
        {
            case LLMProvider.Gemini:   currentProvider = geminiProvider;   break;
            case LLMProvider.GPT:      currentProvider = openaiProvider;   break;
            case LLMProvider.DeepSeek: currentProvider = deepseekProvider; break;
            default:                   currentProvider = geminiProvider;   break;
        }

        DebugLog($"切换到 {currentProvider.GetProviderName()} Provider");
    }

    private void OnProviderChanged(LLMProvider newProvider)
    {
        UpdateCurrentProvider();
    }

    public ILLMProvider GetCurrentProvider()
    {
        return currentProvider;
    }

    // ==================== JSON 解析 ====================

    /// <summary>
    /// 尝试将 LLM 原始输出解析为 LLMJsonResponse。
    /// 支持：标准 JSON、被 ```json``` 包裹的 JSON。
    /// 失败时 Fallback：整个原始文本作为 reply，使用旧版 end 检测。
    /// </summary>
    public LLMJsonResponse TryParseJsonResponse(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return new LLMJsonResponse { reply = "...", suggestions = new string[0] };

        try
        {
            string cleaned = raw.Trim();

            // 去除 Markdown 代码围栏（部分 LLM 会包裹 ```json ... ```）
            if (cleaned.StartsWith("```"))
            {
                int firstNewline = cleaned.IndexOf('\n');
                int lastFence    = cleaned.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                    cleaned = cleaned.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
            }

            if (cleaned.StartsWith("{"))
            {
                LLMJsonResponse parsed = JsonUtility.FromJson<LLMJsonResponse>(cleaned);
                if (parsed != null && !string.IsNullOrEmpty(parsed.reply))
                {
                    DebugLog($"JSON 解析成功，reply 长度: {parsed.reply.Length}，" +
                             $"suggestions: {parsed.suggestions?.Length ?? 0}，end: {parsed.end}");
                    return parsed;
                }
            }
        }
        catch (Exception e)
        {
            DebugLog($"JSON 解析失败，使用 Fallback。原因: {e.Message}");
        }

        // Fallback：非 JSON 格式，兼容旧版纯文本输出
        return new LLMJsonResponse
        {
            reply       = CleanAIResponse(raw),
            suggestions = new string[0],
            disclosed   = false,
            end         = DialogueLoader.ShouldEndByAI(raw)
        };
    }

    // ==================== 对话管理 ====================

    public string CleanAIResponsePublic(string response)
    {
        return CleanAIResponse(response);
    }

    private string GetCurrentScriptFileName()
    {
        string fileName = "zh";
        if (LanguageManager.Instance != null)
        {
            switch (LanguageManager.Instance.currentLanguage)
            {
                case SupportedLanguage.Chinese:  fileName = "zh"; break;
                case SupportedLanguage.English:  fileName = "en"; break;
                case SupportedLanguage.Japanese: fileName = "jp"; break;
                default:                         fileName = "zh"; break;
            }
        }
        return fileName;
    }

    public void StartDialogue(string blockId)
    {
        string fileName = GetCurrentScriptFileName();
        StartDialogue(fileName, blockId);
    }

    public void StartDialogue(string fileName, string blockId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueManager: fileName 或 blockId 不能为空 " +
                           $"(fileName: {fileName}, blockId: {blockId})");
            return;
        }

        if (isPlayingDialogue)
        {
            dialogueQueue.Enqueue((fileName, blockId));
            DebugLog($"对话块已加入队列: {fileName}:{blockId}，队列长度: {dialogueQueue.Count}");
            return;
        }

        PlayDialogue(fileName, blockId);
    }

    private void PlayDialogue(string fileName, string blockId)
    {
        DialogueData dialogueData = DialogueLoader.LoadBlock(fileName, blockId);

        currentDialogueFile  = fileName;
        currentDialogueBlockId = blockId;
        isPlayingDialogue    = true;

        DebugLog($"开始播放对话: {fileName}:{blockId}");

        if (dialogueUI != null)
            dialogueUI.StartDialogue(dialogueData, fileName, blockId);
    }

    /// <summary>
    /// 处理玩家发送的消息，调用 LLM 并返回 AI 回复
    /// </summary>
    public void ProcessLLMMessage(string characterId, string playerMessage, Action<string> onResponse)
    {
        if (isProcessingLLM)
        {
            DebugLog("正在处理其他 LLM 请求，跳过");
            onResponse?.Invoke("系统繁忙，请稍后再试...");
            return;
        }

        if (string.IsNullOrEmpty(playerMessage))
        {
            onResponse?.Invoke("输入错误...");
            return;
        }

        DebugLog($"处理 LLM 消息: {characterId} <- {playerMessage}");
        StartCoroutine(ProcessLLMCoroutine(characterId, playerMessage, onResponse));
    }

    /// <summary>
    /// LLM 处理协程：构建 Prompt → 调用 Provider → 解析 JSON → 更新状态 → 回调
    /// </summary>
    private IEnumerator ProcessLLMCoroutine(string characterId, string playerMessage, Action<string> onResponse)
    {
        isProcessingLLM = true;

        string fullPrompt = historyManager.BuildPromptWithHistory(playerMessage);

        bool responseReceived = false;
        string rawResponse = "";

        yield return StartCoroutine(currentProvider.GenerateText(
            fullPrompt,
            response =>
            {
                rawResponse = response;
                responseReceived = true;
            },
            error =>
            {
                rawResponse = "";
                responseReceived = true;
                DebugLog($"AI 请求失败: {error}");
            }
        ));

        while (!responseReceived) yield return null;

        // 解析 JSON 响应
        LLMJsonResponse parsed = string.IsNullOrEmpty(rawResponse)
            ? new LLMJsonResponse { reply = GetErrorResponse(characterId), suggestions = new string[0] }
            : TryParseJsonResponse(rawResponse);

        // 将干净的 AI 回复（不含 end 标记）记录进历史
        historyManager.AddLLMResponse(parsed.reply, parsed.disclosed);

        // 触发 Suggestions 事件 → DialogueUI 更新输入框 Placeholder
        FireSuggestionsReady(parsed.suggestions);

        // 如果 LLM 要求结束，附加 "end" 标记（DialogueUI 会检测并清理）
        string replyForUI = parsed.end
            ? parsed.reply.TrimEnd() + "\nend"
            : parsed.reply;

        onResponse?.Invoke(replyForUI);
        isProcessingLLM = false;

        DebugLog($"LLM 响应完成，disclosed: {parsed.disclosed}，end: {parsed.end}");
    }

    private string CleanAIResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "...";

        string cleaned = response.Trim();

        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
            cleaned = cleaned.Substring(1, cleaned.Length - 2);

        if (cleaned.Length > 300)
            cleaned = cleaned.Substring(0, 297) + "...";

        return cleaned;
    }

    private string GetErrorResponse(string characterId)
    {
        switch (characterId)
        {
            case "RecycleBin":   return "系统错误...我的数据出现了问题...";
            case "TaskManager":  return "连接超时...请稍后重试...";
            case "ControlPanel": return "访问被拒绝...权限不足...";
            default:             return "系统故障...无法响应...";
        }
    }

    /// <summary>
    /// 对话块完成回调（带队列处理）
    /// </summary>
    public void OnDialogueBlockComplete(string fileName, string blockId)
    {
        DebugLog($"对话块完成: {fileName}:{blockId}");

        currentDialogueFile = null;

        GameFlowController gameFlow = FindObjectOfType<GameFlowController>();
        if (gameFlow != null)
            gameFlow.OnDialogueBlockComplete(blockId);

        if (dialogueQueue.Count > 0)
        {
            var next = dialogueQueue.Dequeue();
            DebugLog($"从队列播放下一个对话: {next.fileName}:{next.blockId}，" +
                     $"剩余队列: {dialogueQueue.Count}");
            PlayDialogue(next.fileName, next.blockId);
        }
        else
        {
            currentDialogueBlockId = null;
            isPlayingDialogue = false;
            DebugLog("所有对话播放完毕");
        }
    }

    public bool IsDialogueActive()
    {
        return !string.IsNullOrEmpty(currentDialogueBlockId);
    }

    public void ClearCharacterHistory(string characterId)
    {
        if (conversationHistory.ContainsKey(characterId))
        {
            conversationHistory[characterId].Clear();
            DebugLog($"清除了 {characterId} 的对话历史");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLog)
            Debug.Log($"[DialogueManager] {message}");
    }

    void OnDestroy()
    {
        if (GlobalSystemManager.Instance != null)
            GlobalSystemManager.OnLLMProviderChanged -= OnProviderChanged;
    }
}
