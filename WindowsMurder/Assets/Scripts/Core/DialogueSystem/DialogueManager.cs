using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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

    // 当前使用的Provider
    private ILLMProvider currentProvider;

    // 私有变量
    private Dictionary<string, List<string>> conversationHistory;
    private string currentDialogueFile;
    private string currentDialogueBlockId;
    private bool isProcessingLLM = false;

    // 对话队列
    private Queue<(string fileName, string blockId)> dialogueQueue = new Queue<(string, string)>();
    private bool isPlayingDialogue = false;

    void Start()
    {
        InitializeManager();
    }

    /// <summary>
    /// 初始化管理器
    /// </summary>
    private void InitializeManager()
    {
        // 初始化数据结构
        conversationHistory = new Dictionary<string, List<string>>();

        // 查找组件引用
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (historyManager == null)
            historyManager = FindObjectOfType<ConversationHistoryManager>();

        // 初始化所有LLM Provider
        InitializeProviders();

        DebugLog("DialogueManager 初始化完成");
    }

    void InitializeProviders()
    {
        if (geminiProvider == null)
            Debug.LogError("GeminiProvider未配置");
        if (openaiProvider == null)
            Debug.LogError("OpenAIProvider未配置");
        if (deepseekProvider == null)
            Debug.LogError("DeepSeekProvider未配置");

        UpdateCurrentProvider();
    }

    /// <summary>
    /// 更新当前使用的Provider
    /// </summary>
    private void UpdateCurrentProvider()
    {
        if (GlobalSystemManager.Instance == null)
        {
            Debug.LogError("GlobalSystemManager未找到，使用默认Gemini Provider");
            currentProvider = geminiProvider;
            return;
        }

        LLMProvider providerType = GlobalSystemManager.Instance.GetCurrentLLMProvider();

        switch (providerType)
        {
            case LLMProvider.Gemini:
                currentProvider = geminiProvider;
                break;
            case LLMProvider.GPT:
                currentProvider = openaiProvider;
                break;
            case LLMProvider.DeepSeek:
                currentProvider = deepseekProvider;
                break;
            default:
                currentProvider = geminiProvider;
                break;
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

    // ==================== 对话管理 ====================

    public string CleanAIResponsePublic(string response)
    {
        return CleanAIResponse(response);
    }

    /// <summary>
    /// 获取当前语言对应的剧本文件名
    /// </summary>
    private string GetCurrentScriptFileName()
    {
        string fileName = "zh";
        if (LanguageManager.Instance != null)
        {
            switch (LanguageManager.Instance.currentLanguage)
            {
                case SupportedLanguage.Chinese:
                    fileName = "zh";
                    break;
                case SupportedLanguage.English:
                    fileName = "en";
                    break;
                case SupportedLanguage.Japanese:
                    fileName = "jp";
                    break;
                default:
                    fileName = "zh";
                    break;
            }
        }
        return fileName;
    }

    /// <summary>
    /// 开始指定的对话
    /// </summary>
    public void StartDialogue(string blockId)
    {
        string fileName = GetCurrentScriptFileName();
        StartDialogue(fileName, blockId);
    }

    /// <summary>
    /// 开始指定的对话 - 完整版本（带队列）
    /// </summary>
    public void StartDialogue(string fileName, string blockId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueManager: fileName 或 blockId 不能为空 (fileName: {fileName}, blockId: {blockId})");
            return;
        }

        // 如果当前正在播放对话，加入队列
        if (isPlayingDialogue)
        {
            dialogueQueue.Enqueue((fileName, blockId));
            DebugLog($"对话块已加入队列: {fileName}:{blockId}，队列长度: {dialogueQueue.Count}");
            return;
        }

        // 直接播放
        PlayDialogue(fileName, blockId);
    }

    /// <summary>
    /// 实际播放对话
    /// </summary>
    private void PlayDialogue(string fileName, string blockId)
    {
        DialogueData dialogueData = DialogueLoader.LoadBlock(fileName, blockId);

        currentDialogueFile = fileName;
        currentDialogueBlockId = blockId;
        isPlayingDialogue = true;

        DebugLog($"开始播放对话: {fileName}:{blockId}");

        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(dialogueData, fileName, blockId);
        }
    }

    public void ProcessLLMMessage(string characterId, string playerMessage, Action<string> onResponse)
    {
        if (isProcessingLLM)
        {
            DebugLog("正在处理其他LLM请求，跳过");
            onResponse?.Invoke("系统繁忙，请稍后再试...");
            return;
        }

        if (string.IsNullOrEmpty(playerMessage))
        {
            onResponse?.Invoke("输入错误...");
            return;
        }

        DebugLog($"处理LLM消息: {characterId} <- {playerMessage}");

        StartCoroutine(ProcessLLMCoroutine(characterId, playerMessage, onResponse));
    }

    /// <summary>
    /// LLM处理协程 - 使用当前选择的Provider
    /// </summary>
    private IEnumerator ProcessLLMCoroutine(string characterId, string playerMessage, Action<string> onResponse)
    {
        isProcessingLLM = true;

        // 使用HistoryManager构建包含历史的prompt
        string fullPrompt = historyManager.BuildPromptWithHistory(playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        // 使用当前Provider（不再hardcoded使用geminiAPI）
        yield return StartCoroutine(currentProvider.GenerateText(
            fullPrompt,
            response =>
            {
                aiResponse = CleanAIResponse(response);
                responseReceived = true;
            },
            error =>
            {
                aiResponse = GetErrorResponse(characterId);
                responseReceived = true;
                DebugLog($"AI请求失败: {error}");
            }
        ));

        while (!responseReceived)
        {
            yield return null;
        }

        // 将AI回复添加到历史管理器
        historyManager.AddLLMResponse(aiResponse);

        onResponse?.Invoke(aiResponse);
        isProcessingLLM = false;

        DebugLog($"LLM响应完成: {aiResponse.Substring(0, Mathf.Min(30, aiResponse.Length))}...");
    }

    /// <summary>
    /// 清理AI响应
    /// </summary>
    private string CleanAIResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return "...";
        }

        string cleaned = response.Trim();

        // 移除可能的引号
        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2);
        }

        // 长度限制
        if (cleaned.Length > 300)
        {
            cleaned = cleaned.Substring(0, 297) + "...";
        }

        return cleaned;
    }

    /// <summary>
    /// 获取错误响应
    /// </summary>
    private string GetErrorResponse(string characterId)
    {
        switch (characterId)
        {
            case "RecycleBin":
                return "系统错误...我的数据出现了问题...";
            case "TaskManager":
                return "连接超时...请稍后重试...";
            case "ControlPanel":
                return "访问被拒绝...权限不足...";
            default:
                return "系统故障...无法响应...";
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
        {
            gameFlow.OnDialogueBlockComplete(blockId);
        }

        // 检查队列
        if (dialogueQueue.Count > 0)
        {
            // 有队列，播放下一个
            var next = dialogueQueue.Dequeue();
            DebugLog($"从队列播放下一个对话: {next.fileName}:{next.blockId}，剩余队列: {dialogueQueue.Count}");
            PlayDialogue(next.fileName, next.blockId);
        }
        else
        {
            currentDialogueBlockId = null;  // 现在才清空
            isPlayingDialogue = false;
            DebugLog("所有对话播放完毕");
        }
    }

    public bool IsDialogueActive()
    {
        return !string.IsNullOrEmpty(currentDialogueBlockId);
    }

    /// <summary>
    /// 清除角色对话历史
    /// </summary>
    public void ClearCharacterHistory(string characterId)
    {
        if (conversationHistory.ContainsKey(characterId))
        {
            conversationHistory[characterId].Clear();
            DebugLog($"清除了 {characterId} 的对话历史");
        }
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DialogueManager] {message}");
        }
    }

    void OnDestroy()
    {
        // 取消事件监听
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.OnLLMProviderChanged -= OnProviderChanged;
        }
    }
}
