using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// 简化的Icon交互 - 实例化prefab并监听单个线索
/// 线索解锁后自动触发对话块
/// </summary>
public class SpawnAndWaitIconAction : IconAction
{
    [Header("=== Prefab配置 ===")]
    public GameObject prefabToSpawn;
    public Vector2 spawnPosition = Vector2.zero;
    public Canvas targetCanvas;

    [Header("=== 线索与对话配置 ===")]
    [Tooltip("要监听的线索ID")]
    public string targetClueId = "clue_example";

    [Tooltip("线索解锁后触发的对话块ID")]
    public string dialogueBlockId = "602";

    [Header("=== 功能选项 ===")]
    public bool allowMultipleSpawn = false; // 是否允许重复生成
    public bool enableDebugLog = true;

    [Header("=== 状态显示（运行时只读）===")]
    [SerializeField] private bool hasSpawned = false;
    [SerializeField] private bool isWaitingForClue = false;
    [SerializeField] private bool hasTriggeredDialogue = false;

    private GameFlowController gameFlowController;
    private Canvas canvas;
    private GameObject spawnedObject;

    #region Unity生命周期

    void Start()
    {
        gameFlowController = FindObjectOfType<GameFlowController>();
        FindTargetCanvas();
        CheckInitialState();
    }

    void OnEnable()
    {
        GameEvents.OnClueUnlocked += HandleClueUnlocked;
        DebugLog("已订阅线索解锁事件");
    }

    void OnDisable()
    {
        GameEvents.OnClueUnlocked -= HandleClueUnlocked;
        DebugLog("已取消订阅线索解锁事件");
    }

    #endregion

    #region Canvas查找

    private void FindTargetCanvas()
    {
        if (targetCanvas != null)
        {
            canvas = targetCanvas;
            DebugLog($"使用指定Canvas: {canvas.name}");
            return;
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            DebugLog($"从父级找到Canvas: {canvas.name}");
            return;
        }

        GameObject canvasObj = GameObject.FindWithTag("WindowCanvas");
        if (canvasObj != null)
        {
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas != null)
            {
                DebugLog($"通过Tag找到Canvas: {canvas.name}");
                return;
            }
        }

        DebugLog("未找到Canvas，将使用场景中的第一个Canvas");
        canvas = FindObjectOfType<Canvas>();
    }

    #endregion

    #region 初始状态检查

    private void CheckInitialState()
    {

        // 检查线索是否已经解锁
        if (gameFlowController.HasClue(targetClueId))
        {
            DebugLog($"初始化时发现线索 [{targetClueId}] 已解锁");
            hasTriggeredDialogue = true;

            // 检查对话是否已完成
            var completedBlocks = gameFlowController.GetCompletedBlocksSafe();
            if (!completedBlocks.Contains(dialogueBlockId))
            {
                DebugLog("对话未完成，准备触发对话");
                StartCoroutine(DelayedTriggerDialogue());
            }
            else
            {
                DebugLog("对话已完成");
            }
        }
    }

    #endregion

    #region 双击交互

    public override void Execute()
    {
        DebugLog($"Execute() 被调用");

        // 检查是否已经生成过
        if (hasSpawned && !allowMultipleSpawn)
        {
            DebugLog("已经生成过prefab，不允许重复生成");
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

    #region Prefab生成

    private void SpawnPrefab()
    {
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                DebugLog("错误：未找到Canvas");
                return;
            }
        }

        // 实例化prefab
        spawnedObject = Instantiate(prefabToSpawn, canvas.transform);
        spawnedObject.name = $"Spawned";

        // 设置位置
        RectTransform rectTransform = spawnedObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = spawnPosition;
            DebugLog($"已生成prefab到位置: {spawnPosition}");
        }
        else
        {
            spawnedObject.transform.position = spawnPosition;
            DebugLog($"已生成prefab到世界坐标: {spawnPosition}");
        }

        hasSpawned = true;
        isWaitingForClue = true;

        DebugLog($"等待线索 [{targetClueId}] 解锁");
    }

    #endregion

    #region 线索监听

    private void HandleClueUnlocked(string unlockedClueId)
    {
        // 检查是否是我们要监听的线索
        if (unlockedClueId != targetClueId)
        {
            return;
        }

        // 检查是否已经触发过对话
        if (hasTriggeredDialogue)
        {
            DebugLog($"线索 [{unlockedClueId}] 已处理过，忽略");
            return;
        }

        DebugLog($"检测到目标线索解锁: {unlockedClueId}");

        isWaitingForClue = false;
        hasTriggeredDialogue = true;

        // 延迟触发对话，避免与其他系统冲突
        StartCoroutine(DelayedTriggerDialogue());
    }

    private IEnumerator DelayedTriggerDialogue()
    {
        // 等待几帧，确保其他系统处理完毕
        yield return null;
        yield return null;
        yield return null;

        TriggerDialogue();
    }

    private void TriggerDialogue()
    {
        if (gameFlowController == null)
        {
            DebugLog("错误：未找到GameFlowController");
            return;
        }

        DebugLog($"触发对话块: {dialogueBlockId}");
        gameFlowController.StartDialogueBlock(dialogueBlockId);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 获取生成的对象引用
    /// </summary>
    public GameObject GetSpawnedObject()
    {
        return spawnedObject;
    }

    /// <summary>
    /// 销毁生成的对象
    /// </summary>
    public void DestroySpawnedObject()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
            hasSpawned = false;
            DebugLog("已销毁生成的对象");
        }
    }

    #endregion

    #region 调试工具

    private void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[SpawnAndWaitIconAction] {message}");
        }
    }

    #endregion
}
