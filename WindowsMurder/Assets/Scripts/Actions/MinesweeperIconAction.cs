using System.Linq;
using UnityEngine;

/// <summary>
/// ɨ����ϷIcon����
/// �״β��Ž��ܶԻ���������Ϸ������ֱ��������Ϸ��֧�ֶര�ڣ�
/// </summary>
public class MinesweeperIconAction : IconAction
{
    [Header("=== �Ի����� ===")]
    [SerializeField] private string introDialogueBlockId = "011";

    [Header("=== ��Ϸ���� ===")]
    [SerializeField] private GameObject minesweeperPrefab;
    [SerializeField] private Transform spawnParent;

    [Header("=== ���� ===")]
    [SerializeField] private bool hasPlayedIntro = false;

    private GameFlowController flowController;

    void Awake()
    {
        flowController = FindObjectOfType<GameFlowController>();
    }

    void OnEnable()
    {
        GameEvents.OnDialogueBlockCompleted += OnDialogueCompleted;
    }

    void OnDisable()
    {
        GameEvents.OnDialogueBlockCompleted -= OnDialogueCompleted;
    }

    void Start()
    {
        // �����ܶԻ��Ƿ������
        if (flowController != null)
        {
            hasPlayedIntro = flowController.GetCompletedBlocksSafe().Contains(introDialogueBlockId);
        }
    }

    public override void Execute()
    {
        if (hasPlayedIntro)
        {
            // �Ѳ��Ź����ܣ�ֱ��������Ϸ
            LaunchGame();
        }
        else
        {
            // �״ν��������Ž��ܶԻ�
            PlayIntroDialogue();
        }
    }

    public override bool CanExecute()
    {
        if (!base.CanExecute()) return false;

        // �Ѳ��Ž��ܣ�����Ƿ���Ԥ����
        if (hasPlayedIntro)
        {
            return minesweeperPrefab != null;
        }

        // δ���Ž��ܣ�����Ƿ��жԻ�������
        return !string.IsNullOrEmpty(introDialogueBlockId) && flowController != null;
    }

    /// <summary>
    /// ���Ž��ܶԻ�
    /// </summary>
    private void PlayIntroDialogue()
    {
        Debug.Log($"[{actionName}] ���Ž��ܶԻ�: {introDialogueBlockId}");
        flowController.StartDialogueBlock(introDialogueBlockId);
    }

    /// <summary>
    /// ����ɨ����Ϸ
    /// </summary>
    private void LaunchGame()
    {
        // ȷ�����ɸ���
        Transform parent = spawnParent;

        // ������Ϸʵ����֧�ֶര�ڣ�
        Instantiate(minesweeperPrefab, parent);
        Debug.Log($"[{actionName}] ����ɨ����Ϸ");
    }

    /// <summary>
    /// �Ի���ɻص�
    /// </summary>
    private void OnDialogueCompleted(string blockId)
    {
        if (blockId == introDialogueBlockId)
        {
            hasPlayedIntro = true;

            // ���ܶԻ���ɺ�����������Ϸ
            LaunchGame();
        }
    }
}