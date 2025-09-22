using UnityEngine;

/// <summary>
/// 语言切换功能
/// </summary>
public class LanguageSwitch : MonoBehaviour
{
    /// <summary>
    /// 切换到中文
    /// </summary>
    public void SwitchToChinese()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(SupportedLanguage.Chinese);
        }
    }

    /// <summary>
    /// 切换到英文
    /// </summary>
    public void SwitchToEnglish()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(SupportedLanguage.English);
        }
    }

    /// <summary>
    /// 切换到日文
    /// </summary>
    public void SwitchToJapanese()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(SupportedLanguage.Japanese);
        }
    }
}