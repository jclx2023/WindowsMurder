using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("�������")]
    public DialogueUI dialogueUI;
    public GeminiAPI geminiAPI;

    [Header("��ɫPrompts")]
    public CharacterPrompt[] characterPrompts; // ÿ����ɫ�Ķ���prompt

    [Header("����")]
    public bool enableDebugLog = true;

    // ˽�б���
    private Dictionary<string, string> characterPromptsDict;
    private Dictionary<string, List<string>> conversationHistory; // ÿ����ɫ�ĶԻ���ʷ
    private string currentDialogueId;
    private bool isProcessingLLM = false;

    void Start()
    {
        InitializeManager();
    }

    /// <summary>
    /// ��ʼ��������
    /// </summary>
    private void InitializeManager()
    {
        // ��ʼ�����ݽṹ
        characterPromptsDict = new Dictionary<string, string>();
        conversationHistory = new Dictionary<string, List<string>>();

        // ���ؽ�ɫPrompts
        LoadCharacterPrompts();

        // �����������
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (geminiAPI == null)
            geminiAPI = FindObjectOfType<GeminiAPI>();

        DebugLog("DialogueManager ��ʼ�����");
    }

    /// <summary>
    /// ���ؽ�ɫPrompts
    /// </summary>
    private void LoadCharacterPrompts()
    {
        if (characterPrompts == null) return;

        foreach (var prompt in characterPrompts)
        {
            if (!string.IsNullOrEmpty(prompt.characterId) && !string.IsNullOrEmpty(prompt.prompt))
            {
                characterPromptsDict[prompt.characterId] = prompt.prompt;

                // ��ʼ���Ի���ʷ
                if (!conversationHistory.ContainsKey(prompt.characterId))
                {
                    conversationHistory[prompt.characterId] = new List<string>();
                }
            }
        }

        DebugLog($"������ {characterPromptsDict.Count} ����ɫPrompts");
    }

    /// <summary>
    /// ��ʼָ���ĶԻ�
    /// </summary>
    /// <param name="dialogueId">�Ի�ID</param>
    public void StartDialogue(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogError("DialogueManager: dialogueId ����Ϊ��");
            return;
        }

        // ���ضԻ�����
        DialogueData dialogueData = DialogueLoader.Load(dialogueId);
        if (dialogueData == null)
        {
            Debug.LogError($"DialogueManager: �޷����ضԻ� {dialogueId}");
            return;
        }

        currentDialogueId = dialogueId;
        DebugLog($"��ʼ�Ի�: {dialogueId}");

        // ����UI����
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(dialogueData);
        }
        else
        {
            Debug.LogError("DialogueManager: DialogueUI ���δ�ҵ�");
        }
    }

    /// <summary>
    /// ����LLM��Ϣ (��DialogueUI����)
    /// </summary>
    /// <param name="characterId">��ɫID</param>
    /// <param name="playerMessage">�����Ϣ</param>
    /// <param name="onResponse">�ص�����</param>
    public void ProcessLLMMessage(string characterId, string playerMessage, Action<string> onResponse)
    {
        if (isProcessingLLM)
        {
            DebugLog("���ڴ�������LLM��������");
            onResponse?.Invoke("ϵͳ��æ�����Ժ�����...");
            return;
        }

        if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(playerMessage))
        {
            onResponse?.Invoke("�������...");
            return;
        }

        DebugLog($"����LLM��Ϣ: {characterId} <- {playerMessage}");

        StartCoroutine(ProcessLLMCoroutine(characterId, playerMessage, onResponse));
    }

    /// <summary>
    /// LLM����Э��
    /// </summary>
    private IEnumerator ProcessLLMCoroutine(string characterId, string playerMessage, Action<string> onResponse)
    {
        isProcessingLLM = true;

        // ��������prompt
        string fullPrompt = BuildFullPrompt(characterId, playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        // ����GeminiAPI
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
                DebugLog($"AI����ʧ��: {error}");
            }
        ));

        // �ȴ���Ӧ
        while (!responseReceived)
        {
            yield return null;
        }

        // ��¼�Ի���ʷ
        AddToHistory(characterId, $"���: {playerMessage}");
        AddToHistory(characterId, $"AI�ظ�: {aiResponse}");

        // ���ؽ��
        onResponse?.Invoke(aiResponse);

        isProcessingLLM = false;
        DebugLog($"LLM��Ӧ���: {aiResponse.Substring(0, Mathf.Min(30, aiResponse.Length))}...");
    }

    /// <summary>
    /// ����������prompt
    /// </summary>
    private string BuildFullPrompt(string characterId, string playerMessage)
    {
        // ��ȡ��ɫ����prompt
        string basePrompt = "";
        if (characterPromptsDict.ContainsKey(characterId))
        {
            basePrompt = characterPromptsDict[characterId];
        }
        else
        {
            basePrompt = $"����{characterId}������ش��û����⡣";
            DebugLog($"����: �Ҳ�����ɫ {characterId} ��prompt��ʹ��Ĭ��prompt");
        }

        // ��ӶԻ���ʷ
        string historyText = "";
        if (conversationHistory.ContainsKey(characterId) && conversationHistory[characterId].Count > 0)
        {
            historyText = "\n\n֮ǰ�ĶԻ�:\n";
            List<string> history = conversationHistory[characterId];
            int startIndex = Mathf.Max(0, history.Count - 4); // ֻ�������2�ֶԻ�

            for (int i = startIndex; i < history.Count; i++)
            {
                historyText += history[i] + "\n";
            }
        }

        // �������prompt
        string fullPrompt = $"{basePrompt}{historyText}\n\n��ǰ����: {playerMessage}\n\n��ظ�:";

        return fullPrompt;
    }

    /// <summary>
    /// ����AI��Ӧ
    /// </summary>
    private string CleanAIResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            return "...";
        }

        string cleaned = response.Trim();

        // �Ƴ����ܵ�����
        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2);
        }

        // ��������
        if (cleaned.Length > 150)
        {
            cleaned = cleaned.Substring(0, 147) + "...";
        }

        return cleaned;
    }

    /// <summary>
    /// ��ȡ������Ӧ
    /// </summary>
    private string GetErrorResponse(string characterId)
    {
        switch (characterId)
        {
            case "RecycleBin":
                return "ϵͳ����...�ҵ����ݳ���������...";
            case "TaskManager":
                return "���ӳ�ʱ...���Ժ�����...";
            case "ControlPanel":
                return "���ʱ��ܾ�...Ȩ�޲���...";
            default:
                return "ϵͳ����...�޷���Ӧ...";
        }
    }

    /// <summary>
    /// �Ի���ɻص� (��DialogueUI����)
    /// </summary>
    /// <param name="dialogueId">��ɵĶԻ�ID</param>
    public void OnDialogueComplete(string dialogueId)
    {
        DebugLog($"�Ի����: {dialogueId}");
        currentDialogueId = null;
    }

    /// <summary>
    /// ��ӵ��Ի���ʷ
    /// </summary>
    private void AddToHistory(string characterId, string message)
    {
        if (!conversationHistory.ContainsKey(characterId))
        {
            conversationHistory[characterId] = new List<string>();
        }

        conversationHistory[characterId].Add(message);

        // ������ʷ���ȣ�����prompt����
        if (conversationHistory[characterId].Count > 8)
        {
            conversationHistory[characterId].RemoveAt(0);
        }
    }

    /// <summary>
    /// �����ɫ�Ի���ʷ
    /// </summary>
    public void ClearCharacterHistory(string characterId)
    {
        if (conversationHistory.ContainsKey(characterId))
        {
            conversationHistory[characterId].Clear();
            DebugLog($"����� {characterId} �ĶԻ���ʷ");
        }
    }

    /// <summary>
    /// ������־
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DialogueManager] {message}");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("���Ի���վ�Ի�")]
    private void TestRecycleBin()
    {
        StartDialogue("recyclebin_test");
    }

    [ContextMenu("��������������Ի�")]
    private void TestTaskManager()
    {
        StartDialogue("taskmanager_test");
    }
#endif
}

/// <summary>
/// ��ɫPrompt����
/// </summary>
[System.Serializable]
public class CharacterPrompt
{
    [Header("��ɫ��Ϣ")]
    public string characterId;

    [Header("����Prompt")]
    [TextArea(5, 15)]
    public string prompt;
}