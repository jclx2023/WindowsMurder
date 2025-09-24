using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("组件引用")]
    public DialogueUI dialogueUI;
    public GeminiAPI geminiAPI;

    [Header("角色Prompts")]
    public CharacterPrompt[] characterPrompts; // 每个角色的独立prompt

    [Header("调试")]
    public bool enableDebugLog = true;

    // 私有变量
    private Dictionary<string, string> characterPromptsDict;
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
        characterPromptsDict = new Dictionary<string, string>();
        conversationHistory = new Dictionary<string, List<string>>();

        // 加载角色Prompts
        LoadCharacterPrompts();

        // 查找组件引用
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (geminiAPI == null)
            geminiAPI = FindObjectOfType<GeminiAPI>();

        DebugLog("DialogueManager 初始化完成");
    }

    /// <summary>
    /// 加载角色Prompts
    /// </summary>
    private void LoadCharacterPrompts()
    {
        if (characterPrompts == null) return;

        foreach (var prompt in characterPrompts)
        {
            if (!string.IsNullOrEmpty(prompt.characterId) && !string.IsNullOrEmpty(prompt.prompt))
            {
                characterPromptsDict[prompt.characterId] = prompt.prompt;

                // 初始化对话历史
                if (!conversationHistory.ContainsKey(prompt.characterId))
                {
                    conversationHistory[prompt.characterId] = new List<string>();
                }
            }
        }

        DebugLog($"加载了 {characterPromptsDict.Count} 个角色Prompts");
    }

    /// <summary>
    /// 开始指定的对话
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
        if (dialogueData == null)
        {
            Debug.LogError($"DialogueManager: 无法加载对话块 {fileName}:{blockId}");
            return;
        }

        currentDialogueFile = fileName;
        currentDialogueBlockId = blockId;
        DebugLog($"开始对话: {fileName}:{blockId}");

        // 启动UI播放，传递文件名和块ID
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(dialogueData, fileName, blockId);
        }
        else
        {
            Debug.LogError("DialogueManager: DialogueUI 组件未找到");
        }
    }

    /// <summary>
    /// 处理LLM消息 (从DialogueUI调用)
    /// </summary>
    /// <param name="characterId">角色ID</param>
    /// <param name="playerMessage">玩家消息</param>
    /// <param name="onResponse">回调函数</param>
    public void ProcessLLMMessage(string characterId, string playerMessage, Action<string> onResponse)
    {
        if (isProcessingLLM)
        {
            DebugLog("正在处理其他LLM请求，跳过");
            onResponse?.Invoke("系统繁忙，请稍后再试...");
            return;
        }

        if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(playerMessage))
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

        // 构建完整prompt
        string fullPrompt = BuildFullPrompt(characterId, playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        // 调用GeminiAPI
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

        // 等待响应
        while (!responseReceived)
        {
            yield return null;
        }

        // 记录对话历史
        AddToHistory(characterId, $"玩家: {playerMessage}");
        AddToHistory(characterId, $"AI回复: {aiResponse}");

        // 返回结果
        onResponse?.Invoke(aiResponse);

        isProcessingLLM = false;
        DebugLog($"LLM响应完成: {aiResponse.Substring(0, Mathf.Min(30, aiResponse.Length))}...");
    }

    /// <summary>
    /// 构建完整的prompt
    /// </summary>
    private string BuildFullPrompt(string characterId, string playerMessage)
    {
        // 获取角色基础prompt
        string basePrompt = "";
        if (characterPromptsDict.ContainsKey(characterId))
        {
            basePrompt = characterPromptsDict[characterId];
        }
        else
        {
            basePrompt = $"你是{characterId}程序，请回答用户问题。";
            DebugLog($"警告: 找不到角色 {characterId} 的prompt，使用默认prompt");
        }

        // 添加对话历史
        string historyText = "";
        if (conversationHistory.ContainsKey(characterId) && conversationHistory[characterId].Count > 0)
        {
            historyText = "\n\n之前的对话:\n";
            List<string> history = conversationHistory[characterId];
            int startIndex = Mathf.Max(0, history.Count - 4); // 只保留最近2轮对话

            for (int i = startIndex; i < history.Count; i++)
            {
                historyText += history[i] + "\n";
            }
        }

        // 组合最终prompt
        string fullPrompt = $"{basePrompt}{historyText}\n\n当前问题: {playerMessage}\n\n请回复:";

        return fullPrompt;
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
        if (cleaned.Length > 150)
        {
            cleaned = cleaned.Substring(0, 147) + "...";
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

    /// <summary>
    /// 添加到对话历史
    /// </summary>
    private void AddToHistory(string characterId, string message)
    {
        if (!conversationHistory.ContainsKey(characterId))
        {
            conversationHistory[characterId] = new List<string>();
        }

        conversationHistory[characterId].Add(message);

        // 限制历史长度，避免prompt过长
        if (conversationHistory[characterId].Count > 8)
        {
            conversationHistory[characterId].RemoveAt(0);
        }
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
    /// 获取当前对话信息
    /// </summary>
    /// <returns>返回格式: "fileName:blockId"</returns>
    public string GetCurrentDialogueInfo()
    {
        if (string.IsNullOrEmpty(currentDialogueFile) || string.IsNullOrEmpty(currentDialogueBlockId))
        {
            return null;
        }

        return $"{currentDialogueFile}:{currentDialogueBlockId}";
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

    #region 调试工具

    [ContextMenu("测试加载对话块")]
    private void TestLoadDialogueBlock()
    {
        // 测试加载第一个对话块
        StartDialogue("main_script", "001");
    }

    [ContextMenu("显示当前对话信息")]
    private void ShowCurrentDialogueInfo()
    {
        string info = GetCurrentDialogueInfo();
        if (!string.IsNullOrEmpty(info))
        {
            Debug.Log($"当前对话: {info}");
        }
        else
        {
            Debug.Log("当前没有进行中的对话");
        }
    }

    #endregion
}

/// <summary>
/// 角色Prompt配置
/// </summary>
[System.Serializable]
public class CharacterPrompt
{
    [Header("角色信息")]
    public string characterId;

    [Header("完整Prompt")]
    [TextArea(5, 15)]
    public string prompt;
}