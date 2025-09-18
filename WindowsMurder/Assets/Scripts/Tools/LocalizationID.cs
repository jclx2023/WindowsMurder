using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 本地化ID组件 - 附加到需要多语言的UI组件上
/// </summary>
public class LocalizationID : MonoBehaviour
{
    [SerializeField]
    public int localizationKey;

    [SerializeField]
    public string description; // 用于识别这个文本的用途
}

#if UNITY_EDITOR

/// <summary>
/// 多语言工具配置
/// </summary>
[System.Serializable]
public class LocalizationToolConfig
{
    public string xlsxPath = "Assets/Localization/LocalizationTable.xlsx";
    public int currentMaxId = 1000;
    public int idRangeStart = 1001;
    public int idRangeEnd = 9999;
}

/// <summary>
/// Unity编辑器多语言注册工具
/// </summary>
public class LocalizationTool : EditorWindow
{
    private static LocalizationToolConfig config;
    private static string configPath = "Assets/Editor/LocalizationToolConfig.json";

    private Vector2 scrollPosition;
    private List<GameObject> selectedObjects = new List<GameObject>();

    [MenuItem("Window/多语言工具")]
    public static void OpenWindow()
    {
        LocalizationTool window = GetWindow<LocalizationTool>("多语言工具");
        window.minSize = new Vector2(400, 300);
        LoadConfig();
    }

    void OnEnable()
    {
        LoadConfig();
        RefreshSelectedObjects();
    }

    void OnGUI()
    {
        GUILayout.Label("多语言注册工具", EditorStyles.boldLabel);

        DrawConfigSection();
        DrawSelectedObjectsSection();
        DrawToolsSection();
    }

    /// <summary>
    /// 绘制配置区域
    /// </summary>
    void DrawConfigSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("配置设置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        config.xlsxPath = EditorGUILayout.TextField("CSV文件路径:", config.xlsxPath);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择文件", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFilePanel("选择CSV文件", Application.dataPath, "csv");
            if (!string.IsNullOrEmpty(path))
            {
                config.xlsxPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }

        if (GUILayout.Button("创建新表", GUILayout.Width(80)))
        {
            CreateNewCSV();
        }
        EditorGUILayout.EndHorizontal();

        config.currentMaxId = EditorGUILayout.IntField("当前最大ID:", config.currentMaxId);
        config.idRangeStart = EditorGUILayout.IntField("ID起始范围:", config.idRangeStart);
        config.idRangeEnd = EditorGUILayout.IntField("ID结束范围:", config.idRangeEnd);

        if (EditorGUI.EndChangeCheck())
        {
            SaveConfig();
        }

        // 文件状态显示
        string csvPath = config.xlsxPath.Replace(".xlsx", ".csv");
        bool fileExists = File.Exists(csvPath);
        EditorGUILayout.HelpBox(
            fileExists ? $"✓ CSV文件存在: {csvPath}" : $"✗ CSV文件不存在: {csvPath}",
            fileExists ? MessageType.Info : MessageType.Warning
        );
    }

    /// <summary>
    /// 绘制选中对象区域
    /// </summary>
    void DrawSelectedObjectsSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("当前选中的对象", EditorStyles.boldLabel);

