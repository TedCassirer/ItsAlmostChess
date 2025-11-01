using UnityEngine;

namespace Core {
    public class Board {
        private int[,] squares = new int[8, 8];


        public void LoadFENPosition(string fen) {
            int rank = 7;
            int file = 0;
            foreach (char symbol in fen) {
                if (symbol == '/') {
                    rank--;
                    file = 0;
                }
                else if (char.IsDigit(symbol)) {
                    file += (int)char.GetNumericValue(symbol);
                }
                else {
                    int piece = Piece.FromFENSymbol(symbol);
                    squares[file, rank] = piece;
                    file++;
                }
            }
            Debug.Log(squares);
        }

        public int GetSquare(int file, int rank) {
            return squares[file, rank];
        }
    }
    
    
}