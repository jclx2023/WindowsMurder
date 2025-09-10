using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeminiChatUIStream : MonoBehaviour
{
    [Header("References")]
    public GeminiAPI gemini;                 // 挂载的 GeminiAPI 脚本
    public TMP_InputField userInput;         // 输入框
    public TMP_Text chatOutput;              // 输出文本（显示对话）
    public Button submitButton;              // 提交按钮

    [Header("Typing Effect")]
    public float charsPerSecond = 30f;       // 打字速度

    private Coroutine typingCoroutine;

    private void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
    }

    private void OnDestroy()
    {
        submitButton.onClick.RemoveListener(OnSubmit);
    }

    private void OnSubmit()
    {
        string prompt = userInput.text;
        if (string.IsNullOrWhiteSpace(prompt)) return;

        // 在输出里先显示玩家输入
        chatOutput.text += $"\n<size=120%><color=#00FFFF>你:</color></size> {prompt}\n";
        userInput.text = "";

        // 请求 Gemini 回复
        StartCoroutine(gemini.GenerateText(
            prompt,
            reply =>
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(reply));
            },
            err =>
            {
                chatOutput.text += $"\n<color=red>[错误]</color> {err}\n";
            }
        ));
    }

    private IEnumerator TypeText(string fullText)
    {
        chatOutput.text += "<size=120%><color=#FFCC00>Gemini:</color></size> ";
        int i = 0;
        while (i < fullText.Length)
        {
            int charsToAdd = Mathf.Max(1, Mathf.RoundToInt(charsPerSecond * Time.deltaTime));
            int nextLen = Mathf.Min(i + charsToAdd, fullText.Length);

            chatOutput.text += fullText.Substring(i, nextLen - i);
            i = nextLen;
            yield return null;
        }
        chatOutput.text += "\n"; // 换行
    }
}
