using System;
using System.Collections.Generic;
using System.Linq;

namespace Core {
    internal struct BoardState {
        public bool IsWhitesTurn;
        public bool WhiteCanCastleKingside;
        public bool WhiteCanCastleQueenside;
        public bool BlackCanCastleKingside;
        public bool BlackCanCastleQueenside;
        public Coord? EnPassantTarget;
        public int MoveNumber;
        public int HalfmoveClock;
        public Move? LastMove;
    }

    public class Board {
        private int[,] _squares = new int[8, 8];
        private Stack<BoardState> _history = new();
        private BoardState _currentState;

        public bool IsWhitesTurn => _currentState.IsWhitesTurn;

        public int ColorToMove => IsWhitesTurn ? Piece.White : Piece.Black;
        public int OpponentColor => IsWhitesTurn ? Piece.Black : Piece.White;

        public Coord? EnPassantTarget => _currentState.EnPassantTarget;

        public bool CanCastleKingSide =>
            IsWhitesTurn ? _currentState.WhiteCanCastleKingside : _currentState.BlackCanCastleKingside;

        public bool CanCastleQueenSide =>
            IsWhitesTurn ? _currentState.WhiteCanCastleQueenside : _currentState.BlackCanCastleQueenside;

        public void LoadFenPosition(string fen) {
            // renamed from LoadFENPosition per style
            var rank = 7;
            var file = 0;
            Array.Clear(_squares, 0, _squares.Length);
            string[] parts = fen.Split(' ');
            string boardPlacement = parts[0];
            string castlingRights = parts[2];

            _currentState = new BoardState {
                IsWhitesTurn = parts[1] == "w",
                WhiteCanCastleKingside = castlingRights.Contains('K'),
                WhiteCanCastleQueenside = castlingRights.Contains('Q'),
                BlackCanCastleKingside = castlingRights.Contains('k'),
                BlackCanCastleQueenside = castlingRights.Contains('q'),
                EnPassantTarget = parts[3] != "-" ? Coord.Parse(parts[3]) : null,
                HalfmoveClock = int.Parse(parts[4]),
                MoveNumber = int.Parse(parts[5])
            };
            foreach (var symbol in boardPlacement) {
                if (symbol == '/') {
                    rank--;
                    file = 0;
                }
                else if (char.IsDigit(symbol)) {
                    file += (int)char.GetNumericValue(symbol);
                }
                else {
                    var piece = Piece.FromFENSymbol(symbol);
                    _squares[file, rank] = piece;
                    file++;
                }
            }
        }

        public int GetPiece(Coord coord) {
            return GetPiece(coord.File, coord.Rank);
        }

        public int GetPiece(int file, int rank) {
            return _squares[file, rank];
        }

        public void CommitMove(Move move) {
            // Prepare new state snapshot
            BoardState newState = _currentState;
            newState.LastMove = move;
            // Move piece (basic piece relocation first)
            var movingPiece = _squares[move.From.File, move.From.Rank];
            SetPiece(move.From, Piece.None);
            SetPiece(move.To, movingPiece);

            UpdateEnPassantSquare(move, ref newState);

            // Promotion
            if (move.PromotionPiece != Piece.None) ApplyPromotion(move);
            // Special moves
            if (move.IsEnPassant && move.EnPassantCapturedPawnSquare.HasValue) ApplyEnPassantCapture(move);
            if (move.IsCastling) ApplyCastlingRookMove(move, ref newState);

            // Update castling rights after any king/rook movement or capture on rook square
            UpdateCastlingRights(move, ref newState);

            // Advance turn + clocks
            newState.IsWhitesTurn = !IsWhitesTurn;
            newState.MoveNumber = _currentState.MoveNumber + (IsWhitesTurn ? 0 : 1);
            newState.HalfmoveClock = _currentState.HalfmoveClock + 1;
            if (Piece.Type(movingPiece) == Piece.Pawn || move.CapturedPiece != Piece.None) {
                newState.HalfmoveClock = 0;
            }


            // Push previous state and publish new
            _history.Push(_currentState);
            _currentState = newState;
        }

        public void UndoMove() {
            if (!_currentState.LastMove.HasValue) {
                return;
            }

            Move move = _currentState.LastMove.Value;
            _squares[move.From.File, move.From.Rank] = _squares[move.To.File, move.To.Rank];
            if (move.PromotionPiece != Piece.None) {
                // Revert promotion
                _squares[move.From.File, move.From.Rank] = Piece.Pawn | (IsWhitesTurn ? Piece.Black : Piece.White);
            }

            if (move.EnPassantCapturedPawnSquare != null) {
                Coord capturedPawnSquare = move.EnPassantCapturedPawnSquare.Value;
                _squares[move.To.File, move.To.Rank] = Piece.None;
                _squares[capturedPawnSquare.File, capturedPawnSquare.Rank] =
                    Piece.Pawn | (IsWhitesTurn ? Piece.White : Piece.Black);
            }
            else {
                _squares[move.To.File, move.To.Rank] = move.CapturedPiece;
            }

            if (move.IsCastling) {
                Coord rookFrom, rookTo;
                if (move.To.File == 6) {
                    // Kingside
                    rookFrom = Coord.Create(7, move.From.Rank);
                    rookTo = Coord.Create(5, move.From.Rank);
                }
                else {
                    // Queenside
                    rookFrom = Coord.Create(0, move.From.Rank);
                    rookTo = Coord.Create(3, move.From.Rank);
                }

                _squares[rookFrom.File, rookFrom.Rank] = _squares[rookTo.File, rookTo.Rank];
                _squares[rookTo.File, rookTo.Rank] = Piece.None;
            }

            _currentState = _history.Pop();
        }


