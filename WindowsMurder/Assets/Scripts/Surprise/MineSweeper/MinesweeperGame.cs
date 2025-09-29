using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 扫雷游戏主控制器 - 简化版
/// </summary>
public class MinesweeperGame : MonoBehaviour
{
    [Header("游戏设置")]
    public int width = 9;
    public int height = 9;
    public int mineCount = 10;

    [Header("精灵管理器")]
    public MinesweeperSpriteManager spriteManager;  // 统一管理所有精灵

    [Header("格子预制体")]
    public GameObject cellPrefab;

    [Header("游戏容器")]
    public Transform gameBoard;

    [Header("格子大小")]
    public float cellSize = 16f;

    private MinesweeperCell[,] cells;
    private int remainingCells;
    private int flaggedCount = 0;
    private bool gameStarted = false;
    private bool gameOver = false;
    private float gameTime = 0f;

    // 事件
    public System.Action<int> OnMineCountChanged;      // 剩余地雷数变化
    public System.Action<int> OnTimeChanged;           // 时间变化（秒）
    public System.Action<GameState> OnGameStateChanged; // 游戏状态变化

    public enum GameState
    {
        Normal,     // 正常游戏中
        Win,        // 胜利
        Lose        // 失败
    }

    public GameState CurrentState { get; private set; } = GameState.Normal;

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (gameStarted && !gameOver)
        {
            gameTime += Time.deltaTime;
            OnTimeChanged?.Invoke(Mathf.Min(Mathf.FloorToInt(gameTime), 999));
        }
    }

    /// <summary>
    /// 初始化游戏
    /// </summary>
    public void InitializeGame()
    {
        // 清空旧的游戏板
        if (gameBoard != null)
        {
            foreach (Transform child in gameBoard)
            {
                Destroy(child.gameObject);
            }
        }

        cells = new MinesweeperCell[width, height];
        remainingCells = width * height - mineCount;
        flaggedCount = 0;
        gameStarted = false;
        gameOver = false;
        gameTime = 0f;
        CurrentState = GameState.Normal;

        // 创建格子
        CreateCells();

        // 更新UI
        OnMineCountChanged?.Invoke(mineCount);
        OnTimeChanged?.Invoke(0);
        OnGameStateChanged?.Invoke(GameState.Normal);
    }

    /// <summary>
    /// 创建所有格子
    /// </summary>
    void CreateCells()
    {
        // 确保 GameBoard 的锚点和轴心在左上角
        RectTransform boardRect = gameBoard.GetComponent<RectTransform>();
        if (boardRect != null)
        {
            boardRect.pivot = new Vector2(0, 1);  // 左上角为轴心
            boardRect.anchorMin = new Vector2(0, 1);
            boardRect.anchorMax = new Vector2(0, 1);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gameBoard);

                // 设置位置 - 从左上角开始排列
                RectTransform rect = cellObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                // 初始化格子
                MinesweeperCell cell = cellObj.GetComponent<MinesweeperCell>();
                cell.Initialize(x, y, this, spriteManager);

                cells[x, y] = cell;
            }
        }
    }

    /// <summary>
    /// 第一次点击时生成地雷
    /// </summary>
    public void GenerateMines(int firstClickX, int firstClickY)
    {
        if (gameStarted) return;

        gameStarted = true;

        // 随机放置地雷
        int minesPlaced = 0;
        while (minesPlaced < mineCount)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // 不在第一次点击的位置及周围放置地雷
            if (Mathf.Abs(x - firstClickX) <= 1 && Mathf.Abs(y - firstClickY) <= 1)
                continue;

            if (!cells[x, y].IsMine)
            {
                cells[x, y].SetMine(true);
                minesPlaced++;
            }
        }

        // 计算每个格子周围的地雷数
        CalculateAdjacentMines();
    }

    /// <summary>
    /// 计算所有格子周围的地雷数
    /// </summary>
    void CalculateAdjacentMines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].IsMine) continue;

                int count = 0;
                List<MinesweeperCell> neighbors = GetNeighbors(x, y);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor.IsMine) count++;
                }

                cells[x, y].SetAdjacentMines(count);
            }
        }
    }

    /// <summary>
    /// 获取相邻的格子
    /// </summary>
    public List<MinesweeperCell> GetNeighbors(int x, int y)
    {
        List<MinesweeperCell> neighbors = new List<MinesweeperCell>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    neighbors.Add(cells[nx, ny]);
                }
            }
        }

        return neighbors;
    }

    /// <summary>
    /// 翻开格子
    /// </summary>
    public void RevealCell(int x, int y)
    {
        if (gameOver) return;

        MinesweeperCell cell = cells[x, y];
        if (cell.IsRevealed || cell.IsFlagged) return;

        // 第一次点击时生成地雷
        if (!gameStarted)
        {
            GenerateMines(x, y);
        }

        cell.Reveal();

        // 点到地雷，游戏结束
        if (cell.IsMine)
        {
            GameOver(false);
            return;
        }

        remainingCells--;

        // 如果是空白格子，自动翻开周围
        if (cell.AdjacentMines == 0)
        {
            List<MinesweeperCell> neighbors = GetNeighbors(x, y);
            foreach (var neighbor in neighbors)
            {
                if (!neighbor.IsRevealed && !neighbor.IsFlagged)
                {
                    RevealCell(neighbor.X, neighbor.Y);
                }
            }
        }

        // 检查是否胜利
        if (remainingCells == 0)
        {
            GameOver(true);
        }
    }

    /// <summary>
    /// 切换旗帜状态
    /// </summary>
    public void ToggleFlag(int x, int y)
    {
        if (gameOver) return;

        MinesweeperCell cell = cells[x, y];
        if (cell.IsRevealed) return;

        if (cell.IsFlagged)
        {
            cell.SetFlag(false);
            flaggedCount--;
        }
        else
        {
            cell.SetFlag(true);
            flaggedCount++;
        }

        OnMineCountChanged?.Invoke(mineCount - flaggedCount);
    }

    /// <summary>
    /// 和弦揭露 - 中键点击已翻开的数字格，自动揭露周围未标记的格子
    /// </summary>
    public void ChordReveal(int x, int y)
    {
        if (gameOver || !gameStarted) return;

        MinesweeperCell cell = cells[x, y];

        // 只有已翻开且有数字的格子才能使用和弦
        if (!cell.IsRevealed || cell.AdjacentMines == 0) return;

        // 统计周围的旗帜数量
        List<MinesweeperCell> neighbors = GetNeighbors(x, y);
        int flagCount = 0;

        foreach (var neighbor in neighbors)
        {
            if (neighbor.IsFlagged) flagCount++;
        }

        // 旗帜数量必须等于数字才能自动揭露
        if (flagCount != cell.AdjacentMines) return;

        // 揭露所有未标记的邻居
        foreach (var neighbor in neighbors)
        {
            if (!neighbor.IsRevealed && !neighbor.IsFlagged)
            {
                RevealCell(neighbor.X, neighbor.Y);
            }
        }
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    void GameOver(bool won)
    {
        gameOver = true;
        CurrentState = won ? GameState.Win : GameState.Lose;

        // 显示所有地雷
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].IsMine)
                {
                    cells[x, y].Reveal();
                }
            }
        }

        OnGameStateChanged?.Invoke(CurrentState);
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        InitializeGame();
    }
}