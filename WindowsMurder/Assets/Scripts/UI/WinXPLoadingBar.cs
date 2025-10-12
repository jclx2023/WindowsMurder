using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Windows XP���Ķ�������������
/// ֧������ѭ�����ź�ָ����������
/// </summary>
public class WinXPLoadingBar : MonoBehaviour
{
    #region ���ò���

    [Header("=== Unit���� ===")]
    [Tooltip("UnitԤ����")]
    public GameObject unitPrefab;

    [Tooltip("Unit����������Horizontal Layout�ĸ����壩")]
    public Transform unitContainer;

    [Tooltip("���Unit����")]
    public int maxUnits = 10;

    [Header("=== �������� ===")]
    [Tooltip("ÿ��Unit���ɵļ��ʱ�䣨�룩")]
    public float unitSpawnInterval = 0.2f;

    [Tooltip("һ��ѭ����ɺ���ղ����¿�ʼ���ӳ�ʱ��")]
    public float cycleDelay = 0.3f;

    [Header("=== �Զ����� ===")]
    [Tooltip("����ʱ�Ƿ��Զ���ʼ����ѭ������")]
    public bool autoStart = true;

    [Header("=== ���� ===")]
    [SerializeField] private bool enableDebugLog = false;

    #endregion

    #region ˽�б���

    private List<GameObject> currentUnits = new List<GameObject>();
    private Coroutine loadingCoroutine;
    private bool isLoading = false;

    #endregion

    #region Unity��������

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

    #region �����ӿ�

    /// <summary>
    /// ��ʼ����ѭ������
    /// </summary>
    public void StartLoading()
    {
        if (isLoading)
        {
            Log("���ڲ����У������ظ�����");
            return;
        }

        isLoading = true;

        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }

        loadingCoroutine = StartCoroutine(InfiniteLoadingCoroutine());
        Log("��ʼ����ѭ������");
    }

    /// <summary>
    /// ֹͣ����
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
        Log("ֹͣ����");
    }

    /// <summary>
    /// ����ָ��������ѭ�������ڽ���ݳ��ȳ�����
    /// </summary>
    public IEnumerator PlayCycles(int cycleCount)
    {
        if (isLoading)
        {
            Log("��ǰ���ڲ��ţ���ֹͣ");
            StopLoading();
        }

        if (cycleCount <= 0)
        {
            LogWarning($"ѭ�������������0����ǰֵ: {cycleCount}");
            yield break;
        }

        isLoading = true;
        Log($"��ʼ���� {cycleCount} ��ѭ��");

        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            Log($"ѭ�� {cycle + 1}/{cycleCount}");

            // ����һ������ѭ��
            yield return PlaySingleCycle();

            // ���һ��ѭ������Ҫ�ӳ�
            if (cycle < cycleCount - 1)
            {
                yield return new WaitForSeconds(cycleDelay);
            }
        }

        isLoading = false;
        ClearAllUnits();
        Log($"��� {cycleCount} ��ѭ������");
    }

    #endregion

    #region �����߼�

    /// <summary>
    /// ����ѭ������Э��
    /// </summary>
    private IEnumerator InfiniteLoadingCoroutine()
    {
        while (isLoading)
        {
            // ����һ������ѭ��
            yield return PlaySingleCycle();

            // �ȴ�ѭ���ӳ�
            yield return new WaitForSeconds(cycleDelay);
        }
    }

    /// <summary>
    /// ���ŵ���ѭ��
    /// </summary>
    private IEnumerator PlaySingleCycle()
    {
        // ��������Units
        for (int i = 0; i < maxUnits; i++)
        {
            if (!isLoading) yield break; // �����ֹͣ�������˳�

            SpawnUnit();
            yield return new WaitForSeconds(unitSpawnInterval);
        }

        // �������Units
        ClearAllUnits();
    }

    /// <summary>
    /// ����һ��Unit
    /// </summary>
    private void SpawnUnit()
    {
        if (unitPrefab == null || unitContainer == null)
        {
            LogWarning("unitPrefab��unitContainerδ����");
            return;
        }

        GameObject newUnit = Instantiate(unitPrefab, unitContainer);
        currentUnits.Add(newUnit);
    }

    /// <summary>
    /// ������������ɵ�Units
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

    #region ��־����

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