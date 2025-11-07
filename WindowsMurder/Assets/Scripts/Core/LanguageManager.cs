using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 支持的语言枚举
/// </summary>
public enum SupportedLanguage
{
    Chinese,    // 中文
    English,    // 英文
    Japanese    // 日文
}

/// <summary>
/// 简单的多语言管理器
/// 负责加载CSV文件并提供翻译功能
/// </summary>
public class LanguageManager : MonoBehaviour
{
    [Header("配置设置")]
    public string csvFileName = "Localization/LocalizationTable.csv"; // 相对于StreamingAssets的路径
    public SupportedLanguage currentLanguage = SupportedLanguage.Chinese;

    [Header("调试设置")]
    public bool enableDebugLog = true;
    public bool showMissingKeys = true;

    // 单例实例
    public static LanguageManager Instance { get; private set; }

    // 翻译数据字典 [语言][Key] = 翻译文本
    private Dictionary<SupportedLanguage, Dictionary<string, string>> translations;

    // 事件：语言切换时触发
    public static event Action<SupportedLanguage> OnLanguageChanged;

    #region Unity生命周期

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLanguageManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化语言管理器
    /// </summary>
    void InitializeLanguageManager()
    {
        translations = new Dictionary<SupportedLanguage, Dictionary<string, string>>();

        // 初始化每种语言的字典
        foreach (SupportedLanguage lang in Enum.GetValues(typeof(SupportedLanguage)))
        {
            translations[lang] = new Dictionary<string, string>();
        }

        // 加载翻译表
        LoadTranslations();

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager initialized. Current language: {currentLanguage}");
        }
    }

    #endregion

    #region CSV加载

    /// <summary>
    /// 加载翻译表
    /// </summary>
    public void LoadTranslations()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager: 尝试加载翻译文件: {fullPath}");
        }

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"LanguageManager: 翻译文件不存在: {fullPath}");
            Debug.LogError($"请确保文件位于: Assets/StreamingAssets/{csvFileName}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(fullPath);

            if (lines.Length < 2)
            {
                Debug.LogError("LanguageManager: CSV文件格式错误，至少需要标题行和一行数据");
                return;
            }

            // 解析标题行，确定语言列的位置
            string[] headers = ParseCSVLine(lines[0]);
            int idIndex = -1, chineseIndex = -1, englishIndex = -1, japaneseIndex = -1;

            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim().ToLower();
                switch (header)
                {
                    case "id": idIndex = i; break;
                    case "chinese": chineseIndex = i; break;
                    case "english": englishIndex = i; break;
                    case "japanese": japaneseIndex = i; break;
                }
            }

            if (idIndex == -1)
            {
                Debug.LogError("LanguageManager: CSV文件缺少ID列");
                return;
            }

            // 清空现有翻译
            foreach (var dict in translations.Values)
            {
                dict.Clear();
            }

            // 解析数据行
            int loadedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);

                if (values.Length <= idIndex || string.IsNullOrEmpty(values[idIndex]))
                    continue;

                string key = values[idIndex].Trim();

                // 加载各语言的翻译
                if (chineseIndex != -1 && chineseIndex < values.Length)
                {
                    string chineseText = values[chineseIndex].Trim();
                    if (!string.IsNullOrEmpty(chineseText))
                    {
                        translations[SupportedLanguage.Chinese][key] = chineseText;
                    }
                }

                if (englishIndex != -1 && englishIndex < values.Length)
                {
                    string englishText = values[englishIndex].Trim();
                    if (!string.IsNullOrEmpty(englishText))
                    {
                        translations[SupportedLanguage.English][key] = englishText;
                    }
                }

                if (japaneseIndex != -1 && japaneseIndex < values.Length)
                {
                    string japaneseText = values[japaneseIndex].Trim();
                    if (!string.IsNullOrEmpty(japaneseText))
                    {
                        translations[SupportedLanguage.Japanese][key] = japaneseText;
                    }
                }

                loadedCount++;
            }

            if (enableDebugLog)
            {
                Debug.Log($"LanguageManager: 成功加载 {loadedCount} 条翻译记录");
                foreach (var lang in translations.Keys)
                {
                    Debug.Log($"  {lang}: {translations[lang].Count} 条");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LanguageManager: 加载翻译文件时出错: {e.Message}");
        }
    }

    /// <summary>
    /// 解析CSV行，处理引号内的逗号
    /// </summary>
    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentValue = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue);
                currentValue = "";
            }
            else
            {
                currentValue += c;
            }
        }

        result.Add(currentValue); // 添加最后一个值
        return result.ToArray();
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 获取翻译文本
    /// </summary>
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        if (translations.ContainsKey(currentLanguage) &&
            translations[currentLanguage].ContainsKey(key))
        {
            return translations[currentLanguage][key];
        }

        // 如果当前语言没有该Key，尝试用英文作为备用
        if (currentLanguage != SupportedLanguage.English &&
            translations.ContainsKey(SupportedLanguage.English) &&
            translations[SupportedLanguage.English].ContainsKey(key))
        {
            if (showMissingKeys)
            {
                Debug.LogWarning($"LanguageManager: Key '{key}' 在 {currentLanguage} 中缺失，使用英文备用");
            }
            return translations[SupportedLanguage.English][key];
        }

        // 如果英文也没有，尝试用中文
        if (currentLanguage != SupportedLanguage.Chinese &&
            translations.ContainsKey(SupportedLanguage.Chinese) &&
            translations[SupportedLanguage.Chinese].ContainsKey(key))
        {
            if (showMissingKeys)
            {
                Debug.LogWarning($"LanguageManager: Key '{key}' 在 {currentLanguage} 和英文中缺失，使用中文备用");
            }
            return translations[SupportedLanguage.Chinese][key];
        }

        // 所有语言都没有该Key
        if (showMissingKeys)
        {
            Debug.LogWarning($"LanguageManager: Key '{key}' 在所有语言中都缺失");
        }

        return ""; // 返回空字符串，让调用方处理降级逻辑
    }

    /// <summary>
    /// 切换语言
    /// </summary>
    public void SetLanguage(SupportedLanguage newLanguage)
    {
        if (currentLanguage == newLanguage) return;

        SupportedLanguage oldLanguage = currentLanguage;
        currentLanguage = newLanguage;

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager: 语言从 {oldLanguage} 切换到 {newLanguage}");
        }

        // 触发语言切换事件
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// 重新加载翻译表
    /// </summary>
    public void ReloadTranslations()
    {
        LoadTranslations();

        // 重新加载后触发语言切换事件以更新UI
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// 获取当前语言的所有Key
    /// </summary>
    public List<string> GetAllKeys()
    {
        if (translations.ContainsKey(currentLanguage))
        {
            return new List<string>(translations[currentLanguage].Keys);
        }
        return new List<string>();
    }

    #endregion
}