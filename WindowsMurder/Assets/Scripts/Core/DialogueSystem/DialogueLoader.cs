using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����Ի�������������֧�ֶ����ԣ�
/// </summary>
[Serializable]
public class DialogueLine
{
    public string id;
    public bool mode;         // "T=preset" / "F=llm"
    public string characterId;  // ˵����ɫID��preset��������ʾĳ��ɫ̨�ʣ�llm����ʾ��ǰ��Player�͸���ѯĿ��ĶԻ���
    public string text;         // �� preset ��ʹ��
    public string portraitId;   // ����ID
}


/// <summary>
/// �����Ի��Σ����ԣ�
/// </summary>
[Serializable]
public class DialogueData
{
    public string conversationId;       // �Ի���ID
    public List<DialogueLine> lines;    // ̨��˳��
}

/// <summary>
/// �Ի����ݼ�����
/// </summary>
public static class DialogueLoader
{
    /// <summary>
    /// �� Resources/DialogueData/ �¼��� JSON �ļ�
    /// </summary>
    public static DialogueData Load(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>($"DialogueData/{fileName}");
        if (jsonFile == null)
        {
            Debug.LogError($"DialogueLoader: �Ҳ����ļ� {fileName}.json");
            return null;
        }

        return JsonUtility.FromJson<DialogueData>(jsonFile.text);
    }
}
