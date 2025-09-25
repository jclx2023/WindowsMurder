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
/// 单个对话块
/// </summary>
[Serializable]
public class DialogueBlock
{
    public string blockId;
    public List<DialogueLine> lines;
}

/// <summary>
/// 整本剧本（包含多个对话块）
/// </summary>
[Serializable]
public class DialogueBook
{
    public List<DialogueBlock> blocks;
}

/// <summary>
/// 对话数据集合（用于统一接口返回）
/// </summary>
[Serializable]
public class DialogueData
{
    public string conversationId;       // 对话块ID
    public List<DialogueLine> lines;    // 对话句子列表
}

/// <summary>
/// 对话数据加载器（新版，仅支持整本剧本 JSON）
/// </summary>
public class DialogueLoader : MonoBehaviour
{
    [Header("设置")]
    public string resourcePath = "DialogueData"; // Resources 文件夹下的路径

    /// <summary>
    /// 从整本剧本 JSON 加载指定 block
    /// </summary>
    public static DialogueData LoadBlock(string fileName, string blockId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(blockId))
        {
            Debug.LogError("DialogueLoader: fileName 或 blockId 不能为空");
            return null;
        }

        TextAsset jsonFile = Resources.Load<TextAsset>($"DialogueData/{fileName}");
        if (jsonFile == null)
        {
            Debug.LogError($"DialogueLoader: 找不到文件 DialogueData/{fileName}.json");
            return null;
        }

        try
        {
            DialogueBook book = JsonUtility.FromJson<DialogueBook>(jsonFile.text);
            if (book == null || book.blocks == null || book.blocks.Count == 0)
            {
                Debug.LogError($"DialogueLoader: JSON解析失败或 blocks 为空 - {fileName}");
                return null;
            }

            DialogueBlock block = book.blocks.Find(b => b.blockId == blockId);
            if (block == null)
            {
                Debug.LogError($"DialogueLoader: 在 {fileName} 中找不到 blockId={blockId}");
                return null;
            }

            ValidateLines($"{fileName}:{blockId}", block.lines);

            DialogueData data = new DialogueData
            {
                conversationId = blockId,
                lines = block.lines
            };

            Debug.Log($"DialogueLoader: 成功加载 {fileName}:{blockId}，包含 {data.lines.Count} 句对话");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"DialogueLoader: 解析JSON时出错 - {fileName}:{blockId}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 验证对话句子
    /// </summary>
    private static void ValidateLines(string context, List<DialogueLine> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            DialogueLine line = lines[i];

            if (string.IsNullOrEmpty(line.id))
            {
                line.id = $"{context}_line_{i:000}";
            }

            if (line.mode && string.IsNullOrEmpty(line.text))
            {
                Debug.LogWarning($"DialogueLoader: 预设模式的对话 {line.id} 缺少文本内容");
            }

            if (!line.mode && string.IsNullOrEmpty(line.characterId))
            {
                Debug.LogWarning($"DialogueLoader: LLM模式的对话 {line.id} 缺少角色ID");
            }

            if (!line.mode && (line.endKeywords == null || line.endKeywords.Count == 0))
            {
                line.endKeywords = new List<string> { "结束", "再见", "END", "end" };
            }
        }
    }

    // ==================== 工具方法 ====================

    public static int GetNextPresetLine(DialogueData data, int currentIndex)
    {
        if (data?.lines == null) return -1;
        for (int i = currentIndex + 1; i < data.lines.Count; i++)
        {
            if (data.lines[i].mode) return i;
        }
        return -1;
    }

    public static bool ShouldEndLLMDialogue(string message, List<string> endKeywords)
    {
        if (string.IsNullOrEmpty(message) || endKeywords == null) return false;
        string lowerMessage = message.ToLower().Trim();
        foreach (string keyword in endKeywords)
        {
            if (lowerMessage.Contains(keyword.ToLower())) return true;
        }
        return false;
    }

    public static DialogueLine GetLineAt(DialogueData data, int index)
    {
        if (data?.lines == null || index < 0 || index >= data.lines.Count) return null;
        return data.lines[index];
    }

    public static bool ShouldEndByAI(string aiResponse)
    {
        if (string.IsNullOrEmpty(aiResponse)) return false;

        string lowerResponse = aiResponse.ToLower().Trim();

        // 检查末尾是否有end标记（可以调整检测逻辑）
        return lowerResponse.EndsWith("end") ||
               lowerResponse.EndsWith("end.") ||
               lowerResponse.EndsWith("end。");
    }

    public static string CleanEndMarker(string aiResponse)
    {
        if (string.IsNullOrEmpty(aiResponse)) return aiResponse;

        string cleaned = aiResponse.Trim();

        // 移除末尾的end标记
        if (cleaned.ToLower().EndsWith("end"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3).TrimEnd();
        }
        else if (cleaned.ToLower().EndsWith("end."))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 4).TrimEnd();
        }
        else if (cleaned.ToLower().EndsWith("end。"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 4).TrimEnd();
        }

        return cleaned;
    }
}
