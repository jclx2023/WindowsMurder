using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI���")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image characterPortrait;
    public TMP_InputField playerInputField;
    public Button sendButton;
    public Button exitLLMButton;

    [Header("���ֻ�Ч������")]
    public float textSpeed = 0.05f;
    public bool useTypingEffect = true;

    [Header("���ֶ���Ч��")]
    public bool enableBounceEffect = true;
    public float bounceHeight = 8f;
    public float bounceDuration = 0.3f;
    public AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool enableRandomOffset = true;
    public float randomOffsetRange = 2f;
    public float offsetSmoothTime = 0.15f;

    [Header("��Ч")]
    public AudioSource audioSource;
    private AudioClip[] typingSounds;
    private const int TYPING_SOUND_COUNT = 32;

    [Header("LLM������ʾ")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    // ========== ������CMDģʽ ==========
    [Header("CMDģʽ����")]
    public bool enableCmdMode = false; // �Ƿ�����CMD��ʽ
    public string cmdPath = "C:\\Windows\\System32>";
    public Color cmdTextColor = new Color(0.3f, 1f, 0.3f); // ��CMD��ɫ����

    // ========== �¼�ϵͳ ==========
    public static event System.Action<string, string, string, bool> OnLineStarted;
    public static event System.Action<string, string, string, bool> OnLineCompleted;
    public static event System.Action<string, string> OnDialogueBlockStarted;
    public static event System.Action<string, string> OnDialogueBlockEnded;

    // ========== ״̬������ ==========
    private enum LLMState { Inactive, WaitingForAI, ShowingAIText, WaitingForClick, WaitingForInput, ProcessingInput }
    private enum PresetState { Inactive, ShowingText, WaitingForClick }

    // ========== ״̬���� ==========
    private LLMState llmState = LLMState.Inactive;
    private PresetState presetState = PresetState.Inactive;

    private bool isLLMTyping = false;
    private bool aiRequestedEnd = false;
    private string currentLLMCharacter;
    private Coroutine llmTypingCoroutine;

    private bool isPresetTyping = false;
    private Coroutine presetTypingCoroutine;

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

    private DialogueData currentDialogue;
    private string currentDialogueFileName;
    private string currentDialogueBlockId;
    private int currentLineIndex = 0;
    private string fullCurrentText = "";

    // ============================================================
    void Start()
    {
        HideDialoguePanel();
        EnsureAudioSource();
        LoadTypingSounds();

        if (sendButton != null) sendButton.onClick.AddListener(OnSendMessage);
        if (playerInputField != null) playerInputField.onSubmit.AddListener(delegate { OnSendMessage(); });

        if (exitLLMButton != null)
        {
            exitLLMButton.onClick.AddListener(OnExitLLMButtonClicked);
            exitLLMButton.gameObject.SetActive(false);
        }

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
        if (dialoguePanel == null) return;

        dialoguePanel.SetActive(true);

        if (enableCmdMode)
        {

            if (characterNameText != null)
                characterNameText.gameObject.SetActive(false);

            if (characterPortrait != null)
                characterPortrait.gameObject.SetActive(true);
        }
    }


    public void HideDialoguePanel()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public bool IsDialoguePanelVisible() =>
        dialoguePanel != null && dialoguePanel.activeSelf;

    // ========== LLM״̬�� ==========
    private void SetLLMState(LLMState newState)
    {
        llmState = newState;

        switch (newState)
        {
            case LLMState.Inactive:
                isLLMTyping = false;
                aiRequestedEnd = false;
                SetExitButtonActive(false);
                break;

            case LLMState.WaitingForAI:
                SetTextActive(true);
                SetInputActive(false);
                SetExitButtonActive(true);
                if (dialogueText != null) dialogueText.text = waitingForAIHint;
                break;

            case LLMState.ShowingAIText:
                SetTextActive(true);
                SetInputActive(false);
                SetExitButtonActive(true);
                isLLMTyping = true;
                break;

            case LLMState.WaitingForInput:
                SetTextActive(false);
                SetInputActive(true);
                SetExitButtonActive(true);
                isLLMTyping = false;
                if (playerInputField != null)
                {
                    playerInputField.text = "";
                    playerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = waitingForInputHint;
                    playerInputField.Select();
                }
                break;

            case LLMState.ProcessingInput:
                SetTextActive(true);
                SetInputActive(false);
                SetExitButtonActive(true);
                if (dialogueText != null) dialogueText.text = waitingForAIHint;
                break;
        }
    }

    private void SetPresetState(PresetState newState)
    {
        presetState = newState;

        switch (newState)
        {
            case PresetState.Inactive:
                isPresetTyping = false;
                break;

            case PresetState.ShowingText:
                SetTextActive(true);
                SetInputActive(false);
                SetExitButtonActive(false);
                isPresetTyping = true;
                break;

            case PresetState.WaitingForClick:
                isPresetTyping = false;
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

    private void SetExitButtonActive(bool active)
    {
        if (exitLLMButton != null)
            exitLLMButton.gameObject.SetActive(active);
    }

    // ========== ������� ==========
    private void OnDialogueTextClicked()
    {
        if (llmState != LLMState.Inactive) HandleLLMClick();
        else if (presetState != PresetState.Inactive) HandlePresetClick();
    }

    private void HandleLLMClick()
    {
        if (llmState == LLMState.WaitingForClick)
            SetLLMState(LLMState.WaitingForInput);
    }

    private void HandlePresetClick()
    {
        switch (presetState)
        {
            case PresetState.ShowingText:
                if (isPresetTyping) SkipPresetTyping();
                break;

            case PresetState.WaitingForClick:
                currentLineIndex++;
                ShowNextLine();
                break;
        }
    }

    private void OnExitLLMButtonClicked()
    {
        if (llmState == LLMState.Inactive) return;
        CleanupBeforeHide();
        EndLLMMode();
    }

    // ========== ���� ==========
    private void CleanupBeforeHide()
    {
        if (presetTypingCoroutine != null) StopCoroutine(presetTypingCoroutine);
        if (llmTypingCoroutine != null) StopCoroutine(llmTypingCoroutine);
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();

        SetLLMState(LLMState.Inactive);
        SetPresetState(PresetState.Inactive);

        if (dialogueText != null) dialogueText.text = "";
    }

    // ========== �Ի����� ==========
    public void StartDialogue(DialogueData dialogueData, string fileName, string blockId)
    {
        if (dialogueData == null) return;

        ShowDialoguePanel();
        currentDialogue = dialogueData;
        currentDialogueFileName = fileName;
        currentDialogueBlockId = blockId;
        currentLineIndex = 0;

        OnDialogueBlockStarted?.Invoke(fileName, blockId);
        ClearDialogue();
        ShowNextLine();
    }

    public void StartDialogue(DialogueData dialogueData) =>
        StartDialogue(dialogueData, "unknown", dialogueData?.conversationId ?? "unknown");

    public void ShowNextLine()
    {
        if (currentDialogue?.lines == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            OnDialogueEnd();
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];
        if (line.mode) ShowPresetLine(line);
        else StartLLMMode(line);
    }

    // ========== ��ͨģʽ��ʾ ==========
    private void ShowPresetLine(DialogueLine line)
    {
        SetLLMState(LLMState.Inactive);
        SetCharacterInfo(line.characterId, line.portraitId);
        fullCurrentText = line.text ?? "";

        OnLineStarted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);

        if (enableCmdMode)
        {
            string prefix = $"{cmdPath} {GetCharacterDisplayName(line.characterId)}> ";
            dialogueText.text = prefix;
        }

        if (useTypingEffect && !string.IsNullOrEmpty(fullCurrentText))
            StartPresetTyping(fullCurrentText);
        else
        {
            dialogueText.text += fullCurrentText;
            SetPresetState(PresetState.WaitingForClick);
            OnLineCompleted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);
        }
    }

    private void StartPresetTyping(string text)
    {
        if (presetTypingCoroutine != null) StopCoroutine(presetTypingCoroutine);
        presetTypingCoroutine = StartCoroutine(PresetTypeText(text));
    }

    private IEnumerator PresetTypeText(string text)
    {
        SetPresetState(PresetState.ShowingText);
        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(CurrentLine.characterId)} >" : "";
        dialogueText.text = prefix;
        fullCurrentText = text;
        string visibleText = "";

        for (int i = 0; i < text.Length; i++)
        {
            visibleText += text[i];
            dialogueText.text = prefix + visibleText;
            PlayRandomTypingSound();
            yield return new WaitForSeconds(textSpeed);
        }

        SetPresetState(PresetState.WaitingForClick);
        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentDialogueBlockId, true);
    }

    private void SkipPresetTyping()
    {
        if (presetTypingCoroutine != null) StopCoroutine(presetTypingCoroutine);
        dialogueText.text = (enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(CurrentLine.characterId)}> " : "") + fullCurrentText;
        SetPresetState(PresetState.WaitingForClick);
    }

    // ========== LLMģʽ��ʾ ==========
    private void StartLLMMode(DialogueLine line)
    {
        SetPresetState(PresetState.Inactive);
        SetLLMState(LLMState.WaitingForAI);
        aiRequestedEnd = false;
        currentLLMCharacter = line.characterId;
        SetCharacterInfo(line.characterId, line.portraitId);
        OnLineStarted?.Invoke(line.id, line.characterId, currentDialogueBlockId, false);

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.historyManager != null)
        {
            dialogueManager.historyManager.StartLLMSession(line.text, OnInitialLLMResponse, dialogueManager);
        }
    }

    private void OnInitialLLMResponse(string response) => OnLLMResponse(response);

    public void OnLLMResponse(string response)
    {
        if (llmState == LLMState.Inactive) return;

        if (DialogueLoader.ShouldEndByAI(response))
        {
            aiRequestedEnd = true;
            response = DialogueLoader.CleanEndMarker(response);
        }

        fullCurrentText = response;
        if (useTypingEffect) StartLLMTyping(fullCurrentText);
        else
        {
            dialogueText.text = fullCurrentText;
            SetLLMState(LLMState.WaitingForInput);
        }
    }

    private void StartLLMTyping(string text)
    {
        if (llmTypingCoroutine != null) StopCoroutine(llmTypingCoroutine);
        llmTypingCoroutine = StartCoroutine(LLMTypeText(text));
    }

    private IEnumerator LLMTypeText(string text)
    {
        SetLLMState(LLMState.ShowingAIText);
        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(currentLLMCharacter)}> " : "";
        dialogueText.text = prefix;
        fullCurrentText = text;
        string visibleText = "";

        for (int i = 0; i < text.Length; i++)
        {
            visibleText += text[i];
            dialogueText.text = prefix + visibleText;
            PlayRandomTypingSound();
            yield return new WaitForSeconds(textSpeed);
        }

        SetLLMState(aiRequestedEnd ? LLMState.Inactive : LLMState.WaitingForClick);
        if (aiRequestedEnd)
        {
            yield return new WaitForSeconds(1f);
            EndLLMMode();
        }
    }

    // ========== �û����� ==========
    public void OnSendMessage()
    {
        if (llmState != LLMState.WaitingForInput) return;
        if (playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        SetLLMState(LLMState.ProcessingInput);
        DialogueLine currentLine = DialogueLoader.GetLineAt(currentDialogue, currentLineIndex);
        if (currentLine != null && DialogueLoader.ShouldEndLLMDialogue(message, currentLine.endKeywords))
        {
            EndLLMMode();
            return;
        }

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        SetLLMState(LLMState.WaitingForAI);
        dialogueManager.ProcessLLMMessage(currentLLMCharacter, message, OnLLMResponse);
    }

    private void EndLLMMode()
    {
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.historyManager != null)
            dialogueManager.historyManager.EndLLMSession();

        SetLLMState(LLMState.Inactive);
        currentLineIndex++;
        ShowNextLine();
    }

    // ========== ���� ==========
    private void EnsureAudioSource()
    {
        if (audioSource != null) return;
        audioSource = FindObjectOfType<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void LoadTypingSounds()
    {
        typingSounds = new AudioClip[TYPING_SOUND_COUNT];
        for (int i = 0; i < TYPING_SOUND_COUNT; i++)
        {
            string path = $"Audio/SFX/Typing/keypress-{(i + 1):D3}";
            typingSounds[i] = Resources.Load<AudioClip>(path);
        }
    }

    private void PlayRandomTypingSound()
    {
        if (audioSource == null || typingSounds == null || typingSounds.Length == 0) return;
        int randomIndex = Random.Range(0, typingSounds.Length);
        AudioClip selectedClip = typingSounds[randomIndex];
        if (selectedClip != null) audioSource.PlayOneShot(selectedClip);
    }

    private void SetCharacterInfo(string characterId, string portraitId)
    {
        // ���֣���ͨģʽ��ʾ��CMDģʽ���ض�������������������ǰ׺�
        if (characterNameText != null)
            characterNameText.text = GetCharacterDisplayName(characterId);

        // ͷ�񣺻ָ���������ʾ�߼�
        if (characterPortrait == null) return;

        // ���� portraitId ���Լ��أ��������� characterId ����
        Sprite portrait = null;
        if (!string.IsNullOrEmpty(portraitId))
        {
            portrait = Resources.Load<Sprite>($"Art/Characters/{portraitId}");
        }
        if (portrait == null && !string.IsNullOrEmpty(characterId))
        {
            portrait = Resources.Load<Sprite>($"Art/Characters/{characterId}");
        }

        if (portrait != null)
        {
            characterPortrait.sprite = portrait;
            characterPortrait.enabled = true;
            characterPortrait.gameObject.SetActive(true);
        }
        else
        {
            // û��Դ�����أ�������ʾ��Ӱ
            characterPortrait.gameObject.SetActive(false);
        }
    }


    private string GetCharacterDisplayName(string characterId)
    {
        switch (LanguageManager.Instance.currentLanguage)
        {
            case SupportedLanguage.Chinese: return GetChineseCharacterName(characterId);
            case SupportedLanguage.English: return GetEnglishCharacterName(characterId);
            case SupportedLanguage.Japanese: return GetJapaneseCharacterName(characterId);
            default: return GetEnglishCharacterName(characterId);
        }
    }

    private string GetChineseCharacterName(string id) => id switch
    {
        "me" => "��",
        "guardian" => "��ȫ��ʿ",
        "narrator" => "�԰�",
        "ps" => "PhotoShop",
        "controlpanel" => "�������",
        "qq" => "QQ",
        "7zip" => "7-Zip",
        "mines" => "ɨ��",
        "xunlei" => "Ѹ��",
        "ie" => "IE",
        "notepad" => "���±�",
        _ => id
    };

    private string GetEnglishCharacterName(string id) => id switch
    {
        "me" => "Me",
        "guardian" => "System Guardian",
        "narrator" => "Narrator",
        "ps" => "Photoshop",
        "controlpanel" => "Control Panel",
        "qq" => "QQ",
        "7zip" => "7-Zip",
        "mines" => "Minesweeper",
        "xunlei" => "Xunlei",
        "ie" => "Internet Explorer",
        _ => id
    };

    private string GetJapaneseCharacterName(string id) => id switch
    {
        "me" => "˽",
        "guardian" => "�����ƥ६�`�ǥ�����",
        "narrator" => "�ʥ�`���`",
        "ps" => "�ե��ȥ���å�",
        "controlpanel" => "����ȥ�`��ѥͥ�",
        "qq" => "QQ",
        "7zip" => "7-Zip",
        "mines" => "Minesweeper",
        "xunlei" => "Ѹ��",
        "ie" => "���󥿩`�ͥåȥ������ץ�`��`",
        "notepad" => "��⎤",
        _ => id
    };


    private void OnDialogueEnd()
    {
        CleanupBeforeHide();
        OnDialogueBlockEnded?.Invoke(currentDialogueFileName, currentDialogueBlockId);
        DialogueManager dm = FindObjectOfType<DialogueManager>();
        if (dm != null) dm.OnDialogueBlockComplete(currentDialogueFileName, currentDialogueBlockId);
        HideDialoguePanel();
    }

    private void ClearDialogue()
    {
        if (dialogueText != null) dialogueText.text = "";
        if (characterNameText != null) characterNameText.text = "";
        if (playerInputField != null) playerInputField.text = "";
    }
}
