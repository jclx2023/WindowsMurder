using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 扫雷游戏UI管理器 - 极简版
/// </summary>
public class MinesweeperUI : MonoBehaviour
{
    [Header("游戏引用")]
    public MinesweeperGame game;
    public MinesweeperSpriteManager spriteManager;

    [Header("地雷计数器（3位数字）")]
    public Image mineDigit100;          // 百位
    public Image mineDigit10;           // 十位
    public Image mineDigit1;            // 个位

    [Header("时间计数器（3位数字）")]
    public Image timeDigit100;          // 百位
    public Image timeDigit10;           // 十位
    public Image timeDigit1;            // 个位

    [Header("笑脸按钮")]
    public Button smileButton;          // 笑脸按钮
    public Image smileIcon;             // 笑脸图标

    void Start()
    {
        // 绑定事件
        if (game != null)
        {
            game.OnMineCountChanged += UpdateMineCount;
            game.OnTimeChanged += UpdateTime;
            game.OnGameStateChanged += UpdateSmileFace;
        }

        // 笑脸按钮点击重新开始
        if (smileButton != null)
        {
            smileButton.onClick.AddListener(OnSmileClick);
        }

        // 初始化笑脸
        UpdateSmileFace(MinesweeperGame.GameState.Normal);
        UpdateMineCount(game != null ? game.mineCount : 10);  // 显示初始地雷数
        UpdateTime(0);  // 显示 000
    }

    /// <summary>
    /// 更新地雷计数显示
    /// </summary>
    void UpdateMineCount(int remainingMines)
    {
        SetDigitDisplay(mineDigit100, mineDigit10, mineDigit1, remainingMines);
    }

    /// <summary>
    /// 更新计时器显示
    /// </summary>
    void UpdateTime(int seconds)
    {
        SetDigitDisplay(timeDigit100, timeDigit10, timeDigit1, seconds);
    }

    /// <summary>
    /// 设置三位数字显示（通用方法）
    /// </summary>
    void SetDigitDisplay(Image digit100, Image digit10, Image digit1, int value)
    {
        if (spriteManager == null) return;

        // 限制范围 0-999
        value = Mathf.Clamp(value, 0, 999);

        // 分解为三位数字
        int hundreds = value / 100;
        int tens = (value % 100) / 10;
        int ones = value % 10;

        // 设置精灵
        if (digit100 != null)
            digit100.sprite = spriteManager.GetDigitSprite(hundreds);

        if (digit10 != null)
            digit10.sprite = spriteManager.GetDigitSprite(tens);

        if (digit1 != null)
            digit1.sprite = spriteManager.GetDigitSprite(ones);
    }

    /// <summary>
    /// 更新笑脸表情
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
    /// 点击笑脸重新开始
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
        // 取消事件监听
        if (game != null)
        {
            game.OnMineCountChanged -= UpdateMineCount;
            game.OnTimeChanged -= UpdateTime;
            game.OnGameStateChanged -= UpdateSmileFace;
        }
    }
}
