using UnityEngine;

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

            Debug.Log(_squares);
        }

        public int GetPiece(Coord coord) {
            return GetPiece(coord.file, coord.rank);
        }

        public int GetPiece(int file, int rank) {
            return _squares[file, rank];
        }

        public bool MakeMove(Move move) {
            var moveGenerator = new MoveGenerator(this);
            var isValid = moveGenerator.ValidMovesForSquare(move.from).Exists(m => m.Equals(move));

            if (!isValid) return false;
            _squares[move.to.file, move.to.rank] = _squares[move.from.file, move.from.rank];
            _squares[move.from.file, move.from.rank] = Piece.None;

            IsWhitesTurn ^= true;
            return true;
        }

        public bool IsPieceColor(Coord sq, int color) {
            return Piece.IsColor(GetPiece(sq), color);
        }
    }
}