using System.Collections;
using Core;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

public class BoardUI : MonoBehaviour {
    public bool showThreats;
    public float animationSpeed = 0.35f;

    private Board _board;
    private readonly BoardSquare[,] _squares = new BoardSquare[8, 8];
    private readonly SpriteRenderer[,] _squarePieceRenderers = new SpriteRenderer[8, 8];

    public BoardTheme boardTheme;
    public PieceTheme pieceTheme;
    private const float PieceDepth = -0.1f;
    private const float PieceDragDepth = -0.3f;
    private const float PieceScale = 4.5f;
    private Camera _cam;
    private Move? _lastMove;


    private void Awake() {
        _cam = Camera.main;
    }

    [ContextMenu("Create Board UI")]
    public void CreateBoardUI() {
        DeleteBoardUI();
        var boardGO = new GameObject("Chess board");
        boardGO.transform.parent = transform;
        boardGO.transform.position = new Vector3(-3.5f, -3.5f, 0f);
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var squareGO = new GameObject(BoardUtils.SquareName(file, rank));
            squareGO.transform.SetParent(boardGO.transform, false);
            var square = BoardSquare.Create(squareGO, Coord.Create(file, rank), boardTheme);
            squareGO.transform.localPosition = new Vector3(file, rank, 0f);
            squareGO.transform.localScale = Vector3.one;

            var pieceGo = new GameObject("Piece");

            var pieceRenderer = pieceGo.AddComponent<SpriteRenderer>();
            pieceRenderer.transform.SetParent(squareGO.transform, false);
            pieceRenderer.transform.localPosition = new Vector3(0f, 0f, PieceDepth);
            pieceRenderer.transform.localScale = Vector3.one / PieceScale;

            _squares[file, rank] = square;
            _squarePieceRenderers[file, rank] = pieceRenderer;
        }
    }

    void DeleteBoardUI() {
        foreach (Transform child in transform) {
            DestroyImmediate(child.gameObject);
        }
    }


    void OnEnable() {
        CreateBoardUI();
    }

    void OnDisable() {
        DeleteBoardUI();
    }
    
    public void UpdatePieces(Board board) {
        _board = board;
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var piece = board.GetPiece(file, rank);
            _squarePieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite(piece);
        }

        ResetSquares();
    }

    public void OnMoveChosen(Move move, bool animate = false) {
        _lastMove = move;
        ResetSquares();
        if (animate) {
            StartCoroutine(AnimateMove(move));
        }
        else {
            UpdatePieces(_board);
        }
        
        BoardSquare fromSquare = _squares[move.From.File, move.From.Rank];
        BoardSquare toSquare = _squares[move.To.File, move.To.Rank];
        fromSquare.MoveIndicatorColor();
        toSquare.MoveIndicatorColor();
    }

    private IEnumerator AnimateMove(Move move) {
        var animateGO = new GameObject("Animate Move");
        var animateRenderer = animateGO.AddComponent<SpriteRenderer>();
        SpriteRenderer fromPieceRenderer = _squarePieceRenderers[move.From.File, move.From.Rank];
        SpriteRenderer toPieceRenderer = _squarePieceRenderers[move.To.File, move.To.Rank];
        BoardSquare toSquare = _squares[move.To.File, move.To.Rank];
        
        Vector3 startPos = new Vector3(fromPieceRenderer.transform.position.x, fromPieceRenderer.transform.position.y, PieceDragDepth);
        Vector3 endPos = new Vector3(toSquare.transform.position.x, toSquare.transform.position.y, PieceDragDepth);
        animateRenderer.transform.position = startPos;
        animateRenderer.transform.localScale = Vector3.one / PieceScale;
        animateRenderer.sprite = pieceTheme.GetPieceSprite(_board.GetPiece(move.To.File, move.To.Rank));
        fromPieceRenderer.sprite = null;
        float elapsed = 0f;

        while (elapsed < animationSpeed) {
            animateRenderer.transform.position = Vector3.Lerp(startPos, endPos, elapsed / animationSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        toPieceRenderer.sprite = pieceTheme.GetPieceSprite(_board.GetPiece(move.To.File, move.To.Rank));
        
        Destroy(animateGO);
    }


    public void Reset() {
        _lastMove = null;
        UpdatePieces(_board);
        ResetSquares();
    }
    
    // Update is called once per frame
    public void ResetSquares() {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            _squares[file, rank].NormalColor();
            _squares[file, rank].ShowMoveMarker(false);
        }
        
        if (_lastMove != null) {
            BoardSquare fromSquare = _squares[_lastMove.Value.From.File, _lastMove.Value.From.Rank];
            BoardSquare toSquare = _squares[_lastMove.Value.To.File, _lastMove.Value.To.Rank];
            fromSquare.MoveIndicatorColor();
            toSquare.MoveIndicatorColor();
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
        return _squares[coord.File, coord.Rank];
    }

    public void DeselectSquares() {
        ResetSquares();
    }

    public void HighlightSquare(Coord square) {
        GetSquare(square).HighlightColor();
    }

    public void HighlightValidMoves(Coord square) {
        var generator = new MoveGenerator(_board);
        foreach (var move in generator.ValidMovesForSquare(square)) GetSquare(move.To).ShowMoveMarker(true);
    }

    public void HighlightThreats(Coord square) {
        var generator = new MoveGenerator(_board);
        foreach (var attackedSquare in generator.GetThreats(square))
            GetSquare(attackedSquare).HighlightColor();
    }

    public void HighlightAllThreats() {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var piece = _board.GetPiece(file, rank);
            if (Piece.IsColor(piece, _board.OpponentColor)) HighlightThreats(Coord.Create(file, rank));
        }
    }

    public void DragPiece(Coord square) {
        var piece = _squarePieceRenderers[square.File, square.Rank];
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        piece.transform.position = new Vector3(mousePos.x, mousePos.y, PieceDragDepth);
    }

    public void ReleasePiece(Coord square) {
        var piece = _squarePieceRenderers[square.File, square.Rank].transform;
        piece.localPosition = new Vector3(0, 0, PieceDepth);
    }
}