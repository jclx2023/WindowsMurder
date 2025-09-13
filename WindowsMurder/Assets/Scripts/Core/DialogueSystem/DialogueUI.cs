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
    public GameObject inputPanel;                  // ������壨LLMģʽʱ��ʾ��

    [Header("Ч������")]
    public float textSpeed = 0.05f;               // ���ֻ�Ч���ٶ�
    public bool useTypingEffect = true;           // �Ƿ�ʹ�ô��ֻ�Ч��

    [Header("��Ч")]
    public AudioSource audioSource;
    public AudioClip typingSound;                 // ���ֻ���Ч

    // ˽�б���
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool inLLMMode = false;
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

        // ��ʼ�����������
        SetInputPanelActive(false);
    }

    /// <summary>
    /// ��ʼ���ŶԻ�
    /// </summary>
    /// <param name="dialogueData">�Ի�����</param>
    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueUI: �Ի�����Ϊ��");
            return;
        }

        currentDialogue = dialogueData;
        currentLineIndex = 0;
        inLLMMode = false;

        // �����ʾ
        ClearDialogue();

        Debug.Log($"DialogueUI: ��ʼ���ŶԻ� {dialogueData.conversationId}");
        ShowNextLine();
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
        SetInputPanelActive(false);

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

        // �����һ�䲻��LLMģʽ���ӳ�һ��ʱ������
        if (currentLineIndex < currentDialogue.lines.Count &&
            currentDialogue.lines[currentLineIndex].mode)
        {
            StartCoroutine(DelayedNextLine());
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

        // ��ʾ��ʾ��Ϣ
        dialogueText.text += $"\n<color=yellow>--- ��ʼ�� {GetCharacterDisplayName(line.characterId)} �ĶԻ� ---</color>\n";
        dialogueText.text += "<color=gray>�������������...</color>\n";

        // ��ʾ�������
        SetInputPanelActive(true);

        // �۽������
        if (playerInputField != null)
        {
            playerInputField.text = "";
            playerInputField.Select();
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
    /// �ӳ���ʾ��һ��
    /// </summary>
    private IEnumerator DelayedNextLine()
    {
        yield return new WaitForSeconds(2f); // �ȴ�2��

        if (!inLLMMode) // ȷ����û����LLMģʽ
        {
            ShowNextLine();
        }
    }

    /// <summary>
    /// ��ҷ�����Ϣ
    /// </summary>
    public void OnSendMessage()
    {
        if (!inLLMMode || playerInputField == null) return;

        string message = playerInputField.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        // ��ʾ�����Ϣ
        AddPlayerMessage(message);

        // ��������
        playerInputField.text = "";

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

        // ���¾۽������
        playerInputField.Select();
    }

    /// <summary>
    /// LLM�ظ��ص�
    /// </summary>
    /// <param name="response">AI�ظ�����</param>
    public void OnLLMResponse(string response)
    {
        if (!inLLMMode) return;

        AddCharacterMessage(currentLLMCharacter, response);
    }

    /// <summary>
    /// ��������Ϣ���Ի���
    /// </summary>
    private void AddPlayerMessage(string message)
    {
        dialogueText.text += $"<color=cyan>���:</color> {message}\n";
        ScrollToBottom();
    }

    /// <summary>
    /// ��ӽ�ɫ��Ϣ���Ի���
    /// </summary>
    private void AddCharacterMessage(string characterId, string message)
    {
        string characterName = GetCharacterDisplayName(characterId);
        dialogueText.text += $"<color=orange>{characterName}:</color> {message}\n";
        ScrollToBottom();
    }

    /// <summary>
    /// ����LLMģʽ������Ԥ��Ի�
    /// </summary>
    private void EndLLMMode()
    {
        inLLMMode = false;
        SetInputPanelActive(false);

        dialogueText.text += $"<color=yellow>--- �� {GetCharacterDisplayName(currentLLMCharacter)} �ĶԻ����� ---</color>\n";

        // �ƽ�����һ��
        currentLineIndex++;
        StartCoroutine(DelayedNextLine());
    }

    /// <summary>
    /// �Ի�����
    /// </summary>
    private void OnDialogueEnd()
    {
        inLLMMode = false;
        SetInputPanelActive(false);

        dialogueText.text += "\n<color=green>--- �Ի����� ---</color>";

        Debug.Log("DialogueUI: �Ի��������");

        // ֪ͨDialogueManager�Ի�����
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueComplete(currentDialogue.conversationId);
        }
    }

    /// <summary>
    /// �������������ʾ״̬
    /// </summary>
    private void SetInputPanelActive(bool active)
    {
        if (inputPanel != null)
        {
            inputPanel.SetActive(active);
        }
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
    }

    /// <summary>
    /// �������ײ�
    /// </summary>
    private void ScrollToBottom()
    {
        // �����ScrollRect������������ײ�
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
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
        currentLineIndex = 0;
        inLLMMode = false;

        SetInputPanelActive(false);
        ClearDialogue();
    }

#if UNITY_EDITOR
    /// <summary>
    /// �༭�����Թ���
    /// </summary>
    [ContextMenu("���ԶԻ�")]
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