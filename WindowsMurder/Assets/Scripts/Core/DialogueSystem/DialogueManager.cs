using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("组件引用")]
    public DialogueUI dialogueUI;
    public GeminiAPI geminiAPI;
    public ConversationHistoryManager historyManager;

    [Header("调试")]
    public bool enableDebugLog = true;

    // 私有变量
    private Dictionary<string, List<string>> conversationHistory; // 每个角色的对话历史
    private string currentDialogueFile;
    private string currentDialogueBlockId;
    private bool isProcessingLLM = false;

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
        if (geminiAPI == null)
            geminiAPI = FindObjectOfType<GeminiAPI>();
        if (historyManager == null)
            historyManager = FindObjectOfType<ConversationHistoryManager>();

        DebugLog("DialogueManager 初始化完成");
    }
    public string CleanAIResponsePublic(string response)
    {
        return CleanAIResponse(response);
    }

    /// <summary>
    /// 获取当前语言对应的剧本文件名
    /// </summary>
    private string GetCurrentScriptFileName()
    {
        string fileName = "zh"; // 默认中文
                                // 从 LanguageManager 获取当前语言
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
    /// <param name="fileName">剧本文件名</param>
    /// <param name="blockId">对话块ID</param>
    public void StartDialogue(string blockId)
    {
        string fileName = GetCurrentScriptFileName();
        StartDialogue(fileName, blockId);
    }

    /// <summary>
    /// 开始指定的对话 - 完整版本（保留兼容性）
    /// </summary>
    /// <param name="fileName">剧本文件名</param>
    /// <param name="blockId">对话块ID</param>
    public void StartDialogue(string fileName, string blockId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueManager: fileName 或 blockId 不能为空 (fileName: {fileName}, blockId: {blockId})");
            return;
        }

        // 使用新的 DialogueLoader 加载对话数据
        DialogueData dialogueData = DialogueLoader.LoadBlock(fileName, blockId);

        currentDialogueFile = fileName;
        currentDialogueBlockId = blockId;
        DebugLog($"开始对话: {fileName}:{blockId}");

        // 启动UI播放，传递文件名和块ID
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
    /// LLM处理协程
    /// </summary>
    private IEnumerator ProcessLLMCoroutine(string characterId, string playerMessage, Action<string> onResponse)
    {
        isProcessingLLM = true;

        // 使用HistoryManager构建包含历史的prompt
        string fullPrompt = historyManager.BuildPromptWithHistory(playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        yield return StartCoroutine(geminiAPI.GenerateText(
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
    /// 对话块完成回调 (从DialogueUI调用)
    /// </summary>
    /// <param name="fileName">剧本文件名</param>
    /// <param name="blockId">对话块ID</param>
    public void OnDialogueBlockComplete(string fileName, string blockId)
    {
        DebugLog($"对话块完成: {fileName}:{blockId}");

        // 清空当前状态
        currentDialogueFile = null;
        currentDialogueBlockId = null;

        // 通知 GameFlowController 对话完成
        GameFlowController gameFlow = FindObjectOfType<GameFlowController>();
        if (gameFlow != null)
        {
            gameFlow.OnDialogueBlockComplete(blockId);
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
}