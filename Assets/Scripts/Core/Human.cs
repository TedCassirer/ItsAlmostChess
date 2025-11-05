using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core {
    public class Human {
        private readonly BoardUI _boardUI;
        private readonly Board _board;
        private Coord _selectedPieceSquare;
        private int _selectedPiece;
        private bool _isDraggingPiece;

        public Human(Board board, BoardUI boardUI) {
            _board = board;
            _boardUI = boardUI;
        }

        public void Update() {
            HandleInput();
            if (_isDraggingPiece) _boardUI.DragPiece(_selectedPieceSquare);
        }

        private void HandleInput() {
            if (Mouse.current.leftButton.wasReleasedThisFrame) HandleMouseUp();

            if (_boardUI.TryGetSquareUnderMouse(out var targetSquare)) {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    HandleSelectSquare(targetSquare);
                if (Mouse.current.rightButton.wasPressedThisFrame)
                    _boardUI.GetSquare(targetSquare).SetHighlighted(true);
            }
        }

        private void HandleSelectSquare(Coord square) {
            _boardUI.ResetSquares();
            var piece = _board.GetPiece(square.file, square.rank);
            if (piece != Piece.None) {
                _selectedPieceSquare = square;
                _selectedPiece = piece;
                if (Piece.IsColor(piece, _board.IsWhitesTurn ? Piece.White : Piece.Black)) {
                    _boardUI.HighlightValidMoves(_selectedPieceSquare);
                }
                else {
                    var moveGenerator = new MoveGenerator(_board);
                    _boardUI.HighlightThreats(square);
                    foreach (var attackedSquare in moveGenerator.GetAttackedSquares(square))
                        _boardUI.HighlightSquare(attackedSquare);
                }

                _isDraggingPiece = true;
            }
        }

        private void HandleMouseUp() {
            if (_isDraggingPiece) {
                _boardUI.ReleasePiece(_selectedPieceSquare);
                _isDraggingPiece = false;

                if (_boardUI.TryGetSquareUnderMouse(out Coord targetSquare)) {
                    var isCapture = Piece.IsOppositeColor(_selectedPiece, _board.GetPiece(targetSquare));
                    var move = new Move(_selectedPieceSquare, targetSquare, isCapture);
                    if (_board.MakeMove(move)) {
                        _boardUI.ResetSquares();
                        _boardUI.UpdatePosition(_board);
                    }
                }
            }
        }
    }
}