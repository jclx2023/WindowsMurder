using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ɨ����Ϸ�������� - �򻯰�
/// </summary>
public class MinesweeperGame : MonoBehaviour
{
    [Header("��Ϸ����")]
    public int width = 9;
    public int height = 9;
    public int mineCount = 10;

    [Header("���������")]
    public MinesweeperSpriteManager spriteManager;  // ͳһ�������о���

    [Header("����Ԥ����")]
    public GameObject cellPrefab;

    [Header("��Ϸ����")]
    public Transform gameBoard;

    [Header("���Ӵ�С")]
    public float cellSize = 16f;

    private MinesweeperCell[,] cells;
    private int remainingCells;
    private int flaggedCount = 0;
    private bool gameStarted = false;
    private bool gameOver = false;
    private float gameTime = 0f;

    // �¼�
    public System.Action<int> OnMineCountChanged;      // ʣ��������仯
    public System.Action<int> OnTimeChanged;           // ʱ��仯���룩
    public System.Action<GameState> OnGameStateChanged; // ��Ϸ״̬�仯

    public enum GameState
    {
        Normal,     // ������Ϸ��
        Win,        // ʤ��
        Lose        // ʧ��
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
    /// ��ʼ����Ϸ
    /// </summary>
    public void InitializeGame()
    {
        // ��վɵ���Ϸ��
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

        // ��������
        CreateCells();

        // ����UI
        OnMineCountChanged?.Invoke(mineCount);
        OnTimeChanged?.Invoke(0);
        OnGameStateChanged?.Invoke(GameState.Normal);
    }

    /// <summary>
    /// �������и���
    /// </summary>
    void CreateCells()
    {
        // ȷ�� GameBoard ��ê������������Ͻ�
        RectTransform boardRect = gameBoard.GetComponent<RectTransform>();
        if (boardRect != null)
        {
            boardRect.pivot = new Vector2(0, 1);  // ���Ͻ�Ϊ����
            boardRect.anchorMin = new Vector2(0, 1);
            boardRect.anchorMax = new Vector2(0, 1);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gameBoard);

                // ����λ�� - �����Ͻǿ�ʼ����
                RectTransform rect = cellObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
                rect.sizeDelta = new Vector2(cellSize, cellSize);

                // ��ʼ������
                MinesweeperCell cell = cellObj.GetComponent<MinesweeperCell>();
                cell.Initialize(x, y, this, spriteManager);

                cells[x, y] = cell;
            }
        }
    }

    /// <summary>
    /// ��һ�ε��ʱ���ɵ���
    /// </summary>
    public void GenerateMines(int firstClickX, int firstClickY)
    {
        if (gameStarted) return;

        gameStarted = true;

        // ������õ���
        int minesPlaced = 0;
        while (minesPlaced < mineCount)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // ���ڵ�һ�ε����λ�ü���Χ���õ���
            if (Mathf.Abs(x - firstClickX) <= 1 && Mathf.Abs(y - firstClickY) <= 1)
                continue;

            if (!cells[x, y].IsMine)
            {
                cells[x, y].SetMine(true);
                minesPlaced++;
            }
        }

        // ����ÿ��������Χ�ĵ�����
        CalculateAdjacentMines();
    }

    /// <summary>
    /// �������и�����Χ�ĵ�����
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
    /// ��ȡ���ڵĸ���
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
    /// ��������
    /// </summary>
    public void RevealCell(int x, int y)
    {
        if (gameOver) return;

        MinesweeperCell cell = cells[x, y];
        if (cell.IsRevealed || cell.IsFlagged) return;

        // ��һ�ε��ʱ���ɵ���
        if (!gameStarted)
        {
            GenerateMines(x, y);
        }

        cell.Reveal();

        // �㵽���ף���Ϸ����
        if (cell.IsMine)
        {
            GameOver(false);
            return;
        }

        remainingCells--;

        // ����ǿհ׸��ӣ��Զ�������Χ
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

        // ����Ƿ�ʤ��
        if (remainingCells == 0)
        {
            GameOver(true);
        }
    }

    /// <summary>
    /// �л�����״̬
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
    /// ���ҽ�¶ - �м�����ѷ��������ָ��Զ���¶��Χδ��ǵĸ���
    /// </summary>
    public void ChordReveal(int x, int y)
    {
        if (gameOver || !gameStarted) return;

        MinesweeperCell cell = cells[x, y];

        // ֻ���ѷ����������ֵĸ��Ӳ���ʹ�ú���
        if (!cell.IsRevealed || cell.AdjacentMines == 0) return;

        // ͳ����Χ����������
        List<MinesweeperCell> neighbors = GetNeighbors(x, y);
        int flagCount = 0;

        foreach (var neighbor in neighbors)
        {
            if (neighbor.IsFlagged) flagCount++;
        }

        // ������������������ֲ����Զ���¶
        if (flagCount != cell.AdjacentMines) return;

        // ��¶����δ��ǵ��ھ�
        foreach (var neighbor in neighbors)
        {
            if (!neighbor.IsRevealed && !neighbor.IsFlagged)
            {
                RevealCell(neighbor.X, neighbor.Y);
            }
        }
    }

    /// <summary>
    /// ��Ϸ����
    /// </summary>
    void GameOver(bool won)
    {
        gameOver = true;
        CurrentState = won ? GameState.Win : GameState.Lose;

        // ��ʾ���е���
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
    /// ���¿�ʼ��Ϸ
    /// </summary>
    public void RestartGame()
    {
        InitializeGame();
    }
}