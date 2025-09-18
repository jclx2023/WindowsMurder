using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Windows XP风格的读条动画控制器 - 简化版
/// </summary>
public class WinXPLoadingBar : MonoBehaviour
{
    [Header("读条设置")]
    [SerializeField] private GameObject unitPrefab;          // Unit预制体
    [SerializeField] private Transform unitContainer;        // Unit容器（有Horizontal Layout的父物体）
    [SerializeField] private int maxUnits = 10;             // 最大Unit数量

    [Header("动画参数")]
    [SerializeField] private float unitSpawnInterval = 0.2f; // Unit生成间隔（秒）
    [SerializeField] private float cycleDelay = 0.3f;        // 循环间隔（清空后再开始的延迟时间）
    [SerializeField] private bool autoStart = true;          // 是否自动开始

    // 私有变量
    private List<GameObject> currentUnits = new List<GameObject>();
    private Coroutine loadingCoroutine;
    private bool isLoading = false;

    void Start()
    {
        if (autoStart)
        {
            StartLoading();
        }
    }

    /// <summary>
    /// 开始读条动画
    /// </summary>
    public void StartLoading()
    {
        if (isLoading) return;

        isLoading = true;

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(LoadingAnimation());
    }

    /// <summary>
    /// 停止读条动画
    /// </summary>
    public void StopLoading()
    {
        isLoading = false;

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }

        ClearAllUnits();
    }

    /// <summary>
    /// 读条动画协程
    /// </summary>
    private IEnumerator LoadingAnimation()
    {
        while (isLoading)
        {
            // 生成Units
            for (int i = 0; i < maxUnits; i++)
            {
                if (!isLoading) break;

                SpawnUnit();
                yield return new WaitForSeconds(unitSpawnInterval);
            }

            // 清空所有Unit
            ClearAllUnits();

            // 等待循环间隔
            yield return new WaitForSeconds(cycleDelay);
        }
    }

    /// <summary>
    /// 生成单个Unit
    /// </summary>
    private void SpawnUnit()
    {
        if (unitPrefab == null || unitContainer == null) return;

        GameObject newUnit = Instantiate(unitPrefab, unitContainer);
        currentUnits.Add(newUnit);
    }

    /// <summary>
    /// 清空所有Unit
    /// </summary>
    private void ClearAllUnits()
    {
        foreach (GameObject unit in currentUnits)
        {
            if (unit != null)
            {
                DestroyImmediate(unit);
            }
        }

        currentUnits.Clear();
    }

    void OnDestroy()
    {
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        ClearAllUnits();
    }
}