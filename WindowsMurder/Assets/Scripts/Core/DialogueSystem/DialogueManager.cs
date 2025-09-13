using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("组件引用")]
    public DialogueUI dialogueUI;
    public GeminiAPI geminiAPI;

    [Header("角色数据")]
    public CharacterDataSO[] characterDatabase; // 角色数据配置

    [Header("调试")]
    public bool enableDebugLog = true;

    // 私有变量
    private Dictionary<string, CharacterData> characters;
    private Dictionary<string, List<string>> conversationHistory; // 每个角色的对话历史
    private string currentDialogueId;
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
        characters = new Dictionary<string, CharacterData>();
        conversationHistory = new Dictionary<string, List<string>>();

        // 加载角色数据
        LoadCharacterData();

        // 查找组件引用
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (geminiAPI == null)
            geminiAPI = FindObjectOfType<GeminiAPI>();

        DebugLog("DialogueManager 初始化完成");
    }

    /// <summary>
    /// 加载角色数据
    /// </summary>
    private void LoadCharacterData()
    {
        if (characterDatabase == null) return;

        foreach (var characterSO in characterDatabase)
        {
            if (characterSO != null)
            {
                characters[characterSO.characterId] = new CharacterData
                {
                    characterId = characterSO.characterId,
                    displayName = characterSO.displayName,
                    personality = characterSO.personality,
                    knownFacts = new List<string>(characterSO.knownFacts),
                    secrets = new List<string>(characterSO.secrets)
                };

                // 初始化对话历史
                conversationHistory[characterSO.characterId] = new List<string>();
            }
        }

        DebugLog($"加载了 {characters.Count} 个角色数据");
    }

    /// <summary>
    /// 开始指定的对话
    /// </summary>
    /// <param name="dialogueId">对话ID</param>
    public void StartDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogError("DialogueManager: dialogueId 不能为空");
            return;
        }

        // 加载对话数据
        DialogueData dialogueData = DialogueLoader.Load(dialogueId);
        if (dialogueData == null)
        {
            Debug.LogError($"DialogueManager: 无法加载对话 {dialogueId}");
            return;
        }

        currentDialogueId = dialogueId;
        DebugLog($"开始对话: {dialogueId}");

        // 启动UI播放
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(dialogueData);
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

        // 记录玩家消息到历史
        AddToHistory(characterId, $"玩家: {playerMessage}");

        // 构建AI提示词
        string prompt = BuildAIPrompt(characterId, playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        // 调用GeminiAPI
        yield return StartCoroutine(geminiAPI.GenerateText(
            prompt,
            response =>
            {
                aiResponse = ProcessAIResponse(response);
                responseReceived = true;
            },
            error =>
            {
                aiResponse = HandleAIError(characterId, error);
                responseReceived = true;
            }
        ));

        // 等待响应
        while (!responseReceived)
        {
            yield return null;
        }

        // 记录AI回复到历史
        AddToHistory(characterId, $"{GetCharacterDisplayName(characterId)}: {aiResponse}");

        // 返回结果
        onResponse?.Invoke(aiResponse);

        isProcessingLLM = false;
        DebugLog($"LLM响应完成: {characterId} -> {aiResponse.Substring(0, Mathf.Min(50, aiResponse.Length))}...");
    }

    /// <summary>
    /// 构建AI提示词
    /// </summary>
    private string BuildAIPrompt(string characterId, string playerMessage)
    {
        CharacterData character = GetCharacterData(characterId);
        if (character == null)
        {
            return $"请回复玩家的问题：{playerMessage}";
        }

        string prompt = $@"你现在扮演Windows XP系统中的{character.displayName}程序。

角色设定：
{character.personality}

你知道的事实：";

        foreach (string fact in character.knownFacts)
        {
            prompt += $"\n- {fact}";
        }

        prompt += "\n\n你隐藏的秘密：";
        foreach (string secret in character.secrets)
        {
            prompt += $"\n- {secret}";
        }

        // 添加最近的对话历史
        List<string> history = conversationHistory[characterId];
        if (history.Count > 0)
        {
            prompt += "\n\n最近的对话：";
            int startIndex = Mathf.Max(0, history.Count - 6); // 只包含最近3轮对话
            for (int i = startIndex; i < history.Count; i++)
            {
                prompt += $"\n{history[i]}";
            }
        }

        prompt += $@"

当前玩家问题：{playerMessage}

请用{character.displayName}的身份和性格回复，注意：
1. 保持角色性格，简洁回答（不超过100字）
2. 不要立即透露所有秘密，要根据问题谨慎回答
3. 可以表现出程序特有的表达方式
4. 如果问到敏感信息，可以表现出紧张或回避";

        return prompt;
    }

    /// <summary>
    /// 处理AI响应
    /// </summary>
    private string ProcessAIResponse(string rawResponse)
    {
        if (string.IsNullOrEmpty(rawResponse))
        {
            return "...无响应...";
        }

        // 清理响应文本
        string cleanResponse = rawResponse.Trim();

        // 移除可能的引号
        if (cleanResponse.StartsWith("\"") && cleanResponse.EndsWith("\""))
        {
            cleanResponse = cleanResponse.Substring(1, cleanResponse.Length - 2);
        }

        // 长度限制
        if (cleanResponse.Length > 200)
        {
            cleanResponse = cleanResponse.Substring(0, 197) + "...";
        }

        return cleanResponse;
    }

    /// <summary>
    /// 处理AI错误
    /// </summary>
    private string HandleAIError(string characterId, string error)
    {
        DebugLog($"AI请求失败: {error}");

        // 根据角色返回不同的错误信息
        switch (characterId)
        {
            case "RecycleBin":
                return "系统错误...我的数据似乎损坏了...";
            case "TaskManager":
                return "连接超时...请检查网络状态...";
            default:
                return "系统故障...请稍后重试...";
        }
    }

    /// <summary>
    /// 对话完成回调 (从DialogueUI调用)
    /// </summary>
    /// <param name="dialogueId">完成的对话ID</param>
    public void OnDialogueComplete(string dialogueId)
    {
        DebugLog($"对话完成: {dialogueId}");
        currentDialogueId = null;

        // 可以在这里触发游戏事件，比如解锁新线索等
        // GameEvents.OnDialogueCompleted?.Invoke(dialogueId);
    }

    /// <summary>
    /// 获取角色数据
    /// </summary>
    private CharacterData GetCharacterData(string characterId)
    {
        if (characters.ContainsKey(characterId))
        {
            return characters[characterId];
        }
        return null;
    }

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    private string GetCharacterDisplayName(string characterId)
    {
        CharacterData character = GetCharacterData(characterId);
        return character?.displayName ?? characterId;
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

        // 限制历史长度
        if (conversationHistory[characterId].Count > 20)
        {
            conversationHistory[characterId].RemoveAt(0);
        }
    }

    /// <summary>
    /// 获取角色对话历史
    /// </summary>
    public List<string> GetCharacterHistory(string characterId)
    {
        if (conversationHistory.ContainsKey(characterId))
        {
            return new List<string>(conversationHistory[characterId]);
        }
        return new List<string>();
    }

    /// <summary>
    /// 清除指定角色的对话历史
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
    /// 更新角色数据（比如发现新线索后）
    /// </summary>
    public void UpdateCharacterFacts(string characterId, string newFact)
    {
        CharacterData character = GetCharacterData(characterId);
        if (character != null && !character.knownFacts.Contains(newFact))
        {
            character.knownFacts.Add(newFact);
            DebugLog($"为 {characterId} 添加新事实: {newFact}");
        }
    }

    /// <summary>
    /// 检查是否正在进行对话
    /// </summary>
    public bool IsDialogueActive()
    {
        return !string.IsNullOrEmpty(currentDialogueId);
    }

    /// <summary>
    /// 强制结束当前对话
    /// </summary>
    public void ForceEndCurrentDialogue()
    {
        if (IsDialogueActive())
        {
            DebugLog($"强制结束对话: {currentDialogueId}");
            currentDialogueId = null;

            if (dialogueUI != null)
            {
                dialogueUI.ForceEndDialogue();
            }
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

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器测试功能
    /// </summary>
    [ContextMenu("测试对话系统")]
    private void TestDialogueSystem()
    {
        StartDialogue("test_dialogue");
    }

    [ContextMenu("打印所有角色历史")]
    private void PrintAllHistory()
    {
        foreach (var kvp in conversationHistory)
        {
            Debug.Log($"=== {kvp.Key} 的对话历史 ===");
            foreach (string line in kvp.Value)
            {
                Debug.Log(line);
            }
        }
    }
#endif
}

/// <summary>
/// 角色数据结构
/// </summary>
[System.Serializable]
public class CharacterData
{
    public string characterId;
    public string displayName;
    public string personality;
    public List<string> knownFacts;
    public List<string> secrets;
}

/// <summary>
/// 角色数据ScriptableObject (需要单独创建文件)
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "WindowsMurder/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("基本信息")]
    public string characterId = "NewCharacter";
    public string displayName = "新角色";

    [Header("AI设定")]
    [TextArea(3, 8)]
    public string personality = "请填写角色性格描述...";

    [Header("已知事实")]
    public List<string> knownFacts = new List<string>();

    [Header("隐藏秘密")]
    public List<string> secrets = new List<string>();
}