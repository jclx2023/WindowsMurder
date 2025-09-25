using UnityEngine;
using System;

/// <summary>
/// �Ի���ʷ������ - ר�Ŵ���LLM���ֶԻ��������Ĺ���
/// </summary>
public class ConversationHistoryManager : MonoBehaviour
{
    [Header("����")]
    public bool enableDebugLog = true;

    // ˽�б���
    private string currentLLMHistory = "";     // ��ǰLLM�Ự�������Ի���ʷ
    private bool isLLMActive = false;          // �Ƿ����ڽ���LLM�Ի�
    private string lastPlayerInput = "";       // �������һ���������

    /// <summary>
    /// ��ʼ�µ�LLM�Ự�����ͳ�ʼprompt
    /// </summary>
    /// <param name="initialPrompt">��ʼprompt������JSON�е�text�ֶΣ�</param>
    /// <param name="onResponse">AI�ظ��Ļص�</param>
    /// <param name="dialogueManager">�Ի�����������</param>
    public void StartLLMSession(string initialPrompt, Action<string> onResponse, DialogueManager dialogueManager)
    {
        if (string.IsNullOrEmpty(initialPrompt))
        {
            DebugLog("����: ��ʼpromptΪ��");
            onResponse?.Invoke("ϵͳ����: ��ʼpromptΪ��");
            return;
        }

        // ��ʼ���Ự״̬
        currentLLMHistory = initialPrompt;
        isLLMActive = true;
        lastPlayerInput = "";

        DebugLog($"��ʼLLM�Ự����ʼprompt: {initialPrompt.Substring(0, Math.Min(50, initialPrompt.Length))}...");

        // ֱ�ӷ��ͳ�ʼprompt��LLM
        StartCoroutine(SendInitialPrompt(initialPrompt, onResponse, dialogueManager));
    }

    /// <summary>
    /// ���ͳ�ʼprompt��Э��
    /// </summary>
    private System.Collections.IEnumerator SendInitialPrompt(string initialPrompt, Action<string> onResponse, DialogueManager dialogueManager)
    {
        bool responseReceived = false;
        string aiResponse = "";

        // ����GeminiAPI
        yield return StartCoroutine(dialogueManager.geminiAPI.GenerateText(
            initialPrompt,
            response =>
            {
                aiResponse = dialogueManager.CleanAIResponsePublic(response);
                responseReceived = true;
            },
            error =>
            {
                aiResponse = "ϵͳ����...�޷�����...";
                responseReceived = true;
                DebugLog($"��ʼprompt����ʧ��: {error}");
            }
        ));

        // �ȴ���Ӧ
        while (!responseReceived)
        {
            yield return null;
        }

        // ��AI�ظ���ӵ���ʷ
        AddLLMResponse(aiResponse);

        // ���ؽ��
        onResponse?.Invoke(aiResponse);

        DebugLog($"��ʼLLM��Ӧ���: {aiResponse.Substring(0, Math.Min(30, aiResponse.Length))}...");
    }

    /// <summary>
    /// ���LLM�ظ�����ʷ��¼
    /// </summary>
    /// <param name="aiResponse">AI�ظ�����</param>
    public void AddLLMResponse(string aiResponse)
    {
        if (!isLLMActive || string.IsNullOrEmpty(aiResponse))
        {
            DebugLog("����: LLMδ�����ظ�Ϊ�գ�������ӻظ�");
            return;
        }

        // �����������룬�����������������AI�ظ�
        if (!string.IsNullOrEmpty(lastPlayerInput))
        {
            currentLLMHistory += $"\n\n���: {lastPlayerInput}";
            lastPlayerInput = ""; // ��ջ���
        }

        currentLLMHistory += $"\nAI: {aiResponse}";

        DebugLog($"���LLM�ظ�����ʷ����ǰ��ʷ����: {currentLLMHistory.Length}");
    }

    /// <summary>
    /// ����������ʷ������prompt�������������������
    /// </summary>
    /// <param name="playerInput">�������</param>
    /// <returns>���������Ի���ʷ��prompt</returns>
    public string BuildPromptWithHistory(string playerInput)
    {
        if (!isLLMActive)
        {
            DebugLog("����: LLMδ�������ԭʼ����");
            return playerInput;
        }

        if (string.IsNullOrEmpty(playerInput))
        {
            DebugLog("����: �������Ϊ��");
            return currentLLMHistory;
        }

        // ����������룬���յ�AI�ظ�ʱ���õ�
        lastPlayerInput = playerInput;

        // ��������prompt
        string fullPrompt = $"{currentLLMHistory}\n\n���: {playerInput}\n\nAI:";

        DebugLog($"����������ʷ��prompt���ܳ���: {fullPrompt.Length}");
        DebugLog($"��ǰpromptԤ��: {fullPrompt.Substring(0, Math.Min(100, fullPrompt.Length))}...");

        return fullPrompt;
    }

    /// <summary>
    /// ������ǰLLM�Ự
    /// </summary>
    public void EndLLMSession()
    {
        if (!isLLMActive)
        {
            DebugLog("LLM�Ựδ����������");
            return;
        }

        DebugLog($"����LLM�Ự����ʷ��¼����: {currentLLMHistory.Length}");

        // ����Ự����
        currentLLMHistory = "";
        lastPlayerInput = "";
        isLLMActive = false;
    }

    /// <summary>
    /// ������־
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[ConversationHistoryManager] {message}");
        }
    }
}