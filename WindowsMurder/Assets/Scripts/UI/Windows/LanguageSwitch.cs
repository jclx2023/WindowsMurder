using UnityEngine;

/// <summary>
/// �����л�����
/// </summary>
public class LanguageSwitch : MonoBehaviour
{
    /// <summary>
    /// �л�������
    /// </summary>
    public void SwitchToChinese()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(SupportedLanguage.Chinese);
        }
    }

    /// <summary>
    /// �л���Ӣ��
    /// </summary>
    public void SwitchToEnglish()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(SupportedLanguage.English);
        }
    }

    /// <summary>
    /// �л�������
    /// </summary>
    public void SwitchToJapanese()
    {
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.SetLanguage(SupportedLanguage.Japanese);
        }
    }
}