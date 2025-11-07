using UnityEngine;
using UnityEngine.UI;

public class DialogueTestController : MonoBehaviour
{
    [Header("组件引用")]
    public DialogueManager dialogueManager;

    [Header("测试按钮")]
    public Button testPresetDialogueBtn;     // 测试预设对话
    public Button testLLMDialogueBtn;        // 测试LLM对话
    public Button testMixedDialogueBtn;      // 测试混合对话
    public Button clearHistoryBtn;           // 清除历史
    public Button stopDialogueBtn;           // 停止对话

    void Start()
    {
        // 绑定按钮事件
        if (testPresetDialogueBtn != null)
            testPresetDialogueBtn.onClick.AddListener(TestPresetDialogue);

        if (testLLMDialogueBtn != null)
            testLLMDialogueBtn.onClick.AddListener(TestLLMDialogue);

        if (testMixedDialogueBtn != null)
            testMixedDialogueBtn.onClick.AddListener(TestMixedDialogue);

        if (clearHistoryBtn != null)
            clearHistoryBtn.onClick.AddListener(ClearAllHistory);

        // 查找DialogueManager
        if (dialogueManager == null)
            dialogueManager = FindObjectOfType<DialogueManager>();
    }

    /// <summary>
    /// 测试纯预设对话
    /// </summary>
    public void TestPresetDialogue()
    {
        Debug.Log("开始测试预设对话");

        // 创建测试用的预设对话数据
        DialogueData testData = CreatePresetTestDialogue();

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.StartDialogue(testData);
        }
    }

    /// <summary>
    /// 测试纯LLM对话
    /// </summary>
    public void TestLLMDialogue()
    {
        Debug.Log("开始测试LLM对话");

        DialogueData testData = CreateLLMTestDialogue();

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.StartDialogue(testData);
        }
    }

    /// <summary>
    /// 测试混合对话（预设+LLM+预设）
    /// </summary>
    public void TestMixedDialogue()
    {
        Debug.Log("开始测试混合对话");

        DialogueData testData = CreateMixedTestDialogue();

        if (dialogueManager != null && dialogueManager.dialogueUI != null)
        {
            dialogueManager.dialogueUI.StartDialogue(testData);
        }
    }

    /// <summary>
    /// 创建预设对话测试数据
    /// </summary>
    private DialogueData CreatePresetTestDialogue()
    {
        DialogueData data = new DialogueData();
        data.conversationId = "preset_test";
        data.lines = new System.Collections.Generic.List<DialogueLine>();

        // 第一句
        DialogueLine line1 = new DialogueLine();
        line1.id = "preset_01";
        line1.mode = true;  // 预设模式
        line1.characterId = "TestCharacter";
        line1.text = "first";
        line1.portraitId = "";
        data.lines.Add(line1);

        // 第二句
        DialogueLine line2 = new DialogueLine();
        line2.id = "preset_02";
        line2.mode = true;
        line2.characterId = "TestCharacter";
        line2.text = "second。";
        line2.portraitId = "";
        data.lines.Add(line2);

        // 第三句
        DialogueLine line3 = new DialogueLine();
        line3.id = "preset_03";
        line3.mode = true;
        line3.characterId = "TestCharacter";
        line3.text = "done！";
        line3.portraitId = "";
        data.lines.Add(line3);

        return data;
    }

    /// <summary>
    /// 创建LLM对话测试数据
    /// </summary>
    private DialogueData CreateLLMTestDialogue()
    {
        DialogueData data = new DialogueData();
        data.conversationId = "llm_test";
        data.lines = new System.Collections.Generic.List<DialogueLine>();

        // LLM对话句子
        DialogueLine llmLine = new DialogueLine();
        llmLine.id = "llm_01";
        llmLine.mode = false;  // LLM模式
        llmLine.characterId = "RecycleBin";  // 使用回收站角色
        llmLine.text = "";  // LLM模式不需要text
        llmLine.portraitId = "";
        llmLine.endKeywords = new System.Collections.Generic.List<string>
        {
            "结束", "再见", "END", "end", "谢谢", "测试完成"
        };
        data.lines.Add(llmLine);

        return data;
    }

    /// <summary>
    /// 创建混合对话测试数据
    /// </summary>
    private DialogueData CreateMixedTestDialogue()
    {
        DialogueData data = new DialogueData();
        data.conversationId = "mixed_test";
        data.lines = new System.Collections.Generic.List<DialogueLine>();

        // 开场预设对话
        DialogueLine intro = new DialogueLine();
        intro.id = "mixed_intro";
        intro.mode = true;
        intro.characterId = "RecycleBin";
        intro.text = "Hello，operator。I'm recycle bin ，let's talk。";
        intro.portraitId = "";
        data.lines.Add(intro);

        // LLM交互环节
        DialogueLine llmChat = new DialogueLine();
        llmChat.id = "mixed_llm";
        llmChat.mode = false;
        llmChat.characterId = "RecycleBin";
        llmChat.text = "";
        llmChat.portraitId = "";
        llmChat.endKeywords = new System.Collections.Generic.List<string>
        {
            "结束", "再见", "END", "end", "谢谢"
        };
        data.lines.Add(llmChat);

        // 结尾预设对话
        DialogueLine outro = new DialogueLine();
        outro.id = "mixed_outro";
        outro.mode = true;
        outro.characterId = "RecycleBin";
        outro.text = "Done问询结束。希望我提供的信息对你有帮助。";
        outro.portraitId = "";
        data.lines.Add(outro);

        return data;
    }

    /// <summary>
    /// 清除所有角色历史
    /// </summary>
    public void ClearAllHistory()
    {
        Debug.Log("清除所有对话历史");

        if (dialogueManager != null)
        {
            dialogueManager.ClearCharacterHistory("RecycleBin");
            dialogueManager.ClearCharacterHistory("TaskManager");
            dialogueManager.ClearCharacterHistory("TestCharacter");
        }
    }
}