        public Board Clone() {
            var newBoard = new Board();
            Array.Copy(_squares, newBoard._squares, _squares.Length);
            newBoard._currentState = _currentState;
            newBoard._history = new Stack<BoardState>(new Stack<BoardState>(_history));
            return newBoard;
        }

        public bool IsPieceColor(Coord sq, int color) {
            return Piece.IsColor(GetPiece(sq), color);
        }

        public Coord GetFriendlyKing() {
            var kingPiece = Piece.King | ColorToMove;
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
                if (_squares[file, rank] == kingPiece)
                    return Coord.Create(file, rank);

            return Coord.Invalid;
        }

        public bool IsEmpty(Coord sq) {
            return GetPiece(sq) == Piece.None;
        }

        public Coord FindPiece(int piece) {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
                if (_squares[file, rank] == piece)
                    return Coord.Create(file, rank);

            throw new Exception("Couldn't find the target piece");
        }

        public IEnumerable<(int, Coord)> GetPieceLocations() {
            for (var file = 0; file < 8; file++)
            for (var rank = 0; rank < 8; rank++)
                if (_squares[file, rank] != Piece.None)
                    yield return (_squares[file, rank], Coord.Create(file, rank));
        }

        private void SetPiece(Coord sq, int piece) {
            _squares[sq.File, sq.Rank] = piece;
        }

        private void ApplyPromotion(Move move) {
            SetPiece(move.To, move.PromotionPiece);
        }

        private void ApplyEnPassantCapture(Move move) {
            if (!move.EnPassantCapturedPawnSquare.HasValue) return;
            Coord capturedPawnSquare = move.EnPassantCapturedPawnSquare.Value;
            SetPiece(capturedPawnSquare, Piece.None);
        }

        private void ApplyCastlingRookMove(Move move, ref BoardState newState) {
            Coord rookFrom;
            Coord rookTo;
            var kingside = move.To.File == 6; // destination file 6 => O-O
            if (kingside) {
                rookFrom = Coord.Create(7, move.From.Rank);
                rookTo = Coord.Create(5, move.From.Rank);
            }
            else {
                rookFrom = Coord.Create(0, move.From.Rank);
                rookTo = Coord.Create(3, move.From.Rank);
            }

            SetPiece(rookTo, _squares[rookFrom.File, rookFrom.Rank]);
            SetPiece(rookFrom, Piece.None);
            if (_currentState.IsWhitesTurn) {
                newState.WhiteCanCastleKingside = false;
                newState.WhiteCanCastleQueenside = false;
            }
            else {
                newState.BlackCanCastleKingside = false;
                newState.BlackCanCastleQueenside = false;
            }
        }

        private void UpdateEnPassantSquare(Move move, ref BoardState newState) {
            var movingPiece = GetPiece(move.To);
            if (Piece.Type(movingPiece) == Piece.Pawn && Math.Abs(move.From.Rank - move.To.Rank) == 2) {
                var epRank = (move.From.Rank + move.To.Rank) / 2;
                newState.EnPassantTarget = Coord.Create(move.From.File, epRank);
            }
            else {
                newState.EnPassantTarget = null;
            }
        }

        private void UpdateCastlingRights(Move move, ref BoardState newState) {
            // If king or rook moved or rook was captured on its initial square, revoke corresponding right.
            Coord whiteKingsideRookSquare = Coord.Parse("h1");
            Coord whiteQueensideRookSquare = Coord.Parse("a1");
            Coord blackKingsideRookSquare = Coord.Parse("h8");
            Coord blackQueensideRookSquare = Coord.Parse("a8");
            Coord whiteKingSquare = Coord.Parse("e1");
            Coord blackKingSquare = Coord.Parse("e8");


            bool MovedFromOrTo(Coord c) {
                return move.From.Equals(c) || move.To.Equals(c);
            }

            newState.WhiteCanCastleKingside &=
                !(MovedFromOrTo(whiteKingSquare) || MovedFromOrTo(whiteKingsideRookSquare));
            newState.WhiteCanCastleQueenside &=
                !(MovedFromOrTo(whiteKingSquare) || MovedFromOrTo(whiteQueensideRookSquare));
            newState.BlackCanCastleKingside &=
                !(MovedFromOrTo(blackKingSquare) || MovedFromOrTo(blackKingsideRookSquare));
            newState.BlackCanCastleQueenside &=
                !(MovedFromOrTo(blackKingSquare) || MovedFromOrTo(blackQueensideRookSquare));
        }

        public void LogMoveHistory() {
            Console.WriteLine("\nBoard Move History:");
            foreach (BoardState boardState in _history.Reverse().Skip(1)) {
                Console.WriteLine(
                    $"{boardState.LastMove}, {boardState.MoveNumber} {(boardState.IsWhitesTurn ? "Black" : "White")}");
            }

            Console.WriteLine(
                $"{_currentState.LastMove}, {_currentState.MoveNumber} {(_currentState.IsWhitesTurn ? "Black" : "White")}");
        }
    }
}