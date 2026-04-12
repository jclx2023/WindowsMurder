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
    public Button exitLLMButton;

    [Header("打字机效果设置")]
    public float textSpeed = 0.05f;
    public bool useTypingEffect = true;

    [Header("文字动画效果")]
    public bool enableBounceEffect = true;
    public float bounceHeight = 8f;
    public float bounceDuration = 0.3f;
    public AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool enableRandomOffset = true;
    public float randomOffsetRange = 2f;
    public float offsetSmoothTime = 0.15f;

    [Header("音效")]
    public AudioSource audioSource;
    private AudioClip[] typingSounds;
    private const int TYPING_SOUND_COUNT = 32;

    [Header("LLM交互提示")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    [Header("Suggestions设置")]
    [Tooltip("多条建议问题的切换间隔（秒）")]
    public float suggestionCycleInterval = 4f;

    // ========== CMD模式 ==========
    [Header("CMD模式设置")]
    public bool enableCmdMode = false;
    public string cmdPath = "C:\\Windows\\System32>";
    public Color cmdTextColor = new Color(0.3f, 1f, 0.3f);

    // 事件系统
    public static event System.Action<string, string, string, bool> OnLineStarted;
    public static event System.Action<string, string, string, bool> OnLineCompleted;
    public static event System.Action<string, string> OnDialogueBlockStarted;
    public static event System.Action<string, string> OnDialogueBlockEnded;

    // ===== 状态机定义 =====
    private enum LLMState
    {
        Inactive,
        WaitingForAI,
        ShowingAIText,
        WaitingForClick,
        WaitingForInput,
        ProcessingInput
    }

    private enum PresetState
    {
        Inactive,
        ShowingText,
        WaitingForClick
    }

    // ===== 状态变量 =====
    private LLMState llmState = LLMState.Inactive;
    private PresetState presetState = PresetState.Inactive;

    // ===== LLM模式专用变量 =====
    private bool isLLMTyping = false;
    private bool aiRequestedEnd = false;
    private string currentLLMCharacter;
    private Coroutine llmTypingCoroutine;

    // ===== 普通模式专用变量 =====
    private bool isPresetTyping = false;
    private Coroutine presetTypingCoroutine;

    // ===== Suggestions 变量 =====
    private string[] currentSuggestions;
    private int currentSuggestionIndex = 0;
    private Coroutine suggestionCycleCoroutine;

    // ===== 通用变量 =====
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

    // ===== Unity 生命周期 =====

    void Start()
    {
        HideDialoguePanel();
        EnsureAudioSource();
        LoadTypingSounds();

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendMessage);

        if (playerInputField != null)
            playerInputField.onSubmit.AddListener(delegate { OnSendMessage(); });

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

        // 订阅 Suggestions 事件
        DialogueManager.OnSuggestionsReady += ShowSuggestions;

        UpdateDialogueSettings();
        GlobalSystemManager.OnDialogueSettingsChanged += UpdateDialogueSettings;
    }

    void OnDestroy()
    {
        // 取消订阅
        DialogueManager.OnSuggestionsReady -= ShowSuggestions;
        GlobalSystemManager.OnDialogueSettingsChanged -= UpdateDialogueSettings;
    }

    private void UpdateDialogueSettings()
    {
        float settings = GlobalSystemManager.Instance.GetDialogueSettings();
        textSpeed = settings;
    }

    // ===== Suggestions 系统 =====

    /// <summary>
    /// 接收 LLM 返回的建议问题，设置为输入框 Placeholder 并开始循环切换
    /// </summary>
    private void ShowSuggestions(string[] suggestions)
    {
        if (suggestions == null || suggestions.Length == 0) return;

        // 仅在等待玩家输入时显示（其他状态下无意义）
        if (llmState != LLMState.WaitingForInput) return;

        currentSuggestions = suggestions;
        currentSuggestionIndex = 0;
        UpdateSuggestionPlaceholder();

        // 多条建议时，启动循环切换
        if (suggestionCycleCoroutine != null)
            StopCoroutine(suggestionCycleCoroutine);

        if (suggestions.Length > 1)
            suggestionCycleCoroutine = StartCoroutine(CycleSuggestions());
    }

    /// <summary>
    /// 将当前建议问题写入输入框 Placeholder（灰色提示文字）
    /// </summary>
    private void UpdateSuggestionPlaceholder()
    {
        if (playerInputField == null || currentSuggestions == null || currentSuggestions.Length == 0)
            return;

        var placeholder = playerInputField.placeholder?.GetComponent<TextMeshProUGUI>();
        if (placeholder != null)
            placeholder.text = currentSuggestions[currentSuggestionIndex];
    }

    /// <summary>
    /// 每隔 suggestionCycleInterval 秒切换到下一条建议
    /// </summary>
    private IEnumerator CycleSuggestions()
    {
        while (true)
        {
            yield return new WaitForSeconds(suggestionCycleInterval);

            if (currentSuggestions == null || currentSuggestions.Length == 0) yield break;

            currentSuggestionIndex = (currentSuggestionIndex + 1) % currentSuggestions.Length;
            UpdateSuggestionPlaceholder();
        }
    }

    /// <summary>
    /// 清除建议问题，恢复默认 Placeholder 提示文字
    /// </summary>
    private void ClearSuggestions()
    {
        currentSuggestions = null;
        currentSuggestionIndex = 0;

        if (suggestionCycleCoroutine != null)
        {
            StopCoroutine(suggestionCycleCoroutine);
            suggestionCycleCoroutine = null;
        }

        if (playerInputField != null)
        {
            var placeholder = playerInputField.placeholder?.GetComponent<TextMeshProUGUI>();
            if (placeholder != null)
                placeholder.text = waitingForInputHint;
        }
    }

    // ===== 面板显示 =====

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
        else
        {
            if (characterNameText != null)
                characterNameText.gameObject.SetActive(true);
        }
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

    // ===== LLM 状态机控制 =====

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
                if (dialogueText != null)
                    dialogueText.text = waitingForAIHint;
                break;

            case LLMState.ShowingAIText:
                SetTextActive(true);
                SetInputActive(false);
                SetExitButtonActive(true);
                isLLMTyping = true;
                break;

            case LLMState.WaitingForClick:
                break;

            case LLMState.WaitingForInput:
                SetTextActive(false);
                SetInputActive(true);
                SetExitButtonActive(true);
                isLLMTyping = false;

                if (playerInputField != null)
                {
                    playerInputField.text = "";
                    // Placeholder 优先由 ShowSuggestions 设置；若暂无建议则使用默认提示
                    var placeholder = playerInputField.placeholder?.GetComponent<TextMeshProUGUI>();
                    if (placeholder != null && (currentSuggestions == null || currentSuggestions.Length == 0))
                        placeholder.text = waitingForInputHint;
                    playerInputField.Select();
                }
                break;

            case LLMState.ProcessingInput:
                SetTextActive(true);
                SetInputActive(false);
                SetExitButtonActive(true);
                if (dialogueText != null)
                    dialogueText.text = waitingForAIHint;
                break;
        }
    }

    // ===== 普通模式状态机控制 =====

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

    // ===== 点击处理 =====

    private void OnDialogueTextClicked()
    {
        if (llmState != LLMState.Inactive)
            HandleLLMClick();
        else if (presetState != PresetState.Inactive)
            HandlePresetClick();
    }

    private void HandleLLMClick()
    {
        switch (llmState)
        {
            case LLMState.ShowingAIText:
                break;
            case LLMState.WaitingForClick:
                SetLLMState(LLMState.WaitingForInput);
                break;
        }
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

    // ===== 退出按钮处理 =====

    private void OnExitLLMButtonClicked()
    {
        if (llmState == LLMState.Inactive) return;
        CleanupBeforeHide();
        EndLLMMode();
    }

    // ===== 清理方法 =====

    private void CleanupBeforeHide()
    {
        if (presetTypingCoroutine != null)
        {
            StopCoroutine(presetTypingCoroutine);
            presetTypingCoroutine = null;
        }

        if (llmTypingCoroutine != null)
        {
            StopCoroutine(llmTypingCoroutine);
            llmTypingCoroutine = null;
        }

        StopAllCoroutines();

        if (audioSource != null)
            audioSource.Stop();

        // 清除建议问题（StopAllCoroutines 已停止循环协程，这里只清数据和 Placeholder）
        ClearSuggestions();

        SetLLMState(LLMState.Inactive);
        SetPresetState(PresetState.Inactive);
        isLLMTyping = false;
        isPresetTyping = false;

        if (dialogueText != null)
            dialogueText.text = "";
    }

    // ===== 对话启动 =====

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

    // ===== 普通模式显示 =====

    private void ShowPresetLine(DialogueLine line)
    {
        SetLLMState(LLMState.Inactive);
        SetCharacterInfo(line.characterId, line.portraitId);
        fullCurrentText = line.text ?? "";

        OnLineStarted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);

        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(line.characterId)}> " : "";

        if (useTypingEffect && !string.IsNullOrEmpty(fullCurrentText))
        {
            StartPresetTyping(fullCurrentText);
        }
        else
        {
            dialogueText.text = prefix + fullCurrentText;
            SetPresetState(PresetState.WaitingForClick);
            OnLineCompleted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);
        }
    }

    private void StartPresetTyping(string text)
    {
        if (presetTypingCoroutine != null)
            StopCoroutine(presetTypingCoroutine);
        presetTypingCoroutine = StartCoroutine(PresetTypeText(text));
    }

    private IEnumerator PresetTypeText(string text)
    {
        SetPresetState(PresetState.ShowingText);

        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(CurrentLine.characterId)}> " : "";
        dialogueText.text = prefix;
        fullCurrentText = text;

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            dialogueText.ForceMeshUpdate();
            PlayRandomTypingSound();

            if (enableBounceEffect || enableRandomOffset)
                StartCoroutine(AnimateCharacter(i + prefix.Length));

            yield return new WaitForSeconds(textSpeed);
        }

        SetPresetState(PresetState.WaitingForClick);

        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentDialogueBlockId, true);
    }

    private void SkipPresetTyping()
    {
        if (presetTypingCoroutine != null)
            StopCoroutine(presetTypingCoroutine);

        StopAllCoroutines();

        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(CurrentLine.characterId)}> " : "";
        dialogueText.text = prefix + fullCurrentText;
        dialogueText.ForceMeshUpdate();

        SetPresetState(PresetState.WaitingForClick);

        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentDialogueBlockId, true);
    }

    // ===== LLM 模式显示 =====

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

    public void OnLLMResponse(string response)
    {
        if (llmState == LLMState.Inactive) return;

        if (DialogueLoader.ShouldEndByAI(response))
        {
            aiRequestedEnd = true;
            response = DialogueLoader.CleanEndMarker(response);
        }

        fullCurrentText = response;

        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(currentLLMCharacter)}> " : "";

        if (useTypingEffect)
        {
            StartLLMTyping(fullCurrentText);
        }
        else
        {
            dialogueText.text = prefix + fullCurrentText;
            SetLLMState(LLMState.WaitingForInput);

            DialogueLine currentLine = CurrentLine;
            OnLineCompleted?.Invoke(currentLine?.id ?? "", currentLine?.characterId ?? "", currentDialogueBlockId, false);
        }
    }

    private void StartLLMTyping(string text)
    {
        if (llmTypingCoroutine != null)
            StopCoroutine(llmTypingCoroutine);
        llmTypingCoroutine = StartCoroutine(LLMTypeText(text));
    }

    private IEnumerator LLMTypeText(string text)
    {
        SetLLMState(LLMState.ShowingAIText);

        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(currentLLMCharacter)}> " : "";
        dialogueText.text = prefix;
        fullCurrentText = text;

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            dialogueText.ForceMeshUpdate();
            PlayRandomTypingSound();

            if (enableBounceEffect || enableRandomOffset)
                StartCoroutine(AnimateCharacter(i + prefix.Length));

            yield return new WaitForSeconds(textSpeed);
        }

        DialogueLine currentLine = CurrentLine;
        OnLineCompleted?.Invoke(currentLine?.id ?? "", currentLine?.characterId ?? "", currentDialogueBlockId, false);

        if (aiRequestedEnd)
        {
            yield return new WaitForSeconds(1f);
            EndLLMMode();
        }
        else
        {
            SetLLMState(LLMState.WaitingForClick);
        }
    }

    // ===== 用户输入处理 =====

    public void OnSendMessage()
    {
        if (llmState != LLMState.WaitingForInput) return;
        if (playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // 玩家发送消息后清除建议问题
        ClearSuggestions();

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

    // ===== 字符动画 =====

    private IEnumerator AnimateCharacter(int charIndex)
    {
        Vector2 randomOffset = Vector2.zero;
        if (enableRandomOffset)
        {
            randomOffset = new Vector2(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(-randomOffsetRange, randomOffsetRange)
            );
        }

        float elapsedTime = 0f;
        float animDuration = enableBounceEffect ? bounceDuration : offsetSmoothTime;

        while (elapsedTime < animDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / animDuration);

            dialogueText.ForceMeshUpdate();
            TMP_TextInfo textInfo = dialogueText.textInfo;

            if (charIndex >= textInfo.characterCount || !textInfo.characterInfo[charIndex].isVisible)
                yield break;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 bounceOffset = Vector3.zero;
            if (enableBounceEffect)
            {
                float bounceValue = bounceCurve.Evaluate(progress);
                float currentHeight = bounceHeight * (1 - bounceValue);
                bounceOffset = new Vector3(0, currentHeight, 0);
            }

            Vector3 currentRandomOffset = Vector3.zero;
            if (enableRandomOffset)
            {
                float offsetProgress = Mathf.SmoothStep(1f, 0f, progress);
                currentRandomOffset = randomOffset * offsetProgress;
            }

            Vector3 totalOffset = bounceOffset + currentRandomOffset;

            vertices[vertexIndex + 0] += totalOffset;
            vertices[vertexIndex + 1] += totalOffset;
            vertices[vertexIndex + 2] += totalOffset;
            vertices[vertexIndex + 3] += totalOffset;

            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            yield return null;
        }

        dialogueText.ForceMeshUpdate();
        if (charIndex < dialogueText.textInfo.characterCount && dialogueText.textInfo.characterInfo[charIndex].isVisible)
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    // ===== 辅助方法 =====

    private void EnsureAudioSource()
    {
        if (audioSource != null) return;

        audioSource = FindObjectOfType<AudioSource>();
        if (audioSource != null) return;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1f;
    }

    private void LoadTypingSounds()
    {
        typingSounds = new AudioClip[TYPING_SOUND_COUNT];
        for (int i = 0; i < TYPING_SOUND_COUNT; i++)
        {
            string path = $"Audio/SFX/Typing/keypress-{(i + 1):D3}";
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip != null)
                typingSounds[i] = clip;
            else
                Debug.LogWarning($"DialogueUI: 无法加载音效文件: {path}");
        }
    }

    private void PlayRandomTypingSound()
    {
        if (audioSource == null || typingSounds == null || typingSounds.Length == 0) return;
        AudioClip clip = typingSounds[Random.Range(0, typingSounds.Length)];
        if (clip != null) audioSource.PlayOneShot(clip);
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
                case SupportedLanguage.Chinese:  return GetChineseCharacterName(characterId);
                case SupportedLanguage.English:  return GetEnglishCharacterName(characterId);
                case SupportedLanguage.Japanese: return GetJapaneseCharacterName(characterId);
                default:                         return GetEnglishCharacterName(characterId);
            }
        }
        return GetChineseCharacterName(characterId);
    }

    private string GetChineseCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me":           return "我";
            case "guardian":     return "安全卫士";
            case "narrator":     return "旁白";
            case "ps":           return "PhotoShop";
            case "controlpanel": return "控制面板";
            case "qq":           return "QQ";
            case "7zip":         return "7-Zip";
            case "mines":        return "扫雷";
            case "xunlei":       return "迅雷";
            case "ie":           return "IE";
            case "notepad":      return "记事本";
            case "recycle":      return "回收站";
            case "registry":     return "注册表工具";
            default:             return characterId;
        }
    }

    private string GetEnglishCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me":           return "Me";
            case "guardian":     return "System Guardian";
            case "narrator":     return "Narrator";
            case "ps":           return "Photoshop";
            case "controlpanel": return "Control Panel";
            case "qq":           return "QQ";
            case "7zip":         return "7-Zip";
            case "mines":        return "Minesweeper";
            case "xunlei":       return "Xunlei";
            case "ie":           return "Internet Explorer";
            case "notepad":      return "Notepad";
            case "recycle":      return "Recycle Bin";
            case "registry":     return "Registry Editor";
            default:             return characterId;
        }
    }

    private string GetJapaneseCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me":           return "私";
            case "guardian":     return "システムガーディアン";
            case "narrator":     return "ナレーター";
            case "ps":           return "フォトショップ";
            case "controlpanel": return "コントロールパネル";
            case "qq":           return "QQ";
            case "7zip":         return "7-Zip";
            case "mines":        return "Minesweeper";
            case "xunlei":       return "迅雷";
            case "ie":           return "インターネットエクスプローラー";
            case "notepad":      return "メモ帳";
            case "recycle":      return "ゴミ箱";
            case "registry":     return "レジストリ エディター";
            default:             return characterId;
        }
    }

    private void OnDialogueEnd()
    {
        CleanupBeforeHide();

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
        if (dialogueText != null)     dialogueText.text = "";
        if (characterNameText != null) characterNameText.text = "";
        if (characterPortrait != null) characterPortrait.gameObject.SetActive(false);
        if (playerInputField != null)  playerInputField.text = "";
        ClearSuggestions();
    }
}
