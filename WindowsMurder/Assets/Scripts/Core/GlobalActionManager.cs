using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ȫ�ֹ��ܹ����� - ����ת��Ч����ҵ���߼�����
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

    [Header("ת��Ч������")]
    public float clearPixel = 500f;     // ����̬
    public float blurPixel = 6f;        // ģ��̬�����ػ��̶ȣ���ֵԽСԽ����
    public float preDuration = 0.5f;   // �볡ǰ���ʱ��
    public float postDuration = 0.55f;  // �볡�������ʱ��
    public float jitterDuring = 0.9f;   // �����ж���ǿ��
    public float jitterClear = 0.9f;    // �ȶ��󶶶�ǿ��
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ����ʱЯ�����³����ĳ�ʼֵ����֤�޷죩
    private float carryPixel;
    private float carryJitter;
    private bool postSceneReady;
    private Material postSceneMat; // �³����ҵ��Ĳ���

    // ����Ƿ�������Ϸ���Ǽ�����Ϸ
    private bool isNewGame = false;
    private bool isContinueGame = false;

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

        // ȷ��SaveManager����
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("GlobalActionManager: SaveManagerδ��ʼ�������Դ���");
            GameObject saveManagerObj = new GameObject("SaveManager");
            saveManagerObj.AddComponent<SaveManager>();
        }
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

        // �������Ϸ�������ȱ������
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                Debug.Log("GlobalActionManager: �ѱ��浱ǰ��Ϸ����");
            }
        }

        // ���ñ��
        isNewGame = false;
        isContinueGame = false;

        PlaySound(sceneTransitionSound);
        SwitchSceneWithEffect("MainMenu");
    }

    /// <summary>
    /// ��ʼ����Ϸ
    /// </summary>
    public void NewGame()
    {
        Debug.Log("GlobalActionManager: ��ʼ����Ϸ");

        // ɾ���ɴ浵
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            Debug.Log("GlobalActionManager: ��ɾ���ɴ浵");
        }

        // ͬʱͨ��GlobalSystemManagerɾ�������ּ��ݣ�
        if (GlobalSystemManager.Instance != null)
        {
            GlobalSystemManager.Instance.DeleteSave();
        }

        // ���ñ��
        isNewGame = true;
        isContinueGame = false;

        PlaySound(sceneTransitionSound);
        SwitchSceneWithEffect("GameScene");
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public void Continue()
    {
        // ����Ƿ��д浵
        bool hasSave = false;

        // ����ʹ��SaveManager���
        if (SaveManager.Instance != null)
        {
            hasSave = SaveManager.Instance.HasSaveData();
        }
        // ���ã�ͨ��GlobalSystemManager���
        else if (GlobalSystemManager.Instance != null)
        {
            hasSave = GlobalSystemManager.Instance.HasGameSave();
        }

        if (!hasSave)
        {
            Debug.LogWarning("GlobalActionManager: �޷�������Ϸ��û���ҵ��浵");
            // TODO: ������������ʾ��ʾ����
            return;
        }

        Debug.Log("GlobalActionManager: ������Ϸ");

        // ���ش浵����
        if (SaveManager.Instance != null)
        {
            if (SaveManager.Instance.LoadGame())
            {
                Debug.Log("GlobalActionManager: �浵�����Ѽ��ص��ڴ�");
            }
            else
            {
                Debug.LogError("GlobalActionManager: ���ش浵ʧ��");
                return;
            }
        }

        // ���ñ��
        isNewGame = false;
        isContinueGame = true;

        PlaySound(sceneTransitionSound);
        SwitchSceneWithEffect("GameScene");
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

        // �˳�ǰ������Ϸ���������Ϸ������
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
                Debug.Log("GlobalActionManager: �˳�ǰ�ѱ�����Ϸ");
            }
        }

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

    // ==================== ״̬��ѯ�ӿ� ====================

    /// <summary>
    /// ����Ƿ�����Ϸ�浵
    /// </summary>
    public bool HasGameSave()
    {
        // ����ʹ��SaveManager
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.HasSaveData();
        }
        // ���ã�ʹ��GlobalSystemManager
        else if (GlobalSystemManager.Instance != null)
        {
            return GlobalSystemManager.Instance.HasGameSave();
        }
        return false;
    }

    /// <summary>
    /// ��ȡ��ǰ��Ϸģʽ������Ϸ/������Ϸ��
    /// </summary>
    public bool IsNewGame() { return isNewGame; }
    public bool IsContinueGame() { return isContinueGame; }

    // ==================== ת��Ч��ϵͳ ====================

    /// <summary>
    /// ��ת��Ч���ĳ����л�
    /// </summary>
    public void SwitchSceneWithEffect(string sceneName)
    {
        StartCoroutine(SceneTransitionCoroutine(sceneName));
    }

    private IEnumerator SceneTransitionCoroutine(string sceneName)
    {
        Debug.Log($"GlobalActionManager: ��ʼת���л��� {sceneName}");

        // 1) �뿪����ǰ���ҵ���ǰ������ MainMenuBack ��������������ģ��
        Material curMat = FindMainMenuBackMaterial();

        float startPixel = GetFloatSafe(curMat, "_PixelResolution", clearPixel);
        float startJitter = GetFloatSafe(curMat, "_JitterStrength", jitterClear);

        // Ԥ���ɣ����� �� ģ��
        yield return AnimateTwoFloats(
            curMat,
            "_PixelResolution", startPixel, blurPixel,
            "_JitterStrength", startJitter, Mathf.Max(jitterDuring, startJitter),
            preDuration, ease
        );

        // 2) �첽�����³�����׼�����³����� MainMenuBack ��ʼ��Ϊ"ģ��̬"
        carryPixel = blurPixel;
        carryJitter = jitterDuring;
        postSceneReady = false;
        postSceneMat = null;

        SceneManager.sceneLoaded += OnSceneLoadedSetInitial;

        var async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;
        while (async.progress < 0.9f) // �ȴ�������ȫ��Unity �� 0.9 ��ʾ����ɣ������
            yield return null;

        // ������������������̴��� sceneLoaded �ص�
        async.allowSceneActivation = true;

        // �ȴ������ڻص�����³����� MainMenuBack ��ʼ����
        while (!postSceneReady)
            yield return null;

        SceneManager.sceneLoaded -= OnSceneLoadedSetInitial;

        // 3) �볡�󣺰��³����� MainMenuBack ��ģ��������
        if (postSceneMat != null)
        {
            yield return AnimateTwoFloats(
                postSceneMat,
                "_PixelResolution", carryPixel, clearPixel,
                "_JitterStrength", carryJitter, jitterClear,
                postDuration, ease
            );

            // ȷ������״̬��ȷ����
            postSceneMat.SetFloat("_PixelResolution", clearPixel);
            postSceneMat.SetFloat("_JitterStrength", jitterClear);
            Debug.Log($"ת����ɣ�����״̬: PixelResolution={clearPixel}, JitterStrength={jitterClear}");
        }

        // 4) �����л���ɺ�Ķ��⴦��
        if (sceneName == "GameScene")
        {
            // ֪ͨSaveManager�����Ѽ������
            // SaveManager����OnSceneLoaded���Զ�����ָ��߼�
            Debug.Log($"GlobalActionManager: GameScene������ɣ�ģʽ: ����Ϸ={isNewGame}, ����={isContinueGame}");
        }

        // �����л���ɺ����ñ��
        if (sceneName == "MainMenu")
        {
            isNewGame = false;
            isContinueGame = false;
        }
    }

    // ���³�������ʱ���ã����ҵ� MainMenuBack ���Ѳ���ֱ����Ϊ"ģ�����"������һ֡������
    private void OnSceneLoadedSetInitial(Scene scene, LoadSceneMode mode)
    {
        var mat = FindMainMenuBackMaterial();
        if (mat != null)
        {
            SetFloatSafe(mat, "_PixelResolution", carryPixel);
            SetFloatSafe(mat, "_JitterStrength", carryJitter);
            postSceneMat = mat;
        }
        postSceneReady = true;
    }

    // ==================== ת��Ч�����߷��� ====================

    private Material FindMainMenuBackMaterial()
    {
        // ���� GameObject.Find��ֻ�Ҽ������
        GameObject go = GameObject.Find("MainMenuBack");
        Image img = null;

        if (go != null) img = go.GetComponent<Image>();

        // ȷ��Ϊ��ǰ���󴴽�����ʵ��������ĵ� sharedMaterial
        //��ֻ����Ҫ���ƶ����Ķ�������������
        img.material = new Material(img.material);
        return img.material;
    }

    private IEnumerator AnimateTwoFloats(
        Material mat,
        string propA, float fromA, float toA,
        string propB, float fromB, float toB,
        float duration, AnimationCurve curve)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            float e = curve != null ? curve.Evaluate(k) : k;

            mat.SetFloat(propA, Mathf.Lerp(fromA, toA, e));
            mat.SetFloat(propB, Mathf.Lerp(fromB, toB, e));

            yield return null;
        }

        // ȷ������ֵ��ȷ����
        mat.SetFloat(propA, toA);
        mat.SetFloat(propB, toB);
    }

    private float GetFloatSafe(Material m, string name, float fallback)
    {
        return m.HasProperty(name) ? m.GetFloat(name) : fallback;
    }

    private void SetFloatSafe(Material m, string name, float v)
    {
        if (m != null && m.HasProperty(name)) m.SetFloat(name, v);
    }
}