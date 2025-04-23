using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour
{
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;
    public GameObject hoveringPiece;
    public Text winnerText;
    public Text debugText;

    public GameObject boardObject;
    public float boardStartX = -3.5f;
    public float boardStartY = -3.5f;

    private int[,] board = new int[8, 8];
    private GameObject[,] pieceObjects = new GameObject[8, 8];
    private bool isWhiteTurn = true;
    private bool gameOver = false;
    private float cellWidth;
    private float cellHeight;

    void Start()
    {
        CalculateBoardDimensions();
        InitializeBoard();
        winnerText.gameObject.SetActive(false);
        CreateHoveringPiece();
        PrintBoard();
    }

    void Update()
    {
        if (gameOver) return;
        MoveHoveringPiece();

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int col = Mathf.FloorToInt((mousePos.x - boardStartX) / cellWidth);
        int row = 7 - Mathf.FloorToInt((mousePos.y - boardStartY) / cellHeight);
        debugText.text = $"Mouse Pos: ({mousePos.x:F2}, {mousePos.y:F2}) | Grid: ({row}, {col})";

        if (Input.GetMouseButtonDown(0))
        {
            if (IsInsideBoard(row, col))
            {
                Vector3 spawnPos = CellToWorld(row, col);
                debugText.text += $" | Cell World Pos: ({spawnPos.x:F2}, {spawnPos.y:F2})";
            }

            if (IsInsideBoard(row, col) && IsValidMove(row, col, isWhiteTurn))
            {
                StartCoroutine(PlaceAndFlip(row, col));
            }
        }
    }

    IEnumerator PlaceAndFlip(int row, int col)
    {
        yield return StartCoroutine(FlipPieces(row, col, isWhiteTurn));

        board[row, col] = isWhiteTurn ? 1 : 2;
        pieceObjects[row, col] = Instantiate(isWhiteTurn ? whitePiecePrefab : blackPiecePrefab);
        pieceObjects[row, col].transform.position = CellToWorld(row, col);

        PrintBoard();
        CheckGameEnd();

        bool opponentHasMove = HasValidMove(!isWhiteTurn);
        if (opponentHasMove)
        {
            isWhiteTurn = !isWhiteTurn;
            UpdateHoveringPiece();
        }
        else if (!HasValidMove(isWhiteTurn))
        {
            EndGame();
        }
    }

    void InitializeBoard()
    {
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                board[i, j] = 0;

        board[3, 3] = 1; // White
        board[3, 4] = 2; // Black
        board[4, 3] = 2; // Black
        board[4, 4] = 1; // White

        pieceObjects[3, 3] = Instantiate(whitePiecePrefab, CellToWorld(3, 3), Quaternion.identity);
        pieceObjects[3, 4] = Instantiate(blackPiecePrefab, CellToWorld(3, 4), Quaternion.identity);
        pieceObjects[4, 3] = Instantiate(blackPiecePrefab, CellToWorld(4, 3), Quaternion.identity);
        pieceObjects[4, 4] = Instantiate(whitePiecePrefab, CellToWorld(4, 4), Quaternion.identity);
    }

    void CalculateBoardDimensions()
    {
        SpriteRenderer sr = boardObject.GetComponent<SpriteRenderer>();
        cellWidth = sr.bounds.size.x / 8f;
        cellHeight = sr.bounds.size.y / 8f;
    }

    void CreateHoveringPiece()
    {
        hoveringPiece = Instantiate(isWhiteTurn ? whitePiecePrefab : blackPiecePrefab);
        hoveringPiece.GetComponent<Collider2D>().enabled = false;
    }

    void UpdateHoveringPiece()
    {
        Destroy(hoveringPiece);
        CreateHoveringPiece();
    }

    void MoveHoveringPiece()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        hoveringPiece.transform.position = new Vector3(mousePos.x, mousePos.y, 0);
    }

    bool IsInsideBoard(int r, int c)
    {
        return r >= 0 && r < 8 && c >= 0 && c < 8;
    }

    bool IsValidMove(int r, int c, bool isWhite)
    {
        if (board[r, c] != 0) return false;
        int current = isWhite ? 1 : 2;
        int opponent = isWhite ? 2 : 1;

        foreach (Vector2Int dir in directions)
        {
            int i = r + dir.x, j = c + dir.y;
            bool foundOpponent = false;

            while (IsInsideBoard(i, j) && board[i, j] == opponent)
            {
                i += dir.x;
                j += dir.y;
                foundOpponent = true;
            }

            if (foundOpponent && IsInsideBoard(i, j) && board[i, j] == current)
                return true;
        }
        return false;
    }

    IEnumerator FlipPieces(int r, int c, bool isWhite)
    {
        int current = isWhite ? 1 : 2;
        int opponent = isWhite ? 2 : 1;

        foreach (Vector2Int dir in directions)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            int i = r + dir.x, j = c + dir.y;

            while (IsInsideBoard(i, j) && board[i, j] == opponent)
            {
                path.Add(new Vector2Int(i, j));
                i += dir.x;
                j += dir.y;
            }

            if (IsInsideBoard(i, j) && board[i, j] == current)
            {
                foreach (Vector2Int pos in path)
                {
                    board[pos.x, pos.y] = current;
                    Animator anim = pieceObjects[pos.x, pos.y].GetComponent<Animator>();
                    if (isWhite)
                    {
                        anim.SetBool("flippingToBlack", false);
                        anim.SetBool("flippingToWhite", true);
                    }
                    else
                    {
                        anim.SetBool("flippingToWhite", false);
                        anim.SetBool("flippingToBlack", true);
                    }
                }
            }
        }
        yield return null;
    }

    bool HasValidMove(bool checkWhite)
    {
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (IsValidMove(r, c, checkWhite)) return true;
        return false;
    }

    void CheckGameEnd()
    {
        bool full = true;
        for (int i = 0; i < 8 && full; i++)
            for (int j = 0; j < 8 && full; j++)
                if (board[i, j] == 0)
                    full = false;

        if (full || (!HasValidMove(true) && !HasValidMove(false)))
            EndGame();
    }

    void EndGame()
    {
        gameOver = true;
        int white = 0, black = 0;
        foreach (int val in board)
        {
            if (val == 1) white++;
            else if (val == 2) black++;
        }
        if (white > black)
            winnerText.text = "White Wins!";
        else if (black > white)
            winnerText.text = "Black Wins!";
        else
            winnerText.text = "It's a Tie!";
        winnerText.gameObject.SetActive(true);
    }

    Vector3 CellToWorld(int r, int c)
    {
        float x = boardStartX + (c + 0.5f) * cellWidth;
        float y = boardStartY + ((7 - r) + 0.5f) * cellHeight;
        return new Vector3(x, y, 0);
    }

    void PrintBoard()
    {
        string output = "";
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                output += board[r, c] + " ";
            }
            Debug.Log(output);
            output = "";
        }
    }

    private static readonly List<Vector2Int> directions = new List<Vector2Int>
    {
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1), new Vector2Int(-1, -1)
    };
}