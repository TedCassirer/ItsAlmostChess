using System;
using Core;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

public class BoardUI : MonoBehaviour {
    private MeshRenderer[,] _squareRenderers;
    private SpriteRenderer[,] _squarePieceRenderers;

    public BoardTheme boardTheme;
    public PieceTheme pieceTheme;
    private const float _pieceDepth = -0.1f;
    private const float _pieceDragDepth = -0.2f;
    private const float PieceScale = 4.5f;
    private Camera _cam;


    private void Awake() {
        Debug.Log("Awake");
        CreateBoardUI();
        _cam = Camera.main;
    }

    private void CreateBoardUI() {
        Debug.Log("Creating and stuff");
        var squareShader = Shader.Find("Unlit/Color");
        _squareRenderers = new MeshRenderer[8, 8];
        _squarePieceRenderers = new SpriteRenderer[8, 8];
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var square = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
            square.parent = transform;
            square.name = BoardUtils.SquareName(file, rank);
            square.position = new Vector3(-4f + file, -4f + rank, 0);

            var squareRenderer = square.gameObject.GetComponent<MeshRenderer>();
            squareRenderer.material = new Material(squareShader);

            var pieceRenderer = new GameObject("Piece").AddComponent<SpriteRenderer>();
            pieceRenderer.transform.parent = square;
            pieceRenderer.transform.position = new Vector3(square.position.x, square.position.y, _pieceDepth);
            pieceRenderer.transform.localScale = Vector3.one / PieceScale;

            _squareRenderers[file, rank] = squareRenderer;
            _squarePieceRenderers[file, rank] = pieceRenderer;
        }

        ResetSquares();

        Debug.Log("Done");
    }

    public void Update() {
    }

    public void UpdatePosition(Board board) {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var piece = board.GetSquare(file, rank);
            _squarePieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite(piece);
        }
    }

    // Update is called once per frame
    public void ResetSquares() {
        for (var rank = 0; rank < 8; rank++)
        for (var file = 0; file < 8; file++) {
            var squareRenderer = _squareRenderers[file, rank];
            var color = BoardUtils.IsLightSquare(file, rank)
                ? boardTheme.lightSquares.normal
                : boardTheme.darkSquares.normal;
            squareRenderer.material.color = color;
        }
    }

    public bool TryGetSquareUnderMouse(out Coord selectedCoord) {
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        var file = mousePos.x + 4.5f;
        var rank = mousePos.y + 4.5f;
        selectedCoord = new Coord((int)file, (int)rank);
        return file is >= 0 and < 8 && rank is >= 0 and < 8;
    }

    public void HighlightSquare(Coord coord) {
        var squareRenderer = _squareRenderers[coord.file, coord.rank];
        squareRenderer.material.color = boardTheme.Selected(coord);
    }

    public void DragPiece(Coord square) {
        var piece = _squarePieceRenderers[square.file, square.rank];
        Vector2 mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        piece.transform.position = new Vector3(mousePos.x, mousePos.y, _pieceDragDepth);
    }

    public void ReleasePiece(Coord square) {
        var origPosition = _squareRenderers[square.file, square.rank].transform.position;
        var piece = _squarePieceRenderers[square.file, square.rank].transform;
        piece.position = new Vector3(origPosition.x, origPosition.y, _pieceDepth);
    }
}