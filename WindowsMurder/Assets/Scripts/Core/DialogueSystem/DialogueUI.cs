using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI组件")]
    public TextMeshProUGUI characterNameText;      // 角色名称
    public TextMeshProUGUI dialogueText;           // 对话文本显示区
    public Image characterPortrait;                // 角色立绘
    public TMP_InputField playerInputField;        // 玩家输入框
    public Button sendButton;                      // 发送按钮
    public GameObject inputPanel;                  // 输入面板（LLM模式时显示）

    [Header("效果设置")]
    public float textSpeed = 0.05f;               // 打字机效果速度
    public bool useTypingEffect = true;           // 是否使用打字机效果

    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip typingSound;                 // 打字机音效

    // 私有变量
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool inLLMMode = false;
    private string currentLLMCharacter;
    private Coroutine typingCoroutine;

    void Start()
    {
        // 绑定按钮事件
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendMessage);

        // 绑定输入框回车事件
        if (playerInputField != null)
            playerInputField.onSubmit.AddListener(delegate { OnSendMessage(); });

        // 初始隐藏输入面板
        SetInputPanelActive(false);
    }

    /// <summary>
    /// 开始播放对话
    /// </summary>
    /// <param name="dialogueData">对话数据</param>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueUI: 对话数据为空");
            return;
        }

        currentDialogue = dialogueData;
        currentLineIndex = 0;
        inLLMMode = false;

        // 清空显示
        ClearDialogue();

        Debug.Log($"DialogueUI: 开始播放对话 {dialogueData.conversationId}");
        ShowNextLine();
    }

    /// <summary>
    /// 显示下一句对话
    /// </summary>
    public void ShowNextLine()
    {
        if (currentDialogue?.lines == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            // 对话结束
            OnDialogueEnd();
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];

        if (line.mode) // 预设文本模式
        {
            ShowPresetLine(line);
        }
        else // LLM模式
        {
            StartLLMMode(line);
        }
    }

    /// <summary>
    /// 显示预设文本
    /// </summary>
    private void ShowPresetLine(DialogueLine line)
    {
        inLLMMode = false;
        SetInputPanelActive(false);

        // 设置角色信息
        SetCharacterInfo(line.characterId, line.portraitId);

        // 显示文本
        if (useTypingEffect && !string.IsNullOrEmpty(line.text))
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(line.text));
        }
        else
        {
            dialogueText.text = line.text ?? "";
        }

        // 自动推进到下一句
        currentLineIndex++;

        // 如果下一句不是LLM模式，延迟一点时间后继续
        if (currentLineIndex < currentDialogue.lines.Count &&
            currentDialogue.lines[currentLineIndex].mode)
        {
            StartCoroutine(DelayedNextLine());
        }
    }

    /// <summary>
    /// 开始LLM对话模式
    /// </summary>
    private void StartLLMMode(DialogueLine line)
    {
        inLLMMode = true;
        currentLLMCharacter = line.characterId;

        // 设置角色信息
        SetCharacterInfo(line.characterId, line.portraitId);

        // 显示提示信息
        dialogueText.text += $"\n<color=yellow>--- 开始与 {GetCharacterDisplayName(line.characterId)} 的对话 ---</color>\n";
        dialogueText.text += "<color=gray>请输入你的问题...</color>\n";

        // 显示输入面板
        SetInputPanelActive(true);

        // 聚焦输入框
        if (playerInputField != null)
        {
            playerInputField.text = "";
            playerInputField.Select();
        }
    }

    /// <summary>
    /// 设置角色信息
    /// </summary>
    private void SetCharacterInfo(string characterId, string portraitId)
    {
        // 设置角色名称
        if (characterNameText != null)
        {
            characterNameText.text = GetCharacterDisplayName(characterId);
        }

        // 设置立绘（如果有的话）
        if (characterPortrait != null && !string.IsNullOrEmpty(portraitId))
        {
            Sprite portrait = Resources.Load<Sprite>($"Portraits/{portraitId}");
            if (portrait != null)
            {
                characterPortrait.sprite = portrait;
                characterPortrait.gameObject.SetActive(true);
            }
            else
            {
                characterPortrait.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 获取角色显示名称
    /// </summary>
    private string GetCharacterDisplayName(string characterId)
    {
        // 这里可以扩展为从配置文件读取
        switch (characterId)
        {
            case "RecycleBin": return "回收站";
            case "TaskManager": return "任务管理器";
            case "ControlPanel": return "控制面板";
            case "MyComputer": return "我的电脑";
            default: return characterId;
        }
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        // 播放打字音效
        if (audioSource != null && typingSound != null)
        {
            audioSource.clip = typingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        foreach (char c in text.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        // 停止音效
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        isTyping = false;
    }

    /// <summary>
    /// 延迟显示下一句
    /// </summary>
    private IEnumerator DelayedNextLine()
    {
        yield return new WaitForSeconds(2f); // 等待2秒

        if (!inLLMMode) // 确保还没进入LLM模式
        {
            ShowNextLine();
        }
    }

    /// <summary>
    /// 玩家发送消息
    /// </summary>
    public void OnSendMessage()
    {
        if (!inLLMMode || playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // 显示玩家消息
        AddPlayerMessage(message);

        // 清空输入框
        playerInputField.text = "";

        // 检查是否结束LLM对话
        DialogueLine currentLine = DialogueLoader.GetLineAt(currentDialogue, currentLineIndex);
        if (currentLine != null && DialogueLoader.ShouldEndLLMDialogue(message, currentLine.endKeywords))
        {
            EndLLMMode();
            return;
        }

        // 发送给DialogueManager处理
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.ProcessLLMMessage(currentLLMCharacter, message, OnLLMResponse);
        }
        else
        {
            // 没有DialogueManager的话，显示默认回复
            OnLLMResponse("系统错误：找不到对话管理器");
        }

        // 重新聚焦输入框
        playerInputField.Select();
    }

    /// <summary>
    /// LLM回复回调
    /// </summary>
    /// <param name="response">AI回复内容</param>
    public void OnLLMResponse(string response)
    {
        if (!inLLMMode) return;

        AddCharacterMessage(currentLLMCharacter, response);
    }

    /// <summary>
    /// 添加玩家消息到对话框
    /// </summary>
    private void AddPlayerMessage(string message)
    {
        dialogueText.text += $"<color=cyan>玩家:</color> {message}\n";
        ScrollToBottom();
    }

    /// <summary>
    /// 添加角色消息到对话框
    /// </summary>
    private void AddCharacterMessage(string characterId, string message)
    {
        string characterName = GetCharacterDisplayName(characterId);
        dialogueText.text += $"<color=orange>{characterName}:</color> {message}\n";
        ScrollToBottom();
    }

    /// <summary>
    /// 结束LLM模式，继续预设对话
    /// </summary>
    private void EndLLMMode()
    {
        inLLMMode = false;
        SetInputPanelActive(false);

        dialogueText.text += $"<color=yellow>--- 与 {GetCharacterDisplayName(currentLLMCharacter)} 的对话结束 ---</color>\n";

        // 推进到下一句
        currentLineIndex++;
        StartCoroutine(DelayedNextLine());
    }

    /// <summary>
    /// 对话结束
    /// </summary>
    private void OnDialogueEnd()
    {
        inLLMMode = false;
        SetInputPanelActive(false);

        dialogueText.text += "\n<color=green>--- 对话结束 ---</color>";

        Debug.Log("DialogueUI: 对话播放完毕");

        // 通知DialogueManager对话结束
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueComplete(currentDialogue.conversationId);
        }
    }

    /// <summary>
    /// 设置输入面板显示状态
    /// </summary>
    private void SetInputPanelActive(bool active)
    {
        if (inputPanel != null)
        {
            inputPanel.SetActive(active);
        }
    }

    /// <summary>
    /// 清空对话显示
    /// </summary>
    private void ClearDialogue()
    {
        if (dialogueText != null)
            dialogueText.text = "";

        if (characterNameText != null)
            characterNameText.text = "";

        if (characterPortrait != null)
            characterPortrait.gameObject.SetActive(false);
    }

    /// <summary>
    /// 滚动到底部
    /// </summary>
    private void ScrollToBottom()
    {
        // 如果有ScrollRect组件，滚动到底部
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// 跳过当前打字效果
    /// </summary>
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;

            // 停止音效
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    /// <summary>
    /// 强制结束当前对话
    /// </summary>
    public void ForceEndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        currentDialogue = null;
        currentLineIndex = 0;
        inLLMMode = false;

        SetInputPanelActive(false);
        ClearDialogue();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器测试功能
    /// </summary>
    [ContextMenu("测试对话")]
    private void TestDialogue()
    {
        DialogueData testData = DialogueLoader.Load("test_dialogue");
        if (testData != null)
        {
            StartDialogue(testData);
        }
    }
#endif
}