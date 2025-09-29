using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// ·����Ϣ - �򻯵�·�����ݽṹ
/// </summary>
[Serializable]
public class PathInfo
{
    [Header("·������")]
    public string pathId;                    // ·��ID
    public string displayName;               // ��ʾ����
    public GameObject contentContainer;      // ��·���µ���������

    [Header("״̬����")]
    public bool isAccessible = true;         // �Ƿ�ɷ���
    public bool requiresPermission = false;  // �Ƿ���ҪȨ����֤

    [Header("Ȩ������")]
    [Tooltip("��Ҫ������ID�б�ȫ��������ܷ��ʣ�")]
    public List<string> requiredClues = new List<string>();

    [Header("������Ϣ")]
    [SerializeField] public bool isCurrentPath = false; // Inspector����ʾ�Ƿ�Ϊ��ǰ·��

    public PathInfo(string id, string name, GameObject container)
    {
        pathId = id;
        displayName = name;
        contentContainer = container;
        isAccessible = true;
        requiresPermission = false;
    }
}

/// <summary>
/// ��Դ���������Ŀ����� - ����ʵ���汾
/// ���𵥸����ڵ�·���л���������������ַ������
/// </summary>
public class ExplorerManager : MonoBehaviour
{
    [Header("UI�������")]
    public TextMeshProUGUI windowTitleText;          // ���ڱ����ı�����ʾ��ǰ·����

    [Header("Ȩ����ʾ����")]
    [SerializeField] private GameObject accessDeniedWindowPrefab;  // Ȩ�ޱ��ܾ���ʾ����Ԥ����
    [SerializeField] private Transform notificationParent;          // ��ʾ���ڵĸ�����ͨ����Canvas��

    [Header("·������")]
    public List<PathInfo> allPaths = new List<PathInfo>();   // ���п���·��
    public string defaultPathId = "root";                     // Ĭ��·��

    [Header("��������")]
    public string windowTypeName = "�ļ���Դ������";         // ������������
    public bool showPathInTitle = true;                       // �Ƿ��ڱ�������ʾ·��

    [Header("��������")]
    public bool enableDebugLog = true;                        // �Ƿ����õ�����־

    // ʵ���¼�ί�� - ÿ������ʵ���������¼�
    public event Action<string> OnPathChanged;                // ·���л��¼�
    public event Action<string> OnPathAccessDenied;           // ·�����ʱ��ܾ��¼�

    // ��̬�¼�ί�� - ȫ���¼�����ѡ��
    public static event Action<ExplorerManager, string> OnAnyWindowPathChanged;

    // ˽�б���
    private string currentPathId;
    private PathInfo currentPath;
    private Dictionary<string, PathInfo> pathDictionary;
    private GameFlowController flowController;

    // ��̬���� - �������д���ʵ��
    public static List<ExplorerManager> AllInstances { get; private set; } = new List<ExplorerManager>();

    #region Unity��������

    void Awake()
    {
        InitializeManager();
    }

    void OnEnable()
    {
        // ע�ᵽȫ��ʵ���б�
        if (!AllInstances.Contains(this))
        {
            AllInstances.Add(this);
        }

        // �������������¼�������������ʱˢ��·��Ȩ��
        GameEvents.OnClueUnlocked += OnClueUnlockedHandler;
    }

    void Start()
    {
        // ������Ĭ��·��
        NavigateToPath(defaultPathId);
    }

    void OnDisable()
    {
        // ��ȫ��ʵ���б��Ƴ�
        AllInstances.Remove(this);

        // ȡ������
        GameEvents.OnClueUnlocked -= OnClueUnlockedHandler;
    }

    void OnDestroy()
    {
        AllInstances.Remove(this);
    }

    #endregion

    #region ��ʼ��

    void InitializeManager()
    {
        flowController = FindObjectOfType<GameFlowController>();
        // ����·���ֵ�
        pathDictionary = new Dictionary<string, PathInfo>();

        foreach (var path in allPaths)
        {
            if (!string.IsNullOrEmpty(path.pathId))
            {
                pathDictionary[path.pathId] = path;

                // ��ʼʱ����������������
                if (path.contentContainer != null)
                {
                    path.contentContainer.SetActive(false);
                }
            }
        }

        if (enableDebugLog)
        {
            Debug.Log($"ExplorerManager ({name}): ��ʼ����ɣ������� {pathDictionary.Count} ��·��");
        }
    }
    #endregion

