using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ɨ����ϷUI������ - �����
/// </summary>
public class MinesweeperUI : MonoBehaviour
{
    [Header("��Ϸ����")]
    public MinesweeperGame game;
    public MinesweeperSpriteManager spriteManager;

    [Header("���׼�������3λ���֣�")]
    public Image mineDigit100;          // ��λ
    public Image mineDigit10;           // ʮλ
    public Image mineDigit1;            // ��λ

    [Header("ʱ���������3λ���֣�")]
    public Image timeDigit100;          // ��λ
    public Image timeDigit10;           // ʮλ
    public Image timeDigit1;            // ��λ

    [Header("Ц����ť")]
    public Button smileButton;          // Ц����ť
    public Image smileIcon;             // Ц��ͼ��

    void Start()
    {
        // ���¼�
        if (game != null)
        {
            game.OnMineCountChanged += UpdateMineCount;
            game.OnTimeChanged += UpdateTime;
            game.OnGameStateChanged += UpdateSmileFace;
        }

        // Ц����ť������¿�ʼ
        if (smileButton != null)
        {
            smileButton.onClick.AddListener(OnSmileClick);
        }

        // ��ʼ��Ц��
        UpdateSmileFace(MinesweeperGame.GameState.Normal);
        UpdateMineCount(game != null ? game.mineCount : 10);  // ��ʾ��ʼ������
        UpdateTime(0);  // ��ʾ 000
    }

    /// <summary>
    /// ���µ��׼�����ʾ
    /// </summary>
    void UpdateMineCount(int remainingMines)
    {
        SetDigitDisplay(mineDigit100, mineDigit10, mineDigit1, remainingMines);
    }

    /// <summary>
    /// ���¼�ʱ����ʾ
    /// </summary>
    void UpdateTime(int seconds)
    {
        SetDigitDisplay(timeDigit100, timeDigit10, timeDigit1, seconds);
    }

    /// <summary>
    /// ������λ������ʾ��ͨ�÷�����
    /// </summary>
    void SetDigitDisplay(Image digit100, Image digit10, Image digit1, int value)
    {
        if (spriteManager == null) return;

        // ���Ʒ�Χ 0-999
        value = Mathf.Clamp(value, 0, 999);

        // �ֽ�Ϊ��λ����
        int hundreds = value / 100;
        int tens = (value % 100) / 10;
        int ones = value % 10;

        // ���þ���
        if (digit100 != null)
            digit100.sprite = spriteManager.GetDigitSprite(hundreds);

        if (digit10 != null)
            digit10.sprite = spriteManager.GetDigitSprite(tens);

        if (digit1 != null)
            digit1.sprite = spriteManager.GetDigitSprite(ones);
    }

    /// <summary>
    /// ����Ц������
    /// </summary>
    void UpdateSmileFace(MinesweeperGame.GameState state)
    {
        if (smileIcon == null || spriteManager == null) return;

        switch (state)
        {
            case MinesweeperGame.GameState.Normal:
                smileIcon.sprite = spriteManager.smileSprite;
                break;
            case MinesweeperGame.GameState.Win:
                smileIcon.sprite = spriteManager.winSprite;
                break;
            case MinesweeperGame.GameState.Lose:
                smileIcon.sprite = spriteManager.deadSprite;
                break;
        }
    }

    /// <summary>
    /// ���Ц�����¿�ʼ
    /// </summary>
    void OnSmileClick()
    {
        if (game != null)
        {
            game.RestartGame();
        }
    }

    void OnDestroy()
    {
        // ȡ���¼�����
        if (game != null)
        {
            game.OnMineCountChanged -= UpdateMineCount;
            game.OnTimeChanged -= UpdateTime;
            game.OnGameStateChanged -= UpdateSmileFace;
        }
    }
}