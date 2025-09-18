using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Windows XP���Ķ������������� - �򻯰�
/// </summary>
public class WinXPLoadingBar : MonoBehaviour
{
    [Header("��������")]
    [SerializeField] private GameObject unitPrefab;          // UnitԤ����
    [SerializeField] private Transform unitContainer;        // Unit��������Horizontal Layout�ĸ����壩
    [SerializeField] private int maxUnits = 10;             // ���Unit����

    [Header("��������")]
    [SerializeField] private float unitSpawnInterval = 0.2f; // Unit���ɼ�����룩
    [SerializeField] private float cycleDelay = 0.3f;        // ѭ���������պ��ٿ�ʼ���ӳ�ʱ�䣩
    [SerializeField] private bool autoStart = true;          // �Ƿ��Զ���ʼ

    // ˽�б���
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

    private IEnumerator LoadingAnimation()
    {
        while (isLoading)
        {
            // ����Units
            for (int i = 0; i < maxUnits; i++)
            {
                if (!isLoading) break;

                SpawnUnit();
                yield return new WaitForSeconds(unitSpawnInterval);
            }

            // �������Unit
            ClearAllUnits();

            // �ȴ�ѭ�����
            yield return new WaitForSeconds(cycleDelay);
        }
    }

    private void SpawnUnit()
    {
        if (unitPrefab == null || unitContainer == null) return;

        GameObject newUnit = Instantiate(unitPrefab, unitContainer);
        currentUnits.Add(newUnit);
    }

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