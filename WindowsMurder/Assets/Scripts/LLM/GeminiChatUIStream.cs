using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeminiChatUIStream : MonoBehaviour
{
    [Header("References")]
    public GeminiAPI gemini;                 // ���ص� GeminiAPI �ű�
    public TMP_InputField userInput;         // �����
    public TMP_Text chatOutput;              // ����ı�����ʾ�Ի���
    public Button submitButton;              // �ύ��ť

    [Header("Typing Effect")]
    public float charsPerSecond = 30f;       // �����ٶ�

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

        // �����������ʾ�������
        chatOutput.text += $"\n<size=120%><color=#00FFFF>��:</color></size> {prompt}\n";
        userInput.text = "";

        // ���� Gemini �ظ�
        StartCoroutine(gemini.GenerateText(
            prompt,
            reply =>
            {
                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(reply));
            },
            err =>
            {
                chatOutput.text += $"\n<color=red>[����]</color> {err}\n";
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
        chatOutput.text += "\n"; // ����
    }
}
