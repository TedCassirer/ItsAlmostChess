using UnityEngine;

namespace Core {
    public class Board {
        private int[,] _squares = new int[8, 8];


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

        public int GetSquare(int file, int rank) {
            return _squares[file, rank];
        }

        public bool MakeMove(Move move) {
            if (move.IsInValid) return false;
            Debug.Log("Making move");
            _squares[move.to.file, move.to.rank] = _squares[move.from.file, move.from.rank];
            _squares[move.from.file, move.from.rank] = Piece.None;
            return true;
        }
    }
}