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

    [Header("效果设置")]
    public float textSpeed = 0.05f;               // 打字机效果速度
    public bool useTypingEffect = true;           // 是否使用打字机效果

    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip typingSound;                 // 打字机音效

    [Header("LLM交互提示")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    // 私有变量
    private DialogueData currentDialogue;
    private string currentDialogueFileName;        // 当前对话的文件名
    private string currentDialogueBlockId;         // 当前对话的块ID
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool inLLMMode = false;
    private bool waitingForPlayerInput = false;  // 是否等待玩家输入
    private bool isProcessingLLM = false;
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

        // 绑定对话框点击事件（用于LLM模式下重新激活输入）
        if (dialogueText != null)
        {
            // 添加一个按钮组件来检测点击
            Button dialogueClickButton = dialogueText.gameObject.GetComponent<Button>();
            if (dialogueClickButton == null)
            {
                dialogueClickButton = dialogueText.gameObject.AddComponent<Button>();
                dialogueClickButton.transition = Selectable.Transition.None; // 不显示视觉反馈
            }
            dialogueClickButton.onClick.AddListener(OnDialogueTextClicked);
        }

        // 初始状态：显示文本，隐藏输入
        SetUIState(UIState.ShowingText);
    }

    /// <summary>
    /// UI状态枚举
    /// </summary>
    private enum UIState
    {
        ShowingText,    // 显示文本（预设对话或AI回复）
        WaitingInput,   // 等待玩家输入
        ProcessingAI    // 处理AI请求中
    }

    /// <summary>
    /// 设置UI状态
    /// </summary>
    private void SetUIState(UIState state)
    {
        switch (state)
        {
            case UIState.ShowingText:
                // 显示文本，隐藏输入
                SetTextActive(true);
                SetInputActive(false);
                waitingForPlayerInput = false;
                break;

            case UIState.WaitingInput:
                // 隐藏文本，显示输入
                SetTextActive(false);
                SetInputActive(true);
                waitingForPlayerInput = true;

                // 聚焦输入框
                if (playerInputField != null)
                {
                    playerInputField.text = "";
                    playerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = waitingForInputHint;
                    playerInputField.Select();
                }
                break;

            case UIState.ProcessingAI:
                // 显示处理提示，禁用输入
                SetTextActive(true);
                SetInputActive(false);
                waitingForPlayerInput = false;

                if (dialogueText != null)
                {
                    dialogueText.text = waitingForAIHint;
                }
                break;
        }
    }

    /// <summary>
    /// 设置文本显示组件状态
    /// </summary>
    private void SetTextActive(bool active)
    {
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 设置输入组件状态
    /// </summary>
    private void SetInputActive(bool active)
    {
        if (playerInputField != null)
        {
            playerInputField.gameObject.SetActive(active);
        }
        if (sendButton != null)
        {
            sendButton.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// 开始播放对话（新版本 - 接收文件名和块ID）
    /// </summary>
    public void StartDialogue(DialogueData dialogueData, string fileName, string blockId)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueUI: 对话数据为空");
            return;
        }

        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueUI: 文件名或块ID为空 (fileName: {fileName}, blockId: {blockId})");
            return;
        }

        currentDialogue = dialogueData;
        currentDialogueFileName = fileName;
        currentDialogueBlockId = blockId;
        currentLineIndex = 0;
        inLLMMode = false;

        // 清空显示
        ClearDialogue();

        Debug.Log($"DialogueUI: 开始播放对话 {fileName}:{blockId}");
        ShowNextLine();
    }

    /// <summary>
    /// 开始播放对话（兼容旧版本的重载方法）
    /// </summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        // 如果没有提供文件名和块ID，使用默认值
        StartDialogue(dialogueData, "unknown", dialogueData?.conversationId ?? "unknown");
        Debug.LogWarning("DialogueUI: 使用了旧版本的StartDialogue方法，建议传递fileName和blockId参数");
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
        SetUIState(UIState.ShowingText);

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

        // 如果下一句还是预设模式，延迟后继续
        if (currentLineIndex < currentDialogue.lines.Count &&
            currentDialogue.lines[currentLineIndex].mode)
        {
            StartCoroutine(DelayedNextLine());
        }
        else if (currentLineIndex < currentDialogue.lines.Count)
        {
            // 下一句是LLM模式，等待当前文本显示完成后继续
            StartCoroutine(WaitForTypingThenContinue());
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

        // 切换到等待输入状态
        SetUIState(UIState.WaitingInput);

        Debug.Log($"进入LLM模式，与 {GetCharacterDisplayName(line.characterId)} 对话");
    }

    /// <summary>
    /// 对话文本被点击（用于LLM模式下重新激活输入）
    /// </summary>
    private void OnDialogueTextClicked()
    {
        if (inLLMMode && !waitingForPlayerInput && !isProcessingLLM)
        {
            // 重新激活输入状态
            SetUIState(UIState.WaitingInput);
            Debug.Log("重新激活输入模式");
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
            case "me": return "我";
            case "guardian": return "系统守护";
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
    /// 等待打字效果完成后继续
    /// </summary>
    private IEnumerator WaitForTypingThenContinue()
    {
        while (isTyping)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1f); // 额外等待1秒
        ShowNextLine();
    }

    /// <summary>
    /// 延迟显示下一句
    /// </summary>
    private IEnumerator DelayedNextLine()
    {
        while (isTyping)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2f); // 等待2秒
        ShowNextLine();
    }

    /// <summary>
    /// 玩家发送消息
    /// </summary>
    public void OnSendMessage()
    {
        if (!inLLMMode || !waitingForPlayerInput || playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        Debug.Log($"玩家发送消息: {message}");

        // 切换到处理AI状态
        SetUIState(UIState.ProcessingAI);
        isProcessingLLM = true;

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
    }

    /// <summary>
    /// LLM回复回调
    /// </summary>
    public void OnLLMResponse(string response)
    {
        if (!inLLMMode) return;

        isProcessingLLM = false;

        // 显示AI回复
        SetUIState(UIState.ShowingText);

        if (useTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeTextThenWaitForClick(response));
        }
        else
        {
            dialogueText.text = response;
        }

        Debug.Log($"AI回复: {response}");
    }

    /// <summary>
    /// 显示文本后等待点击
    /// </summary>
    private IEnumerator TypeTextThenWaitForClick(string text)
    {
        yield return StartCoroutine(TypeText(text));
        // 打字完成后，等待玩家点击对话框来重新激活输入
    }

    /// <summary>
    /// 结束LLM模式，继续预设对话
    /// </summary>
    private void EndLLMMode()
    {
        inLLMMode = false;
        isProcessingLLM = false;

        // 推进到下一句
        currentLineIndex++;
        ShowNextLine();
    }

    /// <summary>
    /// 对话结束
    /// </summary>
    private void OnDialogueEnd()
    {
        inLLMMode = false;
        isProcessingLLM = false;
        SetUIState(UIState.ShowingText);

        Debug.Log("DialogueUI: 对话播放完毕");

        // 通知DialogueManager对话结束
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueBlockComplete(currentDialogueFileName, currentDialogueBlockId);
        }

        // 清理状态
        currentDialogueFileName = null;
        currentDialogueBlockId = null;
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

        if (playerInputField != null)
            playerInputField.text = "";
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
        currentDialogueFileName = null;
        currentDialogueBlockId = null;
        currentLineIndex = 0;
        inLLMMode = false;
        isProcessingLLM = false;

        SetUIState(UIState.ShowingText);
        ClearDialogue();
    }
}