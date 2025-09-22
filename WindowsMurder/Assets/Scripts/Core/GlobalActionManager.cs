using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ȫ�ֹ��ܹ����� - רע��ҵ���߼�����
/// ��GlobalTaskBarManager���������û������ľ���ҵ������
/// </summary>
public class GlobalActionManager : MonoBehaviour
{
    public static GlobalActionManager Instance;

    [Header("����Ԥ����")]
    public GameObject languageSettingsPrefab;
    public GameObject displaySettingsPrefab;
    public GameObject creditsWindowPrefab;

    [Header("��Ч")]
    public AudioClip windowOpenSound;
    public AudioClip sceneTransitionSound;

    void Awake()
    {
        // ����Ƿ�TaskBar����
        if (GlobalTaskBarManager.Instance == null)
        {
            Debug.LogError("GlobalActionManager: Ӧ����GlobalTaskBarManager�����͹���");
            Destroy(gameObject);
            return;
        }

        // ����ģʽ
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// ��ʼ������������GlobalTaskBarManager���ã�
    /// </summary>
    public void InitializeManager()
    {
        Debug.Log("GlobalActionManager ��ʼ����ɣ��ȴ������û�����");
    }

    /// <summary>
    /// ������Ч - ί�и�GlobalSystemManager
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.PlaySFX(clip);
        }
    }

    /// <summary>
    /// �������ڵ�ͨ�÷���
    /// </summary>
    GameObject CreateWindow(GameObject windowPrefab)
    {
        // ���ҵ�ǰ������Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("GlobalActionManager: �Ҳ���Canvas���޷���������");
            return null;
        }

        // ��������
        GameObject window = Instantiate(windowPrefab, canvas.transform);
        PlaySound(windowOpenSound);

        Debug.Log($"GlobalActionManager: �����˴���");
        return window;
    }

    // ==================== ȫ�ֹ��ܽӿ� ====================

    /// <summary>
    /// �������˵�
    /// </summary>
    public void BackToMainMenu()
    {
        Debug.Log("GlobalActionManager: �������˵�");
        PlaySound(sceneTransitionSound);
        StartCoroutine(LoadSceneAsync("MainMenu"));
    }

    /// <summary>
    /// ��ʼ����Ϸ
    /// </summary>
    public void NewGame()
    {
        Debug.Log("GlobalActionManager: ��ʼ����Ϸ");

        // ͨ��GlobalSystemManagerɾ���ɴ浵
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.DeleteSave();
        }

        PlaySound(sceneTransitionSound);
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public void Continue()
    {
        if (!HasGameSave())
        {
            Debug.LogWarning("GlobalActionManager: �޷�������Ϸ��û���ҵ��浵");
            // ������������ʾ��ʾ����
            return;
        }

        Debug.Log("GlobalActionManager: ������Ϸ");
        PlaySound(sceneTransitionSound);
        StartCoroutine(LoadSceneAsync("GameScene"));
    }

    /// <summary>
    /// ����������
    /// </summary>
    public void OpenLanguageSettings()
    {
        Debug.Log("GlobalActionManager: ����������");
        CreateWindow(languageSettingsPrefab);
    }

    /// <summary>
    /// ����ʾ����
    /// </summary>
    public void OpenDisplaySettings()
    {
        Debug.Log("GlobalActionManager: ����ʾ����");
        CreateWindow(displaySettingsPrefab);
    }

    /// <summary>
    /// ��������Ա����
    /// </summary>
    public void OpenCredits()
    {
        Debug.Log("GlobalActionManager: ��������Ա����");
        CreateWindow(creditsWindowPrefab);
    }

    /// <summary>
    /// �ػ�/�˳���Ϸ
    /// </summary>
    public void Shutdown()
    {
        Debug.Log("GlobalActionManager: �˳���Ϸ");

        // ͨ��GlobalSystemManager�˳�
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.QuitApplication();
        }
        else
        {
            // �����˳�����
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // ==================== ״̬��ѯ�ӿڣ�ί�и�GlobalSystemManager�� ====================

    /// <summary>
    /// ����Ƿ�����Ϸ�浵
    /// </summary>
    public bool HasGameSave()
    {
        if (GlobalSystemManager.Instance != null)
        {
            return GlobalSystemManager.Instance.HasGameSave();
        }
        return false;
    }

    /// <summary>
    /// ��ȡ��ǰ��������
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// ����Ƿ������˵�����
    /// </summary>
    public bool IsInMainMenuScene()
    {
        return GetCurrentSceneName() == "MainMenu";
    }

    /// <summary>
    /// ����Ƿ�����Ϸ����
    /// </summary>
    public bool IsInGameScene()
    {
        string sceneName = GetCurrentSceneName();
        return sceneName.Contains("Game") || sceneName == "GameScene";
    }

    // ==================== �������� ====================

    /// <summary>
    /// �첽���س���
    /// </summary>
    IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"GlobalActionManager: ��ʼ���س��� {sceneName}");

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        Debug.Log($"GlobalActionManager: ���� {sceneName} �������");
    }

    // ==================== ��ݷ�����ί�и�GlobalSystemManager�� ====================

    /// <summary>
    /// ��ȡ�����ı�
    /// </summary>
    public string GetText(string key)
    {
        if (GlobalSystemManager.Instance != null)
        {
            return GlobalSystemManager.Instance.GetText(key);
        }
        return key;
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetLanguage(SupportedLanguage language)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetLanguage(language);
        }
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void SetVolume(float master, float sfx, float music)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetVolume(master, sfx, music);
        }
    }

    /// <summary>
    /// ������ʾģʽ
    /// </summary>
    public void SetDisplay(bool fullscreen, Vector2Int resolution)
    {
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.SetDisplay(fullscreen, resolution);
        }
    }
}