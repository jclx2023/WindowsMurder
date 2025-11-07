using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Explorer图标获取器 - 提供Explorer窗口内icons的引用
/// 挂载在Explorer预制体上，在预制体编辑模式中配置icons引用
/// </summary>
public class ExplorerIconGetter : MonoBehaviour
{
    [Header("Explorer中的程序Icons")]
    [Tooltip("IE图标（单独管理，因为有特殊显示时机）")]
    [SerializeField] private GameObject ieIcon;

    [Tooltip("其他程序图标列表")]
    [SerializeField] private List<GameObject> programIcons = new List<GameObject>();

    #region 公共接口

    /// <summary>
    /// 获取IE图标
    /// </summary>
    public GameObject GetIEIcon()
    {
        return ieIcon;
    }

    /// <summary>
    /// 获取其他程序图标列表
    /// </summary>
    public List<GameObject> GetProgramIcons()
    {
        return programIcons;
    }

    #endregion
}
