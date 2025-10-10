using UnityEngine;

/// <summary>
/// ˫������ָ���㼶����Prefab����Ϊ
/// </summary>
public class SpawnPrefabAction : IconAction
{
    [Header("��������")]
    public GameObject prefabToSpawn;       // Ҫ���ɵ�Ԥ����
    public Transform parentTransform;      // ���ɵĸ�������Ϊ�գ�
    public Vector3 spawnPosition;          // ���ɵ�λ�ã�����ڸ�����

    [Header("��ѡЧ��")]
    public bool destroyIfExists = false;   // ���Ѵ���ͬ�������Ƿ����پɵ�
    public string instanceName = "";       // ���ɶ���������Ϊ������prefab����

    private GameObject spawnedInstance;

    public override void Execute()
    {

        // ���Ѵ���ʵ��
        if (spawnedInstance != null)
        {
            if (destroyIfExists)
            {
                Destroy(spawnedInstance);
            }
            else
            {
                Debug.Log($"{name}: �����ɶ��� {spawnedInstance.name}");
                return;
            }
        }

        // ִ������
        spawnedInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, parentTransform);
        if (!string.IsNullOrEmpty(instanceName))
            spawnedInstance.name = instanceName;

        Debug.Log($"{name}: ������Prefab {spawnedInstance.name} �� {spawnPosition}");
    }
}
