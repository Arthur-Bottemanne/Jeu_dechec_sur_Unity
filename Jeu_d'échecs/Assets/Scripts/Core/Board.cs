using UnityEngine;
using UnityEngine.UI;

public enum PieceType
{
    None,
    WhitePawn,
    WhiteKnight,
    WhiteBishop,
    WhiteRook,
    WhiteQueen,
    WhiteKing,
    BlackPawn,
    BlackKnight,
    BlackBishop,
    BlackRook,
    BlackQueen,
    BlackKing
}

public class Board : MonoBehaviour
{
    [Header("TileColors")]
    public Color32 darkSquareColor = new Color32(171, 122, 101, 255);
    public Color32 lightSquareColor = new Color32(238, 216, 192, 255);

    [Header("BoardSize")]
    public static int boardSize = 8;

    [Header("Prefabs")]
    public GameObject tilePrefab;

    // Managers
    private GameManager _gameManager;

    // Tiles
    private Tile[,] _tiles;
    private Piece[,] _pieces;
    private const int XOffset = -450;
    private const int YOffset = -263;

    private static readonly PieceType[,] _initialBoardPosition = new PieceType[8, 8]
    {
        { PieceType.BlackRook, PieceType.BlackKnight, PieceType.BlackBishop, PieceType.BlackQueen, PieceType.BlackKing, PieceType.BlackBishop, PieceType.BlackKnight, PieceType.BlackRook },
        { PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn, PieceType.BlackPawn },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None, PieceType.None },
        { PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn, PieceType.WhitePawn },
        { PieceType.WhiteRook, PieceType.WhiteKnight, PieceType.WhiteBishop, PieceType.WhiteQueen, PieceType.WhiteKing, PieceType.WhiteBishop, PieceType.WhiteKnight, PieceType.WhiteRook }
    };

    private static Board _instance;

    public static Board Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Board>();

                if (_instance == null)
                {
                    GameObject boardObject = new GameObject("Board");
                    _instance = boardObject.AddComponent<Board>();
                }
            }
            return _instance;
        }
    }

    private void Start()
    {
        _tiles = new Tile[boardSize, boardSize];
        _pieces = new Piece[boardSize, boardSize];

        _gameManager = GameManager.Instance;

        InitializeBoard();
    }

    /// <summary>
    /// Initialize the tiles of the board
    /// </summary>
    public void InitializeBoard()
    {
        // cache the size of the tile prefab
        RectTransform tilePrefabRectTransform = tilePrefab.GetComponent<RectTransform>();
        float tileWidth = tilePrefabRectTransform.sizeDelta.x;
        float tileHeight = tilePrefabRectTransform.sizeDelta.y;

        for (int rank = 0; rank < boardSize; rank++)
        {
            for (int file = 0; file < boardSize; file++)
            {
                // Get the position in which the tile should be placed
                Vector3 tilePosition = new Vector3(XOffset + (tileWidth * rank), YOffset + (tileHeight * file), 0);

                // Initialise the tile
                GameObject tileObject = Instantiate(tilePrefab, tilePosition, Quaternion.identity);
                tileObject.transform.SetParent(transform);
                tileObject.name = (char)('a' + rank) + (file + 1).ToString();

                // Change the color of the tile alternating from dark to light squares
                Image tileImage = tileObject.GetComponent<Image>();
                tileImage.color = ((file + rank) % 2 == 0) ? darkSquareColor : lightSquareColor;

                // Add tile to the tiles array
                Tile tileScript = tileObject.GetComponent<Tile>();
                _tiles[file, rank] = tileScript;
                tileScript.DefaultColor = tileImage.color;
            }
        }

        InitializePiece();
    }

    /// <summary>
    /// Initialize the piece according to it's type
    /// </summary>
    /// <param name="pieceType">The type of the piece to initialize.</param>
    /// <param name="pieceObject">The GameObject representing the piece.</param>
    /// <returns></returns>
    private void InitializePiece()
    {
        for (int rank = 0; rank < boardSize; rank++)
        {
            for (int file = 0; file < boardSize; file++)
            {
                Tile pieceTile = _tiles[file, rank];
                GameObject pieceObject = pieceTile.transform.Find("Piece").gameObject;

                // Reverses the array to correspond with how the tiles are generated
                PieceType pieceType = _initialBoardPosition[boardSize - 1 - file, rank];

                if (pieceType == PieceType.None)
                {
                    pieceTile.RemovePiece();
                }
                else
                {
                    Image pieceImage = pieceObject.GetComponent<Image>();
                    SpriteManager.Instance.SetImageSprite(pieceType, pieceImage);

                    if (pieceType != PieceType.None)
                    {
                        pieceTile.InitializePiece(pieceType);
                    }

                    Piece pieceScript = pieceObject.GetComponent<Piece>();

                    pieceObject.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// Moves a piece from a departure tile to a destination tile.
    /// Updates the pieces state, Handling special moves and switching turns.
    /// </summary>
    /// <param name="departureTile">The tile that the piece originates from.</param>
    /// <param name="destinationTile">The tile to move the piece to.</param>
    public void MovePiece(Tile departureTile, Tile destinationTile)
    {
        GameObject pieceObject = departureTile.transform.Find("Piece").gameObject;
        Piece pieceScript = departureTile.OccupyingPiece;

        if (pieceScript is Pawn pawn && destinationTile == pawn.GetCanTakeEnPassantTile)
        {
            HandleEnPassant(pawn);
        }

        Piece.ResetEnPassant(_pieces);

        if (pieceScript is Pawn movingPawn)
        {
            movingPawn.SetPieceHasMoved();
            movingPawn.CheckEnPassant(destinationTile);
        }

        // Move the piece from the departure tile to the destination tile, taking any piece that is on the destination tile.
        destinationTile.RemovePiece();
        destinationTile.PlacePiece(pieceObject);
        departureTile.RemovePiece();

        // End of turn logic
        pieceScript.ResetGeneratedMoves();
        _gameManager.SwitchTurn();
        updatePiecesList();
    }

    /// <summary>
    /// Removes the en passant piece of the pawn.
    /// </summary>
    /// <param name="pawn">Pawn to check for en passant capture</param>
    private void HandleEnPassant(Pawn pawn)
    {
        Piece pieceToTakeEnPassant = pawn.GetPieceToTakeEnPassant;

        if (pieceToTakeEnPassant != null)
        {
            Tile pieceToTakeEnPassantTile = pieceToTakeEnPassant.transform.parent.GetComponent<Tile>();
            pieceToTakeEnPassantTile.RemovePiece();
            updatePiecesList();
        }
    }

    /// <summary>
    /// Checks if the specified file and rank are within the board limits.
    /// </summary>
    /// <param name="fileToMove">The file to move the piece to.</param>
    /// <param name="rankToMove">The rank to move the piece to.</param>
    /// <returns>True if the position is within the board limits, false otherwise.</returns>
    public static bool IsInBoardLimits(int fileToMove, int rankToMove)
    {
        bool isInBoardLimits = (fileToMove >= 0 && rankToMove >= 0 && fileToMove < boardSize && rankToMove < boardSize);

        return isInBoardLimits;
    }

    private void updatePiecesList()
    {
        for (int rank = 0; rank < boardSize; rank++)
        {
            for (int file = 0; file < boardSize; file++)
            {
                Tile tile = _tiles[file, rank];

                Piece piece = tile.OccupyingPiece;

                _pieces[file, rank] = piece;
            }
        }
    }

    public Tile[,] GetTiles
    {
        get { return _tiles; }
    }

    public Piece[,] GetPieces
    {
        get
        {
            updatePiecesList();

            return _pieces;
        }
    }
}
