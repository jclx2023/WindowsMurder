using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 扫雷单元格 - 简化版，统一从SpriteManager获取资源
/// </summary>
public class MinesweeperCell : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件")]
    public Image cellImage;         // 背景层（凸起/平坦）
    public Image contentImage;      // 内容层（数字/地雷/旗帜）

    private int x, y;
    private bool isMine = false;
    private bool isRevealed = false;
    private bool isFlagged = false;
    private int adjacentMines = 0;

    private MinesweeperGame gameController;
    private MinesweeperSpriteManager sprites;

    // 属性
    public int X => x;
    public int Y => y;
    public bool IsMine => isMine;
    public bool IsRevealed => isRevealed;
    public bool IsFlagged => isFlagged;
    public int AdjacentMines => adjacentMines;

    /// <summary>
    /// 初始化格子
    /// </summary>
    public void Initialize(int x, int y, MinesweeperGame controller, MinesweeperSpriteManager spriteManager)
    {
        this.x = x;
        this.y = y;
        this.gameController = controller;
        this.sprites = spriteManager;

        // 初始状态：未翻开
        UpdateVisual();
    }

    /// <summary>
    /// 设置是否为地雷
    /// </summary>
    public void SetMine(bool value)
    {
        isMine = value;
    }

    /// <summary>
    /// 设置周围地雷数
    /// </summary>
    public void SetAdjacentMines(int count)
    {
        adjacentMines = count;
    }

    /// <summary>
    /// 设置旗帜状态
    /// </summary>
    public void SetFlag(bool value)
    {
        isFlagged = value;
        UpdateVisual();
    }

    /// <summary>
    /// 翻开格子
    /// </summary>
    public void Reveal()
    {
        if (isRevealed) return;

        isRevealed = true;
        UpdateVisual();
    }

    /// <summary>
    /// 更新视觉表现 - 统一从SpriteManager获取资源
    /// </summary>
    void UpdateVisual()
    {
        if (sprites == null) return;

        // 未翻开状态
        if (!isRevealed)
        {
            // 背景：未翻开的凸起方块
            if (cellImage != null)
            {
                cellImage.sprite = sprites.coveredSprite;
            }

            // 内容：旗帜或无
            if (isFlagged)
            {
                contentImage.enabled = true;
                contentImage.sprite = sprites.flagSprite;
            }
            else
            {
                contentImage.enabled = false;
            }
        }
        // 已翻开状态
        else
        {
            // 背景：已翻开的平坦方块
            if (cellImage != null)
            {
                cellImage.sprite = sprites.revealedSprite;
            }

            if (isMine)
            {
                // 显示地雷
                contentImage.enabled = true;
                contentImage.sprite = sprites.mineSprite;
            }
            else if (adjacentMines > 0)
            {
                // 显示数字（1-8）
                contentImage.enabled = true;
                contentImage.sprite = sprites.GetNumberSprite(adjacentMines - 1);
            }
            else
            {
                // 空白格子
                contentImage.enabled = false;
            }
        }
    }

    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameController == null) return;

        // 左键点击 - 翻开格子
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameController.RevealCell(x, y);
        }
        // 右键点击 - 插旗
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            gameController.ToggleFlag(x, y);
        }
        // 中键点击 - 自动揭露（和弦）
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            gameController.ChordReveal(x, y);
        }
    }
}