namespace Core.AI {
    public class BoardEvaluator {
        public int Evaluate(Board board) {
            var score = 0;
            foreach ((var piece, Coord _) in board.GetPieceLocations()) {
                score += Piece.Value(piece) * (Piece.IsColor(piece, Piece.White) ? 1 : -1);
            }

            return score;
        }
    }
}