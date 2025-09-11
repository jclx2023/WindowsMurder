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
//    private string llmTargetCharacter; // 当前问询对象

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
//            Debug.Log("对话结束");
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

//            dialogueHistoryText.text += $"\n--- 开始问询 {llmTargetCharacter} ---\n";
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

//    // 玩家点击发送
//    public void OnSendClicked()
//    {
//        if (!inLLMMode) return;

//        string playerText = inputField.text.Trim();
//        if (string.IsNullOrEmpty(playerText)) return;

//        inputField.text = "";
//        StartCoroutine(TypeText("Player", playerText, ""));

//        // TODO: 替换为真实 LLM 调用
//        StartCoroutine(RequestLLM(playerText));
//    }

//    // 模拟 LLM 响应
//    private IEnumerator RequestLLM(string playerInput)
//    {
//        yield return new WaitForSeconds(1.5f);

//        string npcResponse = $"我收到了: {playerInput}";
//        // 假设 LLM 返回 END
//        if (playerInput.Contains("结束"))
//        {
//            npcResponse = "END";
//        }

//        if (npcResponse == "END")
//        {
//            dialogueHistoryText.text += $"--- {llmTargetCharacter} 问询结束 ---\n";
//            inLLMMode = false;
//            inputField.gameObject.SetActive(false);
//            sendButton.gameObject.SetActive(false);
//            currentLineIndex++;
//            ShowNextLine(); // 回到 preset 模式
//        }
//        else
//        {
//            StartCoroutine(TypeText(llmTargetCharacter, npcResponse, ""));
//        }
//    }
//}
