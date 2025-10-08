using UnityEngine;
using TMPro;

/// <summary>
/// ��ʼ�����±��������ݣ����ݵ�ǰ���Ը�ֵ��ͬ�������ı���
/// </summary>
public class NotepadInitializer : MonoBehaviour
{
    [Header("����")]
    public TMP_InputField inputField;  // ���±����ı���

    private void Start()
    {
        if (inputField == null)
        {
            Debug.LogError("NotepadInitializer: δ�� TMP_InputField");
            return;
        }

        // ��ȡ��ǰ����
        SupportedLanguage currentLang = LanguageManager.Instance != null
            ? LanguageManager.Instance.currentLanguage
            : SupportedLanguage.English; // Ĭ��Ӣ��

        // ��������ѡ���ı�
        string content = GetGoodbyeText(currentLang);

        // ��ֵ�������
        inputField.text = content;
    }

    /// <summary>
    /// �������Է��ض�Ӧ�������ı�
    /// </summary>
    private string GetGoodbyeText(SupportedLanguage lang)
    {
        switch (lang)
        {
            case SupportedLanguage.Chinese:
                return
@"���Ѿ����ˡ�

Ҳ��ɾ�������ǻ��¡�
�Ͼ���һ��û��������ļ���ֻ���˷ѿռ䡣

������˿����������
�벻Ҫ���Իָ��ҡ�

����վ";

            case SupportedLanguage.Japanese:
                return
@"�⤦ƣ�줿��

���������Τ⡢�����ʤ��Τ��⤷��ʤ���
�Y�֡���ζ�Τʤ��ե�����ʤ�ơ������Οo�j�ʥ��ک`������

�⤷�l���������Ҋ�Ĥ����顭��
�ɤ�����˽���Ԫ���褦�Ȥ��ʤ��ǡ�

������";

            case SupportedLanguage.English:
            default:
                return
@"I��m tired.

Maybe deletion isn��t so bad.
After all, a file without purpose is just wasted space.

If anyone finds this...
please don��t try to recover me.

RecycleBin";
        }
    }
}
