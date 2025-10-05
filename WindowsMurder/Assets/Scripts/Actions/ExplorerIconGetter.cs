using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Explorerͼ���ȡ�� - �ṩExplorer������icons������
/// ������ExplorerԤ�����ϣ���Ԥ����༭ģʽ������icons����
/// </summary>
public class ExplorerIconGetter : MonoBehaviour
{
    [Header("Explorer�еĳ���Icons")]
    [Tooltip("IEͼ�꣨����������Ϊ��������ʾʱ����")]
    [SerializeField] private GameObject ieIcon;

    [Tooltip("��������ͼ���б�")]
    [SerializeField] private List<GameObject> programIcons = new List<GameObject>();

    #region �����ӿ�

    /// <summary>
    /// ��ȡIEͼ��
    /// </summary>
    public GameObject GetIEIcon()
    {
        return ieIcon;
    }

    /// <summary>
    /// ��ȡ��������ͼ���б�
    /// </summary>
    public List<GameObject> GetProgramIcons()
    {
        return programIcons;
    }

    #endregion
}