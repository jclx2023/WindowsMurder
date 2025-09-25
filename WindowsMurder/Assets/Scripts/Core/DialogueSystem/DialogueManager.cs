using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("�������")]
    public DialogueUI dialogueUI;
    public GeminiAPI geminiAPI;
    public ConversationHistoryManager historyManager;

    [Header("����")]
    public bool enableDebugLog = true;

    // ˽�б���
    private Dictionary<string, List<string>> conversationHistory; // ÿ����ɫ�ĶԻ���ʷ
    private string currentDialogueFile;
    private string currentDialogueBlockId;
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
        conversationHistory = new Dictionary<string, List<string>>();

        // �����������
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (geminiAPI == null)
            geminiAPI = FindObjectOfType<GeminiAPI>();
        if (historyManager == null)
            historyManager = FindObjectOfType<ConversationHistoryManager>();

        DebugLog("DialogueManager ��ʼ�����");
    }
    public string CleanAIResponsePublic(string response)
    {
        return CleanAIResponse(response);
    }

    /// <summary>
    /// ��ʼָ���ĶԻ�
    /// </summary>
    /// <param name="fileName">�籾�ļ���</param>
    /// <param name="blockId">�Ի���ID</param>
    public void StartDialogue(string fileName, string blockId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueManager: fileName �� blockId ����Ϊ�� (fileName: {fileName}, blockId: {blockId})");
            return;
        }

        // ʹ���µ� DialogueLoader ���ضԻ�����
        DialogueData dialogueData = DialogueLoader.LoadBlock(fileName, blockId);
        if (dialogueData == null)
        {
            Debug.LogError($"DialogueManager: �޷����ضԻ��� {fileName}:{blockId}");
            return;
        }

        currentDialogueFile = fileName;
        currentDialogueBlockId = blockId;
        DebugLog($"��ʼ�Ի�: {fileName}:{blockId}");

        // ����UI���ţ������ļ����Ϳ�ID
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(dialogueData, fileName, blockId);
        }
        else
        {
            Debug.LogError("DialogueManager: DialogueUI ���δ�ҵ�");
        }
    }

    public void ProcessLLMMessage(string characterId, string playerMessage, Action<string> onResponse)
    {
        if (isProcessingLLM)
        {
            DebugLog("���ڴ�������LLM��������");
            onResponse?.Invoke("ϵͳ��æ�����Ժ�����...");
            return;
        }

        if (string.IsNullOrEmpty(playerMessage))
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

        // ʹ��HistoryManager����������ʷ��prompt
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
                DebugLog($"AI����ʧ��: {error}");
            }
        ));

        while (!responseReceived)
        {
            yield return null;
        }

        // ��AI�ظ���ӵ���ʷ������
        historyManager.AddLLMResponse(aiResponse);

        onResponse?.Invoke(aiResponse);
        isProcessingLLM = false;

        DebugLog($"LLM��Ӧ���: {aiResponse.Substring(0, Mathf.Min(30, aiResponse.Length))}...");
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
        if (cleaned.Length > 300)
        {
            cleaned = cleaned.Substring(0, 297) + "...";
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
    /// �Ի�����ɻص� (��DialogueUI����)
    /// </summary>
    /// <param name="fileName">�籾�ļ���</param>
    /// <param name="blockId">�Ի���ID</param>
    public void OnDialogueBlockComplete(string fileName, string blockId)
    {
        DebugLog($"�Ի������: {fileName}:{blockId}");

        // ��յ�ǰ״̬
        currentDialogueFile = null;
        currentDialogueBlockId = null;

        // ֪ͨ GameFlowController �Ի����
        GameFlowController gameFlow = FindObjectOfType<GameFlowController>();
        if (gameFlow != null)
        {
            gameFlow.OnDialogueBlockComplete(blockId);
        }
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
    /// ��ȡ��ǰ�Ի���Ϣ
    /// </summary>
    /// <returns>���ظ�ʽ: "fileName:blockId"</returns>
    public string GetCurrentDialogueInfo()
    {
        if (string.IsNullOrEmpty(currentDialogueFile) || string.IsNullOrEmpty(currentDialogueBlockId))
        {
            return null;
        }

        return $"{currentDialogueFile}:{currentDialogueBlockId}";
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

    #region ���Թ���

    [ContextMenu("���Լ��ضԻ���")]
    private void TestLoadDialogueBlock()
    {
        // ���Լ��ص�һ���Ի���
        StartDialogue("main_script", "001");
    }

    [ContextMenu("��ʾ��ǰ�Ի���Ϣ")]
    private void ShowCurrentDialogueInfo()
    {
        string info = GetCurrentDialogueInfo();
        if (!string.IsNullOrEmpty(info))
        {
            Debug.Log($"��ǰ�Ի�: {info}");
        }
        else
        {
            Debug.Log("��ǰû�н����еĶԻ�");
        }
    }

    #endregion
}