using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Windows XP风格的读条动画控制器
/// 支持无限循环播放和指定次数播放
/// </summary>
public class WinXPLoadingBar : MonoBehaviour
{
    #region 配置参数

    [Header("=== Unit设置 ===")]
    [Tooltip("Unit预制体")]
    public GameObject unitPrefab;

    [Tooltip("Unit容器（挂载Horizontal Layout的父物体）")]
    public Transform unitContainer;

    [Tooltip("最大Unit数量")]
    public int maxUnits = 10;

    [Header("=== 动画参数 ===")]
    [Tooltip("每个Unit生成的间隔时间（秒）")]
    public float unitSpawnInterval = 0.2f;

    [Tooltip("一次循环完成后，清空并重新开始的延迟时间")]
    public float cycleDelay = 0.3f;

    [Header("=== 自动播放 ===")]
    [Tooltip("启动时是否自动开始无限循环播放")]
    public bool autoStart = true;

    [Header("=== 调试 ===")]
    [SerializeField] private bool enableDebugLog = false;

    #endregion

    #region 私有变量

    private List<GameObject> currentUnits = new List<GameObject>();
    private Coroutine loadingCoroutine;
    private bool isLoading = false;

    #endregion

    #region Unity生命周期

    void Start()
    {
        if (autoStart)
        {
            StartLoading();
        }
    }

    void OnDestroy()
    {
        StopLoading();
        ClearAllUnits();
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 开始无限循环播放
    /// </summary>
    public void StartLoading()
    {
        if (isLoading)
        {
            Log("已在播放中，跳过重复启动");
            return;
        }

        isLoading = true;

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(InfiniteLoadingCoroutine());
        Log("开始无限循环播放");
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopLoading()
    {
        if (!isLoading)
        {
            return;
        }

        isLoading = false;

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        ClearAllUnits();
        Log("停止播放");
    }

    /// <summary>
    /// 播放指定次数的循环（用于结局演出等场景）
    /// </summary>
    public IEnumerator PlayCycles(int cycleCount)
    {
        if (isLoading)
        {
            Log("当前正在播放，先停止");
            StopLoading();
        }

        if (cycleCount <= 0)
        {
            LogWarning($"循环次数必须大于0，当前值: {cycleCount}");
            yield break;
        }

        isLoading = true;
        Log($"开始播放 {cycleCount} 次循环");

        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            Log($"循环 {cycle + 1}/{cycleCount}");

            // 播放一次完整循环
            yield return PlaySingleCycle();

            // 最后一次循环不需要延迟
            if (cycle < cycleCount - 1)
            {
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        isLoading = false;
        ClearAllUnits();
        Log($"完成 {cycleCount} 次循环播放");
    }

    #endregion

    #region 核心逻辑

    /// <summary>
    /// 无限循环播放协程
    /// </summary>
    private IEnumerator InfiniteLoadingCoroutine()
    {
        while (isLoading)
        {
            // 播放一次完整循环
            yield return PlaySingleCycle();

            // 等待循环延迟
            yield return new WaitForSeconds(cycleDelay);
        }
    }

    /// <summary>
    /// 播放单次循环
    /// </summary>
    private IEnumerator PlaySingleCycle()
    {
        // 生成所有Units
        for (int i = 0; i < maxUnits; i++)
        {
            if (!isLoading) yield break; // 如果被停止，立即退出

            SpawnUnit();
            yield return new WaitForSeconds(unitSpawnInterval);
        }

        // 清空所有Units
        ClearAllUnits();
    }

    /// <summary>
    /// 生成一个Unit
    /// </summary>
    private void SpawnUnit()
    {
        if (unitPrefab == null || unitContainer == null)
        {
            LogWarning("unitPrefab或unitContainer未设置");
            return;
        }

        GameObject newUnit = Instantiate(unitPrefab, unitContainer);
        currentUnits.Add(newUnit);
    }

    /// <summary>
    /// 清空所有已生成的Units
    /// </summary>
    private void ClearAllUnits()
    {
        foreach (GameObject unit in currentUnits)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }

        currentUnits.Clear();
    }

    #endregion

    #region 日志工具

    private void Log(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[WinXPLoadingBar] {message}");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLog)
        {
            Debug.LogWarning($"[WinXPLoadingBar] {message}");
        }
    }

    #endregion
}
