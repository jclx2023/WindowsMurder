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
    private const int TYPING_SOUND_COUNT = 32; // ��Ч�ļ�����

    [Header("LLM������ʾ")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    // ========== ������CMDģʽ ==========
    [Header("CMDģʽ����")]
    public bool enableCmdMode = false; // �Ƿ�����CMD��ʽ
    public string cmdPath = "C:\\Windows\\System32>";
    public Color cmdTextColor = new Color(0.3f, 1f, 0.3f); // ��CMD��ɫ����

    // �¼�ϵͳ
    public static event System.Action<string, string, string, bool> OnLineStarted;
    public static event System.Action<string, string, string, bool> OnLineCompleted;
    public static event System.Action<string, string> OnDialogueBlockStarted;
    public static event System.Action<string, string> OnDialogueBlockEnded;

    // ===== ״̬������ =====
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

    // ===== ״̬���� =====
    private LLMState llmState = LLMState.Inactive;
    private PresetState presetState = PresetState.Inactive;

    // ===== LLMģʽר�ñ��� =====
    private bool isLLMTyping = false;
    private bool aiRequestedEnd = false;
    private string currentLLMCharacter;
    private Coroutine llmTypingCoroutine;

    // ===== ��ͨģʽר�ñ��� =====
    private bool isPresetTyping = false;
    private Coroutine presetTypingCoroutine;

    // ===== ͨ�ñ��� =====
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

        UpdateDialogueSettings();
        GlobalSystemManager.OnDialogueSettingsChanged += UpdateDialogueSettings;
    }

    private void UpdateDialogueSettings()
    {
        float settings = GlobalSystemManager.Instance.GetDialogueSettings();
        textSpeed = settings;
    }

    // ���޸ĵ� 1�������� CMD ģʽ�� UI ���������/��ʾ�߼�
    public void ShowDialoguePanel()
    {
        if (dialoguePanel == null) return;
        dialoguePanel.SetActive(true);

        if (enableCmdMode)
        {
            // CMD ģʽ�£����ض�����������
            if (characterNameText != null)
                characterNameText.gameObject.SetActive(false);

            // ȷ��ͷ�񱻼��� (ԭ�� SetCharacterInfo �ᴦ��ͼƬ���غ���ʾ������ֻ������)
            if (characterPortrait != null)
                characterPortrait.gameObject.SetActive(true);
        }
        else
        {
            // �� CMD ģʽ�£�ȷ���������ɼ�
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

    // ===== LLM״̬������ (��ԭ��һ��) =====
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
                // ԭ���߼������ȴ����
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
                if (dialogueText != null)
                    dialogueText.text = waitingForAIHint;
                break;
        }
    }

    // ===== ��ͨģʽ״̬������ (��ԭ��һ��) =====
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

    // ===== ������� (��ԭ��һ��) =====
    private void OnDialogueTextClicked()
    {
        if (llmState != LLMState.Inactive)
        {
            HandleLLMClick();
        }
        else if (presetState != PresetState.Inactive)
        {
            HandlePresetClick();
        }
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

            default:
                break;
        }
    }

    private void HandlePresetClick()
    {
        switch (presetState)
        {
            case PresetState.ShowingText:
                if (isPresetTyping)
                {
                    SkipPresetTyping();
                }
                break;

            case PresetState.WaitingForClick:
                currentLineIndex++;
                ShowNextLine();
                break;
        }
    }

    // ===== �˳���ť���� (��ԭ��һ��) =====
    private void OnExitLLMButtonClicked()
    {
        if (llmState == LLMState.Inactive)
            return;

        CleanupBeforeHide();
        EndLLMMode();
    }

    // ===== ������ (��ԭ��һ��) =====
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

        SetLLMState(LLMState.Inactive);
        SetPresetState(PresetState.Inactive);
        isLLMTyping = false;
        isPresetTyping = false;

        if (dialogueText != null)
            dialogueText.text = "";
    }

    // ===== �Ի����� (��ԭ��һ��) =====
    public void StartDialogue(DialogueData dialogueData, string fileName, string blockId)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueUI: �Ի�����Ϊ��");
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

    // ===== ��ͨģʽ��ʾ (���� CMD ǰ׺�߼�) =====
    private void ShowPresetLine(DialogueLine line)
    {
        SetLLMState(LLMState.Inactive);

        SetCharacterInfo(line.characterId, line.portraitId);
        fullCurrentText = line.text ?? "";

        OnLineStarted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);

        // ���� CMD ģʽǰ׺
        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(line.characterId)}> " : "";

        if (useTypingEffect && !string.IsNullOrEmpty(fullCurrentText))
        {
            StartPresetTyping(fullCurrentText);
        }
        else
        {
            dialogueText.text = prefix + fullCurrentText; // ���޸ĵ� 2a�����Ǵ��ֻ�ʱ����ǰ׺
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

    // ���޸ĵ� 2b�������ֻ�Э���м��� CMD ǰ׺�߼�
    private IEnumerator PresetTypeText(string text)
    {
        SetPresetState(PresetState.ShowingText);

        // ���� CMD ģʽǰ׺
        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(CurrentLine.characterId)}> " : "";
        dialogueText.text = prefix; // ��ʼ���ı�Ϊǰ׺

        fullCurrentText = text;

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            dialogueText.ForceMeshUpdate();

            PlayRandomTypingSound();

            if (enableBounceEffect || enableRandomOffset)
            {
                // ע�⣺�ַ�������Ҫ����ǰ׺�ĳ���
                StartCoroutine(AnimateCharacter(i + prefix.Length));
            }

            yield return new WaitForSeconds(textSpeed);
        }

        SetPresetState(PresetState.WaitingForClick);

        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
        {
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentDialogueBlockId, true);
        }
    }

    // ���޸ĵ� 2c�����������ֻ�ʱ���� CMD ǰ׺�߼�
    private void SkipPresetTyping()
    {
        if (presetTypingCoroutine != null)
            StopCoroutine(presetTypingCoroutine);

        StopAllCoroutines();

        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(CurrentLine.characterId)}> " : "";
        dialogueText.text = prefix + fullCurrentText; // ����ǰ׺
        dialogueText.ForceMeshUpdate();

        SetPresetState(PresetState.WaitingForClick);

        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
        {
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentDialogueBlockId, true);
        }
    }

    // ===== LLMģʽ��ʾ (���� CMD ǰ׺�߼�) =====
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
        if (llmState == LLMState.Inactive)
            return;

        if (DialogueLoader.ShouldEndByAI(response))
        {
            aiRequestedEnd = true;
            response = DialogueLoader.CleanEndMarker(response);
        }

        fullCurrentText = response;

        // ���� CMD ģʽǰ׺
        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(currentLLMCharacter)}> " : "";

        if (useTypingEffect)
        {
            StartLLMTyping(fullCurrentText);
        }
        else
        {
            dialogueText.text = prefix + fullCurrentText; // ���޸ĵ� 3a�����Ǵ��ֻ�ʱ����ǰ׺
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

    // ���޸ĵ� 3b����LLM ���ֻ�Э���м��� CMD ǰ׺�߼�
    private IEnumerator LLMTypeText(string text)
    {
        SetLLMState(LLMState.ShowingAIText);

        // ���� CMD ģʽǰ׺
        string prefix = enableCmdMode ? $"{cmdPath} {GetCharacterDisplayName(currentLLMCharacter)}> " : "";
        dialogueText.text = prefix; // ��ʼ���ı�Ϊǰ׺

        fullCurrentText = text;

        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            dialogueText.ForceMeshUpdate();

            PlayRandomTypingSound();

            if (enableBounceEffect || enableRandomOffset)
            {
                // ע�⣺�ַ�������Ҫ����ǰ׺�ĳ���
                StartCoroutine(AnimateCharacter(i + prefix.Length));
            }

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

    // ===== �û����봦�� (��ԭ��һ��) =====
    public void OnSendMessage()
    {
        if (llmState != LLMState.WaitingForInput)
            return;

        if (playerInputField == null)
            return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message))
            return;

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
        {
            dialogueManager.historyManager.EndLLMSession();
        }

        SetLLMState(LLMState.Inactive);
        currentLineIndex++;
        ShowNextLine();
    }

    // ===== �ַ����� (��ԭ��һ��) =====
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

            Vector3 bottomLeft = vertices[vertexIndex + 0];
            Vector3 topLeft = vertices[vertexIndex + 1];
            Vector3 topRight = vertices[vertexIndex + 2];
            Vector3 bottomRight = vertices[vertexIndex + 3];

            vertices[vertexIndex + 0] = bottomLeft + totalOffset;
            vertices[vertexIndex + 1] = topLeft + totalOffset;
            vertices[vertexIndex + 2] = topRight + totalOffset;
            vertices[vertexIndex + 3] = bottomRight + totalOffset;

            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            yield return null;
        }

        dialogueText.ForceMeshUpdate();
        TMP_TextInfo finalTextInfo = dialogueText.textInfo;

        if (charIndex < finalTextInfo.characterCount && finalTextInfo.characterInfo[charIndex].isVisible)
        {
            TMP_CharacterInfo finalCharInfo = finalTextInfo.characterInfo[charIndex];
            int finalMaterialIndex = finalCharInfo.materialReferenceIndex;
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }

    // ===== �������� (��ԭ��һ��) =====
    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = FindObjectOfType<AudioSource>();

        if (audioSource != null)
        {
            Debug.Log($"DialogueUI: �ҵ�AudioSource: {audioSource.gameObject.name}");
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 1f;

        Debug.Log("DialogueUI: δ�ҵ�AudioSource�����Զ�����");
    }

    private void LoadTypingSounds()
    {
        typingSounds = new AudioClip[TYPING_SOUND_COUNT];

        for (int i = 0; i < TYPING_SOUND_COUNT; i++)
        {
            string path = $"Audio/SFX/Typing/keypress-{(i + 1):D3}";
            AudioClip clip = Resources.Load<AudioClip>(path);

            if (clip != null)
            {
                typingSounds[i] = clip;
            }
            else
            {
                Debug.LogWarning($"DialogueUI: �޷�������Ч�ļ�: {path}");
            }
        }

        Debug.Log($"DialogueUI: �ɹ����� {typingSounds.Length} ��������Ч");
    }

    private void PlayRandomTypingSound()
    {
        if (audioSource == null || typingSounds == null || typingSounds.Length == 0)
            return;

        int randomIndex = Random.Range(0, typingSounds.Length);
        AudioClip selectedClip = typingSounds[randomIndex];

        if (selectedClip != null)
        {
            audioSource.PlayOneShot(selectedClip);
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
            case "me": return "��";
            case "guardian": return "��ȫ��ʿ";
            case "narrator": return "�԰�";
            case "ps": return "PhotoShop";
            case "controlpanel": return "�������";
            case "qq": return "QQ";
            case "7zip": return "7-Zip";
            case "mines": return "ɨ��";
            case "xunlei": return "Ѹ��";
            case "ie": return "IE";
            case "notepad": return "���±�";
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
            case "mines": return "Minesweeper";
            case "xunlei": return "Xunlei";
            case "ie": return "Internet Explorer";
            default: return characterId;
        }
    }

    private string GetJapaneseCharacterName(string characterId)
    {
        switch (characterId)
        {
            case "me": return "˽";
            case "guardian": return "�����ƥ६�`�ǥ�����";
            case "narrator": return "�ʥ�`���`";
            case "ps": return "�ե��ȥ���å�";
            case "controlpanel": return "����ȥ�`��ѥͥ�";
            case "qq": return "QQ";
            case "7zip": return "7-Zip";
            case "mines": return "Minesweeper";
            case "xunlei": return "Ѹ��";
            case "ie": return "���󥿩`�ͥåȥ������ץ�`��`";
            case "notepad": return "��⎤";
            default: return characterId;
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
        if (dialogueText != null)
            dialogueText.text = "";

        if (characterNameText != null)
            characterNameText.text = "";

        if (characterPortrait != null)
            characterPortrait.gameObject.SetActive(false);

        if (playerInputField != null)
            playerInputField.text = "";
    }
}