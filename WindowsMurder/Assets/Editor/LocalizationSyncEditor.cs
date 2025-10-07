using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 打包前自动同步 Localization CSV 到 StreamingAssets
/// </summary>
public static class LocalizationSyncEditor
{
    private const string sourcePath = "Assets/Localization/LocalizationTable.csv";
    private const string targetDir = "Assets/StreamingAssets/Localization";
    private const string targetPath = "Assets/StreamingAssets/Localization/LocalizationTable.csv";

    // 每次构建前执行
    [InitializeOnLoadMethod]
    static void OnEditorLoad()
    {
        // 监听构建流程
        BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
    }

    private static void OnBuildPlayer(BuildPlayerOptions buildPlayerOptions)
    {
        SyncLocalizationFile();

        // 手动调用默认构建逻辑
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildPlayerOptions);
    }

    [MenuItem("Tools/Localization/Sync to StreamingAssets %#l")]
    public static void SyncLocalizationFile()
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"❌ LocalizationSyncEditor: 找不到源文件: {sourcePath}");
            return;
        }

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        File.Copy(sourcePath, targetPath, true);
        AssetDatabase.Refresh();

        Debug.Log($"✅ LocalizationSyncEditor: 已同步 CSV → {targetPath}");
    }
}
