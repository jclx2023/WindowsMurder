using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stage1���̿�����
/// </summary>
public class Stage1Controller : MonoBehaviour
{
    [Header("UI���")]
    public Image flashImage;            // ��˸Ч����ͼƬ���

    [Header("��Ч����")]
    public float flashDuration = 0.5f;  // ������˸����ʱ��

    // ˽�б���
    private bool waitingForClick = true;
    private bool dialogueStarted = false;
    private DialogueManager dialogueManager;

    void Start()
    {
        InitializeStage();
    }

    void Update()
    {
        // �ȴ���ҵ������λ�ÿ�ʼ�Ի�
        if (waitingForClick && Input.GetMouseButtonDown(0))
        {
            StartStage1Dialogue();
        }
    }

    /// <summary>
    /// ��ʼ��Stage1
    /// </summary>
    private void InitializeStage()
    {
        // ����DialogueManager
        dialogueManager = FindObjectOfType<DialogueManager>();

        // ȷ����˸ͼƬ��ʼ״̬Ϊ͸��
        if (flashImage != null)
        {
            Color color = flashImage.color;
            color.a = 0f;
            flashImage.color = color;
        }

        // ���ĶԻ��¼�
        SubscribeToDialogueEvents();
    }

    /// <summary>
    /// ��ʼStage1�Ի�
    /// </summary>
    private void StartStage1Dialogue()
    {
        if (dialogueStarted) return;

        waitingForClick = false;
        dialogueStarted = true;
        dialogueManager.StartDialogue("001");
    }

    /// <summary>
    /// ���ĶԻ��¼�
    /// </summary>
    private void SubscribeToDialogueEvents()
    {
        // ����DialogueUI�ĶԻ��п�ʼ�¼�
        DialogueUI.OnLineStarted += OnDialogueLineStarted;
    }

    /// <summary>
    /// �Ի��п�ʼ�¼�����
    /// </summary>
    private void OnDialogueLineStarted(string lineId, string characterId, string blockId, bool isPresetMode)
    {
        // ����Ƿ����ض��ĶԻ���ͶԻ���
        if (lineId == "2" && blockId == "001" && flashImage != null)
        {
            StartCoroutine(FlashEffect());
        }
    }

    /// <summary>
    /// ��˸Ч��Э��
    /// </summary>
    private IEnumerator FlashEffect()
    {
        if (flashImage == null) yield break;

        Debug.Log("������˸Ч��");

        // ��˸����
        for (int i = 0; i < 4; i++)
        {
            // ���� (0 -> 1)
            yield return StartCoroutine(FadeImage(0f, 1f, flashDuration));

            // ���� (1 -> 0)
            yield return StartCoroutine(FadeImage(1f, 0f, flashDuration));
        }

        Debug.Log("��˸Ч�����");
    }

    /// <summary>
    /// ͼƬ���뵭��Э��
    /// </summary>
    private IEnumerator FadeImage(float startAlpha, float endAlpha, float duration)
    {
        Color color = flashImage.color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);

            color.a = alpha;
            flashImage.color = color;

            yield return null;
        }

        // ȷ������ֵ׼ȷ
        color.a = endAlpha;
        flashImage.color = color;
    }

    /// <summary>
    /// �����¼�����
    /// </summary>
    private void OnDestroy()
    {
        // ȡ���¼�����
        DialogueUI.OnLineStarted -= OnDialogueLineStarted;
    }

    #region ���Թ���

    [ContextMenu("������˸Ч��")]
    private void TestFlashEffect()
    {
        if (flashImage != null)
        {
            StartCoroutine(FlashEffect());
        }
    }

    #endregion
}