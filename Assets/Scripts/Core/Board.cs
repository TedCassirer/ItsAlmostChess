using System;
using System.Collections.Generic;

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
        public Move LastMove;
    }

    public class Board {
        private int[,] _squares = new int[8, 8];
        private Stack<BoardState> _history = new();
        private BoardState _currentState;

        public bool IsWhitesTurn {
            get => _currentState.IsWhitesTurn;
        }

        public int ColorToMove => IsWhitesTurn ? Piece.White : Piece.Black;
        public int OpponentColor => IsWhitesTurn ? Piece.Black : Piece.White;

        public Coord? EnPassantTarget {
            get => _currentState.EnPassantTarget;
        }

        public bool CanCastleKingSide =>
            IsWhitesTurn ? _currentState.WhiteCanCastleKingside : _currentState.BlackCanCastleKingside;

        public bool CanCastleQueenSide =>
            IsWhitesTurn ? _currentState.WhiteCanCastleQueenside : _currentState.BlackCanCastleQueenside;
        
        public void LoadFENPosition(string fen) {
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
            BoardState newState = _currentState;

            _squares[move.To.File, move.To.Rank] = _squares[move.From.File, move.From.Rank];
            _squares[move.From.File, move.From.Rank] = Piece.None;
            if (move.PromotionPiece != Piece.None) {
                // Handle promotion
                _squares[move.To.File, move.To.Rank] = move.PromotionPiece;
            }

            if (move.IsEnPassant) {
                var capturedPawnSquare = move.EnPassantCapturedPawnSquare.Value;
                _squares[capturedPawnSquare.File, capturedPawnSquare.Rank] = Piece.None;
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

                _squares[rookTo.File, rookTo.Rank] = _squares[rookFrom.File, rookFrom.Rank];
                _squares[rookFrom.File, rookFrom.Rank] = Piece.None;
                if (IsWhitesTurn) {
                    newState.WhiteCanCastleKingside = false;
                    newState.WhiteCanCastleQueenside = false;
                }
                else {
                    newState.BlackCanCastleKingside = false;
                    newState.BlackCanCastleQueenside = false;
                }
            }
            

            bool disableWhiteKingside = move.From.Equals(Coord.Create(4, 0)) || move.From.Equals(Coord.Create(7, 0)) || move.To.Equals(Coord.Create(7, 0));
            bool disableWhiteQueenside = move.From.Equals(Coord.Create(4, 0)) || move.From.Equals(Coord.Create(0, 0)) || move.To.Equals(Coord.Create(0, 0));
            bool disableBlackKingside = move.From.Equals(Coord.Create(4, 7)) || move.From.Equals(Coord.Create(7, 7)) || move.To.Equals(Coord.Create(7, 7));
            bool disableBlackQueenside = move.From.Equals(Coord.Create(4, 7)) || move.From.Equals(Coord.Create(0, 7)) || move.To.Equals(Coord.Create(0, 7));


            newState.WhiteCanCastleKingside &= !disableWhiteKingside;
            newState.WhiteCanCastleQueenside &= !disableWhiteQueenside;
            newState.BlackCanCastleKingside &= !disableBlackKingside;
            newState.BlackCanCastleQueenside &= !disableBlackQueenside;

            newState.LastMove = move;
            newState.IsWhitesTurn = !IsWhitesTurn;
            newState.MoveNumber += IsWhitesTurn ? 0 : 1;
            newState.HalfmoveClock++;
            _history.Push(_currentState);
            _currentState = newState;
        }

        public void UndoMove() {
            var move = _currentState.LastMove;
            _squares[move.From.File, move.From.Rank] = _squares[move.To.File, move.To.Rank];
            _squares[move.To.File, move.To.Rank] = move.CapturedPiece;
            if (move.PromotionPiece != Piece.None) {
                // Revert promotion
                _squares[move.From.File, move.From.Rank] = Piece.Pawn | (IsWhitesTurn ? Piece.Black : Piece.White);
            }

            if (move.IsEnPassant) {
                var capturedPawnSquare = move.EnPassantCapturedPawnSquare.Value;
                _squares[capturedPawnSquare.File, capturedPawnSquare.Rank] =
                    Piece.Pawn | (IsWhitesTurn ? Piece.White : Piece.Black);
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
            newBoard._history = new Stack<BoardState>(_history);
            return newBoard;
        }

        public bool IsPieceColor(Coord sq, int color) {
            return Piece.IsColor(GetPiece(sq), color);
        }

        public Coord GetFriendlyKing() {
            int kingPiece = Piece.King | ColorToMove;
            for (var file = 0; file < 8; file++) {
                for (var rank = 0; rank < 8; rank++) {
                    if (_squares[file, rank] == kingPiece) {
                        return Coord.Create(file, rank);
                    }
                }
            }

            return Coord.Invalid;
        }

        public bool IsEmpty(Coord sq) {
            return GetPiece(sq) == Piece.None;
        }

        public Coord FindPiece(int piece) {
            for (var file = 0; file < 8; file++) {
                for (var rank = 0; rank < 8; rank++) {
                    if (_squares[file, rank] == piece) {
                        return Coord.Create(file, rank);
                    }
                }
            }

            throw new Exception("Couldn't find the target piece");
        }

        public IEnumerable<(int, Coord)> GetPieceLocations() {
            for (var file = 0; file < 8; file++) {
                for (var rank = 0; rank < 8; rank++) {
                    if (_squares[file, rank] != Piece.None) {
                        yield return (_squares[file, rank], Coord.Create(file, rank));
                    }
                }
            }
        }
    }
}