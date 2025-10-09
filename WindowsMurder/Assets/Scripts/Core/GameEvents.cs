using System;
using UnityEngine;

/// <summary>
/// ��Ϸ��̬�¼�ϵͳ - ���ڽ������ͨ��
/// </summary>
public static class GameEvents
{
    // ��������¼�
    public static event Action<string> OnClueUnlockRequested;
    public static event Action<string> OnClueUnlocked;

    // �Ի�����¼�
    public static event Action<string> OnDialogueBlockRequested;
    public static event Action<string> OnDialogueBlockCompleted;

    // Stage����¼�
    public static event Action<string> OnStageChangeRequested;
    public static event Action<string> OnStageChanged;

    /// <summary>
    /// ֪ͨ�Ի������
    /// </summary>
    public static void NotifyDialogueBlockCompleted(string blockId)
    {
        OnDialogueBlockCompleted?.Invoke(blockId);
    }
    /// <summary>
    /// ��������������κεط������Ե��ã�
    /// </summary>
    public static void RequestUnlockClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId))
        {
            Debug.LogWarning("GameEvents: ����IDΪ��");
            return;
        }

        Debug.Log($"GameEvents: ����������� {clueId}");
        OnClueUnlockRequested?.Invoke(clueId);
    }

    /// <summary>
    /// ֪ͨ�����ѽ�������GameFlowController���ã�
    /// </summary>
    public static void NotifyClueUnlocked(string clueId)
    {
        OnClueUnlocked?.Invoke(clueId);
    }

    /// <summary>
    /// ���������¼����ģ�����ж��ʱ���ã���ֹ�ڴ�й©��
    /// </summary>
    public static void ClearAllEvents()
    {
        OnClueUnlockRequested = null;
        OnClueUnlocked = null;
        OnDialogueBlockRequested = null;
        OnDialogueBlockCompleted = null;
        OnStageChangeRequested = null;
        OnStageChanged = null;

        Debug.Log("GameEvents: �����������¼�����");
    }
}