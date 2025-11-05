using Core;
using UI;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

public class BoardUI : MonoBehaviour {
    public bool showThreats;

    private Board _board;
    private readonly BoardSquare[,] _squares = new BoardSquare[8, 8];
    private readonly SpriteRenderer[,] _squarePieceRenderers = new SpriteRenderer[8, 8];

    public BoardTheme boardTheme;
    public PieceTheme pieceTheme;
    private const float PieceDepth = -0.1f;
    private const float PieceDragDepth = -0.3f;
    private const float PieceScale = 4.5f;
    private Camera _cam;


    private void Awake() {
        _cam = Camera.main;
        CreateBoardUI();
    }

    private void CreateBoardUI() {
        var boardGO = new GameObject("Chess board");
        boardGO.transform.parent = transform;
        boardGO.transform.position = new Vector3(-3.5f, -3.5f, 0f);
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var squareGO = new GameObject(BoardUtils.SquareName(file, rank));
            squareGO.transform.SetParent(boardGO.transform, false);
            squareGO.transform.localPosition = new Vector3(file, rank, 0f);
            var square = squareGO.transform.AddComponent<BoardSquare>();
            square.Init(Coord.Create(file, rank), boardTheme);

            var pieceGO = new GameObject("Piece");
            pieceGO.transform.SetParent(squareGO.transform, false);
            var pieceRenderer = pieceGO.AddComponent<SpriteRenderer>();
            pieceRenderer.transform.localPosition = new Vector3(0f, 0f, PieceDepth);
            pieceRenderer.transform.localScale = Vector3.one / PieceScale;

            _squares[file, rank] = square;
            _squarePieceRenderers[file, rank] = pieceRenderer;
        }


        ResetSquares();

        Debug.Log("Done");
    }

    public void Update() {
    }

    public void UpdatePosition(Board board) {
        _board = board;
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var piece = board.GetPiece(file, rank);
            _squarePieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite(piece);
        }
    }

    // Update is called once per frame
    public void ResetSquares() {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            _squares[file, rank].SetHighlighted(false);
            _squares[file, rank].ShowMoveMarker(false);
        }

        if (showThreats) HighlightAllThreats();
    }

    public bool TryGetSquareUnderMouse(out Coord selectedCoord) {
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        var file = mousePos.x + 4f;
        var rank = mousePos.y + 4f;
        selectedCoord = Coord.Create((int)file, (int)rank);
        return file is >= 0 and < 8 && rank is >= 0 and < 8;
    }

    public BoardSquare GetSquare(Coord coord) {
        return _squares[coord.file, coord.rank];
    }

    public void DeselectSquares() {
        ResetSquares();
    }

    public void HighlightSquare(Coord square) {
        GetSquare(square).SetHighlighted(true);
    }

    public void HighlightValidMoves(Coord square) {
        var generator = new MoveGenerator(_board);
        foreach (var move in generator.ValidMovesForSquare(square)) GetSquare(move.To).ShowMoveMarker(true);
    }

    public void HighlightThreats(Coord square) {
        var generator = new MoveGenerator(_board);
        foreach (var attackedSquare in generator.GetAttackedSquares(square))
            GetSquare(attackedSquare).SetHighlighted(true);
    }

    public void HighlightAllThreats() {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var piece = _board.GetPiece(file, rank);
            if (Piece.IsColor(piece, _board.OpponentColor)) HighlightThreats(Coord.Create(file, rank));
        }
    }

    public void DragPiece(Coord square) {
        var piece = _squarePieceRenderers[square.file, square.rank];
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        piece.transform.position = new Vector3(mousePos.x, mousePos.y, PieceDragDepth);
    }

    public void ReleasePiece(Coord square) {
        var origPosition = _squares[square.file, square.rank].transform.position;
        var piece = _squarePieceRenderers[square.file, square.rank].transform;
        piece.position = new Vector3(origPosition.x, origPosition.y, PieceDepth);
    }
}