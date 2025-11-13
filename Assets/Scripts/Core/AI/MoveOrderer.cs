namespace Core.AI {
    public class MoveOrderer {
        public int ScoreMove(Move move) {
            var score = 0;
            if (move.IsCapture) {
                // Captures are prioritized based on the value of the captured piece
                score += 100 + Piece.Value(move.CapturedPiece) - Piece.Value(move.MovePiece);
            }

            if (move.IsPromotion) {
                // Promotions are highly prioritized
                score += 200 + Piece.Value(move.PromotionPiece);
            }

            return score;
        }
    }
}