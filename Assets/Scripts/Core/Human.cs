using UnityEngine;
using UnityEngine.InputSystem;

namespace Core {
    public class Human : Player {
        private BoardUI _boardUI;
        private Coord _selectedPieceSquare;
        private bool _isDraggingPiece;
        private MoveGenerator _moveGenerator;
        
        protected override void OnInitialized() {
            _boardUI = FindFirstObjectByType<BoardUI>();
            _moveGenerator = new MoveGenerator(Board);
        }

        public override void Tick() {
            HandleInput();
            if (_isDraggingPiece) _boardUI.DragPiece(_selectedPieceSquare);
        }

        protected override void OnTurnStarted() {
            _moveGenerator.Refresh();
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
            var piece = Board.GetPiece(square.File, square.Rank);
            if (piece != Piece.None) {
                _selectedPieceSquare = square;
                if (Piece.IsColor(piece, Board.IsWhitesTurn ? Piece.White : Piece.Black)) {
                    _boardUI.HighlightValidMoves(_selectedPieceSquare);
                }
                else {
                    _boardUI.HighlightThreats(square);
                    foreach (Coord attackedSquare in _moveGenerator.GetThreats(square))
                        _boardUI.HighlightSquare(attackedSquare);
                }

                _isDraggingPiece = true;
            }
        }

        private void HandleMouseUp() {
            if (_isDraggingPiece) {
                _boardUI.ReleasePiece(_selectedPieceSquare);
                _isDraggingPiece = false;

                if (IsTurnActive && _boardUI.TryGetSquareUnderMouse(out Coord targetSquare)) {
                    if (targetSquare.Equals(_selectedPieceSquare)) return;
                    if (_moveGenerator.ValidateMove(_selectedPieceSquare, targetSquare, out Move validMove)) {
                        ChooseMove(validMove);
                        _moveGenerator.Refresh();
                    }
                }
            }
        }
    }
}