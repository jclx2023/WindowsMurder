using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ��Ϸ��ʼ���� - ��Ϸ�����ĵ�һ������
/// ���𴴽�ȫ��ϵͳ����ת���˵�
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("��������")]
    public string mainMenuSceneName = "MainMenu";

    [Header("ϵͳ����")]
    public float systemLoadingDelay = 1f; // �ȴ�ȫ��ϵͳ������ɵ��ӳ�

    [Header("��Ч")]
    public AudioClip startupSound;

    [Header("ȫ��ϵͳ")]
    public GameObject globalSystemPrefab; // ȫ��ϵͳԤ����

    // ˽�б���
    private AudioSource audioSource;

    void Start()
    {
        // ��ȡ��Ƶ���
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // ��ʼ��ʼ������
        StartCoroutine(InitializationSequence());
    }

    /// <summary>
    /// �����ĳ�ʼ������
    /// </summary>
    IEnumerator InitializationSequence()
    {
        Debug.Log("��Ϸ��ʼ����ʼ...");

        // 1. ���ſ�����Ч
        PlayStartupSound();

        // 2. ����ȫ��ϵͳ
        CreateGlobalSystem();

        // 3. �ȴ�ȫ��ϵͳ��ȫ����
        yield return StartCoroutine(WaitForSystemReady());

        // 4. ��ת�����˵�
        yield return StartCoroutine(TransitionToMainMenu());
    }

    /// <summary>
    /// ���ſ�����Ч
    /// </summary>
    void PlayStartupSound()
    {
        if (startupSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(startupSound);
            Debug.Log("����Windows������Ч");
        }
    }

    /// <summary>
    /// ����ȫ��ϵͳ
    /// </summary>
    void CreateGlobalSystem()
    {
        Debug.Log("��ʼ����ȫ��ϵͳ...");

        GameObject globalSystem = null;

        // ���Դ�Ԥ���崴��
        if (globalSystemPrefab != null)
        {
            globalSystem = Instantiate(globalSystemPrefab);
            globalSystem.name = "GlobalSystemManager"; // ȥ��(Clone)��׺
            Debug.Log("��Ԥ���崴��ȫ��ϵͳ");
        }
        else
        {
            // ���Դ�Resources����
            GameObject prefab = Resources.Load<GameObject>("Prefabs/GlobalSystemManager");
            if (prefab != null)
            {
                globalSystem = Instantiate(prefab);
                globalSystem.name = "GlobalSystemManager";
                Debug.Log("��Resources����ȫ��ϵͳ");
            }
            else
            {
                // ֱ�Ӵ���GameObject��������
                globalSystem = new GameObject("GlobalSystemManager");
                globalSystem.AddComponent<GlobalSystemManager>();
                Debug.Log("ֱ�Ӵ���ȫ��ϵͳ����");
            }
        }

        // ����Ϊ�糡��������
        if (globalSystem != null)
        {
            DontDestroyOnLoad(globalSystem);
            Debug.Log("ȫ��ϵͳ�����ɹ���������ΪDontDestroyOnLoad");
        }
        else
        {
            Debug.LogError("ȫ��ϵͳ����ʧ�ܣ�");
        }
    }

    /// <summary>
    /// �ȴ�ȫ��ϵͳ׼������
    /// </summary>
    IEnumerator WaitForSystemReady()
    {
        Debug.Log("�ȴ�ȫ��ϵͳ��ʼ�����...");

        // �ȴ��̶�ʱ�䣬ȷ��GlobalSystemManager��Awake��Start���
        yield return new WaitForSeconds(systemLoadingDelay);

        // ���GlobalSystemManager�Ƿ���ڲ���ʼ�����
        GlobalSystemManager globalManager = FindObjectOfType<GlobalSystemManager>();

        if (globalManager != null && GlobalSystemManager.Instance != null)
        {
            Debug.Log("ȫ��ϵͳ��ʼ�����");
        }
        else
        {
            Debug.LogWarning("ȫ��ϵͳ����δ��ȷ��ʼ��");
        }

        // ����İ�ȫ�ӳ�
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// ��ת�����˵�����
    /// </summary>
    IEnumerator TransitionToMainMenu()
    {
        Debug.Log("׼����ת�����˵�...");

        // �����ӳ٣�ģ����ʵ��ϵͳ��Ӧʱ��
        yield return new WaitForSeconds(0.5f);

        // �������˵�����
        Debug.Log($"���س���: {mainMenuSceneName}");
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Ӧ�ó�����ͣʱ�Ĵ�����ѡ��
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("��Ϸ��ʼ��������Ӧ����ͣ");
        }
    }

    /// <summary>
    /// Ӧ�ó��򽹵�仯ʱ�Ĵ�����ѡ��
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Debug.Log("��Ϸ��ʼ��������ʧȥ����");
        }
    }
}