using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("UI���")]
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
    private bool waitingForPlayerInput = false;  // �Ƿ�ȴ��������
    private bool isProcessingLLM = false;
    private string currentLLMCharacter;
    private Coroutine typingCoroutine;

    void Start()
    {
        // �󶨰�ť�¼�
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendMessage);

        // �������س��¼�
        if (playerInputField != null)
            playerInputField.onSubmit.AddListener(delegate { OnSendMessage(); });

        // �󶨶Ի������¼�������LLMģʽ�����¼������룩
        if (dialogueText != null)
        {
            // ���һ����ť����������
            Button dialogueClickButton = dialogueText.gameObject.GetComponent<Button>();
            if (dialogueClickButton == null)
            {
                dialogueClickButton = dialogueText.gameObject.AddComponent<Button>();
                dialogueClickButton.transition = Selectable.Transition.None; // ����ʾ�Ӿ�����
            }
            dialogueClickButton.onClick.AddListener(OnDialogueTextClicked);
        }

        // ��ʼ״̬����ʾ�ı�����������
        SetUIState(UIState.ShowingText);
    }

    /// <summary>
    /// UI״̬ö��
    /// </summary>
    private enum UIState
    {
        ShowingText,    // ��ʾ�ı���Ԥ��Ի���AI�ظ���
        WaitingInput,   // �ȴ��������
        ProcessingAI    // ����AI������
    }

    /// <summary>
    /// ����UI״̬
    /// </summary>
    private void SetUIState(UIState state)
    {
        switch (state)
        {
            case UIState.ShowingText:
                // ��ʾ�ı�����������
                SetTextActive(true);
                SetInputActive(false);
                waitingForPlayerInput = false;
                break;

            case UIState.WaitingInput:
                // �����ı�����ʾ����
                SetTextActive(false);
                SetInputActive(true);
                waitingForPlayerInput = true;

                // �۽������
                if (playerInputField != null)
                {
                    playerInputField.text = "";
                    playerInputField.placeholder.GetComponent<TextMeshProUGUI>().text = waitingForInputHint;
                    playerInputField.Select();
                }
                break;

            case UIState.ProcessingAI:
                // ��ʾ������ʾ����������
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
    /// �����ı���ʾ���״̬
    /// </summary>
    private void SetTextActive(bool active)
    {
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// �����������״̬
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
    /// ��ʼ���ŶԻ����°汾 - �����ļ����Ϳ�ID��
    /// </summary>
    public void StartDialogue(DialogueData dialogueData, string fileName, string blockId)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueUI: �Ի�����Ϊ��");
            return;
        }

        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError($"DialogueUI: �ļ������IDΪ�� (fileName: {fileName}, blockId: {blockId})");
            return;
        }

        currentDialogue = dialogueData;
        currentDialogueFileName = fileName;
        currentDialogueBlockId = blockId;
        currentLineIndex = 0;
        inLLMMode = false;

        // �����ʾ
        ClearDialogue();

        Debug.Log($"DialogueUI: ��ʼ���ŶԻ� {fileName}:{blockId}");
        ShowNextLine();
    }

    /// <summary>
    /// ��ʼ���ŶԻ������ݾɰ汾�����ط�����
    /// </summary>
    public void StartDialogue(DialogueData dialogueData)
    {
        // ���û���ṩ�ļ����Ϳ�ID��ʹ��Ĭ��ֵ
        StartDialogue(dialogueData, "unknown", dialogueData?.conversationId ?? "unknown");
        Debug.LogWarning("DialogueUI: ʹ���˾ɰ汾��StartDialogue���������鴫��fileName��blockId����");
    }

    /// <summary>
    /// ��ʾ��һ��Ի�
    /// </summary>
    public void ShowNextLine()
    {
        if (currentDialogue?.lines == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            // �Ի�����
            OnDialogueEnd();
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];

        if (line.mode) // Ԥ���ı�ģʽ
        {
            ShowPresetLine(line);
        }
        else // LLMģʽ
        {
            StartLLMMode(line);
        }
    }

    /// <summary>
    /// ��ʾԤ���ı�
    /// </summary>
    private void ShowPresetLine(DialogueLine line)
    {
        inLLMMode = false;
        SetUIState(UIState.ShowingText);

        // ���ý�ɫ��Ϣ
        SetCharacterInfo(line.characterId, line.portraitId);

        // ��ʾ�ı�
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

        // �Զ��ƽ�����һ��
        currentLineIndex++;

        // �����һ�仹��Ԥ��ģʽ���ӳٺ����
        if (currentLineIndex < currentDialogue.lines.Count &&
            currentDialogue.lines[currentLineIndex].mode)
        {
            StartCoroutine(DelayedNextLine());
        }
        else if (currentLineIndex < currentDialogue.lines.Count)
        {
            // ��һ����LLMģʽ���ȴ���ǰ�ı���ʾ��ɺ����
            StartCoroutine(WaitForTypingThenContinue());
        }
    }

    /// <summary>
    /// ��ʼLLM�Ի�ģʽ
    /// </summary>
    private void StartLLMMode(DialogueLine line)
    {
        inLLMMode = true;
        currentLLMCharacter = line.characterId;

        // ���ý�ɫ��Ϣ
        SetCharacterInfo(line.characterId, line.portraitId);

        // �л����ȴ�����״̬
        SetUIState(UIState.WaitingInput);

        Debug.Log($"����LLMģʽ���� {GetCharacterDisplayName(line.characterId)} �Ի�");
    }

    /// <summary>
    /// �Ի��ı������������LLMģʽ�����¼������룩
    /// </summary>
    private void OnDialogueTextClicked()
    {
        if (inLLMMode && !waitingForPlayerInput && !isProcessingLLM)
        {
            // ���¼�������״̬
            SetUIState(UIState.WaitingInput);
            Debug.Log("���¼�������ģʽ");
        }
    }

    /// <summary>
    /// ���ý�ɫ��Ϣ
    /// </summary>
    private void SetCharacterInfo(string characterId, string portraitId)
    {
        // ���ý�ɫ����
        if (characterNameText != null)
        {
            characterNameText.text = GetCharacterDisplayName(characterId);
        }

        // �������棨����еĻ���
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
    /// ��ȡ��ɫ��ʾ����
    /// </summary>
    private string GetCharacterDisplayName(string characterId)
    {
        // ���������չΪ�������ļ���ȡ
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

    /// <summary>
    /// ���ֻ�Ч��
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        // ���Ŵ�����Ч
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

        // ֹͣ��Ч
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        isTyping = false;
    }

    /// <summary>
    /// �ȴ�����Ч����ɺ����
    /// </summary>
    private IEnumerator WaitForTypingThenContinue()
    {
        while (isTyping)
        {
            yield return null;
        }

        yield return new WaitForSeconds(1f); // ����ȴ�1��
        ShowNextLine();
    }

    /// <summary>
    /// �ӳ���ʾ��һ��
    /// </summary>
    private IEnumerator DelayedNextLine()
    {
        while (isTyping)
        {
            yield return null;
        }

        yield return new WaitForSeconds(2f); // �ȴ�2��
        ShowNextLine();
    }

    /// <summary>
    /// ��ҷ�����Ϣ
    /// </summary>
    public void OnSendMessage()
    {
        if (!inLLMMode || !waitingForPlayerInput || playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        Debug.Log($"��ҷ�����Ϣ: {message}");

        // �л�������AI״̬
        SetUIState(UIState.ProcessingAI);
        isProcessingLLM = true;

        // ����Ƿ����LLM�Ի�
        DialogueLine currentLine = DialogueLoader.GetLineAt(currentDialogue, currentLineIndex);
        if (currentLine != null && DialogueLoader.ShouldEndLLMDialogue(message, currentLine.endKeywords))
        {
            EndLLMMode();
            return;
        }

        // ���͸�DialogueManager����
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.ProcessLLMMessage(currentLLMCharacter, message, OnLLMResponse);
        }
        else
        {
            // û��DialogueManager�Ļ�����ʾĬ�ϻظ�
            OnLLMResponse("ϵͳ�����Ҳ����Ի�������");
        }
    }

    /// <summary>
    /// LLM�ظ��ص�
    /// </summary>
    public void OnLLMResponse(string response)
    {
        if (!inLLMMode) return;

        isProcessingLLM = false;

        // ��ʾAI�ظ�
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

        Debug.Log($"AI�ظ�: {response}");
    }

    /// <summary>
    /// ��ʾ�ı���ȴ����
    /// </summary>
    private IEnumerator TypeTextThenWaitForClick(string text)
    {
        yield return StartCoroutine(TypeText(text));
        // ������ɺ󣬵ȴ���ҵ���Ի��������¼�������
    }

    /// <summary>
    /// ����LLMģʽ������Ԥ��Ի�
    /// </summary>
    private void EndLLMMode()
    {
        inLLMMode = false;
        isProcessingLLM = false;

        // �ƽ�����һ��
        currentLineIndex++;
        ShowNextLine();
    }

    /// <summary>
    /// �Ի�����
    /// </summary>
    private void OnDialogueEnd()
    {
        inLLMMode = false;
        isProcessingLLM = false;
        SetUIState(UIState.ShowingText);

        Debug.Log("DialogueUI: �Ի��������");

        // ֪ͨDialogueManager�Ի�����
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueBlockComplete(currentDialogueFileName, currentDialogueBlockId);
        }

        // ����״̬
        currentDialogueFileName = null;
        currentDialogueBlockId = null;
    }

    /// <summary>
    /// ��նԻ���ʾ
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
    /// ������ǰ����Ч��
    /// </summary>
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;

            // ֹͣ��Ч
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }

    /// <summary>
    /// ǿ�ƽ�����ǰ�Ի�
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