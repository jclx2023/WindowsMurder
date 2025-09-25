using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject dialoguePanel;               // 整个对话框面板
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
    private bool waitingForPlayerInput = false;
    private bool isProcessingLLM = false;
    private bool waitingForContinue = false;       // 是否等待玩家点击继续

    private string currentLLMCharacter;
    private Coroutine typingCoroutine;
    private string fullCurrentText = "";           // 当前完整文本缓存

    void Start()
    {
        HideDialoguePanel();

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendMessage);

        if (playerInputField != null)
            playerInputField.onSubmit.AddListener(delegate { OnSendMessage(); });

        if (dialogueText != null)
        {
            Button dialogueClickButton = dialogueText.gameObject.GetComponent<Button>();
            if (dialogueClickButton == null)
            {
                dialogueClickButton = dialogueText.gameObject.AddComponent<Button>();
                dialogueClickButton.transition = Selectable.Transition.None;
            }
            dialogueClickButton.onClick.AddListener(OnDialogueTextClicked);
        }
    }

    #region 对话框显示控制

    public void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }

    public void HideDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public bool IsDialoguePanelVisible()
    {
        return dialoguePanel != null && dialoguePanel.activeSelf;
    }

    #endregion

    private enum UIState
    {
        ShowingText,    // 显示文本（预设对话或AI回复）
        WaitingInput,   // 等待玩家输入
        ProcessingAI    // 处理AI请求中
    }

    private void SetUIState(UIState state)
    {
        switch (state)
        {
            case UIState.ShowingText:
                SetTextActive(true);
                SetInputActive(false);
                waitingForPlayerInput = false;
                break;

            case UIState.WaitingInput:
                SetTextActive(false);
                SetInputActive(true);
                waitingForPlayerInput = true;

                if (playerInputField != null)
                {
                    playerInputField.text = "";
                    playerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = waitingForInputHint;
                    playerInputField.Select();
                }
                break;

            case UIState.ProcessingAI:
                SetTextActive(true);
                SetInputActive(false);
                waitingForPlayerInput = false;

                if (dialogueText != null)
                    dialogueText.text = waitingForAIHint;
                break;
        }
    }

    private void SetTextActive(bool active)
    {
        if (dialogueText != null)
            dialogueText.gameObject.SetActive(active);
    }

    private void SetInputActive(bool active)
    {
        if (playerInputField != null)
            playerInputField.gameObject.SetActive(active);
        if (sendButton != null)
            sendButton.gameObject.SetActive(active);
    }

    #region 对话流程

    public void StartDialogue(DialogueData dialogueData, string fileName, string blockId)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueUI: 对话数据为空");
            return;
        }

        ShowDialoguePanel();

        currentDialogue = dialogueData;
        currentDialogueFileName = fileName;
        currentDialogueBlockId = blockId;
        currentLineIndex = 0;
        inLLMMode = false;

        ClearDialogue();
        ShowNextLine();
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        StartDialogue(dialogueData, "unknown", dialogueData?.conversationId ?? "unknown");
    }

    public void ShowNextLine()
    {
        if (currentDialogue?.lines == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            OnDialogueEnd();
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];

        if (line.mode) // 预设文本
            ShowPresetLine(line);
        else
            StartLLMMode(line);
    }

    private void ShowPresetLine(DialogueLine line)
    {
        inLLMMode = false;
        SetUIState(UIState.ShowingText);

        SetCharacterInfo(line.characterId, line.portraitId);

        fullCurrentText = line.text ?? "";

        if (useTypingEffect && !string.IsNullOrEmpty(fullCurrentText))
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(fullCurrentText));
        }
        else
        {
            dialogueText.text = fullCurrentText;
            waitingForContinue = true;
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        waitingForContinue = false;
        dialogueText.text = "";

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

        if (audioSource != null)
            audioSource.Stop();

        isTyping = false;
        waitingForContinue = true;
    }

    private void OnDialogueTextClicked()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (inLLMMode)
        {
            SetUIState(UIState.WaitingInput);
            return;
        }

        // 普通对话模式：点击继续到下一行
        if (waitingForContinue)
        {
            waitingForContinue = false;
            currentLineIndex++;
            ShowNextLine();
        }
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (audioSource != null)
            audioSource.Stop();

        dialogueText.text = fullCurrentText;
        isTyping = false;
        waitingForContinue = true;
    }

    private void SetCharacterInfo(string characterId, string portraitId)
    {
        if (characterNameText != null)
            characterNameText.text = GetCharacterDisplayName(characterId);

        if (characterPortrait != null)
        {
            if (!string.IsNullOrEmpty(portraitId))
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
            else
            {
                characterPortrait.gameObject.SetActive(false);
            }
        }
    }

    private string GetCharacterDisplayName(string characterId)
    {
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

    private void OnDialogueEnd()
    {
        inLLMMode = false;
        isProcessingLLM = false;

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
            dialogueManager.OnDialogueBlockComplete(currentDialogueFileName, currentDialogueBlockId);

        currentDialogue = null;
        currentDialogueFileName = null;
        currentDialogueBlockId = null;

        HideDialoguePanel();
    }

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

    #endregion

    #region LLM模式（保留原逻辑）

    private void StartLLMMode(DialogueLine line)
    {
        inLLMMode = true;
        currentLLMCharacter = line.characterId;

        SetCharacterInfo(line.characterId, line.portraitId);

        // 使用HistoryManager开始LLM会话，直接发送初始prompt
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.historyManager != null)
        {
            SetUIState(UIState.ProcessingAI); // 显示"思考中..."

            dialogueManager.historyManager.StartLLMSession(
                line.text,                    // 初始prompt
                OnInitialLLMResponse,         // 处理初始回复的回调
                dialogueManager               // 传递DialogueManager引用
            );
        }
    }
    private void OnInitialLLMResponse(string response)
    {
        OnLLMResponse(response); // 复用现有的回复处理逻辑
    }

    public void OnSendMessage()
    {
        if (!inLLMMode || !waitingForPlayerInput || playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        SetUIState(UIState.ProcessingAI);
        isProcessingLLM = true;

        DialogueLine currentLine = DialogueLoader.GetLineAt(currentDialogue, currentLineIndex);
        if (currentLine != null && DialogueLoader.ShouldEndLLMDialogue(message, currentLine.endKeywords))
        {
            EndLLMMode();
            return;
        }

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        dialogueManager.ProcessLLMMessage(currentLLMCharacter, message, OnLLMResponse);
    }

    public void OnLLMResponse(string response)
    {
        if (!inLLMMode) return;

        isProcessingLLM = false;
        SetUIState(UIState.ShowingText);

        fullCurrentText = response;

        if (useTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeText(fullCurrentText));
        }
        else
        {
            dialogueText.text = fullCurrentText;
            waitingForContinue = true; // 设置等待点击状态
        }
    }

    private void EndLLMMode()
    {
        // 结束历史管理器的会话
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.historyManager != null)
        {
            dialogueManager.historyManager.EndLLMSession();
        }

        inLLMMode = false;
        isProcessingLLM = false;
        currentLineIndex++;
        ShowNextLine();
    }

    #endregion
}