    #region ·������

    /// <summary>
    /// ������ָ��·��
    /// </summary>
    public bool NavigateToPath(string targetPathId)
    {

        PathInfo targetPath = pathDictionary[targetPathId];

        // ������Ȩ��
        if (!CanAccessPath(targetPath))
        {
            // �����¼�
            OnPathAccessDenied?.Invoke(targetPathId);
            // ��ʾȨ�ޱ��ܾ���ʾ����
            ShowAccessDeniedNotification();

            return false;
        }

        // ִ��·���л�
        return SwitchToPath(targetPath);
    }

    /// <summary>
    /// �л���ָ��·��
    /// </summary>
    private bool SwitchToPath(PathInfo targetPath)
    {
        string previousPathId = currentPathId;

        // ���ص�ǰ·��������
        if (currentPath?.contentContainer != null)
        {
            currentPath.contentContainer.SetActive(false);
            currentPath.isCurrentPath = false;
        }

        // �л�����·��
        currentPathId = targetPath.pathId;
        currentPath = targetPath;

        // ��ʾ��·��������
        if (currentPath.contentContainer != null)
        {
            currentPath.contentContainer.SetActive(true);
            currentPath.isCurrentPath = true;
        }

        // ���µ�ַ��
        UpdateWindowTitle();

        // �����¼�
        OnPathChanged?.Invoke(currentPathId);
        OnAnyWindowPathChanged?.Invoke(this, currentPathId);

        if (enableDebugLog)
        {
            Debug.Log($"ExplorerManager ({name}): ·���л��ɹ� - {previousPathId} �� {currentPathId}");
        }

        return true;
    }

    #endregion

    #region Ȩ�޼��

    /// <summary>
    /// ����Ƿ���Է���ָ��·��
    /// </summary>
    private bool CanAccessPath(PathInfo path)
    {
        // �������ɷ�����
        if (!path.isAccessible)
        {
            return false;
        }

        if (!path.requiresPermission)
        {
            return true;
        }

        // ����Ƿ���Ҫ����Ȩ��
        if (path.requiredClues != null && path.requiredClues.Count > 0)
        {
            // ���ÿ�����������
            foreach (string clueId in path.requiredClues)
            {
                if (!flowController.HasClue(clueId))
                {
                    if (enableDebugLog)
                    {
                        Debug.Log($"ExplorerManager ({name}): ȱ�ٱ������� - {clueId}");
                    }
                    return false;
                }
            }
        }

        // ͨ�����м��
        return true;
    }

    /// <summary>
    /// ���������¼�������
    /// </summary>
    private void OnClueUnlockedHandler(string clueId)
    {
        // ����������ʱ������Ƿ���·����˱�ÿɷ���
        foreach (var path in allPaths)
        {
            if (path.requiresPermission && path.requiredClues.Contains(clueId))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"ExplorerManager ({name}): ���� {clueId} ������·�� {path.pathId} ���ܱ�ÿɷ���");
                }
            }
        }
    }

    #endregion

    #region Ȩ����ʾ����

    private void ShowAccessDeniedNotification()
    {
        // ȷ������λ��
        Transform parent = notificationParent;
        if (parent == null)
        {
            // ������Ϊ "DialogueCanvas" �� GameObject
            GameObject dialogueCanvasObj = GameObject.Find("DialogueCanvas");
            if (dialogueCanvasObj != null)
            {
                parent = dialogueCanvasObj.transform;
            }
        }
        // ������ʾ���ڣ�Ԥ�������Ѿ�Ԥ����������ݣ�
        GameObject notificationObj = Instantiate(accessDeniedWindowPrefab, parent);
    }

    #endregion

    #region UI����

    /// <summary>
    /// ���´��ڱ��⣨��ַ����
    /// </summary>
    private void UpdateWindowTitle()
    {
        if (windowTitleText != null && currentPath != null)
        {
            string displayText;

            if (showPathInTitle)
            {
                // �����ʾ������������ - ·��
                string pathName = currentPath.displayName;
                displayText = $"{pathName}";
            }
            else
            {
                // ����ʾ����������
                displayText = windowTypeName;
            }

            windowTitleText.text = displayText;
        }
    }

    #endregion

    #region �����ӿ�

    /// <summary>
    /// ��ȡ��ǰ·��ID
    /// </summary>
    public string GetCurrentPathId()
    {
        return currentPathId;
    }

    #endregion
}