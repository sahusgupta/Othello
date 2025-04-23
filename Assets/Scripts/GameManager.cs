using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    // Game board references
    public GameObject tokenPrefab;
    public Transform boardParent;
    public AudioSource audioSource;
    public AudioClip placementSound;
    public TextMeshProUGUI gameStatusText;
    public GameObject heldPiece;

    // Board dimensions
    private const int BOARD_SIZE = 8;

    // Game state variables
    private GameObject[,] board = new GameObject[BOARD_SIZE, BOARD_SIZE];
    private bool isBlackTurn = true; // Black always moves first according to rule 1
    private bool gameOver = false;

    // Direction vectors for checking valid moves
    private readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 1),   // Up-Right
        new Vector2Int(1, 0),   // Right
        new Vector2Int(1, -1),  // Down-Right
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, -1), // Down-Left
        new Vector2Int(-1, 0),  // Left
        new Vector2Int(-1, 1)   // Up-Left
    };

    void Start()
    {
        InitializeBoard();
    }

    void Update()
    {
        // Handle reset input
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
            return;
        }

        if (gameOver) return;

        // Update held piece position to follow mouse
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        heldPiece.transform.position = mousePos;

        // Handle player input
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void InitializeBoard()
    {
        // Clear any existing game objects
        foreach (Transform child in boardParent)
        {
            Destroy(child.gameObject);
        }

        // Initialize the board with empty spaces
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                GameObject token = Instantiate(tokenPrefab, new Vector3(x, y, 0), Quaternion.identity, boardParent);
                token.name = $"Token_{x}_{y}";
                board[x, y] = token;

                // Set all tokens to empty state
                Animator animator = token.GetComponent<Animator>();
                animator.Play("Empty");
            }
        }

        // Set initial four pieces in the center as per Figure 1 in the rules
        // Black pieces
        PlaceInitialPiece(3, 4, true); // D5 in the rules notation
        PlaceInitialPiece(4, 3, true); // E4 in the rules notation

        // White pieces
        PlaceInitialPiece(3, 3, false); // D4 in the rules notation
        PlaceInitialPiece(4, 4, false); // E5 in the rules notation

        // Set initial turn to black
        isBlackTurn = true;
        UpdateHeldPiece();

        gameOver = false;
        gameStatusText.text = "Black's Turn";
    }

    private void PlaceInitialPiece(int x, int y, bool isBlack)
    {
        Animator animator = board[x, y].GetComponent<Animator>();
        animator.Play(isBlack ? "Black Token" : "White Token");
    }

    private void HandleMouseClick()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int x = Mathf.RoundToInt(mousePos.x);
        int y = Mathf.RoundToInt(mousePos.y);

        // Check if click is within board bounds
        if (x < 0 || x >= BOARD_SIZE || y < 0 || y >= BOARD_SIZE)
            return;

        // Check if the move is valid
        List<Vector2Int> piecesToFlip = GetPiecesToFlip(x, y, isBlackTurn);
        if (piecesToFlip.Count == 0)
            return;

        // Place the piece and flip opponent pieces
        PlacePiece(x, y, piecesToFlip);
    }

    private void PlacePiece(int x, int y, List<Vector2Int> piecesToFlip)
    {
        // Play placement sound
        audioSource.PlayOneShot(placementSound);

        // Set the clicked position to current player's color
        Animator clickedAnimator = board[x, y].GetComponent<Animator>();
        clickedAnimator.Play(isBlackTurn ? "Black Token" : "White Token");

        // Start coroutine to flip pieces with delay
        StartCoroutine(FlipPiecesWithDelay(x, y, piecesToFlip));
    }

    private IEnumerator FlipPiecesWithDelay(int placedX, int placedY, List<Vector2Int> piecesToFlip)
    {
        // Sort pieces by distance from placed piece for domino effect
        piecesToFlip.Sort((a, b) =>
        {
            float distA = Vector2.Distance(new Vector2(placedX, placedY), new Vector2(a.x, a.y));
            float distB = Vector2.Distance(new Vector2(placedX, placedY), new Vector2(b.x, b.y));
            return distA.CompareTo(distB);
        });

        // Flip pieces with delay
        foreach (Vector2Int pos in piecesToFlip)
        {
            yield return new WaitForSeconds(0.3f);

            Animator animator = board[pos.x, pos.y].GetComponent<Animator>();
            animator.Play(isBlackTurn ? "Flip to Black" : "Flip to White");

            // Play sound after animation completes
            float animationLength = 0.5f; // Adjust based on actual animation length
            yield return new WaitForSeconds(animationLength);
            audioSource.PlayOneShot(placementSound);
        }

        // Check if game continues after the move
        yield return new WaitForSeconds(0.5f);
        CheckGameState();
    }

    private void CheckGameState()
    {
        // Toggle turn
        isBlackTurn = !isBlackTurn;

        // Check if next player has valid moves
        bool hasValidMoves = HasValidMoves(isBlackTurn);

        if (!hasValidMoves)
        {
            // According to rule 2, if a player cannot make a valid move, they forfeit their turn
            // Check if the other player has moves
            bool otherPlayerHasMoves = HasValidMoves(!isBlackTurn);

            if (otherPlayerHasMoves)
            {
                // Current player has no moves, switch back to other player
                isBlackTurn = !isBlackTurn;
                string currentTurn = isBlackTurn ? "Black" : "White";
                gameStatusText.text = $"{currentTurn}'s Turn (Opponent has no valid moves)";
            }
            else
            {
                // Game over - neither player has valid moves (according to rule 10)
                EndGame();
                return;
            }
        }
        else
        {
            // Update turn display
            string currentTurn = isBlackTurn ? "Black" : "White";
            gameStatusText.text = $"{currentTurn}'s Turn";
        }

        // Update the held piece color
        UpdateHeldPiece();
    }

    private void EndGame()
    {
        gameOver = true;

        // Count pieces to determine the winner (rule 10)
        int blackCount = 0;
        int whiteCount = 0;

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                Animator animator = board[x, y].GetComponent<Animator>();
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

                if (state.IsName("Black Token"))
                    blackCount++;
                else if (state.IsName("White Token"))
                    whiteCount++;
            }
        }

        // Determine winner according to the rules
        if (blackCount > whiteCount)
            gameStatusText.text = $"Game Over! Black wins {blackCount}-{whiteCount}";
        else if (whiteCount > blackCount)
            gameStatusText.text = $"Game Over! White wins {whiteCount}-{blackCount}";
        else
            gameStatusText.text = $"Game Over! Tie game {blackCount}-{whiteCount}";

        gameStatusText.text += "\nPress 'R' to restart";
    }

    private void UpdateHeldPiece()
    {
        Animator heldPieceAnimator = heldPiece.GetComponent<Animator>();
        heldPieceAnimator.Play(isBlackTurn ? "Black Token" : "White Token");
    }

    private List<Vector2Int> GetPiecesToFlip(int x, int y, bool isBlackTurn)
    {
        // Check if position is already occupied
        Animator clickedAnimator = board[x, y].GetComponent<Animator>();
        AnimatorStateInfo state = clickedAnimator.GetCurrentAnimatorStateInfo(0);

        if (!state.IsName("Empty"))
            return new List<Vector2Int>();

        List<Vector2Int> piecesToFlip = new List<Vector2Int>();

        // Check in all 8 directions
        foreach (Vector2Int dir in directions)
        {
            List<Vector2Int> piecesInDirection = GetPiecesToFlipInDirection(x, y, dir.x, dir.y, isBlackTurn);
            piecesToFlip.AddRange(piecesInDirection);
        }

        return piecesToFlip;
    }

    private List<Vector2Int> GetPiecesToFlipInDirection(int x, int y, int dx, int dy, bool isBlackTurn)
    {
        List<Vector2Int> piecesToFlip = new List<Vector2Int>();

        int currentX = x + dx;
        int currentY = y + dy;

        // Check if next position is on the board
        if (currentX < 0 || currentX >= BOARD_SIZE || currentY < 0 || currentY >= BOARD_SIZE)
            return new List<Vector2Int>();

        // Check if next position has opponent's piece
        Animator nextAnimator = board[currentX, currentY].GetComponent<Animator>();
        AnimatorStateInfo nextState = nextAnimator.GetCurrentAnimatorStateInfo(0);

        bool isNextBlack = nextState.IsName("Black Token");
        bool isNextWhite = nextState.IsName("White Token");

        if ((isBlackTurn && isNextWhite) || (!isBlackTurn && isNextBlack))
        {
            // Found opponent's piece, add to potential flips
            piecesToFlip.Add(new Vector2Int(currentX, currentY));

            // Continue in this direction
            while (true)
            {
                currentX += dx;
                currentY += dy;

                // Check if we're still on the board
                if (currentX < 0 || currentX >= BOARD_SIZE || currentY < 0 || currentY >= BOARD_SIZE)
                    return new List<Vector2Int>(); // Off board, invalid move

                Animator currentAnimator = board[currentX, currentY].GetComponent<Animator>();
                AnimatorStateInfo currentState = currentAnimator.GetCurrentAnimatorStateInfo(0);

                if (currentState.IsName("Empty"))
                    return new List<Vector2Int>(); // Empty space, invalid move

                bool isCurrentBlack = currentState.IsName("Black Token");

                if ((isBlackTurn && isCurrentBlack) || (!isBlackTurn && !isCurrentBlack))
                {
                    // Found our own piece, move is valid
                    return piecesToFlip;
                }

                // Found another opponent piece, add to list and continue
                piecesToFlip.Add(new Vector2Int(currentX, currentY));
            }
        }

        return new List<Vector2Int>();
    }

    private bool HasValidMoves(bool forBlackPlayer)
    {
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                Animator animator = board[x, y].GetComponent<Animator>();
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

                if (state.IsName("Empty"))
                {
                    List<Vector2Int> piecesToFlip = GetPiecesToFlip(x, y, forBlackPlayer);
                    if (piecesToFlip.Count > 0)
                        return true;
                }
            }
        }

        return false;
    }

    public void ResetGame()
    {
        InitializeBoard();
    }
}