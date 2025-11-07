using UnityEngine;
using UnityEngine.InputSystem;

namespace Core {
    public class Human: Player {
        private BoardUI _boardUI;
        private Coord _selectedPieceSquare;
        private bool _isDraggingPiece;
        private bool _isTurnToPlay;
        private Board _board;
        private MoveGenerator _moveGenerator;
        
        public override void Init(Board board) {
            _board = board;
            _boardUI = FindFirstObjectByType<BoardUI>();
            _moveGenerator = new MoveGenerator(_board);
        }

        public override void Update() {
            HandleInput();
            if (_isDraggingPiece) _boardUI.DragPiece(_selectedPieceSquare);
        }

        public override void NotifyTurnToPlay() {
            _isTurnToPlay = true;
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
            var piece = _board.GetPiece(square.File, square.Rank);
            if (piece != Piece.None) {
                _selectedPieceSquare = square;
                if (Piece.IsColor(piece, _board.IsWhitesTurn ? Piece.White : Piece.Black)) {
                    _boardUI.HighlightValidMoves(_selectedPieceSquare);
                }
                else {
                    var moveGenerator = new MoveGenerator(_board);
                    _boardUI.HighlightThreats(square);
                    foreach (Coord attackedSquare in moveGenerator.GetThreats(square))
                        _boardUI.HighlightSquare(attackedSquare);
                }

                _isDraggingPiece = true;
            }
        }

        private void HandleMouseUp() {
            if (_isDraggingPiece) {
                _boardUI.ReleasePiece(_selectedPieceSquare);
                _isDraggingPiece = false;

                if (_isTurnToPlay && _boardUI.TryGetSquareUnderMouse(out Coord targetSquare)) {
                    if (targetSquare.Equals(_selectedPieceSquare)) return;
                    if (_moveGenerator.ValidateMove(_selectedPieceSquare, targetSquare, out Move validMove)) {
                        _isTurnToPlay = false;
                        ChooseMove(validMove);
                        _moveGenerator.Refresh();
                    }
                }
            }
        }
    }
}