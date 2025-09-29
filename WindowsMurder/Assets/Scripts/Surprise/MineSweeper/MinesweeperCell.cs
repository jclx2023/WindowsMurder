using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ɨ�׵�Ԫ�� - �򻯰棬ͳһ��SpriteManager��ȡ��Դ
/// </summary>
public class MinesweeperCell : MonoBehaviour, IPointerClickHandler
{
    [Header("UI���")]
    public Image cellImage;         // �����㣨͹��/ƽ̹��
    public Image contentImage;      // ���ݲ㣨����/����/���ģ�

    private int x, y;
    private bool isMine = false;
    private bool isRevealed = false;
    private bool isFlagged = false;
    private int adjacentMines = 0;

    private MinesweeperGame gameController;
    private MinesweeperSpriteManager sprites;

    // ����
    public int X => x;
    public int Y => y;
    public bool IsMine => isMine;
    public bool IsRevealed => isRevealed;
    public bool IsFlagged => isFlagged;
    public int AdjacentMines => adjacentMines;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Initialize(int x, int y, MinesweeperGame controller, MinesweeperSpriteManager spriteManager)
    {
        this.x = x;
        this.y = y;
        this.gameController = controller;
        this.sprites = spriteManager;

        // ��ʼ״̬��δ����
        UpdateVisual();
    }

    /// <summary>
    /// �����Ƿ�Ϊ����
    /// </summary>
    public void SetMine(bool value)
    {
        isMine = value;
    }

    /// <summary>
    /// ������Χ������
    /// </summary>
    public void SetAdjacentMines(int count)
    {
        adjacentMines = count;
    }

    /// <summary>
    /// ��������״̬
    /// </summary>
    public void SetFlag(bool value)
    {
        isFlagged = value;
        UpdateVisual();
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void Reveal()
    {
        if (isRevealed) return;

        isRevealed = true;
        UpdateVisual();
    }

    /// <summary>
    /// �����Ӿ����� - ͳһ��SpriteManager��ȡ��Դ
    /// </summary>
    void UpdateVisual()
    {
        if (sprites == null) return;

        // δ����״̬
        if (!isRevealed)
        {
            // ������δ������͹�𷽿�
            if (cellImage != null)
            {
                cellImage.sprite = sprites.coveredSprite;
            }

            // ���ݣ����Ļ���
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
        // �ѷ���״̬
        else
        {
            // �������ѷ�����ƽ̹����
            if (cellImage != null)
            {
                cellImage.sprite = sprites.revealedSprite;
            }

            if (isMine)
            {
                // ��ʾ����
                contentImage.enabled = true;
                contentImage.sprite = sprites.mineSprite;
            }
            else if (adjacentMines > 0)
            {
                // ��ʾ���֣�1-8��
                contentImage.enabled = true;
                contentImage.sprite = sprites.GetNumberSprite(adjacentMines - 1);
            }
            else
            {
                // �հ׸���
                contentImage.enabled = false;
            }
        }
    }

    /// <summary>
    /// ���������
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameController == null) return;

        // ������ - ��������
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameController.RevealCell(x, y);
        }
        // �Ҽ���� - ����
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            gameController.ToggleFlag(x, y);
        }
        // �м���� - �Զ���¶�����ң�
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            gameController.ChordReveal(x, y);
        }
    }
}