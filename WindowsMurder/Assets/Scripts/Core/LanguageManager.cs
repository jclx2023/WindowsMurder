using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 支锟街碉拷锟斤拷锟斤拷枚锟斤拷
/// </summary>
public enum SupportedLanguage
{
    Chinese,    // 锟斤拷锟斤拷
    English,    // 英锟斤拷
    Japanese    // 锟斤拷锟斤拷
}

/// <summary>
/// 锟津单的讹拷锟斤拷锟皆癸拷锟斤拷锟斤拷
/// 锟斤拷锟斤拷锟斤拷锟紺SV锟侥硷拷锟斤拷锟结供锟斤拷锟诫功锟斤拷
/// </summary>
public class LanguageManager : MonoBehaviour
{
    [Header("锟斤拷锟斤拷锟斤拷锟斤拷")]
    public string csvFileName = "Localization/LocalizationTable.csv"; // 锟斤拷锟斤拷锟絊treamingAssets锟斤拷路锟斤拷
    public SupportedLanguage currentLanguage = SupportedLanguage.Chinese;

    [Header("锟斤拷锟斤拷锟斤拷锟斤拷")]
    public bool enableDebugLog = true;
    public bool showMissingKeys = true;

    // 锟斤拷锟斤拷实锟斤拷
    public static LanguageManager Instance { get; private set; }

    // 锟斤拷锟斤拷锟斤拷锟斤拷锟街碉拷 [锟斤拷锟斤拷][Key] = 锟斤拷锟斤拷锟侥憋拷
    private Dictionary<SupportedLanguage, Dictionary<string, string>> translations;

    // 锟铰硷拷锟斤拷锟斤拷锟斤拷锟叫伙拷时锟斤拷锟斤拷
    public static event Action<SupportedLanguage> OnLanguageChanged;

    #region Unity锟斤拷锟斤拷锟斤拷锟斤拷

    void Awake()
    {
        // 锟斤拷锟斤拷模式
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

    #region 锟斤拷始锟斤拷

    /// <summary>
    /// 锟斤拷始锟斤拷锟斤拷锟皆癸拷锟斤拷锟斤拷
    /// </summary>
    void InitializeLanguageManager()
    {
        translations = new Dictionary<SupportedLanguage, Dictionary<string, string>>();

        // 锟斤拷始锟斤拷每锟斤拷锟斤拷锟皆碉拷锟街碉拷
        foreach (SupportedLanguage lang in Enum.GetValues(typeof(SupportedLanguage)))
        {
            translations[lang] = new Dictionary<string, string>();
        }

        // 锟斤拷锟截凤拷锟斤拷锟?        LoadTranslations();

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager initialized. Current language: {currentLanguage}");
        }
    }

    #endregion

    #region CSV锟斤拷锟斤拷

