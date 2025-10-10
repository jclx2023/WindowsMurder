using UnityEngine;

/// <summary>
/// 双击后在指定层级生成Prefab的行为
/// </summary>
public class SpawnPrefabAction : IconAction
{
    [Header("生成设置")]
    public GameObject prefabToSpawn;       // 要生成的预制体
    public Transform parentTransform;      // 生成的父级（可为空）
    public Vector3 spawnPosition;          // 生成的位置（相对于父级）

    [Header("可选效果")]
    public bool destroyIfExists = false;   // 若已存在同名对象是否销毁旧的
    public string instanceName = "";       // 生成对象命名（为空则用prefab名）

    private GameObject spawnedInstance;

    public override void Execute()
    {

        // 若已存在实例
        if (spawnedInstance != null)
        {
            if (destroyIfExists)
            {
                Destroy(spawnedInstance);
            }
            else
            {
                Debug.Log($"{name}: 已生成对象 {spawnedInstance.name}");
                return;
            }
        }

        // 执行生成
        spawnedInstance = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, parentTransform);
        if (!string.IsNullOrEmpty(instanceName))
            spawnedInstance.name = instanceName;

        Debug.Log($"{name}: 已生成Prefab {spawnedInstance.name} 于 {spawnPosition}");
    }
}
