using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("�������")]
    public DialogueUI dialogueUI;
    public GeminiAPI geminiAPI;

    [Header("��ɫ����")]
    public CharacterDataSO[] characterDatabase; // ��ɫ��������

    [Header("����")]
    public bool enableDebugLog = true;

    // ˽�б���
    private Dictionary<string, CharacterData> characters;
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
        characters = new Dictionary<string, CharacterData>();
        conversationHistory = new Dictionary<string, List<string>>();

        // ���ؽ�ɫ����
        LoadCharacterData();

        // �����������
        if (dialogueUI == null)
            dialogueUI = FindObjectOfType<DialogueUI>();
        if (geminiAPI == null)
            geminiAPI = FindObjectOfType<GeminiAPI>();

        DebugLog("DialogueManager ��ʼ�����");
    }

    /// <summary>
    /// ���ؽ�ɫ����
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

                // ��ʼ���Ի���ʷ
                conversationHistory[characterSO.characterId] = new List<string>();
            }
        }

        DebugLog($"������ {characters.Count} ����ɫ����");
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

        // ��¼�����Ϣ����ʷ
        AddToHistory(characterId, $"���: {playerMessage}");

        // ����AI��ʾ��
        string prompt = BuildAIPrompt(characterId, playerMessage);

        bool responseReceived = false;
        string aiResponse = "";

        // ����GeminiAPI
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

        // �ȴ���Ӧ
        while (!responseReceived)
        {
            yield return null;
        }

        // ��¼AI�ظ�����ʷ
        AddToHistory(characterId, $"{GetCharacterDisplayName(characterId)}: {aiResponse}");

        // ���ؽ��
        onResponse?.Invoke(aiResponse);

        isProcessingLLM = false;
        DebugLog($"LLM��Ӧ���: {characterId} -> {aiResponse.Substring(0, Mathf.Min(50, aiResponse.Length))}...");
    }

    /// <summary>
    /// ����AI��ʾ��
    /// </summary>
    private string BuildAIPrompt(string characterId, string playerMessage)
    {
        CharacterData character = GetCharacterData(characterId);
        if (character == null)
        {
            return $"��ظ���ҵ����⣺{playerMessage}";
        }

        string prompt = $@"�����ڰ���Windows XPϵͳ�е�{character.displayName}����

��ɫ�趨��
{character.personality}

��֪������ʵ��";

        foreach (string fact in character.knownFacts)
        {
            prompt += $"\n- {fact}";
        }

        prompt += "\n\n�����ص����ܣ�";
        foreach (string secret in character.secrets)
        {
            prompt += $"\n- {secret}";
        }

        // �������ĶԻ���ʷ
        List<string> history = conversationHistory[characterId];
        if (history.Count > 0)
        {
            prompt += "\n\n����ĶԻ���";
            int startIndex = Mathf.Max(0, history.Count - 6); // ֻ�������3�ֶԻ�
            for (int i = startIndex; i < history.Count; i++)
            {
                prompt += $"\n{history[i]}";
            }
        }

        prompt += $@"

��ǰ������⣺{playerMessage}

����{character.displayName}����ݺ��Ը�ظ���ע�⣺
1. ���ֽ�ɫ�Ը񣬼��ش𣨲�����100�֣�
2. ��Ҫ����͸¶�������ܣ�Ҫ������������ش�
3. ���Ա��ֳ��������еı�﷽ʽ
4. ����ʵ�������Ϣ�����Ա��ֳ����Ż�ر�";

        return prompt;
    }

    /// <summary>
    /// ����AI��Ӧ
    /// </summary>
    private string ProcessAIResponse(string rawResponse)
    {
        if (string.IsNullOrEmpty(rawResponse))
        {
            return "...����Ӧ...";
        }

        // ������Ӧ�ı�
        string cleanResponse = rawResponse.Trim();

        // �Ƴ����ܵ�����
        if (cleanResponse.StartsWith("\"") && cleanResponse.EndsWith("\""))
        {
            cleanResponse = cleanResponse.Substring(1, cleanResponse.Length - 2);
        }

        // ��������
        if (cleanResponse.Length > 200)
        {
            cleanResponse = cleanResponse.Substring(0, 197) + "...";
        }

        return cleanResponse;
    }

    /// <summary>
    /// ����AI����
    /// </summary>
    private string HandleAIError(string characterId, string error)
    {
        DebugLog($"AI����ʧ��: {error}");

        // ���ݽ�ɫ���ز�ͬ�Ĵ�����Ϣ
        switch (characterId)
        {
            case "RecycleBin":
                return "ϵͳ����...�ҵ������ƺ�����...";
            case "TaskManager":
                return "���ӳ�ʱ...��������״̬...";
            default:
                return "ϵͳ����...���Ժ�����...";
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

        // ���������ﴥ����Ϸ�¼������������������
        // GameEvents.OnDialogueCompleted?.Invoke(dialogueId);
    }

    /// <summary>
    /// ��ȡ��ɫ����
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
    /// ��ȡ��ɫ��ʾ����
    /// </summary>
    private string GetCharacterDisplayName(string characterId)
    {
        CharacterData character = GetCharacterData(characterId);
        return character?.displayName ?? characterId;
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

        // ������ʷ����
        if (conversationHistory[characterId].Count > 20)
        {
            conversationHistory[characterId].RemoveAt(0);
        }
    }

    /// <summary>
    /// ��ȡ��ɫ�Ի���ʷ
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
    /// ���ָ����ɫ�ĶԻ���ʷ
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
    /// ���½�ɫ���ݣ����緢����������
    /// </summary>
    public void UpdateCharacterFacts(string characterId, string newFact)
    {
        CharacterData character = GetCharacterData(characterId);
        if (character != null && !character.knownFacts.Contains(newFact))
        {
            character.knownFacts.Add(newFact);
            DebugLog($"Ϊ {characterId} �������ʵ: {newFact}");
        }
    }

    /// <summary>
    /// ����Ƿ����ڽ��жԻ�
    /// </summary>
    public bool IsDialogueActive()
    {
        return !string.IsNullOrEmpty(currentDialogueId);
    }

    /// <summary>
    /// ǿ�ƽ�����ǰ�Ի�
    /// </summary>
    public void ForceEndCurrentDialogue()
    {
        if (IsDialogueActive())
        {
            DebugLog($"ǿ�ƽ����Ի�: {currentDialogueId}");
            currentDialogueId = null;

            if (dialogueUI != null)
            {
                dialogueUI.ForceEndDialogue();
            }
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
    /// <summary>
    /// �༭�����Թ���
    /// </summary>
    [ContextMenu("���ԶԻ�ϵͳ")]
    private void TestDialogueSystem()
    {
        StartDialogue("test_dialogue");
    }

    [ContextMenu("��ӡ���н�ɫ��ʷ")]
    private void PrintAllHistory()
    {
        foreach (var kvp in conversationHistory)
        {
            Debug.Log($"=== {kvp.Key} �ĶԻ���ʷ ===");
            foreach (string line in kvp.Value)
            {
                Debug.Log(line);
            }
        }
    }
#endif
}

/// <summary>
/// ��ɫ���ݽṹ
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
/// ��ɫ����ScriptableObject (��Ҫ���������ļ�)
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "WindowsMurder/Character Data")]
public class CharacterDataSO : ScriptableObject
{
    [Header("������Ϣ")]
    public string characterId = "NewCharacter";
    public string displayName = "�½�ɫ";

    [Header("AI�趨")]
    [TextArea(3, 8)]
    public string personality = "����д��ɫ�Ը�����...";

    [Header("��֪��ʵ")]
    public List<string> knownFacts = new List<string>();

    [Header("��������")]
    public List<string> secrets = new List<string>();
}