    /// <summary>
    /// 锟斤拷锟截凤拷锟斤拷锟?    /// </summary>
    public void LoadTranslations()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager: 锟斤拷锟皆硷拷锟截凤拷锟斤拷锟侥硷拷: {fullPath}");
        }

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"LanguageManager: 锟斤拷锟斤拷锟侥硷拷锟斤拷锟斤拷锟斤拷: {fullPath}");
            Debug.LogError($"锟斤拷确锟斤拷锟侥硷拷位锟斤拷: Assets/StreamingAssets/{csvFileName}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(fullPath);

            if (lines.Length < 2)
            {
                Debug.LogError("LanguageManager: CSV锟侥硷拷锟斤拷式锟斤拷锟斤拷锟斤拷锟斤拷锟斤拷要锟斤拷锟斤拷锟叫猴拷一锟斤拷锟斤拷锟斤拷");
                return;
            }

            // 锟斤拷锟斤拷锟斤拷锟斤拷锟叫ｏ拷确锟斤拷锟斤拷锟斤拷锟叫碉拷位锟斤拷
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
                Debug.LogError("LanguageManager: CSV锟侥硷拷缺锟斤拷ID锟斤拷");
                return;
            }

            // 锟斤拷锟斤拷锟斤拷蟹锟斤拷锟?            foreach (var dict in translations.Values)
            {
                dict.Clear();
            }

            // 锟斤拷锟斤拷锟斤拷锟斤拷锟斤拷
            int loadedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);

                if (values.Length <= idIndex || string.IsNullOrEmpty(values[idIndex]))
                    continue;

                string key = values[idIndex].Trim();

                // 锟斤拷锟截革拷锟斤拷锟皆的凤拷锟斤拷
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
                Debug.Log($"LanguageManager: 锟缴癸拷锟斤拷锟斤拷 {loadedCount} 锟斤拷锟斤拷锟斤拷锟铰?);
                foreach (var lang in translations.Keys)
                {
                    Debug.Log($"  {lang}: {translations[lang].Count} 锟斤拷");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LanguageManager: 锟斤拷锟截凤拷锟斤拷锟侥硷拷时锟斤拷锟斤拷: {e.Message}");
        }
    }

    /// <summary>
    /// 锟斤拷锟斤拷CSV锟叫ｏ拷锟斤拷锟斤拷锟斤拷锟斤拷锟节的讹拷锟斤拷
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

        result.Add(currentValue); // 锟斤拷锟斤拷锟斤拷一锟斤拷值
        return result.ToArray();
    }

    #endregion

    #region 锟斤拷锟斤拷锟接匡拷

    /// <summary>
    /// 锟斤拷取锟斤拷锟斤拷锟侥憋拷
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

        // 锟斤拷锟斤拷锟角帮拷锟斤拷锟矫伙拷懈锟終ey锟斤拷锟斤拷锟斤拷锟斤拷英锟斤拷锟斤拷为锟斤拷锟斤拷
        if (currentLanguage != SupportedLanguage.English &&
            translations.ContainsKey(SupportedLanguage.English) &&
            translations[SupportedLanguage.English].ContainsKey(key))
        {
            if (showMissingKeys)
            {
                Debug.LogWarning($"LanguageManager: Key '{key}' 锟斤拷 {currentLanguage} 锟斤拷缺失锟斤拷使锟斤拷英锟侥憋拷锟斤拷");
            }
            return translations[SupportedLanguage.English][key];
        }

        // 锟斤拷锟接拷锟揭裁伙拷校锟斤拷锟斤拷锟斤拷锟斤拷锟斤拷锟?        if (currentLanguage != SupportedLanguage.Chinese &&
            translations.ContainsKey(SupportedLanguage.Chinese) &&
            translations[SupportedLanguage.Chinese].ContainsKey(key))
        {
            if (showMissingKeys)
            {
                Debug.LogWarning($"LanguageManager: Key '{key}' 锟斤拷 {currentLanguage} 锟斤拷英锟斤拷锟斤拷缺失锟斤拷使锟斤拷锟斤拷锟侥憋拷锟斤拷");
            }
            return translations[SupportedLanguage.Chinese][key];
        }

        // 锟斤拷锟斤拷锟斤拷锟皆讹拷没锟叫革拷Key
        if (showMissingKeys)
        {
            Debug.LogWarning($"LanguageManager: Key '{key}' 锟斤拷锟斤拷锟斤拷锟斤拷锟斤拷锟叫讹拷缺失");
        }

        return ""; // 锟斤拷锟截匡拷锟街凤拷锟斤拷锟斤拷锟矫碉拷锟矫凤拷锟斤拷锟斤拷锟斤拷锟竭硷拷
    }

    /// <summary>
    /// 锟叫伙拷锟斤拷锟斤拷
    /// </summary>
    public void SetLanguage(SupportedLanguage newLanguage)
    {
        if (currentLanguage == newLanguage) return;

        SupportedLanguage oldLanguage = currentLanguage;
        currentLanguage = newLanguage;

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager: 锟斤拷锟皆达拷 {oldLanguage} 锟叫伙拷锟斤拷 {newLanguage}");
        }

        // 锟斤拷锟斤拷锟斤拷锟斤拷锟叫伙拷锟铰硷拷
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// 锟斤拷锟铰硷拷锟截凤拷锟斤拷锟?    /// </summary>
    public void ReloadTranslations()
    {
        LoadTranslations();

        // 锟斤拷锟铰硷拷锟截后触凤拷锟斤拷锟斤拷锟叫伙拷锟铰硷拷锟皆革拷锟斤拷UI
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// 锟斤拷取锟斤拷前锟斤拷锟皆碉拷锟斤拷锟斤拷Key
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
