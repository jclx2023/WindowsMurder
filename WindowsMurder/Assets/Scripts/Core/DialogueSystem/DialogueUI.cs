//using TMPro;
//using UnityEngine;

//public class DialogueUI : MonoBehaviour
//{
//    [Header("UI References")]
//    public TextMeshProUGUI characterNameText;
//    public TextMeshProUGUI dialogueHistoryText;
//    public Image portraitImage;
//    public TMP_InputField inputField;
//    public Button sendButton;

//    [Header("Settings")]
//    public float textSpeed = 0.05f;

//    private DialogueData currentDialogue;
//    private int currentLineIndex = 0;
//    private bool inLLMMode = false;
//    private string llmTargetCharacter; // ��ǰ��ѯ����

//    public void StartDialogue(DialogueData data)
//    {
//        currentDialogue = data;
//        currentLineIndex = 0;
//        ShowNextLine();
//    }

//    private void ShowNextLine()
//    {
//        if (currentLineIndex >= currentDialogue.lines.Count)
//        {
//            Debug.Log("�Ի�����");
//            return;
//        }

//        DialogueLine line = currentDialogue.lines[currentLineIndex];

//        if (line.mode) // preset
//        {
//            inputField.gameObject.SetActive(false);
//            sendButton.gameObject.SetActive(false);

//            StartCoroutine(TypeText(line.characterId, line.text, line.portraitId));
//            currentLineIndex++;
//        }
//        else // llm
//        {
//            inLLMMode = true;
//            llmTargetCharacter = line.characterId;

//            inputField.gameObject.SetActive(true);
//            sendButton.gameObject.SetActive(true);

//            dialogueHistoryText.text += $"\n--- ��ʼ��ѯ {llmTargetCharacter} ---\n";
//        }
//    }

//    private IEnumerator TypeText(string characterId, string text, string portraitId)
//    {
//        characterNameText.text = characterId;
//        dialogueHistoryText.text += $"{characterId}: ";

//        foreach (char c in text.ToCharArray())
//        {
//            dialogueHistoryText.text += c;
//            yield return new WaitForSeconds(textSpeed);
//        }
//        dialogueHistoryText.text += "\n";
//    }

//    // ��ҵ������
//    public void OnSendClicked()
//    {
//        if (!inLLMMode) return;

//        string playerText = inputField.text.Trim();
//        if (string.IsNullOrEmpty(playerText)) return;

//        inputField.text = "";
//        StartCoroutine(TypeText("Player", playerText, ""));

//        // TODO: �滻Ϊ��ʵ LLM ����
//        StartCoroutine(RequestLLM(playerText));
//    }

//    // ģ�� LLM ��Ӧ
//    private IEnumerator RequestLLM(string playerInput)
//    {
//        yield return new WaitForSeconds(1.5f);

//        string npcResponse = $"���յ���: {playerInput}";
//        // ���� LLM ���� END
//        if (playerInput.Contains("����"))
//        {
//            npcResponse = "END";
//        }

//        if (npcResponse == "END")
//        {
//            dialogueHistoryText.text += $"--- {llmTargetCharacter} ��ѯ���� ---\n";
//            inLLMMode = false;
//            inputField.gameObject.SetActive(false);
//            sendButton.gameObject.SetActive(false);
//            currentLineIndex++;
//            ShowNextLine(); // �ص� preset ģʽ
//        }
//        else
//        {
//            StartCoroutine(TypeText(llmTargetCharacter, npcResponse, ""));
//        }
//    }
//}
