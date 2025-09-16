using UnityEngine;
using UnityEngine.UI;

public class DialogueTestController : MonoBehaviour
{
    [Header("�������")]
    public DialogueManager dialogueManager;

    [Header("���԰�ť")]
    public Button testPresetDialogueBtn;     // ����Ԥ��Ի�
    public Button testLLMDialogueBtn;        // ����LLM�Ի�
    public Button testMixedDialogueBtn;      // ���Ի�϶Ի�
    public Button clearHistoryBtn;           // �����ʷ
    public Button stopDialogueBtn;           // ֹͣ�Ի�

    void Start()
    {
        // �󶨰�ť�¼�
        if (testPresetDialogueBtn != null)
            testPresetDialogueBtn.onClick.AddListener(TestPresetDialogue);

        if (testLLMDialogueBtn != null)
            testLLMDialogueBtn.onClick.AddListener(TestLLMDialogue);

        if (testMixedDialogueBtn != null)
            testMixedDialogueBtn.onClick.AddListener(TestMixedDialogue);

        if (clearHistoryBtn != null)
            clearHistoryBtn.onClick.AddListener(ClearAllHistory);

        if (stopDialogueBtn != null)
            stopDialogueBtn.onClick.AddListener(StopCurrentDialogue);

        // ����DialogueManager
        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();
    }

    /// <summary>
    /// ���Դ�Ԥ��Ի�
    /// </summary>
    public void TestPresetDialogue()
    {
        Debug.Log("��ʼ����Ԥ��Ի�");

        // ���������õ�Ԥ��Ի�����
        DialogueData testData = CreatePresetTestDialogue();

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.StartDialogue(testData);
        }
    }

    /// <summary>
    /// ���Դ�LLM�Ի�
    /// </summary>
    public void TestLLMDialogue()
    {
        Debug.Log("��ʼ����LLM�Ի�");

        DialogueData testData = CreateLLMTestDialogue();

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.StartDialogue(testData);
        }
    }

    /// <summary>
    /// ���Ի�϶Ի���Ԥ��+LLM+Ԥ�裩
    /// </summary>
    public void TestMixedDialogue()
    {
        Debug.Log("��ʼ���Ի�϶Ի�");

        DialogueData testData = CreateMixedTestDialogue();

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.StartDialogue(testData);
        }
    }

    /// <summary>
    /// ����Ԥ��Ի���������
    /// </summary>
    private DialogueData CreatePresetTestDialogue()
    {
        DialogueData data = new DialogueData();
        data.conversationId = "preset_test";
        data.lines = new System.Collections.Generic.List<DialogueLine>();

        // ��һ��
        DialogueLine line1 = new DialogueLine();
        line1.id = "preset_01";
        line1.mode = true;  // Ԥ��ģʽ
        line1.characterId = "TestCharacter";
        line1.text = "first";
        line1.portraitId = "";
        data.lines.Add(line1);

        // �ڶ���
        DialogueLine line2 = new DialogueLine();
        line2.id = "preset_02";
        line2.mode = true;
        line2.characterId = "TestCharacter";
        line2.text = "second��";
        line2.portraitId = "";
        data.lines.Add(line2);

        // ������
        DialogueLine line3 = new DialogueLine();
        line3.id = "preset_03";
        line3.mode = true;
        line3.characterId = "TestCharacter";
        line3.text = "done��";
        line3.portraitId = "";
        data.lines.Add(line3);

        return data;
    }

    /// <summary>
    /// ����LLM�Ի���������
    /// </summary>
    private DialogueData CreateLLMTestDialogue()
    {
        DialogueData data = new DialogueData();
        data.conversationId = "llm_test";
        data.lines = new System.Collections.Generic.List<DialogueLine>();

        // LLM�Ի�����
        DialogueLine llmLine = new DialogueLine();
        llmLine.id = "llm_01";
        llmLine.mode = false;  // LLMģʽ
        llmLine.characterId = "RecycleBin";  // ʹ�û���վ��ɫ
        llmLine.text = "";  // LLMģʽ����Ҫtext
        llmLine.portraitId = "";
        llmLine.endKeywords = new System.Collections.Generic.List<string>
        {
            "����", "�ټ�", "END", "end", "лл", "�������"
        };
        data.lines.Add(llmLine);

        return data;
    }

    /// <summary>
    /// ������϶Ի���������
    /// </summary>
    private DialogueData CreateMixedTestDialogue()
    {
        DialogueData data = new DialogueData();
        data.conversationId = "mixed_test";
        data.lines = new System.Collections.Generic.List<DialogueLine>();

        // ����Ԥ��Ի�
        DialogueLine intro = new DialogueLine();
        intro.id = "mixed_intro";
        intro.mode = true;
        intro.characterId = "RecycleBin";
        intro.text = "Hello��operator��I'm recycle bin ��let's talk��";
        intro.portraitId = "";
        data.lines.Add(intro);

        // LLM��������
        DialogueLine llmChat = new DialogueLine();
        llmChat.id = "mixed_llm";
        llmChat.mode = false;
        llmChat.characterId = "RecycleBin";
        llmChat.text = "";
        llmChat.portraitId = "";
        llmChat.endKeywords = new System.Collections.Generic.List<string>
        {
            "����", "�ټ�", "END", "end", "лл"
        };
        data.lines.Add(llmChat);

        // ��βԤ��Ի�
        DialogueLine outro = new DialogueLine();
        outro.id = "mixed_outro";
        outro.mode = true;
        outro.characterId = "RecycleBin";
        outro.text = "Done��ѯ������ϣ�����ṩ����Ϣ�����а�����";
        outro.portraitId = "";
        data.lines.Add(outro);

        return data;
    }

    /// <summary>
    /// ������н�ɫ��ʷ
    /// </summary>
    public void ClearAllHistory()
    {
        Debug.Log("������жԻ���ʷ");

        if (dialogueManager != null)
        {
            dialogueManager.ClearCharacterHistory("RecycleBin");
            dialogueManager.ClearCharacterHistory("TaskManager");
            dialogueManager.ClearCharacterHistory("TestCharacter");
        }
    }

    /// <summary>
    /// ֹͣ��ǰ�Ի�
    /// </summary>
    public void StopCurrentDialogue()
    {
        Debug.Log("ǿ��ֹͣ��ǰ�Ի�");

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.ForceEndDialogue();
        }
    }
}