using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单句对话数据
/// </summary>
[Serializable]
public class DialogueLine
{
    public string id;           // 句子ID
    public bool mode;           // true=预设文本, false=LLM模式
    public string characterId;  // 角色ID
    public string text;         // 预设文本内容（仅mode=true时使用）
    public string portraitId;   // 立绘ID

    [Header("LLM模式设置")]
    public List<string> endKeywords; // LLM结束关键词，如["结束", "再见", "END"]
}

/// <summary>
/// 对话数据集合
/// </summary>
[Serializable]
public class DialogueData
{
    public string conversationId;       // 对话ID
    public List<DialogueLine> lines;    // 对话句子列表
}

/// <summary>
/// 对话数据加载器 - 简单版本
/// </summary>
public class DialogueLoader : MonoBehaviour
{
    [Header("设置")]
    public string resourcePath = "DialogueData"; // Resources文件夹下的路径

    /// <summary>
    /// 从Resources加载对话JSON文件
    /// </summary>
    public static DialogueData Load(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogError("DialogueLoader: fileName is null or empty");
            return null;
        }

        // 从Resources加载JSON文件
        TextAsset jsonFile = Resources.Load<TextAsset>($"DialogueData/{fileName}");
        if (jsonFile == null)
        {
            Debug.LogError($"DialogueLoader: 找不到文件 DialogueData/{fileName}.json");
            return null;
        }

        try
        {
            // 解析JSON
            DialogueData data = JsonUtility.FromJson<DialogueData>(jsonFile.text);

            // 简单验证
            if (data == null)
            {
                Debug.LogError($"DialogueLoader: JSON解析失败 - {fileName}");
                return null;
            }

            if (string.IsNullOrEmpty(data.conversationId))
            {
                Debug.LogWarning($"DialogueLoader: {fileName} 缺少conversationId");
                data.conversationId = fileName;
            }

            if (data.lines == null || data.lines.Count == 0)
            {
                Debug.LogWarning($"DialogueLoader: {fileName} 没有对话内容");
                return data;
            }

            // 验证每句对话
            for (int i = 0; i < data.lines.Count; i++)
            {
                DialogueLine line = data.lines[i];

                // 自动生成ID
                if (string.IsNullOrEmpty(line.id))
                {
                    line.id = $"{fileName}_line_{i:000}";
                }

                // 预设模式检查文本
                if (line.mode && string.IsNullOrEmpty(line.text))
                {
                    Debug.LogWarning($"DialogueLoader: 预设模式的对话 {line.id} 缺少文本内容");
                }

                // LLM模式检查角色ID
                if (!line.mode && string.IsNullOrEmpty(line.characterId))
                {
                    Debug.LogWarning($"DialogueLoader: LLM模式的对话 {line.id} 缺少角色ID");
                }

                // 设置默认结束关键词
                if (!line.mode && (line.endKeywords == null || line.endKeywords.Count == 0))
                {
                    line.endKeywords = new List<string> { "结束", "再见", "END", "end" };
                }
            }

            Debug.Log($"DialogueLoader: 成功加载 {fileName}，包含 {data.lines.Count} 句对话");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"DialogueLoader: 解析JSON时出错 - {fileName}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取下一句预设对话（跳过LLM模式的句子）
    /// </summary>
    public static int GetNextPresetLine(DialogueData data, int currentIndex)
    {
        if (data?.lines == null) return -1;

        for (int i = currentIndex + 1; i < data.lines.Count; i++)
        {
            if (data.lines[i].mode) // 预设模式
            {
                return i;
            }
        }

        return -1; // 没有找到
    }

    /// <summary>
    /// 获取下一句LLM对话
    /// </summary>
    public static int GetNextLLMLine(DialogueData data, int currentIndex)
    {
        if (data?.lines == null) return -1;

        for (int i = currentIndex + 1; i < data.lines.Count; i++)
        {
            if (!data.lines[i].mode) // LLM模式
            {
                return i;
            }
        }

        return -1; // 没有找到
    }

    /// <summary>
    /// 检查消息是否包含结束关键词
    /// </summary>
    public static bool ShouldEndLLMDialogue(string message, List<string> endKeywords)
    {
        if (string.IsNullOrEmpty(message) || endKeywords == null)
            return false;

        string lowerMessage = message.ToLower().Trim();

        foreach (string keyword in endKeywords)
        {
            if (lowerMessage.Contains(keyword.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取指定索引的对话句子
    /// </summary>
    public static DialogueLine GetLineAt(DialogueData data, int index)
    {
        if (data?.lines == null || index < 0 || index >= data.lines.Count)
        {
            return null;
        }

        return data.lines[index];
    }
}