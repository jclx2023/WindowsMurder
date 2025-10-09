using System;
using UnityEngine;

/// <summary>
/// 游戏静态事件系统 - 用于解耦组件通信
/// </summary>
public static class GameEvents
{
    // 线索相关事件
    public static event Action<string> OnClueUnlockRequested;
    public static event Action<string> OnClueUnlocked;

    // 对话相关事件
    public static event Action<string> OnDialogueBlockRequested;
    public static event Action<string> OnDialogueBlockCompleted;

    // Stage相关事件
    public static event Action<string> OnStageChangeRequested;
    public static event Action<string> OnStageChanged;

    /// <summary>
    /// 通知对话块完成
    /// </summary>
    public static void NotifyDialogueBlockCompleted(string blockId)
    {
        OnDialogueBlockCompleted?.Invoke(blockId);
    }
    /// <summary>
    /// 请求解锁线索（任何地方都可以调用）
    /// </summary>
    public static void RequestUnlockClue(string clueId)
    {
        if (string.IsNullOrEmpty(clueId))
        {
            Debug.LogWarning("GameEvents: 线索ID为空");
            return;
        }

        Debug.Log($"GameEvents: 请求解锁线索 {clueId}");
        OnClueUnlockRequested?.Invoke(clueId);
    }

    /// <summary>
    /// 通知线索已解锁（由GameFlowController调用）
    /// </summary>
    public static void NotifyClueUnlocked(string clueId)
    {
        OnClueUnlocked?.Invoke(clueId);
    }

    /// <summary>
    /// 清理所有事件订阅（场景卸载时调用，防止内存泄漏）
    /// </summary>
    public static void ClearAllEvents()
    {
        OnClueUnlockRequested = null;
        OnClueUnlocked = null;
        OnDialogueBlockRequested = null;
        OnDialogueBlockCompleted = null;
        OnStageChangeRequested = null;
        OnStageChanged = null;

        Debug.Log("GameEvents: 已清理所有事件订阅");
    }
}