        if (GUILayout.Button("刷新选中对象"))
        {
            RefreshSelectedObjects();
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

        if (selectedObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("请在Hierarchy中选择需要本地化的UI对象", MessageType.Info);
        }
        else
        {
            foreach (var obj in selectedObjects)
            {
                if (obj == null) continue;

                EditorGUILayout.BeginHorizontal("box");

                // 对象信息
                EditorGUILayout.LabelField($"{obj.name}", GUILayout.Width(150));

                // 组件类型
                string componentType = GetLocalizableComponentType(obj);
                EditorGUILayout.LabelField($"({componentType})", GUILayout.Width(100));

                // 当前内容预览
                string currentText = GetCurrentText(obj);
                if (!string.IsNullOrEmpty(currentText) && currentText.Length > 20)
                    currentText = currentText.Substring(0, 20) + "...";
                EditorGUILayout.LabelField($"\"{currentText}\"", GUILayout.Width(120));

                // 本地化状态
                LocalizationID locId = obj.GetComponent<LocalizationID>();
                if (locId != null)
                {
                    EditorGUILayout.LabelField($"ID: {locId.localizationKey}", GUILayout.Width(80));

                    if (GUILayout.Button("移除", GUILayout.Width(50)))
                    {
                        RemoveLocalizationFromObject(obj);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("未注册", GUILayout.Width(80));

                    if (GUILayout.Button("注册", GUILayout.Width(50)))
                    {
                        RegisterSingleObject(obj);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 绘制工具区域
    /// </summary>
    void DrawToolsSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("批量操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("批量注册选中对象"))
        {
            BatchRegisterSelectedObjects();
        }

        if (GUILayout.Button("移除所有本地化"))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要移除所有选中对象的本地化设置吗？", "确定", "取消"))
            {
                BatchRemoveLocalization();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("扫描场景中所有文本组件"))
        {
            ScanSceneForTextComponents();
        }

        if (GUILayout.Button("打开CSV文件"))
        {
            string csvPath = config.xlsxPath.Replace(".xlsx", ".csv");
            if (File.Exists(csvPath))
            {
                System.Diagnostics.Process.Start(csvPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "CSV文件不存在", "确定");
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// 刷新选中对象列表
    /// </summary>
    void RefreshSelectedObjects()
    {
        selectedObjects.Clear();

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (IsLocalizableObject(obj))
            {
                selectedObjects.Add(obj);
            }
        }

        Repaint();
    }

    /// <summary>
    /// 判断对象是否可本地化
    /// </summary>
    bool IsLocalizableObject(GameObject obj)
    {
        return obj.GetComponent<TextMeshProUGUI>() != null ||
               obj.GetComponent<Text>() != null ||
               obj.GetComponent<Image>() != null;
    }

    /// <summary>
    /// 获取可本地化组件类型
    /// </summary>
    string GetLocalizableComponentType(GameObject obj)
    {
        if (obj.GetComponent<TextMeshProUGUI>()) return "TMP";
        if (obj.GetComponent<Text>()) return "Text";
        if (obj.GetComponent<Image>()) return "Image";
        return "Unknown";
    }

    /// <summary>
    /// 获取当前文本内容
    /// </summary>
    string GetCurrentText(GameObject obj)
    {
        var tmp = obj.GetComponent<TextMeshProUGUI>();
        if (tmp != null) return tmp.text;

        var text = obj.GetComponent<Text>();
        if (text != null) return text.text;

        return "";
    }

    /// <summary>
    /// 注册单个对象
    /// </summary>
    void RegisterSingleObject(GameObject obj)
    {
        if (obj.GetComponent<LocalizationID>() != null)
        {
            EditorUtility.DisplayDialog("提示", $"{obj.name} 已经注册过了", "确定");
            return;
        }

        int newId = GetNextId();
        string description = $"{obj.name}_{GetLocalizableComponentType(obj)}";

        // 添加LocalizationID组件
        LocalizationID locId = obj.AddComponent<LocalizationID>();
        locId.localizationKey = newId;
        locId.description = description;

        // 写入CSV，只写ID和描述
        WriteToCSV(newId, description);

        // 标记对象为dirty
        EditorUtility.SetDirty(obj);

        Debug.Log($"已注册: {obj.name} -> ID: {newId}, 请在CSV文件中手动填写翻译内容");
        RefreshSelectedObjects();
    }

    /// <summary>
    /// 批量注册选中对象
    /// </summary>
    void BatchRegisterSelectedObjects()
    {
        int registeredCount = 0;

        foreach (var obj in selectedObjects)
        {
            if (obj.GetComponent<LocalizationID>() == null)
            {
                RegisterSingleObject(obj);
                registeredCount++;
            }
        }

        if (registeredCount > 0)
        {
            EditorUtility.DisplayDialog("完成", $"已注册 {registeredCount} 个对象", "确定");
        }
        else
        {
            EditorUtility.DisplayDialog("提示", "没有新的对象需要注册", "确定");
        }
    }

    /// <summary>
    /// 移除对象的本地化设置
    /// </summary>
    void RemoveLocalizationFromObject(GameObject obj)
    {
        LocalizationID locId = obj.GetComponent<LocalizationID>();
        if (locId != null)
        {
            DestroyImmediate(locId);
            EditorUtility.SetDirty(obj);
            RefreshSelectedObjects();
        }
    }

    /// <summary>
    /// 批量移除本地化设置
    /// </summary>
    void BatchRemoveLocalization()
    {
        int removedCount = 0;

        foreach (var obj in selectedObjects)
        {
            LocalizationID locId = obj.GetComponent<LocalizationID>();
            if (locId != null)
            {
                DestroyImmediate(locId);
                EditorUtility.SetDirty(obj);
                removedCount++;
            }
        }

        RefreshSelectedObjects();
        EditorUtility.DisplayDialog("完成", $"已移除 {removedCount} 个对象的本地化设置", "确定");
    }

    /// <summary>
    /// 扫描场景中所有文本组件
    /// </summary>
    void ScanSceneForTextComponents()
    {
        var allTextObjects = new List<GameObject>();

        // 查找所有TextMeshProUGUI组件
        var tmpComponents = FindObjectsOfType<TextMeshProUGUI>();
        foreach (var tmp in tmpComponents)
        {
            allTextObjects.Add(tmp.gameObject);
        }

        // 查找所有Text组件
        var textComponents = FindObjectsOfType<Text>();
        foreach (var text in textComponents)
        {
            allTextObjects.Add(text.gameObject);
        }

        // 更新选择
        Selection.objects = allTextObjects.ToArray();
        RefreshSelectedObjects();

        EditorUtility.DisplayDialog("完成", $"找到 {allTextObjects.Count} 个文本组件", "确定");
    }

    /// <summary>
    /// 获取下一个ID
    /// </summary>
    int GetNextId()
    {
        config.currentMaxId++;
        if (config.currentMaxId > config.idRangeEnd)
        {
            config.currentMaxId = config.idRangeStart;
        }
        SaveConfig();
        return config.currentMaxId;
    }

    /// <summary>
    /// 创建新的CSV文件
    /// </summary>
    void CreateNewCSV()
    {
        string folderPath = Path.GetDirectoryName(config.xlsxPath);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 创建CSV文件，支持中英日三语言
        string csvContent = "ID,Description,Chinese,English,Japanese\n";
        string csvPath = config.xlsxPath.Replace(".xlsx", ".csv");
        File.WriteAllText(csvPath, csvContent);

        EditorUtility.DisplayDialog("提示", $"已创建多语言表文件：{csvPath}\n包含列：ID, Description, Chinese, English, Japanese", "确定");
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 写入CSV文件 - 只写入ID和描述，不填充文本内容
    /// </summary>
    void WriteToCSV(int id, string description)
    {
        string csvPath = config.xlsxPath.Replace(".xlsx", ".csv");

        if (!File.Exists(csvPath))
        {
            string header = "ID,Description,Chinese,English,Japanese\n";
            File.WriteAllText(csvPath, header);
        }

        // 只写入ID和描述，语言字段留空供翻译人员填写
        string newRow = $"{id},\"{description}\",\"\",\"\",\"\"\n";
        File.AppendAllText(csvPath, newRow);

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    static void LoadConfig()
    {
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            config = JsonUtility.FromJson<LocalizationToolConfig>(json);
        }
        else
        {
            config = new LocalizationToolConfig();
            SaveConfig();
        }
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    static void SaveConfig()
    {
        string json = JsonUtility.ToJson(config, true);
        string dir = Path.GetDirectoryName(configPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(configPath, json);
    }

    void OnSelectionChange()
    {
        RefreshSelectedObjects();
    }
}

#endif