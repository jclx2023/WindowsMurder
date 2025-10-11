using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ŀ����ɫ����
/// </summary>
[System.Serializable]
public class TargetColorConfig
{
    [Header("��ʶ")]
    public string colorId = "dark_red";

    [Header("��ɫ����")]
    public Color targetColor = new Color(0.545f, 0, 0); // RGB(139, 0, 0)
    [Range(0f, 0.2f)]
    public float tolerance = 0.05f;

    [Header("�Ի�����")]
    public string dialogueBlockId = "dialogue_dark_red";

    [Header("�����������ã�")]
    [TextArea(2, 3)]
    public string description = "���ϵ����ɫѪ��";
}

/// <summary>
/// ��ɫ֤�ݿ����� - ����ȡɫ֤������
/// </summary>
public class ColorEvidenceController : MonoBehaviour
{
    [Header("=== Ŀ����ɫ ===")]
    [SerializeField] private List<TargetColorConfig> targetColors = new List<TargetColorConfig>();

    [Header("=== ������� ===")]
    [SerializeField] private string completionDialogueId = "dialogue_blood_complete";
    [SerializeField] private string unlockedClueId = "evidence_fake_blood";
    [SerializeField] private bool requireAllColors = true;

    [Header("=== ���� ===")]
    [SerializeField] private GameFlowController gameFlowController;

    [Header("=== ����ʱ״̬��ֻ����===")]
    [SerializeField] private List<string> pickedColorIdsList = new List<string>();
    [SerializeField] private bool isCompleted = false;
    [SerializeField] private int pickAttempts = 0;

    [Header("=== ���� ===")]
    [SerializeField] private bool debugMode = true;

    // �ڲ�״̬
    private HashSet<string> pickedColorIds = new HashSet<string>();

    #region ��������

    void Awake()
    {
        if (gameFlowController == null)
        {
            gameFlowController = FindObjectOfType<GameFlowController>();
        }
    }

    void OnEnable()
    {
        // ����ȫ��ȡɫ�¼�
        EyedropperTool.OnAnyColorPicked += HandleColorPicked;
    }

    void OnDisable()
    {
        // ȡ������
        EyedropperTool.OnAnyColorPicked -= HandleColorPicked;
    }

    void Start()
    {
    }

    #endregion

    #region �����߼�

    /// <summary>
    /// ����ȡɫ�¼�
    /// </summary>
    private void HandleColorPicked(Color pickedColor)
    {
        // �������ɣ����Ժ���ȡɫ
        if (isCompleted)
        {
            LogDebug("֤������ɣ�����ȡɫ");
            return;
        }

        pickAttempts++;
        LogDebug($"�յ�ȡɫ�¼� #{pickAttempts}: #{ColorUtility.ToHtmlStringRGB(pickedColor)}");

        // ����Ŀ����ɫ������ƥ��
        foreach (var target in targetColors)
        {
            if (IsColorMatch(pickedColor, target.targetColor, target.tolerance))
            {
                LogDebug($"ƥ�䵽Ŀ����ɫ: {target.colorId}");
                OnTargetColorPicked(target);
                return; // ֻƥ��һ���͹���
            }
        }

        // û��ƥ�䵽�κ�Ŀ��
        LogDebug("δƥ�䵽�κ�Ŀ����ɫ");
    }

    /// <summary>
    /// ��ɫƥ���ж�
    /// </summary>
    private bool IsColorMatch(Color pickedColor, Color targetColor, float tolerance)
    {
        // ����ŷ�Ͼ���
        float distance = Mathf.Sqrt(
            Mathf.Pow(pickedColor.r - targetColor.r, 2) +
            Mathf.Pow(pickedColor.g - targetColor.g, 2) +
            Mathf.Pow(pickedColor.b - targetColor.b, 2)
        );

        bool isMatch = distance < tolerance;

        if (debugMode)
        {
            LogDebug($"��ɫ����: {distance:F4}, �ݲ�: {tolerance}, ƥ��: {isMatch}");
        }

        return isMatch;
    }

    /// <summary>
    /// Ŀ����ɫ���д���
    /// </summary>
    private void OnTargetColorPicked(TargetColorConfig target)
    {
        // ����Ƿ��Ѿ�ȡ��
        if (pickedColorIds.Contains(target.colorId))
        {
            LogDebug($"��ɫ [{target.colorId}] �Ѿ�ȡ��������");
            return;
        }

        // ��¼��ȡ������ɫ
        pickedColorIds.Add(target.colorId);
        pickedColorIdsList.Add(target.colorId);
        LogDebug($"��¼��ɫ: {target.colorId}����ǰ��ȡ {pickedColorIds.Count}/{targetColors.Count}");

        if (CheckCompletion())
        {
            // ����ˣ�ֻ������ɶԻ��������ŵ�����ɫ�ĶԻ�
            OnEvidenceCompleted();
        }
        else
        {
            // δ��ɣ����ŵ�����ɫ�ĶԻ�
            TriggerDialogue(target.dialogueBlockId);
        }
    }

    /// <summary>
    /// ����Ƿ���ɣ������Ƿ���ɣ�
    /// </summary>
    private bool CheckCompletion()
    {
        if (!requireAllColors)
        {
            // �����Ҫ��ȫ��ȡ����ȡ������һ��������ɣ��ݲ�ʹ�ã�
            return pickedColorIds.Count > 0;
        }

        // ����Ƿ�����Ŀ����ɫ��ȡ����
        foreach (var target in targetColors)
        {
            if (!pickedColorIds.Contains(target.colorId))
            {
                return false; // ����δȡ����
            }
        }

        return true; // ȫ��ȡ��
    }

    /// <summary>
    /// ֤����ɴ���
    /// </summary>
    private void OnEvidenceCompleted()
    {
        if (isCompleted)
        {
            LogDebug("֤���ѱ��Ϊ��ɣ������ظ�����");
            return;
        }

        isCompleted = true;

        // ������ɶԻ�
        TriggerDialogue(completionDialogueId);

        // ��������
        if (!string.IsNullOrEmpty(unlockedClueId))
        {
            UnlockClue(unlockedClueId);
        }
    }

    #endregion

    #region GameFlowController����

    /// <summary>
    /// �����Ի���
    /// </summary>
    private void TriggerDialogue(string dialogueBlockId)
    {
        LogDebug($"�����Ի���: {dialogueBlockId}");
        gameFlowController.StartDialogueBlock(dialogueBlockId);
    }

    /// <summary>
    /// ��������
    /// </summary>
    private void UnlockClue(string clueId)
    {

        LogDebug($"��������: {clueId}");
        gameFlowController.UnlockClue(clueId);
    }

    #endregion

    #region ���Թ���
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            //Debug.Log($"[ColorEvidence:] {message}");
        }
    }

    #endregion
}