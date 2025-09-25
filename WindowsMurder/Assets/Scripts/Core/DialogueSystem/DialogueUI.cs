using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI���")]
    public GameObject dialoguePanel;               // �����Ի������
    public TextMeshProUGUI characterNameText;      // ��ɫ����
    public TextMeshProUGUI dialogueText;           // �Ի��ı���ʾ��
    public Image characterPortrait;                // ��ɫ����
    public TMP_InputField playerInputField;        // ��������
    public Button sendButton;                      // ���Ͱ�ť

    [Header("Ч������")]
    public float textSpeed = 0.05f;               // ���ֻ�Ч���ٶ�
    public bool useTypingEffect = true;           // �Ƿ�ʹ�ô��ֻ�Ч��

    [Header("��Ч")]
    public AudioSource audioSource;
    public AudioClip typingSound;                 // ���ֻ���Ч

    [Header("LLM������ʾ")]
    public string waitingForInputHint = "Please enter your question...";
    public string waitingForAIHint = "Thinking...";

    // ˽�б���
    private DialogueData currentDialogue;
    private string currentDialogueFileName;        // ��ǰ�Ի����ļ���
    private string currentDialogueBlockId;         // ��ǰ�Ի��Ŀ�ID
    private int currentLineIndex = 0;

    private bool isTyping = false;
    private bool inLLMMode = false;
    private bool waitingForPlayerInput = false;
    private bool isProcessingLLM = false;
    private bool waitingForContinue = false;       // �Ƿ�ȴ���ҵ������

    private string currentLLMCharacter;
    private Coroutine typingCoroutine;
    private string fullCurrentText = "";           // ��ǰ�����ı�����

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

    #region �Ի�����ʾ����

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
        ShowingText,    // ��ʾ�ı���Ԥ��Ի���AI�ظ���
        WaitingInput,   // �ȴ��������
        ProcessingAI    // ����AI������
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

    #region �Ի�����

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

        if (line.mode) // Ԥ���ı�
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

        // ��ͨ�Ի�ģʽ�������������һ��
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
            case "RecycleBin": return "����վ";
            case "TaskManager": return "���������";
            case "ControlPanel": return "�������";
            case "MyComputer": return "�ҵĵ���";
            case "me": return "��";
            case "guardian": return "ϵͳ�ػ�";
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

    #region LLMģʽ������ԭ�߼���

    private void StartLLMMode(DialogueLine line)
    {
        inLLMMode = true;
        currentLLMCharacter = line.characterId;

        SetCharacterInfo(line.characterId, line.portraitId);

        // ʹ��HistoryManager��ʼLLM�Ự��ֱ�ӷ��ͳ�ʼprompt
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null && dialogueManager.historyManager != null)
        {
            SetUIState(UIState.ProcessingAI); // ��ʾ"˼����..."

            dialogueManager.historyManager.StartLLMSession(
                line.text,                    // ��ʼprompt
                OnInitialLLMResponse,         // �����ʼ�ظ��Ļص�
                dialogueManager               // ����DialogueManager����
            );
        }
    }
    private void OnInitialLLMResponse(string response)
    {
        OnLLMResponse(response); // �������еĻظ������߼�
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
            waitingForContinue = true; // ���õȴ����״̬
        }
    }

    private void EndLLMMode()
    {
        // ������ʷ�������ĻỰ
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
