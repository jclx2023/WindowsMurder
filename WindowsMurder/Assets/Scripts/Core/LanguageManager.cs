using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// ֧�ֵ�����ö��
/// </summary>
public enum SupportedLanguage
{
    Chinese,    // ����
    English,    // Ӣ��
    Japanese    // ����
}

/// <summary>
/// �򵥵Ķ����Թ�����
/// �������CSV�ļ����ṩ���빦��
/// </summary>
public class LanguageManager : MonoBehaviour
{
    [Header("��������")]
    public string csvFileName = "Localization/LocalizationTable.csv"; // �����StreamingAssets��·��
    public SupportedLanguage currentLanguage = SupportedLanguage.Chinese;

    [Header("��������")]
    public bool enableDebugLog = true;
    public bool showMissingKeys = true;

    // ����ʵ��
    public static LanguageManager Instance { get; private set; }

    // ���������ֵ� [����][Key] = �����ı�
    private Dictionary<SupportedLanguage, Dictionary<string, string>> translations;

    // �¼��������л�ʱ����
    public static event Action<SupportedLanguage> OnLanguageChanged;

    #region Unity��������

    void Awake()
    {
        // ����ģʽ
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

    #region ��ʼ��

    /// <summary>
    /// ��ʼ�����Թ�����
    /// </summary>
    void InitializeLanguageManager()
    {
        translations = new Dictionary<SupportedLanguage, Dictionary<string, string>>();

        // ��ʼ��ÿ�����Ե��ֵ�
        foreach (SupportedLanguage lang in Enum.GetValues(typeof(SupportedLanguage)))
        {
            translations[lang] = new Dictionary<string, string>();
        }

        // ���ط����
        LoadTranslations();

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager initialized. Current language: {currentLanguage}");
        }
    }

    #endregion

    #region CSV����

    /// <summary>
    /// ���ط����
    /// </summary>
    public void LoadTranslations()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager: ���Լ��ط����ļ�: {fullPath}");
        }

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"LanguageManager: �����ļ�������: {fullPath}");
            Debug.LogError($"��ȷ���ļ�λ��: Assets/StreamingAssets/{csvFileName}");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(fullPath);

            if (lines.Length < 2)
            {
                Debug.LogError("LanguageManager: CSV�ļ���ʽ����������Ҫ�����к�һ������");
                return;
            }

            // ���������У�ȷ�������е�λ��
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
                Debug.LogError("LanguageManager: CSV�ļ�ȱ��ID��");
                return;
            }

            // ������з���
            foreach (var dict in translations.Values)
            {
                dict.Clear();
            }

            // ����������
            int loadedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);

                if (values.Length <= idIndex || string.IsNullOrEmpty(values[idIndex]))
                    continue;

                string key = values[idIndex].Trim();

                // ���ظ����Եķ���
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
                Debug.Log($"LanguageManager: �ɹ����� {loadedCount} �������¼");
                foreach (var lang in translations.Keys)
                {
                    Debug.Log($"  {lang}: {translations[lang].Count} ��");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LanguageManager: ���ط����ļ�ʱ����: {e.Message}");
        }
    }

    /// <summary>
    /// ����CSV�У����������ڵĶ���
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

        result.Add(currentValue); // ������һ��ֵ
        return result.ToArray();
    }

    #endregion

    #region �����ӿ�

    /// <summary>
    /// ��ȡ�����ı�
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

        // �����ǰ����û�и�Key��������Ӣ����Ϊ����
        if (currentLanguage != SupportedLanguage.English &&
            translations.ContainsKey(SupportedLanguage.English) &&
            translations[SupportedLanguage.English].ContainsKey(key))
        {
            if (showMissingKeys)
            {
                Debug.LogWarning($"LanguageManager: Key '{key}' �� {currentLanguage} ��ȱʧ��ʹ��Ӣ�ı���");
            }
            return translations[SupportedLanguage.English][key];
        }

        // ���Ӣ��Ҳû�У�����������
        if (currentLanguage != SupportedLanguage.Chinese &&
            translations.ContainsKey(SupportedLanguage.Chinese) &&
            translations[SupportedLanguage.Chinese].ContainsKey(key))
        {
            if (showMissingKeys)
            {
                Debug.LogWarning($"LanguageManager: Key '{key}' �� {currentLanguage} ��Ӣ����ȱʧ��ʹ�����ı���");
            }
            return translations[SupportedLanguage.Chinese][key];
        }

        // �������Զ�û�и�Key
        if (showMissingKeys)
        {
            Debug.LogWarning($"LanguageManager: Key '{key}' �����������ж�ȱʧ");
        }

        return ""; // ���ؿ��ַ������õ��÷��������߼�
    }

    /// <summary>
    /// �л�����
    /// </summary>
    public void SetLanguage(SupportedLanguage newLanguage)
    {
        if (currentLanguage == newLanguage) return;

        SupportedLanguage oldLanguage = currentLanguage;
        currentLanguage = newLanguage;

        if (enableDebugLog)
        {
            Debug.Log($"LanguageManager: ���Դ� {oldLanguage} �л��� {newLanguage}");
        }

        // ���������л��¼�
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// ���¼��ط����
    /// </summary>
    public void ReloadTranslations()
    {
        LoadTranslations();

        // ���¼��غ󴥷������л��¼��Ը���UI
        OnLanguageChanged?.Invoke(currentLanguage);
    }

    /// <summary>
    /// ��ȡ��ǰ���Ե�����Key
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