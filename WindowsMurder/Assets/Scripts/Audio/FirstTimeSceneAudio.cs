using UnityEngine;

/// <summary>
/// 游戏会话首次场景音频播放器
/// 每次启动游戏第一次进入场景时播放，同一游戏会话内再次进入不播放
/// </summary>
public class SessionFirstTimeAudio : MonoBehaviour
{
    [Header("音频设置")]
    [SerializeField] private AudioClip audioClip;

    [Header("场景标识")]
    [Tooltip("留空则自动使用当前场景名称")]
    [SerializeField] private string sceneIdentifier = "";

    [Header("调试")]
    [SerializeField] private bool debugMode = true;

    // 静态字典记录当前游戏会话中已播放的场景
    private static System.Collections.Generic.HashSet<string> playedScenes =
        new System.Collections.Generic.HashSet<string>();

    void Start()
    {
        // 自动获取场景名称作为标识符
        if (string.IsNullOrEmpty(sceneIdentifier))
        {
            sceneIdentifier = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        // 检查当前会话是否已播放过
        if (!playedScenes.Contains(sceneIdentifier))
        {
            PlayAudio();
            playedScenes.Add(sceneIdentifier);
        }
    }

    /// <summary>
    /// 播放音频
    /// </summary>
    private void PlayAudio()
    {
        GlobalSystemManager.Instance.PlaySFX(audioClip);
        LogDebug($"播放场景首次进入音频: {audioClip.name}");
    }
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SessionFirstTimeAudio] {message}");
        }
    }
}