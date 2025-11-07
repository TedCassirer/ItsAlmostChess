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
    private MoveGenerator _moveGenerator;
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

            square.ApplyTheme(boardTheme);
        }
    }

    void DeleteBoardUI() {
        foreach (Transform child in transform) {
            DestroyImmediate(child.gameObject);
        }
    }


    void OnEnable() {
        // Subscribe to theme change
        CreateBoardUI();
        if (boardTheme != null)
            boardTheme.Changed += OnThemeChanged;
    }

    void OnDisable() {
        // Always unsubscribe to avoid leaks
        if (boardTheme != null)
            boardTheme.Changed -= OnThemeChanged;
        DeleteBoardUI();
    }


#if UNITY_EDITOR
    private void OnValidate() {
        OnThemeChanged();
    }
#endif

    private void OnThemeChanged() {
        Debug.Log("Theme changed!");
        foreach (var square in GetComponentsInChildren<BoardSquare>(true))
            square.ApplyTheme(boardTheme);
    }

    public void UpdatePieces(Board board) {
        _board = board;
        _moveGenerator = new MoveGenerator(board);
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var piece = board.GetPiece(file, rank);
            _squarePieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite(piece);
        }

        ResetSquares();
    }

    public void OnMoveChosen(Move move, bool animate = false) {
        SpriteRenderer pieceRenderer = _squarePieceRenderers[move.From.File, move.From.Rank];
        BoardSquare targetSquare = _squares[move.To.File, move.To.Rank];
        if (animate) {
            StartCoroutine(AnimateMove(pieceRenderer, targetSquare));
        }
        else {
            UpdatePieces(_board);
            ResetSquares();
        }
    }

    private IEnumerator AnimateMove(SpriteRenderer pieceRenderer, BoardSquare targetSquare) {
        Vector3 startPos = pieceRenderer.transform.position;
        Vector3 endPos = targetSquare.transform.position + new Vector3(0f, 0f, PieceDepth);
        float elapsed = 0f;

        while (elapsed < animationSpeed) {
            pieceRenderer.transform.position = Vector3.Lerp(startPos, endPos, elapsed / animationSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        pieceRenderer.transform.localPosition = new Vector3(0f, 0f, PieceDepth);
        UpdatePieces(_board);
        ResetSquares();
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
        return _squares[coord.File, coord.Rank];
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
        foreach (var attackedSquare in generator.GetThreats(square))
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
        var piece = _squarePieceRenderers[square.File, square.Rank];
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        piece.transform.position = new Vector3(mousePos.x, mousePos.y, PieceDragDepth);
    }

    public void ReleasePiece(Coord square) {
        var piece = _squarePieceRenderers[square.File, square.Rank].transform;
        piece.localPosition = new Vector3(0, 0, PieceDepth);
    }
}