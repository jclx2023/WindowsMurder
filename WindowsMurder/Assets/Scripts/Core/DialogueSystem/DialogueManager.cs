using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("�������")]
    public DialogueUI dialogueUI;
    public ConversationHistoryManager historyManager;

    [Header("����")]
    public bool enableDebugLog = true;

    [Header("LLM Providers")]
    public GeminiProvider geminiProvider;
    public OpenAIProvider openaiProvider;
    public DeepSeekProvider deepseekProvider;

    // ��ǰʹ�õ�Provider
    private ILLMProvider currentProvider;

    // ˽�б���
    private Dictionary<string, List<string>> conversationHistory;
    private string currentDialogueFile;
    private string currentDialogueBlockId;
    private bool isProcessingLLM = false;

    // �Ի�����
    private Queue<(string fileName, string blockId)> dialogueQueue = new Queue<(string, string)>();
    private bool isPlayingDialogue = false;

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
        if (historyManager == null)
            historyManager = FindObjectOfType<ConversationHistoryManager>();

        // ��ʼ������LLM Provider
        InitializeProviders();

        DebugLog("DialogueManager ��ʼ�����");
    }

    void InitializeProviders()
    {
        if (geminiProvider == null)
            Debug.LogError("GeminiProviderδ����");
        if (openaiProvider == null)
            Debug.LogError("OpenAIProviderδ����");
        if (deepseekProvider == null)
            Debug.LogError("DeepSeekProviderδ����");

        UpdateCurrentProvider();
    }

    /// <summary>
    /// ���µ�ǰʹ�õ�Provider
    /// </summary>
    private void UpdateCurrentProvider()
    {
        if (GlobalSystemManager.Instance == null)
        {
            Debug.LogError("GlobalSystemManagerδ�ҵ���ʹ��Ĭ��Gemini Provider");
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

        DebugLog($"�л��� {currentProvider.GetProviderName()} Provider");
    }

    private void OnProviderChanged(LLMProvider newProvider)
    {
        UpdateCurrentProvider();
    }

    public ILLMProvider GetCurrentProvider()
    {
        return currentProvider;
    }

    // ==================== �Ի����� ====================

    public string CleanAIResponsePublic(string response)
    {
        return CleanAIResponse(response);
    }

    /// <summary>
    /// ��ȡ��ǰ���Զ�Ӧ�ľ籾�ļ���
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
    /// ��ʼָ���ĶԻ�
    /// </summary>
    public void StartDialogue(string blockId)
    {
        string fileName = GetCurrentScriptFileName();
        StartDialogue(fileName, blockId);
    }

    /// <summary>
    /// ��ʼָ���ĶԻ� - �����汾�������У�
    /// </summary>
    public void StartDialogue(string fileName, string blockId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueManager: fileName �� blockId ����Ϊ�� (fileName: {fileName}, blockId: {blockId})");
            return;
        }

        // �����ǰ���ڲ��ŶԻ����������
        if (isPlayingDialogue)
        {
            dialogueQueue.Enqueue((fileName, blockId));
            DebugLog($"�Ի����Ѽ������: {fileName}:{blockId}�����г���: {dialogueQueue.Count}");
            return;
        }

        // ֱ�Ӳ���
        PlayDialogue(fileName, blockId);
    }

    /// <summary>
    /// ʵ�ʲ��ŶԻ�
    /// </summary>
    private void PlayDialogue(string fileName, string blockId)
    {
        DialogueData dialogueData = DialogueLoader.LoadBlock(fileName, blockId);

        currentDialogueFile = fileName;
        currentDialogueBlockId = blockId;
        isPlayingDialogue = true;

        DebugLog($"��ʼ���ŶԻ�: {fileName}:{blockId}");

        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(dialogueData, fileName, blockId);
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
    /// LLM����Э�� - ʹ�õ�ǰѡ���Provider
    /// </summary>
    private IEnumerator ProcessLLMCoroutine(string characterId, string playerMessage, Action<string> onResponse)
    {
        isProcessingLLM = true;

        // ʹ��HistoryManager����������ʷ��prompt
        string fullPrompt = historyManager.BuildPromptWithHistory(playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        // ʹ�õ�ǰProvider������hardcodedʹ��geminiAPI��
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
    /// �Ի�����ɻص��������д���
    /// </summary>
    public void OnDialogueBlockComplete(string fileName, string blockId)
    {
        DebugLog($"�Ի������: {fileName}:{blockId}");

        currentDialogueFile = null;

        GameFlowController gameFlow = FindObjectOfType<GameFlowController>();
        if (gameFlow != null)
        {
            gameFlow.OnDialogueBlockComplete(blockId);
        }

        // ������
        if (dialogueQueue.Count > 0)
        {
            // �ж��У�������һ��
            var next = dialogueQueue.Dequeue();
            DebugLog($"�Ӷ��в�����һ���Ի�: {next.fileName}:{next.blockId}��ʣ�����: {dialogueQueue.Count}");
            PlayDialogue(next.fileName, next.blockId);
        }
        else
        {
            currentDialogueBlockId = null;  // ���ڲ����
            isPlayingDialogue = false;
            DebugLog("���жԻ��������");
        }
    }

    public bool IsDialogueActive()
    {
        return !string.IsNullOrEmpty(currentDialogueBlockId);
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

    void OnDestroy()
    {
        // ȡ���¼�����
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.OnLLMProviderChanged -= OnProviderChanged;
        }
    }
}