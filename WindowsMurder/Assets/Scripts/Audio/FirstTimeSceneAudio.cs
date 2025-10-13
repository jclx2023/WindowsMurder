using UnityEngine;

/// <summary>
/// ��Ϸ�Ự�״γ�����Ƶ������
/// ÿ��������Ϸ��һ�ν��볡��ʱ���ţ�ͬһ��Ϸ�Ự���ٴν��벻����
/// </summary>
public class SessionFirstTimeAudio : MonoBehaviour
{
    [Header("��Ƶ����")]
    [SerializeField] private AudioClip audioClip;

    [Header("������ʶ")]
    [Tooltip("�������Զ�ʹ�õ�ǰ��������")]
    [SerializeField] private string sceneIdentifier = "";

    [Header("����")]
    [SerializeField] private bool debugMode = true;

    // ��̬�ֵ��¼��ǰ��Ϸ�Ự���Ѳ��ŵĳ���
    private static System.Collections.Generic.HashSet<string> playedScenes =
        new System.Collections.Generic.HashSet<string>();

    void Start()
    {
        // �Զ���ȡ����������Ϊ��ʶ��
        if (string.IsNullOrEmpty(sceneIdentifier))
        {
            sceneIdentifier = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }

        // ��鵱ǰ�Ự�Ƿ��Ѳ��Ź�
        if (!playedScenes.Contains(sceneIdentifier))
        {
            PlayAudio();
            playedScenes.Add(sceneIdentifier);
        }
    }

    /// <summary>
    /// ������Ƶ
    /// </summary>
    private void PlayAudio()
    {
        GlobalSystemManager.Instance.PlaySFX(audioClip);
        LogDebug($"���ų����״ν�����Ƶ: {audioClip.name}");
    }
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SessionFirstTimeAudio] {message}");
        }
    }
}