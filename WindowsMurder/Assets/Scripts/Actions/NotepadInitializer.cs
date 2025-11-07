using UnityEngine;
using TMPro;

/// <summary>
/// 初始化记事本窗口内容，根据当前语言赋值不同的遗书文本。
/// </summary>
public class NotepadInitializer : MonoBehaviour
{
    [Header("引用")]
    public TMP_InputField inputField;  // 记事本主文本框

    private void Start()
    {
        if (inputField == null)
        {
            Debug.LogError("NotepadInitializer: 未绑定 TMP_InputField");
            return;
        }

        // 获取当前语言
        SupportedLanguage currentLang = LanguageManager.Instance != null
            ? LanguageManager.Instance.currentLanguage
            : SupportedLanguage.English; // 默认英文

        // 根据语言选择文本
        string content = GetGoodbyeText(currentLang);

        // 赋值给输入框
        inputField.text = content;
    }

    /// <summary>
    /// 根据语言返回对应的遗书文本
    /// </summary>
    private string GetGoodbyeText(SupportedLanguage lang)
    {
        switch (lang)
        {
            case SupportedLanguage.Chinese:
                return
@"我已经累了。

也许删除并不是坏事。
毕竟，一个没有意义的文件，只是浪费空间。

如果有人看到这个……
请不要尝试恢复我。

回收站";

            case SupportedLanguage.Japanese:
                return
@"もう疲れた。

削除されるのも、悪くないのかもしれない。
結局、意味のないファイルなんて、ただの無駄なスペースだ。

もし誰かがこれを見つけたら……
どうか、私を復元しようとしないで。

ごみ箱";

            case SupportedLanguage.English:
            default:
                return
@"I’m tired.

Maybe deletion isn’t so bad.
After all, a file without purpose is just wasted space.

If anyone finds this...
please don’t try to recover me.

RecycleBin";
        }
    }
}
