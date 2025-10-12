using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// �򻯵�Icon���� - ʵ����prefab��������������
/// �����������Զ������Ի���
/// </summary>
public class SpawnAndWaitIconAction : IconAction
{
    [Header("=== Prefab���� ===")]
    public GameObject prefabToSpawn;
    public Vector2 spawnPosition = Vector2.zero;
    public Canvas targetCanvas;

    [Header("=== ������Ի����� ===")]
    [Tooltip("Ҫ����������ID")]
    public string targetClueId = "clue_example";

    [Tooltip("���������󴥷��ĶԻ���ID")]
    public string dialogueBlockId = "602";

    [Header("=== ����ѡ�� ===")]
    public bool allowMultipleSpawn = false; // �Ƿ������ظ�����
    public bool enableDebugLog = true;

    [Header("=== ״̬��ʾ������ʱֻ����===")]
    [SerializeField] private bool hasSpawned = false;
    [SerializeField] private bool isWaitingForClue = false;
    [SerializeField] private bool hasTriggeredDialogue = false;

    private GameFlowController gameFlowController;
    private Canvas canvas;
    private GameObject spawnedObject;

    #region Unity��������

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        FindTargetCanvas();
        CheckInitialState();
    }

    void OnEnable()
    {
        GameEvents.OnClueUnlocked += HandleClueUnlocked;
        DebugLog("�Ѷ������������¼�");
    }

    void OnDisable()
    {
        GameEvents.OnClueUnlocked -= HandleClueUnlocked;
        DebugLog("��ȡ���������������¼�");
    }

    #endregion

    #region Canvas����

    private void FindTargetCanvas()
    {
        if (targetCanvas != null)
        {
            canvas = targetCanvas;
            DebugLog($"ʹ��ָ��Canvas: {canvas.name}");
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            DebugLog($"�Ӹ����ҵ�Canvas: {canvas.name}");
            return;
        }

        GameObject canvasObj = GameObject.FindWithTag("WindowCanvas");
        if (canvasObj != null)
        {
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                DebugLog($"ͨ��Tag�ҵ�Canvas: {canvas.name}");
                return;
            }
        }

        DebugLog("δ�ҵ�Canvas����ʹ�ó����еĵ�һ��Canvas");
        canvas = FindObjectOfType<Canvas>();
    }

    #endregion

    #region ��ʼ״̬���

    private void CheckInitialState()
    {

        // ��������Ƿ��Ѿ�����
        if (gameFlowController.HasClue(targetClueId))
        {
            DebugLog($"��ʼ��ʱ�������� [{targetClueId}] �ѽ���");
            hasTriggeredDialogue = true;

            // ���Ի��Ƿ������
            var completedBlocks = gameFlowController.GetCompletedBlocksSafe();
            if (!completedBlocks.Contains(dialogueBlockId))
            {
                DebugLog("�Ի�δ��ɣ�׼�������Ի�");
                StartCoroutine(DelayedTriggerDialogue());
            }
            else
            {
                DebugLog("�Ի������");
            }
        }
    }

    #endregion

    #region ˫������

    public override void Execute()
    {
        DebugLog($"Execute() ������");

        // ����Ƿ��Ѿ����ɹ�
        if (hasSpawned && !allowMultipleSpawn)
        {
            DebugLog("�Ѿ����ɹ�prefab���������ظ�����");
            return;
        }

        SpawnPrefab();
    }

    public override bool CanExecute()
    {
        if (hasSpawned && !allowMultipleSpawn)
        {
            return false;
        }

        return base.CanExecute();
    }

    #endregion

    #region Prefab����

    private void SpawnPrefab()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                DebugLog("����δ�ҵ�Canvas");
                return;
            }
        }

        // ʵ����prefab
        spawnedObject = Instantiate(prefabToSpawn, canvas.transform);
        spawnedObject.name = $"Spawned";

        // ����λ��
        RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = spawnPosition;
            DebugLog($"������prefab��λ��: {spawnPosition}");
        }
        else
        {
            spawnedObject.transform.position = spawnPosition;
            DebugLog($"������prefab����������: {spawnPosition}");
        }

        hasSpawned = true;
        isWaitingForClue = true;

        DebugLog($"�ȴ����� [{targetClueId}] ����");
    }

    #endregion

    #region ��������

    private void HandleClueUnlocked(string unlockedClueId)
    {
        // ����Ƿ�������Ҫ����������
        if (unlockedClueId != targetClueId)
        {
            return;
        }

        // ����Ƿ��Ѿ��������Ի�
        if (hasTriggeredDialogue)
        {
            DebugLog($"���� [{unlockedClueId}] �Ѵ����������");
            return;
        }

        DebugLog($"��⵽Ŀ����������: {unlockedClueId}");

        isWaitingForClue = false;
        hasTriggeredDialogue = true;

        // �ӳٴ����Ի�������������ϵͳ��ͻ
        StartCoroutine(DelayedTriggerDialogue());
    }

    private IEnumerator DelayedTriggerDialogue()
    {
        // �ȴ���֡��ȷ������ϵͳ�������
        yield return null;
        yield return null;
        yield return null;

        TriggerDialogue();
    }

    private void TriggerDialogue()
    {
        if (gameFlowController == null)
        {
            DebugLog("����δ�ҵ�GameFlowController");
            return;
        }

        DebugLog($"�����Ի���: {dialogueBlockId}");
        gameFlowController.StartDialogueBlock(dialogueBlockId);
    }

    #endregion

    #region ��������

    /// <summary>
    /// ��ȡ���ɵĶ�������
    /// </summary>
    public GameObject GetSpawnedObject()
    {
        return spawnedObject;
    }

    /// <summary>
    /// �������ɵĶ���
    /// </summary>
    public void DestroySpawnedObject()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
            hasSpawned = false;
            DebugLog("���������ɵĶ���");
        }
    }

    #endregion

    #region ���Թ���

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SpawnAndWaitIconAction] {message}");
        }
    }

    #endregion
}