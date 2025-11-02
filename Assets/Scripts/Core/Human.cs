using UnityEngine;
using UnityEngine.InputSystem;

namespace Core {
    public class Human {
        private readonly BoardUI _boardUI;
        private readonly Board _board;
        private Coord _selectedSquare;
        private bool _isDraggingPiece;

        public Human(Board board, BoardUI boardUI) {
            _board = board;
            _boardUI = boardUI;
        }

        public void Update() {
            HandleInput();
            if (_isDraggingPiece) _boardUI.DragPiece(_selectedSquare);
        }

        private void HandleInput() {
            if (_boardUI.TryGetSquareUnderMouse(out var targetSquare)) {
                if (Mouse.current.leftButton.wasPressedThisFrame) {
                    var piece = _board.GetSquare(targetSquare.file, targetSquare.rank);
                    if (piece != Piece.None) {
                        _selectedSquare = targetSquare;
                        _isDraggingPiece = true;
                    }
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    if (_isDraggingPiece) {
                        _boardUI.ReleasePiece(_selectedSquare);
                        _isDraggingPiece = false;
                        var move = new Move(_selectedSquare, targetSquare);
                        if (_board.MakeMove(move)) {
                            _boardUI.ResetSquares();
                            _boardUI.UpdatePosition(_board);
                        }
                    }
            }
        }
    }
}