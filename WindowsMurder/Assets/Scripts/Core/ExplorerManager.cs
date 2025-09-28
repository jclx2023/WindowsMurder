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

    [Header("·������")]
    public List<PathInfo> allPaths = new List<PathInfo>();   // ���п���·��
    public string defaultPathId = "root";                 // Ĭ��·��

    [Header("��������")]
    public string windowTypeName = "�ļ���Դ������";         // ������������
    public bool showPathInTitle = true;                      // �Ƿ��ڱ�������ʾ·��

    [Header("��������")]
    public bool enableDebugLog = true;                       // �Ƿ����õ�����־

    // ʵ���¼�ί�� - ÿ������ʵ���������¼�
    public event Action<string> OnPathChanged;               // ·���л��¼�
    public event Action<string> OnPathAccessDenied;          // ·�����ʱ��ܾ��¼�

    // ��̬�¼�ί�� - ȫ���¼�����ѡ��
    public static event Action<ExplorerManager, string> OnAnyWindowPathChanged;

    // ˽�б���
    private string currentPathId;
    private PathInfo currentPath;
    private Dictionary<string, PathInfo> pathDictionary;

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
    }

    void OnDestroy()
    {
        AllInstances.Remove(this);
    }

    #endregion

    #region ��ʼ��

    void InitializeManager()
    {
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
            if (enableDebugLog)
            {
                Debug.Log($"ExplorerManager ({name}): ·�����ʱ��ܾ� - {targetPathId}");
            }

            OnPathAccessDenied?.Invoke(targetPathId);
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
        if (!path.isAccessible)
        {
            return false;
        }

        if (path.requiresPermission)
        {
            // TODO: ������ϷȨ��ϵͳ
            // ����ͨ����̬�ӿڲ�ѯȫ��Ȩ��״̬
            // return SystemPermissionManager.HasPermission(path.pathId);
            return true;
        }

        return true;
    }

    /// <summary>
    /// ����·���ķ���Ȩ��
    /// </summary>
    public void SetPathAccessible(string pathId, bool accessible)
    {
        if (pathDictionary.ContainsKey(pathId))
        {
            pathDictionary[pathId].isAccessible = accessible;

            if (enableDebugLog)
            {
                Debug.Log($"ExplorerManager ({name}): ·�� {pathId} ����Ȩ������Ϊ {accessible}");
            }
        }
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

    /// <summary>
    /// ��ȡ��ǰ·����Ϣ
    /// </summary>
    public PathInfo GetCurrentPath()
    {
        return currentPath;
    }

    #endregion

    #region ��̬���߷���

    /// <summary>
    /// ������ʾָ��·���Ĵ���ʵ��
    /// </summary>
    public static ExplorerManager FindWindowWithPath(string pathId)
    {
        foreach (var instance in AllInstances)
        {
            if (instance.GetCurrentPathId() == pathId)
            {
                return instance;
            }
        }
        return null;
    }

    /// <summary>
    /// ��ȡ���л�Ծ����Դ��������������
    /// </summary>
    public static int GetActiveWindowCount()
    {
        return AllInstances.Count;
    }

    #endregion
}