using UnityEngine.InputSystem;

namespace Core {
    public class Human {
        private readonly BoardUI _boardUI;
        private readonly Board _board;
        private Coord _selectedPieceSquare;
        private bool _isDraggingPiece;
        private MoveGenerator _moveGenerator;

        public Human(Board board, BoardUI boardUI, MoveGenerator moveGenerator) {
            _board = board;
            _boardUI = boardUI;
            _moveGenerator = moveGenerator;
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

                if (_boardUI.TryGetSquareUnderMouse(out Coord targetSquare)) {
                    if (targetSquare.Equals(_selectedPieceSquare)) return;
                    if (_moveGenerator.ValidateMove(_selectedPieceSquare, targetSquare, out Move? validMove)) {
                        _board.CommitMove(validMove.Value);
                        _moveGenerator.Refresh();
                        _boardUI.ResetSquares();
                        _boardUI.UpdatePosition(_board);
                    }
                }
            }
        }
    }
}