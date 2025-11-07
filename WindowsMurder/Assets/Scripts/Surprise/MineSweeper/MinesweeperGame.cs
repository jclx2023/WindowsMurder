using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 鎵浄娓告垙涓绘帶鍒跺櫒 - 绠€鍖栫増
/// </summary>
public class MinesweeperGame : MonoBehaviour
{
    [Header("娓告垙璁剧疆")]
    public int width = 9;
    public int height = 9;
    public int mineCount = 10;

    [Header("绮剧伒绠＄悊鍣?)]
    public MinesweeperSpriteManager spriteManager;  // 缁熶竴绠＄悊鎵€鏈夌簿鐏?
    [Header("鏍煎瓙棰勫埗浣?)]
    public GameObject cellPrefab;

    [Header("娓告垙瀹瑰櫒")]
    public Transform gameBoard;

    [Header("鏍煎瓙澶у皬")]
    public float cellSize = 16f;

    private MinesweeperCell[,] cells;
    private int remainingCells;
    private int flaggedCount = 0;
    private bool gameStarted = false;
    private bool gameOver = false;
    private float gameTime = 0f;

    // 浜嬩欢
    public System.Action<int> OnMineCountChanged;      // 鍓╀綑鍦伴浄鏁板彉鍖?    public System.Action<int> OnTimeChanged;           // 鏃堕棿鍙樺寲锛堢锛?    public System.Action<GameState> OnGameStateChanged; // 娓告垙鐘舵€佸彉鍖?
    public enum GameState
    {
        Normal,     // 姝ｅ父娓告垙涓?        Win,        // 鑳滃埄
        Lose        // 澶辫触
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
    /// 鍒濆鍖栨父鎴?    /// </summary>
    public void InitializeGame()
    {
        // 娓呯┖鏃х殑娓告垙鏉?        if (gameBoard != null)
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

        // 鍒涘缓鏍煎瓙
        CreateCells();

        // 鏇存柊UI
        OnMineCountChanged?.Invoke(mineCount);
        OnTimeChanged?.Invoke(0);
        OnGameStateChanged?.Invoke(GameState.Normal);
    }

    /// <summary>
    /// 鍒涘缓鎵€鏈夋牸瀛?    /// </summary>
    void CreateCells()
    {
        // 纭繚 GameBoard 鐨勯敋鐐瑰拰杞村績鍦ㄥ乏涓婅
        RectTransform boardRect = gameBoard.GetComponent<RectTransform>();
        if (boardRect != null)
        {
            boardRect.pivot = new Vector2(0, 1);  // 宸︿笂瑙掍负杞村績
            boardRect.anchorMin = new Vector2(0, 1);
            boardRect.anchorMax = new Vector2(0, 1);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gameBoard);

                // 璁剧疆浣嶇疆 - 浠庡乏涓婅寮€濮嬫帓鍒?                RectTransform rect = cellObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                // 鍒濆鍖栨牸瀛?                MinesweeperCell cell = cellObj.GetComponent<MinesweeperCell>();
                cell.Initialize(x, y, this, spriteManager);

                cells[x, y] = cell;
            }
        }
    }

    /// <summary>
    /// 绗竴娆＄偣鍑绘椂鐢熸垚鍦伴浄
    /// </summary>
    public void GenerateMines(int firstClickX, int firstClickY)
    {
        if (gameStarted) return;

        gameStarted = true;

        // 闅忔満鏀剧疆鍦伴浄
        int minesPlaced = 0;
        while (minesPlaced < mineCount)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // 涓嶅湪绗竴娆＄偣鍑荤殑浣嶇疆鍙婂懆鍥存斁缃湴闆?            if (Mathf.Abs(x - firstClickX) <= 1 && Mathf.Abs(y - firstClickY) <= 1)
                continue;

            if (!cells[x, y].IsMine)
            {
                cells[x, y].SetMine(true);
                minesPlaced++;
            }
        }

        // 璁＄畻姣忎釜鏍煎瓙鍛ㄥ洿鐨勫湴闆锋暟
        CalculateAdjacentMines();
    }

    /// <summary>
    /// 璁＄畻鎵€鏈夋牸瀛愬懆鍥寸殑鍦伴浄鏁?    /// </summary>
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
    /// 鑾峰彇鐩搁偦鐨勬牸瀛?    /// </summary>
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
    /// 缈诲紑鏍煎瓙
    /// </summary>
    public void RevealCell(int x, int y)
    {
        if (gameOver) return;

        MinesweeperCell cell = cells[x, y];
        if (cell.IsRevealed || cell.IsFlagged) return;

        // 绗竴娆＄偣鍑绘椂鐢熸垚鍦伴浄
        if (!gameStarted)
        {
            GenerateMines(x, y);
        }

        cell.Reveal();

        // 鐐瑰埌鍦伴浄锛屾父鎴忕粨鏉?        if (cell.IsMine)
        {
            GameOver(false);
            return;
        }

        remainingCells--;

        // 濡傛灉鏄┖鐧芥牸瀛愶紝鑷姩缈诲紑鍛ㄥ洿
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

        // 妫€鏌ユ槸鍚﹁儨鍒?        if (remainingCells == 0)
        {
            GameOver(true);
        }
    }

    /// <summary>
    /// 鍒囨崲鏃楀笢鐘舵€?    /// </summary>
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
    /// 鍜屽鸡鎻湶 - 涓敭鐐瑰嚮宸茬炕寮€鐨勬暟瀛楁牸锛岃嚜鍔ㄦ彮闇插懆鍥存湭鏍囪鐨勬牸瀛?    /// </summary>
    public void ChordReveal(int x, int y)
    {
        if (gameOver || !gameStarted) return;

        MinesweeperCell cell = cells[x, y];

        // 鍙湁宸茬炕寮€涓旀湁鏁板瓧鐨勬牸瀛愭墠鑳戒娇鐢ㄥ拰寮?        if (!cell.IsRevealed || cell.AdjacentMines == 0) return;

        // 缁熻鍛ㄥ洿鐨勬棗甯滄暟閲?        List<MinesweeperCell> neighbors = GetNeighbors(x, y);
        int flagCount = 0;

        foreach (var neighbor in neighbors)
        {
            if (neighbor.IsFlagged) flagCount++;
        }

        // 鏃楀笢鏁伴噺蹇呴』绛変簬鏁板瓧鎵嶈兘鑷姩鎻湶
        if (flagCount != cell.AdjacentMines) return;

        // 鎻湶鎵€鏈夋湭鏍囪鐨勯偦灞?        foreach (var neighbor in neighbors)
        {
            if (!neighbor.IsRevealed && !neighbor.IsFlagged)
            {
                RevealCell(neighbor.X, neighbor.Y);
            }
        }
    }

    /// <summary>
    /// 娓告垙缁撴潫
    /// </summary>
    void GameOver(bool won)
    {
        gameOver = true;
        CurrentState = won ? GameState.Win : GameState.Lose;

        // 鏄剧ず鎵€鏈夊湴闆?        for (int x = 0; x < width; x++)
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
    /// 閲嶆柊寮€濮嬫父鎴?    /// </summary>
    public void RestartGame()
    {
        InitializeGame();
    }
}
