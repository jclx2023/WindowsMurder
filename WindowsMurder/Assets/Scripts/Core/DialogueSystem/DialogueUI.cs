using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image characterPortrait;
    public TMP_InputField playerInputField;
    public Button sendButton;

    [Header("效果设置")]
    public float textSpeed = 0.05f;
    public bool useTypingEffect = true;

    [Header("音效")]
    public AudioSource audioSource;
    public AudioClip typingSound;

    [Header("LLM交互提示")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    // 事件系统
    public static event System.Action<string, string, bool> OnLineStarted;
    public static event System.Action<string, string, bool> OnLineCompleted;
    public static event System.Action<string, string> OnDialogueBlockStarted;
    public static event System.Action<string, string> OnDialogueBlockEnded;

    // 公共属性
    public DialogueLine CurrentLine
    {
        get
        {
            if (currentDialogue?.lines != null && currentLineIndex >= 0 && currentLineIndex < currentDialogue.lines.Count)
                return currentDialogue.lines[currentLineIndex];
            return null;
        }
    }
    public string CurrentLineId => CurrentLine?.id;
    public int CurrentLineIndex => currentLineIndex;

    // 私有变量
    private DialogueData currentDialogue;
    private string currentDialogueFileName;
    private string currentDialogueBlockId;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool inLLMMode = false;
    private bool waitingForPlayerInput = false;
    private bool isProcessingLLM = false;
    private bool aiRequestedEnd = false;
    private bool waitingForContinue = false;
    private string currentLLMCharacter;
    private Coroutine typingCoroutine;
    private string fullCurrentText = "";

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

        UpdateDialogueSettings();
        GlobalSystemManager.OnDialogueSettingsChanged += UpdateDialogueSettings;
    }

    private void UpdateDialogueSettings()
    {
        float settings = GlobalSystemManager.Instance.GetDialogueSettings();
        textSpeed = settings;
    }

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

    private enum UIState
    {
        ShowingText,
        WaitingInput,
        ProcessingAI
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

        OnDialogueBlockStarted?.Invoke(fileName, blockId);

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

        if (line.mode)
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

        OnLineStarted?.Invoke(line.id, line.characterId, true);

        if (useTypingEffect && !string.IsNullOrEmpty(fullCurrentText))
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(fullCurrentText, line.id, line.characterId, true));
        }
        else
        {
            dialogueText.text = fullCurrentText;
            waitingForContinue = true;
            OnLineCompleted?.Invoke(line.id, line.characterId, true);
        }
    }

    private IEnumerator TypeText(string text, string lineId = null, string characterId = null, bool isPresetMode = true)
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

        if (!string.IsNullOrEmpty(lineId))
        {
            OnLineCompleted?.Invoke(lineId, characterId ?? "", isPresetMode);
        }
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
            if (aiRequestedEnd)
            {
                EndLLMMode();
                return;
            }

            SetUIState(UIState.WaitingInput);
            return;
        }

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

        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
        {
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentLine.mode);
        }
    }

    private void SetCharacterInfo(string characterId, string portraitId)
    {
        if (characterNameText != null)
            characterNameText.text = GetCharacterDisplayName(characterId);

        if (characterPortrait != null)
        {
            if (!string.IsNullOrEmpty(portraitId))
            {
                Sprite portrait = Resources.Load<Sprite>($"Art/Characters/{characterId}");
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
        if (LanguageManager.Instance != null)
        {
            switch (LanguageManager.Instance.currentLanguage)
            {
                case SupportedLanguage.Chinese:
                    return GetChineseCharacterName(characterId);
                case SupportedLanguage.English:
                    return GetEnglishCharacterName(characterId);
                case SupportedLanguage.Japanese:
                    return GetJapaneseCharacterName(characterId);
                default:
                    return GetEnglishCharacterName(characterId);
            }
        }

        return GetChineseCharacterName(characterId);
    }

    private string GetChineseCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me": return "我";
            case "guardian": return "安全卫士";
            case "narrator": return "旁白";
            case "ps": return "PhotoShop";
            case "controlpanel": return "控制面板";
            case "qq": return "QQ";
            case "7zip": return "7-Zip";
            case "mine": return "扫雷";
            case "xunlei": return "迅雷";
            case "ie": return "IE";
            default: return characterId;
        }
    }

    private string GetEnglishCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me": return "Me";
            case "guardian": return "System Guardian";
            case "narrator": return "Narrator";
            case "ps": return "Photoshop";
            case "controlpanel": return "Control Panel";
            case "qq": return "QQ";
            case "7zip": return "7-Zip";
            case "mine": return "Minesweeper";
            case "xunlei": return "Xunlei";
            case "ie": return "Internet Explorer";
            default: return characterId;
        }
    }

    private string GetJapaneseCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me": return "私";
            case "guardian": return "システムガーディアン";
            case "narrator": return "ナレーター";
            case "ps": return "フォトショップ";
            case "controlpanel": return "コントロールパネル";
            case "qq": return "QQ";
            case "7zip": return "7-Zip";
            case "mine": return "Minesweeper";
            case "xunlei": return "迅雷";
            case "ie": return "インターネットエクスプローラー";
            default: return characterId;
        }
    }

    private void OnDialogueEnd()
    {
        inLLMMode = false;
        isProcessingLLM = false;

        OnDialogueBlockEnded?.Invoke(currentDialogueFileName, currentDialogueBlockId);

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

    private void StartLLMMode(DialogueLine line)
    {
        inLLMMode = true;
        aiRequestedEnd = false;
        currentLLMCharacter = line.characterId;

        SetCharacterInfo(line.characterId, line.portraitId);

        OnLineStarted?.Invoke(line.id, line.characterId, false);

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.historyManager != null)
        {
            SetUIState(UIState.ProcessingAI);

            dialogueManager.historyManager.StartLLMSession(
                line.text,
                OnInitialLLMResponse,
                dialogueManager
            );
        }
    }

    private void OnInitialLLMResponse(string response)
    {
        OnLLMResponse(response);
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

        if (DialogueLoader.ShouldEndByAI(response))
        {
            aiRequestedEnd = true;
            response = DialogueLoader.CleanEndMarker(response);
        }

        fullCurrentText = response;

        if (useTypingEffect)
        {
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            DialogueLine currentLine = CurrentLine;
            typingCoroutine = StartCoroutine(TypeText(fullCurrentText, currentLine?.id, currentLine?.characterId, false));
        }
        else
        {
            dialogueText.text = fullCurrentText;
            waitingForContinue = true;

            DialogueLine currentLine = CurrentLine;
            OnLineCompleted?.Invoke(currentLine?.id ?? "", currentLine?.characterId ?? "", false);
        }
    }

    private void EndLLMMode()
    {
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
}