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

    [Header("���ֻ�Ч������")]
    public float textSpeed = 0.05f;
    public bool useTypingEffect = true;

    [Header("���ֶ���Ч��")]
    public bool enableBounceEffect = true;           // �Ƿ����õ���Ч��
    public float bounceHeight = 8f;                   // �����߶ȣ����أ�
    public float bounceDuration = 0.3f;               // ��������ʱ��
    public AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // ��������

    public bool enableRandomOffset = true;            // �Ƿ��������ƫ��
    public float randomOffsetRange = 2f;              // ���ƫ�Ʒ�Χ�����أ�
    public float offsetSmoothTime = 0.15f;            // ƫ��ƽ��ʱ��

    [Header("��Ч")]
    public AudioSource audioSource;
    public AudioClip typingSound;

    [Header("LLM������ʾ")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    // �¼�ϵͳ
    public static event System.Action<string, string, string, bool> OnLineStarted;
    public static event System.Action<string, string, string, bool> OnLineCompleted;
    public static event System.Action<string, string> OnDialogueBlockStarted;
    public static event System.Action<string, string> OnDialogueBlockEnded;

    // ��������
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

    // ˽�б���
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
            Debug.LogError("DialogueUI: �Ի�����Ϊ��");
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

        OnLineStarted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);

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
            OnLineCompleted?.Invoke(line.id, line.characterId, currentDialogueBlockId, true);
        }
    }

    /// <summary>
    /// ���ֻ�Ч�� + ���������ƫ�ƶ���
    /// </summary>
    private IEnumerator TypeText(string text, string lineId = null, string characterId = null, bool isPresetMode = true)
    {
        isTyping = true;
        waitingForContinue = false;
        dialogueText.text = "";

        // ���Ŵ�����Ч
        if (audioSource != null && typingSound != null)
        {
            audioSource.clip = typingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // ��������ı�
        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];

            // ǿ�Ƹ��������Ի�ȡ���ַ���Ϣ
            dialogueText.ForceMeshUpdate();

            // Ϊ����ӵ��ַ�Ӧ�ö���Ч��
            if (enableBounceEffect || enableRandomOffset)
            {
                StartCoroutine(AnimateCharacter(i));
            }

            yield return new WaitForSeconds(textSpeed);
        }

        // ֹͣ��Ч
        if (audioSource != null)
            audioSource.Stop();

        isTyping = false;
        waitingForContinue = true;

        if (!string.IsNullOrEmpty(lineId))
        {
            OnLineCompleted?.Invoke(lineId, characterId ?? "", currentDialogueBlockId, isPresetMode);
        }
    }

    /// <summary>
    /// Ϊ�����ַ���ӵ�����ƫ�ƶ���
    /// </summary>
    private IEnumerator AnimateCharacter(int charIndex)
    {
        // �������ƫ�ƣ�������ã�
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

            // ����������Ϣ
            dialogueText.ForceMeshUpdate();
            TMP_TextInfo textInfo = dialogueText.textInfo;

            // ����ַ������Ƿ���Ч
            if (charIndex >= textInfo.characterCount || !textInfo.characterInfo[charIndex].isVisible)
                yield break;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // ��ȡ��������
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // ���㵯��ƫ��
            Vector3 bounceOffset = Vector3.zero;
            if (enableBounceEffect)
            {
                float bounceValue = bounceCurve.Evaluate(progress);
                float currentHeight = bounceHeight * (1 - bounceValue);
                bounceOffset = new Vector3(0, currentHeight, 0);
            }

            // �������ƫ�ƣ���ƽ�����ɣ�
            Vector3 currentRandomOffset = Vector3.zero;
            if (enableRandomOffset)
            {
                float offsetProgress = Mathf.SmoothStep(1f, 0f, progress);
                currentRandomOffset = randomOffset * offsetProgress;
            }

            // �����ƫ��
            Vector3 totalOffset = bounceOffset + currentRandomOffset;

            // ��ȡ�ַ���4�������ԭʼλ��
            Vector3 bottomLeft = vertices[vertexIndex + 0];
            Vector3 topLeft = vertices[vertexIndex + 1];
            Vector3 topRight = vertices[vertexIndex + 2];
            Vector3 bottomRight = vertices[vertexIndex + 3];

            // �����ַ����ĵ�
            Vector3 center = (bottomLeft + topLeft + topRight + bottomRight) / 4f;

            // Ӧ��ƫ�Ƶ����ж���
            vertices[vertexIndex + 0] = bottomLeft + totalOffset;
            vertices[vertexIndex + 1] = topLeft + totalOffset;
            vertices[vertexIndex + 2] = topRight + totalOffset;
            vertices[vertexIndex + 3] = bottomRight + totalOffset;

            // ��������
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);

            yield return null;
        }

        // ����������ȷ���ַ��ص�����λ��
        dialogueText.ForceMeshUpdate();
        TMP_TextInfo finalTextInfo = dialogueText.textInfo;

        if (charIndex < finalTextInfo.characterCount && finalTextInfo.characterInfo[charIndex].isVisible)
        {
            TMP_CharacterInfo finalCharInfo = finalTextInfo.characterInfo[charIndex];
            int finalMaterialIndex = finalCharInfo.materialReferenceIndex;
            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
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

        // ֹͣ�����ַ�����
        StopAllCoroutines();

        dialogueText.text = fullCurrentText;
        dialogueText.ForceMeshUpdate();

        isTyping = false;
        waitingForContinue = true;

        DialogueLine currentLine = CurrentLine;
        if (currentLine != null)
        {
            OnLineCompleted?.Invoke(currentLine.id, currentLine.characterId, currentDialogueBlockId, currentLine.mode);
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
            case "mine": return "ɨ��";
            case "xunlei": return "Ѹ��";
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
            case "me": return "˽";
            case "guardian": return "�����ƥ६�`�ǥ�����";
            case "narrator": return "�ʥ�`���`";
            case "ps": return "�ե��ȥ���å�";
            case "controlpanel": return "����ȥ�`��ѥͥ�";
            case "qq": return "QQ";
            case "7zip": return "7-Zip";
            case "mine": return "Minesweeper";
            case "xunlei": return "Ѹ��";
            case "ie": return "���󥿩`�ͥåȥ������ץ�`��`";
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

        OnLineStarted?.Invoke(line.id, line.characterId, currentDialogueBlockId, false);

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
            OnLineCompleted?.Invoke(currentLine?.id ?? "", currentLine?.characterId ?? "", currentDialogueBlockId, false);
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