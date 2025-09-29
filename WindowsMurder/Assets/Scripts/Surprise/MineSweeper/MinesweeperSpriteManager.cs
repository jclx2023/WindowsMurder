using UnityEngine;

/// <summary>
/// ɨ����Ϸ������Դ������
/// ͳһ��������ͼƬ��Դ
/// </summary>
[CreateAssetMenu(fileName = "MinesweeperSprites", menuName = "Minesweeper/Sprite Manager")]
public class MinesweeperSpriteManager : ScriptableObject
{
    [Header("����״̬����")]
    public Sprite coveredSprite;        // δ�����ĸ��� (empty.png)
    public Sprite revealedSprite;       // �ѷ����Ŀհ׸��� (open1.png)

    [Header("��Ǿ���")]
    public Sprite flagSprite;           // ���� (flag.png)
    public Sprite questionSprite;       // �ʺ� (question.png) - ��ѡ

    [Header("���׾���")]
    public Sprite mineSprite;           // ��ͨ���� (mine.png)
    public Sprite mineDeathSprite;      // ���еĵ��� (mine-death.png)
    public Sprite mineFlaggedSprite;    // ������ (misflagged.png)

    [Header("���־��� 1-8")]
    public Sprite number1;              // digit1.png
    public Sprite number2;              // digit2.png
    public Sprite number3;              // digit3.png
    public Sprite number4;              // digit4.png
    public Sprite number5;              // digit5.png
    public Sprite number6;              // digit6.png
    public Sprite number7;              // digit7.png
    public Sprite number8;              // digit8.png

    [Header("���������־��� 0-9")]
    public Sprite digit0;               // ��Ӧ checked.png ��ר�ŵ�����0
    public Sprite digit1;               // digit_c.png (����ʵ���ļ���)
    public Sprite digit2;
    public Sprite digit3;
    public Sprite digit4;
    public Sprite digit5;
    public Sprite digit6;
    public Sprite digit7;
    public Sprite digit8;
    public Sprite digit9;

    [Header("Ц������")]
    public Sprite smileSprite;          // smile.png
    public Sprite winSprite;            // win.png
    public Sprite deadSprite;           // dead.png
    public Sprite ohhSprite;            // ohh.png

    /// <summary>
    /// ��ȡ���������־��飨1-8��
    /// </summary>
    public Sprite GetNumberSprite(int index)
    {
        switch (index)
        {
            case 0: return number1;
            case 1: return number2;
            case 2: return number3;
            case 3: return number4;
            case 4: return number5;
            case 5: return number6;
            case 6: return number7;
            case 7: return number8;
            default: return null;
        }
    }

    /// <summary>
    /// ��ȡ���������־��飨0-9��
    /// </summary>
    public Sprite GetDigitSprite(int digit)
    {
        switch (digit)
        {
            case 0: return digit0;
            case 1: return digit1;
            case 2: return digit2;
            case 3: return digit3;
            case 4: return digit4;
            case 5: return digit5;
            case 6: return digit6;
            case 7: return digit7;
            case 8: return digit8;
            case 9: return digit9;
            default: return digit0;
        }
    }
}