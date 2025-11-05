using System;

namespace Core {
    public class Board {
        private int[,] _squares = new int[8, 8];
        public bool IsWhitesTurn { get; private set; } = true;


        public int ColorToMove => IsWhitesTurn ? Piece.White : Piece.Black;
        public int OpponentColor => IsWhitesTurn ? Piece.Black : Piece.White;
        
        public void LoadFENPosition(string fen) {
            var rank = 7;
            var file = 0;
            foreach (var symbol in fen)
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

        public int GetPiece(Coord coord) {
            return GetPiece(coord.file, coord.rank);
        }

        public int GetPiece(int file, int rank) {
            return _squares[file, rank];
        }

        public bool MakeMove(Move move) {
            _squares[move.To.file, move.To.rank] = _squares[move.From.file, move.From.rank];
            _squares[move.From.file, move.From.rank] = Piece.None;
            IsWhitesTurn ^= true;
            return true;
        }

        public void UndoMove(Move move) {
            _squares[move.From.file, move.From.rank] = _squares[move.To.file, move.To.rank];
            _squares[move.To.file, move.To.rank] = move.CapturedPiece;
            if (move.PromotionPiece != Piece.None) {
                // Revert promotion
                _squares[move.From.file, move.From.rank] = Piece.Pawn | Piece.Color(move.PromotionPiece);
            }
            IsWhitesTurn ^= true;
        }
        
        
        public Board Clone() {
            var newBoard = new Board();
            Array.Copy(_squares, newBoard._squares, _squares.Length);
            newBoard.IsWhitesTurn = IsWhitesTurn;
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

            throw new Exception("Couldn't find the target king piece");
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
    }
}