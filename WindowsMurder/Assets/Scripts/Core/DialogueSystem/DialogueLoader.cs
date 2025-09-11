using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单句对话（不含语音，支持多语言）
/// </summary>
[Serializable]
public class DialogueLine
{
    public string id;
    public bool mode;         // "T=preset" / "F=llm"
    public string characterId;  // 说话角色ID（preset：用于显示某角色台词；llm：表示当前是Player和该问询目标的对话）
    public string text;         // 仅 preset 下使用
    public string portraitId;   // 立绘ID
}


/// <summary>
/// 整个对话段（线性）
/// </summary>
[Serializable]
public class DialogueData
{
    public string conversationId;       // 对话段ID
    public List<DialogueLine> lines;    // 台词顺序
}

/// <summary>
/// 对话数据加载器
/// </summary>
public static class DialogueLoader
{
    /// <summary>
    /// 从 Resources/DialogueData/ 下加载 JSON 文件
    /// </summary>
    public static DialogueData Load(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>($"DialogueData/{fileName}");
        if (jsonFile == null)
        {
            Debug.LogError($"DialogueLoader: 找不到文件 {fileName}.json");
            return null;
        }

        return JsonUtility.FromJson<DialogueData>(jsonFile.text);
    }
}
